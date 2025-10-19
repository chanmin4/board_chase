// === 이 파일은 원문 그대로이며, 추가/수정 지점만 주석으로 안내합니다 ===

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
    public float time_to_live;           // ★ TODO: (변경) 개별 TTL로 교체. 지금은 set 기반 값(zoneLifetime) 사용 중
    public float remain;             // ★ TODO: (변경) 개별 TTL 남은 시간으로 업데이트
    public Material domeMat;         // 비주얼용 머티리얼 (없으면 VM에서 fallback)
    public Material ringMat;
}

public enum ZoneSize { Small, Medium, Large } 

// ===== 인스펙터에서 편집할 존 설정(프로필) =====
[System.Serializable]
public class ZoneProfile
{
    public string name = "LifeZone";
    public Vector2Int footprint = new Vector2Int(3, 3);

    [Header("Size")]                
    public ZoneSize size = ZoneSize.Small; 

    [Header("진입 요구(벽 튕김 스택)")]
    public int requiredZoneHits = 1;
    [Header("게이지 이득/보너스")]
    public float enterBonus = 30f;
    public float gainPerSec = 0f;
    [Header("존 소모시 xp획득량")]
    public float xp;

    [Header("비주얼(선택)")]
    public Material domeMat;
    public Material ringMat;
    [Header("Lifetime (per-zone)")]
    public float time_to_live_profile = 15f;
}

public class SurvivalDirector : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;
    public Transform player;
    public Rigidbody playerRb;
    public SurvivalGauge gauge;
    public DragAimController dragaimcontroller;
    public BoardMaskRenderer maskRenderer;
    public DiskInkLeveler diskleveler;

    [Header("Inspector-Driven Zones")]
    public List<ZoneProfile> zoneProfiles = new List<ZoneProfile>(); // ★ 인스펙터에서 관리

    //[Header("Zone Lifetime")]
    //public float zoneLifetime = 8f; // ★ TODO: (변경) 세트 타이머가 아니라 "기본 TTL"로만 사용하거나, 프로필 lifeTimeRange 없을 때의 default로 사용

    [Header("Spawn Rules")]
    public float minZoneSeparationTiles = 0.35f;
    public int excludeRadius = 1;
    public int minSpawnTileDistance = 6;
    public bool padByHalfFootprint = true;

    [Header("Clean Ratio 검사(유효면적 비율)")]
    public float baseAlpha = 0.50f;
    public float minAlpha = 0.10f;
    public float relaxStep = 0.05f;

    [Header("Zone Entry 판정")]
    public float zoneTouchToleranceTiles = 0.35f;
    [Header("Bonus Sector (directional hit)")]
    public bool enableBonusSector = true;
    [Range(1f, 360f)] public float bonusArcDeg = 10f; // 섹터 각도(전체)
    public int normalHitAward = 1;                 // 일반 접촉 시 +1
    public int bonusHitAward = 2;                 // 보너스 접촉 시 +2
    public float bonusRefreshDelay = 0.05f;           // 보너스 히트 후 재배치 지연(초)

    [Header("Zone Bounce Tuning")]
    public float zoneRestitution = 0.98f;
    public float reflectClampSpeed = 0f;
    public float zoneBounceCooldown = 0.08f;
    public float consumeLockAfterBounce = 0.15f;
    public bool requireExitReenterAfterBounce = true;
    [Tooltip("미충족 후엔 한 번 존 밖으로 나갔다 재진입해야 소비 허용")]
    public int ResetSeq { get; private set; } = 0;

    [Header("Risk Tuning")]
    public float zoneEnterBonusMul = 1f;

    [Header("Risk Tuning - Per Size")]
    public int zoneReqHitsAdd_S = 0;
    public int zoneReqHitsAdd_M = 0;
    public int zoneReqHitsAdd_L = 0;

    [Header("Layout (Counts per Set)")]          // ★
    public bool useLayoutCounts = true;
    [Min(0)] public int layoutCountSmall = 0;
    [Min(0)] public int layoutCountMedium = 0;
    [Min(0)] public int layoutCountLarge = 0;

    // 보너스 아크 리롤 코루틴(존별)
    readonly Dictionary<int, Coroutine> _bonusReroll = new Dictionary<int, Coroutine>();



    // ===== 이벤트 =====
    public event System.Action<Vector3, float> OnClearedCircleWorld;
    public event System.Action<int> OnZonesResetSeq;
    public event Action<ZoneSnapshot> OnZoneSpawned;
    public event Action<int> OnZoneExpired;
    public event Action OnZonesReset;
    public event Action<int, float> OnZoneProgress;                    // 0~1
    public event Action<int, Vector3, float> OnZoneContaminatedCircle;//오염원생성
    public event System.Action<Vector3, int, int> OnEnterContam; // (worldPos, ix, iy)
    public event System.Action<Vector3, int, int> OnExitContam;  // (worldPos, ix, iy)
    public event Action<int> OnZoneConsumed;
    // public event Action<int> OnZoneHitsChanged;
    public event System.Action<int, int, int, bool> OnZoneHit;
    public event System.Action<int, float, float> OnZoneBonusSectorChanged;
    public event System.Action<Vector3, float, bool> OnPlayerPaintCircleWorld;
    public event System.Action ContamSpawn;
    public event System.Action ZoneNormalHit_SFX;
    public event System.Action ZoneCritHit_SFX;

    public bool HasState =>
    board != null &&
    state != null &&
    state.Length == board.width * board.height;

    // 사이즈별 프로필을 “순서대로” 뽑아오는 헬퍼
    System.Collections.Generic.List<int> TakeProfileIndicesBySize(ZoneSize s, int count)
    {
        var idxs = new System.Collections.Generic.List<int>();
        if (zoneProfiles == null || count <= 0) return idxs;

        var pool = new System.Collections.Generic.List<int>();
        for (int i = 0; i < zoneProfiles.Count; i++)
            if (zoneProfiles[i] != null && zoneProfiles[i].size == s)
                pool.Add(i);

        if (pool.Count == 0) return idxs;

        int k = 0;
        while (idxs.Count < count)
        {
            idxs.Add(pool[k]);
            k = (k + 1) % pool.Count;
        }
        return idxs;
    }

    // ===== 내부 상태 =====
    enum TileState { Clean, Contaminated }
    TileState[] state;

    class Zone
    {
        public int id;
        public int profileIndex;
        public Vector2Int center;
        public List<Vector2Int> tiles;
        public float xp;
        public float remaintime;
        public float time_to_live;
        public int curhit;                 // ★ 누적 히트(존별)
        public int reqHit;             // 요구 튕김(종류별 고정)
        public float enterBonus;
        public float gainPerSec;
        public Vector2Int footprint;
        public Vector3 centerWorld;
        public float radiusWorld;
        public Material domeMat;
        public Material ringMat;

        public float consumeUnlockTime = 0f;
        public bool mustExitFirst = false;

        public float bonusAngleDeg;          // 0~360, 존 중심에서 바라보는 방향
        public float bonusNextRefreshAt;     // >0이면 해당 시각에 각도 리롤
        public int RemainingHit => Mathf.Max(0, reqHit - curhit);
    }

    List<Zone> zones = new List<Zone>();
    int nextZoneId = 1;
    int lastBounceZoneId = -1;
    float lastBounceZoneTime = -999f;
    bool _prevInContam = false;
    // ===== 편의 Getter =====
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
        RegenerateAllZones(); // ★ 유지: 초기 스폰
    }

    void Update()
    {
        if (!board || !player) return;
        float dt = Time.deltaTime;
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
        for (int i = zones.Count - 1; i >= 0; --i)
        {
            var z = zones[i];
            z.remaintime -= dt;
            float time_decrease_ratio = 1f - Mathf.Clamp01(z.remaintime / Mathf.Max(0.0001f, z.time_to_live));
            OnZoneProgress?.Invoke(z.id, time_decrease_ratio); // ← 링/타이머 UI는 이 값으로
            if (z.remaintime <= 0f)
            {
                MarkContaminationCircle(z);
                zones.RemoveAt(i);
                StartCoroutine(RespawnAfterDelay(z.profileIndex, 1.0f));
                continue;
            }
        }

        var pWorld = player.position;
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            var z = zones[i];
            bool inside = PlayerInsideZoneWorld(z, pWorld);

            if (requireExitReenterAfterBounce && z.mustExitFirst && !inside)
                z.mustExitFirst = false;

            if (!inside) continue;

            // 이미 요구치 충족 상태면 즉시 소비 허용(락/재진입 조건은 성공 시 무시)
            if (z.curhit >= z.reqHit)
            {
                ConsumeZone(z);
                StartCoroutine(RespawnAfterDelay(z.profileIndex, 1.0f));
                continue;
            }

            // 보너스 섹터 판정
            int award = normalHitAward;
            bool isBonus = false;
            if (enableBonusSector)
            {
                float ang = BearingDeg(z.centerWorld, pWorld);
                float half = bonusArcDeg * 0.5f;
                // z.bonusAngleDeg는 스폰시에 랜덤 세팅되어 있음
                if (AngleDeltaDeg(ang, z.bonusAngleDeg) <= half)
                {
                    award = bonusHitAward;
                    isBonus = true;
                }
            }

            // 이번 접촉으로 요구치가 충족되는가?
            int nextHits = z.curhit + Mathf.Max(1, award);

            // 소비 락/재진입 규칙: "미충족으로 튕긴 뒤"에만 적용
            bool canConsume = (Time.time >= z.consumeUnlockTime) && (!z.mustExitFirst);

            if (nextHits >= z.reqHit && canConsume)
            {
                // 바로 소비(튕기지 않음)
                z.curhit = z.reqHit;
                OnZoneHit?.Invoke(z.id, z.curhit, z.reqHit, isBonus); // (선택) 크랙 연출 트리거
                ConsumeZone(z);
                StartCoroutine(RespawnAfterDelay(z.profileIndex, 1.0f));
            }
            else
            {
                // 조건 미달 → 튕기고 히트 누적
                if (!(lastBounceZoneId == z.id && Time.time - lastBounceZoneTime < zoneBounceCooldown))
                {
                    BounceFromZone(z);
                    if (isBonus)
                    {
                        // 기존 예약이 있으면 취소
                        if (_bonusReroll.TryGetValue(z.id, out var co)) { StopCoroutine(co); _bonusReroll.Remove(z.id); }
                        _bonusReroll[z.id] = StartCoroutine(RerollBonusSectorAfter(z.id, bonusRefreshDelay));
                    }
                    z.curhit = nextHits; // ★ 존별 카운트 증가
                    OnZoneHit?.Invoke(z.id, z.curhit, z.reqHit, isBonus); // (선택) 크랙 연출

                    // 보너스면 0.2s 뒤 섹터 리롤
                    if (isBonus)
                    {
                        z.bonusNextRefreshAt = Time.time + bonusRefreshDelay;
                        ZoneCritHit_SFX?.Invoke();
                    }
                    else
                    {
                        ZoneNormalHit_SFX?.Invoke();
                    }

                    lastBounceZoneId = z.id;
                    lastBounceZoneTime = Time.time;

                    z.consumeUnlockTime = Time.time + consumeLockAfterBounce;
                    z.mustExitFirst = true;
                }
            }
        }


    }
    System.Collections.IEnumerator RerollBonusSectorAfter(int zoneId, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 아직 살아있는 같은 id의 존만 갱신
        var z = zones.Find(zz => zz.id == zoneId);
        if (z != null)
        {
            z.bonusAngleDeg = UnityEngine.Random.Range(0f, 360f);
            OnZoneBonusSectorChanged?.Invoke(zoneId, z.bonusAngleDeg, bonusArcDeg);
        }
        _bonusReroll.Remove(zoneId);
    }
    System.Collections.IEnumerator RespawnAfterDelay(int profileIndex, float delaySec)
    {
        yield return new WaitForSeconds(delaySec);
        var nz = TrySpawnZoneByProfile(profileIndex);
        if (nz != null) SpawnAndNotify(nz);
    }


    // ===== 존 재생성(리스트 기반) =====
    void RegenerateAllZones()
    {
        ResetSeq++;
        OnZonesResetSeq?.Invoke(ResetSeq);
        zones.Clear();
        OnZonesReset?.Invoke();
        if (useLayoutCounts)
        {
            void SpawnByCount(ZoneSize s, int cnt)
            {
                var idxs = TakeProfileIndicesBySize(s, cnt);
                for (int i = 0; i < idxs.Count; i++)
                {
                    var z = TrySpawnZoneByProfile(idxs[i]);
                    if (z != null) SpawnAndNotify(z);
                }
            }

            SpawnByCount(ZoneSize.Small, layoutCountSmall);
            SpawnByCount(ZoneSize.Medium, layoutCountMedium);
            SpawnByCount(ZoneSize.Large, layoutCountLarge);
        }
        else
        {
            for (int i = 0; i < zoneProfiles.Count; i++)
            {
                var z = TrySpawnZoneByProfile(i);
                if (z != null) SpawnAndNotify(z);
            }
        }

        int expectedCount = useLayoutCounts
            ? (layoutCountSmall + layoutCountMedium + layoutCountLarge)
            : zoneProfiles.Count;

        int guard = 200;
        while (zones.Count < expectedCount && guard-- > 0)
        {
            int pick = UnityEngine.Random.Range(0, zoneProfiles.Count);
            var extra = TrySpawnZoneByProfile(pick);
            if (extra != null) SpawnAndNotify(extra);
            else break;
        }

        // ★ 참고: 이후부터는 "개별 TTL 루프"가 존을 만료/보충하므로,
        //         여기서는 "초기 5개 채우기" 역할만 담당.
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
                    xp = p.xp,
                    remaintime = p.time_to_live_profile, // ★ TODO: (변경) 위 ttl 변수로 교체 (개별 TTL)
                    time_to_live = p.time_to_live_profile,
                    curhit = 0,
                    reqHit = GetEffectiveRequiredHits(p),
                    enterBonus = Mathf.Max(0, p.enterBonus),
                    gainPerSec = Mathf.Max(0, p.gainPerSec),
                    footprint = p.footprint,
                    domeMat = p.domeMat,
                    ringMat = p.ringMat,
                    bonusAngleDeg = UnityEngine.Random.Range(0f, 360f),
                    bonusNextRefreshAt = -1f,
                };

            }
        }
        return null;
    }

    // ★ 존 중심에서 플레이어를 본 방위각(도) 구하기
    float BearingDeg(Vector3 from, Vector3 to)
    {
        Vector2 a = new Vector2(from.x, from.z);
        Vector2 b = new Vector2(to.x, to.z);
        Vector2 d = (b - a).normalized;
        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; // x→0°, 반시계+
        if (ang < 0f) ang += 360f;
        return ang;
    }


    // ★ angA와 angB의 절대 각도차(도), 0~180
    float AngleDeltaDeg(float a, float b)
    {
        float d = Mathf.Abs(a - b) % 360f;
        return d > 180f ? 360f - d : d;
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
        if (enableBonusSector)
            OnZoneBonusSectorChanged?.Invoke(z.id, z.bonusAngleDeg, bonusArcDeg);

    }

    ZoneSnapshot BuildSnapshot(Zone z)
    {
        return new ZoneSnapshot
        {
            id = z.id,
            profileIndex = z.profileIndex,
            centerWorld = z.centerWorld,
            baseRadius = z.radiusWorld,
            time_to_live = z.time_to_live,
            remain = z.remaintime,
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

            if (!PlayerFarEnough(c, footprint)) continue;

            var tiles = CollectBlock(c, footprint);
            if (tiles == null || tiles.Count == 0) continue;

            if (zones.Any(z => Intersects(z.tiles, tiles))) continue;
            if (CirclesOverlapExisting(c, footprint)) continue;

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
            if ((dx * dx + dz * dz) < sum * sum) return true;
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
    public void PaintPlayerCircleWorld(Vector3 centerWorld, float radiusWorld,
                                   bool applyBoardClean, bool clearPollutionMask)
    {
        // --- 보드 상태(점유율) ---
        if (applyBoardClean)
        {
            // 기존 파이프라인 재사용: 내부에서 타일/카운트/이벤트 처리
            ClearCircleWorld(centerWorld, radiusWorld);
        }

        // --- 플레이어 페인트 비주얼(별도 레이어에 칠하기) ---
        OnPlayerPaintCircleWorld?.Invoke(centerWorld, radiusWorld, clearPollutionMask);

        // --- 오염 비주얼 덮어쓰기(렌더 마스크 0으로) ---
        if (clearPollutionMask)
        {
            // 기존 렌더 파이프 유지: 오염 마스크 지우는 이벤트
            OnClearedCircleWorld?.Invoke(centerWorld, radiusWorld);
        }
    }


    void ConsumeZone(Zone z)
    {
        if (z.enterBonus > 0f) gauge?.Add(z.enterBonus * Mathf.Max(0f, zoneEnterBonusMul));

        // ★ 추가: 성공 소비 시 해당 존 영역을 플레이어 색으로 덮고, 오염 마스크는 0으로 지움
        PaintPlayerCircleWorld(z.centerWorld, z.radiusWorld,
                               applyBoardClean: false,
                               clearPollutionMask: true);

        OnZoneConsumed?.Invoke(z.id);
        diskleveler.GrantXP(50f, "zone");
        if (_bonusReroll.TryGetValue(z.id, out var co)) { StopCoroutine(co); _bonusReroll.Remove(z.id); }
        zones.Remove(z);

        // (필요시) 보충 스폰은 기존 주석대로 사용
        // StartCoroutine(RespawnAfterDelay(z.profileIndex, 1.0f));
    }

    void BounceFromZone(Zone z)
    {
        if (!playerRb) return;

        Vector3 zoneCenterW = z.centerWorld;
        Vector3 v = playerRb.linearVelocity;
        if (v.sqrMagnitude < 0.0001f)
            v = (player.position - zoneCenterW).normalized * 2f;

        Vector3 n = (player.position - zoneCenterW).normalized;
        Vector3 rDir = Vector3.Reflect(v, n).normalized;

        float speed = v.magnitude * zoneRestitution;
        if (reflectClampSpeed > 0f)
            speed = Mathf.Min(speed, reflectClampSpeed);

        playerRb.linearVelocity = rDir * speed;
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


    public void ClearContamination(int x, int y)
    {
        if (!board || x < 0 || y < 0 || x >= board.width || y >= board.height) return;
        int idx = Idx(x, y);
        if (state[idx] == TileState.Contaminated)
            state[idx] = TileState.Clean;
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
    public void ContaminateCircleWorld(Vector3 centerWorld, float radiusWorld)
    {
        if (!board) return;
        if (!board.WorldToIndex(centerWorld, out int cx, out int cy)) return;

        float radiusTiles = radiusWorld / Mathf.Max(0.0001f, board.tileSize);

        foreach (var t in CollectCircleTiles(new Vector2Int(cx, cy), radiusTiles))
            state[Idx(t.x, t.y)] = TileState.Contaminated;

        OnZoneContaminatedCircle?.Invoke(-999, centerWorld, radiusWorld);
        ContamSpawn?.Invoke();
    }

    public void ClearCircleWorld(Vector3 centerWorld, float radiusWorld)
    {
        if (!board) return;
        if (!board.WorldToIndex(centerWorld, out int cx, out int cy)) return;

        float rTiles = radiusWorld / Mathf.Max(0.0001f, board.tileSize);

        foreach (var t in CollectCircleTiles(new Vector2Int(cx, cy), rTiles))
            ClearContamination(t.x, t.y);

        OnClearedCircleWorld?.Invoke(centerWorld, radiusWorld);
    }

    public IEnumerable<Vector2Int> CollectCircleTilesPublic(Vector2Int center, float radiusTiles)
    {
        return CollectCircleTiles(center, radiusTiles);
    }

    int GetReqAddBySize(ZoneSize s)
    {
        switch (s)
        {
            case ZoneSize.Small: return zoneReqHitsAdd_S;
            case ZoneSize.Medium: return zoneReqHitsAdd_M;
            case ZoneSize.Large: return zoneReqHitsAdd_L;
            default: return 0;
        }
    }

    public int GetEffectiveRequiredHits(ZoneProfile p)
    {
        return Mathf.Max(0, p.requiredZoneHits + GetReqAddBySize(p.size));
    }



    void MarkContaminationCircle(Zone z)
    {
        float radiusTiles = z.footprint.x * 0.5f;

        foreach (var t in CollectCircleTiles(z.center, radiusTiles))
            state[Idx(t.x, t.y)] = TileState.Contaminated;

        Vector3 cW = z.centerWorld;
        float rWorld = radiusTiles * board.tileSize;
        if (_bonusReroll.TryGetValue(z.id, out var co)) { StopCoroutine(co); _bonusReroll.Remove(z.id); }
        OnZoneContaminatedCircle?.Invoke(z.id, cW, rWorld);
        OnZoneExpired?.Invoke(z.id);
    }
    public int maskRendererPlayerPixelsPerTile()
    {
        if (!maskRenderer)
            maskRenderer = FindAnyObjectByType<BoardMaskRenderer>(); // PaintMaskRenderer 쓰면 타입 교체

        // 위에서 1)에서 만든 게터를 사용
        return maskRenderer ? Mathf.Max(1, maskRenderer.PlayerPixelsPerTile) : 15;
    }

    // 모든 활성 존의 보너스 섹터 각도를 'arcDeg'로 즉시 갱신(새로 생성될 존도 이 값 사용)
    public void SetBonusArcForAll(float arcDeg)
    {
        bonusArcDeg = Mathf.Clamp(arcDeg, 1f, 360f);
         for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            OnZoneBonusSectorChanged?.Invoke(z.id, z.bonusAngleDeg, bonusArcDeg);
        }
    }

}

