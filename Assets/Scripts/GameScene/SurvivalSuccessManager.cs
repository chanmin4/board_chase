using UnityEngine;
using UnityEngine.UI;               // 버튼
using UnityEngine.SceneManagement;  // 씬 로드
using System.Collections;
using System.Linq;

public class SurvivalSuccessManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("바깥 사각형: 생존성공 패널 (비활성로 시작 권장)")]
    public GameObject successPanel;

    [Tooltip("업적 팝업 매니저 (SuccessPanel 내부 Content에 프리팹을 띄움)")]
    public AchievementPopupManager popupManager;

    [Header("Main Menu")]
    [Tooltip("생존성공 패널 안의 '메인 메뉴' 버튼")]
    public Button mainMenuButton;
    [Tooltip("메인 메뉴 씬 이름 (인스펙터에서 실제 씬명 설정)")]
    public string mainMenuSceneName = "A_MainScene";

    [Header("Gate")]
    public bool oneShot = true;
    bool fired;

    // 이번 판 포인트(메인메뉴 클릭 시 재저장 용도)
    int lastRunPoints = 0;

    [Header("All-Stop Settings")]
    [Tooltip("성공 시 Time.timeScale=0 으로 정지")]
    public bool stopTimeScale = true;
    [Tooltip("성공 시 오디오 정지")]
    public bool pauseAudio = true;
    [Tooltip("성공 시 커서 표시/해제")]
    public bool showCursor = true;
    [Tooltip("성공 시 자동으로 다른 UI(CanvasGroup)를 비활성(클릭 차단)")]
    public bool autoBlockOtherUI = true;

    // 필요하면 개별 Behaviour를 끄고 싶을 때 여기에 등록(플레이어 컨트롤 등)
    [Tooltip("성공 시 비활성화할 컴포넌트들(플레이어 컨트롤 등)")]
    public Behaviour[] disableOnSuccess;
    void Awake()                      // ★ 추가: 시작 시 무조건 비활성
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

    /// <summary>
    /// HUD에서 successSeconds 도달 시 이 메서드만 호출.
    /// </summary>
    public void TriggerSuccess()
    {
        Debug.Log($"[SurvivalSuccessManager] TriggerSuccess() called by {name} (panel={(successPanel?successPanel.name:"null")})");
        if (oneShot && fired) return;
        fired = true;

        // 1) 성공 패널 먼저 보이기
        if (successPanel) successPanel.SetActive(true);

        // 1-1) 전역 입력/시간/오디오/클릭 차단 (올스탑)
        FreezeAndBlockEverythingExceptSuccess();

        // 2) 최고 포인트 갱신(이번 판 리스크 포인트 합으로)
        lastRunPoints = CalcRunPoints();
        if (ProgressManager.Instance) ProgressManager.Instance.ReportRunScore(lastRunPoints);

        // 3) 1초 뒤 해금 가능한 업적들을 낮은 요구 포인트부터 보여주고,
        //    팝업 클릭 시 다음으로 진행
        StartCoroutine(ShowUnlocksSequence());
    }

    IEnumerator ShowUnlocksSequence()
    {
        yield return new WaitForSecondsRealtime(1f);

        if (popupManager == null || ProgressManager.Instance == null)
            yield break;

        var pm = ProgressManager.Instance;

        var list = Achievements.Table
            .Where(a => pm.IsAchievementEligible(a.id) && !pm.IsAchievementClaimed(a.id))
            .OrderBy(a => a.requiredBestScore)
            .ToList();

        foreach (var a in list)
        {
            if (pm.TryClaim(a.id))
            {
                // 내부 아무 곳 클릭 시 닫히고 다음 항목으로
                yield return popupManager.ShowOnce($"업적 해금: {a.title}", a.description, true, 0.15f);
            }
        }
    }

    int CalcRunPoints()
    {
        if (RiskSession.Selected == null) return 0;
        return RiskSession.Selected.Where(d => d != null).Sum(d => Mathf.Max(0, d.points));
    }

    // ─────────────────────────────────────────────────────────────
    // 메인메뉴 버튼
    void OnClickMainMenu()
    {
        // 혹시라도 한 번 더 저장
        if (ProgressManager.Instance)
            ProgressManager.Instance.ReportRunScore(lastRunPoints);

        // 정지 해제
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
            Debug.LogWarning("[SurvivalSuccessManager] mainMenuSceneName 이 비어있습니다.");
    }

    // ─────────────────────────────────────────────────────────────
    // "올스탑" 핵심
    void FreezeAndBlockEverythingExceptSuccess()
    {
        // 시간/오디오/커서
        if (stopTimeScale) Time.timeScale = 0f;
        if (pauseAudio)    AudioListener.pause = true;

        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // 등록한 컴포넌트 끄기(플레이어 입력 등)
        if (disableOnSuccess != null)
        {
            foreach (var b in disableOnSuccess)
                if (b) b.enabled = false;
        }

        // 다른 UI 전부 클릭/상호작용 차단 (성공 패널 제외)
        if (autoBlockOtherUI && successPanel)
        {
            var groups = Object.FindObjectsByType<CanvasGroup>(
                FindObjectsInactive.Include,        // 비활성 포함
                FindObjectsSortMode.None            // 정렬 불필요 시 성능상 유리
            );
            foreach (var g in groups)
            {
                if (!g) continue;
                if (g.transform.IsChildOf(successPanel.transform)) continue; // 성공 패널은 살려둠
                g.interactable = false;
                g.blocksRaycasts = false;
            }

            // 성공 패널이 화면 전체 클릭을 가로채도록 보장(배경 이미지를 raycastTarget=true로)
            var img = successPanel.GetComponent<Image>();
            if (!img) img = successPanel.AddComponent<Image>();
            img.raycastTarget = true;                // 아래 UI 클릭 차단
            if (img.color.a == 0f) img.color = new Color(0, 0, 0, 0); // 완전 투명 유지
        }
    }
}
