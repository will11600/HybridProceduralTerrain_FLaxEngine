using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ProceduralGraph.Terrain.Topography.Processors;

/// <summary>
/// Simulates thermal erosion (talus deposition), where material falls down slopes 
/// that exceed a specific angle of repose (Talus Angle).
/// </summary>
[DisplayName("Thermal Erosion")]
public sealed class ThermalErosionProcessor : GraphComponent, ITopographyPostProcessor
{
    private static ParallelOptions ParallelOptions { get; } = new ParallelOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount 
    };

    private int _iterations = 250000;
    public int Iterations
    {
        get => _iterations;
        set => RaiseAndSetIfChanged(ref _iterations, in value);
    }

    private int _seed = 12345;
    public int Seed
    {
        get => _seed;
        set => RaiseAndSetIfChanged(ref _seed, in value);
    }

    private float _talusAngle = 45.0f;
    /// <summary>
    /// The critical angle (in degrees) at which the terrain becomes unstable.
    /// </summary>
    public float TalusAngle
    {
        get => _talusAngle;
        set => RaiseAndSetIfChanged(ref _talusAngle, in value);
    }

    private float _strength = 0.5f;
    /// <summary>
    /// How much material is moved per iteration (0.0 - 1.0).
    /// </summary>
    public float Strength
    {
        get => _strength;
        set => RaiseAndSetIfChanged(ref _strength, in value);
    }

    public unsafe void Apply(FlaxEngine.Terrain terrain, FatPointer<float> heightMap, int width)
    {
        if (heightMap.Buffer == null || width <= 0)
        {
            return;
        }

        int height = heightMap.Length / width;

        ThermalErosion erosion = new(heightMap.Buffer, _talusAngle)
        {
            Width = width,
            Height = height,
            Seed = _seed,
            Strength = _strength
        };

        Parallel.For(0, _iterations, ParallelOptions, erosion.ProcessIteration);
    }
}
