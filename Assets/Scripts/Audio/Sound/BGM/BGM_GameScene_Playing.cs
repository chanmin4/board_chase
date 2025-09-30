using UnityEngine;

public class BGM_GameScene_Playing : MonoBehaviour
{
    public string bgmKey = "bgm.gamescene.playing";

    void Start()
    {
        if (AudioMaster.I) AudioMaster.I.PlayBGMKey(bgmKey);
    }
}
