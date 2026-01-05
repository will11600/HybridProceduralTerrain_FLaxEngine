using FlaxEngine;

namespace ProceduralGraph.Terrain;

#if USE_LARGE_WORLDS
using Real = System.Double;
#else
using Real = System.Single;
#endif

public interface ITerrainModifier
{
    Real GetDistance(Vector3 localPos);
}