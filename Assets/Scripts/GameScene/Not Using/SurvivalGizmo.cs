/*

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// SceneView에 보드 격자, 오염 타일, 현재 스폰된 LifeZone(원형), 플레이어 타일을 그려주는 Gizmo.
/// - Inspector-Driven ZoneProfile 구조에 맞춰 동작
/// - Director 이벤트를 구독하여 현재 존 스냅샷을 캐시하고 그림
/// </summary>
[ExecuteAlways]
public class SurvivalGizmo : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;
    public SurvivalDirector director;
    public Transform player;

    [Header("Colors")]
    public Color gridLine    = new Color(0, 0, 0, 0.25f);
    public Color border      = new Color(0, 0, 0, 0.80f);
    public Color playerTileC = new Color(1f, 1f, 1f, 0.35f);
    public Color contamTileC = new Color(0.6f, 0.2f, 0.8f, 0.25f);

    // 프로필 인덱스별 기본 팔레트 (필요시 인스펙터에서 바꿔도 됨)
    public Color[] profileFill = new Color[]
    {
        new Color(0.10f, 0.80f, 1.00f, 0.20f), // P0
        new Color(0.10f, 1.00f, 0.40f, 0.20f), // P1
        new Color(1.00f, 0.90f, 0.10f, 0.20f), // P2
        new Color(1.00f, 0.40f, 0.20f, 0.20f), // P3
        new Color(0.60f, 0.50f, 1.00f, 0.20f), // P4
        new Color(0.20f, 1.00f, 0.80f, 0.20f), // P5
    };
    public Color ringLine = new Color(0f, 0f, 0f, 0.9f);

    // 진행도(세트 타이머) 표시에 쓰는 링 두께
    const float RING_THICKNESS = 2.0f;

    // 현재 스폰된 존 스냅샷 캐시 (director 이벤트로 갱신)
    struct Snap
    {
        public int id;
        public int profileIndex;
        public Vector3 centerW;
        public float baseRadiusW;
        public float progress01; // 세트 진행도(0~1)
    }
    Dictionary<int, Snap> _zones = new Dictionary<int, Snap>();

    void OnEnable()
    {
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();

        Subscribe();
        // 플레이 모드가 아니면 즉시 그림만 담당(스폰 이벤트는 런타임에 들어옴)
    }

    void OnDisable()
    {
        Unsubscribe();
        _zones.Clear();
    }

    void Subscribe()
    {
        if (!director) return;
        Unsubscribe();

        director.OnZonesReset += HandleReset;
        director.OnZoneSpawned += HandleSpawn;
        director.OnZoneExpired += HandleExpired;
        director.OnZoneProgress += HandleProgress;
        director.OnZoneConsumed += HandleConsumed;
    }

    void Unsubscribe()
    {
        if (!director) return;
        director.OnZonesReset    -= HandleReset;
        director.OnZoneSpawned   -= HandleSpawn;
        director.OnZoneExpired   -= HandleExpired;
        director.OnZoneProgress  -= HandleProgress;
        director.OnZoneConsumed  -= HandleConsumed;
    }

    void HandleReset()
    {
        _zones.Clear();
        SceneView.RepaintAll();
    }

    void HandleSpawn(ZoneSnapshot s)
    {
        _zones[s.id] = new Snap {
            id           = s.id,
            profileIndex = s.profileIndex,
            centerW      = s.centerWorld,
            baseRadiusW  = s.baseRadius,
            progress01   = 0f
        };
        SceneView.RepaintAll();
    }

    void HandleExpired(int id)
    {
        if (_zones.ContainsKey(id)) _zones.Remove(id);
        SceneView.RepaintAll();
    }

    void HandleConsumed(int id)
    {
        // 소비로 사라진 존: 기즈모에서도 제거
        if (_zones.ContainsKey(id)) _zones.Remove(id);
        SceneView.RepaintAll();
    }

    void HandleProgress(int id, float p01)
    {
        if (_zones.TryGetValue(id, out var s))
        {
            s.progress01 = Mathf.Clamp01(p01);
            _zones[id] = s;
            // SceneView.RepaintAll(); // 매 프레임 호출되므로 생략 가능
        }
    }

    void OnDrawGizmos()
    {
        if (!board) return;

        var o = board.origin;
        float s = board.tileSize;

        // 1) 격자
        Handles.color = gridLine;
        for (int x = 0; x <= board.width; x++)
            Handles.DrawLine(o + new Vector3(x * s, 0, 0), o + new Vector3(x * s, 0, board.height * s));
        for (int y = 0; y <= board.height; y++)
            Handles.DrawLine(o + new Vector3(0, 0, y * s), o + new Vector3(board.width * s, 0, y * s));

        // 2) 테두리
        Handles.color = border;
        Vector3 p0 = o;
        Vector3 p1 = o + new Vector3(board.width * s, 0, 0);
        Vector3 p2 = o + new Vector3(board.width * s, 0, board.height * s);
        Vector3 p3 = o + new Vector3(0, 0, board.height * s);
        Handles.DrawAAPolyLine(3f, p0, p1, p2, p3, p0);

        // 디렉터 상태 체크(오염/존)
        if (director == null || !director.HasState) 
        {
            DrawPlayerTile(o, s);
            return;
        }

        // 3) 오염 타일
        Handles.color = contamTileC;
        for (int y = 0; y < board.height; y++)
        {
            for (int x = 0; x < board.width; x++)
            {
                if (director.IsContaminated(x, y))
                {
                    Handles.DrawSolidRectangleWithOutline(
                        new Rect(o.x + x * s, o.z + y * s, s, s), contamTileC, Color.clear);
                }
            }
        }

        // 4) 존(원형)
        foreach (var kv in _zones)
        {
            var z = kv.Value;
            var fill = ProfileFill(z.profileIndex);

            // 밑면 채우기(반투명 원 디스크)
            Handles.color = fill;
            Handles.DrawSolidDisc(z.centerW, Vector3.up, z.baseRadiusW);

            // 진행 링(0 → baseRadius까지)
            float r = Mathf.Lerp(0f, z.baseRadiusW, z.progress01);
            if (r > 0.0001f)
            {
                Handles.color = ringLine;
                Handles.DrawWireDisc(z.centerW, Vector3.up, r, RING_THICKNESS);
            }
        }

        // 5) 플레이어 타일 마커
        DrawPlayerTile(o, s);
    }

    void DrawPlayerTile(Vector3 origin, float tileSize)
    {
        if (!player || !board) return;
        if (board.WorldToIndex(player.position, out int px, out int py))
        {
            Handles.DrawSolidRectangleWithOutline(
                new Rect(origin.x + px * tileSize, origin.z + py * tileSize, tileSize, tileSize),
                playerTileC, Color.clear);
        }
    }

    Color ProfileFill(int profileIndex)
    {
        if (profileFill != null && profileFill.Length > 0)
        {
            int i = Mathf.Abs(profileIndex) % profileFill.Length;
            return profileFill[i];
        }
        // fallback
        return new Color(0.2f, 0.8f, 1f, 0.2f);
    }
}
#endif
*/