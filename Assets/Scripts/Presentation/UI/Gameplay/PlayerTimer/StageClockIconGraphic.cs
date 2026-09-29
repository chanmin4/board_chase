/*
 * Role hierarchy: Presentation > UI > Gameplay > PlayerTimer
 * Primary responsibility: Manages StageClockIconGraphic stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small resolution-independent clock icon used by the compact stage timer.
/// It avoids relying on a font glyph or an imported temporary sprite.
/// </summary>
[DisallowMultipleComponent]
public sealed class StageClockIconGraphic : MaskableGraphic
{
    private const int CircleSegments = 24;

    // Purpose: Responds to the populate mesh callback.
    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * 0.43f);
        float thickness = Mathf.Max(1.5f, radius * 0.14f);

        for (int i = 0; i < CircleSegments; i++)
        {
            float angleA = i / (float)CircleSegments * Mathf.PI * 2f;
            float angleB = (i + 1) / (float)CircleSegments * Mathf.PI * 2f;
            AddLine(
                vertexHelper,
                center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * radius,
                center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * radius,
                thickness);
        }

        AddLine(vertexHelper, center, center + Vector2.up * radius * 0.58f, thickness);
        AddLine(vertexHelper, center, center + Vector2.right * radius * 0.42f, thickness);
    }

    // Purpose: Adds the requested line to this component.
    private void AddLine(VertexHelper helper, Vector2 start, Vector2 end, float width)
    {
        Vector2 direction = end - start;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Vector2 normal = new(-direction.y, direction.x);
        normal.Normalize();
        normal *= width * 0.5f;

        int vertexStart = helper.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = start - normal;
        helper.AddVert(vertex);
        vertex.position = start + normal;
        helper.AddVert(vertex);
        vertex.position = end + normal;
        helper.AddVert(vertex);
        vertex.position = end - normal;
        helper.AddVert(vertex);

        helper.AddTriangle(vertexStart, vertexStart + 1, vertexStart + 2);
        helper.AddTriangle(vertexStart, vertexStart + 2, vertexStart + 3);
    }
}
