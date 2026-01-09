using FlaxEngine;
using System.ComponentModel;

namespace ProceduralGraph.Terrain.Topography.Normalization;

[DisplayName("Custom Normalizer")]
public sealed class CustomNormalizer : LinearNormalizer
{
    private static BezierCurve<float>.Keyframe[] DefaultKeyframes { get; } = [new(0.0f, 0.0f, 0.0f, 0.0f), new(0.5f, 0.5f, 0.5f, 0.5f), new(1.0f, 1.0f, 1.0f, 1.0f)];

    private BezierCurve<float> _curve = new(DefaultKeyframes);
    /// <summary>
    /// Gets or sets the Bézier curve used for normalization.
    /// </summary>
    public BezierCurve<float> Curve
    {
        get => _curve;
        set => RaiseAndSetIfChanged(ref _curve, in value);
    }

    protected override void Evaluate(int index, int width, ref float height)
    {
        _curve.Evaluate(out height, height, loop: false);
        base.Evaluate(index, width, ref height);
    }
}
