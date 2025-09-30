using UnityEngine;

public class BGM_GameScene_GameOver : MonoBehaviour
{
    public GameOverUI GameOver;
    public string key = "sfx.rocketlaunch";

    void Reset()    =>GameOver ??= GetComponent<GameOverUI>();
    void OnEnable()
    {
        GameOver??= GetComponent<GameOverUI>();
        if (!GameOver) { enabled = false; return; }
        GameOver.GameOver+= Trigger;
    }
    void OnDisable()
    {
        if (GameOver) GameOver.GameOver -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}
