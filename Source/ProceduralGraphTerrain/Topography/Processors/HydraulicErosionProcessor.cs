using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ProceduralGraph.Terrain.Topography.Processors;

/// <summary>
/// A high-performance hydraulic erosion simulation using droplet-based particle physics.
/// </summary>
[DisplayName("Hydraulic Erosion")]
public sealed class HydraulicErosionProcessor : GraphComponent, ITopographyPostProcessor
{
    private static ParallelOptions ParallelOptions { get; } = new ParallelOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    private int _iterations = 50000;
    public int Iterations
    {
        get => _iterations;
        set => RaiseAndSetIfChanged(ref _iterations, in value);
    }

    private int _seed = 1337;
    public int Seed
    {
        get => _seed;
        set => RaiseAndSetIfChanged(ref _seed, in value);
    }

    private float _erosionSpeed = 0.3f;
    public float ErosionSpeed
    {
        get => _erosionSpeed;
        set => RaiseAndSetIfChanged(ref _erosionSpeed, in value);
    }

    private float _depositionSpeed = 0.3f;
    public float DepositionSpeed
    {
        get => _depositionSpeed;
        set => RaiseAndSetIfChanged(ref _depositionSpeed, in value);
    }

    private float _inertia = 0.05f;
    public float Inertia
    {
        get => _inertia;
        set => RaiseAndSetIfChanged(ref _inertia, in value);
    }

    private float _gravity = 4.0f;
    public float Gravity
    {
        get => _gravity;
        set => RaiseAndSetIfChanged(ref _gravity, in value);
    }

    private float _evaporationSpeed = 0.01f;
    public float EvaporationSpeed
    {
        get => _evaporationSpeed;
        set => RaiseAndSetIfChanged(ref _evaporationSpeed, in value);
    }

    private float _initialWater = 1.0f;
    public float InitialWater
    {
        get => _initialWater;
        set => RaiseAndSetIfChanged(ref _initialWater, in value);
    }

    private float _initialSpeed = 1.0f;
    public float InitialSpeed
    {
        get => _initialSpeed;
        set => RaiseAndSetIfChanged(ref _initialSpeed, in value);
    }

    private float _minSlopeCapacity = 0.01f;
    public float MinSlopeCapacity
    {
        get => _minSlopeCapacity;
        set => RaiseAndSetIfChanged(ref _minSlopeCapacity, in value);
    }

    private int _maxLifetime = 30;
    public int MaxLifetime
    {
        get => _maxLifetime;
        set => RaiseAndSetIfChanged(ref _maxLifetime, in value);
    }

    public unsafe void Apply(FlaxEngine.Terrain terrain, Span<float> heightMap, int width)
    {
        
    }

    public unsafe void Apply(FlaxEngine.Terrain terrain, float* heightMapPtr, int heightMapLength, int width)
    {
        if (heightMapPtr == null || width <= 0)
        {
            return;
        }

        int height = heightMapLength / width;

        HydraulicErosion erosion = new(heightMapPtr)
        {
            Width = width,
            Height = height,
            Seed = _seed,
            Inertia = _inertia,
            Gravity = _gravity,
            MinSlopeCapacity = _minSlopeCapacity,
            ErosionSpeed = _erosionSpeed,
            DepositionSpeed = _depositionSpeed,
            EvaporationSpeed = _evaporationSpeed,
            MaxLifetime = _maxLifetime,
            InitialSpeed = _initialSpeed,
            InitialWater = _initialWater
        };

        Parallel.For(0, _iterations, ParallelOptions, erosion.ProcessDroplet);
    }
}
