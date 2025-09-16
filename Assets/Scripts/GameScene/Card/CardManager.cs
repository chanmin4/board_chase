using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

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
    [Range(0.15f, 0.5f)] public float doubleClickWindow = 0.3f; // 초
    float lastClickTime = -999f;

    [Header("Resource Key")]
    public string cardResourceName = "Cards/Cleaner"; // Resources/Cards/Cleaner.asset

    [Header("Refs")]
    public SurvivalDirector director;
    public Transform player; // 디스크 Transform

    public int riskAddRequiredCharge = 0;   // 요구 충전 +N
    public bool riskDisableUse = false;     // 카드 사용 금지 ON/OFF
    

    CardData data;
    int charge;
    bool onCooldown;
    CardAbility ability;

    int lastWallHits;
    private Coroutine durationCo;

public int EffectiveMaxCharge => (data ? Mathf.Max(0, data.maxCharge + riskAddRequiredCharge) : 0);

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!player)   player   = GameObject.FindGameObjectWithTag("Player")?.transform;

        data = Resources.Load<CardData>(cardResourceName);
        if (!data) Debug.LogError($"[CardManager] CardData not found: {cardResourceName}");

         if (useButton) useButton.onClick.AddListener(OnUseButtonClicked);

        // ⬇⬇ 여기서 “벽 튕김 수 변경” 이벤트 직접 구독 → Δ만큼 충전
        if (director) director.OnWallHitsChanged += HandleWallHitsChanged;

        ApplyUI();
    }

    void OnDestroy()
    {
        if (director) director.OnWallHitsChanged -= HandleWallHitsChanged;
        if (durationCo != null) StopCoroutine(durationCo);
    }

    void Update()
    {
        // 스페이스바(혹은 지정 키)로 사용
        if (enableSpacebarUse && Input.GetKeyDown(useKey) && IsReady())
        {
            TryUse();
        }
    }
    bool IsReady() => data && !riskDisableUse && !onCooldown && charge >= data.maxCharge;
     void OnUseButtonClicked()
    {
        if (!IsReady()) return;

        if (enableDoubleClick)
        {
            float now = Time.unscaledTime;
            if (now - lastClickTime <= doubleClickWindow)
            {
                // 더블클릭으로 인정
                lastClickTime = -999f;
                TryUse();
            }
            else
            {
                // 첫 클릭만 기록 (싱글클릭 발동은 하지 않음)
                lastClickTime = now;
            }
        }
        else
        {
            // 옵션으로 싱글클릭 허용하고 싶을 때 사용
            TryUse();
        }
    }
    void HandleWallHitsChanged(int hitsNow)
    {
        int delta = Mathf.Max(0, hitsNow - lastWallHits);
        lastWallHits = hitsNow;
        if (riskDisableUse || delta <= 0 || !data) return;  

        int baseGain = Mathf.Max(1, data.gainPerWallBounce);
        //float mul = FeverManager.ChargeMul;      //피어중일경우 현재 적용x           
        int gain = Mathf.RoundToInt(delta * baseGain);
        AddCharge(gain);
    }

    void AddCharge(int amount)
    {
        if (riskDisableUse || onCooldown || data == null) return; 
        charge = Mathf.Min(data.maxCharge, charge + amount);
        ApplyUI();
    }

    void ApplyUI()
    {
        if (!data) { if (useButton) useButton.interactable = false; return; }
        if (icon)     icon.sprite = data.icon;
        if (nameText) nameText.text = data.cardName;
        chargeText.text = $"{charge}/{EffectiveMaxCharge}";
         if (durationText) durationText.text = $"{data.duration:0.0}s";
        bool ready = !riskDisableUse && !onCooldown && charge >= data.maxCharge;
        useButton.interactable = !onCooldown && !riskDisableUse && charge >= EffectiveMaxCharge;
    }

    void TryUse()
    {
        if (!data || onCooldown || riskDisableUse || charge < EffectiveMaxCharge) return;
        EnsureAbility();
        ability.Activate(player, director, data);

        charge = 0;
        ApplyUI();
        if (durationCo != null) StopCoroutine(durationCo);
        durationCo = StartCoroutine(DurationCountdownCo(data.duration));

        if (data.cooldown > 0f) StartCoroutine(CooldownCo(data.cooldown));
    }

    System.Collections.IEnumerator CooldownCo(float sec)
    {
        onCooldown = true; ApplyUI();
        yield return new WaitForSeconds(sec);
        onCooldown = false; ApplyUI();
    }
  System.Collections.IEnumerator DurationCountdownCo(float sec)
    {
        float t = Mathf.Max(0f, sec);
        // 시작 프레임에 즉시 반영
        if (durationText) durationText.text = $"{t:0.0}s";

        var wait = new WaitForEndOfFrame();
        while (t > 0f)
        {
            t -= Time.deltaTime;
            if (durationText) durationText.text = $"{Mathf.Max(0f, t):0.0}s";
            yield return wait;
        }
        // 종료 후 기본 표시(리소스 값)로 복귀
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
            default:
                ability = host.AddComponent<CleanTrailAbility>(); // 통합본
                break;
        }
    }
}
