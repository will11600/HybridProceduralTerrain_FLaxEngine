using System.Collections.Generic;
using System.Threading;
using FlaxEngine;

namespace ProceduralGraph.Terrain;

using TerrainActor = FlaxEngine.Terrain;

internal sealed class TerrainConverter : IGraphConverter
{
    public bool CanConvert(Actor actor)
    {
        return actor is TerrainActor;
    }

    public IGraphEntity Convert(Actor actor, IEnumerable<GraphComponent> components, CancellationToken stoppingToken)
    {
        return new TerrainEntity(actor, components, stoppingToken);
    }
}
