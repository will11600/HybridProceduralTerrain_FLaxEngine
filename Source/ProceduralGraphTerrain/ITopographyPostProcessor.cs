using System;

namespace AdvancedTerrainToolsEditor;

/// <summary>
/// IHeightmapPostProcess interface.
/// </summary>
public interface ITopographyPostProcessor
{
    void Apply(Memory<float> heightmap, int size);
}
