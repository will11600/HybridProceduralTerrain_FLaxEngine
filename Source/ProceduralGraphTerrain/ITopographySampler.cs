namespace ProceduralGraph.Terrain;

/// <summary>
/// ITopographySampler interface.
/// </summary>
public interface ITopographySampler
{
    void GetHeight(FlaxEngine.Terrain terrain, float u, float v, ref float height);
}
