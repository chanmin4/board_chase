using UnityEngine;

public class SFX_ContamSpawn : MonoBehaviour
{
    public SurvivalDirector director;
    public string key = "sfx.contamspawn";

    void Reset()    => director ??= GetComponent<SurvivalDirector>();
    void OnEnable()
    {
        director??= GetComponent<SurvivalDirector>();
        if (!director) { enabled = false; return; }
        director.ContamSpawn+= Spawn;
    }
    void OnDisable()
    {
        if (director) director.ContamSpawn -= Spawn;
    }
    void Spawn() => AudioMaster.I?.PlayKey(key);
}