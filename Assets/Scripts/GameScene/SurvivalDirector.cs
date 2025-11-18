// === 이 파일은 원문 그대로이며, 추가/수정 지점만 주석으로 안내합니다 ===

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// ===== 비주얼/외부에 전달할 스냅샷 =====




// ===== 인스펙터에서 편집할 존 설정(프로필) =====


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
    public ZoneSpawner zonespawner;





    [Header("Risk Tuning")]
    public float zoneEnterBonusMul = 1f;

    [Header("Risk Tuning - Per Size")]
    public int zoneReqHitsAdd_S = 0;
    public int zoneReqHitsAdd_M = 0;
    public int zoneReqHitsAdd_L = 0;



  





    // ===== 이벤트 =====
    public event System.Action<Vector3, float> OnClearedCircleWorld;
 
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

    bool _prevInContam = false;
    // ===== 편의 Getter =====
    public int Width => board ? board.width : 0;
    public int Height => board ? board.height : 0;

    int Idx(int x, int y) => y * board.width + x;
 


    // ===== 내부 상태 =====
    enum TileState { Clean, Contaminated }
    TileState[] state;

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
       bool PlayerInsideZoneWorld(Zone z, Vector3 playerPos)
    {
        var a = new Vector2(z.centerWorld.x, z.centerWorld.z);
        var b = new Vector2(playerPos.x, playerPos.z);
        float tol = zonespawner.zoneTouchToleranceTiles * board.tileSize;
        return Vector2.SqrMagnitude(a - b) <= (z.radiusWorld + tol) * (z.radiusWorld + tol);
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
    void BounceZone_RandomOutward(Vector3 zoneCenterWorld,
                              Vector3 contactWorld,          // 디스크-존 접점(없으면 플레이어 pos)
                              float speedMul = 1.00f,        // 반사 후 속도 배율
                              float addSpeed = 0f,           // (선택) 추가 속도
                              float minOutwardDot = 0.0f,    // (선택) 최소 바깥 성분(0~1)
                              float smallNudge = 0.05f)      // (선택) 겹침 방지 미세 밀기
    {
        if (!playerRb) return;

        // 1) 바깥 방향(존 중심→접점)
        Vector3 n = contactWorld - zoneCenterWorld;
        n.y = 0f;
        if (n.sqrMagnitude < 1e-6f)
            n = (playerRb.linearVelocity.sqrMagnitude > 1e-6f) ? playerRb.linearVelocity : Vector3.forward;
        n.Normalize();

        // 2) n을 중심으로 ±90° 범위에서 임의 회전 → 항상 바깥 반평면
        float phi = UnityEngine.Random.Range(-90f, +90f);
        Vector3 dir = Quaternion.AngleAxis(phi, Vector3.up) * n; // 평면 회전
        dir.y = 0f; dir.Normalize();

        // 3) 바깥 성분 하한 보장(원하면 사용)
        if (minOutwardDot > 0f && Vector3.Dot(dir, n) < minOutwardDot)
        {
            // minOutwardDot에 해당하는 최대 허용 회전각으로 클램프
            float maxDeg = Mathf.Acos(Mathf.Clamp(minOutwardDot, 0f, 1f)) * Mathf.Rad2Deg; // 0~90
            phi = Mathf.Clamp(phi, -maxDeg, +maxDeg);
            dir = Quaternion.AngleAxis(phi, Vector3.up) * n;
            dir.y = 0f; dir.Normalize();
        }

        // 4) 속도 재설정
        float s = playerRb.linearVelocity.magnitude * speedMul + addSpeed;
        s = Mathf.Max(0f, s);
        playerRb.linearVelocity = dir * s;

        // 5) 즉시 재충돌 방지: 바깥으로 아주 살짝 밀어냄(선택)
        if (smallNudge > 0f)
            player.position += n * smallNudge;
    }


}