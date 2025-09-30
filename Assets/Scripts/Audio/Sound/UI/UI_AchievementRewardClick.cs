using UnityEngine;
using UnityEngine.UI;
public class UI_AchievementRewardClick : MonoBehaviour
{
    public string key = "ui.achievementrewardclick";
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (AudioMaster.I) AudioMaster.I.PlayKey(key);
        });
    }
    
}
