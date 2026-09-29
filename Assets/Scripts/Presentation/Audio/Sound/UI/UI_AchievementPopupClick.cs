/*
 * Role hierarchy: Presentation > Audio > Sound > UI
 * Primary responsibility: Implements the focused UI_AchievementPopupClick responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class UI_AchievementPopupClick : MonoBehaviour
{
    public AchievementPopupManager AchievePopup;
    public string key = "sfx.achievementpopupclick";

    void Reset()    =>AchievePopup ??= GetComponent<AchievementPopupManager>();
    void OnEnable()
    {
        AchievePopup??= GetComponent<AchievementPopupManager>();
        if (!AchievePopup) { enabled = false; return; }
        AchievePopup.AchievePopup+= Trigger;
    }
    void OnDisable()
    {
        if (AchievePopup) AchievePopup.AchievePopup -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}
