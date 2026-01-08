using FlaxEngine;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Terrain.Topography;

internal unsafe sealed class ThermalErosion(float* mapPtr, float talusAngle)
{
    private readonly float* _mapPtr = mapPtr;

    public int Width { get; init; }

    public int Height { get; init; }

    public int Seed { get; init; }

    public float TalusThreshold { get; } = Mathf.Tan(talusAngle * Mathf.DegreesToRadians);

    public float Strength { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ProcessIteration(int i)
    {
        var rng = new XorShiftRandom((uint)(Seed + i * 823));

        // Pick a random location
        // Restrict RNG to [1, Width-2] to avoid boundary checks
        int x = 1 + (int)(rng.NextFloat() * (Width - 2));
        int y = 1 + (int)(rng.NextFloat() * (Height - 2));
        int idx = y * Width + x;

        float h = _mapPtr[idx];
        float maxDiff = 0;
        int maxIdx = -1;

        // Check Von Neumann neighborhood (Right, Left, Down, Up)

        // Right
        int nIdx = idx + 1;
        float diff = h - _mapPtr[nIdx];
        if (diff > maxDiff) { maxDiff = diff; maxIdx = nIdx; }

        // Left
        nIdx = idx - 1;
        diff = h - _mapPtr[nIdx];
        if (diff > maxDiff) { maxDiff = diff; maxIdx = nIdx; }

        // Down
        nIdx = idx + Width;
        diff = h - _mapPtr[nIdx];
        if (diff > maxDiff) { maxDiff = diff; maxIdx = nIdx; }

        // Up
        nIdx = idx - Width;
        diff = h - _mapPtr[nIdx];
        if (diff > maxDiff) { maxDiff = diff; maxIdx = nIdx; }

        // If slope exceeds the angle of repose, move material
        if (maxDiff > TalusThreshold)
        {
            float amountToMove = (maxDiff - TalusThreshold) * Strength;

            // Cap movement to 50% of the difference to prevent oscillation
            if (amountToMove > maxDiff * 0.5f)
                amountToMove = maxDiff * 0.5f;

            FloatUtils.AtomicAdd(ref _mapPtr[idx], -amountToMove);
            FloatUtils.AtomicAdd(ref _mapPtr[maxIdx], amountToMove);
        }
    }
}
