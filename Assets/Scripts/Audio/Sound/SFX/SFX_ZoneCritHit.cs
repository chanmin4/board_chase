/*
using UnityEngine;

public class SFX_ZoneCritHit : MonoBehaviour
{
    public SurvivalDirector Director;
    public string key = "sfx.zonecrithit";

    void Reset()    =>Director ??= GetComponent<SurvivalDirector>();
    void OnEnable()
    {
        Director??= GetComponent<SurvivalDirector>();
        if (!Director) { enabled = false; return; }
        Director.ZoneCritHit_SFX+= Trigger;
    }
    void OnDisable()
    {
        if (Director) Director.ZoneCritHit_SFX -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}
*/