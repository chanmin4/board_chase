#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Temporary editor-play overlay. One UI batch; no camera, collider or raycast target.</summary>
public sealed class GameColliderOverlay : Graphic
{
    public Action<VertexHelper, RectTransform> Populate;
    public Action DrawLabels;
    private void LateUpdate()
    {
        if (Populate != null) SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Populate?.Invoke(vertices, rectTransform);
    }
    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || DrawLabels == null) return;
        int previous = GUI.depth;
        GUI.depth = -1000;
        try { DrawLabels(); }
        finally { GUI.depth = previous; }
    }
    public static bool ProjectSegment(Camera camera, RectTransform root, Vector3 a, Vector3 b,
        out Vector2 start, out Vector2 end)
    {
        start = end = default;
        if (camera == null) return false;
        float near = camera.nearClipPlane + .001f;
        float za = Vector3.Dot(a - camera.transform.position, camera.transform.forward);
        float zb = Vector3.Dot(b - camera.transform.position, camera.transform.forward);
        if (za < near && zb < near) return false;
        if (za < near) a = Vector3.Lerp(a, b, (near - za) / (zb - za));
        else if (zb < near) b = Vector3.Lerp(b, a, (near - zb) / (za - zb));
        Vector3 sa = camera.WorldToScreenPoint(a), sb = camera.WorldToScreenPoint(b);
        Vector2 delta = sb - sa;
        Rect viewport = camera.pixelRect;
        float first = 0, last = 1;
        if (!Clip(-delta.x, sa.x - viewport.xMin, ref first, ref last) ||
            !Clip(delta.x, viewport.xMax - sa.x, ref first, ref last) ||
            !Clip(-delta.y, sa.y - viewport.yMin, ref first, ref last) ||
            !Clip(delta.y, viewport.yMax - sa.y, ref first, ref last)) return false;
        Vector2 clippedStart = (Vector2)sa + delta * first;
        Vector2 clippedEnd = (Vector2)sa + delta * last;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, clippedStart, null, out start);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, clippedEnd, null, out end);
        return true;
    }
    private static bool Clip(float p, float q, ref float first, ref float last)
    {
        if (Mathf.Abs(p) < .00001f) return q >= 0;
        float t = q / p;
        if (p < 0) { if (t > last) return false; first = Mathf.Max(first, t); }
        else { if (t < first) return false; last = Mathf.Min(last, t); }
        return true;
    }
    public static void AddLine(VertexHelper vertices, Vector2 start, Vector2 end, Color color)
    {
        Vector2 delta = end - start;
        if (delta.sqrMagnitude < .0001f || vertices.currentVertCount > 63996) return;
        var width = new Vector2(-delta.y, delta.x).normalized * .8f;
        int index = vertices.currentVertCount;
        vertices.AddVert(start - width, color, Vector2.zero);
        vertices.AddVert(start + width, color, Vector2.zero);
        vertices.AddVert(end + width, color, Vector2.zero);
        vertices.AddVert(end - width, color, Vector2.zero);
        vertices.AddTriangle(index, index + 1, index + 2);
        vertices.AddTriangle(index, index + 2, index + 3);
    }
}
#endif
