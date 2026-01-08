using FlaxEngine;
using System;
using System.ComponentModel;

namespace ProceduralGraph.Terrain.Topography.Samplers;

/// <summary>
/// GradientSampler class.
/// </summary>
[DisplayName("Gradient")]
public sealed class GradientSampler : GraphComponent, ITopographySampler
{
    private Float2 _origin = new(0.5f, 0.5f);
    public Float2 Origin
    {
        get => _origin;
        set => RaiseAndSetIfChanged(ref _origin, in value);
    }

    private float _innerRadius;
    [Range(0.0f, 1.0f)]
    public float InnerRadius
    {
        get => _innerRadius;
        set => RaiseAndSetIfChanged(ref _innerRadius, in value);
    }

    private float _outerRadius = 1.0f;
    [Range(0.0f, 1.0f)]
    public float OuterRadius
    {
        get => _outerRadius;
        set => RaiseAndSetIfChanged(ref _outerRadius, in value);
    }

    private float _amplitude = 100.0f;
    [Limit(0.0f)]
    public float Amplitude
    {
        get => _amplitude;
        set => RaiseAndSetIfChanged(ref _amplitude, in value);
    }

    private float _shape = 0.5f;
    [Range(0.0f, 1.0f)]
    public float Shape
    {
        get => _shape;
        set => RaiseAndSetIfChanged(ref _shape, in value);
    }

    private BlendMode _mode = BlendMode.Add;
    public BlendMode Mode
    {
        get => _mode;
        set => RaiseAndSetIfChanged(ref _mode, in value);
    }

    public void GetHeight(float u, float v, ref float height)
    {
        u = Mathf.Abs(_origin.X - u);
        v = Mathf.Abs(_origin.Y - v);

        float circularGradient = CircularGradient(u, v);
        float boxGradient = BoxGradient(u, v);

        float change = Mathf.Lerp(circularGradient, boxGradient, _shape) * _amplitude;

        height = _mode switch
        {
            BlendMode.Add => height + change,
            BlendMode.Subtract => height - change,
            BlendMode.Multiply => height * change,
            _ => height
        };
    }

    private float CircularGradient(float u, float v)
    {
        float sqrDistance = u * u + v * v;
        float sqrInnerRadius = _innerRadius * _innerRadius;
        float sqrOuterRadius = _outerRadius * _outerRadius;
        float t = (sqrDistance - sqrInnerRadius) / Mathf.Max(sqrOuterRadius - sqrInnerRadius, float.Epsilon);
        return 1.0f - Mathf.Saturate(t);
    }

    private float BoxGradient(float u, float v)
    {
        float distance = Mathf.Max(u, v);
        float range = Mathf.Max(_outerRadius - _innerRadius, float.Epsilon);
        float t = (distance - _innerRadius) / range;
        return 1.0f - Mathf.Saturate(t);
    }
}
