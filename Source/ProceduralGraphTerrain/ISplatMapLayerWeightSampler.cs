#nullable enable
namespace ProceduralGraph.Terrain;

public interface ISplatMapLayerWeightSampler
{
    /// <summary>
    /// The index of the terrain splat map layer to paint.
    /// </summary>
    int LayerIndex { get; }

    byte ComputeWeight(FlaxEngine.Terrain terrain, ref readonly float height, ref readonly float inclination);
}
