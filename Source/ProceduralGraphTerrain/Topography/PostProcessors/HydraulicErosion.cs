using FlaxEngine;
using ProceduralGraph;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AdvancedTerrainToolsEditor.Topography.PostProcessors;

/// <summary>
/// HydraulicErosion class.
/// </summary>
[DisplayName("Hydraulic Erosion")]
public sealed class HydraulicErosion : GraphComponent, ITopographyPostProcessor
{
    private const float MinSlope = 0.01f;
    private const float SedimentCapacityFactor = 4.0f;
    private const float EvaporationRate = 0.5f;

    private int _droplets = 60000;
    /// <summary>
    /// Gets or sets the number of simulated droplets used for hydraulic erosion.
    /// </summary>
    public int Droplets
    {
        get => _droplets;
        set => RaiseAndSetIfChanged(ref _droplets, in value);
    }

    private float _erosionRate = 0.12f;
    /// <summary>
    /// Gets or sets the rate at which soil is eroded by droplets.
    /// </summary>
    public float ErosionRate
    {
        get => _erosionRate;
        set => RaiseAndSetIfChanged(ref _erosionRate, in value);
    }

    private float _depositionRate = 0.05f;
    /// <summary>
    /// Gets or sets the rate at which sediment is deposited.
    /// </summary>
    public float DepositionRate
    {
        get => _depositionRate;
        set => RaiseAndSetIfChanged(ref _depositionRate, in value);
    }

    private float _gravity = 1.0f;
    /// <summary>
    /// Gets or sets the gravity constant affecting erosion simulation.
    /// </summary>
    public float Gravity
    {
        get => _gravity;
        set => RaiseAndSetIfChanged(ref _gravity, in value);
    }

    private int _maxDropletLifetime = 30;
    /// <summary>
    /// Max steps a droplet survives.
    /// </summary>
    public int MaxDropletLifetime
    {
        get => _maxDropletLifetime;
        set => RaiseAndSetIfChanged(ref _maxDropletLifetime, in value);
    }

    private float _inertia;
    /// <summary>
    /// 0 = water follows slope instantly, 1 = water keeps moving in previous direction.
    /// </summary>
    [Range(0.0f, 1.0f)]
    public float Inertia
    {
        get => _inertia;
        set => RaiseAndSetIfChanged(ref _inertia, in value);
    }

    public void Apply(Memory<float> heightmap, int width)
    {
        // Get dimensions once
        int length = heightmap.Length;
        int height = length / width;
        int mapW = width - 1;
        int mapH = height - 1;

        // We use Parallel.For to split the droplet workload across CPU cores.
        // We do NOT use Memory<T>.Span here because Spans cannot be captured in lambdas.
        // Instead, we assume Memory is backed by an array for fast access, or we access via Memory.Span inside the loop.
        // For maximum performance in Flax/Unity, usually getting the raw array or pointer is best, 
        // but Memory<T> is safe and reasonably fast.

        Parallel.For(0, _droplets,
            // 1. Local Init: Create a Random generator for this specific thread
            () => new Random(),

            // 2. Body: Run the simulation for a batch of droplets
            (i, loopState, rng) =>
            {
                // Access span per-thread (Check if your engine allows concurrent Span access; standard C# Memory<T> does)
                var map = heightmap.Span;

                // --- Simulation Start ---
                Float2 pos = new Float2(
                    (float)rng.NextDouble() * (mapW - 1),
                    (float)rng.NextDouble() * (mapH - 1)
                );

                Float2 dir = Float2.Zero;
                float speed = 1.0f;
                float water = 1.0f;
                float sediment = 0.0f;

                for (int step = 0; step < _maxDropletLifetime; step++)
                {
                    int nodeX = (int)pos.X;
                    int nodeY = (int)pos.Y;
                    float cellOffsetX = pos.X - nodeX;
                    float cellOffsetY = pos.Y - nodeY;

                    // Calculate Gradient and Height
                    int index = nodeY * width + nodeX;

                    // Boundary safety check for index access
                    if (index < 0 || index + width + 1 >= length) break;

                    float h00 = map[index];
                    float h10 = map[index + 1];
                    float h01 = map[index + width];
                    float h11 = map[index + width + 1];

                    float gradientX = (h10 - h00) * (1 - cellOffsetY) + (h11 - h01) * cellOffsetY;
                    float gradientY = (h01 - h00) * (1 - cellOffsetX) + (h11 - h10) * cellOffsetX;

                    float heightAtPos = (h00 * (1 - cellOffsetX) * (1 - cellOffsetY)) +
                                        (h10 * cellOffsetX * (1 - cellOffsetY)) +
                                        (h01 * (1 - cellOffsetX) * cellOffsetY) +
                                        (h11 * cellOffsetX * cellOffsetY);

                    // Update Direction
                    Float2 gradient = new Float2(gradientX, gradientY);
                    dir = (dir * _inertia) - (gradient * (1 - _inertia));

                    if (Math.Abs(dir.X) < 0.0001f && Math.Abs(dir.Y) < 0.0001f)
                    {
                        dir = new Float2((float)rng.NextDouble(), (float)rng.NextDouble());
                    }
                    dir.Normalize();

                    // Move
                    pos += dir;

                    if (pos.X < 0 || pos.X >= mapW - 1 || pos.Y < 0 || pos.Y >= mapH - 1)
                        break;

                    // New Height Calculation
                    int newNodeX = (int)pos.X;
                    int newNodeY = (int)pos.Y;
                    float newCellOffsetX = pos.X - newNodeX;
                    float newCellOffsetY = pos.Y - newNodeY;

                    int newIndex = newNodeY * width + newNodeX;
                    // Boundary safety
                    if (newIndex < 0 || newIndex + width + 1 >= length) break;

                    float nh00 = map[newIndex];
                    float nh10 = map[newIndex + 1];
                    float nh01 = map[newIndex + width];
                    float nh11 = map[newIndex + width + 1];

                    float newHeight = (nh00 * (1 - newCellOffsetX) * (1 - newCellOffsetY)) +
                                      (nh10 * newCellOffsetX * (1 - newCellOffsetY)) +
                                      (nh01 * (1 - newCellOffsetX) * newCellOffsetY) +
                                      (nh11 * newCellOffsetX * newCellOffsetY);

                    float diff = newHeight - heightAtPos;
                    float sedimentCapacity = Math.Max(-diff, MinSlope) * speed * water * SedimentCapacityFactor;

                    // Erosion / Deposition
                    if (sediment > sedimentCapacity || diff > 0)
                    {
                        float amountToDeposit = (diff > 0) ? Math.Min(diff, sediment) : (sediment - sedimentCapacity) * _depositionRate;
                        sediment -= amountToDeposit;

                        // Dirty Write (Simultaneous access acceptable for erosion noise)
                        map[index] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                        map[index + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                        map[index + width] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                        map[index + width + 1] += amountToDeposit * cellOffsetX * cellOffsetY;
                    }
                    else
                    {
                        float amountToErode = Math.Min((sedimentCapacity - sediment) * _erosionRate, -diff);
                        sediment += amountToErode;

                        map[index] -= amountToErode * (1 - cellOffsetX) * (1 - cellOffsetY);
                        map[index + 1] -= amountToErode * cellOffsetX * (1 - cellOffsetY);
                        map[index + width] -= amountToErode * (1 - cellOffsetX) * cellOffsetY;
                        map[index + width + 1] -= amountToErode * cellOffsetX * cellOffsetY;
                    }

                    speed = (float)Math.Sqrt(Math.Max(0, speed * speed + diff * _gravity));
                    water *= (1 - EvaporationRate);

                    if (water < 0.01f) break;
                }

                // Return the thread-local RNG to be passed to the next iteration
                return rng;
            },
            // 3. Finalizer (not needed for RNG, so empty)
            (rng) => { }
        );
    }
}
