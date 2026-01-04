using Flax.Build;

public class ProceduralGraphTerrainEditorTarget : GameProjectEditorTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for editor
        Modules.Add("AdvancedTerrainTools");
        Modules.Add(nameof(ProceduralGraphTerrain));
    }
}
