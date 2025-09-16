using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panel;                 // GameOver 패널(시작 시 비활성)
    public TextMeshProUGUI timeText;         // 최종 시간 표시용
    public TextMeshProUGUI pointText;
    public Button retryButton;
    public Button MainMenuButton;
    public SurvivalTimerHUD liveTimer;       // HUD의 현재 시간(Text) 복사
    public SurvivalGauge gauge;              // ★ 게이지 이벤트 자동 구독용(인스펙터 연결)
    [SerializeField] string mainMenuSceneName = "A_MainScene";

    [Header("Flow Stop")]
    [Tooltip("게임오버 시 끌 스크립트들(입력/스폰/조작 등)")]
    public Behaviour[] toDisableOnGameOver;
    [Tooltip("게임오버 시 정지할 리지드바디(선택)")]
    public Rigidbody[] toSleepBodies;

    bool gameOver;

    void Awake()
    {
        if (panel) panel.SetActive(false);
    }

    void Start()
    {
        Debug.Log("[GameOverUI] Start called");
        if (retryButton) retryButton.onClick.AddListener(OnClickRetry);
        if (MainMenuButton) MainMenuButton.onClick.AddListener(OnClickMainMenu);

        // ★ 인스펙터 이벤트를 안 걸어도 안전하게 작동하도록 자동 구독
        if (gauge)
        {

            Debug.Log("[GameOverUI] gauge found, adding onDepleted listener");
            gauge.onDepleted.AddListener(ShowGameOver);


        }
        else
        {
            Debug.LogWarning("[GameOverUI] gauge is null!");
        }
    
    }
    void Log(string msg) { Debug.Log($"[GameOverUI] {msg}"); }

    // SurvivalGauge.onDepleted에 연결될 함수

    public void ShowGameOver()
    {
        if (gameOver) return;
        gameOver = true;

        try
        {
            Log("BEGIN");

            // 1) 최종 표시 문자열 & 저장용 시간(밀리초) 계산 — 정지 전에 확보
            string clock = null;
            if (liveTimer && liveTimer.timeText) clock = liveTimer.timeText.text;
            if (timeText) timeText.text = clock ?? "--:--.-";
            int finalTimeMs = 0;
            int finalPoints = 0;
            if (RiskSession.Selected != null)
                finalPoints = RiskSession.Selected.Sum(d => d ? Mathf.Max(0, d.points) : 0);

            if (pointText) pointText.text = $"Tried Point : {finalPoints} PT";


            if (!string.IsNullOrEmpty(clock))
            {
                float secs = TimeUtils.ParseClockToSeconds(clock);
                finalTimeMs = TimeUtils.SecondsToMs(secs);
            }
            Log($"finalTimeMs={finalTimeMs}");

            // 2) 저장 (예외는 잡고 계속 진행)
            try
            {
                if (ProgressManager.Instance != null)
                {
                    ProgressManager.Instance.ReportRunTimeMs(finalTimeMs);
                    Log("Save OK");
                }
                else
                {
                    var go = new GameObject("ProgressManager");
                    go.AddComponent<ProgressManager>(); // Awake에서 Instance 설정 + 로드됨
                }
                ProgressManager.Instance.ReportRunTimeMs(finalTimeMs);
                Log("Save OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameOverUI] Save error: {e}");
            }

            // 3) 외부 흐름 중단 (UI/이벤트 시스템은 끄지 말 것)
            foreach (var m in toDisableOnGameOver)
            {
                if (!m) continue;
                // Canvas, EventSystem, 본인 비활성화 금지
                if (m is Canvas || m is UnityEngine.EventSystems.EventSystem || m == this) continue;
                m.enabled = false;
            }
            foreach (var rb in toSleepBodies)
            {
                if (!rb) continue;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 4) 패널 먼저 띄우고(즉시 보이게), 그 다음 시간정지
            if (panel) panel.SetActive(true);

            AudioListener.pause = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0f;

            Log("END (panel shown)");
        }
        catch (System.Exception e)
        {
            // 어떤 예외가 나도 “패널은 뜨도록” 보정
            Debug.LogError($"[GameOverUI] FATAL: {e}");
            if (panel) panel.SetActive(true);
            AudioListener.pause = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0f;
        }
    }
    void OnClickRetry()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene().name;
        RiskInstaller.EnsureSingleton(scene);
        SceneManager.LoadScene(scene);
    }
    public void OnClickMainMenu()
    {
        // 타임스케일/오디오 복구
        AudioListener.pause = false;
        Time.timeScale = 1f;

        if (IsSceneInBuild(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError(
              $"Scene '{mainMenuSceneName}' is not in Build Settings. " +
              "Open File > Build Profiles and add it to 'Scenes In Build'.");
        }
    }
    static bool IsSceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }
    
    

}
