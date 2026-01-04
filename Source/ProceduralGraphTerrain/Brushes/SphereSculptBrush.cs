using FlaxEngine;
using System.ComponentModel;

namespace AdvancedTerrainToolsEditor.Brushes;

#if USE_LARGE_WORLDS
using Real = System.Double;
#else
using Real = System.Single;
#endif

/// <summary>
/// SphereSculptBrush class.
/// </summary>
[DisplayName("Sphere")]
public sealed class SphereSculptBrush : SculptBrush
{
    public override Real GetDistance(Vector3 localPos)
    {
        return localPos.Length;
    }

    public override void Update()
    {
        Color mainColor = Mode == SculptMode.Additive ? Color.Orange : Color.Cyan;
        Color falloffColor = mainColor * 0.5f;

        Vector3 pos = Transform.Translation;
        Float3 scale = Transform.Scale;

        float maxScale = Mathf.Max(scale.X, Mathf.Max(scale.Y, scale.Z));

        DebugDraw.DrawWireSphere(
            new BoundingSphere(pos, Radius * maxScale),
            mainColor
        );

        DebugDraw.DrawWireSphere(
            new BoundingSphere(pos, (Radius - Falloff) * maxScale),
            falloffColor
        );
    }
}
