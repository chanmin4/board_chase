using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (AudioMaster.I) AudioMaster.I.PlayBGMKey("bgm.gameplay");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
