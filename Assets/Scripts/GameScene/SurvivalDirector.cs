
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// </summary>
public class SurvivalDirector : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;
    public Transform player;
    public Rigidbody playerRb;
    public SurvivalGauge gauge;
    public BoardMaskRenderer maskRenderer;

    // ===== (Legacy) Zone tuning fields (Risk 스크립트들이 참조함) =====
    [Header("Legacy Zone Tuning (unused while zones disabled)")]

    [Header("Legacy Spawn Rules (unused while zones disabled)")]
    public float minZoneSeparationTiles = 0.35f;
    public int excludeRadius = 1;
    public int minSpawnTileDistance = 6;
    public bool padByHalfFootprint = true;

    [Header("Legacy Clean Ratio (unused while zones disabled)")]
    public float baseAlpha = 0.50f;
    public float minAlpha = 0.10f;
    public float relaxStep = 0.05f;

    [Header("Legacy Zone Entry/Bounce (unused while zones disabled)")]
    public float zoneTouchToleranceTiles = 0.35f;
    public bool enableBonusSector = true;
    [Range(1f, 180f)] public float bonusArcDeg = 10f;
    public int normalHitAward = 1;
    public int bonusHitAward = 2;
    public float bonusRefreshDelay = 0.05f;

    public float zoneRestitution = 0.98f;
    public float reflectClampSpeed = 0f;
    public float zoneBounceCooldown = 0.08f;
    public float consumeLockAfterBounce = 0.15f;
    public bool requireExitReenterAfterBounce = true;

    [Header("Legacy Risk Tuning (unused while zones disabled)")]
    public float zoneEnterBonusMul = 1f;

    [Header("Legacy Risk Tuning - Per Size (unused while zones disabled)")]
    public int zoneReqHitsAdd_S = 0;
    public int zoneReqHitsAdd_M = 0;
    public int zoneReqHitsAdd_L = 0;

    [Header("Legacy Layout Counts (unused while zones disabled)")]
    public bool useLayoutCounts = true;
    [Min(0)] public int layoutCountSmall = 0;
    [Min(0)] public int layoutCountMedium = 0;
    [Min(0)] public int layoutCountLarge = 0;

    [Tooltip("Zones 시스템을 다시 붙일 때만 true로 사용(현재는 false 권장)")]
    public bool zonesEnabled = false;

    [Tooltip("미충족 후엔 한 번 존 밖으로 나갔다 재진입해야 소비 허용 (Legacy)")]
    public int ResetSeq { get; private set; } = 0;

    // ===== Events =====
    // 오염/보드 관련(유지)
    public event Action<Vector3, float> OnClearedCircleWorld;
    public event Action<Vector3, int, int> OnEnterContam; // (worldPos, ix, iy)
    public event Action<Vector3, int, int> OnExitContam;  // (worldPos, ix, iy)
    public event Action<Vector3, float, bool> OnPlayerPaintCircleWorld;
    public event Action ContamSpawn;

    public bool HasState =>
        board != null &&
        _state != null &&
        _state.Length == board.width * board.height;

    // ===== Internal Contamination State =====
    enum TileState { Clean, Contaminated }
    TileState[] _state;
    bool _prevInContam = false;

    int Idx(int x, int y) => y * board.width + x;

    void Awake()
    {
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!playerRb && player) playerRb = player.GetComponent<Rigidbody>();

        if (board)
        {
            _state = new TileState[board.width * board.height];
            for (int i = 0; i < _state.Length; i++) _state[i] = TileState.Clean;
        }
    }

    void Start()
    {
        // zonesEnabled=true로 다시 붙일 때만 사용(현재는 호출하지 않음)
        // if (zonesEnabled) RegenerateAllZones();
    }

    void Update()
    {
        if (!board || !player) return;

        // 플레이어가 오염 타일 위에 있는지(게이지/이벤트)
        if (board.WorldToIndex(player.position, out int px, out int py))
        {
            bool now = IsContaminated(px, py);
            gauge?.SetContaminated(now);

            if (now != _prevInContam)
            {
                if (now) OnEnterContam?.Invoke(player.position, px, py);
                else OnExitContam?.Invoke(player.position, px, py);
                _prevInContam = now;
            }
        }

        // Zone 루프(임시 비활성)
        // if (zonesEnabled) { ... }  // 나중에 ZoneSpawner/Interaction을 붙일 때 별도 컴포넌트로 구현 권장
    }

    // ===== Contamination utilities =====

    IEnumerable<Vector2Int> CollectCircleTiles(Vector2Int center, float radiusTiles)
    {
        if (!board) yield break;

        int rCeil = Mathf.CeilToInt(radiusTiles);
        float r2 = radiusTiles * radiusTiles;

        for (int y = center.y - rCeil; y <= center.y + rCeil; y++)
        for (int x = center.x - rCeil; x <= center.x + rCeil; x++)
        {
            if (x < 0 || y < 0 || x >= board.width || y >= board.height) continue;
            float dx = x - center.x;
            float dy = y - center.y;
            if (dx * dx + dy * dy <= r2) yield return new Vector2Int(x, y);
        }
    }

    public IEnumerable<Vector2Int> CollectCircleTilesPublic(Vector2Int center, float radiusTiles)
    {
        return CollectCircleTiles(center, radiusTiles);
    }

    public void ClearContamination(int x, int y)
    {
        if (!board || _state == null) return;
        if (x < 0 || y < 0 || x >= board.width || y >= board.height) return;

        int idx = Idx(x, y);
        if (_state[idx] == TileState.Contaminated)
            _state[idx] = TileState.Clean;
    }

    public bool IsContaminated(int x, int y)
    {
        if (!board || _state == null) return false;
        if (x < 0 || y < 0 || x >= board.width || y >= board.height) return false;

        int idx = Idx(x, y);
        if (idx < 0 || idx >= _state.Length) return false;

        return _state[idx] == TileState.Contaminated;
    }

    /// <summary>
    /// 월드 좌표/반경으로 오염 지대 생성 (미사일/고스트 등이 호출)
    /// </summary>
    public void ContaminateCircleWorld(Vector3 centerWorld, float radiusWorld)
    {
        if (!board || _state == null) return;
        if (!board.WorldToIndex(centerWorld, out int cx, out int cy)) return;

        float radiusTiles = radiusWorld / Mathf.Max(0.0001f, board.tileSize);

        foreach (var t in CollectCircleTiles(new Vector2Int(cx, cy), radiusTiles))
            _state[Idx(t.x, t.y)] = TileState.Contaminated;

        // (Legacy) ZoneContam 이벤트로도 알림(구독자 호환용)
       // OnZoneContaminatedCircle?.Invoke(-999, centerWorld, radiusWorld);
        ContamSpawn?.Invoke();
    }

    /// <summary>
    /// 월드 좌표/반경으로 오염 제거
    /// </summary>
    public void ClearCircleWorld(Vector3 centerWorld, float radiusWorld)
    {
        if (!board || _state == null) return;
        if (!board.WorldToIndex(centerWorld, out int cx, out int cy)) return;

        float rTiles = radiusWorld / Mathf.Max(0.0001f, board.tileSize);

        foreach (var t in CollectCircleTiles(new Vector2Int(cx, cy), rTiles))
            ClearContamination(t.x, t.y);

        OnClearedCircleWorld?.Invoke(centerWorld, radiusWorld);
    }

    /// <summary>
    /// 플레이어 잉크(페인트) + 필요시 오염 마스크 덮어쓰기(렌더러 이벤트)
    /// - applyBoardClean: 보드의 오염 state까지 제거할지
    /// - clearPollutionMask: 시각 오염 마스크만 0으로 지울지
    /// </summary>
    public void PaintPlayerCircleWorld(Vector3 centerWorld, float radiusWorld,
                                       bool applyBoardClean, bool clearPollutionMask)
    {
        if (applyBoardClean)
            ClearCircleWorld(centerWorld, radiusWorld);

        OnPlayerPaintCircleWorld?.Invoke(centerWorld, radiusWorld, clearPollutionMask);

        if (clearPollutionMask)
            OnClearedCircleWorld?.Invoke(centerWorld, radiusWorld);
    }

    // ===== Legacy stubs (Risk/old UI compile compatibility) =====

    /// <summary>
    /// (Legacy) Zones 전체 재생성. 현재는 "리셋 이벤트만 발행"하고 실제 스폰은 하지 않음.
    /// </summary>
    public void RegenerateAllZones()
    {
        ResetSeq++;
       // OnZonesResetSeq?.Invoke(ResetSeq);
       // OnZonesReset?.Invoke();
        // zonesEnabled=true일 때의 실제 스폰은 ZoneSpawner에서 처리하도록 분리하는 걸 추천
    }

    // 유지: 다른 스크립트가 ppu를 묻는 헬퍼
    public int maskRendererPlayerPixelsPerTile()
    {
        if (!maskRenderer)
            maskRenderer = FindAnyObjectByType<BoardMaskRenderer>();

        return maskRenderer ? Mathf.Max(1, maskRenderer.PlayerPixelsPerTile) : 15;
    }
}
