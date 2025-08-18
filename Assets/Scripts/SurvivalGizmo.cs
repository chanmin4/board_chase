#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class SurvivalGizmo : MonoBehaviour
{
    public BoardGrid board;
    public SurvivalDirector director;
    public Transform player;

    public Color zoneSmall = new Color(0.1f, 0.8f, 1f, 0.25f);
    public Color zoneMedium = new Color(0.1f, 1f, 0.4f, 0.25f);
    public Color zoneLarge = new Color(1f, 0.9f, 0.1f, 0.25f);
    public Color contam = new Color(0.6f, 0.2f, 0.8f, 0.25f);
    public Color gridLine = new Color(0,0,0,0.25f);
    public Color border = new Color(0,0,0,0.8f);
    public Color playerTile = new Color(1f, 1f, 1f, 0.35f);

    void OnDrawGizmos()
    {
        if (!board) return;
        var o = board.origin;
        float s = board.tileSize;

        // 격자
        Handles.color = gridLine;
        for (int x = 0; x <= board.width; x++)
            Handles.DrawLine(o + new Vector3(x * s, 0, 0), o + new Vector3(x * s, 0, board.height * s));
        for (int y = 0; y <= board.height; y++)
            Handles.DrawLine(o + new Vector3(0, 0, y * s), o + new Vector3(board.width * s, 0, y * s));

        // 테두리
        Handles.color = border;
        Vector3 p0 = o;
        Vector3 p1 = o + new Vector3(board.width * s, 0, 0);
        Vector3 p2 = o + new Vector3(board.width * s, 0, board.height * s);
        Vector3 p3 = o + new Vector3(0, 0, board.height * s);
        Handles.DrawAAPolyLine(3f, p0, p1, p2, p3, p0);

        if (!director || !director.HasState) return;
            // ✅ 유효 크기 계산 (board/direction 둘 중 작은 값 사용)
    int w = Mathf.Min(board.width,  director.Width);
    int h = Mathf.Min(board.height, director.Height);
    if (w <= 0 || h <= 0) return;

        // 오염칸
        for (int y = 0; y < board.height; y++)
        {
            for (int x = 0; x < board.width; x++)
            {
                if (director.IsContaminated(x, y))
                {
                    Handles.DrawSolidRectangleWithOutline(
                        new Rect(o.x + x * s, o.z + y * s, s, s), contam, Color.clear);
                }
            }
        }

        // 영역 (정식 enum으로 스위치)
        foreach (var zone in director.GetZones())
        {
            var kind = zone.kind;
            var tiles = zone.tiles;

            Color c;
            switch (kind)
            {
                case SurvivalDirector.ZoneKind.Small:  c = zoneSmall;  break;
                case SurvivalDirector.ZoneKind.Medium: c = zoneMedium; break;
                default:                               c = zoneLarge;  break;
            }

            foreach (var t in tiles)
            {
                if (t.x < 0 || t.y < 0 || t.x >= w || t.y >= h) continue;
                Handles.DrawSolidRectangleWithOutline(
                    new Rect(o.x + t.x * s, o.z + t.y * s, s, s), c, Color.clear);
            }
        }

        // 플레이어 현재 칸
        if (player && board.WorldToIndex(player.position, out int px, out int py))
        {
            Handles.DrawSolidRectangleWithOutline(
                new Rect(o.x + px * s, o.z + py * s, s, s), playerTile, Color.clear);
        }
    }
}
#endif
