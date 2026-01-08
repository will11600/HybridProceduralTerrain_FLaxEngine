using FlaxEngine;
using System;

namespace ProceduralGraph.Terrain;

internal sealed class PatchHeightMap(Int2 patchCoord, int size) : IPatchMap
{
    private bool _disposed;

    private readonly FatPointer<float> _heightMap = new(size * size);
    public unsafe float* HeightMap => _heightMap.Buffer;

    private Int2 _coordinate = patchCoord;
    public Int2 Coordinate => _coordinate;

    public int Length => _heightMap.Length;

    public unsafe void Setup(FlaxEngine.Terrain terrain)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (terrain.SetupPatchHeightMap(ref _coordinate, _heightMap.Length, _heightMap.Buffer))
        {
            throw new InvalidOperationException($"Failed to setup patch heightmap ({Coordinate})");
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
