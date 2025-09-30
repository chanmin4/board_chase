using UnityEngine;

public class SlowMotionDrag : MonoBehaviour
{
    [Header("Refs")]
    public DragAimController drag;

    [Header("Slow Motion")]
    [Range(0.02f, 1f)] public float slowScale = 0.30f;   // 드래그 중 타임스케일
    public float blendInSeconds  = 0.08f;                // 슬로우로 들어갈 때 블렌드
    public float blendOutSeconds = 0.10f;                // 원복 블렌드
    public bool scaleFixedDelta  = true;                 // 물리 fixedDeltaTime도 스케일링

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
    }

    void OnDragEnd()
    {
        _armed = false;
        _targetScale = 1f;
    }

    void Update()
    {
        // 블렌드는 unscaledDeltaTime 기준으로—슬로우 중에도 동일 속도로 섞이도록
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
    }

    void SetScaleImmediate(float s)
    {
        Time.timeScale = Mathf.Clamp(s, 0.01f, 1f);
        if (scaleFixedDelta)
            Time.fixedDeltaTime = _baseFixed * Time.timeScale;
    }
}
