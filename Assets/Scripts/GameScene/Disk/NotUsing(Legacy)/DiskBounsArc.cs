/*
using UnityEngine;
using UnityEngine.Events;

/// 디스크용 보너스 아크(시각 + 판정 + 이벤트 발화)
[DisallowMultipleComponent]
public class DiskBonusArc : MonoBehaviour
{
    public enum Team { Player, Enemy }

    [Header("Team")]
    public Team team = Team.Player;

    [Header("Visual")]
    public Material arcMaterial;       // 없으면 Sprites/Default
    public Color    arcColor = Color.red;
    [Range(8,128)] public int segments = 32;
    [Tooltip("반경 오프셋(월드, 디스크 가장자리에서 얼마나 띄울지)")]
    public float radiusOffset = 0.08f;
    [Tooltip("아크 두께(월드)")]
    public float width = 0.08f;
    [Tooltip("아크 높이(바닥에서 띄우기, 월드)")]
    public float yLift = 0.10f;

    [Header("Arc Config (Inspector tunable)")]
    [Range(5f, 180f)] public float arcDegLocal = 90f; // 인스펙터에서 바꿔도 됨(공유설정 미사용 시)
    [Tooltip("초당 회전 속도(도/초). 공유설정 사용 시 방향/속도도 공유됨")]
    public float spinDegPerSec = 120f;

    [Header("Shared Match Config (both disks same)")]
    public bool  useSharedMatchConfig = true;
    public bool  applySpin = false;

    // 매판 공용 설정(서로 같은 각/방향/시작각)
    static float S_startAngleDeg;
    static float S_arcDeg;
    static int   S_spinDir; // +1(CW) / -1(CCW)
    static bool S_inited;

    [Header("Hit Trigger (Event-Driven)")]
    public UnityEvent<Transform> OnBonusArcHit;

    [Header("Optional – Auto Gauge Penalty")]
    public bool autoApplyGaugePenalty = false;
    public float gaugePenalty = 50f;   // 맞은 ‘상대’ 게이지 감소치
    [Header("Distance Override")]
    public bool  useRadiusOverride = true;   // 콜라이더 추정 말고 반경을 고정값으로
    public float radiusOverrideWorld = 0.5f;  // 월드 반경(센터 기준)
    public float edgeGapWorld = 0f;           // 가장자리에서 ±로 더하는 갭(음수면 더 붙음)

    LineRenderer lr;
    float currentAngleDeg;

    Collider _col; // 내 디스크 콜라이더 참조(반경 추정용)


    void Awake()
    {
        _col = GetComponentInChildren<Collider>();
        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 2;
        lr.numCapVertices    = 2;
        lr.positionCount     = segments + 1;
        lr.widthMultiplier   = width;

        var mat = arcMaterial ? new Material(arcMaterial) : new Material(Shader.Find("Sprites/Default"));
        mat.renderQueue = 5000;
        lr.material = mat;
        lr.startColor = lr.endColor = arcColor;

        if (useSharedMatchConfig)
        {
            if (!S_inited)
            {
                RandomizeMatchConfig(Random.Range(0f, 360f), arcDegLocal, Random.value < 0.5f);
                S_inited = true; // 매판 딱 1회만 랜덤
            }
            currentAngleDeg = GetStartAngle();
        }
        else
        {
            currentAngleDeg = 0f; // 공유 안 쓰면 로컬 설정
        }

        currentAngleDeg = GetStartAngle();
        RebuildArcMesh(); // 첫 프레임
    }

    void Update()
    {
        // 회전
        if (applySpin)
        {
            float dir = useSharedMatchConfig ? S_spinDir : +1f;
            float spd = spinDegPerSec * dir;
            currentAngleDeg = Mathf.Repeat(currentAngleDeg + spd * Time.deltaTime, 360f);
        }
        RebuildArcMesh();
    }

    // ==== 공개: 매판 공용 설정 초기화(게임 시작 시 1회 호출) ====
    public static void RandomizeMatchConfig(float startAngleDeg, float arcDeg, bool clockwise)
    {
        S_startAngleDeg = Mathf.Repeat(startAngleDeg, 360f);
        S_arcDeg        = Mathf.Clamp(arcDeg, 5f, 180f);
        S_spinDir       = clockwise ? +1 : -1;
    }

    // ==== 외부에서 충돌 알림(디스크-디스크 충돌 시 호출) ====
    // otherRoot : 상대 디스크의 Transform(루트), hitPointWorld : 충돌 접점
    public void OnCollisionWithOtherDisk(Transform otherRoot, Vector3 hitPointWorld)
    {
        Debug.Log("[Arc] OnCollisionWithOtherDisk start");

        // 팀 필터 (플레이어↔적만)
        var selfIsPlayer = (team == Team.Player);
        if (TryGetOtherTeam(otherRoot, out bool otherIsPlayer))
            if (selfIsPlayer == otherIsPlayer) return;

        // ★ 존처럼: 상대 중심을 각도판정에 사용
        var otherCenterW = GetRootCenterXZ(otherRoot);
        if (!IsPointInArc(otherCenterW)) return;

        Debug.Log("[Arc] IN-ARC -> Invoke");
        OnBonusArcHit?.Invoke(otherRoot);
    }

    static float BearingDeg(Vector3 fromW, Vector3 toW)
    {
        Vector3 v = toW - fromW; v.y = 0f;
        if (v.sqrMagnitude < 1e-6f) return 0f;
        return Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
    }


    // Mathf.DeltaAngle과 동일 개념 (존 코드와 의미 맞춤)
    static float AngleDeltaDeg(float a, float b) => Mathf.DeltaAngle(a, b);

    Vector3 GetRootCenterXZ(Transform t)
    {
        var p2 = t.position;
        return new Vector3(p2.x, 0f, p2.z);
    }



    // ==== 판정: 월드 한 점이 현재 아크 안인가? ====
    bool IsPointInArc(Vector3 otherCenterW)
    {
        Vector3 myC = GetCenterOnBoardY();
        float ang = Mathf.Atan2(otherCenterW.z - myC.z, otherCenterW.x - myC.x) * Mathf.Rad2Deg;
        float mid = MidDegWorld();
        float half = Mathf.Clamp(GetArcDeg() * 0.5f, 0f, 180f);
        float d = Mathf.DeltaAngle(ang, mid);

        // 디버그
        Debug.Log($"[Arc] ang={ang:F1}, mid={mid:F1}, delta={d:F1}, half={half:F1}");
        return Mathf.Abs(d) <= half;
    }



    // ===== 내부 유틸 =====
    float GetArcDeg() => useSharedMatchConfig ? S_arcDeg : arcDegLocal;
    float GetStartAngle() => useSharedMatchConfig ? S_startAngleDeg : 0f;

    float MidDegWorld()
    {
        // 공유 사용: 매판 고정 절대각, 공유 미사용: 내 forward + currentAngle
        if (useSharedMatchConfig) return S_startAngleDeg;
        float fwd = Mathf.Atan2(transform.forward.z, transform.forward.x) * Mathf.Rad2Deg;
        return fwd + currentAngleDeg;
    }

    void RebuildArcMesh()
    {
        float r = GetVisualRadius();
        float mid = MidDegWorld();
        float half = GetArcDeg() * 0.5f;
        float a0 = Mathf.Deg2Rad * (mid - half);
        float a1 = Mathf.Deg2Rad * (mid + half);

        Vector3 c = GetCenterOnBoardY();   // (x,z) 중심
        Vector3 baseW = new Vector3(c.x, yLift, c.z);

        lr.widthMultiplier = width;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float a = Mathf.Lerp(a0, a1, t);
            Vector3 off = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            lr.SetPosition(i, baseW + off); // ← 월드 좌표로 직접 세팅
        }
    }




    float GetVisualRadius()
    {
        // 1) 기본은 콜라이더에서 추정
        float baseR = 0.5f;
        if (!useRadiusOverride)
        {
            if (_col is CapsuleCollider cap)
            {
                GetCapsuleWorld(cap, out var A, out var B, out var rr);
                baseR = rr;
            }
            else if (_col is SphereCollider sph)
            {
                baseR = GetSphereRadiusWorld(sph);
            }
            else if (_col is BoxCollider box)
            {
                Vector3 e = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
                baseR = Mathf.Sqrt(e.x * e.x + e.z * e.z);
            }
        }
        else
        {
            // 2) 강제 반경(센터 기준 고정값)
            baseR = Mathf.Max(0.01f, radiusOverrideWorld);
        }

        // 3) 가장자리에서의 간격(기존 radiusOffset과 새 edgeGapWorld를 함께 반영)
        return Mathf.Max(0.01f, baseR + radiusOffset + edgeGapWorld);
    }

    Vector3 GetCenterOnBoardY()
    {
        var p = transform.position;
        return new Vector3(p.x, 0f, p.z);
    }

    static float ShortestDeltaDeg(float a, float b)
    {
        float d = Mathf.DeltaAngle(a, b);
        return d; // -180~+180
    }

    // 팀 판정: 상대 루트에 어느 게이지가 붙어있는지로 간단 구분
    bool TryGetOtherTeam(Transform otherRoot, out bool otherIsPlayer)
    {
        otherIsPlayer = otherRoot.GetComponentInParent<SurvivalGauge>() != null;
        return otherIsPlayer || (otherRoot.GetComponentInParent<EnemyInkGauge>() != null);
    }

    // === 콜라이더 월드치수 헬퍼(이미 여러 곳에서 쓰던 패턴) ===
    static void GetCapsuleWorld(CapsuleCollider cap, out Vector3 worldA, out Vector3 worldB, out float worldRadius)
    {
        Vector3 axisLocal = cap.direction == 0 ? Vector3.right :
                            cap.direction == 1 ? Vector3.up    : Vector3.forward;
        Vector3 s = cap.transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        worldRadius = cap.radius * radiusScale;

        float axisScale = cap.direction == 0 ? Mathf.Abs(s.x) :
                          cap.direction == 1 ? Mathf.Abs(s.y) : Mathf.Abs(s.z);

        float worldHeight = Mathf.Max(0f, cap.height * axisScale - 2f * worldRadius);
        Vector3 aLocal = cap.center + axisLocal * (worldHeight * 0.5f / axisScale);
        Vector3 bLocal = cap.center - axisLocal * (worldHeight * 0.5f / axisScale);

        worldA = cap.transform.TransformPoint(aLocal);
        worldB = cap.transform.TransformPoint(bLocal);
    }
    static float GetSphereRadiusWorld(SphereCollider sph)
    {
        Vector3 s = sph.transform.lossyScale;
        float uniform = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        return sph.radius * uniform;
    }
}

*/