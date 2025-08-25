using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardManager : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI chargeText; // “현재/최대”
    public Button useButton;

    [Header("Resource Key")]
    public string cardResourceName = "Cards/Cleaner"; // Resources/Cards/Cleaner.asset

    [Header("Refs")]
    public SurvivalDirector director;
    public Transform player; // 디스크 Transform

    CardData data;
    int charge;
    bool onCooldown;
    CardAbility ability;

    int lastWallHits;

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!player)   player   = GameObject.FindGameObjectWithTag("Player")?.transform;

        data = Resources.Load<CardData>(cardResourceName);
        if (!data) Debug.LogError($"[CardManager] CardData not found: {cardResourceName}");

        if (useButton) useButton.onClick.AddListener(TryUse);

        // ⬇⬇ 여기서 “벽 튕김 수 변경” 이벤트 직접 구독 → Δ만큼 충전
        if (director) director.OnWallHitsChanged += HandleWallHitsChanged;

        ApplyUI();
    }

    void OnDestroy()
    {
        if (director) director.OnWallHitsChanged -= HandleWallHitsChanged;
    }

    void HandleWallHitsChanged(int hitsNow)
    {
        int delta = Mathf.Max(0, hitsNow - lastWallHits);
        lastWallHits = hitsNow;
        if (delta > 0 && data) AddCharge(delta * Mathf.Max(1, data.gainPerWallBounce));
    }

    void AddCharge(int amount)
    {
        if (onCooldown || data == null) return;
        charge = Mathf.Min(data.maxCharge, charge + amount);
        ApplyUI();
    }

    void ApplyUI()
    {
        if (!data) { if (useButton) useButton.interactable = false; return; }
        if (icon)     icon.sprite = data.icon;
        if (nameText) nameText.text = data.cardName;
        if (chargeText) chargeText.text = $"{charge} / {data.maxCharge}";

        bool ready = !onCooldown && charge >= data.maxCharge;
        if (useButton) useButton.interactable = ready;
    }

    void TryUse()
    {
        if (!data || onCooldown || charge < data.maxCharge) return;
        EnsureAbility();
        ability.Activate(player, director, data);

        charge = 0;
        ApplyUI();

        if (data.cooldown > 0f) StartCoroutine(CooldownCo(data.cooldown));
    }

    System.Collections.IEnumerator CooldownCo(float sec)
    {
        onCooldown = true; ApplyUI();
        yield return new WaitForSeconds(sec);
        onCooldown = false; ApplyUI();
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
