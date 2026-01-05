using FlaxEngine;
using System;
using System.ComponentModel;

namespace ProceduralGraph.Terrain.Topography;

/// <summary>
/// HydraulicErosion class.
/// </summary>
[DisplayName("Hydraulic Erosion")]
public sealed class HydraulicErosion : GraphComponent, ITopographyPostProcessor
{
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

    public void Apply(Memory<float> heightmap, int width)
    {
        Span<float> map = heightmap.Span;

        for (int i = 0; i < _droplets; i++)
        {
            float px = RandomUtil.Rand() * (width - 1.1f);
            float py = RandomUtil.Rand() * (width - 1.1f);

            float dirX = 0, dirY = 0;
            float sediment = 0;
            float water = 1;
            float velocity = 1;

            for (int step = 0; step < 35; step++)
            {
                int ix = (int)px;
                int iy = (int)py;
                int idx = iy * width + ix;

                float gx = map[idx + 1] - map[idx];
                float gy = map[idx + width] - map[idx];

                dirX = dirX * 0.1f - gx;
                dirY = dirY * 0.1f - gy;

                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len > 0)
                {
                    dirX /= len;
                    dirY /= len;
                }

                px += dirX;
                py += dirY;

                if (px < 0 || px >= width - 1 || py < 0 || py >= width - 1)
                    break;

                float newHeight = map[(int)py * width + (int)px];
                float delta = map[idx] - newHeight;

                float capacity = Mathf.Max(delta, 0.01f) * velocity * water * 5f;

                if (sediment > capacity)
                {
                    float deposit = (sediment - capacity) * _depositionRate;
                    sediment -= deposit;
                    map[idx] += deposit;
                }
                else
                {
                    float erode = Mathf.Min((capacity - sediment) * _erosionRate, delta);
                    sediment += erode;
                    map[idx] -= erode;
                }

                velocity = Mathf.Sqrt(Mathf.Max(0, velocity * velocity + delta * _gravity));
                water *= 0.98f;
            }
        }
    }
}
