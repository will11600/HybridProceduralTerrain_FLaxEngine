using ProceduralGraph;
using System;
using System.ComponentModel;

namespace AdvancedTerrainToolsEditor.Topography.PostProcessors;

/// <summary>
/// ThermalErosion class.
/// </summary>
[DisplayName("Thermal Erosion")]
public sealed class ThermalErosion : GraphComponent, ITopographyPostProcessor
{
    private float _strength = 0.3f;
    /// <summary>
    /// Gets or sets the strength of the thermal erosion (talus movement).
    /// </summary>
    public float Strength
    {
        get => _strength;
        set => RaiseAndSetIfChanged(ref _strength, in value);
    }

    private float _angleThreshold = 0.5f;
    /// <summary>
    /// Gets or sets the slope threshold for thermal erosion.
    /// </summary>
    public float AngleThreshold
    {
        get => _angleThreshold;
        set => RaiseAndSetIfChanged(ref _angleThreshold, in value);
    }

    public void Apply(Memory<float> heightmap, int size)
    {
        for (int iter = 0; iter < 2; iter++)
        {
            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    int idx = y * size + x;
                    int[] neighbors =
                    {
                        idx + 1,
                        idx - 1,
                        idx + size,
                        idx - size
                    };

                    foreach (int n in neighbors)
                    {
                        float diff = heightmap.Span[idx] - heightmap.Span[n];
                        if (diff > AngleThreshold / 1000f)
                        {
                            float move = (diff - AngleThreshold / 1000f) * Strength;
                            heightmap.Span[idx] -= move;
                            heightmap.Span[n] += move;
                        }
                    }
                }
            }
        }
    }
}
