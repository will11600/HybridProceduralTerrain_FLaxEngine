#nullable enable
using FlaxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Terrain;

internal sealed class TerrainGenerator : IGenerator<TerrainGenerator>, IDisposable
{
    private static readonly BoundedChannelOptions _boundedChannelOptions = new(500)
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false,
        FullMode = BoundedChannelFullMode.Wait
    };

    private readonly Channel<IPatchMap> _patchMaps;
    private readonly FatPointer2D<float> _heightMap;
    private readonly IEnumerable<GraphComponent> _components;
    private bool _disposed;

    private readonly List<ITopographySampler> _topographySamplers = [];
    private readonly List<ITopographyPostProcessor> _topographyPostProcessors = [];
    private readonly List<ISplatMapLayerWeightSampler> _splatSamplers = [];

    public FlaxEngine.Terrain Target { get; }
    public Int2 Size { get; }
    public TerrainPatches Patches { get; }

    public TerrainGenerator(FlaxEngine.Terrain terrain, IEnumerable<GraphComponent> components)
    {
        Target = terrain ?? throw new ArgumentNullException(nameof(terrain));

        _components = components ?? throw new ArgumentNullException(nameof(components));

        _patchMaps = Channel.CreateBounded<IPatchMap>(_boundedChannelOptions);

        Int2 patchCount = CountPatches(terrain);
        int patchStride = terrain.ChunkSize * FlaxEngine.Terrain.PatchEdgeChunksCount;
        Size = (patchCount * patchStride) + Int2.One;
        Patches = new TerrainPatches(patchStride + 1, patchCount, patchStride);

        _heightMap = new FatPointer2D<float>(Size.X, Size.Y);

        Scripting.Update += OnUpdate;
    }

    public static TerrainGenerator Create(Actor actor, IEnumerable<GraphComponent> components)
    {
        return new((FlaxEngine.Terrain)actor, components);
    }

    private void OnUpdate()
    {
        long startTime = DateTime.Now.Ticks;
        long timeBudget = 5 * 10000;
        while ((DateTime.Now.Ticks - startTime) < timeBudget && _patchMaps.Reader.TryRead(out IPatchMap? patchMap))
        {
            try
            {
                patchMap.Setup(Target);
            }
            finally
            {
                patchMap.Dispose();
            }
        }
    }

    public async Task BuildAsync(CancellationToken cancellationToken)
    {
        int stage = 0;
        foreach (GraphComponent component in _components)
        {
            switch (component)
            {
                case ITopographySampler sampler:
                    stage = await AdvanceStageAsync(0, sampler, _topographySamplers, stage, cancellationToken);
                    break;
                case ITopographyPostProcessor postProcessor:
                    stage = await AdvanceStageAsync(1, postProcessor, _topographyPostProcessors, stage, cancellationToken);
                    break;
                case ISplatMapLayerWeightSampler splatSampler:
                    _splatSamplers.Add(splatSampler);
                    break;
            }
        }

        Int2Enumerable coordEnumerator = await ExecutePass(cancellationToken);
        await Parallel.ForEachAsync(coordEnumerator, cancellationToken, SetupPatchMapsAsync);

        _patchMaps.Writer.Complete();
        await _patchMaps.Reader.Completion;
    }

    private async ValueTask<int> AdvanceStageAsync<T>(int processStage, T processor, List<T> processors, int currentStage, CancellationToken cancellationToken)
    {
        if (processStage < currentStage)
        {
            await ExecutePass(cancellationToken);

            _topographySamplers.Clear();
            _topographyPostProcessors.Clear();
            _splatSamplers.Clear();

            processors.Add(processor);

            return 0;
        }

        processors.Add(processor);

        return processStage;
    }

    private async Task<Int2Enumerable> ExecutePass(CancellationToken cancellationToken)
    {
        Int2Enumerable coordEnumerator = new(Patches.Count);
        await Parallel.ForEachAsync(coordEnumerator, cancellationToken, BuildPatchAsync);
        foreach (ITopographyPostProcessor postProcessor in _topographyPostProcessors)
        {
            postProcessor.Apply(Target, _heightMap);
        }
        return coordEnumerator;
    }

    private unsafe ValueTask BuildPatchAsync(Int2 patchCoord, CancellationToken cancellationToken)
    {
        Int2 start = patchCoord * Patches.Stride;

        Int2 end = start;
        end.X += patchCoord.X == (Patches.Count.X - 1) ? Patches.Size : Patches.Stride;
        end.Y += patchCoord.Y == (Patches.Count.Y - 1) ? Patches.Size : Patches.Stride;

        float invSizeX = 1.0f / Size.X;
        float invSizeY = 1.0f / Size.Y;

        float* rawBuffer = _heightMap.Buffer;
        int totalWidth = Size.X;

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int y = start.Y; y < end.Y; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            float v = y * invSizeY;
            int rowOffset = y * totalWidth;

            for (int x = start.X; x < end.X; x++)
            {
                float u = x * invSizeX;
                ref float height = ref rawBuffer[rowOffset + x];
                foreach (ITopographySampler layer in _topographySamplers)
                {
                    layer.GetHeight(Target, u, v, ref height);
                }

                min = height < min ? height : min;
                max = height > max ? height : max;
            }
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask SetupPatchMapsAsync(Int2 patchCoord, CancellationToken cancellationToken)
    {
        await SetupPatchHeightMapAsync(patchCoord, cancellationToken);

        if (_splatSamplers.Count == 0)
        {
            return;
        }

        foreach (var group in _splatSamplers.GroupBy(SplatMapIndexOf))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int mapIndex = group.Key;
            PatchSplatMap splatMap = new(patchCoord, mapIndex, Patches.Size);

            try
            {
                unsafe { GenerateSplatMapForPatch(splatMap.SplatMap, Patches.Size, patchCoord * Patches.Stride, group); }
            }
            catch
            {
                splatMap.Dispose();
                throw;
            }

            await _patchMaps.Writer.WriteAsync(splatMap, cancellationToken);
        }
    }

    private async ValueTask SetupPatchHeightMapAsync(Int2 patchCoord, CancellationToken cancellationToken)
    {
        PatchHeightMap patchHeightMap = new(patchCoord, Patches.Size);
        try
        {
            unsafe
            {
                SplitPatch(_heightMap.Buffer, Size.X, patchHeightMap.HeightMap, Patches.Size, patchCoord * Patches.Stride);
            }
            
            await _patchMaps.Writer.WriteAsync(patchHeightMap, cancellationToken);
        }
        catch 
        {
            patchHeightMap.Dispose();
            throw;
        }
    }

    private unsafe void GenerateSplatMapForPatch(Color32* destBuffer, int patchSize, Int2 offset, IEnumerable<ISplatMapLayerWeightSampler> samplers)
    {
        float* heightMapBuffer = _heightMap.Buffer;

        int globalWidth = Size.X;
        int globalHeight = Size.Y;

        for (int y = 0; y < patchSize; y++)
        {
            int globalY = offset.Y + y;
            int rowOffset = globalY * globalWidth;

            for (int x = 0; x < patchSize; x++)
            {
                int globalX = offset.X + x;
                int globalIndex = rowOffset + globalX;

                ref float height = ref heightMapBuffer[globalIndex];

                float inclination = CalculateInclination(heightMapBuffer, globalX, globalY, globalWidth, globalHeight, Target.Scale * FlaxEngine.Terrain.UnitsPerVertex);

                ref Color32 color = ref destBuffer[(y * patchSize) + x];
                fixed (Color32* colorPtr = &color)
                {
                    Span<byte> channels = new(colorPtr, sizeof(Color32));
                    foreach (var sampler in samplers)
                    {
                        channels[sampler.LayerIndex % channels.Length] = sampler.ComputeWeight(Target, in height, in inclination);
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe float CalculateInclination(float* buffer, int x, int y, int width, int height, Float3 scale)
    {
        int xL = x > 0 ? x - 1 : x;
        int xR = x < width - 1 ? x + 1 : x;
        int yD = y > 0 ? y - 1 : y;
        int yU = y < height - 1 ? y + 1 : y;

        float hL = buffer[y * width + xL];
        float hR = buffer[y * width + xR];
        float hD = buffer[yD * width + x];
        float hU = buffer[yU * width + x];

        float runX = (xR - xL) * scale.X;
        float runZ = (yU - yD) * scale.Z;

        if (runX <= float.Epsilon) runX = 1.0f;
        if (runZ <= float.Epsilon) runZ = 1.0f;

        float riseX = (hR - hL) * scale.Y;
        float riseZ = (hU - hD) * scale.Y;

        float slopeX = riseX / runX;
        float slopeZ = riseZ / runZ;

        float slope = Mathf.Sqrt(slopeX * slopeX + slopeZ * slopeZ);

        return Mathf.Atan(slope) * Mathf.RadiansToDegrees;
    }

    private static unsafe int SplatMapIndexOf(ISplatMapLayerWeightSampler sampler)
    {
        return sampler.LayerIndex / sizeof(Color32);
    }

    private static Int2 CountPatches(FlaxEngine.Terrain terrain)
    {
        if (terrain.PatchesCount == 0)
        {
            return Int2.Zero;
        }

        Int2 min = Int2.Maximum;
        Int2 max = Int2.Minimum;

        for (int i = 0; i < terrain.PatchesCount; i++)
        {
            terrain.GetPatchCoord(i, out Int2 coord);

            min.X = coord.X < min.X ? coord.X : min.X;
            min.Y = coord.Y < min.Y ? coord.Y : min.Y;

            max.X = coord.X > max.X ? coord.X : max.X;
            max.Y = coord.Y > max.Y ? coord.Y : max.Y;
        }

        max += Int2.One;

        return max - min;
    }

    private static unsafe void SplitPatch<T>(T* srcBuffer, int srcWidth, T* dstBuffer, int dstWidth, Int2 offset) where T : unmanaged
    {
        nuint rowByteCount = (nuint)(dstWidth * sizeof(T));
        for (int y = 0; y < dstWidth; y++)
        {
            T* srcRow = srcBuffer + (((offset.Y + y) * srcWidth) + offset.X);
            T* dstRow = dstBuffer + (y * dstWidth);
            NativeMemory.Copy(srcRow, dstRow, rowByteCount);
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Scripting.Update -= OnUpdate;
            _heightMap.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}