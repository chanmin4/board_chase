using UnityEngine;

public class bgm_mainscene : MonoBehaviour
{
    public string bgmKey = "bgm.mainscene";

    void Start()
    {
        if (AudioMaster.I) AudioMaster.I.PlayBGMKey(bgmKey);
    }
}
