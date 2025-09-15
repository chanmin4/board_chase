// AchievementRewardEntry.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementRewardEntry : MonoBehaviour
{
    public Image icon;            // (없어도 동작)
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text statusText;
    public Button claimButton;

    RewardSO so;
    ProgressManager pm;

    public void Bind(RewardSO reward, ProgressManager manager)
    {
        so = reward;
        pm = manager;

        if (icon)      icon.sprite = reward.icon;
        if (titleText) titleText.text = string.IsNullOrEmpty(reward.title) ? $"{reward.requiredBestScore}pt reward" : reward.title;
        if (descText)  descText.text  = reward.description ?? "";

        RefreshState();

        if (claimButton)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() =>
            {
                if (pm.ClaimAchievement(so.id))
                {
                    RewardDB.GrantVisualOrRuntime(so.id, pm); // 필요 시 후처리
                    RefreshState();
                }
            });
        }
    }

    void RefreshState()
    {
        bool eligible = pm.Data.bestScore >= so.requiredBestScore;
        bool claimed  = pm.IsAchievementClaimed(so.id);

        if (statusText)
            statusText.text = !eligible ? "Not achieved"
                              : (claimed ? "Received" : "Achieved");

        if (claimButton)
        {
            claimButton.gameObject.SetActive(eligible);
            claimButton.interactable = eligible && !claimed;
        }
    }
}
