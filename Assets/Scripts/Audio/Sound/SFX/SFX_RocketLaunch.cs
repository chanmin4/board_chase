using UnityEngine;

public class SFX_RocketLaunch : MonoBehaviour
{
    public RocketHazardSystem RocketLaunch;
    public string key = "sfx.rocketlaunch";

    void Reset()    =>RocketLaunch ??= GetComponent<RocketHazardSystem>();
    void OnEnable()
    {
        RocketLaunch??= GetComponent<RocketHazardSystem>();
        if (!RocketLaunch) { enabled = false; return; }
        RocketLaunch.RocketLaunch+= Trigger;
    }
    void OnDisable()
    {
        if (RocketLaunch) RocketLaunch.RocketLaunch -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}