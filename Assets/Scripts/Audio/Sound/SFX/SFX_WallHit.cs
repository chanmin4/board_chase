using UnityEngine;

public class SFX_WallHit : MonoBehaviour
{
     public PlayerDisk Disk;
    public string key = "sfx.wallhit";

    void Reset()    =>Disk ??= GetComponent<PlayerDisk>();
    void OnEnable()
    {
        Disk??= GetComponent<PlayerDisk>();
        if (!Disk) { enabled = false; return; }
        //Disk.WallHit+= Trigger;
    }
    void OnDisable()
    {
       // if (Disk) Disk.WallHit -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}