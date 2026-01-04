using FlaxEngine;

namespace AdvancedTerrainToolsEditor;

/// <summary>
/// ITopographyProvider interface.
/// </summary>
public interface ITopographyProvider
{
    void GetHeight(float u, float v, ref float height);
}
