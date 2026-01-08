using FlaxEngine;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ProceduralGraph.Terrain.Topography.Processors;

/// <summary>
/// Applies a Gaussian blur to terrain heightmaps using separable convolution and multithreading.
/// </summary>
[DisplayName("Gaussian Blur")]
public sealed class GaussianBlurProcessor : GraphComponent, ITopographyPostProcessor
{
    private static ParallelOptions ParallelOptions { get; } = new ParallelOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    private int _blurRadius = 3;
    /// <summary>
    /// The radius of the blur kernel. Higher values cost more performance but create wider blurs.
    /// </summary>
    public int BlurRadius
    {
        get => _blurRadius;
        set => RaiseAndSetIfChanged(ref _blurRadius, ref value);
    }

    private float _sigma = 1.5f;
    /// <summary>
    /// The standard deviation of the Gaussian distribution. Controls the "smoothness" weight.
    /// </summary>
    public float Sigma
    {
        get => _sigma;
        set => RaiseAndSetIfChanged(ref _sigma, ref value);
    }

    public unsafe void Apply(FlaxEngine.Terrain terrain, FatPointer<float> heightMap, int width)
    {
        if (heightMap.Buffer == null || width <= 0)
        {
            return;
        }

        int height = heightMap.Length / width;
        int radius = Mathf.Clamp(_blurRadius, 1, 50);

        using GaussianBlur blur = new(heightMap, radius, _sigma)
        {
            Width = width,
            Height = height
        };

        Parallel.For(0, height, ParallelOptions, blur.ProcessRow);
        Parallel.For(0, width, ParallelOptions, blur.ProcessColumn);
    }
}
