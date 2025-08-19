using UnityEngine;
using TMPro;

public class SurvivalTimerHUD : MonoBehaviour
{
    public TextMeshProUGUI timeText;   // Canvas의 TMP 텍스트 연결
    public SurvivalGauge gauge;        // 게이지 연결(같은 씬 오브젝트)

    float startTime;
    bool stopped;

    void Start()
    {
        startTime = Time.time;
        if (gauge != null)
        {
            // 게이지가 비면 시간 멈추도록 이벤트 연결
            gauge.onDepleted.AddListener(StopTimer);
        }
    }

    void Update()
    {
        if (stopped) return;
        float t = Time.time - startTime;
        if (timeText) timeText.text = FormatTime(t);
    }

    void StopTimer()
    {
        stopped = true;
        // 필요 시 여기서 최종 시간을 기록해서 GameOverUI에 넘겨도 됨
    }

    string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        float s = t % 60f;
        return $"{m:00}:{s:00.0}";
    }
}
