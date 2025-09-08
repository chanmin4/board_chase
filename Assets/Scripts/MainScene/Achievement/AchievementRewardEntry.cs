// AchievementRewardEntry.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementRewardEntry : MonoBehaviour
{
    public Image icon;            // (없어도 됨)
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text statusText;
    public Button claimButton;

    Achievement data;
    ProgressManager pm;

    public void Bind(Achievement a, ProgressManager manager)
    {
        data = a;
        pm = manager;

        if (titleText) titleText.text = string.IsNullOrEmpty(a.title) ? $"{a.requiredBestScore}pt 보상" : a.title;
        if (descText)  descText.text  = a.description ?? "";

        RefreshState();

        if (claimButton)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() =>
            {
                if (pm.TryClaim(a.id))
                    RefreshState();
            });
        }
    }

    void RefreshState()
    {
        bool eligible = pm.IsAchievementEligible(data.id);
        bool claimed  = pm.IsAchievementClaimed(data.id);

        if (statusText)
            statusText.text = !eligible ? "미달성" : (claimed ? "수령 완료" : "수령 가능");

        if (claimButton)
        {
            claimButton.gameObject.SetActive(eligible);
            claimButton.interactable = eligible && !claimed;
        }
    }
}
