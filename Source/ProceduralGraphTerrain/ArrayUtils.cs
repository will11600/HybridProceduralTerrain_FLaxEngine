#nullable enable
using System;
using System.Buffers;

namespace ProceduralGraph.Terrain;

internal static class ArrayUtils
{
    public static Memory<T> Rent<T>(ArrayPool<T> arrayPool, int size, out T[] rentedArray)
    {
        rentedArray = arrayPool.Rent(size);
        try
        {
            return new Memory<T>(rentedArray, 0, size);
        }
        catch
        {
            arrayPool.Return(rentedArray);
            throw;
        }
    }
}