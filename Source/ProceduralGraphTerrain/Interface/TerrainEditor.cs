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
        if (TryFindEntity(out IGraphEntity? entity))
        {
            var group = layout.Group("Procedural Generation");
            group.Property("Components", entity.ValueContainer);
        }
    }
}
