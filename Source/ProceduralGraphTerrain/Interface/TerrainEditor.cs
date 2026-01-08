using FlaxEditor.CustomEditors;
using FlaxEngine;
using ProceduralGraph.Interface;

namespace ProceduralGraph.Terrain.Interface;

[CustomEditor(typeof(FlaxEngine.Terrain))]
internal sealed class TerrainEditor : ProceduralActorEditor
{
    public override void Initialize(LayoutElementsContainer layout)
    {
        base.Initialize(layout);

        if (!TryFindEntity(out IGraphEntity? entity))
        {
            return;
        }

        var group = layout.Group("Procedural Generation");
        group.Property("Components", entity.ValueContainer);

        if (entity is TerrainEntity terrainEntity)
        {
            var button = layout.Button("Regenerate").Button;
            button.Clicked += terrainEntity.MarkAsDirty;
        }
    }
}
