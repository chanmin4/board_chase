using System.Collections.Generic;
using UnityEngine;
using System;
/// 모든 "원 도장" 요청을 프레임 단위로 배치 처리하는 공용 시스템.
/// - 주체(플레이어/적)는 EnqueueCircle / EnqueueTrail 만 호출
/// - 실제 텍스처 쓰기는 BoardMaskRenderer의 배치 API를 통해 1프레임 1회 Apply
public class BoardPaintSystem : MonoBehaviour
{
    public enum PaintChannel { Enemy, Player }

    [Header("Refs")]
    public BoardGrid board;
    public BoardMaskRenderer maskRenderer;     // Board 위 오염/플레이어 마스크를 그리는 기존 렌더러
    public SurvivalGauge survivalgauge;
    public EnemyInkGauge enemyinkgauge;

    [Header("Trail Sampling")]
    [Tooltip("스탬프 간격 = min( r * spacingByRadius , worldPixel * spacingByPixel )")]
    public float spacingByRadius = 0.7f;
    public float spacingByPixel = 0.9f;
    [Tooltip("최소 간격(너무 조밀한 샘플링 방지)")]
    public float minSpacingWorld = 0.02f;

    [Header("Frame Budget")]
    [Range(8, 512)] public int maxStampsPerFrame = 128;   // 프레임 스파이크 방지용 상한
    public static event Action<float, bool, Vector3, float> OnPlayerPaintStamp;
    public static event Action<float, bool, Vector3, float> OnEnemyPaintStamp;
    [SerializeField] bool headStampImmediate = true;
    [SerializeField] float headStampMinInterval = 0f; // 0이면 매 프레임 허용
    float _nextHeadStampPlayer, _nextHeadStampEnemy;


    // ===== 내부 큐 =====
    struct CircleCmd
    {
        public PaintChannel ch;
        public Vector3 worldPos;
        public float radiusWorld;
        public bool clearOther; // Player가 Enemy 지울지 여부
        public int priority;    // 나중 확장용
        public float stampMeters; // 이 도장을 1개 찍을 때 ‘이동한 길이’로 간주할 값
        public float widthMul;    // 굵기 가중치(필요 없으면 1)
    }

    struct TrailCmd
    {
        public PaintChannel ch;
        public Vector3 prev;
        public Vector3 now;
        public float baseRadiusWorld;
        public bool clearOther;
        public int priority;

        // 누적 거리(이월)
        public float carryDist;
    }

    readonly List<CircleCmd> _circleQueue = new();//최종적으로 찍는 큐
    readonly List<TrailCmd> _trailQueue = new();//circle로 변환하기위한큐

    void Awake()
    {
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!maskRenderer) maskRenderer = FindAnyObjectByType<BoardMaskRenderer>();
        if (!board || !maskRenderer)
            Debug.LogWarning("[BoardPaintSystem] Board or MaskRenderer missing. Please assign.");
        if (!survivalgauge) survivalgauge = FindAnyObjectByType<SurvivalGauge>();
        if (!enemyinkgauge) enemyinkgauge = FindAnyObjectByType<EnemyInkGauge>();
    }

    // ========== 외부 API ==========
    public void EnqueueCircle(PaintChannel ch, Vector3 worldPos, float radiusWorld,
                              bool clearOtherChannel = true, int priority = 0)
    {

        float rW = Mathf.Max(0.001f, radiusWorld);
        float worldPixel = (board && maskRenderer)
        ? board.tileSize / Mathf.Max(1, maskRenderer.PlayerPixelsPerTile)
        : minSpacingWorld;
        float widthMul = 1f;
        float spacing = Mathf.Max(
        minSpacingWorld,
        Mathf.Min(rW * spacingByRadius, worldPixel * spacingByPixel)
        );
        _circleQueue.Add(new CircleCmd
        {

            ch = ch,
            worldPos = worldPos,
            radiusWorld = Mathf.Max(0.001f, radiusWorld),
            clearOther = clearOtherChannel,
            priority = priority,
            stampMeters = spacing,
            widthMul = widthMul
        });
    }

    /// prev→now 구간을 등간격으로 분해하여 도장(거리 기반, 프레임 드랍에도 빈틈 최소)
    public void EnqueueTrail(PaintChannel ch, Vector3 prev, Vector3 now, float baseRadiusWorld,
                             float spacingMeters = -1f, bool clearOtherChannel = true, int priority = 0)
    {
        _trailQueue.Add(new TrailCmd
        {
            ch = ch,
            prev = prev,
            now = now,
            baseRadiusWorld = Mathf.Max(0.001f, baseRadiusWorld),
            clearOther = clearOtherChannel,
            priority = priority,
            carryDist = 0f
        });
    }

    // ========== 배치 처리 ==========
    void LateUpdate()
    {
        if (!maskRenderer || !board) { _circleQueue.Clear(); _trailQueue.Clear(); return; }

        // 60fps 기준으로 프레임이 늘어지면 예산을 자동 확대
        int baseBudget = Mathf.Max(8, maxStampsPerFrame);
        float factor = Mathf.Clamp(Time.unscaledDeltaTime / (1f / 60f), 1f, 6f);
        int budget = Mathf.CeilToInt(baseBudget * factor);

        // ── Trail을 Circle로 확장 ───────────────────────────────
        // (바로 직전 위치와 너무 가까운 스탬프는 생략해서 중복 줄임)
        const float MERGE_EPS = 0.05f; // 월드거리 (반경이 더 크면 자동으로 더 널널)
        Vector3 lastQueuedPos = (_circleQueue.Count > 0) ? _circleQueue[_circleQueue.Count - 1].worldPos : new Vector3(99999, 99999, 99999);

        for (int i = 0; i < _trailQueue.Count && budget > 0; i++)
        {
            var t = _trailQueue[i];

            float worldPixel = board.tileSize / Mathf.Max(1, maskRenderer.PlayerPixelsPerTile);
            float spacing = Mathf.Max(minSpacingWorld,
                                      Mathf.Min(t.baseRadiusWorld * spacingByRadius,
                                                worldPixel * spacingByPixel));

            Vector3 delta = t.now - t.prev;
            float dist = delta.magnitude;
            float need = t.carryDist + dist;

            if (need >= spacing * 0.5f)
            {
                Vector3 dir = (dist > 1e-4f) ? delta / dist : Vector3.zero;
                float d = spacing - t.carryDist; if (d < 0f) d = 0f;

                int placed = 0;
                while (d <= dist && budget > 0)
                {
                    Vector3 p = t.prev + dir * d;

                    // 중복 컷팅: 마지막 찍힌 곳과 너무 가깝고 반경이 크면 생략
                    if ((p - lastQueuedPos).sqrMagnitude > (MERGE_EPS * MERGE_EPS))
                    {
                        _circleQueue.Add(new CircleCmd
                        {
                            ch = t.ch,
                            worldPos = p,
                            radiusWorld = t.baseRadiusWorld,
                            clearOther = t.clearOther,
                            priority = t.priority,
                            stampMeters = spacing,
                            widthMul = 1f
                        });
                        lastQueuedPos = p;
                        placed++; budget--;
                    }
                    d += spacing;
                }

                float used = (placed > 0) ? (d - spacing) : 0f;
                t.carryDist = Mathf.Max(0f, dist - used);
            }
            else t.carryDist = need;

            t.prev = t.now;
            _trailQueue[i] = t;
        }

        // ── Circle 큐 실제 처리 ───────────────────────────────
        // (잔여 예산만큼 처리 + 약간의 시간 슬라이스로 캐치업)
        int toProcess = Mathf.Min(_circleQueue.Count, budget);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < toProcess; i++)
        {
            var c = _circleQueue[i];
            bool isenemyink = maskRenderer.IsEnemyPaintedWorld(c.worldPos);
            bool isplayerink = maskRenderer.IsPlayerPaintedWorld(c.worldPos);

            switch (c.ch)
            {
                case PaintChannel.Enemy:
                    if (enemyinkgauge != null &&
                        !enemyinkgauge.TryConsumeByPaint(c.stampMeters, isenemyink, c.widthMul))
                        continue; // 잉크 부족 → 스킵
                    maskRenderer.EnemyCircleWorld_Batched(c.worldPos, c.radiusWorld);
                    OnEnemyPaintStamp?.Invoke(c.stampMeters, isplayerink, c.worldPos, c.radiusWorld);
                    break;

                case PaintChannel.Player:
                    if (survivalgauge != null &&
                        !survivalgauge.TryConsumeByPaint(c.stampMeters, isplayerink, c.widthMul))
                        continue;
                    maskRenderer.PaintPlayerCircleWorld_Batched(c.worldPos, c.radiusWorld, c.clearOther);
                    OnPlayerPaintStamp?.Invoke(c.stampMeters, isenemyink, c.worldPos, c.radiusWorld);
                    break;
            }
        }
        if (toProcess > 0) _circleQueue.RemoveRange(0, toProcess);

        // 남은 큐가 많으면 1ms 정도 더 처리(프레임 회복 캐치업)
        double timeBudgetMs = 1.0;
        while (_circleQueue.Count > 0 && sw.Elapsed.TotalMilliseconds < timeBudgetMs)
        {
            var c = _circleQueue[0];
            // (동일 처리 루틴; 중복이라 간단히 호출로 빼도 OK)
            if (c.ch == PaintChannel.Enemy)
            {
                bool isenemyink = maskRenderer.IsEnemyPaintedWorld(c.worldPos);
                if (enemyinkgauge == null || enemyinkgauge.TryConsumeByPaint(c.stampMeters, isenemyink, c.widthMul))
                    maskRenderer.EnemyCircleWorld_Batched(c.worldPos, c.radiusWorld);
            }
            else
            {
                bool isplayerink = maskRenderer.IsPlayerPaintedWorld(c.worldPos);
                if (survivalgauge == null || survivalgauge.TryConsumeByPaint(c.stampMeters, isplayerink, c.widthMul))
                    maskRenderer.PaintPlayerCircleWorld_Batched(c.worldPos, c.radiusWorld, c.clearOther);
            }
            _circleQueue.RemoveAt(0);
        }
        sw.Stop();
    }
    public bool TryStampCircleNow(PaintChannel ch, Vector3 posW, float radiusW,
                              bool clearOther, float stampMeters = -1f, float widthMul = 1f)
    {
        if (!maskRenderer || !board) return false;

        // 기본 소모량(프레임 드랍 시 과소/과다를 막기 위해 기존 규칙에 맞춰 대략 계산)
        if (stampMeters <= 0f)
        {
            float worldPixel = board.tileSize / Mathf.Max(1, maskRenderer.PlayerPixelsPerTile);
            // 기존 spacing 규칙에 준하는 보수적 최소치
            stampMeters = Mathf.Max(minSpacingWorld,
                                    radiusW * spacingByRadius * 0.5f,
                                    worldPixel * spacingByPixel * 0.5f);
        }

        switch (ch)
        {
            case PaintChannel.Player:
                {
                    bool isEnemyHere = maskRenderer.IsEnemyPaintedWorld(posW);
                    // 게이지 체크(실패 시 false 반환, 찍지 않음)
                    if (survivalgauge != null &&
                        !survivalgauge.TryConsumeByPaint(stampMeters, /*alreadyMine=*/maskRenderer.IsPlayerPaintedWorld(posW), widthMul))
                        return false;

                    maskRenderer.PaintPlayerCircleWorld_Batched(posW, radiusW, clearOther);
                    OnPlayerPaintStamp?.Invoke(stampMeters, isEnemyHere, posW, radiusW);
                    return true;
                }

            case PaintChannel.Enemy:
                {
                    bool isPlayerHere = maskRenderer.IsPlayerPaintedWorld(posW);
                    if (enemyinkgauge != null &&
                        !enemyinkgauge.TryConsumeByPaint(stampMeters, /*alreadyMine=*/maskRenderer.IsEnemyPaintedWorld(posW), widthMul))
                        return false;

                    maskRenderer.EnemyCircleWorld_Batched(posW, radiusW);
                    OnEnemyPaintStamp?.Invoke(stampMeters, isPlayerHere, posW, radiusW);
                    return true;
                }
        }
        return false;
    }


    public void HeadStampNow(PaintChannel ch, Vector3 centerW, float radiusW, bool clearOther)
    {
        if (!headStampImmediate) return;
        float now = Time.unscaledTime;

        if (ch == PaintChannel.Player)
        {
            if (now < _nextHeadStampPlayer) return;
            TryStampCircleNow(ch, centerW, radiusW, clearOther,stampMeters: 0f);
            _nextHeadStampPlayer = now + headStampMinInterval;
        }
        else
        {
            if (now < _nextHeadStampEnemy) return;
            TryStampCircleNow(ch, centerW, radiusW, clearOther,stampMeters: 0f);
            _nextHeadStampEnemy = now + headStampMinInterval;
        }
    }


}
