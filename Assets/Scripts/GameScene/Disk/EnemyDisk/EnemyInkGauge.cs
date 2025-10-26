using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;   // ★ 추가
public class EnemyInkGauge : MonoBehaviour
{
    [Header("Gauge")]
    public float max = 100f;
    public float current = 100f;

    [Header("UI")]
    public Slider slider;   // Slider (Min 0, Max 1)
    public Image fill;     // slider.Fill Area/Fill 의 Image

    [Header("Colors")]
    public Color colorGreen = new Color(0.2f, 1f, 0.4f, 1f); // >50%
    public Color colorYellow = new Color(1f, 0.9f, 0.2f, 1f); // 20~50%
    public Color colorRed = new Color(1f, 0.3f, 0.3f, 1f); // <20%
    public Color colorContam = new Color(0.7f, 0.3f, 1f, 1f); // 오염(보라)

    [Header("Thresholds")]
    [Range(0f, 1f)] public float yellowThreshold = 0.50f;
    [Range(0f, 1f)] public float redThreshold = 0.20f;

    [Header("Smoothing")]
    public float lerpSpeed = 8f; // 값/색 보간 속도
                               
    [Header("Events")]
    public UnityEvent onEnemyDepleted;
    public UnityEvent onEnemyStunBegin;
    public UnityEvent onEnemyStunEnd;

    [Header("Stun/Recovery")]
    [Tooltip("게이지가 0이 되면 이 시간(초) 동안 0→100으로 서서히 회복")]
    public float recoverDuration = 5f;

    [Tooltip("기절 중 디스크 이동속도 배수 (1=변화 없음, 0.6=40% 감소 등)")]
    [Range(0.1f, 1.0f)] public float stunMoveSpeedMul = 0.4f;

    [Tooltip("기절 중 발사 쿨타임에 더해지는 초(가산)")]
    public float stunCooldownAddSeconds = 1.5f;

    [Header("Refs · (선택) 디버프 적용 대상")]
    public EnemyDiskLauncher Enemydisk;   // 인스펙터에서 디스크(Launcher) 연결

    [Header("Ink Painting Cost")]
    [Tooltip("내 영역을 1m 칠할 때 드는 기본 잉크 소모량")]
    public float baseCostPerMeter = 0.25f;

    [Tooltip("적 영역(오염)을 덧칠할 때 곱해지는 배수 (예: 1.5)")]
    public float contamExtraMul = 1.5f;

    [ContextMenu("DEBUG_ForceDeplete")]
    public void DEBUG_ForceDeplete()
    {
        current = 0f;
        invoked = false;
        Debug.Log("[SG] DEBUG_ForceDeplete()");
        onEnemyDepleted?.Invoke();
    }

    bool recovering = false;    // 기절(회복) 중인지
    float recoverT = 0f;       // 0~1 회복 진행률
    bool invoked; // 중복 호출 방지
    // 상태
    bool contaminated;
    bool isstun = false;
    float display01 = 1f;
    public bool CanPaint => !recovering && current > 0f;

    // 다른 스크립트용 프로퍼티
    public float Value01 => Mathf.Clamp01(current / Mathf.Max(0.0001f, max));
    public float Current => current;
    public float Max => max;
    public bool IsContaminated => contaminated;
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
        if (recovering)
        {
            // 0→1까지 선형 진행
            recoverT += (recoverDuration > 0f ? Time.deltaTime / recoverDuration : 1f);
            recoverT = Mathf.Clamp01(recoverT);

            // 자연 회복량
            float natural = Mathf.Lerp(0f, max, recoverT);

            // 외부 회복(존 보상 등)으로 current가 더 커졌다면 그 값을 우선시
            current = Mathf.Max(current, natural);
            recoverT = Mathf.Max(recoverT, current / Mathf.Max(0.0001f, max));
            // 꽉 찼으면 즉시 기절 해제
            if (current >= max - 0.0001f)
            {
                current = max;
                EndStun();
            }
        }

        else
        {
            // 0이 되었을 때 한 번만 트리거 → 기절 시작
            if (!invoked && current <= 0f)
            {
                invoked = true;
                Debug.Log($"[SG] Depleted at t={Time.time:0.00}");
                onEnemyDepleted?.Invoke();
                BeginStun();   // ★ 여기서 기절 상태 진입
            }
        }

        // ─ 공통: 슬라이더/색상 보간 ─
        float v01 = Value01;
        display01 = Mathf.Lerp(display01, v01, Time.deltaTime * lerpSpeed);
        if (slider) slider.value = display01;

        if (fill)
        {
            Color target =
                contaminated ? colorContam :
                (display01 > yellowThreshold ? colorGreen :
                 display01 > redThreshold ? colorYellow :
                                            colorRed);
            fill.color = Color.Lerp(fill.color, target, Time.deltaTime * lerpSpeed);
        }


    }

    // 값 올리기(존 보상 등)
    public void Add(float delta)
    {
        current = Mathf.Clamp(current + delta, 0f, max);
        GaugeGet?.Invoke();

        if (recovering)
            recoverT = Mathf.Max(recoverT, current / Mathf.Max(0.0001f, max));

        if (recovering && current >= max - 0.0001f)
        {
            current = max;
            EndStun();
        }

    }


    // 오염 상태 토글(디렉터에서 호출)
    public void SetContaminated(bool v)
    {
        contaminated = v;
    }
    void BeginStun()
    {

        recovering = true;
        recoverT = 0f;
        current = 0f;
        isstun = true;
        // 디버프 적용(선택)
        if (Enemydisk)
        {
            Debug.Log("begin stun&debuff");
            Enemydisk.externalSpeedMul?.Invoke(Mathf.Clamp(stunMoveSpeedMul, 0.1f, 1.0f));
            Enemydisk.externalCooldownAdd?.Invoke(Mathf.Max(0f, stunCooldownAddSeconds));
        }
        onEnemyStunBegin?.Invoke();
    }

    void EndStun()
    {
        recovering = false;
        current = max;   // 안전하게 꽉 채움
        invoked = false; // 다음 사이클에서 다시 0 트리거 가능
        isstun = false;
        // 디버프 해제
        if (Enemydisk)
        {
            Enemydisk.externalSpeedMul?.Invoke(1f);
            Enemydisk.externalCooldownAdd?.Invoke(0f);
        }
        onEnemyStunEnd?.Invoke();
    }
    public bool TryConsumeByPaint(float meters, bool isContam, float widthMul = 1f)   // ★ ADDED
    {
        if (isstun) return false;
        if (meters <= 0f || baseCostPerMeter <= 0f || widthMul <= 0f)
            return true; // 비용 없음 → 성공 처리
        float mul = isContam ? contamExtraMul : 1f;
        float cost = meters * baseCostPerMeter * mul * widthMul;

        if (current <= 0f || cost <= 0f)
            return false;

        // ★ 부분 소모 허용: 잔여 잉크만큼은 깎아서 0 도달 가능하게
        float consume = Mathf.Min(current, cost);
        if (consume > 0f) Add(-consume);

        // 전액 지불 여부로 이번 도장 성공/실패 반환
        return (consume >= cost);
    }
    
    

}
