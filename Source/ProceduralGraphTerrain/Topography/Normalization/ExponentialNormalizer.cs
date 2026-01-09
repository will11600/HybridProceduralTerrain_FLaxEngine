using FlaxEngine;
using System.ComponentModel;

namespace ProceduralGraph.Terrain.Topography.Normalization;

[DisplayName("Exponential Normalizer")]
public sealed class ExponentialNormalizer : LinearNormalizer
{
    private float _exponent = 2.0f;
    /// <summary>
    /// Gets or sets the exponent value used in calculations.
    /// </summary>
    public float Exponent
    {
        get => _exponent;
        set => RaiseAndSetIfChanged(ref _exponent, in value);
    }

    protected override void Evaluate(int index, int width, ref float height)
    {
        height = Mathf.Pow(height, Exponent);
        base.Evaluate(index, width, ref height);
    }
}
