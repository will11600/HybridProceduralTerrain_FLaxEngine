#nullable enable
using FlaxEngine;
using System;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Terrain;

internal sealed class Patch(Int2 index, int length) : IDisposable
{
    private bool _disposed;

    public unsafe float* HeightMapPtr { get; } = (float*)NativeMemory.Alloc((nuint)(length * sizeof(float)));

    public int HeightMapLength { get; } = length;

    public Int2 index = index;

    private unsafe void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Dispose of managed resources if any.
        }

        NativeMemory.Free(HeightMapPtr);

        _disposed = true;
    }

    ~Patch()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
