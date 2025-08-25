using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetRegenTimerHUD : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public TMP_Text label;          // 캔버스의 TextMeshProUGUI
    public Image  fillImage;        // 선택: Filled Image(원형/바)로 진행도 표시

    [Header("Format")]
    public bool showMilliseconds = false;    // 00:12.3 같은 표기
    public string prefix = "SET ";           // 예: "SET " → "SET 00:12"
    public string suffix = "";               // 예: " 남음"

    [Header("Colors")]
    public Color colorHigh = new Color(0.2f, 1f, 0.4f, 1f);   // >50% (초록)
    public Color colorMid  = new Color(1f, 0.9f, 0.2f, 1f);   // 20~50% (노랑)
    public Color colorLow  = new Color(1f, 0.3f, 0.3f, 1f);   // <20% (빨강)

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
    }

    void Update()
    {
        if (!director) return;

        float remain   = director.SetRemain;
        float duration = director.SetDuration;
        float prog01   = director.SetProgress01;  // 0→1 (지나간 비율)

        // 텍스트 갱신
        if (label)
        {
            label.text = prefix + FormatTime(remain, showMilliseconds) + suffix;
            label.color = PickColor(1f - prog01); // 남은 비율로 색 결정
        }

        // 프로그레스 바(선택)
        if (fillImage)
        {
            // Image.type = Filled 이어야 함 (Radial360 또는 Horizontal 등)
            fillImage.fillAmount = Mathf.Clamp01(1f - prog01); // 남은 비율
            fillImage.color      = PickColor(1f - prog01);
        }
    }

    string FormatTime(float seconds, bool withMs)
    {
        seconds = Mathf.Max(0f, seconds);
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60;
        if (withMs)
            return $"{m:00}:{s:00.0}";
        else
            return $"{m:00}:{(int)s:00}";
    }

    Color PickColor(float remainingRatio)
    {
        // remainingRatio: 1=가득 남음, 0=없음
        if (remainingRatio > 0.5f) return colorHigh;
        if (remainingRatio > 0.20f) return colorMid;
        return colorLow;
    }
}
