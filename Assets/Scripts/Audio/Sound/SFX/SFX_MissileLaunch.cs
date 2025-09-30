using UnityEngine;

public class SFX_MissileLaunch : MonoBehaviour
{
    public BarrageMissileSpawner missilespawner;
    public string key = "sfx.missilelaunch";

    void Reset()    => missilespawner ??= GetComponent<BarrageMissileSpawner>();
    void OnEnable()
    {
        missilespawner??= GetComponent<BarrageMissileSpawner>();
        if (!missilespawner) { enabled = false; return; }
        missilespawner.MissileLaunch+= Trigger;
    }
    void OnDisable()
    {
        if (missilespawner) missilespawner.MissileLaunch -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}