#nullable enable
using FlaxEngine;

namespace ProceduralGraph.Terrain;

internal readonly record struct TerrainPatches(int Size, Int2 Count, int Stride);