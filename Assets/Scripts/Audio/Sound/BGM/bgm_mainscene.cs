using UnityEngine;

public class bgm_mainscene : MonoBehaviour
{
    public MainMenuController Main;
    public string key = "bgm.mainscene";

    void Reset()    =>Main ??= GetComponent<MainMenuController>();
    void OnEnable()
    {
        Main??= GetComponent<MainMenuController>();
        if (!Main) { enabled = false; return; }
        Main.BGM_Mainscene+= Trigger;
    }
    void OnDisable()
    {
        if (Main) Main.BGM_Mainscene -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}
