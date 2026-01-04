using System.Collections.Generic;
using System.Threading;
using FlaxEngine;
using ProceduralGraph;

namespace AdvancedTerrainToolsEditor;

internal sealed class TerrainConverter : IGraphConverter
{
    public bool CanConvert(Actor actor)
    {
        return actor is Terrain;
    }

    public IGraphNode Convert(Actor actor, IEnumerable<GraphModel> models, CancellationToken stoppingToken)
    {
        return new TerrainNode(actor, models, stoppingToken);
    }
}
