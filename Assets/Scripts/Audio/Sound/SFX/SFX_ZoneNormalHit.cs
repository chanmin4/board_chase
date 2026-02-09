/*
using UnityEngine;

public class SFX_ZoneNormalHit : MonoBehaviour
{
    public SurvivalDirector Director;
    public string key = "sfx.zonenormalhit";

    void Reset()    =>Director ??= GetComponent<SurvivalDirector>();
    void OnEnable()
    {
        Director??= GetComponent<SurvivalDirector>();
        if (!Director) { enabled = false; return; }
        Director.ZoneNormalHit_SFX+= Trigger;
    }
    void OnDisable()
    {
        if (Director) Director.ZoneNormalHit_SFX -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}
*/