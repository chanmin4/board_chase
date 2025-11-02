using UnityEngine;

public class SlowMotionDrag : MonoBehaviour
{
    [Header("Refs")]
    public DragAimController drag;

    [Header("Slow Motion")]
    [Range(0.02f, 1f)] public float slowScale = 0.20f;   // 드래그 중 타임스케일
    public float blendInSeconds  = 0.08f;                // 슬로우로 들어갈 때 블렌드
    public float blendOutSeconds = 0.10f;                // 원복 블렌드
    public bool scaleFixedDelta  = true;                 // 물리 fixedDeltaTime도 스케일링
    [Header("Drag Duration Limit")]
    public bool useDragDurationLimit = true;
    [Tooltip("드래그 유지 최대 시간(실시간, 언스케일드)")]
    public float dragMaxHoldSec = 3f;

    // 내부
    float _dragStartUnscaled;
    float _baseFixed;
    float _targetScale = 1f;
    float _vel; // SmoothDamp용
    bool  _armed;

    void Awake()
    {
        if (!drag) drag = FindAnyObjectByType<DragAimController>();
        _baseFixed = Time.fixedDeltaTime;
    }

    void OnEnable()
    {
        if (drag != null)
        {
            drag.DragPull += OnDragStart;  // 드래그 시작
            drag.DragPush += OnDragEnd;    // 드래그 릴리즈
        }
    }
    void OnDisable()
    {
        if (drag != null)
        {
            drag.DragPull -= OnDragStart;
            drag.DragPush -= OnDragEnd;
        }
        // 혹시 남았으면 원복
        SetScaleImmediate(1f);
    }

    void OnDragStart()
    {
        _armed = true;
        _targetScale = Mathf.Clamp(slowScale, 0.02f, 1f);
         _dragStartUnscaled = Time.unscaledTime;      
    }

    void OnDragEnd()
    {
        _armed = false;
        _targetScale = 1f;
    }

    void Update()
    {
        // 블렌드…
        float t = (_armed ? blendInSeconds : blendOutSeconds);
        if (t <= 0.0001f)
        {
            SetScaleImmediate(_targetScale);
        }
        else
        {
            float cur = Time.timeScale;
            float next = Mathf.SmoothDamp(cur, _targetScale, ref _vel, t, Mathf.Infinity, Time.unscaledDeltaTime);
            SetScaleImmediate(next);
        }

        // ★ 드래그 지속시간 초과 시, 우클릭 취소와 동일 처리
        if (_armed && useDragDurationLimit && (Time.unscaledTime - _dragStartUnscaled) >= dragMaxHoldSec)
        {
            if (drag) drag.CancelDragExternal();             // DragAimController 쪽 메서드 호출
                                                             // OnDragEnd()는 drag가 DragPush를 쏘면서 자동으로 호출됨
        }
    }

    void SetScaleImmediate(float s)
    {
        Time.timeScale = Mathf.Clamp(s, 0.01f, 1f);
        if (scaleFixedDelta)
            Time.fixedDeltaTime = _baseFixed * Time.timeScale;
    }
}
