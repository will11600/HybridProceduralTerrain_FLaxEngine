namespace ProceduralGraph.Terrain;

/// <summary>
/// ITopographySampler interface.
/// </summary>
public interface ITopographySampler
{
    void GetHeight(float u, float v, ref float height);
}
