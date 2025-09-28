using UnityEngine;

public class SFX_GaugeGet : MonoBehaviour
{
     public SurvivalGauge gauge;
    public string key = "sfx.gaugeget";

    void Reset()    => gauge ??= GetComponent<SurvivalGauge>();
    void OnEnable()
    {
        gauge??= GetComponent<SurvivalGauge>();
        if (!gauge) { enabled = false; return; }
        gauge.GaugeGet+= Trigger;
    }
    void OnDisable()
    {
        if (gauge) gauge.GaugeGet -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}