using UnityEngine;

public class BGM_GameScene_Success : MonoBehaviour
{
    public SurvivalSuccessManager survivalsuccess;
    public string key = "sfx.rocketlaunch";

    void Reset()    =>survivalsuccess ??= GetComponent<SurvivalSuccessManager>();
    void OnEnable()
    {
        survivalsuccess??= GetComponent<SurvivalSuccessManager>();
        if (!survivalsuccess) { enabled = false; return; }
        survivalsuccess.Success+= Trigger;
    }
    void OnDisable()
    {
        if (survivalsuccess) survivalsuccess.Success -= Trigger;
    }
    void Trigger() => AudioMaster.I?.PlayKey(key);
}
