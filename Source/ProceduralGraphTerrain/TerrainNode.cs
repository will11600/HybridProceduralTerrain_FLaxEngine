using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FlaxEngine;
using ProceduralGraph;

namespace AdvancedTerrainToolsEditor;

internal sealed class TerrainNode(Actor actor, IEnumerable<GraphModel> models, CancellationToken stoppingToken) : RealtimeGraphNode<TerrainGenerator>(actor, models, stoppingToken)
{
    protected override void OnUpdate()
    {
        base.OnUpdate();

        foreach (ISculptBrush brush in Models.OfType<ISculptBrush>())
        {
            brush.Update();
        }
    }
}