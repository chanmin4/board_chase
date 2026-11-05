using UnityEngine;
using UnityEngine.UI;

/// <summary>Small texture-free ring. Never captures clicks or changes interaction range.</summary>
public sealed class InteractionNearbyDotGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        float radius = Mathf.Min(rect.width, rect.height) * .5f;
        AddRing(vh, rect.center, radius, radius * .45f, new Color(0f, 0f, 0f, .8f));
        AddRing(vh, rect.center, radius * .85f, radius * .6f, color);
    }

    private static void AddRing(VertexHelper vh, Vector2 center, float outer, float inner, Color tint)
    {
        const int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float b = (i + 1) * Mathf.PI * 2f / segments;
            Vector2 first = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 second = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            int start = vh.currentVertCount;
            vh.AddVert(center + first * outer, tint, Vector2.zero);
            vh.AddVert(center + second * outer, tint, Vector2.zero);
            vh.AddVert(center + second * inner, tint, Vector2.zero);
            vh.AddVert(center + first * inner, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
