using FlaxEngine;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace ProceduralGraph.Terrain.Topography.Processors;

[DisplayName("Beach")]
public sealed class BeachProcessor : GraphComponent, ITopographyPostProcessor
{
    private const float UnderwaterBlendFactor = 0.8f;

    private float _sedimentStiffness = 0.2f;
    [Range(0.0f, 1.0f)]
    public float SedimentStiffness
    {
        get => _sedimentStiffness;
        set => RaiseAndSetIfChanged(ref _sedimentStiffness, in value);
    }

    private float _seaLevel = 0.0f;
    public float SeaLevel
    {
        get => _seaLevel;
        set => RaiseAndSetIfChanged(ref _seaLevel, in value);
    }

    private float _shoreWidth = 50.0f;
    public float ShoreWidth
    {
        get => _shoreWidth;
        set => RaiseAndSetIfChanged(ref _shoreWidth, in value);
    }

    public unsafe void Apply(FlaxEngine.Terrain terrain, FatPointer<float> heightMap, int width)
    {
        float seaLevel = SeaLevel;
        int length = heightMap.Length;
        float* heightBuffer = heightMap.Buffer;
        float shoreWidth = ShoreWidth;

        using CoastlineSdf sdf = new(heightMap, width, seaLevel);
        Parallel.For(0, sdf.Height, sdf.ProcessRow);
        Parallel.For(0, width, sdf.ProcessColumn);

        float* sdfBuffer = sdf.Buffer;
        float threshold = ShoreWidth * 2.0f;
        for (int i = 0; i < length; i++)
        {
            ref readonly float distanceToShoreline = ref sdfBuffer[i];
            ref float height = ref heightBuffer[i];

            if (distanceToShoreline > threshold && height > seaLevel)
            {
                continue;
            }

            if (height <= seaLevel)
            {
                float targetDepth = seaLevel - DeansFormula(SedimentStiffness, distanceToShoreline);
                height = Mathf.Lerp(height, targetDepth, UnderwaterBlendFactor);
            }
            else if (distanceToShoreline <= ShoreWidth)
            {
                heightBuffer[i] = Mathf.Lerp(seaLevel, height, distanceToShoreline / shoreWidth);
            }
        }

        string path = Path.Combine(Globals.ProjectFolder, "Coastline SDF.bmp");
        MapDebug.SaveToBmp(path, sdfBuffer, width, sdf.Height);
    }

    private static float DeansFormula(float sedimentStiffness, float distanceToShoreline)
    {
        const float TwoOverThree = 2.0f / 3.0f;
        return sedimentStiffness * Mathf.Pow(distanceToShoreline, TwoOverThree);
    }
}
