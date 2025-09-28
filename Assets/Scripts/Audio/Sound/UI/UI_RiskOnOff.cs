using UnityEngine;
using UnityEngine.UI;

public class UI_RiskOnOff : MonoBehaviour
{
    public Toggle toggle;                       // 비워도 자동 참조
    public string keyOn  = "ui.riskonoff";        // 켜질 때
    public string keyOff = "ui.riskonoff";       // 꺼질 때
    public bool playOnAwakeSync = false;        // 시작 시 현재 상태에 맞춰 1번 재생할지

    void Reset() => toggle = GetComponent<Toggle>();

    void Awake()
    {
        if (!toggle) toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(Handle);

        if (playOnAwakeSync) Handle(toggle.isOn);
    }

    void OnDestroy()
    {
        if (toggle) toggle.onValueChanged.RemoveListener(Handle);
    }

    void Handle(bool isOn)
    {
        if (AudioMaster.I == null) return;
        AudioMaster.I.PlayKey(isOn ? keyOn : keyOff);
    }
}