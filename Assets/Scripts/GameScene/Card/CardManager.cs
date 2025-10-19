using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;

public class CardManager : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI chargeText; // “현재/최대”
    public Button useButton;
    public TextMeshProUGUI durationText;

    [Header("Input")]
    public bool enableSpacebarUse = true;
    public KeyCode useKey = KeyCode.Space;
    public bool enableDoubleClick = true;
    [Range(0.15f, 0.5f)] public float doubleClickWindow = 0.3f;
    float lastClickTime = -999f;

    [Header("Resource Key")]
    public string cardResourceName = "Cards/Cleaner"; // Resources/Cards/Cleaner.asset

    [Header("Refs")]
    public SurvivalDirector director;
    public Transform player; // 디스크 Transform
     public SurvivalGauge gauge;

    public int riskAddRequiredCharge = 0;   // 요구 충전 +N
    public bool riskDisableUse = false;     // 카드 사용 금지 ON/OFF

    public event System.Action CardUse;

    CardData data;
    [SerializeField] int charge = 10;
    bool onCooldown;
    CardAbility ability;
    Coroutine durationCo;

    // === 여기만 추가 (능력 사용 중 충전 잠금) ===
    [SerializeField] bool chargingLocked = false;    // true면 충전 불가
    // ===========================================

    public int EffectiveMaxCharge => (data ? Mathf.Max(0, data.maxCharge + riskAddRequiredCharge) : 0);

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!player)   player   = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!gauge) gauge = FindAnyObjectByType<SurvivalGauge>();
        if (gauge)
        {
            gauge.onStunBegin?.AddListener(() => chargingLocked = true);
            gauge.onStunEnd  ?.AddListener(() => chargingLocked = false);
        }


        data = Resources.Load<CardData>(cardResourceName);
        if (!data) Debug.LogError($"[CardManager] CardData not found: {cardResourceName}");

        if (useButton) useButton.onClick.AddListener(OnUseButtonClicked);

        if (director) director.OnZoneHit += HandleZoneHit;

        ApplyUI();
    }

    void OnDestroy()
    {
        if (director) director.OnZoneHit -= HandleZoneHit;
        if (gauge)
        {
            gauge.onStunBegin?.RemoveListener(() => chargingLocked = true);
            gauge.onStunEnd  ?.RemoveListener(() => chargingLocked = false);
        }
        if (durationCo != null) StopCoroutine(durationCo);
    }

    void Update()
    {
        if (enableSpacebarUse && Input.GetKeyDown(useKey) && IsReady())
        {
            TryUse();
        }
    }

    bool IsReady() =>
        data && !riskDisableUse && !onCooldown && charge >= EffectiveMaxCharge;

    void OnUseButtonClicked()
    {
        if (!IsReady()) return;

        if (enableDoubleClick)
        {
            float now = Time.unscaledTime;
            if (now - lastClickTime <= doubleClickWindow)
            {
                lastClickTime = -999f;
                TryUse();
            }
            else
            {
                lastClickTime = now;
            }
        }
        else
        {
            TryUse();
        }
    }

    void HandleZoneHit(int zoneId, int cur, int req, bool isBonus)
    {
        // === 잠금 상태면 충전 무시 ===
        if (riskDisableUse || !data || chargingLocked) return;

        int gain = isBonus ? Mathf.Max(1, data.gainPerZoneCritBounce)
                           : Mathf.Max(1, data.gainPerZoneBounce);
        AddCharge(gain);
    }

    void AddCharge(int amount)
    {
        // === 잠금 상태면 충전 무시 ===
        if (riskDisableUse || onCooldown || data == null || chargingLocked) return;

        charge = Mathf.Min(EffectiveMaxCharge, charge + amount);
        ApplyUI();
    }

    void ApplyUI()
    {
        if (!data) { if (useButton) useButton.interactable = false; return; }

        if (icon)     icon.sprite = data.icon;
        if (nameText) nameText.text = data.cardName;
        if (chargeText) chargeText.text = $"{charge}/{EffectiveMaxCharge}";
        if (durationText) durationText.text = $"{data.duration:0.0}s";

        useButton.interactable = !onCooldown && !riskDisableUse && charge >= EffectiveMaxCharge;
    }

    void TryUse()
    {
        if (!data || onCooldown || riskDisableUse || charge < EffectiveMaxCharge) return;

        EnsureAbility();
        CardUse?.Invoke();
        ability.Activate(player, director, data);

        // 사용과 동시에 충전 잠금
        chargingLocked = true;

        charge = 0;
        ApplyUI();

        if (durationCo != null) StopCoroutine(durationCo);
        durationCo = StartCoroutine(DurationCountdownCo(data.duration));

        if (data.cooldown > 0f) StartCoroutine(CooldownCo(data.cooldown));
    }

    IEnumerator CooldownCo(float sec)
    {
        onCooldown = true; ApplyUI();
        yield return new WaitForSeconds(sec);
        onCooldown = false; ApplyUI();
    }

    IEnumerator DurationCountdownCo(float sec)
    {
        float t = Mathf.Max(0f, sec);

        // 시작 프레임: 표시 갱신 + 충전 잠금 유지
        if (durationText) durationText.text = $"{t:0.0}s";

        var wait = new WaitForEndOfFrame();
        while (t > 0f)
        {
            t -= Time.deltaTime;
            if (durationText) durationText.text = $"{Mathf.Max(0f, t):0.0}s";
            yield return wait;
        }

        // 종료: 잠금 해제
        chargingLocked = false;

        // 기본 표시(리소스 값)로 복귀
        if (durationText) durationText.text = $"{data.duration:0.0}s";
        durationCo = null;
    }

    void EnsureAbility()
    {
        if (ability) return;
        var host = new GameObject($"Ability_{data.cardName}");
        host.transform.SetParent(transform, false);

        switch (data.abilityType)
        {
            case "CleanTrail":
                ability = host.AddComponent<CleanTrailAbility_Card>();
                break;
            case "ZoneCrit":
                ability = host.AddComponent<ZoneCriticalArc>();
                break;
            default:
                break;
                
            

        }
    }
}
