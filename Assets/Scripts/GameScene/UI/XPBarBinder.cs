using UnityEngine;
using UnityEngine.UI;

public class XPBarBinder : MonoBehaviour
{
    public DiskInkLeveler leveler;
    public Slider slider;                // Min 0, Max 1 권장
    public bool showFillAmount = true;   // 선택적으로 Image.fillAmount 쓰고 싶으면

    void Awake()
    {
        if (!slider) slider = GetComponentInChildren<Slider>(true);
    }

    void OnEnable()
    {
        if (leveler != null)
            leveler.OnXPChanged.AddListener(OnXPChanged);
    }
    void OnDisable()
    {
        if (leveler != null)
            leveler.OnXPChanged.RemoveListener(OnXPChanged);
    }

    void OnXPChanged(float curXP, float need)
    {
        float v = Mathf.Clamp01(curXP / Mathf.Max(1f, need));
        if (slider) slider.value = v;
    }
}
