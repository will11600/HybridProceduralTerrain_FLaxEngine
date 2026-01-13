namespace ProceduralGraph.Terrain;

/// <summary>
/// IHeightmapPostProcess interface.
/// </summary>
public interface ITopographyPostProcessor
{
    void Apply(FlaxEngine.Terrain terrain, FatPointer2D<float> heightMap);
}
