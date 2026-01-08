using FlaxEngine;
using System.ComponentModel;

namespace ProceduralGraph.Terrain.Splatting;

[DisplayName("Altitude Painter")]
public sealed class AltitudeLayerWeightSampler : GraphComponent, ISplatMapLayerWeightSampler
{
    private int _layerIndex = 2;
    /// <inheritdoc/>
    public int LayerIndex
    {
        get => _layerIndex;
        set => RaiseAndSetIfChanged(ref _layerIndex, in value);
    }

    private float _minAltitude = 50.0f;
    public float MinAltitude
    {
        get => _minAltitude;
        set => RaiseAndSetIfChanged(ref _minAltitude, in value);
    }

    private float _maxAltitude = 250.0f;
    public float MaxAltitude
    {
        get => _maxAltitude;
        set => RaiseAndSetIfChanged(ref _maxAltitude, in value);
    }

    public byte ComputeWeight(FlaxEngine.Terrain terrain, ref readonly float height, ref readonly float normal)
    {
        float altitudeFactor = Mathf.SmoothStep(_minAltitude, _maxAltitude, height);
        return (byte)(byte.MaxValue * altitudeFactor);
    }
}
