// 상단 using 유지
using UnityEngine;
using TMPro;

public class SurvivalTimerHUD : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    [Header("Success Test")]
    public float successSeconds = 300f;
    public SurvivalSuccessManager successManager;
    public bool enableSuccessCheck = true;

    [Header("Pause(Debug)")]
    public bool paused = false;

    float startTime;
    float frozenElapsed;
    bool successFired;

    void Start()
    {
        startTime = Time.time;
        frozenElapsed = 0f;

        // ★ 중복 존재 디텍트 (경고만)
        #if UNITY_2023_1_OR_NEWER
        var all = Object.FindObjectsByType<SurvivalTimerHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        #else
        var all = Object.FindObjectsOfType<SurvivalTimerHUD>(true);
        #endif
        if (all.Length > 1)
            Debug.LogWarning($"[SurvivalTimerHUD] {all.Length}개가 씬에 존재합니다. 값 충돌 가능. (이 인스턴스: {name})");
    }

    void Update()
    {
        float t = paused ? frozenElapsed : (Time.time - startTime);
        if (timeText) timeText.text = FormatTime(t);

        if (!paused && enableSuccessCheck && !successFired && t >= successSeconds)
        {
            successFired = true;
            Debug.Log($"[SurvivalTimerHUD] SUCCESS fired at t={t:0.00}s (threshold={successSeconds}) by {name}");
            successManager?.TriggerSuccess();
        }
    }

    string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        float s = t % 60f;
        return $"{m:00}:{s:00.0}";
    }

    public void PauseClock(bool v)
    {
        if (paused == v) return;
        if (v) { frozenElapsed = Time.time - startTime; paused = true; }
        else { startTime = Time.time - frozenElapsed; paused = false; }
    }
    public float ElapsedSeconds()
    {
        return paused ? frozenElapsed : (Time.time - startTime);
    }

}
