using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

public class SurvivalSuccessManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject successPanel;
    public AchievementPopupManager popupManager;

    [Header("Main Menu")]
    public Button mainMenuButton;
    public string mainMenuSceneName = "A_MainScene";

    [Header("Gate")]
    public bool oneShot = true;
    bool fired;
    int lastRunPoints = 0;

    [Header("All-Stop Settings")]
    public bool stopTimeScale = true;
    public bool pauseAudio = true;
    public bool showCursor = true;
    public bool autoBlockOtherUI = true;

    public GameObject[] dontBlockUIRoots;
    public Behaviour[] disableOnSuccess;

    void Awake()
    {
        if (successPanel) successPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (mainMenuButton)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnClickMainMenu);
        }
    }

    void OnDisable()
    {
        if (mainMenuButton)
            mainMenuButton.onClick.RemoveListener(OnClickMainMenu);
    }

    public void TriggerSuccess()
    {
        if (oneShot && fired) return;
        fired = true;

        if (successPanel) successPanel.SetActive(true);
        if (mainMenuButton) mainMenuButton.interactable = false;
        FreezeAndBlockEverythingExceptSuccess();

        lastRunPoints = CalcRunPoints();
        ProgressManager.Instance?.GameSuccessReportRunScore(lastRunPoints);

        StartCoroutine(ShowUnlocksSequence());
    }

    IEnumerator ShowUnlocksSequence()
    {
        yield return new WaitForSecondsRealtime(1f);

        var pm = ProgressManager.Instance;
        if (pm == null) yield break;

 
        try
        {
            if (popupManager)
            {
                yield return popupManager.ShowEligibleUnlocksFromProgress(pm, true, 0.15f);
            }
            else
            {
                // 폴백: RewardDB 순회 → 수령대기만 등록
                RewardDB.EnsureLoaded();
                var list = RewardDB.All
                    .Where(so => so && pm.IsAchievementClaimable(so.id))
                    .OrderBy(so => so.requiredBestScore)
                    .ToList();

                if (list.Count > 0)
                    Debug.Log($"[Achv] {list.Count} claimable unlock(s) available.");
            }
        }
        finally
        {
            if (mainMenuButton) mainMenuButton.interactable = true;
        }
    }

    int CalcRunPoints()
    {
        if (RiskSession.Selected == null) return 0;
        return RiskSession.Selected.Where(d => d != null).Sum(d => Mathf.Max(0, d.points));
    }

    void OnClickMainMenu()
    {
        ProgressManager.Instance?.GameSuccessReportRunScore(lastRunPoints);
        if (pauseAudio)  AudioListener.pause = false;
        if (stopTimeScale) Time.timeScale = 1f;

        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogWarning("[SurvivalSuccessManager] mainMenuSceneName is empty");
    }

    void FreezeAndBlockEverythingExceptSuccess()
    {
        if (stopTimeScale) Time.timeScale = 0f;
        if (pauseAudio)    AudioListener.pause = true;

        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (disableOnSuccess != null)
            foreach (var b in disableOnSuccess)
                if (b) b.enabled = false;

        if (autoBlockOtherUI && successPanel)
        {
#if UNITY_2023_1_OR_NEWER
            var groups = Object.FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var groups = Object.FindObjectsOfType<CanvasGroup>(true);
#endif
            foreach (var g in groups)
            {
                if (!g) continue;
                bool exempt = g.transform.IsChildOf(successPanel.transform);
                if (!exempt && dontBlockUIRoots != null)
                    foreach (var go in dontBlockUIRoots)
                        if (go && g.transform.IsChildOf(go.transform)) { exempt = true; break; }
                if (exempt) continue;

                g.interactable   = false;
                g.blocksRaycasts = false;
            }

            var img = successPanel.GetComponent<Image>() ?? successPanel.AddComponent<Image>();
            img.raycastTarget = true;
            if (img.color.a == 0f) img.color = new Color(0, 0, 0, 0);
        }
    }
}
