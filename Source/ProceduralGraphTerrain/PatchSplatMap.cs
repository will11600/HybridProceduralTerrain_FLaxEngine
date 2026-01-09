using FlaxEngine;
using System;

namespace ProceduralGraph.Terrain;

internal sealed class PatchSplatMap(Int2 patchCoord, int mapIndex, int size) : IPatchMap
{
    private bool _disposed;

    private Int2 _coordinate = patchCoord;
    public Int2 Coordinate => _coordinate;

    public int Index { get; } = mapIndex;

    private readonly FatPointer<Color32> _splatMap = new(size * size);
    public unsafe Color32* SplatMap => _splatMap.Buffer;

    public int Length => _splatMap.Length;

    public unsafe void Setup(FlaxEngine.Terrain terrain)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (terrain.SetupPatchSplatMap(ref _coordinate, Index, _splatMap.Length, _splatMap.Buffer))
        {
            throw new InvalidOperationException($"Failed to setup patch splatmap ({Coordinate}, Index: {Index})");
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
            _splatMap.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
