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

    [Header("Trail Sampling")]
    [Tooltip("스탬프 간격 = min( r * spacingByRadius , worldPixel * spacingByPixel )")]
    public float spacingByRadius = 0.7f;
    public float spacingByPixel  = 0.9f;
    [Tooltip("최소 간격(너무 조밀한 샘플링 방지)")]
    public float minSpacingWorld = 0.02f;

    [Header("Frame Budget")]
    [Range(8, 512)] public int maxStampsPerFrame = 128;   // 프레임 스파이크 방지용 상한
public static event Action<float, bool, Vector3, float> OnPlayerPaintStamp; 
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
    readonly List<TrailCmd>  _trailQueue  = new();//circle로 변환하기위한큐

    void Awake()
    {
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!maskRenderer) maskRenderer = FindAnyObjectByType<BoardMaskRenderer>();
        if (!board || !maskRenderer)
            Debug.LogWarning("[BoardPaintSystem] Board or MaskRenderer missing. Please assign.");
        if (!survivalgauge) survivalgauge = FindAnyObjectByType<SurvivalGauge>(); 
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
        _trailQueue.Add(new TrailCmd {
            ch = ch, prev = prev, now = now,
            baseRadiusWorld = Mathf.Max(0.001f, baseRadiusWorld),
            clearOther = clearOtherChannel, priority = priority,
            carryDist = 0f
        });
    }

    // ========== 배치 처리 ==========
    void LateUpdate()
    {
        if (!maskRenderer || !board) { _circleQueue.Clear(); _trailQueue.Clear(); return; }

        // 1) Trail을 circle로 확장(프레임 예산 고려)
        int budget = Mathf.Max(8, maxStampsPerFrame);

        // trailQueue는 다음 프레임에도 이어질 수 있으므로, 한 번 복사해서 재적재
        for (int i = 0; i < _trailQueue.Count && budget > 0; i++)
        {
            var t = _trailQueue[i];

            // 간격 산출: 반지름 기반 vs 마스크 픽셀 월드 기반 중 더 보수적인 값
            float worldPixel = board.tileSize / Mathf.Max(1, maskRenderer.PlayerPixelsPerTile);
            float spacing = Mathf.Max(minSpacingWorld,
                                      Mathf.Min(t.baseRadiusWorld * spacingByRadius,
                                                worldPixel        * spacingByPixel));

            Vector3 delta = t.now - t.prev;
            float dist    = delta.magnitude;
            float need    = t.carryDist + dist;

            if (need >= spacing * 0.5f)
            {
                Vector3 dir = (dist > 1e-4f) ? delta / dist : Vector3.zero;

                float d = spacing - t.carryDist;
                if (d < 0f) d = 0f;

                int placed = 0;
                while (d <= dist && budget > 0)
                {
                    Vector3 p = t.prev + dir * d;
                    _circleQueue.Add(new CircleCmd
                    {
                        ch = t.ch,
                        worldPos = p,
                        radiusWorld = t.baseRadiusWorld,
                        clearOther = t.clearOther,
                        priority = t.priority,
                        stampMeters = spacing, 
                        widthMul    = 1f       

                    });
                    placed++; budget--;
                    d += spacing;
                }

                float used = (placed > 0) ? (d - spacing) : 0f;
                t.carryDist = Mathf.Max(0f, dist - used);
            }
            else
            {
                t.carryDist = need;
            }

            // prev를 now로 이동 (다음 프레임 이어서)
            t.prev = t.now;
            _trailQueue[i] = t;
        }

        // 2) Circle 큐 처리 → BoardMaskRenderer 배치 API로 전달 (Apply는 렌더러가 LateUpdate에서 1회)
        int toProcess = Mathf.Min(_circleQueue.Count, budget);
        for (int i = 0; i < toProcess; i++)
        {
            var c = _circleQueue[i];
            switch (c.ch)
            {
                case PaintChannel.Enemy:
                    // 오염 찍기
                    maskRenderer.ContaminateCircleWorld_Batched(c.worldPos, c.radiusWorld);
                    break;

                case PaintChannel.Player:
                    // 플레이어 칠하기 (옵션: 적 마스크 지우기)
                    bool isContam = maskRenderer.IsContaminatedWorld(c.worldPos);
                    if (survivalgauge != null)
                    {
                        if (!survivalgauge.TryConsumeByPaint(c.stampMeters, isContam, c.widthMul))
                            break; // 잉크 부족 → 이번 도장 스킵
                    }
                    maskRenderer.PaintPlayerCircleWorld_Batched(c.worldPos, c.radiusWorld, c.clearOther);
                    OnPlayerPaintStamp?.Invoke(c.stampMeters, isContam, c.worldPos, c.radiusWorld);
                    break;
            }
        }

        if (toProcess > 0) _circleQueue.RemoveRange(0, toProcess);
        // trailQueue는 carryDist 누적을 위해 유지
        // (단, “이 프레임에서 더 이상 이동이 없으면” 외부에서 trail을 새로 넣지 말아야 낭비가 없음)
    }
}
