using UnityEngine;

public class AnimatorSpeedTester : MonoBehaviour
{
    [Header("Animator Param")]
    public string parameterName = "Speed";
    public float targetSpeed = 0f;      // Inspector에서 바로 조절

    [Header("Keyboard Shortcuts")]
    public bool useKeys = true;
    public float keyStep = 0.2f;        // +/- 증감량

    [Header("Auto Oscillate (optional)")]
    public bool oscillate = false;
    public float oscMin = 0f;
    public float oscMax = 0.2f;         // 0.1 임계 넘기도록 기본 0.2
    public float oscPeriod = 1.5f;      // 왕복 시간(초)

    [Header("Damping")]
    public float dampTime = 0.1f;       // 부드럽게 변경

    Animator _anim;
    int _hash;

    void Awake()
    {
        _anim = GetComponentInChildren<Animator>(true);   // 자식 Animator 캐치
        if (_anim) _hash = Animator.StringToHash(parameterName);
    }

    void Update()
    {
        if (!_anim) return;

        // 키로 빠르게 전환/증감
        if (useKeys)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) targetSpeed = 0f;   // Idle 쪽
            if (Input.GetKeyDown(KeyCode.Alpha2)) targetSpeed = 0.2f; // Move 쪽
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                targetSpeed = Mathf.Max(0f, targetSpeed - keyStep);
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                targetSpeed = Mathf.Min(10f, targetSpeed + keyStep);
        }

        float s = targetSpeed;

        // 자동 왕복
        if (oscillate && oscPeriod > 0f)
        {
            float t = Mathf.PingPong(Time.time / oscPeriod, 1f);
            s = Mathf.Lerp(oscMin, oscMax, t);
        }

        // m/s 그대로 전달 (임계 0.1 기준이면 컨트롤러에서 전환됨)
        _anim.SetFloat(_hash, s, dampTime, Time.deltaTime);
    }
}
