using System;
using UnityEngine;

[DefaultExecutionOrder(+55)]
[DisallowMultipleComponent]
public class PerfectBounce : MonoBehaviour
{
    // ── Refs ──
    [Header("Refs")]
    public DiskLauncher launcher;
    public Rigidbody rb;
    public Collider diskCollider;
    public CleanTrailAbility_Disk trail; // 콤보 반경 증가(선택)
    public SurvivalGauge gauge;          // 잉크 자원(선택) gauge.Add(float) 가정

    // ── 보상/패널티 ──
    [Header("Rewards (성공 시)")]
    public float speedAddOnSuccess = 8f;
    public float inkGainOnSuccess  = 12.5f;   // TODO: 프로젝트 단위에 맞게 조정

    [Header("Penalty (실패 시)")]
    public float speedDecOnFail = 15f;
    public float inkLossOnFail  = 25f;      // TODO: 프로젝트 단위에 맞게 조정

    [Header("Combo → Radius")]
    public float extraRadiusPerComboTiles = 0f;

    // ── 경계 허용(45/135/225/315 양쪽 인정) ──
    [Header("Boundary")]
    [Tooltip("경계 허용 오차(도). 이 각도 안쪽이면 양쪽 섹터 모두 성공으로 인정")]
    public float boundaryGraceDeg = 1.0f;
    [Range(10f,170f)] public float PerfectBounceDeg = 90f;

    // ── Selection Visual (프리팹 or LineRenderer 폴백) ──
    [Header("Selection Visual")]
    public GameObject visualizeIndicatorPrefab;                 // 선택 방향 표시 프리팹(있으면 사용)
    [Tooltip("프리팹 없을 때 폴백 아크 반지름(0이면 콜라이더로 추정)")]
    public float visualizeArcRadiusWorld = 0f;
    [Tooltip("아크 표시 높이(Y 오프셋)")]
    public float visualizeArcHeight = 0.02f;
    [Tooltip("아크 반각(도). 45면 섹터 중심 기준 ±45°")]
    [NonSerialized] public float visualizeArcHalfAngleDeg;
    [Range(8,128)] public int visualizeArcSegments = 32;
    public Color visualizeArcColor = new Color(1f, 0.2f, 0.2f, 0.9f);
    [Tooltip("폴백(LineRenderer) 선 굵기")]
    public float visualizeArcLineWidth = 0.08f;

    // ── 내부 상태 ──
    public enum EightDir { NorthWest,North,NorthEast, West, East,SouthWest,South,SouthEast } // WASD = 북서남동
    [Header("Runtime")]
    public EightDir selected = EightDir.North;

    int combo = 0;
    //float baseExtraRadiusTiles;

    // 시각화 인스턴스
    GameObject visGO;         // 프리팹 또는 LineRenderer를 담는 GO(디스크의 자식)
    LineRenderer visLR;       // 폴백용 LineRenderer(프리팹 없을 때만)
    EightDir visDir;           // 현재 시각화 중인 방향
    PerfectBounce.EightDir ComputeEightDirFromWASD(PerfectBounce.EightDir fallback)
    {
        int x = 0, z = 0; // X=좌(-)/우(+), Z=뒤(-)/앞(+)
        if (Input.GetKey(KeyCode.A)) x -= 1;
        if (Input.GetKey(KeyCode.D)) x += 1;
        if (Input.GetKey(KeyCode.S)) z -= 1;
        if (Input.GetKey(KeyCode.W)) z += 1;

        if (x == 0 && z == 0) return fallback; // 아무 키도 없음 → 유지

        if (z > 0) // 북쪽 계열
        {
            if (x < 0) return EightDir.NorthWest;
            if (x > 0) return EightDir.NorthEast;
            return EightDir.North;
        }
        if (z < 0) // 남쪽 계열
        {
            if (x < 0) return EightDir.SouthWest;
            if (x > 0) return EightDir.SouthEast;
            return EightDir.South;
        }
        // 좌/우 단일
        if (x < 0) return EightDir.West;
        if (x > 0) return EightDir.East;

        return fallback;
    }

    // ★ ADD: KeyDown/KeyUp 헬퍼(선택)
    bool AnyKeyDown() =>
        Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
        Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D);

    bool AnyKeyUp() =>
        Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.A) ||
        Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D);


    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        diskCollider = GetComponent<Collider>();
        launcher = GetComponent<DiskLauncher>();
        trail = GetComponent<CleanTrailAbility_Disk>();
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!diskCollider) diskCollider = GetComponent<Collider>();
        if (!launcher) launcher = GetComponent<DiskLauncher>();
        if (!trail) trail = GetComponent<CleanTrailAbility_Disk>();
        //if (trail) baseExtraRadiusTiles = trail.extraRadiusTiles;
        visualizeArcHalfAngleDeg= PerfectBounceDeg * 0.5f;
    }

    void Update()
    {
        if (AnyHold())
        {
            var prev = selected;
            selected = ComputeEightDirFromWASD(prev);

            if (prev != selected) StartVisualize(selected);
            else UpdateVisualize();
        }
        else
        {
            StopVisualize();
        }
    
    }

    void ResolveHit(Vector3 hitPoint)
    {
        
        bool holdOk = IsSelectedHeld();  // 선택된 방향 키가 현재 눌려있어야 함
        bool dirOk  = IsDirectionOk(hitPoint);

        if (holdOk && dirOk) ApplySuccess();
        else                 ApplyFail();
    }

    void OnCollisionEnter(Collision c)
    {
        if (!launcher) return;
        // 벽만이 아니라 모든 충돌을 받으려면 위 필터 주석 유지

        var cp = c.GetContact(0);

        ResolveHit(cp.point);
    }
    void OnTriggerEnter(Collider other)
    {
        if (!launcher) return;
        if (other.transform == transform || other.transform.IsChildOf(transform)) return;

        Vector3 p = other.ClosestPoint(transform.position);
        ResolveHit(p);
    }


    // ── 12시=0°, 시계방향 각도 → 섹터 매칭 ──
    bool IsDirectionOk(Vector3 hitPoint)
    {
        // 충돌점 → 디스크 중심 방향 벡터
        Vector3 v = hitPoint - transform.position;
        v.y = 0f;
        if (v.sqrMagnitude < 1e-6f) return false;

        // +Z(12시)=0°, +X(3시)=90° 기준 각도
        float ang = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        if (ang < 0f) ang += 360f;

        // 섹터 반각(half) + 경계 여유각(grace)만큼 허용
        float half = Mathf.Clamp(PerfectBounceDeg * 0.5f, 1f, 179f);
        float lim = half + Mathf.Max(0f, boundaryGraceDeg);

        // 선택된 4방위의 '중심각'
        float center = 0f;
        switch (selected)
        {
            case EightDir.NorthWest: center = 315f; break;
            case EightDir.North: center = 0f; break;  // 12시
            case EightDir.NorthEast: center = 45f; break;
            case EightDir.West: center = 270f; break;  // 9시
            case EightDir.East: center = 90f; break;  // 3시
            case EightDir.SouthWest: center = 225f; break;  
            case EightDir.South: center = 180f; break;  // 6시
            case EightDir.SouthEast: center = 135f; break;  // 9시
            
        }

        // 중심각과의 차이(래핑 포함)가 lim 이하면 성공
        return Mathf.Abs(Mathf.DeltaAngle(ang, center)) <= lim;
    }

    // ── 성공/실패 ──
    void ApplySuccess()
    {
        if (rb)
        {
            Vector3 v = rb.linearVelocity;
            Vector3 dir = (v.sqrMagnitude > 1e-6f) ? v.normalized : transform.forward;
            float s = v.magnitude + speedAddOnSuccess;         // ← 합연산(+)
            rb.linearVelocity = dir * Mathf.Max(0f, s);        // (마이너스 방지만 최소 보정)
        }

        if (gauge && inkGainOnSuccess != 0f) gauge.Add(inkGainOnSuccess);

        combo = Mathf.Max(0, combo + 1);
        //if (trail) trail.extraRadiusTiles = baseExtraRadiusTiles + combo * extraRadiusPerComboTiles;
    }

    void ApplyFail()
    {
        if (rb)
        {
            Vector3 v = rb.linearVelocity;
            Vector3 dir = (v.sqrMagnitude > 1e-6f) ? v.normalized : transform.forward;
            float s = v.magnitude - speedDecOnFail;            // ← 합연산(-)
            rb.linearVelocity = dir * Mathf.Max(0f, s);        // (0 미만 방지)
        }

        if (gauge && inkLossOnFail != 0f) gauge.Add(-inkLossOnFail);

        combo = 0;
        //if (trail) trail.extraRadiusTiles = baseExtraRadiusTiles;
    }

    // ── 입력 상태 ──
    bool AnyHold()
    {
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
               Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
    }
    bool IsSelectedHeld()
    {
        switch (selected)
        {
            // 대각: 두 키 모두 눌림 필요
            case EightDir.NorthWest: return Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A);
            case EightDir.NorthEast: return Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D);
            case EightDir.SouthWest: return Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A);
            case EightDir.SouthEast: return Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D);

            // 단일: 해당 키만 눌려 있으면 OK
            case EightDir.North: return Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D);
            case EightDir.South: return Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D);
            case EightDir.West: return Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S);
            case EightDir.East: return Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S);
        }
        return false;
    }




    // ── 방향 벡터 ──
    static Vector3 DirVector(EightDir d)
    {
        switch (d)
        {
            case EightDir.North: return Vector3.forward;                     // W
            case EightDir.South: return Vector3.back;                        // S
            case EightDir.East: return Vector3.right;                       // D
            case EightDir.West: return Vector3.left;                        // A
            case EightDir.NorthEast: return (Vector3.forward + Vector3.right).normalized;   // W+D
            case EightDir.NorthWest: return (Vector3.forward + Vector3.left).normalized;    // W+A
            case EightDir.SouthEast: return (Vector3.back + Vector3.right).normalized;   // S+D
            case EightDir.SouthWest: return (Vector3.back + Vector3.left).normalized;    // S+A
        }
        return Vector3.forward;
    }


    // ── 시각화 (프리팹 or LineRenderer 폴백) ──
    void StartVisualize(EightDir dir)
    {
        visDir = dir;

        if (visualizeIndicatorPrefab)
        {
            if (!visGO)
            {
                visGO = Instantiate(visualizeIndicatorPrefab);
                visGO.transform.SetParent(transform, true); // 자식
            }
            visGO.transform.position = transform.position + Vector3.up * visualizeArcHeight;
            visGO.transform.rotation = Quaternion.LookRotation(DirVector(dir), Vector3.up);
            visLR = null; // 프리팹 사용 중
        }
        else
        {
            if (!visGO)
            {
                visGO = new GameObject("(visualize) SectorArc");
                visGO.transform.SetParent(transform, false);
                visGO.transform.localPosition = Vector3.zero;

                visLR = visGO.AddComponent<LineRenderer>();
                visLR.useWorldSpace = false; // 부모 따라감
                visLR.loop = false;
                visLR.widthMultiplier = visualizeArcLineWidth;
                visLR.numCapVertices = 4;
                visLR.numCornerVertices = 2;
                visLR.material = new Material(Shader.Find("Sprites/Default"));
                visLR.startColor = visualizeArcColor;
                visLR.endColor   = visualizeArcColor;
            }
            BuildArc(visLR, dir);
        }
    }

    void StopVisualize()
    {
        if (visGO)
        {
            Destroy(visGO);
            visGO = null;
            visLR = null;
        }
    }

    void UpdateVisualize()
    {
        if (!visGO) return;

        if (visualizeIndicatorPrefab)
        {
            // 프리팹은 방향만 맞춰주면 됨
            visGO.transform.position = transform.position + Vector3.up * visualizeArcHeight;
            visGO.transform.rotation = Quaternion.LookRotation(DirVector(selected), Vector3.up);
        }
        else if (visLR)
        {
            // 폴백 라인은 선택 방향이 변했으면 다시 빌드
            if (visDir != selected)
            {
                visDir = selected;
                BuildArc(visLR, visDir);
            }
        }
    }

    void BuildArc(LineRenderer lr, EightDir dir)
    {
        if (!lr) return;

        // 로컬 반경 계산(부모 스케일 고려)
        float rWorld = visualizeArcRadiusWorld > 0f ? visualizeArcRadiusWorld : Mathf.Max(0.2f, EstimateColliderRadiusWorld(diskCollider) * 1.2f);
        Vector3 s = transform.lossyScale;
        float k = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z), 1e-4f);
        float rLocal = rWorld / k;

        Vector3 centerLocal = Vector3.up * visualizeArcHeight;
        Vector3 fwdLocal = (Quaternion.Inverse(transform.rotation) * DirVector(dir)).normalized;

        int seg = Mathf.Max(4, visualizeArcSegments);
        lr.positionCount = seg + 1;

        float halfRad = Mathf.Deg2Rad * Mathf.Clamp(visualizeArcHalfAngleDeg, 1f, 179f);
        for (int i = 0; i <= seg; ++i)
        {
            float t = Mathf.Lerp(-halfRad, +halfRad, i / (float)seg);
            Vector3 dirRotLocal = Quaternion.AngleAxis(t * Mathf.Rad2Deg, Vector3.up) * fwdLocal;
            lr.SetPosition(i, centerLocal + dirRotLocal * rLocal);
        }

        lr.widthMultiplier = visualizeArcLineWidth; // 인스펙터 변경 반영
        lr.startColor = visualizeArcColor;
        lr.endColor   = visualizeArcColor;
    }

    static float EstimateColliderRadiusWorld(Collider col)
    {
        if (!col) return 0.5f;
        var t = col.transform;

        if (col is SphereCollider sph)
        {
            var s = t.lossyScale;
            float k = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            return Mathf.Max(0.01f, sph.radius * k);
        }
        if (col is CapsuleCollider cap)
        {
            var s = t.lossyScale;
            float k = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
            return Mathf.Max(0.01f, cap.radius * k);
        }
        if (col is BoxCollider box)
        {
            Vector3 e = Vector3.Scale(box.size * 0.5f, t.lossyScale);
            return Mathf.Sqrt(e.x * e.x + e.z * e.z);
        }
        return 0.5f;
    }
}
