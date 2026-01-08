#nullable enable
using FlaxEngine;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    private readonly FatPointer<float> _heightMap;
    private readonly int _patchStride;
    private bool _disposed;

    private readonly ImmutableArray<ITopographyProvider> _topographyProviders;
    private readonly ImmutableArray<ITopographyPostProcessor> _topographyPostProcessors;

    public FlaxEngine.Terrain Target { get; }
    public Int2 Size { get; }
    public TerrainPatches Patches { get; }

    public TerrainGenerator(FlaxEngine.Terrain terrain, IEnumerable<GraphComponent> models)
    {
        Target = terrain ?? throw new ArgumentNullException(nameof(terrain));

        ArgumentNullException.ThrowIfNull(models, nameof(models));
        _topographyProviders = [.. models.OfType<ITopographyProvider>()];
        _topographyPostProcessors = [.. models.OfType<ITopographyPostProcessor>()];

        _patchMaps = Channel.CreateBounded<IPatchMap>(_boundedChannelOptions);

        Int2 patchCount = CountPatches(terrain);
        _patchStride = terrain.ChunkSize * FlaxEngine.Terrain.PatchEdgeChunksCount;
        Size = (patchCount * _patchStride) + Int2.One;
        Patches = new TerrainPatches(_patchStride + 1, patchCount);

        _heightMap = new FatPointer<float>(Size.X * Size.Y);

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
        Int2Enumerable coordEnumerator = new(Patches.Count);
        await Parallel.ForEachAsync(coordEnumerator, cancellationToken, BuildPatchAsync);
        ApplyPostProcessors((Span<float>)_heightMap);
        await Parallel.ForEachAsync(coordEnumerator, cancellationToken, SetupPatchMapsAsync);
        _patchMaps.Writer.Complete();
        await _patchMaps.Reader.Completion;
    }

    private unsafe ValueTask BuildPatchAsync(Int2 patchCoord, CancellationToken cancellationToken)
    {
        Int2 start = patchCoord * _patchStride;
        Int2 end = start + Patches.Size;

        float invSizeX = 1.0f / Size.X;
        float invSizeY = 1.0f / Size.Y;

        float* rawBuffer = _heightMap.Buffer;
        int totalWidth = Size.X;

        for (int y = start.Y; y < end.Y; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            float v = y * invSizeY;
            int rowOffset = y * totalWidth;

            for (int x = start.X; x < end.X; x++)
            {
                float u = x * invSizeX;
                ref float height = ref rawBuffer[rowOffset + x];
                foreach (ITopographyProvider layer in _topographyProviders)
                {
                    layer.GetHeight(u, v, ref height);
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    private void ApplyPostProcessors(Span<float> heightmap)
    {
        foreach (ITopographyPostProcessor postProcessor in _topographyPostProcessors)
        {
            postProcessor.Apply(heightmap, Size.X);
        }
    }

    private unsafe ValueTask SetupPatchMapsAsync(Int2 patchCoord, CancellationToken cancellationToken)
    {
        PatchHeightMap patchHeightMap = new(patchCoord, Patches.Size);
        try
        {
            SplitPatch(_heightMap.Buffer, Size.X, patchHeightMap.HeightMap, Patches.Size, patchCoord * _patchStride);
            return _patchMaps.Writer.WriteAsync(patchHeightMap, cancellationToken);
        }
        catch
        {
            patchHeightMap.Dispose();
            throw;
        }
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
