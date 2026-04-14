using System.Collections;
using UnityEngine;

/// 디스크 전용: 시작/활성화 시 자동으로 꼬리 청소 + 플레이어 페인트.
/// - 카드 필요 없음(지속 동작)
/// - 잉크 소모/스턴은 SurvivalGauge(ink) 쪽에서 처리됨(BoardPaintSystem가 호출)
[DefaultExecutionOrder(+50)]
public class CleanTrailAbility_Disk : MonoBehaviour
{
    [Header("Ink Radius (meters)")]
    [Tooltip("잉크(청소+페인트) 반지름 배수 (1=원래 r)")]
    public float radiusMul = 1f;
    [Tooltip("잉크(청소+페인트) 반지름 추가/감소 값(미터, 음수 가능)")]
    public float radiusAddWorld = 0f;

    [Header("Behaviour")]
    [Tooltip("씬 시작/활성화 시 자동 실행")]
    public bool runOnEnable = true;
    [Tooltip("초기화 레이스 방지용 소량 딜레이")]
    public float startDelay = 0f;

    [Header("Baseline (Optional)")]
    [Tooltip("ON이면 콜라이더 대신 '처음' 기준 잉크 반지름을 고정값(월드 m)으로 사용")]
    public bool useExplicitBaseRadius = true;
    [Tooltip("기본 잉크 반지름(월드 m). useExplicitBaseRadius = ON일 때 사용")]
    public float explicitBaseRadiusWorld = 1f;

    [Header("Hybrid Stamp (Patch2)")]
    [Tooltip("이동량 임계(미터). 이 값 미만이면 정지로 간주하고 아무 것도 안 찍음")]
    public float minMoveMeters = 0.02f;
    [Tooltip("백필 간격 = rInk * segSpacingMul (작을수록 촘촘)")]
    public float segSpacingMul = 0.6f;
    [Tooltip("한 프레임에 백필로 찍을 최대 개수(프레임 예산)")]
    public int backfillCapPerFrame = 16;

    // (선택) 디버그 확인용 현재 값
    public float CurrentInkRadiusWorld { get; private set; }

    // Refs
    BoardPaintSystem paintSystem;
    BoardGrid board;
    Transform player;
    Collider diskCol;

    // Trail 상태
    bool   isRunning;
    bool   haveLast;
    Vector3 lastCenter;

    Coroutine co;

    void Awake()
    {
        player   = transform;
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
            if (player && paintSystem)
            {
                // 1) 기본 반지름 계산(디스크 콜라이더 형태 대응)
                float addWorld = 0f;
                float colliderBaseWorld;
                Vector3 centerNow;

                if (diskCol is CapsuleCollider cap)
                {
                    GetCapsuleWorld(cap, out Vector3 a, out Vector3 b, out float rad);
                    centerNow = (a + b) * 0.5f;
                    colliderBaseWorld = rad;
                }
                else if (diskCol is SphereCollider sph)
                {
                    centerNow = player.position;
                    colliderBaseWorld = GetSphereRadiusWorld(sph);
                }
                else if (diskCol is BoxCollider box)
                {
                    centerNow = player.position;
                    Vector3 e = Vector3.Scale(box.size * 0.5f, player.lossyScale);
                    colliderBaseWorld = Mathf.Sqrt(e.x * e.x + e.z * e.z);
                }
                else
                {
                    centerNow = player.position;
                    colliderBaseWorld = 0f;
                }

                // ── 기준 반지름 선택 ──
                float baseCore = useExplicitBaseRadius ? Mathf.Max(0f, explicitBaseRadiusWorld)
                                                       : Mathf.Max(0f, colliderBaseWorld);
                float rBase = baseCore + addWorld;

                // 최종 잉크 반지름
                float rInk = Mathf.Max(0.01f, rBase * radiusMul + radiusAddWorld);
                CurrentInkRadiusWorld = rInk;

                // ─────────────────────────────────────────────────────────────
                // [PATCH2] 하이브리드 방식
                //   - 정지면 아무 것도 안 찍음
                //   - 이동하면: 헤드 1점 즉시 Now + 구간 백필 EnqueueCircle(거리기반, 프레임캡)
                // ─────────────────────────────────────────────────────────────

                if (!haveLast)
                {
                    // 시작 1회: 가볍게 큐로 보내고 기준점만 세팅
                    StampQueued(centerNow, rInk);
                    lastCenter = centerNow;
                    haveLast   = true;
                }
                else
                {
                    float dist = (centerNow - lastCenter).magnitude;

                    // 정지면 아무 것도 안 찍음 (가만히 있어도 레벨업 방지)
                    if (dist >= minMoveMeters)
                    {
                        // 1) 헤드 1점 즉시
                        paintSystem.HeadStampNow(BoardPaintSystem.PaintChannel.Player,
                                                 centerNow, rInk, /*clearOther=*/true);

                        // 2) 백필: 거리 기반으로 분할 + 프레임 캡
                        float segSpacing = Mathf.Max(0.001f, rInk * Mathf.Max(0.05f, segSpacingMul));
                        int stepsTarget = Mathf.CeilToInt(dist / segSpacing);
                        int steps = Mathf.Min(Mathf.Max(1, stepsTarget), Mathf.Max(1, backfillCapPerFrame));

                        // i=1..steps 구간
                        for (int i = 1; i <= steps; ++i)
                        {
                            float t = (float)i / steps;
                            Vector3 p = Vector3.Lerp(lastCenter, centerNow, t);
                            paintSystem.EnqueueCircle(BoardPaintSystem.PaintChannel.Player,
                                                      p, rInk, /*clearOtherChannel=*/true);
                        }

                        lastCenter = centerNow;
                    }
                }
            }

            yield return null; // 스케일 적용(일시정지 시 멈춤)
        }
    }

    // 시작 1회/리셋 등 '한 번만' 찍는 용도는 큐 사용(과도한 즉시 처리 방지)
    void StampQueued(Vector3 p, float rInk)
    {
        if (!paintSystem) return;
        paintSystem.EnqueueCircle(BoardPaintSystem.PaintChannel.Player,
                                  p, rInk, /*clearOtherChannel=*/true);
    }

    // ===== Helpers =====
    static void GetCapsuleWorld(CapsuleCollider cap, out Vector3 worldA, out Vector3 worldB, out float worldRadius)
    {
        Vector3 cLocal = cap.center;
        Vector3 axisLocal = cap.direction == 0 ? Vector3.right :
                            cap.direction == 1 ? Vector3.up :
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
