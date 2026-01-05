#nullable enable
using FlaxEngine;
using System;
using System.Buffers;

namespace ProceduralGraph.Terrain;

using TerrainActor = FlaxEngine.Terrain;

internal readonly struct Patch : IDisposable
{
    private static ArrayPool<float> SharedPool { get; } = ArrayPool<float>.Shared;

    public Int2 Index { get; }

    private readonly float[] _heightmap;
    public Memory<float> Heightmap { get; }

    public Patch(Int2 index, int size)
    {
        Index = index;
        int resolution = size * size;
        Heightmap = ArrayUtils.Rent(SharedPool, resolution, out _heightmap);
    }

    public readonly unsafe bool SetupHeightmap(TerrainActor terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain, nameof(terrain));
        Int2 coordinate = Index;
        fixed (float* ptr = &_heightmap[0])
        {
            return !terrain.SetupPatchHeightMap(ref coordinate, Heightmap.Length, ptr);
        }
    }

    public void Dispose()
    {
        SharedPool.Return(_heightmap);
    }
}
