using FlaxEngine;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Terrain.Topography.Normalization;

[DisplayName("Linear Normalizer")]
public class LinearNormalizer : GraphComponent, ITopographyPostProcessor
{
    private float _min = -1000.0f;
    /// <summary>
    /// Gets or sets the minimum height value after normalization.
    /// </summary>
    public float Min
    {
        get => _min;
        set => RaiseAndSetIfChanged(ref _min, in value);
    }

    private float _max = 1000.0f;
    /// <summary>
    /// Gets or sets the maximum height value after normalization.
    /// </summary>
    public float Max
    {
        get => _max;
        set => RaiseAndSetIfChanged(ref _max, in value);
    }

    public unsafe void Apply(FlaxEngine.Terrain terrain, FatPointer2D<float> heightMap)
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
            height = Mathf.InverseLerp(min, max, height);
            Evaluate(i, heightMap.Width, ref height);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void Evaluate(int index, int width, ref float height)
    {
        height = Mathf.Lerp(Min, Max, height);
    }
}
