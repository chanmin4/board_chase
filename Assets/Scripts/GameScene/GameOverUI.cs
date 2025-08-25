using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panel;                 // GameOver 패널(시작 시 비활성)
    public TextMeshProUGUI timeText;         // 최종 시간 표시용
    public Button retryButton;
    public SurvivalTimerHUD liveTimer;       // HUD의 현재 시간(Text) 복사
    public SurvivalGauge gauge;              // ★ 게이지 이벤트 자동 구독용(인스펙터 연결)

    [Header("Flow Stop")]
    [Tooltip("게임오버 시 끌 스크립트들(입력/스폰/조작 등)")]
    public MonoBehaviour[] toDisableOnGameOver;
    [Tooltip("게임오버 시 정지할 리지드바디(선택)")]
    public Rigidbody[] toSleepBodies;

    bool gameOver;

    void Awake()
    {
        if (panel) panel.SetActive(false);
    }

    void Start()
    {
        if (retryButton) retryButton.onClick.AddListener(OnClickRetry);

        // ★ 인스펙터 이벤트를 안 걸어도 안전하게 작동하도록 자동 구독
        if (gauge) gauge.onDepleted.AddListener(ShowGameOver);
    }

    // SurvivalGauge.onDepleted에 연결될 함수
    public void ShowGameOver()
    {
        if (gameOver) return;
        gameOver = true;

        // 1) 최종 시간 표시
        string clock = null;
        if (timeText && liveTimer && liveTimer.timeText)
        {
            clock = liveTimer.timeText.text;
            timeText.text = clock;
        }

        // 2) 저장용 숫자(밀리초)로 변환해 저장
        int finalTimeMs = 0;
        if (!string.IsNullOrEmpty(clock))
        {
            float secs = TimeUtils.ParseClockToSeconds(clock);
            finalTimeMs = TimeUtils.SecondsToMs(secs);
        }
        else
        {
            // HUD 텍스트가 없다면 다른 소스(예: liveTimer.CurrentSeconds)로 대체
            // finalTimeMs = TimeUtils.SecondsToMs(liveTimer.CurrentSeconds);
        }

        // 최고기록 저장
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.ReportRunTimeMs(finalTimeMs);

        // (기존 점수 시스템을 쓰고 싶다면 여전히 ReportRunScore도 호출 가능)
        // ProgressManager.Instance.ReportRunScore(Mathf.RoundToInt(TimeUtils.MsToSeconds(finalTimeMs)));

        // … 이하 기존 정지/패널 활성화 로직 유지
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // …
    }
    void OnClickRetry()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(scene);
    }
}
