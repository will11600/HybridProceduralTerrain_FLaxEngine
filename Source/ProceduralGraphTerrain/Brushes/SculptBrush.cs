using FlaxEngine;
using ProceduralGraph;
using System;

namespace AdvancedTerrainToolsEditor.Brushes;

#if USE_LARGE_WORLDS
using Real = System.Double;
#else
using Real = System.Single;
#endif

/// <summary>
/// SculptBrush class.
/// </summary>
public abstract class SculptBrush : GraphModel, ITerrainModifier, ISculptBrush
{
    private Transform _transform;
    public Transform Transform
    {
        get => _transform;
        set => RaiseAndSetIfChanged(ref _transform, in value);
    }

    private SculptMode _mode = SculptMode.Additive;
    [Header("Brush Settings")]
    public SculptMode Mode
    {
        get => _mode;
        set => RaiseAndSetIfChanged(ref _mode, in value);
    }

    private float _intensity = 1.0f;

    [Range(0.0f, 2.0f)]
    public float Intensity
    {
        get => _intensity;
        set => RaiseAndSetIfChanged(ref _intensity, in value);
    }

    private float _radius = 1500.0f;
    public Real Radius
    {
        get => _radius;
        set => RaiseAndSetIfChanged(ref _radius, in value);
    }

    private float _falloff = 700.0f;
    public Real Falloff
    {
        get => _falloff;
        set => RaiseAndSetIfChanged(ref _falloff, in value);
    }

    private float _edgeNoise = 0.0f;
    [Header("Artistic Variation"), Range(0.0f, 500.0f)]
    public float EdgeNoise
    {
        get => _edgeNoise;
        set => RaiseAndSetIfChanged(ref _edgeNoise, in value);
    }

    public abstract Real GetDistance(Vector3 localPos);

    public abstract void Update();
}
