// AchievementManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        Debug.Log("achievement awake");
        if (closeButton) closeButton.onClick.AddListener(() => panelRoot.SetActive(false));
        if (panelRoot) panelRoot.SetActive(false);
    }

    public void Open()
    {

        RewardDB.EnsureLoaded();
        if (panelRoot) panelRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Refresh();
    }

    public void Refresh()
    {
        Debug.Log($"[Ach] defs={Achievements.Table.Length}, best={ProgressManager.Instance.Data.bestScore}");
        if (!ProgressManager.Instance) return;

        best = ProgressManager.Instance.Data.bestScore;
        maxReq = 1;
        foreach (var a in Achievements.Table)
            if (a.requiredBestScore > maxReq) maxReq = a.requiredBestScore;

        if (bestScoreText) bestScoreText.text = $"Best Point: {best}pt";
        if (progressBar) progressBar.value = Mathf.Clamp01((float)best / maxReq);

        RebuildTicks();
        RebuildRewardList();
    }

    void RebuildTicks()
    {
        Debug.Log($"[Ach] barWidth={barTrack.rect.width}");
        if (!barTrack || !tickContainer || !tickPrefab) return;
        foreach (Transform c in tickContainer) Destroy(c.gameObject);

        float width = barTrack.rect.width; // pivot 0.5 기준 권장
        foreach (var a in Achievements.Table)
        {
            float t = Mathf.Clamp01((float)a.requiredBestScore / maxReq);
            float x = (t - 0.5f) * width;
            Debug.Log($"[Ach] make entry: {a.id} / {a.requiredBestScore}pt");
            var go = Instantiate(tickPrefab, tickContainer).GetComponent<RectTransform>();
            go.anchoredPosition = new Vector2(x, 0f);

            var label = go.GetComponentInChildren<TMP_Text>();
            if (label) label.text = $"{a.requiredBestScore}pt";

            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = (best >= a.requiredBestScore) ? Color.white : new Color(1,1,1,0.4f);
        }
    }

    void RebuildRewardList()
    {
        if (!rewardListParent || !rewardEntryPrefab) return;
        foreach (Transform c in rewardListParent) Destroy(c.gameObject);

        foreach (var a in Achievements.Table)
        {
            var go = Instantiate(rewardEntryPrefab, rewardListParent);
            var entry = go.GetComponent<AchievementRewardEntry>();
            if (entry) entry.Bind(a, ProgressManager.Instance);
        }
    }
}
