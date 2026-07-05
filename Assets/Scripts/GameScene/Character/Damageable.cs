using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Damageable : MonoBehaviour
{
    [Header("Health Config")]
    [FormerlySerializedAs("_mutarusStatConfigSO")]
    [FormerlySerializedAs("_enemyStatConfigSO")]
    [SerializeField] private EntityStatConfigSO _statConfig;

    [Tooltip("Used only when Stat Config is missing.")]
    [SerializeField, Min(1f)] private float _fallbackInitialHealth = 1f;

    [Header("Runtime Health")]
    [ReadOnly] [SerializeField] private float _maxHealth;
    [ReadOnly] [SerializeField] private float _currentHealth;

    [Header("Player Health Floor")]
    [Tooltip("If true, this Damageable never drops below Minimum Alive Health by normal damage.")]
    [SerializeField] private bool _keepAliveAtMinimumHealth = false;
    [SerializeField, Min(0f)] private float _minimumAliveHealth = 1f;

    [Header("Death")]
    [SerializeField] private bool _destroyOnDeath = false;
    [SerializeField] private float _destroyDelay = 1f;
    
    [Header("Damage Multiplier")]
    [SerializeField, Min(0f)] private float _defaultDamageTakenMultiplier = 1f;
    [SerializeField, Min(0f)] private float _damageTakenMultiplier = 1f;
    [SerializeField, Min(0f)] private float _minDamageTakenMultiplier = 0f;
    [SerializeField, Min(0f)] private float _maxDamageTakenMultiplier = 99f;

    [Header("Runtime Debug")]
    [ReadOnly] [SerializeField] private float _debugHealthNormalized;
    [ReadOnly] [SerializeField] private float _debugDamageTakenMultiplier = 1f;

    [Header("Combat")]
    [SerializeField] private DamageFlash_GetHitConfigSO _getHitEffectSO;
    [SerializeField] private Renderer _mainMeshRenderer;
    [SerializeField] private DroppableRewardConfigSO _droppableRewardSO;

    [Header("Stats Runtime")]
    [SerializeField] private ShooterStatsRuntime _statsRuntime;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private EntityPaintMarkController _paintMarkController;

    [Header("Base Armor Fallback")]
    [SerializeField, Min(0f)] private float _baseArmorHealthDamageMultiplier = 1f;
    [SerializeField, Range(0f, 1f)] private float _baseArmorHealthDamageMultiplierPerClassGap = 0.5f;
    [SerializeField, Min(0f)] private float _baseArmorMarkDamageMultiplier = 1f;
    [SerializeField, Range(0f, 1f)] private float _baseArmorMarkDamageMultiplierPerClassGap = 0.75f;

    [Header("Invulnerability")]
    [SerializeField] private InvulnerabilityController _invulnerabilityController;
    [SerializeField] private bool _applyPostHitInvulnerability = false;
    [SerializeField] private InvulnerabilityConfigSO _postHitInvulnerabilityConfig;
    [Header("Broadcasting On")]
    [SerializeField] private HitReceivedEventChannelSO _hitReceivedEvent = default;
    [SerializeField] private VoidEventChannelSO _updateHealthUI = default;
    [SerializeField] private VoidEventChannelSO _deathEvent = default;

    [Header("Listening To")]
    [SerializeField] private FloatEventChannelSO _restoreHealth = default;
    
    [SerializeField] private float _paintMarkDamageTakenAdditive = 0f;
    public DroppableRewardConfigSO DroppableRewardConfig
    {
        get
        {
            if (_statConfig is EnemyStatConfigSO enemyConfig &&
                enemyConfig.DroppableRewardConfig != null)
            {
                return enemyConfig.DroppableRewardConfig;
            }

            return _droppableRewardSO;
        }
    }

    public bool GetHit { get; set; }
    public bool IsDead { get; set; }

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;
    public float HealthNormalized => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
    public float DamageTakenMultiplier => _damageTakenMultiplier;
    public EntityStatConfigSO StatConfig => _statConfig;
    public float PaintMarkDamageTakenAdditive => _paintMarkDamageTakenAdditive;
    public float FinalDamageTakenMultiplier => Mathf.Max(
        0f,
        _damageTakenMultiplier + _paintMarkDamageTakenAdditive);
    public event UnityAction<Damageable> OnDamageMultiplierChanged;
    public event UnityAction<Damageable> OnHealthChanged;
    public event UnityAction OnDie;
    public DamageFlash_GetHitConfigSO GetHitEffectConfig => _getHitEffectSO;
    public Renderer MainMeshRenderer => _mainMeshRenderer;

    public bool IsInvulnerable =>
        _invulnerabilityController != null && _invulnerabilityController.IsInvulnerable;

    public bool CanReceiveDamage => !IsDead && !IsInvulnerable;

    private Coroutine _damageMultiplierRoutine;

    private void Awake()
    {
        InitializeHealthFromConfig();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<ShooterStatsRuntime>() ?? GetComponentInParent<ShooterStatsRuntime>();

        if (_equipmentRuntime == null)
            _equipmentRuntime = GetComponent<EntityEquipmentRuntime>() ?? GetComponentInParent<EntityEquipmentRuntime>();

        if (_paintMarkController == null)
            _paintMarkController = GetComponent<EntityPaintMarkController>() ??
                                   GetComponentInParent<EntityPaintMarkController>() ??
                                   GetComponentInChildren<EntityPaintMarkController>(true);

        if (_invulnerabilityController == null)
            TryGetComponent(out _invulnerabilityController);

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    private void OnEnable()
    {
        if (_restoreHealth != null)
            _restoreHealth.OnEventRaised += Cure;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += NotifyDamageMultiplierChanged;
    }

    private void OnDisable()
    {
        if (_restoreHealth != null)
            _restoreHealth.OnEventRaised -= Cure;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= NotifyDamageMultiplierChanged;
    }

    public void ReceiveAnAttack(float damage, GameObject attacker = null)
    {
        ReceiveAnAttack(damage, attacker, 0);
    }

    public void ReceiveAnAttack(float damage, GameObject attacker, int penetrationClass)
    {
        ReceiveAnAttack(damage, attacker, penetrationClass, 1f);
    }

    public void ReceiveAnAttack(
        float damage,
        GameObject attacker,
        int penetrationClass,
        float armorHealthDurabilityLossMultiplier)
    {
        if (!CanReceiveDamage)
            return;

        if (TryDodgeIncomingAttack())
            return;

        float rawDamage = Mathf.Max(0f, damage);
        float finalDamage = ResolveFinalHealthDamage(rawDamage, penetrationClass);
        ApplyArmorHealthDurabilityDamage(
            rawDamage,
            finalDamage,
            armorHealthDurabilityLossMultiplier);

        if (finalDamage <= 0f)
            return;

        bool useHealthFloor = false;
        float nextHealth = _currentHealth - finalDamage;

        _currentHealth = Mathf.Clamp(nextHealth, 0f, _maxHealth);

        SyncRuntimeHealthDebug();
        GetHit = true;
        _hitReceivedEvent?.RaiseEvent(gameObject);

        bool diedThisHit = !useHealthFloor && _currentHealth <= 0f;

        if (diedThisHit)
        {
            IsDead = true;
        }
        else if (_applyPostHitInvulnerability && _invulnerabilityController != null)
        {
            _invulnerabilityController.Begin(_postHitInvulnerabilityConfig);
        }

        if (!diedThisHit && attacker != null)
        {
            Enemy enemy = GetComponent<Enemy>() ?? GetComponentInParent<Enemy>();
            enemy?.NotifyDamagedBy(attacker);
        }

        NotifyHealthChanged();

        if (!diedThisHit)
            return;
        Debug.Log($"[Damageable] Die by damage owner={name}, hp={_currentHealth}\n{System.Environment.StackTrace}", this);
        OnDie?.Invoke();
        _deathEvent?.RaiseEvent();

        if (_destroyOnDeath)
            Destroy(gameObject, Mathf.Max(0f, _destroyDelay));
    }

    public void Kill()
    {
        if (IsDead)
            return;

        _currentHealth = 0f;
        IsDead = true;

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
        OnDie?.Invoke();
         Debug.Log($"[Damageable] Kill owner={name}\n{System.Environment.StackTrace}", this);
        _deathEvent?.RaiseEvent();

        if (_destroyOnDeath)
            Destroy(gameObject, Mathf.Max(0f, _destroyDelay));
    }

    public void Revive()
    {
        _currentHealth = Mathf.Max(1f, _maxHealth);
        IsDead = false;

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    public void ResetHealthFromConfig()
    {
        InitializeHealthFromConfig();
        IsDead = false;

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    public void ApplyMaxHealthFromStats(float maxHealth, bool healToFull)
    {
        SetMaxHealth(maxHealth, healToFull);
    }

    public void SetMaxHealth(float maxHealth, bool healToFull)
    {
        _maxHealth = Mathf.Max(1f, maxHealth);

        _currentHealth = healToFull
            ? _maxHealth
            : Mathf.Clamp(_currentHealth, 0f, _maxHealth);

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    public void SetCurrentHealth(float currentHealth)
    {
        _currentHealth = Mathf.Clamp(currentHealth, 0f, _maxHealth);
        IsDead = _currentHealth <= 0f;

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    public void SetDamageTakenMultiplier(float multiplier)
    {
        StopDamageMultiplierTimer();

        _damageTakenMultiplier = ClampDamageTakenMultiplier(multiplier);
        NotifyDamageMultiplierChanged();
    }

    public void SetDamageTakenMultiplierForSeconds(float multiplier, float duration)
    {
        StopDamageMultiplierTimer();

        _damageTakenMultiplier = ClampDamageTakenMultiplier(multiplier);
        NotifyDamageMultiplierChanged();

        if (duration > 0f)
            _damageMultiplierRoutine = StartCoroutine(ResetDamageMultiplierAfter(duration));
    }

    public void ResetDamageTakenMultiplier()
    {
        StopDamageMultiplierTimer();

        _damageTakenMultiplier = ClampDamageTakenMultiplier(_defaultDamageTakenMultiplier);
        NotifyDamageMultiplierChanged();
    }

    private void InitializeHealthFromConfig()
    {
        float initialHealth = ResolveInitialHealth();

        _maxHealth = initialHealth;
        _currentHealth = initialHealth;
    }

    private float ResolveInitialHealth()
    {
        return _statConfig != null
            ? _statConfig.ResolveInitialHealth()
            : Mathf.Max(1f, _fallbackInitialHealth);
    }

    private void Cure(float healthToAdd)
    {
        if (IsDead || healthToAdd <= 0f)
            return;

        _currentHealth = Mathf.Clamp(_currentHealth + healthToAdd, 0f, _maxHealth);

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    private float ClampDamageTakenMultiplier(float multiplier)
    {
        float min = Mathf.Max(0f, _minDamageTakenMultiplier);
        float max = Mathf.Max(min, _maxDamageTakenMultiplier);

        return Mathf.Clamp(multiplier, min, max);
    }

    private bool TryDodgeIncomingAttack()
    {
        if (_statsRuntime == null)
            _statsRuntime = GetComponent<ShooterStatsRuntime>() ?? GetComponentInParent<ShooterStatsRuntime>();

        float dodgeChance = _statsRuntime != null
            ? Mathf.Clamp01(_statsRuntime.DodgeChance)
            : 0f;

        return dodgeChance > 0f && Random.value < dodgeChance;
    }

    public float ResolveHealthDamageMultiplierForPenetration(int penetrationClass)
    {
        float equipmentMultiplier = _equipmentRuntime != null
            ? _equipmentRuntime.ResolveHealthDamageMultiplier(
                penetrationClass,
                ResolveBaseArmorClass(),
                ResolveArmorClassDeltaFromPaintMark())
            : ResolveBaseArmorMultiplier(
                penetrationClass,
                ResolveBaseArmorClass(),
                ResolveArmorClassDeltaFromPaintMark(),
                _baseArmorHealthDamageMultiplier,
                _baseArmorHealthDamageMultiplierPerClassGap);

        return Mathf.Max(0f, FinalDamageTakenMultiplier * equipmentMultiplier);
    }

    public float ResolvePaintMarkDamageMultiplierForPenetration(int penetrationClass)
    {
        return _equipmentRuntime != null
            ? _equipmentRuntime.ResolveMarkDamageMultiplier(
                penetrationClass,
                ResolveBaseArmorClass(),
                ResolveArmorClassDeltaFromPaintMark())
            : ResolveBaseArmorMultiplier(
                penetrationClass,
                ResolveBaseArmorClass(),
                ResolveArmorClassDeltaFromPaintMark(),
                _baseArmorMarkDamageMultiplier,
                _baseArmorMarkDamageMultiplierPerClassGap);
    }

    public float ResolveInfectionDamageMultiplierForPenetration(int penetrationClass)
    {
        return ResolvePaintMarkDamageMultiplierForPenetration(penetrationClass);
    }

    private float ResolveFinalHealthDamage(float damage, int penetrationClass)
    {
        return Mathf.Max(
            0f,
            damage * ResolveHealthDamageMultiplierForPenetration(penetrationClass));
    }

    public void ApplyArmorInfectionDurabilityDamage(
        float rawInfectionDamage,
        float finalInfectionDamage,
        float armorInfectionDurabilityLossMultiplier)
    {
        if (_equipmentRuntime == null)
            return;

        float absorbedDamage = Mathf.Max(0f, rawInfectionDamage - finalInfectionDamage);

        if (absorbedDamage <= 0f)
            return;

        _equipmentRuntime.ApplyArmorAbsorbedDamage(
            0f,
            absorbedDamage,
            0f,
            armorInfectionDurabilityLossMultiplier);
    }

    private void ApplyArmorHealthDurabilityDamage(
        float rawDamage,
        float finalHealthDamage,
        float armorHealthDurabilityLossMultiplier)
    {
        if (_equipmentRuntime == null)
            return;

        float absorbedDamage = Mathf.Max(0f, rawDamage - finalHealthDamage);

        if (absorbedDamage <= 0f)
            return;

        _equipmentRuntime.ApplyArmorAbsorbedDamage(
            absorbedDamage,
            0f,
            armorHealthDurabilityLossMultiplier,
            0f);
    }

    private int ResolveArmorClassDeltaFromPaintMark()
    {
        if (_paintMarkController == null)
        {
            _paintMarkController = GetComponent<EntityPaintMarkController>() ??
                                   GetComponentInParent<EntityPaintMarkController>() ??
                                   GetComponentInChildren<EntityPaintMarkController>(true);
        }

        return _paintMarkController != null
            ? _paintMarkController.ArmorClassDelta
            : 0;
    }

    private int ResolveBaseArmorClass()
    {
        if (_statsRuntime != null)
            return Mathf.Max(0, _statsRuntime.ArmorClass);

        return _statConfig != null ? _statConfig.BaseArmorClass : 0;
    }

    private static float ResolveBaseArmorMultiplier(
        int penetrationClass,
        int baseArmorClass,
        int armorClassDelta,
        float sameOrHigherMultiplier,
        float perClassGapMultiplier)
    {
        int effectiveArmorClass = Mathf.Max(0, Mathf.Max(0, baseArmorClass) + armorClassDelta);
        int classGap = Mathf.Max(0, effectiveArmorClass - Mathf.Max(0, penetrationClass));

        return Mathf.Max(
            0f,
            Mathf.Max(0f, sameOrHigherMultiplier) *
            Mathf.Pow(Mathf.Clamp01(perClassGapMultiplier), classGap));
    }

    private void SyncRuntimeHealthDebug()
    {
        _debugHealthNormalized = HealthNormalized;
        _debugDamageTakenMultiplier = _damageTakenMultiplier;
    }

    private IEnumerator ResetDamageMultiplierAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        _damageMultiplierRoutine = null;
        _damageTakenMultiplier = ClampDamageTakenMultiplier(_defaultDamageTakenMultiplier);
        NotifyDamageMultiplierChanged();
    }

    private void StopDamageMultiplierTimer()
    {
        if (_damageMultiplierRoutine == null)
            return;

        StopCoroutine(_damageMultiplierRoutine);
        _damageMultiplierRoutine = null;
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(this);
        _updateHealthUI?.RaiseEvent();
    }

    private void NotifyDamageMultiplierChanged()
    {
        OnDamageMultiplierChanged?.Invoke(this);
        OnHealthChanged?.Invoke(this);
        _updateHealthUI?.RaiseEvent();
    }
    public void SetPaintMarkDamageTakenAdditive(float additive)
    {
        if (Mathf.Approximately(_paintMarkDamageTakenAdditive, additive))
            return;

        _paintMarkDamageTakenAdditive = additive;
        NotifyDamageMultiplierChanged();
    }

    public void ResetPaintMarkDamageTakenAdditive()
    {
        SetPaintMarkDamageTakenAdditive(0f);
    }
}
