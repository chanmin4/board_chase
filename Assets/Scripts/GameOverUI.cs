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

        // 1) 최종 시간 표시 (HUD에서 그대로 복사)
        if (timeText && liveTimer && liveTimer.timeText)
            timeText.text = liveTimer.timeText.text;

        // 2) 외부 흐름 중단
        Time.timeScale = 0f;             // 물리/Update 대부분 정지
        AudioListener.pause = true;      // 오디오 일시정지
        Cursor.visible = true;           // UI 조작 가능하도록
        Cursor.lockState = CursorLockMode.None;

        foreach (var m in toDisableOnGameOver)
            if (m) m.enabled = false;

        foreach (var rb in toSleepBodies)
            if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        // 3) 패널 활성화
        if (panel) panel.SetActive(true);

        Debug.Log("[GameOverUI] ShowGameOver invoked");
    }

    void OnClickRetry()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(scene);
    }
}
