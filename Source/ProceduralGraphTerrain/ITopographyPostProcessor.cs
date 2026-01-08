namespace ProceduralGraph.Terrain;

/// <summary>
/// IHeightmapPostProcessor interface.
/// </summary>
public interface ITopographyPostProcessor
{
    unsafe void Apply(FlaxEngine.Terrain terrain, float* heightMapPtr, int heightMapLength, int width);
}