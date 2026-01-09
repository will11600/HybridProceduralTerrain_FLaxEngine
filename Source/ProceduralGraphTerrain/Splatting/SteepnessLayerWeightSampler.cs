using FlaxEngine;
using System.ComponentModel;

namespace ProceduralGraph.Terrain.Splatting;

/// <summary>
/// Calculates splat map weights based on terrain steepness (angle in degrees).
/// </summary>
[DisplayName("Steepness Painter")]
public sealed class SteepnessLayerWeightSampler : GraphComponent, ISplatMapLayerWeightSampler
{
    private int _layerIndex = 1;
    /// <inheritdoc/>
    public int LayerIndex
    {
        get => _layerIndex;
        set => RaiseAndSetIfChanged(ref _layerIndex, in value);
    }

    private float _minSlopeAngle = 45.0f;
    /// <summary>
    /// The slope angle (in degrees) where the texture starts to appear.
    /// </summary>
    [Limit(0.0f, 90.0f)]
    public float MinSlopeAngle
    {
        get => _minSlopeAngle;
        set => RaiseAndSetIfChanged(ref _minSlopeAngle, in value);
    }

    private float _maxSlopeAngle = 60.0f;
    /// <summary>
    /// The slope angle (in degrees) where the texture is fully opaque.
    /// </summary>
    [Limit(0.0f, 90.0f)]
    public float MaxSlopeAngle
    {
        get => _maxSlopeAngle;
        set => RaiseAndSetIfChanged(ref _maxSlopeAngle, in value);
    }

    public byte ComputeWeight(FlaxEngine.Terrain terrain, ref readonly float height, ref readonly float inclination)
    {
        float slopeFactor = (inclination - MinSlopeAngle) / (MaxSlopeAngle - MinSlopeAngle);
        return (byte)(byte.MaxValue * slopeFactor);
    }
}