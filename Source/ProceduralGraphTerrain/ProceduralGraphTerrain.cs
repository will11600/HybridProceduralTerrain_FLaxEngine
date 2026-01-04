using FlaxEditor;
using FlaxEngine;
using ProceduralGraph;

namespace AdvancedTerrainToolsEditor;

[PluginLoadOrder(DeinitializeBefore = typeof(GraphLifecycleManager), InitializeAfter = typeof(GraphLifecycleManager))]
internal sealed class ProceduralGraphTerrain : EditorPlugin
{
    private TerrainConverter? _converter;

    public ProceduralGraphTerrain() : base()
    {
        _description = new PluginDescription()
        {
            Name = "Procedural Graph: Terrain",
            Author = "William Brocklesby",
            AuthorUrl = "https://william-brocklesby.com",
            Category = "Procedural Graph",
            Description = "",
            RepositoryUrl = "https://github.com/will11600/Procedural-Graph-Terrain.git",
            Version = new(1, 0, 0)
        };
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        _converter = new TerrainConverter();
        GraphLifecycleManager lifecycleManager = PluginManager.GetPlugin<GraphLifecycleManager>();
        lifecycleManager.Converters.Add(_converter);
    }

    /// <inheritdoc/>
    public override void Deinitialize()
    {
        base.Deinitialize();
        if (_converter is { })
        {
            GraphLifecycleManager lifecycleManager = PluginManager.GetPlugin<GraphLifecycleManager>();
            lifecycleManager.Converters.Remove(_converter);
        }
    }
}
