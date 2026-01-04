using FlaxEngine;
using FlaxEngine.Utilities;
using ProceduralGraph;
using System.ComponentModel;

namespace AdvancedTerrainToolsEditor.Topography.Providers;

[DisplayName("Perlin Noise Layer")]
public sealed class PerlinNoiseSampler : GraphModel, ITopographyProvider
{
    private const float MaxOffset = 1_000.0f;

    private float _frequency;
    /// <summary>
    /// The frequency of the noise. Higher values create smaller details.
    /// </summary>
    public float Frequency
    {
        get => _frequency;
        set => RaiseAndSetIfChanged(ref _frequency, in value);
    }

    private float _amplitude;
    /// <summary>
    /// The vertical strength of this noise layer. Typically decreases as Frequency increases.
    /// </summary>
    public float Amplitude
    {
        get => _amplitude;
        set => RaiseAndSetIfChanged(ref _amplitude, in value);
    }

    private Float2 _offset;
    /// <summary>
    /// Offsets the noise sampling.
    /// </summary>
    public Float2 Offset
    {
        get => _offset;
        set => RaiseAndSetIfChanged(ref _offset, in value);
    }

    private BlendMode _mode = BlendMode.Add;
    public BlendMode Mode
    {
        get => _mode;
        set => RaiseAndSetIfChanged(ref _mode, in value);
    }

    public PerlinNoiseSampler()
    {
        _offset.X = MaxOffset * RandomUtil.Rand();
        _offset.Y = MaxOffset * RandomUtil.Rand();
        _amplitude = 100.0f;
        _frequency = 100.0f;
    }

    public void GetHeight(float u, float v, ref float height)
    {
        Float2 coord = default;
        coord.X = (u * _frequency) + _offset.X;
        coord.Y = (v * _frequency) + _offset.Y;

        float sampledValue = Noise.PerlinNoise(coord);

        height = _mode switch
        {
            BlendMode.Add => height + (sampledValue * _amplitude),
            BlendMode.Subtract => height - (sampledValue * _amplitude),
            _ => height * (sampledValue * _amplitude)
        };
    }
}
