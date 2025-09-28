using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;   // ★ 추가
public class SurvivalGauge : MonoBehaviour
{
    [Header("Gauge")]
    public float max = 100f;
    public float current = 100f;

    [Header("Drain (per sec)")]
    public float baseDrain = 5f;          // 기본 감소
    public float contaminatedExtra = 20f; // 오염 시 추가 감소

    [Header("UI")]
    public Slider slider;   // Slider (Min 0, Max 1)
    public Image  fill;     // slider.Fill Area/Fill 의 Image

    [Header("Colors")]
    public Color colorGreen  = new Color(0.2f, 1f, 0.4f, 1f); // >50%
    public Color colorYellow = new Color(1f, 0.9f, 0.2f, 1f); // 20~50%
    public Color colorRed    = new Color(1f, 0.3f, 0.3f, 1f); // <20%
    public Color colorContam = new Color(0.7f, 0.3f, 1f, 1f); // 오염(보라)

    [Header("Thresholds")]
    [Range(0f,1f)] public float yellowThreshold = 0.50f;
    [Range(0f,1f)] public float redThreshold    = 0.20f;

    [Header("Smoothing")]
    public float lerpSpeed = 8f; // 값/색 보간 속도
       // ★ 게임오버 트리거용
    [Header("Events")]
    public UnityEvent onDepleted;
    [ContextMenu("DEBUG_ForceDeplete")]
    public void DEBUG_ForceDeplete()
    {
        current = 0f;
        invoked = false;
        Debug.Log("[SG] DEBUG_ForceDeplete()");
        onDepleted?.Invoke();
    }

    bool invoked; // 중복 호출 방지
    // 상태
    bool contaminated;
    float display01 = 1f;

    // 다른 스크립트용 프로퍼티
    public float Value01        => Mathf.Clamp01(current / Mathf.Max(0.0001f, max));
    public float Current        => current;
    public float Max            => max;
    public bool  IsContaminated => contaminated;
    public event System.Action GaugeGet;
    void Reset()
    {
        slider = GetComponentInChildren<Slider>(true);
        if (slider && slider.fillRect)
            fill = slider.fillRect.GetComponent<Image>();
    }

    void Awake()
    {
        if (!slider) slider = GetComponentInChildren<Slider>(true);
        if (!fill && slider && slider.fillRect) fill = slider.fillRect.GetComponent<Image>();

        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;
        }
    }

    void Update()
    {
        // 1) 감소 처리
        float drain = baseDrain + (contaminated ? contaminatedExtra : 0f);
        if (drain > 0f)
        {
            current = Mathf.Clamp(current - drain * Time.deltaTime, 0f, max);

        }


        // 2) 슬라이더 값 보간
        float v01 = Value01;
        display01 = Mathf.Lerp(display01, v01, Time.deltaTime * lerpSpeed);
        if (slider) slider.value = display01;

        // 3) 색상 (오염 우선)
        if (fill)
        {
            Color target =
                contaminated ? colorContam :
                (display01 > yellowThreshold ? colorGreen :
                 display01 > redThreshold ? colorYellow :
                                               colorRed);
            fill.color = Color.Lerp(fill.color, target, Time.deltaTime * lerpSpeed);
        }
        if (!invoked && current <= 0f)
        {
            invoked = true;
            Debug.Log($"[SG] Depleted at t={Time.time:0.00}");  // ★ 로그
            onDepleted?.Invoke();
        }
    }

    // 값 올리기(존 보상 등)
    public void Add(float delta)
    {
        current = Mathf.Clamp(current + delta, 0f, max);
        GaugeGet?.Invoke();
    }

    // 오염 상태 토글(디렉터에서 호출)
    public void SetContaminated(bool v)
    {
        contaminated = v;
    }
}
