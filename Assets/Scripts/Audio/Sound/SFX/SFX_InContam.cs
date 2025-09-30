using UnityEngine;

public class SFX_InContam : MonoBehaviour
{
    public SurvivalDirector Director;
    public string key = "sfx.incontam";

    void Reset()    =>Director ??= GetComponent<SurvivalDirector>();
    void OnEnable()
    {
        Director??= GetComponent<SurvivalDirector>();
        if (!Director) { enabled = false; return; }
        Director.OnEnterContam+= Trigger;
    }
    void OnDisable()
    {
        if (Director) Director.OnEnterContam  -= Trigger;
    }
    void Trigger(Vector3 pos, int ix, int iy) => AudioMaster.I?.PlayKey(key);
}
