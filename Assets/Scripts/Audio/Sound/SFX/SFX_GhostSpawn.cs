using UnityEngine;

public class SFX_GhostSpawn : MonoBehaviour
{
    public PollutionGhostSpawner ghostspawner;
    public string key = "sfx.ghostspawn";

    void Reset()    => ghostspawner ??= GetComponent<PollutionGhostSpawner>();
    void OnEnable()
    {
        ghostspawner??= GetComponent<PollutionGhostSpawner>();
        if (!ghostspawner) { enabled = false; return; }
        ghostspawner.GhostSpawn+= Trigger;
    }
    void OnDisable()
    {
        if (ghostspawner) ghostspawner.GhostSpawn -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}