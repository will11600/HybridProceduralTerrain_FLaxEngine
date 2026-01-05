using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FlaxEngine;
using ProceduralGraph;

namespace AdvancedTerrainToolsEditor;

internal sealed class TerrainEntity(Actor actor, IEnumerable<GraphComponent> components, CancellationToken stoppingToken) : 
    RealtimeGraphEntity<TerrainGenerator>(actor, components, stoppingToken)
{
    protected override void OnUpdate()
    {
        base.OnUpdate();

        foreach (ISculptBrush brush in Components.OfType<ISculptBrush>())
        {
            brush.Update();
        }
    }
}