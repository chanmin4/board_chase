using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public string StartButtonSceneName = "RiskSelectionScene";
    public TMP_Text bestText;
    public Button StartButton;
    public Button QuitButton;
    public Button SettingButton;
    public Button AchievementButton;
    public GameObject SettingPanel;
    public GameObject AchievementPanel;

    [Header("Reset Options")]
    [SerializeField] bool resetSaveWhenStarting = false;    // ← 항상 초기화하고 시작
    [SerializeField] bool allowShiftToReset = false;         // ← Shift 누른 채 Start 시 초기화
    [SerializeField] KeyCode resetModifier = KeyCode.LeftShift;

    void Start()
    {
        if (!ProgressManager.Instance)
        {
            var go = new GameObject("ProgressManager");
            go.AddComponent<ProgressManager>();
        }
        ProgressManager.Instance.OnBestScoreChanged += _ => Refresh(); // 점수형 해금 쓰면 유지
        ProgressManager.Instance.OnBestTimeChangedMs += _ => Refresh(); // ← 추가!

        ProgressManager.Instance.OnBestScoreChanged += _ => Refresh();
        StartButton.onClick.AddListener(OnClickStart);
        QuitButton.onClick.AddListener(OnClickQuit);
        SettingButton.onClick.AddListener(OnClickSetting);
        AchievementButton.onClick.AddListener(OnClickAchievement);
        if (AudioMaster.I) AudioMaster.I.PlayBGMKey("bgm.main");
        Refresh();
    }

    void OnDestroy()
    {
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.OnBestScoreChanged -= _ => Refresh();
    }

    void Refresh()
    {
        if (bestText)
        {
            int ms = ProgressManager.Instance.Data.bestTimeMs;
            bestText.text = $"BEST: {TimeUtils.FormatMsClock(ms, 1)}"; // "03:27.5" 형태
        }

    }
    public void OnClickSetting()
    {
        SettingPanel.SetActive(true);
    }
    public void OnClickStart()
    {
        // ProgressManager 보장
        if (!ProgressManager.Instance)
        {
            var go = new GameObject("ProgressManager");
            go.AddComponent<ProgressManager>();
        }

        bool doReset = resetSaveWhenStarting ||
                       (allowShiftToReset && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)));

        if (doReset)
        {
            // 1) 파일 삭제(완전 초기화)
            bool deleted = SaveSystem.Delete();
            // 2) 메모리 상태도 초기화
            ProgressManager.Instance.ResetProgress(saveFileAfter: false);

            Debug.Log($"[MainMenu] Save reset {(deleted ? "OK" : "(no file)")} — starting fresh");
            // (선택) 화면에서도 바로 비워 보이게
            if (bestText) bestText.text = "BEST: 00:00.0";
        }

        SceneManager.LoadScene(StartButtonSceneName);
    }

    public void OnClickQuit()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
    void OnClickAchievement()
    {
        AchievementPanel.SetActive(true);
    }
}
