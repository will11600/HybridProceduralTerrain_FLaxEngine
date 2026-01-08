using System.Runtime.CompilerServices;
using System.Threading;

namespace ProceduralGraph.Terrain.Topography;

internal static class FloatUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AtomicAdd(ref float location, float value)
    {
        float initialValue, newValue;
        do
        {
            initialValue = location;
            newValue = initialValue + value;
        }
        while (initialValue != Interlocked.CompareExchange(ref location, newValue, initialValue));
    }
}