using System.Runtime.CompilerServices;

namespace ProceduralGraph.Terrain.Topography;

internal struct XorShiftRandom(uint seed)
{
    private uint _state = seed == 0 ? 1337 : seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat()
    {
        uint x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;
        return (float)x / uint.MaxValue;
    }
}
