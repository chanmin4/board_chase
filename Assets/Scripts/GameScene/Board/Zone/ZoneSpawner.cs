using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
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

[DisallowMultipleComponent]
public class ZoneSpawner : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;
    public Transform player;
    public Rigidbody playerRb;
    public SurvivalGauge gauge;
    public BoardMaskRenderer maskRenderer;
    public DiskInkLeveler diskleveler;


    [Header("Inspector-Driven Zones")]
    public List<ZoneProfile> zoneProfiles = new List<ZoneProfile>(); // ★ 인스펙터에서 관리
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
    [Range(1f, 360f)] public float bonusArcDeg = 60f; // 섹터 각도(전체)
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
    [Header("Layout (Counts per Set)")]          // ★
    public bool useLayoutCounts = true;
    [Min(0)] public int layoutCountSmall = 0;
    [Min(0)] public int layoutCountMedium = 0;
    [Min(0)] public int layoutCountLarge = 0;
    [System.Serializable]
    public struct SpawnBlockRule
    {
        public string label;
        public LayerMask layers;                       // 여러 레이어 체크 가능
        [Tooltip("존 반경 + padding으로 겹침 검사")]
        public float paddingWorld;
        [Tooltip("OverlapSphere Y 오프셋")]
        public float yOffset;
        public QueryTriggerInteraction triggerInteraction; // Ignore/Collide
    }

    [Header("Zone Spawn Blocking (Multi)")]
    [Tooltip("여기에 규칙을 여러 개 추가해서 레이어별로 차단 조건을 세밀하게 설정")]
    public SpawnBlockRule[] spawnBlockRules;

    [Header("Zone Edge Clearance")]
    [Tooltip("보드 경계선으로부터 요구하는 최소 거리(타일 단위)")]
    public float minBoardEdgeClearTiles = 8.0f;


    // 보너스 아크 리롤 코루틴(존별)
    readonly Dictionary<int, Coroutine> _bonusReroll = new Dictionary<int, Coroutine>();

    public event System.Action<int> OnZonesResetSeq;
    public event Action<ZoneSnapshot> OnZoneSpawned;
    public event Action<int> OnZoneExpired;
    public event Action OnZonesReset;
    public event Action<int, float> OnZoneProgress;                    // 0~1
    public event Action<int, Vector3, float> OnZoneContaminatedCircle;//오염원생성

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

    public List<Zone> zones = new List<Zone>();
    int nextZoneId = 1;
    int lastBounceZoneId = -1;
    float lastBounceZoneTime = -999f;
    void Update()
    {
        if (!board || !player) return;
        float dt = Time.deltaTime;
        for (int i = zones.Count - 1; i >= 0; --i)
        {
            var z = zones[i];
            z.remaintime -= dt;
            float time_decrease_ratio = 1f - Mathf.Clamp01(z.remaintime / Mathf.Max(0.0001f, z.time_to_live));
            EmitZoneProgress(z.id, time_decrease_ratio);
            if (z.remaintime <= 0f)
            {
                director.MarkContaminationCircle(z);
                zonespawner.zones.RemoveAt(i);
                StartCoroutine(RespawnAfterDelay(z.profileIndex, 1.0f));
                continue;
            }
        }
        

        var pWorld = player.position;
        for (int i =  zonespawner.zones.Count - 1; i >= 0; i--)
        {
            var z =  zonespawner.zones[i];
            bool inside = PlayerInsideZoneWorld(z, pWorld);

            if (zonespawner.requireExitReenterAfterBounce && z.mustExitFirst && !inside)
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
            int award = zonespawner.normalHitAward;
            bool isBonus = false;
            if (zonespawner.enableBonusSector)
            {
                float ang = BearingDeg(z.centerWorld, pWorld);
                float half = zonespawner.bonusArcDeg * 0.5f;
                // z.bonusAngleDeg는 스폰시에 랜덤 세팅되어 있음
                if (AngleDeltaDeg(ang, z.bonusAngleDeg) <= half)
                {
                    award = zonespawner.bonusHitAward;
                    isBonus = true;
                    gauge.Add(gauge.zonebonusarc);
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

    bool FarFromBoardEdge_World(Vector3 centerWorld, float radiusWorld)
    {
        if (!board) return true;
        var rect = board.GetBoardRectXZ();

        float need = radiusWorld + Mathf.Max(0f, minBoardEdgeClearTiles) * board.tileSize;

        // 사각형 경계까지의 여유
        float left = (centerWorld.x - rect.xMin);
        float right = (rect.xMax - centerWorld.x);
        float bottom = (centerWorld.z - rect.yMin);
        float top = (rect.yMax - centerWorld.z);

        return (left >= need) && (right >= need) && (bottom >= need) && (top >= need);
    }

    // 반경 내 차단 레이어가 있는지 검사
    static readonly Collider[] _overlapBuf = new Collider[64];
    bool AreaIsClearOfObstacles(Vector3 centerWorld, float radiusWorld)
    {
        // 규칙 배열이 설정되어 있으면 그 규칙들 모두 통과해야 함
        if (spawnBlockRules != null && spawnBlockRules.Length > 0)
        {
            for (int i = 0; i < spawnBlockRules.Length; ++i)
            {
                var rule = spawnBlockRules[i];
                if (rule.layers.value == 0) continue; // 비어있는 규칙은 스킵

                float r = Mathf.Max(0f, radiusWorld + rule.paddingWorld);
                Vector3 c = centerWorld + Vector3.up * rule.yOffset;

                int hit = Physics.OverlapSphereNonAlloc(
                    c, r, _overlapBuf, rule.layers, rule.triggerInteraction);

                if (hit > 0) return false; // 하나라도 걸리면 스폰 불가
            }
            return true;
        }
        return true;
    }
    public void EmitZoneProgress(int id, float progress)
    {
        OnZoneProgress?.Invoke(id, progress); // ✅ 같은 타입 내부에서만 Invoke
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
            Vector3 cW = board.IndexToWorld(c.x, c.y);
            float rW = (footprint.x * 0.5f) * board.tileSize;
            if (!FarFromBoardEdge_World(cW, rW)) continue;
            if (!AreaIsClearOfObstacles(cW, rW)) continue;

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

}
