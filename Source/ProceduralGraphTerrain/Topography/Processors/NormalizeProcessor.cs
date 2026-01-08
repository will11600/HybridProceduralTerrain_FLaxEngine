using FlaxEngine;
using System;
using System.ComponentModel;

namespace ProceduralGraph.Terrain.Topography.Processors;

[DisplayName("Normalize")]
public sealed class NormalizeProcessor : GraphComponent, ITopographyPostProcessor
{
    private float _min = -1.0f;
    public float Min
    {
        get => _min;
        set => RaiseAndSetIfChanged(ref _min, in value);
    }

    private float _max = 1.0f;
    public float Max
    {
        get => _max;
        set => RaiseAndSetIfChanged(ref _max, in value);
    }

    private float _exponent = 1.0f;
    [Limit(1.0f)]
    public float Exponent
    {
        get => _exponent;
        set => RaiseAndSetIfChanged(ref _exponent, in value);
    }

    public unsafe void Apply(FlaxEngine.Terrain terrain, FatPointer<float> heightMap, int width)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < heightMap.Length; i++)
        {
            ref readonly float height = ref heightMap.Buffer[i];
            min = height < min ? height : min;
            max = height > max ? height : max;
        }

        for (int i = 0; i < heightMap.Length; i++)
        {
            ref float height = ref heightMap.Buffer[i];
            float normalizedHeight = Mathf.InverseLerp(min, max, height);
            float heightPowN = Mathf.Pow(normalizedHeight, Exponent);
            height = Mathf.Lerp(Min, Max, heightPowN);
        }
    }
}

