// AchievementManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections; 
public class AchievementManager : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;
    public Button closeButton;

    [Header("Progress Bar")]
    public Slider progressBar;
    public TMP_Text bestScoreText;
    public RectTransform barTrack;
    public RectTransform tickContainer;
    public GameObject tickPrefab; // (Image + TMP_Text 자식 선택)

    [Header("Reward List")]
    public Transform rewardListParent;
    public GameObject rewardEntryPrefab;

    int best;
    int maxReq;

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(() => panelRoot.SetActive(false));
        if (panelRoot && panelRoot != gameObject)
            panelRoot.SetActive(false);
    }

    public void Open()
{
        // 혹시 모를 지연 로딩이 있다면 여기서 보장
        // RewardDB.EnsureLoaded();
    if (!gameObject.activeSelf)
        gameObject.SetActive(true); // 이 줄이 핵심

    // 2) 패널 켜기
    if (panelRoot && !panelRoot.activeSelf)
        panelRoot.SetActive(true);

    StartCoroutine(OpenRoutine());
}

IEnumerator OpenRoutine()
{
    Debug.Log("[AchMgr] Open()");
    // (여기 오기 전에 이미 panelRoot는 켜진 상태)

    yield return null;            // 활성 반영 대기
    Refresh();

    RectTransform contentRT = rewardListParent as RectTransform;
    if (!contentRT && panelRoot) contentRT = panelRoot.GetComponentInChildren<RectTransform>(true);
    if (contentRT) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);

    Canvas.ForceUpdateCanvases();

    Debug.Log("[AchMgr] Open() done");
}


    public void Refresh()
    {
        if (!ProgressManager.Instance) return;

        best = ProgressManager.Instance.Data.bestScore;

        var list = RewardDB.All;
        maxReq = Mathf.Max(1, list.Count > 0 ? list.Max(r => r.requiredBestScore) : 1);

        if (bestScoreText) bestScoreText.text = $"Best Point: {best}pt";
        if (progressBar) progressBar.value = Mathf.Clamp01((float)best / maxReq);

        RebuildTicks(list);
        RebuildRewardList(list);
    }

    void RebuildTicks(System.Collections.Generic.IReadOnlyList<RewardSO> list)
    {
        if (!barTrack || !tickContainer || !tickPrefab) return;
        foreach (Transform c in tickContainer) Destroy(c.gameObject);

        float width = barTrack.rect.width; // pivot 0.5 권장
        foreach (var so in list)
        {
            float t = Mathf.Clamp01((float)so.requiredBestScore / maxReq);
            float x = (t - 0.5f) * width;

            var go = Instantiate(tickPrefab, tickContainer).GetComponent<RectTransform>();
            go.anchoredPosition = new Vector2(x, 0f);

            var label = go.GetComponentInChildren<TMP_Text>();
            if (label) label.text = $"{so.requiredBestScore}pt";

            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = (best >= so.requiredBestScore) ? Color.white : new Color(1, 1, 1, 0.4f);
        }
    }

    void RebuildRewardList(System.Collections.Generic.IReadOnlyList<RewardSO> list)
    {
        if (!rewardListParent || !rewardEntryPrefab) return;
        foreach (Transform c in rewardListParent) Destroy(c.gameObject);

        foreach (var so in list)
        {
            var go = Instantiate(rewardEntryPrefab, rewardListParent);
            var entry = go.GetComponent<AchievementRewardEntry>();
            if (entry) entry.Bind(so, ProgressManager.Instance);
        }
    }
}
