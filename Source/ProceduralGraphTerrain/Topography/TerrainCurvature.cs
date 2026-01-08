using System;
using System.Buffers;
using System.ComponentModel;
using FlaxEngine;

namespace ProceduralGraph.Terrain.Topography;

/// <summary>
/// TerrainCurvature class.
/// </summary>
[DisplayName("Terrain Curvature")]
public sealed class TerrainCurvature : GraphComponent, ITopographyPostProcessor
{
    private float _ridgeGain = 0.35f;
    /// <summary>
    /// Gets or sets the gain applied to ridge details.
    /// </summary>
    [Range(0.0f, 1.0f)]
    public float RidgeGain
    {
        get => _ridgeGain;
        set => RaiseAndSetIfChanged(ref _ridgeGain, in value);
    }

    private float _valleyRelax = 0.2f;
    /// <summary>
    /// Gets or sets the relaxation factor for valley areas.
    /// </summary>
    [Range(0.0f, 1.0f)]
    public float ValleyRelax
    {
        get => _valleyRelax;
        set => RaiseAndSetIfChanged(ref _valleyRelax, in value);
    }

    private int _iterations = 1;
    /// <summary>
    /// Gets or sets the number of iterations for calculating curvature detail.
    /// </summary>
    [Range(1, 3)]
    public int Iterations
    {
        get => _iterations;
        set => RaiseAndSetIfChanged(ref _iterations, in value);
    }

    public void Apply(Span<float> heightmap, int size)
    {
        if (heightmap.Length != size * size || Iterations <= 0)
            return;

        RidgeGain = Mathf.Max(0f, RidgeGain);
        ValleyRelax = Mathf.Max(0f, ValleyRelax);

        ArrayPool<float> sharedPool = ArrayPool<float>.Shared;
        Memory<float> buffer = ArrayUtils.Rent(sharedPool, heightmap.Length, out float[] rentedArray);

        try
        {
            Span<float> src = heightmap;
            Span<float> dst = buffer.Span;

            for (int iter = 0; iter < Iterations; iter++)
            {
                for (int y = 0; y < size; y++)
                {
                    int yOffset = y * size;
                    for (int x = 0; x < size; x++)
                    {
                        int idx = yOffset + x;

                        if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                        {
                            dst[idx] = src[idx];
                            continue;
                        }

                        float h = src[idx];
                        float avg = (src[idx - 1] + src[idx + 1] + src[idx - size] + src[idx + size]) * 0.25f;
                        float diff = h - avg;

                        float delta = diff > 0f ? diff * RidgeGain : diff * ValleyRelax;

                        delta = Mathf.Clamp(delta, -0.05f, 0.05f);

                        dst[idx] = h + delta;
                    }
                }

                Swap(ref src, ref dst);
            }

            src.CopyTo(heightmap);
        }
        finally
        {
            sharedPool.Return(rentedArray);
        }
    }

    private static void Swap(ref Span<float> src, ref Span<float> dst)
    {
        Span<float> temp = src;

        src = dst;
        dst = temp;
    }
}
