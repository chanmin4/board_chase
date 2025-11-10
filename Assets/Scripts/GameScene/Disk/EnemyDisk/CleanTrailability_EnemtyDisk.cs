using System.Collections;
using UnityEngine;

/// 디스크 전용: 시작/활성화 시 자동으로 꼬리 청소 + 적 페인트.
/// EnemyInkGauge 제거 이후에도 항상 칠하도록, 하이브리드(Head Now + Backfill Enqueue) 적용.
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

    [Header("Hybrid Stamp (Patch2)")]
    [Tooltip("이동량 임계(미터). 이 값 미만이면 정지로 간주하고 아무 것도 안 찍음")]
    public float minMoveMeters = 0.02f;
    [Tooltip("백필 간격 = rInk * segSpacingMul (작을수록 촘촘)")]
    public float segSpacingMul = 0.6f;
    [Tooltip("한 프레임에 백필로 찍을 최대 개수(프레임 예산)")]
    public int backfillCapPerFrame = 16;

    // Refs
    BoardPaintSystem paintSystem;
    BoardGrid board;
    Transform enemyDisk;
    Collider  diskCol;

    // Trail 상태
    bool    isRunning;
    bool    haveLast;
    Vector3 lastCenter;

    Coroutine co;

    void Awake()
    {
        enemyDisk   = transform;
        diskCol     = GetComponent<Collider>();
        paintSystem = FindAnyObjectByType<BoardPaintSystem>();
        board       = paintSystem ? paintSystem.board : FindAnyObjectByType<BoardGrid>();

        if (!paintSystem)
            Debug.LogWarning("[CleanTrailAbility_EnemyDisk] BoardPaintSystem not found in scene.");
        if (!diskCol)
            Debug.LogWarning("[CleanTrailAbility_EnemyDisk] Collider not found on disk.");
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
            if (enemyDisk && paintSystem)
            {
                // 1) 기본 반지름 계산(콜라이더 형태 대응) + 타일 기반 추가 반지름
                float addWorld = (board ? board.tileSize : 1f) * Mathf.Max(0f, extraRadiusTiles);

                Vector3 centerNow;
                float   rBase;

                if (diskCol is CapsuleCollider cap)
                {
                    GetCapsuleWorld(cap, out Vector3 a, out Vector3 b, out float rad);
                    centerNow = (a + b) * 0.5f;
                    rBase     = rad + addWorld;
                }
                else if (diskCol is SphereCollider sph)
                {
                    centerNow = enemyDisk.position;
                    rBase     = GetSphereRadiusWorld(sph) + addWorld;
                }
                else if (diskCol is BoxCollider box)
                {
                    centerNow = enemyDisk.position;
                    Vector3 e = Vector3.Scale(box.size * 0.5f, enemyDisk.lossyScale);
                    rBase     = Mathf.Sqrt(e.x * e.x + e.z * e.z) + addWorld;
                }
                else
                {
                    centerNow = enemyDisk.position;
                    rBase     = addWorld;
                }

                float rInk = Mathf.Max(0.01f, rBase * radiusMul + radiusAddWorld);

                // ─────────────────────────────────────────────
                // [PATCH2] 하이브리드
                //  - 정지면 아무 것도 안 찍음
                //  - 이동 시: 헤드 Now + 구간 백필 EnqueueCircle(거리기반, 프레임캡)
                // ─────────────────────────────────────────────
                if (!haveLast)
                {
                    // 시작 1회: 가볍게 큐로 보내고 기준점 세팅
                    EnqueueOne(centerNow, rInk);
                    lastCenter = centerNow;
                    haveLast   = true;
                }
                else
                {
                    float dist = (centerNow - lastCenter).magnitude;

                    if (dist >= minMoveMeters)
                    {
                        // 1) 헤드 1점 즉시
                        paintSystem.HeadStampNow(BoardPaintSystem.PaintChannel.Enemy,
                                                 centerNow, rInk, /*clearOther=*/true);

                        // 2) 백필: 거리 기반 분할 + 프레임 캡
                        float segSpacing = Mathf.Max(0.001f, rInk * Mathf.Max(0.05f, segSpacingMul));
                        int stepsTarget  = Mathf.CeilToInt(dist / segSpacing);
                        int steps        = Mathf.Min(Mathf.Max(1, stepsTarget), Mathf.Max(1, backfillCapPerFrame));

                        for (int i = 1; i <= steps; ++i)
                        {
                            float t = (float)i / steps;
                            Vector3 p = Vector3.Lerp(lastCenter, centerNow, t);
                            EnqueueOne(p, rInk);
                        }

                        lastCenter = centerNow;
                    }
                    // 정지면 아무 것도 안 찍음
                }
            }

            yield return null; // 프레임마다(일시정지 중에도 코루틴은 돌지만, 이동=0이라 찍지 않음)
        }
    }

    void EnqueueOne(Vector3 p, float rInk)
    {
        if (!paintSystem) return;
        paintSystem.EnqueueCircle(BoardPaintSystem.PaintChannel.Enemy,
                                  p, rInk, /*clearOtherChannel=*/true);
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
