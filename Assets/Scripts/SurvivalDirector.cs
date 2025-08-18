using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// 비주얼 전달용 스냅샷
public struct ZoneSnapshot
{
    public int id;
    public SurvivalDirector.ZoneKind kind;
    public Vector3 centerWorld;
    public float baseRadius;   // 돔 밑면 반지름(월드)
    public float lifetime;
    public float remain;
}

public class SurvivalDirector : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;
    public Transform player;               // Disk(플레이어)
    public Rigidbody playerRb;             // 디스크 Rigidbody
    public SurvivalGauge gauge;

    [Header("Zone Lifetime")]
    public float zoneLifetime = 10f;       // 시간 내 미진입 → 만료
    [Header("Zone Spacing")]
    [Tooltip("존들 사이 최소 간격(타일 기준, 원 반지름 합 + 이 값 이상 떨어지도록)")]
    public float minZoneSeparationTiles = 0.35f;

    [Header("Gauge Gains (per sec)")]
    public float gainSmall = 12f;
    public float gainMedium = 9f;
    public float gainLarge = 6f;

    [Header("Clean Ratio")]
    public float baseAlpha = 0.5f;         // 50%
    public float minAlpha = 0.10f;        // 10%
    public float relaxStep = 0.05f;        // 5%p

    [Header("Wall Hit Requirements")]
    public int reqSmall = 2;
    public int reqMedium = 1;
    public int reqLarge = 0;

    [Header("Zone Footprints (tile size)")]
    public Vector2Int smallSize = new Vector2Int(2, 2);
    public Vector2Int mediumSize = new Vector2Int(3, 3);
    public Vector2Int largeSize = new Vector2Int(4, 4);

    [Header("Player Around Exclusion")]
    public int excludeRadius = 1;          // 플레이어 주변 1칸 제외

    [Header("Reflect")]
    public float reflectClampSpeed = 18f;
    [Header("Spawn Distance From Player")]
    public int minSpawnTileDistance = 6;     // 플레이어 타일로부터 최소 거리(타일 단위, 중심-중심)
    public bool padByHalfFootprint = true;   // 존 크기만큼 추가 패딩

    [Header("Zone Bounce Cooldown")]
    public float zoneBounceCooldown = 0.08f; // 같은 존에서 연속 튕김 방지
    int lastBounceZoneId = -1;
    float lastBounceTime = -999f;

    // 클래스 상단 헤더 근처에 추가
    [Header("Zone Entry (consume)")]
    public float enterBonusSmall = 15f;
    public float enterBonusMedium = 12f;
    public float enterBonusLarge = 9f;
    [Tooltip("플레이어-돔 접촉 판정 여유(타일 단위)")]
    public float zoneTouchToleranceTiles = 0.35f;   // 0.35~0.5 권장

    [Header("Zone Entry Lockouts")]
    [Tooltip("미충족으로 튕긴 뒤, 이 시간동안은 소비 불가")]
    public float consumeLockAfterBounce = 0.15f;
    [Tooltip("튕긴 뒤엔 한 번 존 밖으로 나갔다가 다시 들어와야 소비 허용")]
    public bool requireExitReenterAfterBounce = true;

public event System.Action<int> OnZoneConsumed; // ★ 소비(성공 진입) 알림

    public bool HasState => state != null && board != null;

    // ===== 비주얼 이벤트 =====
    public event Action<ZoneSnapshot> OnZoneSpawned;
    public event Action<int> OnZoneExpired;
    public event Action OnZonesReset;
    public event Action<int, float> OnZoneProgress;             // 0~1
    public event Action<int, Vector3, float> OnZoneContaminatedCircle; // (id, centerWorld, radiusWorld)

    //벽 이벤트
    public event System.Action<int> OnWallHitsChanged; // UI용
    public int CurrentWallHits => wallHits;

    int nextZoneId = 1;

    // ===== 내부 상태 =====
    public enum ZoneKind { Small, Medium, Large }

    class Zone
    {
        public int id;
        public ZoneKind kind;
        public Vector2Int center;       // 중심 타일
        public List<Vector2Int> tiles;  // 블록(검증용)
        public float remain;
        public int reqHits;
        public float gain;
        public Vector2Int footprint;    // 2x2/3x3/4x4
        public Vector3 centerWorld;
        public float radiusWorld;
        public float consumeUnlockTime = 0f; // 이 시간 전엔 소비 금지
        public bool  mustExitFirst = false;  // 존 밖으로 한 번 나가야 함
    }

    enum TileState { Clean, Contaminated }
    TileState[] state;

    List<Zone> zones = new List<Zone>();
    int wallHits = 0;

    public int Width => board ? board.width : 0;
    public int Height => board ? board.height : 0;
    int Idx(int x, int y) => y * board.width + x;
    public (int small, int medium, int large) GetWallRequirements()
    => (reqSmall, reqMedium, reqLarge);

    void Awake()
    {
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!playerRb && player) playerRb = player.GetComponent<Rigidbody>();

        state = new TileState[board.width * board.height];
        for (int i = 0; i < state.Length; i++) state[i] = TileState.Clean;

        RegenerateAllZones();
    }

    void Update()
    {
        if (!board || !player) return;

        // 1) 플레이어 현재 칸 & 오염 표시
        int px, py;
        bool onBoard = board.WorldToIndex(player.position, out px, out py);
        gauge?.SetContaminated(onBoard && IsContaminated(px, py));

        // 2) 존 타이머 업데이트 & 세트 만료 여부
        float dt = Time.deltaTime;
        bool anyExpired = false;
        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            z.remain -= dt;
            if (z.remain <= 0f) anyExpired = true;
        }

        if (anyExpired)
        {
            // ★ 세트 종료: 남아있는 "모든" 존을 오염 처리 + 시각 삭제
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                OnZoneExpired?.Invoke(z.id);
                MarkContaminationCircle(z); // state 오염 + 보라 원 디스크
            }
            ResetWallHits();
            RegenerateAllZones(); // 이때만 3개 동시 재생성
            return;               // 이번 프레임 종료
        }

        // 3) 진행도 브로드캐스트 (만료 없을 때만)
        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            float p = 1f - Mathf.Clamp01(z.remain / Mathf.Max(0.0001f, zoneLifetime));
            OnZoneProgress?.Invoke(z.id, p);
        }

        // 4) 플레이어-존 상호작용
        if (onBoard)
        {
            Vector3 pWorld = player.position;

            // ConsumeZone에서 zones에서 제거될 수 있으니 역방향
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                var z = zones[i];

                // 원형 돔 내부 판정
                bool inside = PlayerInsideZoneWorld(z, pWorld);

                // 미충족 튕김 이후엔 한 번 밖으로 나가야 재진입 가능
                if (requireExitReenterAfterBounce && z.mustExitFirst && !inside)
                    z.mustExitFirst = false;

                if (!inside) continue;

                if (wallHits >= z.reqHits)
                {
                    // 잠금 시간/재진입 조건 검사
                    if (Time.time < z.consumeUnlockTime) continue;
                    if (requireExitReenterAfterBounce && z.mustExitFirst) continue;

                    // ★ 조건 충족: 즉시 소비(보너스만, 오염 X). 개별 재스폰 없음!
                    ConsumeZone(z);
                }
                else
                {
                    // ★ 조건 미달: 즉시 튕김 + 벽스택 증가 + 잠금/재진입 요구
                    if (!(lastBounceZoneId == z.id && Time.time - lastBounceTime < zoneBounceCooldown))
                    {
                        BounceFromZone(z);
                        AddWallHit(1);
                        lastBounceZoneId = z.id;
                        lastBounceTime   = Time.time;

                        z.consumeUnlockTime = Time.time + consumeLockAfterBounce;
                        z.mustExitFirst     = true;
                    }
                }
            }
        }
    }
    bool CirclesOverlapExisting(Vector2Int candCenter, Vector2Int candFootprint)
{
    // 후보 원 정보
    Vector3 cW = board.IndexToWorld(candCenter.x, candCenter.y);
    float   rC = (candFootprint.x * 0.5f) * board.tileSize;
    float   sepW = Mathf.Max(0f, minZoneSeparationTiles) * board.tileSize;

    // 이미 등록된 존들과 원형 겹침 검사
    for (int i = 0; i < zones.Count; i++)
    {
        var e = zones[i];
        // e.centerWorld / e.radiusWorld 는 SpawnAndNotify에서 세팅됨
        float sum = rC + e.radiusWorld + sepW;
        float dx = cW.x - e.centerWorld.x;
        float dz = cW.z - e.centerWorld.z;
        if ((dx*dx + dz*dz) < sum * sum) return true; // 겹침(또는 간격 미달)
    }
    return false;
}


    // ===== Zone 생성/검증 =====

    void RegenerateAllZones()
    {
        zones.Clear();
        OnZonesReset?.Invoke(); // 비주얼(돔/링) 정리
        ResetWallHits();
        var s = TrySpawnZone(ZoneKind.Small, smallSize, reqSmall, gainSmall);
        var m = TrySpawnZone(ZoneKind.Medium, mediumSize, reqMedium, gainMedium);
        var l = TrySpawnZone(ZoneKind.Large, largeSize, reqLarge, gainLarge);

        if (s != null) SpawnAndNotify(s);
        if (m != null) SpawnAndNotify(m);
        if (l != null) SpawnAndNotify(l);

        // 실패 보정
        while (zones.Count < 3)
        {
            var forced = ForceSpawnFallback();
            if (forced != null) SpawnAndNotify(forced); else break;
        }
    }

    void SpawnAndNotify(Zone z)
    {
        z.id = nextZoneId++;

        z.centerWorld = board.IndexToWorld(z.center.x, z.center.y);
        z.radiusWorld = (z.footprint.x * 0.5f) * board.tileSize;

        zones.Add(z);
        OnZoneSpawned?.Invoke(BuildSnapshot(z));
        z.consumeUnlockTime = 0f;
        z.mustExitFirst = false;
    }
    bool PlayerInsideZoneWorld(Zone z, Vector3 playerPos)
{
    var a = new Vector2(z.centerWorld.x, z.centerWorld.z);
    var b = new Vector2(playerPos.x,      playerPos.z);
    float tol = zoneTouchToleranceTiles * board.tileSize;
    return Vector2.SqrMagnitude(a - b) <= (z.radiusWorld + tol) * (z.radiusWorld + tol);
}

void ConsumeZone(Zone z)
{
    // 즉시 보너스 가산
    float bonus = z.kind switch {
        ZoneKind.Small  => enterBonusSmall,
        ZoneKind.Medium => enterBonusMedium,
        _               => enterBonusLarge
    };
    gauge?.Add(bonus);

    // 비주얼: 돔/링 제거 (오염 이벤트는 쏘지 않음)
    OnZoneConsumed?.Invoke(z.id);

    // 목록에서 제거
    zones.Remove(z);
}

    Zone TrySpawnZone(ZoneKind kind, Vector2Int size, int reqHits, float gain)
    {
        float alpha = baseAlpha;
        for (; alpha >= minAlpha - 1e-4f; alpha -= relaxStep)
        {
            var cand = FindValidPlacement(size, alpha);
            if (cand.HasValue)
            {
                var (c, tiles) = cand.Value;
                return new Zone
                {
                    kind = kind,
                    center = c,
                    tiles = tiles,
                    remain = zoneLifetime,
                    reqHits = reqHits,
                    gain = gain,
                    footprint = size
                };
            }
        }
        return null;
    }

(Vector2Int center, List<Vector2Int> tiles)? FindValidPlacement(Vector2Int size, float alpha)
{
    for (int tries = 0; tries < 200; tries++)
    {
        int cx = UnityEngine.Random.Range(0, board.width);
        int cy = UnityEngine.Random.Range(0, board.height);
        var c = new Vector2Int(cx, cy);

        // 경계 + 중심은 깨끗한 칸
        if (!board.InBounds(cx, cy)) continue;
        if (state[Idx(cx, cy)] == TileState.Contaminated) continue;

        // 플레이어에서 충분히 먼가?
        if (!PlayerFarEnough(c, size)) continue;

        // 블록 타일 집합
        var tiles = CollectBlock(c, size);
        if (tiles == null || tiles.Count == 0) continue;

        // (타일)겹침 금지
        if (zones.Any(z => Intersects(z.tiles, tiles))) continue;
          // (원형) 겹침 금지 - 시각/판정 일치
        if (CirclesOverlapExisting(c, size)) continue;

        // 유효면적
            int clean = tiles.Count(t => state[Idx(t.x, t.y)] == TileState.Clean);
        float ratio = (float)clean / tiles.Count;
        if (ratio >= alpha) return (c, tiles);
    }
    return null;
}


    Zone ForceSpawnFallback()
    {
        for (int y = 0; y < board.height; y++)
            for (int x = 0; x < board.width; x++)
            {
                if (state[Idx(x, y)] != TileState.Clean) continue;
                var c = new Vector2Int(x, y);
                if (PlayerNear(c, excludeRadius)) continue;

                var tiles = CollectBlock(c, smallSize);
                if (tiles == null) continue;
                if (CirclesOverlapExisting(c, smallSize)) continue; 
                if (zones.Any(z => Intersects(z.tiles, tiles))) continue;

                return new Zone
                {
                    id = 0,
                    kind = ZoneKind.Small,
                    center = c,
                    tiles = tiles,
                    remain = zoneLifetime,
                    reqHits = reqSmall,
                    gain = gainSmall,
                    footprint = smallSize
                };
            }
        return null;
    }

    List<Vector2Int> CollectBlock(Vector2Int center, Vector2Int size)
    {
        int hw = size.x / 2;
        int hh = size.y / 2;

        int minX = center.x - (size.x % 2 == 0 ? hw - 1 : hw);
        int minY = center.y - (size.y % 2 == 0 ? hh - 1 : hh);
        int maxX = minX + size.x - 1;
        int maxY = minY + size.y - 1;

        if (minX < 0 || minY < 0 || maxX >= board.width || maxY >= board.height) return null;

        var list = new List<Vector2Int>(size.x * size.y);
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                list.Add(new Vector2Int(x, y));
        return list;
    }

    bool PlayerNear(Vector2Int t, int r)
    {
        if (!board.WorldToIndex(player.position, out int px, out int py)) return false;
        return Mathf.Abs(t.x - px) <= r && Mathf.Abs(t.y - py) <= r;
    }

    static bool Intersects(List<Vector2Int> a, List<Vector2Int> b)
    {
        var set = new HashSet<Vector2Int>(a);
        foreach (var t in b) if (set.Contains(t)) return true;
        return false;
    }

    // === 반사(조건 미달 시 벽처럼 튕기기) ===
    void BounceFromZone(Zone z)
    {
        if (!playerRb) return;

        Vector3 zoneCenterW = board.IndexToWorld(z.center.x, z.center.y);
        Vector3 v = playerRb.linearVelocity; // ← 표준 속성
        if (v.sqrMagnitude < 0.0001f) v = (player.position - zoneCenterW).normalized * 2f;

        Vector3 n = (player.position - zoneCenterW).normalized;
        Vector3 r = Vector3.Reflect(v, n).normalized * Mathf.Min(v.magnitude, reflectClampSpeed);
        playerRb.linearVelocity = r; // ← 표준 속성
    }

    // === 외부 조회 ===
    public bool IsContaminated(int x, int y)
    {
        if (state == null || !board) return false;
        if (x < 0 || y < 0 || x >= board.width || y >= board.height) return false;
        int idx = y * board.width + x;
        if (idx < 0 || idx >= state.Length) return false;
        return state[idx] == TileState.Contaminated;
    }

    public IEnumerable<(ZoneKind kind, List<Vector2Int> tiles)> GetZones()
    {
        foreach (var z in zones) yield return (z.kind, z.tiles);
    }

    // === 원형 오염 ===
    IEnumerable<Vector2Int> CollectCircleTiles(Vector2Int center, float radiusTiles)
    {
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

    void MarkContaminationCircle(Zone z)
    {
        // 돔 밑면 반지름(타일 단위): footprint.x * 0.5  (2x2→1, 3x3→1.5, 4x4→2)
        float radiusTiles = z.footprint.x * 0.5f;

        foreach (var t in CollectCircleTiles(z.center, radiusTiles))
            state[Idx(t.x, t.y)] = TileState.Contaminated;

        // 비주얼(원형 디스크)
        Vector3 cW = board.IndexToWorld(z.center.x, z.center.y);
        float radiusWorld = radiusTiles * board.tileSize;
        OnZoneContaminatedCircle?.Invoke(z.id, cW, radiusWorld);
    }

    ZoneSnapshot BuildSnapshot(Zone z)
    {
        float radius = (z.footprint.x * 0.5f) * board.tileSize;
        return new ZoneSnapshot
        {
            id = z.id,
            kind = z.kind,
            centerWorld = board.IndexToWorld(z.center.x, z.center.y),
            baseRadius = radius,
            lifetime = zoneLifetime,
            remain = z.remain
        };
    }
    // 벽스택 증가/초기화 공개 메서드
    public void AddWallHit(int amount = 1)
    {
        wallHits = Mathf.Max(0, wallHits + amount);
        OnWallHitsChanged?.Invoke(wallHits);
    }
    public void ResetWallHits()
    {
        wallHits = 0;
        OnWallHitsChanged?.Invoke(wallHits);
    }
    bool TryGetPlayerTile(out int px, out int py)
    {
        if (!board) { px = py = 0; return false; }
        return board.WorldToIndex(player.position, out px, out py);
    }

    bool PlayerFarEnough(Vector2Int center, Vector2Int footprint)
    {
        if (!TryGetPlayerTile(out int px, out int py)) return true;
        float baseNeed = minSpawnTileDistance;
        if (padByHalfFootprint) baseNeed += Mathf.Max(footprint.x, footprint.y) * 0.5f;

        float dx = center.x - px;
        float dy = center.y - py;
        return (dx*dx + dy*dy) >= baseNeed * baseNeed; // √ 없이 거리 비교
    }

}
