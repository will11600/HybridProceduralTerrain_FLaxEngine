#nullable enable
using FlaxEngine;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Terrain;

using TerrainActor = FlaxEngine.Terrain;

internal readonly struct TerrainGenerator : IGenerator<TerrainGenerator>, IDisposable
{
    private static ArrayPool<float> SharedPool { get; } = ArrayPool<float>.Shared;

    private readonly ConcurrentBag<Patch> _patches;

    private readonly float[] _rentedArray;
    private readonly Memory<float> _heightmap;
    private readonly int _patchStride;

    public TerrainActor Target { get; }
    public Int2 Size { get; }
    public TerrainPatches Patches { get; }
    public IEnumerable<GraphComponent> Models { get; }

    public TerrainGenerator(TerrainActor terrain, IEnumerable<GraphComponent> models)
    {
        Target = terrain ?? throw new ArgumentNullException(nameof(terrain));
        Models = models ?? throw new ArgumentNullException(nameof(models));

        _patches = [];

        Int2 patchCount = CountPatches(terrain);
        _patchStride = terrain.ChunkSize * TerrainActor.PatchEdgeChunksCount;
        Size = (patchCount * _patchStride) + Int2.One;
        Patches = new TerrainPatches(_patchStride + 1, patchCount);

        _heightmap = ArrayUtils.Rent(SharedPool, Size.X * Size.Y, out _rentedArray);
    }

    public static TerrainGenerator Create(Actor actor, IEnumerable<GraphComponent> components)
    {
        return new((TerrainActor)actor, components);
    }

    public async Task BuildAsync(CancellationToken cancellationToken)
    {
        Int2Enumerable coordEnumerator = new(Patches.Count - Int2.One);
        try
        {
            await Parallel.ForEachAsync(coordEnumerator, cancellationToken, GeneratePatchAsync);

            foreach (ITopographyPostProcessor postProcessor in Models.OfType<ITopographyPostProcessor>())
            {
                postProcessor.Apply(_heightmap, Size.X);
            }

            await Parallel.ForEachAsync(_patches, cancellationToken, SetupHeightmapAsync);
        }
        finally
        {
            foreach (Patch patch in _patches)
            {
                patch.Dispose();
            }
        }
    }

    private ValueTask SetupHeightmapAsync(Patch patch, CancellationToken cancellationToken)
    {
        SplitPatch(patch);

        if (patch.SetupHeightmap(Target))
        {
            return ValueTask.CompletedTask;
        }

        throw new InvalidOperationException($"Failed to setup patch heightmap ({patch.Index})");
    }

    private async ValueTask GeneratePatchAsync(Int2 index, CancellationToken cancellationToken)
    {
        Patch patch = new(index, Patches.Size);
        Span<float> heightmap = patch.Heightmap.Span;

        Int2 offset = index * (Patches.Size - 1);

        try
        {
            for (int i = 0; i < patch.Heightmap.Length; i++)
            {
                int x = i % Patches.Size;
                int z = i / Patches.Size;

                if (x == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                float u = (offset.X + x) / (float)Size.X;
                float v = (offset.Y + z) / (float)Size.Y;

                ref float height = ref heightmap[i];
                height = default;
                foreach (ITopographyProvider layer in Models.OfType<ITopographyProvider>())
                {
                    layer.GetHeight(u, v, ref height);
                }
            }

            StitchPatch(patch);

            _patches.Add(patch);
        }
        catch
        {
            patch.Dispose();
            throw;
        }
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

    private void StitchPatch(Patch patch)
    {
        int startX = patch.Index.X * _patchStride;
        int startY = patch.Index.Y * _patchStride;

        Span<float> patchSpan = patch.Heightmap.Span;
        Span<float> heightmapSpan = _heightmap.Span;

        for (int z = 0; z < Patches.Size; z++)
        {
            Span<float> sourceRow = patchSpan.Slice(z * Patches.Size, Patches.Size);

            int destIndex = ((startY + z) * Size.X) + startX;

            Span<float> destinationRow = heightmapSpan.Slice(destIndex, Patches.Size);
            sourceRow.CopyTo(destinationRow);
        }
    }

    private void SplitPatch(Patch patch)
    {
        int startX = patch.Index.X * _patchStride;
        int startY = patch.Index.Y * _patchStride;

        Span<float> patchSpan = patch.Heightmap.Span;
        Span<float> heightmapSpan = _heightmap.Span;

        for (int z = 0; z < Patches.Size; z++)
        {
            int srcIndex = ((startY + z) * Size.X) + startX;
            Span<float> sourceRow = heightmapSpan.Slice(srcIndex, Patches.Size);
            Span<float> destinationRow = patchSpan.Slice(z * Patches.Size, Patches.Size);
            sourceRow.CopyTo(destinationRow);
        }
    }

    public void Dispose()
    {
        if (_rentedArray is { })
        {
            SharedPool.Return(_rentedArray);
        }
    }
}
