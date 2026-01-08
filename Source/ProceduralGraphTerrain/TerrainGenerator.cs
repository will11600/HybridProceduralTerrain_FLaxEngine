using FlaxEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Terrain;

using TerrainActor = FlaxEngine.Terrain;

internal sealed class TerrainGenerator : IGenerator<TerrainGenerator>, IDisposable
{
    private readonly ConcurrentBag<Patch> _patches;
    private unsafe readonly float* _heightMapPtr;
    private readonly int _heightMapLength;
    private bool _disposed;

    public TerrainActor Target { get; }

    public Int2 Size { get; }

    public TerrainPatches Patches { get; }

    public IEnumerable<GraphComponent> Components { get; }

    public unsafe TerrainGenerator(TerrainActor terrain, IEnumerable<GraphComponent> components)
    {
        Target = terrain ?? throw new ArgumentNullException(nameof(terrain));
        Components = components ?? throw new ArgumentNullException(nameof(components));

        _patches = [];

        Int2 patchCount = CountPatches(terrain);
        int patchStride = terrain.ChunkSize * TerrainActor.PatchEdgeChunksCount;
        Size = (patchCount * patchStride) + Int2.One;
        Patches = new TerrainPatches(patchStride + 1, patchCount, patchStride);

        _heightMapLength = Size.X * Size.Y;
        _heightMapPtr = (float*)NativeMemory.Alloc((nuint)_heightMapLength * sizeof(float));
    }

    ~TerrainGenerator()
    {
        Dispose(false);
    }

    public static TerrainGenerator Create(Actor actor, IEnumerable<GraphComponent> components)
    {
        return new((TerrainActor)actor, components);
    }

    public async Task BuildAsync(CancellationToken cancellationToken)
    {
        Int2Enumerable coordEnumerator = new(Patches.Count);
        try
        {
            await Parallel.ForEachAsync(coordEnumerator, cancellationToken, GeneratePatchAsync);
            ApplyPostProcessors();
            await Parallel.ForEachAsync(_patches, cancellationToken, BuildPatchMapsAsync);
        }
        finally
        {
            foreach (Patch patch in _patches)
            {
                patch.Dispose();
            }
        }
    }

    private unsafe void ApplyPostProcessors()
    {
        foreach (ITopographyPostProcessor postProcessor in Components.OfType<ITopographyPostProcessor>())
        {
            postProcessor.Apply(Target, _heightMapPtr, _heightMapLength, Size.X);
        }
    }

    private unsafe ValueTask BuildPatchMapsAsync(Patch patch, CancellationToken cancellationToken)
    {
        Int2 offset = patch.index * Patches.Stride;

        SplitPatch(offset, _heightMapPtr, patch.HeightMapPtr);

        if (Target.SetupPatchHeightMap(ref patch.index, _heightMapLength, _heightMapPtr)) // Note: Inverted return value
        {
            throw new InvalidOperationException($"Failed to setup patch heightmap ({patch.index})");
        }

        return SetupSlatMapsAsync(patch, offset, cancellationToken);
    }

    private unsafe ValueTask SetupSlatMapsAsync(Patch patch, Int2 offset, CancellationToken cancellationToken)
    {
        ISplatMapLayerWeightSampler[] providers = [.. Components.OfType<ISplatMapLayerWeightSampler>()];
        if (providers.Length == 0)
        {
            return ValueTask.CompletedTask;
        }

        int resolution = Patches.Size * Patches.Size;
        Span<IntPtr> splatMaps = stackalloc IntPtr[(providers.Max(static p => p.LayerIndex) / 4) + 1];
        try
        {
            FillJagged<Color32>(splatMaps, (nuint)resolution);

            for (int z = 0; z < Patches.Size; z++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int rowOffset = z * Patches.Size;
                for (int x = 0; x < Patches.Size; x++)
                {
                    float inclination = CalculateInclination(_heightMapPtr, offset.X + x, offset.Y + z, Size);
                    int index = rowOffset + x;
                    ref float height = ref patch.HeightMapPtr[index];
                    foreach (ISplatMapLayerWeightSampler provider in providers)
                    {
                        int channelIndex = provider.LayerIndex % 4;
                        int mapIndex = provider.LayerIndex / 4;
                        Color32* pixelPtr = ((Color32*)splatMaps[mapIndex]) + index;
                        (*pixelPtr)[channelIndex] = provider.ComputeWeight(Target, in height, in inclination);
                    }
                }
            }

            for (int i = 0; i < splatMaps.Length; i++)
            {
                if (Target.SetupPatchSplatMap(ref patch.index, i, resolution, (Color32*)splatMaps[i])) // Note: Inverted return value
                {
                    throw new InvalidOperationException($"Failed to setup patch splatmap ({patch.index})");
                }
            }
        }
        finally
        {
            foreach (IntPtr ptr in splatMaps)
            {
                NativeMemory.Free((void*)ptr);
            }
        }

        return ValueTask.CompletedTask;
    }

    private unsafe ValueTask GeneratePatchAsync(Int2 index, CancellationToken cancellationToken)
    {
        Patch patch = new(index, Patches.Size * Patches.Size);
        Int2 offset = index * Patches.Stride;
        try
        {
            CalculateHeightMap(offset, patch.HeightMapPtr, patch.HeightMapLength, cancellationToken);
            StitchPatch(offset, patch.HeightMapPtr, _heightMapPtr);
            _patches.Add(patch);
            return ValueTask.CompletedTask;
        }
        catch
        {
            patch.Dispose();
            throw;
        }
    }

    private unsafe void CalculateHeightMap(Int2 offset, float* heightMapPtr, int length, CancellationToken cancellationToken)
    {
        for (int i = 0; i < length; i++)
        {
            int x = i % Patches.Size;
            int z = i / Patches.Size;

            if (x == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float u = (offset.X + x) / (float)Size.X;
            float v = (offset.Y + z) / (float)Size.Y;

            ref float height = ref heightMapPtr[i];
            foreach (ITopographySampler layer in Components.OfType<ITopographySampler>())
            {
                layer.GetHeight(u, v, ref height);
            }
        }
    }

    private unsafe void StitchPatch(Int2 offset, float* sourcePtr, float* destinationPtr)
    {
        long rowSizeBytes = (long)Patches.Size * sizeof(float);

        for (int z = 0; z < Patches.Size; z++)
        {
            float* srcRow = sourcePtr + (z * Patches.Size);

            long destIndex = ((offset.Y + z) * Size.X) + offset.X;
            float* destRow = destinationPtr + destIndex;

            Buffer.MemoryCopy(srcRow, destRow, rowSizeBytes, rowSizeBytes);
        }
    }

    private unsafe void SplitPatch(Int2 offset, float* sourcePtr, float* destinationPtr)
    {
        long rowSizeBytes = (long)Patches.Size * sizeof(float);

        for (int z = 0; z < Patches.Size; z++)
        {
            long srcIndex = ((offset.Y + z) * Size.X) + offset.X;
            float* srcRow = sourcePtr + srcIndex;

            float* destRow = destinationPtr + (z * Patches.Size);

            Buffer.MemoryCopy(srcRow, destRow, rowSizeBytes, rowSizeBytes);
        }
    }

    private static unsafe float CalculateInclination(float* heightMapPtr, int x, int y, Int2 size)
    {
        int xL = x > 0 ? x - 1 : 0;
        int xR = x < size.X - 1 ? x + 1 : size.X - 1;
        int yU = y > 0 ? y - 1 : 0;
        int yD = y < size.Y - 1 ? y + 1 : size.Y - 1;

        float runX = xR - xL;
        float runY = yD - yU;

        float gx = 0f;
        if (runX > 0)
        {
            float hL = heightMapPtr[y * size.X + xL];
            float hR = heightMapPtr[y * size.X + xR];
            gx = (hR - hL) / runX;
        }

        float gy = 0f;
        if (runY > 0)
        {
            float hU = heightMapPtr[yU * size.X + x];
            float hD = heightMapPtr[yD * size.X + x];
            gy = (hD - hU) / runY;
        }

        float slopeMagnitude = Mathf.Sqrt(gx * gx + gy * gy);
        return Mathf.Atan(slopeMagnitude) * Mathf.RadiansToDegrees;
    }

    private static Int2 CountPatches(TerrainActor terrain)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void FillJagged<T>(Span<IntPtr> items, nuint count) where T : unmanaged
    {
        nuint elementSize = (nuint)sizeof(T);
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = (IntPtr)NativeMemory.Alloc(count, elementSize);
        }
    }

    private unsafe void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // dispose managed state
        }

        NativeMemory.Free(_heightMapPtr);

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}