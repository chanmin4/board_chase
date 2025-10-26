using System.Collections;
using UnityEngine;

/// 디스크 전용: 시작/활성화 시 자동으로 꼬리 청소 + 플레이어 페인트.
/// - 카드 필요 없음(지속 동작)
/// - 잉크 소모/스턴은 SurvivalGauge(ink) 쪽에서 처리됨(BoardPaintSystem가 호출)
[DefaultExecutionOrder(+50)]
public class CleanTrailAbility_EnemyDisk : MonoBehaviour
{
    [Header("Ink Radius (meters)")]
    [Tooltip("잉크(청소+페인트) 반지름 배수 (1=원래 r)")]
    public float radiusMul = 1f;                
    [Tooltip("잉크(청소+페인트) 반지름 추가/감소 값(미터, 음수 가능)")]
    public float radiusAddWorld = 0f;          

    [Header("Extra Radius (tiles)")]
    [Tooltip("추가 반지름(타일). 0이면 추가 없음")]
    public float extraRadiusTiles = 0f;

    [Header("Behaviour")]
    [Tooltip("씬 시작/활성화 시 자동 실행")]
    public bool runOnEnable = true;
    [Tooltip("초기화 레이스 방지용 소량 딜레이")]
    public float startDelay = 0f;

    // Refs
    BoardPaintSystem paintSystem;
    BoardGrid board;
    Transform EnemyDisk;
    Collider diskCol;

    // Trail 상태
    bool   isRunning;
    bool   haveLast;
    Vector3 lastCenter;

    Coroutine co;

    void Awake()
    {
        EnemyDisk   = transform;
        diskCol  = GetComponent<Collider>();
        paintSystem = FindAnyObjectByType<BoardPaintSystem>();
        board       = paintSystem ? paintSystem.board : FindAnyObjectByType<BoardGrid>();

        if (!paintSystem)
            Debug.LogWarning("[CleanTrailAbility_Disk] BoardPaintSystem not found in scene.");
        if (!diskCol)
            Debug.LogWarning("[CleanTrailAbility_Disk] Collider not found on disk.");
    }

    void OnEnable()
    {
        if (runOnEnable) Begin();
    }

    void OnDisable()
    {
        StopNow();
    }

    [ContextMenu("Begin")]
    public void Begin()
    {
        if (isRunning) return;
        isRunning = true;
        haveLast  = false;
        co = StartCoroutine(CleanLoop());
    }

    [ContextMenu("Stop")]
    public void StopNow()
    {
        if (!isRunning) return;
        isRunning = false;
        if (co != null) StopCoroutine(co);
        co = null;
    }

    IEnumerator CleanLoop()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
        else yield return null; // 1프레임 대기(초기화 대기)

        while (isRunning)
        {
            if (EnemyDisk && paintSystem)
            {
                // 1) 기본 반지름 계산(디스크 콜라이더 형태 대응) + 타일 기반 추가 반지름
                float addWorld = (board ? board.tileSize : 1f) * Mathf.Max(0f, extraRadiusTiles);

                Vector3 centerNow;
                float rBase;

                if (diskCol is CapsuleCollider cap)
                {
                    GetCapsuleWorld(cap, out Vector3 a, out Vector3 b, out float rad);
                    centerNow = (a + b) * 0.5f;
                    rBase     = rad + addWorld;
                }
                else if (diskCol is SphereCollider sph)
                {
                    centerNow = EnemyDisk.position;
                    rBase     = GetSphereRadiusWorld(sph) + addWorld;
                }
                else if (diskCol is BoxCollider box)
                {
                    centerNow = EnemyDisk.position;
                    Vector3 e = Vector3.Scale(box.size * 0.5f, EnemyDisk.lossyScale);
                    rBase     = Mathf.Sqrt(e.x * e.x + e.z * e.z) + addWorld;
                }
                else
                {
                    centerNow = EnemyDisk.position;
                    rBase     = addWorld;
                }

                float rInk = Mathf.Max(0.01f, rBase * radiusMul + radiusAddWorld);
                // 2) 첫 프레임은 한 번만 찍고 기준점 세팅
                if (!haveLast)
                {
                    Stamp(centerNow, rInk);
                    lastCenter = centerNow;
                    haveLast   = true;
                }
                else
                {
                    // 3) 지난 프레임→현재 프레임을 Trail로 배치(간격/소모는 PaintSystem에서 처리)
                    paintSystem.EnqueueTrail(BoardPaintSystem.PaintChannel.Enemy,
                                             lastCenter, centerNow,
                                             rInk,
                                             spacingMeters: -1f,
                                             clearOtherChannel: true);
                    lastCenter = centerNow;
                }
            }

            yield return null; // 스케일 적용(일시정지 시 멈춤)
        }
    }

    void Stamp(Vector3 p, float rInk)                           
    {
        if (!paintSystem) return;

        // 이제 하나의 반지름으로 청소+페인트 일괄 처리(덮어쓰기)
        paintSystem.EnqueueCircle(BoardPaintSystem.PaintChannel.Enemy,
                                  p, rInk,
                                  clearOtherChannel: true);
    }


    // ===== Helpers =====
    static void GetCapsuleWorld(CapsuleCollider cap, out Vector3 worldA, out Vector3 worldB, out float worldRadius)
    {
        Vector3 cLocal    = cap.center;
        Vector3 axisLocal = cap.direction == 0 ? Vector3.right :
                            cap.direction == 1 ? Vector3.up    :
                                                 Vector3.forward;

        Vector3 s = cap.transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        worldRadius = cap.radius * radiusScale;

        float axisScale = cap.direction == 0 ? Mathf.Abs(s.x) :
                          cap.direction == 1 ? Mathf.Abs(s.y) :
                                               Mathf.Abs(s.z);

        float worldHeight = Mathf.Max(0f, cap.height * axisScale - 2f * worldRadius);

        Vector3 aLocal = cLocal + axisLocal * (worldHeight * 0.5f / axisScale);
        Vector3 bLocal = cLocal - axisLocal * (worldHeight * 0.5f / axisScale);

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
