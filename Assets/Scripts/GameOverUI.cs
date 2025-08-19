using UnityEngine;
using UnityEngine.UI;
using TMPro;                     // TextMeshPro 쓰면
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panel;     // GameOver 패널 (비활성로 시작)
    public TextMeshProUGUI timeText;
    public Button retryButton;
    public SurvivalTimerHUD liveTimer;

    float startTime;
    bool gameOver;

    void Start()
    {
        startTime = Time.time;
        if (panel) panel.SetActive(false);
        if (retryButton) retryButton.onClick.AddListener(OnClickRetry);
    }

    // SurvivalGauge.onDepleted 에 연결할 함수
    public void ShowGameOver()
    {
        if (panel) panel.SetActive(true);
        if (timeText && liveTimer && liveTimer.timeText)
            timeText.text = liveTimer.timeText.text;

        Time.timeScale = 0f;
    }

    void OnClickRetry()
    {
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(scene);
    }

    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        float seconds = t % 60f;
        return $"{minutes:00}:{seconds:00.0}";
    }
}
