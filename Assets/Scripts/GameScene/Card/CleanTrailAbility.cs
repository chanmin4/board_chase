using UnityEngine;
using System.Collections;

/// 디스크가 지나간 경로를 청소하고(오염0) 같은/다른 반지름으로 플레이어 색을 칠함.
/// - 거리 기반 스탬핑 (프레임 드랍에도 빈틈 최소화)
/// - 프레임 예산으로 스파이크 방지
public class CleanTrailAbility : CardAbility
{
    [Header("Radius Tuning (meters)")]
    [Tooltip("청소 반지름 배수 (1=원래 r)")]
    public float clearRadiusMul = 1f;
    [Tooltip("청소 반지름 추가/감소 값(미터, 음수 가능)")]
    public float clearRadiusAddWorld = 0f;

    [Tooltip("페인트 반지름 배수 (1=원래 r)")]
    public float paintRadiusMul = 0.5f;
    [Tooltip("페인트 반지름 추가/감소 값(미터, 음수 가능)")]
    public float paintRadiusAddWorld = 0f;

    [Header("Card Radius (tiles)")]
    [Tooltip("카드 데이터 추가 반지름(타일 단위)")]
    public float extraRadiusTilesOverride = -1f; // <0 이면 CardData.radiusTiles 사용

    [Header("Trail Sampling")]
    [Tooltip("스탬프 간 간격 = min( rPaint * spacingByRadius , pixelWorld * spacingByPixel )")]
    public float spacingByRadius = 0.7f;
    public float spacingByPixel  = 0.9f;
    [Tooltip("최소 간격(미터) – 너무 조밀해지는 것 방지")]
    public float minSpacingWorld = 0.02f;
    [Tooltip("한 프레임에 최대 몇 번 찍을지(버벅임 방지). 부족분은 다음 프레임으로 이월")]
    [Range(8, 256)] public int maxStampsPerFrame = 64;

    SurvivalDirector director;
    BoardPaintSystem paintSystem;
    Transform player;
    Collider diskCol;
    float extraRadiusTiles;
    Coroutine co;

    // 거리 기반 누적자
    bool   _haveLast;
    Vector3 _lastCenter;
    float   _carryDist; // 이월된 잔여거리

    public override void Activate(Transform playerTf, SurvivalDirector dir, CardData data)
    {
        StopNow();

        player   = playerTf;
        director = dir;

        extraRadiusTiles = (extraRadiusTilesOverride >= 0f)
            ? extraRadiusTilesOverride
            : Mathf.Max(0f, data.radiusTiles);

        if (!player || !director)
        {
            Debug.LogWarning("[CleanTrail] refs missing");
            return;
        }
        paintSystem = FindAnyObjectByType<BoardPaintSystem>();
        if (!paintSystem)
        {
            Debug.LogWarning("[CleanTrail] BoardPaintSystem not found in scene.");
        }
        diskCol = player.GetComponent<Collider>();
        _haveLast  = false;
        _carryDist = 0f;

        IsRunning = true;
        co = StartCoroutine(CleanLoop(data.duration));
    }

    IEnumerator CleanLoop(float duration)
    {
        // 스케일된 시간 기준 → 일시정지 시 멈춤
        float endAt = Time.time + Mathf.Max(0f, duration);

        while (IsRunning && Time.time < endAt)
        {
            if (director && player)
            {
                float addWorld = director.board ? director.board.tileSize * extraRadiusTiles : extraRadiusTiles;

                // 1) 이번 프레임의 ‘중심점’과 ‘기본 반지름’ 산출
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
                    centerNow = player.position;
                    rBase     = GetSphereRadiusWorld(sph) + addWorld;
                }
                else if (diskCol is BoxCollider box)
                {
                    centerNow = player.position;
                    Vector3 e = Vector3.Scale(box.size * 0.5f, player.lossyScale);
                    rBase     = Mathf.Sqrt(e.x * e.x + e.z * e.z) + addWorld;
                }
                else
                {
                    centerNow = player.position;
                    rBase     = addWorld;
                }

                float rClear = Mathf.Max(0.01f, rBase * clearRadiusMul + clearRadiusAddWorld);
                float rPaint = Mathf.Max(0.01f, rBase * paintRadiusMul + paintRadiusAddWorld);

                // 2) 월드 픽셀 크기(플레이어 마스크 해상도) 추정 → spacing 계산
                float pixelWorld = director.board
                    ? (director.board.tileSize / Mathf.Max(1, director.maskRendererPlayerPixelsPerTile()))
                    : 0.05f; // 대충 안전값
                float spacing = Mathf.Max(minSpacingWorld, Mathf.Min(rPaint * spacingByRadius, pixelWorld * spacingByPixel));

                // 3) 지난 프레임 중심에서 지금 중심까지 ‘거리 기반’으로 등간격 스탬핑
                if (!_haveLast)
                {
                    // 첫 프레임: 한 번만 찍고 기준점 세팅
                    Stamp(centerNow, rClear, rPaint);
                    _lastCenter = centerNow;
                    _haveLast   = true;
                }
                else
                {
                    // 지난 프레임 → 현재 프레임까지를 Trail로 한 번에 배치
                    if (paintSystem)
                    {
                        paintSystem.EnqueueTrail(BoardPaintSystem.PaintChannel.Player,
                                                _lastCenter, centerNow,
                                                rPaint,
                                                spacingMeters: -1f,
                                                clearOtherChannel: true);
                    }
                    else
                    {
                        // 혹시 시스템이 없으면 최소한 점 도장으로 대체
                        Stamp(centerNow, rClear, rPaint);
                    }
                    _lastCenter = centerNow;
                }

            }

            yield return null; // 스케일 적용 → 일시정지 시 멈춤
        }

        StopNow();
    }

    void Stamp(Vector3 p, float rClear, float rPaint)
    {
        // “색 덮어쓰기 + 적 마스크 지우기”를 한 번에
        if (paintSystem)
        {
            paintSystem.EnqueueCircle(BoardPaintSystem.PaintChannel.Player,
                                    p, rPaint,
                                    clearOtherChannel: true);
        }
        else if (director) // 폴백: 기존 디렉터 호출
        {
            director.ClearCircleWorld(p, rClear);
            director.PaintPlayerCircleWorld(p, rPaint, applyBoardClean:false, clearPollutionMask:true);
        }
    }
    public override void StopNow()
    {
        if (!IsRunning) return;
        IsRunning = false;
        if (co != null) StopCoroutine(co);
        co = null;
    }

    void OnDisable() => StopNow();

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
