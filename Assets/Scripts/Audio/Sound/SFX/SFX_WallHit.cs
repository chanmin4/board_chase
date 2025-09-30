using UnityEngine;

public class SFX_WallHit : MonoBehaviour
{
     public DiskLauncher Disk;
    public string key = "sfx.wallhit";

    void Reset()    =>Disk ??= GetComponent<DiskLauncher>();
    void OnEnable()
    {
        Disk??= GetComponent<DiskLauncher>();
        if (!Disk) { enabled = false; return; }
        Disk.WallHit+= Trigger;
    }
    void OnDisable()
    {
        if (Disk) Disk.WallHit -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}