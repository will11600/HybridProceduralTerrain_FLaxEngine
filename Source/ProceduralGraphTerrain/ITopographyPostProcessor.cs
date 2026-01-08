using System;

namespace ProceduralGraph.Terrain;

/// <summary>
/// IHeightmapPostProcess interface.
/// </summary>
public interface ITopographyPostProcessor
{
    void Apply(Span<float> heightmap, int size);
}
