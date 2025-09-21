using UnityEngine;

public class MainBGM : MonoBehaviour
{
    public string bgmKey = "bgm.mainscene";

    void Start()
    {
        if (AudioMaster.I) AudioMaster.I.PlayBGMKey(bgmKey);
    }
}
