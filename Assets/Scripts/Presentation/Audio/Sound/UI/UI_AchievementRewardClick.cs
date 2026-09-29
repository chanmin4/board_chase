/*
 * Role hierarchy: Presentation > Audio > Sound > UI
 * Primary responsibility: Implements the focused UI_AchievementRewardClick responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
