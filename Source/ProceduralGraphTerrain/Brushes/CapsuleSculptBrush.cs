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
/// CapsuleSculptBrush class.
/// </summary>
[DisplayName("Capsule")]
public sealed class CapsuleSculptBrush : SculptBrush
{
    public override Real GetDistance(Vector3 localPos)
    {
        Vector3 a = new(0, 0, Radius);
        Vector3 b = new(0, 0, -Radius);
        Vector3 pa = localPos - a;
        Vector3 ba = b - a;
        Real h = Mathr.Saturate(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba));
        return (pa - ba * h).Length;
    }

    public override void Update()
    {
        Color mainColor = Mode == SculptMode.Additive ? Color.Orange : Color.Cyan;
        Color falloffColor = mainColor * 0.5f;

        Vector3 dir = Transform.Forward;
        Real halfLen = Radius * Transform.Scale.Z;
        Real rad = (Radius - Falloff) * Transform.Scale.X;

        Vector3 a = Transform.Translation + dir * halfLen;
        Vector3 b = Transform.Translation - dir * halfLen;
        DebugDraw.DrawLine(a, b, mainColor);
        DebugDraw.DrawWireSphere(new BoundingSphere(a, rad), mainColor);
        DebugDraw.DrawWireSphere(new BoundingSphere(b, rad), mainColor);
        Real falloffRad = Radius * Transform.Scale.X;
        DebugDraw.DrawWireSphere(new BoundingSphere(a, falloffRad), falloffColor);
        DebugDraw.DrawWireSphere(new BoundingSphere(b, falloffRad), falloffColor);
    }
}
