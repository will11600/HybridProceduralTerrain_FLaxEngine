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

    public IGraphEntity Convert(Actor actor, IEnumerable<GraphComponent> components, CancellationToken stoppingToken)
    {
        return new TerrainEntity(actor, components, stoppingToken);
    }
}
