using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// ===== 비주얼/외부에 전달할 스냅샷 =====
public struct ZoneSnapshot
{
    public int id;
    public int profileIndex;         // 어떤 프로필에서 왔는지
    public Vector3 centerWorld;
    public float baseRadius;         // 돔 밑면 반지름(월드 단위)
    public float lifetime;
    public float remain;
    public Material domeMat;         // 비주얼용 머티리얼 (없으면 VM에서 fallback)
    public Material ringMat;
}

// ===== 인스펙터에서 편집할 존 설정(프로필) =====
[System.Serializable]
public class ZoneProfile
{
    public string name = "LifeZone";
    [Tooltip("타일 발자국(지름을 타일 개수로). 2x2, 3x3, 4x4 등")]
    public Vector2Int footprint = new Vector2Int(3, 3);

    [Header("진입 요구(벽 튕김 스택)")]
    [Range(0, 5)] public int requiredWallHits = 1;

    [Header("게이지 이득/보너스")]
    [Tooltip("존 진입/소비 시 즉시 보너스")]
    public float enterBonus = 30f;
    [Tooltip("존 안에 있을 때 초당 회복량(옵션)")]
    public float gainPerSec = 0f;

    [Header("비주얼(선택)")]
    public Material domeMat;     // 반구 머티리얼
    public Material ringMat;     // 링 머티리얼
}

public class SurvivalDirector : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;
    public Transform player;               // 디스크 Transform
    public Rigidbody playerRb;             // 디스크 Rigidbody
    public SurvivalGauge gauge;

    [Header("Inspector-Driven Zones")]
    public List<ZoneProfile> zoneProfiles = new List<ZoneProfile>(); // ★ 인스펙터에서 관리: 항목 수 = 존 수

    [Header("Zone Lifetime")]
    [Tooltip("세트(현재 리스트 분량의 존 묶음)의 지속 시간. 시간이 끝나면 소비 안 된 존들을 오염 처리하고 세트 재생성")]
    public float zoneLifetime = 8f;

    [Header("Spawn Rules")]
    [Tooltip("서로 다른 존들 사이 최소 간격(타일 기준, 원 반지름 합 + 이 값 이상)")]
    public float minZoneSeparationTiles = 0.35f;
    [Tooltip("플레이어 주변 제외 반경(타일)")]
    public int excludeRadius = 1;
    [Tooltip("플레이어로부터 최소 스폰 거리(타일 중심-중심)")]
    public int minSpawnTileDistance = 6;
    [Tooltip("존 크기만큼 반경을 추가로 고려해 패딩할지")]
    public bool padByHalfFootprint = true;

    [Header("Clean Ratio 검사(유효면적 비율)")]
    public float baseAlpha = 0.50f;     // 50%
    public float minAlpha = 0.10f;     // 10%
    public float relaxStep = 0.05f;     // 5%p씩 완화

    [Header("Zone Entry 판정")]
    [Tooltip("플레이어-돔 접촉 판정 여유(타일 단위)")]
    public float zoneTouchToleranceTiles = 0.35f;

    [Header("조건 미달 시 벽처럼 튕기기")]
    public float reflectClampSpeed = 18f;
    public float zoneBounceCooldown = 0.08f;
    [Tooltip("미충족 튕김 직후 소비 금지 시간")]
    public float consumeLockAfterBounce = 0.15f;
    [Tooltip("미충족 후엔 한 번 존 밖으로 나갔다 재진입해야 소비 허용")]
    public bool requireExitReenterAfterBounce = true;
    public int ResetSeq { get; private set; } = 0;


    // ===== 이벤트 =====
    public event System.Action<Vector3, float> OnClearedCircleWorld;
    public event System.Action<int> OnZonesResetSeq;  // 리셋 순번 이벤트
    public event Action<ZoneSnapshot> OnZoneSpawned;
    public event Action<int> OnZoneExpired;
    public event Action OnZonesReset;
    public event Action<int, float> OnZoneProgress;                       // 0~1
    public event Action<int, Vector3, float> OnZoneContaminatedCircle;    // (id, centerW, radiusW)
    public event Action<int> OnZoneConsumed;                               // 성공 진입으로 소비
    public event Action<int> OnWallHitsChanged;                            // 벽 튕김 수 UI
    
    public bool HasState =>
    board != null &&
    state != null &&
    state.Length == board.width * board.height;


    // ===== 내부 상태 =====
    enum TileState { Clean, Contaminated }
    TileState[] state;

    class Zone
    {
        public int id;
        public int profileIndex;        // zoneProfiles의 인덱스
        public Vector2Int center;       // 중심 타일
        public List<Vector2Int> tiles;  // 사전 유효성 검사용 블록
        public float remain;            // (미사용) 존별 카운트가 필요하면 사용
        public int reqHits;          // 요구 튕김
        public float enterBonus;
        public float gainPerSec;
        public Vector2Int footprint;    // 지름 타일 수(2x2/3x3/4x4)
        public Vector3 centerWorld;
        public float radiusWorld;
        public Material domeMat;
        public Material ringMat;

        public float consumeUnlockTime = 0f;
        public bool mustExitFirst = false;
    }

    List<Zone> zones = new List<Zone>();
    int nextZoneId = 1;
    int wallHits = 0;

    float setRemain; // 세트 남은 시간(전체 존 묶음의 타이머)
    public float SetRemain => Mathf.Max(0f, setRemain);
    public float SetDuration => Mathf.Max(0.0001f, zoneLifetime);
    public float SetProgress01 => 1f - Mathf.Clamp01(setRemain / Mathf.Max(0.0001f, zoneLifetime));

    // 쿨다운 관리
    int lastBounceZoneId = -1;
    float lastBounceZoneTime = -999f;

    // ===== 편의 Getter =====
    public int CurrentWallHits => wallHits;
    public int Width => board ? board.width : 0;
    public int Height => board ? board.height : 0;
    int Idx(int x, int y) => y * board.width + x;

    void Awake()
    {
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!playerRb && player) playerRb = player.GetComponent<Rigidbody>();

        state = new TileState[board.width * board.height];
        for (int i = 0; i < state.Length; i++) state[i] = TileState.Clean;
    }

    void Start()
    {
        RegenerateAllZones(); // ★ Awake에서가 아니라 Start에서 호출(이벤트 구독 이후 보장)
    }

    void Update()
    {
        if (!board || !player) return;
        float dt = Time.deltaTime;

        // 플레이어 위치/오염 HUD 업데이트
        if (board.WorldToIndex(player.position, out int px, out int py))
            gauge?.SetContaminated(IsContaminated(px, py));

        // 세트 타이머
        setRemain -= dt;

        // 진행도 브로드캐스트(모든 존 동일 진행도 사용)
        float setProgress = 1f - Mathf.Clamp01(setRemain / Mathf.Max(0.0001f, zoneLifetime));
        for (int i = 0; i < zones.Count; i++)
            OnZoneProgress?.Invoke(zones[i].id, setProgress);

        // 세트 종료 → 미소비 존 전부 오염 디스크 생성 후 세트 재생성
        if (setRemain <= 0f)
        {
            for (int i = 0; i < zones.Count; i++)
                MarkContaminationCircle(zones[i]);

            ResetWallHits();
            RegenerateAllZones();
            return;
        }

        // 플레이어-존 상호작용
        var pWorld = player.position;
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            var z = zones[i];
            bool inside = PlayerInsideZoneWorld(z, pWorld);

            // 미충족으로 튕겼다면, 한 번 존 밖으로 나가야 재진입 허용
            if (requireExitReenterAfterBounce && z.mustExitFirst && !inside)
                z.mustExitFirst = false;

            if (!inside) continue;

            if (wallHits >= z.reqHits)
            {
                // 잠금/재진입 검사
                if (Time.time < z.consumeUnlockTime) continue;
                if (requireExitReenterAfterBounce && z.mustExitFirst) continue;

                // 소비 성공
                ConsumeZone(z);
            }
            else
            {
                // 조건 미달 → 벽처럼 튕김(쿨다운 포함) + 스택 증가
                if (!(lastBounceZoneId == z.id && Time.time - lastBounceZoneTime < zoneBounceCooldown))
                {
                    BounceFromZone(z);
                    AddWallHit(1);
                    lastBounceZoneId = z.id;
                    lastBounceZoneTime = Time.time;

                    z.consumeUnlockTime = Time.time + consumeLockAfterBounce;
                    z.mustExitFirst = true;
                }
            }
        }

        // (옵션) 존 내부 체류 회복이 필요하면 여기에서 gainPerSec 적용
        // foreach (var z in zones) if (PlayerInsideZoneWorld(z, pWorld)) gauge?.Add(z.gainPerSec * dt);
    }

    // ===== 존 재생성(리스트 기반) =====
    void RegenerateAllZones()
    {

        ResetSeq++;
        OnZonesResetSeq?.Invoke(ResetSeq);
        zones.Clear();
        OnZonesReset?.Invoke();
        ResetWallHits();

        setRemain = zoneLifetime;

        // zoneProfiles의 "항목 개수 = 생성 개수"로 1:1 생성
        for (int i = 0; i < zoneProfiles.Count; i++)
        {
            var z = TrySpawnZoneByProfile(i);
            if (z != null) SpawnAndNotify(z);
        }

        // 안전망: 실패가 많아 개수가 모자라면 아무 프로필 랜덤으로 다시 시도
        int guard = 200;
        while (zones.Count < zoneProfiles.Count && guard-- > 0)
        {
            int pick = UnityEngine.Random.Range(0, zoneProfiles.Count);
            var extra = TrySpawnZoneByProfile(pick);
            if (extra != null) SpawnAndNotify(extra); else break;
        }

    }

    Zone TrySpawnZoneByProfile(int profileIndex)
    {
        if (profileIndex < 0 || profileIndex >= zoneProfiles.Count) return null;
        var p = zoneProfiles[profileIndex];

        float alpha = baseAlpha;
        for (; alpha >= minAlpha - 1e-4f; alpha -= relaxStep)
        {
            var cand = FindValidPlacement(p.footprint, alpha);
            if (cand.HasValue)
            {
                var (c, tiles) = cand.Value;
                return new Zone
                {
                    profileIndex = profileIndex,
                    center = c,
                    tiles = tiles,
                    remain = zoneLifetime,
                    reqHits = Mathf.Max(0, p.requiredWallHits),
                    enterBonus = Mathf.Max(0, p.enterBonus),
                    gainPerSec = Mathf.Max(0, p.gainPerSec),
                    footprint = p.footprint,
                    domeMat = p.domeMat,
                    ringMat = p.ringMat
                };
            }
        }
        return null;
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

    ZoneSnapshot BuildSnapshot(Zone z)
    {
        return new ZoneSnapshot
        {
            id = z.id,
            profileIndex = z.profileIndex,
            centerWorld = z.centerWorld,
            baseRadius = z.radiusWorld,
            lifetime = zoneLifetime,
            remain = z.remain,
            domeMat = z.domeMat,
            ringMat = z.ringMat
        };
    }

    // ===== 배치/검증 유틸 =====
    (Vector2Int center, List<Vector2Int> tiles)? FindValidPlacement(Vector2Int footprint, float alpha)
    {
        for (int tries = 0; tries < 200; tries++)
        {
            int cx = UnityEngine.Random.Range(0, board.width);
            int cy = UnityEngine.Random.Range(0, board.height);
            var c = new Vector2Int(cx, cy);

            if (!board.InBounds(cx, cy)) continue;
            if (state[Idx(cx, cy)] == TileState.Contaminated) continue;

            // 플레이어 근처 제외 + 최소 거리
            if (!PlayerFarEnough(c, footprint)) continue;

            // 블록 타일 모음
            var tiles = CollectBlock(c, footprint);
            if (tiles == null || tiles.Count == 0) continue;

            // (타일) 겹침 금지
            if (zones.Any(z => Intersects(z.tiles, tiles))) continue;
            // (원형) 겹침/간격 검사
            if (CirclesOverlapExisting(c, footprint)) continue;

            // 유효면적 검사
            int clean = tiles.Count(t => state[Idx(t.x, t.y)] == TileState.Clean);
            float ratio = (float)clean / tiles.Count;
            if (ratio >= alpha) return (c, tiles);
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

    bool CirclesOverlapExisting(Vector2Int candCenter, Vector2Int candFootprint)
    {
        Vector3 cW = board.IndexToWorld(candCenter.x, candCenter.y);
        float rC = (candFootprint.x * 0.5f) * board.tileSize;
        float sepW = Mathf.Max(0f, minZoneSeparationTiles) * board.tileSize;

        for (int i = 0; i < zones.Count; i++)
        {
            var e = zones[i];
            float sum = rC + e.radiusWorld + sepW;
            float dx = cW.x - e.centerWorld.x;
            float dz = cW.z - e.centerWorld.z;
            if ((dx * dx + dz * dz) < sum * sum) return true; // 겹침/간격 미달
        }
        return false;
    }

    bool PlayerFarEnough(Vector2Int center, Vector2Int footprint)
    {
        if (!board.WorldToIndex(player.position, out int px, out int py)) return true;

        float need = minSpawnTileDistance;
        if (padByHalfFootprint) need += Mathf.Max(footprint.x, footprint.y) * 0.5f;

        float dx = center.x - px;
        float dy = center.y - py;
        return (dx * dx + dy * dy) >= need * need;
    }

    // ===== 상호작용/소비/튕김 =====
    bool PlayerInsideZoneWorld(Zone z, Vector3 playerPos)
    {
        var a = new Vector2(z.centerWorld.x, z.centerWorld.z);
        var b = new Vector2(playerPos.x, playerPos.z);
        float tol = zoneTouchToleranceTiles * board.tileSize;
        return Vector2.SqrMagnitude(a - b) <= (z.radiusWorld + tol) * (z.radiusWorld + tol);
    }

    void ConsumeZone(Zone z)
    {
        if (z.enterBonus > 0f) gauge?.Add(z.enterBonus);

        OnZoneConsumed?.Invoke(z.id);
        zones.Remove(z);

        // 소비해도 "세트"는 계속 유효. 개별 재스폰 없이 진행 → 세트 타이머가 끝나면 재생성
        // (원한다면 여기서 즉시 새 존을 같은 프로필로 다시 뽑아 넣을 수도 있음)
    }

    void BounceFromZone(Zone z)
    {
        if (!playerRb) return;

        Vector3 zoneCenterW = z.centerWorld;
        Vector3 v = playerRb.linearVelocity;
        if (v.sqrMagnitude < 0.0001f) v = (player.position - zoneCenterW).normalized * 2f;

        Vector3 n = (player.position - zoneCenterW).normalized;
        Vector3 r = Vector3.Reflect(v, n).normalized * Mathf.Min(v.magnitude, reflectClampSpeed);
        playerRb.linearVelocity = r;
    }

    // ===== 오염 처리 & 청소 유틸 =====
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
        float radiusTiles = z.footprint.x * 0.5f;

        foreach (var t in CollectCircleTiles(z.center, radiusTiles))
            state[Idx(t.x, t.y)] = TileState.Contaminated;

        Vector3 cW = z.centerWorld;
        float rWorld = radiusTiles * board.tileSize;
        OnZoneContaminatedCircle?.Invoke(z.id, cW, rWorld);
        OnZoneExpired?.Invoke(z.id);
    }

    public void ClearContamination(int x, int y)
    {
        if (!board || x < 0 || y < 0 || x >= board.width || y >= board.height) return;
        int idx = Idx(x, y);
        if (state[idx] == TileState.Contaminated)
            state[idx] = TileState.Clean;
        // (원한다면 여기서 오염 비주얼을 즉시 업데이트하는 이벤트도 추가 가능)
    }

    // ===== 벽 튕김 카운트 (카드 충전 등에서 구독) =====
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

    // 외부 조회
    public bool IsContaminated(int x, int y)
    {
        if (board == null || state == null) return false;
        if (x < 0 || y < 0 || x >= board.width || y >= board.height) return false;

        int idx = y * board.width + x;
        if (idx < 0 || idx >= state.Length) return false; // 🔒 추가 가드

        return state[idx] == TileState.Contaminated;
    }



    static bool Intersects(List<Vector2Int> a, List<Vector2Int> b)
    {
        var set = new HashSet<Vector2Int>(a);
        foreach (var t in b) if (set.Contains(t)) return true;
        return false;
    }
    // === 외부에서 월드 좌표/반경으로 오염 지대 생성 ===
    // === 외부에서 '월드 좌표 + 월드 반경'으로 원형 오염 생성 ===
    public void ContaminateCircleWorld(Vector3 centerWorld, float radiusWorld)
    {
        if (!board) return;
        if (!board.WorldToIndex(centerWorld, out int cx, out int cy)) return;

        float radiusTiles = radiusWorld / Mathf.Max(0.0001f, board.tileSize);

        foreach (var t in CollectCircleTiles(new Vector2Int(cx, cy), radiusTiles))
            state[Idx(t.x, t.y)] = TileState.Contaminated;

        OnZoneContaminatedCircle?.Invoke(-999, centerWorld, radiusWorld);
    }

    public void ClearCircleWorld(Vector3 centerWorld, float radiusWorld)
    {
        if (!board) return;
        if (!board.WorldToIndex(centerWorld, out int cx, out int cy)) return;

        float rTiles = radiusWorld / Mathf.Max(0.0001f, board.tileSize);

        foreach (var t in CollectCircleTiles(new Vector2Int(cx, cy), rTiles))
            ClearContamination(t.x, t.y); // ← 기존 타일 청소 유틸 사용 :contentReference[oaicite:5]{index=5}

        OnClearedCircleWorld?.Invoke(centerWorld, radiusWorld);
    }

    // Instancing 렌더러에서 쓰기 위한 래퍼(내부 이터레이터 노출)
    public IEnumerable<Vector2Int> CollectCircleTilesPublic(Vector2Int center, float radiusTiles)
    {
        return CollectCircleTiles(center, radiusTiles); // 기존 구현 재사용 :contentReference[oaicite:6]{index=6}
    }


}
