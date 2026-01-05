using FlaxEngine;
using System.ComponentModel;

namespace ProceduralGraph.Terrain.Brushes;

#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = Mathd;
#else
using Real = System.Single;
using Mathr = Mathf;
#endif

/// <summary>
/// BoxSculptBrush class.
/// </summary>
[DisplayName("Box")]
public sealed class BoxSculptBrush : SculptBrush
{
    public override Real GetDistance(Vector3 localPos)
    {
        Real dx = Mathr.Max(Mathr.Abs(localPos.X) - Radius, 0);
        Real dy = Mathr.Max(Mathr.Abs(localPos.Y) - Radius, 0);
        Real dz = Mathr.Max(Mathr.Abs(localPos.Z) - Radius, 0);
        return Mathr.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public override void Update()
    {
        Color mainColor = Mode == SculptMode.Additive ? Color.Orange : Color.Cyan;
        Color falloffColor = mainColor * 0.5f;

        Transform t = Transform;

        Vector3 extents = Vector3.One * Radius;
        BoundingBox localBox = new(-extents, extents);

        OrientedBoundingBox obb = new(localBox);
        obb.Transform(ref t);

        DebugDraw.DrawWireBox(obb, mainColor);

        Vector3 falloffExt = Vector3.One * (Radius - Falloff);
        BoundingBox falloffBox = new(-falloffExt, falloffExt);
        OrientedBoundingBox falloffObb = new(falloffBox);
        falloffObb.Transform(ref t);

        DebugDraw.DrawWireBox(falloffObb, falloffColor);
    }
}
