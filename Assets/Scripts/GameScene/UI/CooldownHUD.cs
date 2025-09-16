// Assets/Scripts/UI/CooldownHUD.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CooldownHUD : MonoBehaviour
{
    [Header("Refs")]
    public DiskLauncher launcher;
    public TMP_Text label;        // "READY" or "2.7s"
    public Image   radialFill;    // 선택(원형 이미지 fillAmount 0~1)

    [Header("Style")]
    public string readyText = "READY";
    public string fmt = "0.0s";   // 소수1자리

    void Awake()
    {
        if (!launcher) launcher = FindAnyObjectByType<DiskLauncher>();
        if (launcher) launcher.OnCooldownChanged += OnCD;
    }
    void Start(){ ForceRefresh(); }
    void OnDestroy(){ if (launcher) launcher.OnCooldownChanged -= OnCD; }

    void ForceRefresh(){ OnCD(launcher ? launcher.CooldownRemain : 0f, launcher ? launcher.cooldownSeconds : 1f); }

    void OnCD(float remain, float duration)
    {
        bool ready = remain <= 0.0001f;
        if (label) label.text = ready ? readyText : remain.ToString(fmt);

        if (radialFill)
        {
            float t = (duration > 0f) ? Mathf.Clamp01(remain / duration) : 0f;
            radialFill.fillAmount = t; // 1→0
        }
    }
}
