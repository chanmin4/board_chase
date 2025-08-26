using UnityEngine;

public class MainBGM : MonoBehaviour
{
    [Tooltip("카탈로그 키. 예: bgm.main or bgm.gameplay")]
    public string bgmKey = "bgm.main";

    void Start()
    {
        if (AudioMaster.I) AudioMaster.I.PlayBGMKey(bgmKey);
    }
}
