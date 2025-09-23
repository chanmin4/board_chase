using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AchievementPopupManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject achievePanel;     // 오버레이 (비활성 시작 권장)
    public Transform panelContent;      // ScrollView/Viewport/Content
    public Button panelClickAnywhere;   // 패널 어디나 클릭

    [Header("Prefab")]
    public GameObject popupPrefab;      // 내부에 AchievementPopupPrefab 컴포넌트

    [Header("Timing (Queue mode)")]
    public float showSeconds = 2.0f;
    public float gapSeconds  = 0.3f;

    [Header("Debug")]
    public bool verboseLogs = false;
    public bool deactivateTemplateOnStart = true;

    readonly Queue<(string title, string desc, Sprite image)> q = new();
    bool playing;
    int activePopupCount = 0; // 현재 떠 있는 팝업 수

    void Awake()
    {
        if (achievePanel && achievePanel.activeSelf) achievePanel.SetActive(false);

        if (popupPrefab && deactivateTemplateOnStart && popupPrefab.scene.IsValid() && popupPrefab.activeSelf)
        {
            if (verboseLogs) Debug.Log($"[AchPopup] Deactivate scene template '{popupPrefab.name}'");
            popupPrefab.SetActive(false);
        }
    }

    // ProgressManager 상태 기반으로 “수령 대기”만 만든 뒤 팝업 알림
    public IEnumerator ShowEligibleUnlocksFromProgress(ProgressManager pm, bool requireClick = true, float minShowSeconds = 0.1f)
    {
        if (!pm) yield break;

        RewardDB.EnsureLoaded();

        var list = RewardDB.All
        .Where(so => so && pm.IsUnlockable(so.id))
        .OrderBy(so => so.requiredBestScore)
        .ToList();
        if (list.Count == 0) yield break;

        EnsurePanel(true);

        foreach (var so in list)
        {
            pm.UnlockByAchievementId(so.id);
            string title = string.IsNullOrEmpty(so.title) ? "Achievement Unlocked!" : so.title;
            string desc  = so.description ?? "";
            yield return ShowOnce(title, desc, so.icon, requireClick, minShowSeconds);
        }

        MaybeClosePanel();
    }

    public void Enqueue(string title, string desc, Sprite image = null)
    {
        q.Enqueue((title, desc, image));
        if (!playing) StartCoroutine(PlayQueue());
    }

    public IEnumerator ShowOnce(string title, string desc, Sprite image = null, bool requireClick = true, float minShowSeconds = 0f)
    {
        EnsurePanel(true);

        var refs = Spawn();
        if (!refs) { EnsurePanel(false); yield break; }

        if (refs.titleText)    refs.titleText.text = title;
        if (refs.descText)     refs.descText.text  = desc;
        if (refs.achieveImage) refs.achieveImage.sprite = image;

        bool clicked = false;

        if (requireClick && refs.clickAnywhereButton)
        {
            refs.clickAnywhereButton.onClick.RemoveAllListeners();
            refs.clickAnywhereButton.onClick.AddListener(() => clicked = true);
        }
        if (requireClick && panelClickAnywhere)
        {
            panelClickAnywhere.onClick.RemoveAllListeners();
            panelClickAnywhere.onClick.AddListener(() => clicked = true);
        }

        refs.root.SetActive(true);

        float t = 0f;
        while (true)
        {
            bool timeOk = t >= minShowSeconds;
            if (!requireClick && timeOk) break;
            if (requireClick && timeOk && clicked) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (verboseLogs) Debug.Log($"[AchPopup] Destroy '{refs.gameObject.name}'");
        Destroy(refs.gameObject);

        activePopupCount = Mathf.Max(0, activePopupCount - 1);
        MaybeClosePanel();
    }

    // 구버전 호환 오버로드
    public IEnumerator ShowOnce(string title, string desc, bool requireClick, float minShowSeconds)
        => ShowOnce(title, desc, null, requireClick, minShowSeconds);

    IEnumerator PlayQueue()
    {
        playing = true;
        EnsurePanel(true);

        while (q.Count > 0)
        {
            var (t, d, img) = q.Dequeue();
            var refs = Spawn();
            if (!refs) break;

            if (refs.titleText)    refs.titleText.text = t;
            if (refs.descText)     refs.descText.text  = d;
            if (refs.achieveImage) refs.achieveImage.sprite = img;

            refs.root.SetActive(true);
            yield return new WaitForSecondsRealtime(showSeconds);
            if (verboseLogs) Debug.Log($"[AchPopup] Destroy '{refs.gameObject.name}'");
            Destroy(refs.gameObject);

            activePopupCount = Mathf.Max(0, activePopupCount - 1);
            yield return new WaitForSecondsRealtime(gapSeconds);
        }

        playing = false;
        MaybeClosePanel();
    }

    AchievementPopupPrefab Spawn()
    {
        if (!popupPrefab)
        {
            Debug.LogWarning("[AchPopup] popupPrefab not set");
            return null;
        }

        Transform parent = panelContent ? panelContent : (achievePanel ? achievePanel.transform : transform);

        var inst  = Instantiate(popupPrefab, parent, false);
        inst.name = $"{popupPrefab.name}_Instance";
        inst.transform.SetAsLastSibling();

        if (verboseLogs) Debug.Log($"[AchPopup] Instantiated '{inst.name}' under '{parent.name}'");

        var refs = inst.GetComponent<AchievementPopupPrefab>() ?? inst.AddComponent<AchievementPopupPrefab>();
        if (!refs.root) refs.root = inst;

        if (!refs.titleText || !refs.descText || !refs.clickAnywhereButton)
        {
            var tmps = inst.GetComponentsInChildren<TMP_Text>(true);
            if (!refs.titleText && tmps.Length > 0) refs.titleText = tmps[0];
            if (!refs.descText  && tmps.Length > 1) refs.descText  = tmps[1];

            var buttons = inst.GetComponentsInChildren<Button>(true);
            if (!refs.clickAnywhereButton && buttons.Length > 0) refs.clickAnywhereButton = buttons[0];
        }

        var rt = inst.GetComponent<RectTransform>();
        if (rt)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }

        refs.root.SetActive(false);
        activePopupCount++;
        return refs;
    }

    void EnsurePanel(bool on)
    {
        if (!achievePanel) return;
        if (on && !achievePanel.activeSelf) achievePanel.SetActive(true);
        if (!on && achievePanel.activeSelf) achievePanel.SetActive(false);
    }

    void MaybeClosePanel()
    {
        if (!achievePanel) return;
        StartCoroutine(CoMaybeClosePanel());
    }

    IEnumerator CoMaybeClosePanel()
    {
        yield return null; // Destroy 반영 프레임 대기

        bool noPopupAlive   = (activePopupCount <= 0) && !playing;
        bool emptyByContent = !panelContent || panelContent.childCount == 0;

        if (verboseLogs) Debug.Log($"[AchPopup] CloseCheck noPopupAlive={noPopupAlive}, emptyByContent={emptyByContent}");

        if (noPopupAlive && emptyByContent)
        {
            if (panelClickAnywhere) panelClickAnywhere.onClick.RemoveAllListeners(); // ← 닫을 때만 제거
            EnsurePanel(false);
        }
    }

    [ContextMenu("DEBUG_Spawn test popup")]
    void DEBUG_Spawn()
    {
        StartCoroutine(ShowOnce("업적 해금!", "테스트 팝업입니다.", null, true, 0.1f));
    }
}
