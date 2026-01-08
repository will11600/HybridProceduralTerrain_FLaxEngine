using System;

namespace ProceduralGraph.Terrain;

internal interface IPatchMap : IDisposable
{
    void Setup(FlaxEngine.Terrain terrain);
}