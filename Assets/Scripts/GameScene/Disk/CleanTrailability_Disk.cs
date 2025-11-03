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
                float addWorld = 0;

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

                // ── ★ 기준 반지름 선택: 명시적 기준값 vs 콜라이더 기반 ──
                float baseCore = useExplicitBaseRadius ? Mathf.Max(0f, explicitBaseRadiusWorld)
                                                    : Mathf.Max(0f, colliderBaseWorld);

                // ── addWorld(콤보/타일 보정)는 기준에 더해진 후 배수/가산 적용 ──
                float rBase = baseCore + addWorld;

                // 최종 잉크 반지름
                float rInk = Mathf.Max(0.01f, rBase * radiusMul + radiusAddWorld);

                // (선택) 디버그용 공개 값 업데이트
                CurrentInkRadiusWorld = rInk;

                // ★ 0) 현재 위치 즉시 스탬프(프레임 드랍에도 발밑은 바로 칠해짐)
                paintSystem.HeadStampNow(BoardPaintSystem.PaintChannel.Player,
                                         centerNow, rInk, /*clearOther=*/true);

                // 1) 첫 프레임은 한 번만 찍고 기준점 세팅
                if (!haveLast)
                {
                    Stamp(centerNow, rInk);
                    lastCenter = centerNow;
                    haveLast = true;
                }
                else
                {
                    // 2) 지난 프레임→현재 프레임을 Trail로 배치(빈틈 보강)
                    paintSystem.EnqueueTrail(BoardPaintSystem.PaintChannel.Player,
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

        // 즉시 반영(게이지 체크 포함)
        paintSystem.HeadStampNow(BoardPaintSystem.PaintChannel.Player,
                                 p, rInk, /*clearOther=*/true);
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
