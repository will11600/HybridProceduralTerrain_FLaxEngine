#nullable enable
using System;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Terrain;

public unsafe class FatPointer<T>(int elementCount) : IDisposable where T : unmanaged
{
    private bool _disposed;

    public int Length { get; } = elementCount;

    public T* Buffer { get; private set; } = (T*)NativeMemory.AllocZeroed((nuint)elementCount, (nuint)sizeof(T));

    public bool IsEmpty => Buffer == null || Length == 0;

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (Buffer != null)
        {
            NativeMemory.Free(Buffer);
            Buffer = null;
        }

        _disposed = true;
    }

    ~FatPointer()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static explicit operator Span<T>(FatPointer<T> fatPointer) => new(fatPointer.Buffer, fatPointer.Length);

    public static explicit operator ReadOnlySpan<T>(FatPointer<T> fatPointer) => new(fatPointer.Buffer, fatPointer.Length);
}
