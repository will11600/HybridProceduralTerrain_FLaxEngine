using FlaxEngine;
using System;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Terrain.Topography;

internal unsafe sealed class HydraulicErosion(float* mapPtr)
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required int Seed { get; init; }

    public required float Inertia { get; init; }

    public required float Gravity { get; init; }

    public required float MinSlopeCapacity { get; init; }

    public required float ErosionSpeed { get; init; }

    public required float DepositionSpeed { get; init; }

    public required float EvaporationSpeed { get; init; }

    public required int MaxLifetime { get; init; }

    public required float InitialSpeed { get; init; }

    public required float InitialWater { get; init; }

    private readonly float* _mapPtr = mapPtr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ProcessDroplet(int i)
    {
        var rng = new XorShiftRandom((uint)(Seed + i * 599));

        // Start at random position
        float posX = rng.NextFloat() * (Width - 1);
        float posY = rng.NextFloat() * (Height - 1);

        float dirX = 0;
        float dirY = 0;
        float speed = InitialSpeed;
        float water = InitialWater;
        float sediment = 0;

        for (int step = 0; step < MaxLifetime; step++)
        {
            int nodeX = (int)posX;
            int nodeY = (int)posY;

            // Calculate offset inside the cell
            float u = posX - nodeX;
            float v = posY - nodeY;

            // Calculate GradientSampler and Height
            CalculateGradient(posX, posY, out float gradX, out float gradY, out float height);

            // Update Direction (Inertia)
            dirX = (dirX * Inertia - gradX * (1 - Inertia));
            dirY = (dirY * Inertia - gradY * (1 - Inertia));

            // NormalizeProcessor direction
            float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
            if (len != 0)
            {
                dirX /= len;
                dirY /= len;
            }

            posX += dirX;
            posY += dirY;

            // Stop if out of bounds
            if (posX < 0 || posX >= Width - 1 || posY < 0 || posY >= Height - 1)
                break;

            // Calculate new height difference
            CalculateGradient(posX, posY, out _, out _, out float newHeight);
            float deltaHeight = newHeight - height;

            // Calculate Sediment Capacity
            float sedimentCapacity = MathF.Max(-deltaHeight * speed * water * MinSlopeCapacity, MinSlopeCapacity);

            // Erosion or Deposition
            if (sediment > sedimentCapacity || deltaHeight > 0)
            {
                // Deposit
                float amountToDeposit = (sediment - sedimentCapacity) * DepositionSpeed;
                if (deltaHeight > 0)
                    amountToDeposit = MathF.Min(deltaHeight, sediment);

                sediment -= amountToDeposit;
                Deposit(nodeX, nodeY, u, v, amountToDeposit);
            }
            else
            {
                // Erode
                float amountToErode = MathF.Min((sedimentCapacity - sediment) * ErosionSpeed, -deltaHeight);
                Erode(nodeX, nodeY, u, v, amountToErode);
                sediment += amountToErode;
            }

            // Update Physics
            speed = MathF.Sqrt(speed * speed + MathF.Abs(deltaHeight) * Gravity);
            water *= (1 - EvaporationSpeed);

            if (water < 0.001f) break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CalculateGradient(float x, float y, out float gx, out float gy, out float h)
    {
        int x0 = (int)x;
        int y0 = (int)y;

        float u = x - x0;
        float v = y - y0;

        int idx = y0 * Width + x0;

        // Fetch heights of 4 neighbors
        float h00 = _mapPtr[idx];
        float h10 = _mapPtr[idx + 1];
        float h01 = _mapPtr[idx + Width];
        float h11 = _mapPtr[idx + Width + 1];

        // Bilinear interpolation for height
        h = (h00 * (1 - u) + h10 * u) * (1 - v) + (h01 * (1 - u) + h11 * u) * v;

        // GradientSampler
        gx = (h10 - h00) * (1 - v) + (h11 - h01) * v;
        gy = (h01 - h00) * (1 - u) + (h11 - h10) * u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Deposit(int x, int y, float u, float v, float amount)
    {
        int idx = y * Width + x;
        FloatUtils.AtomicAdd(ref _mapPtr[idx], amount * (1 - u) * (1 - v));
        FloatUtils.AtomicAdd(ref _mapPtr[idx + 1], amount * u * (1 - v));
        FloatUtils.AtomicAdd(ref _mapPtr[idx + Width], amount * (1 - u) * v);
        FloatUtils.AtomicAdd(ref _mapPtr[idx + Width + 1], amount * u * v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Erode(int x, int y, float u, float v, float amount)
    {
        int idx = y * Width + x;
        FloatUtils.AtomicAdd(ref _mapPtr[idx], -amount * (1 - u) * (1 - v));
        FloatUtils.AtomicAdd(ref _mapPtr[idx + 1], -amount * u * (1 - v));
        FloatUtils.AtomicAdd(ref _mapPtr[idx + Width], -amount * (1 - u) * v);
        FloatUtils.AtomicAdd(ref _mapPtr[idx + Width + 1], -amount * u * v);
    }
}