using System.Collections;
using UnityEngine;

/// 카드 사용 시 일정 시간(Time.timeScale) 느리게 했다가 부드럽게 원복.
/// - duration / cooldown 은 CardData 값을 사용.
/// - slowScale, blendIn/Out, fixedDelta 스케일링은 인스펙터에서 튜닝.
[DisallowMultipleComponent]
public class TimeSlowAbility_Card : CardAbility
{
    [Header("Slow Motion")]
    [Range(0.02f, 1f)] public float slowScale = 0.20f;   // 느려질 배수
    public float blendInSeconds  = 0.08f;                // 슬로우로 진입 블렌드
    public float blendOutSeconds = 0.10f;                // 원복 블렌드
    public bool scaleFixedDelta  = true;                 // fixedDeltaTime도 스케일링

    float baseFixed;          // 원래 fixedDeltaTime
    Coroutine runCo;

    void Awake()
    {
        baseFixed = Time.fixedDeltaTime;
    }

    public override void Activate(Transform player, SurvivalDirector director, CardData data)
    {
        // 중복 사용 시 재시작
        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(RunCo(data ? data.duration : 10f));
        IsRunning = true;
    }

    public override void StopNow()
    {
        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(BlendTo(1f, blendOutSeconds)); // 부드럽게 복귀
        IsRunning = false;
    }

    IEnumerator RunCo(float durSec)
    {
        yield return BlendTo(slowScale, blendInSeconds);

        float t = Mathf.Max(0f, durSec);
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime; // 슬로우 중에도 정확한 초로 카운트
            yield return null;
        }

        yield return BlendTo(1f, blendOutSeconds);
        runCo = null;
        IsRunning = false;
    }

    IEnumerator BlendTo(float target, float seconds)
    {
        target = Mathf.Clamp(target, 0.01f, 1f);
        if (seconds <= 0.0001f)
        {
            SetScaleImmediate(target);
            yield break;
        }

        float vel = 0f;
        while (Mathf.Abs(Time.timeScale - target) > 0.001f)
        {
            float next = Mathf.SmoothDamp(Time.timeScale, target, ref vel, seconds,
                                          Mathf.Infinity, Time.unscaledDeltaTime);
            SetScaleImmediate(next);
            yield return null;
        }
        SetScaleImmediate(target);
    }

    void SetScaleImmediate(float s)
    {
        Time.timeScale = s;
        if (scaleFixedDelta) Time.fixedDeltaTime = baseFixed * s;
    }

    void OnDisable()
    {
        // 혹시 남아 있으면 원복
        SetScaleImmediate(1f);
    }

    void OnDestroy()
    {
        SetScaleImmediate(1f);
    }
}
