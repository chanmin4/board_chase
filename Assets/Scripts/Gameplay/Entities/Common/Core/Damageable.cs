/*
 * Role hierarchy: Gameplay > Entities > Common > Core
 * Primary responsibility: Implements the focused EntityDeathCause responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public enum EntityDeathCause
{
    None = 0,
    HealthDepleted = 1,
    InfectionOvertake = 2,
    Forced = 3
}

public class Damageable : MonoBehaviour
{
    public bool IsPeriodicStatusDamage { get; private set; }

    public void AddInfectionExposure(float amount)
    {
        if (IsDead || !InfectionAggravationMath.FinitePositive(amount)) return;
        if (EntityComponentResolver.Resolve<ShooterInfection>(this) is ShooterInfection shooter)
        {
            if (shooter is PlayerShooterInfection player) player.AddExposure(amount);
            else
            {
                var status = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(shooter);
                if (status != null) status.AddInfectionAggravation(amount);
                else shooter.AddInfection(amount);
            }
            return;
        }
        // Creature damageables do not necessarily use the shooter infection component.
        var effects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this) ??
            gameObject.AddComponent<EntityStatusEffectRuntime>();
        effects.AddInfectionAggravation(amount);
    }

    // Classify the current notification only; nested direct hits must remain interrupting attacks.
    private void PublishDamageReceived(float damage, bool periodicStatus = false)
    {
        bool previous = IsPeriodicStatusDamage;
        IsPeriodicStatusDamage = periodicStatus;
        try { OnDamaged?.Invoke(this, damage); }
        finally { IsPeriodicStatusDamage = previous; }
    }

    // Bleeding is not another incoming attack: no dodge/armor, hit lock, invulnerability,
    // target acquisition, or bandage interruption. Health/death notifications still run.
    public float ReceiveStatusDamageOverTime(float damage)
    {
        if (IsDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return 0f;
        float previousHealth = _currentHealth;
        _currentHealth = Mathf.Clamp(_currentHealth - damage, 0f, _maxHealth);
        SyncRuntimeHealthDebug();
        PublishDamageReceived(damage, periodicStatus: true);
        EntityComponentResolver.Resolve<PlayerShooterInfection>(this)?.AddHitExposure(previousHealth - _currentHealth);
        if (_currentHealth <= 0f) Die(EntityDeathCause.HealthDepleted, "status damage over time");
        else NotifyHealthChanged();
        return damage;
    }
    [Header("Health Config")]
    [FormerlySerializedAs("_mutarusStatConfigSO")]
    [FormerlySerializedAs("_enemyStatConfigSO")]
    [SerializeField] private EntityStatConfigSO _statConfig;

    [Tooltip("Used only when Stat Config is missing.")]
    [SerializeField, Min(1f)] private float _fallbackInitialHealth = 1f;

    [Header("Runtime Health")]
    [ReadOnly] [SerializeField] private float _maxHealth;
    [ReadOnly] [SerializeField] private float _uninfectedMaxHealth;
    [ReadOnly] [SerializeField] private float _infectionMaxHealthLoss;
    [ReadOnly] [SerializeField] private float _currentHealth;
    [System.NonSerialized] private GameObject _lastDamageAttacker;

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
    [SerializeField, HideInInspector] private EntityPaintMarkController _paintMarkController;
    [SerializeField] private EntityStatusEffectRuntime _statusEffects;

    [Header("Base Armor Fallback")]
    [SerializeField, Min(0f)] private float _baseArmorHealthDamageMultiplier = 1f;
    [Tooltip("Legacy data retained for existing prefabs. Base armor health damage uses ArmorPenetrationDamageCurve.")]
    [SerializeField, HideInInspector, Range(0f, 1f)] private float _baseArmorHealthDamageMultiplierPerClassGap = 0.5f;
    [SerializeField, HideInInspector, Min(0f)] private float _baseArmorMarkDamageMultiplier = 1f;
    [SerializeField, HideInInspector, Range(0f, 1f)] private float _baseArmorMarkDamageMultiplierPerClassGap = 0.75f;

    [Header("Invulnerability")]
    [SerializeField] private InvulnerabilityController _invulnerabilityController;
    [SerializeField] private bool _applyPostHitInvulnerability = false;
    [SerializeField] private InvulnerabilityConfigSO _postHitInvulnerabilityConfig;

    [Header("Broadcasting On")]
    [SerializeField] private HitReceivedEventChannelSO _hitReceivedEvent = default;
    [SerializeField] private VoidEventChannelSO _updateHealthUI = default;
    [SerializeField] private VoidEventChannelSO _deathEvent = default;

    [Header("Damage Number")]
    [Tooltip("Optional UI event used to display floating damage/heal numbers. Damageable raises HP/heal directly, and external damage tracks can call ReportDamageNumber for values like infection.")]
    [FormerlySerializedAs("_damageNumberEvent")]
    [SerializeField] private DamageNumberEventChannelSO _damageNumberEventChannel = default;

    [Header("Listening To")]
    [SerializeField] private FloatEventChannelSO _restoreHealth = default;

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
    public float UninfectedMaxHealth => _uninfectedMaxHealth > 0f ? _uninfectedMaxHealth : _maxHealth;
    public float InfectionMaxHealthLoss => _infectionMaxHealthLoss;
    public float CurrentHealth => _currentHealth;
    public float HealthNormalized => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
    public EntityDeathCause LastDeathCause { get; private set; }
    public float DamageTakenMultiplier => _damageTakenMultiplier;
    public EntityStatConfigSO StatConfig => _statConfig;
    /// <summary>
    /// The armor class used by both combat and presentation. Equipped armor is
    /// part of the effective value, not merely the damage multiplier path.
    /// It remains fractional after permanent bonuses so the armor curve never
    /// rounds away an authored half-class upgrade.
    /// </summary>
    public float EffectiveArmorClass
    {
        get
        {
            if (_equipmentRuntime == null)
                _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);

            float baseArmorClass = ResolveBaseArmorClass();
            float armorClassDelta = ResolveArmorClassDelta();

            return _equipmentRuntime != null
                ? _equipmentRuntime.ResolveEffectiveArmorClass(
                    baseArmorClass,
                    armorClassDelta)
                : Mathf.Max(0f, baseArmorClass + armorClassDelta);
        }
    }

    public float FinalDamageTakenMultiplier => Mathf.Max(0f, _damageTakenMultiplier);

    public event UnityAction<Damageable> OnDamageMultiplierChanged;
    public event UnityAction<Damageable> OnHealthChanged;
    public event UnityAction<Damageable, float> OnDamaged;
    public event UnityAction OnDie;

    /// <summary>
    /// Keeps this entity alive as a scene object after death so a corpse
    /// controller can replace its visuals and expose loot interaction.
    /// </summary>
    public void PreserveGameObjectOnDeath()
    {
        _destroyOnDeath = false;
    }

    public DamageFlash_GetHitConfigSO GetHitEffectConfig => _getHitEffectSO;
    public Renderer MainMeshRenderer => _mainMeshRenderer;

    public bool IsInvulnerable =>
        _invulnerabilityController != null && _invulnerabilityController.IsInvulnerable;

    public bool CanReceiveDamage => !IsDead && !IsInvulnerable;

    private Coroutine _damageMultiplierRoutine;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        InitializeHealthFromConfig();

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);

        if (_equipmentRuntime == null)
            _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);

        if (_paintMarkController == null)
            _paintMarkController = EntityComponentResolver.Resolve<EntityPaintMarkController>(this);

        if (_invulnerabilityController == null)
            TryGetComponent(out _invulnerabilityController);

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_restoreHealth != null)
            _restoreHealth.OnEventRaised += Cure;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += NotifyDamageMultiplierChanged;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_restoreHealth != null)
            _restoreHealth.OnEventRaised -= Cure;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= NotifyDamageMultiplierChanged;
    }

    // Purpose: Performs the receive an attack workflow for this component.
    public void ReceiveAnAttack(float damage, GameObject attacker = null)
    {
        ReceiveAnAttack(damage, attacker, 0);
    }

    // Purpose: Performs the receive an attack workflow for this component.
    public void ReceiveAnAttack(float damage, GameObject attacker, float penetrationClass)
    {
        ReceiveAnAttack(damage, attacker, penetrationClass, 1f);
    }

    // Purpose: Performs the receive an attack workflow for this component.
    public void ReceiveAnAttack(
        float damage,
        GameObject attacker,
        float penetrationClass,
        float armorHealthDurabilityLossMultiplier)
    {
        if (!CanReceiveDamage)
            return;

        if (TryDodgeIncomingAttack())
            return;

        float rawDamage = Mathf.Max(0f, damage);
        ShieldArmorRuntime shield = EntityComponentResolver.Resolve<ShieldArmorRuntime>(this);
        if (RaidArmorPenetration.Enabled)
        {
            if (_statusEffects == null) _statusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this);
            bool shielding = shield != null && shield.IsProtecting;
            bool vest = !shielding && _equipmentRuntime != null && _equipmentRuntime.HasUsableArmor;
            float armorClass = (shielding ? shield.ArmorClass : vest ? _equipmentRuntime.ArmorClass : 0f) +
                ResolveBaseArmorClass() + ResolveArmorClassDelta();
            var result = RaidArmorPenetration.Resolve(Mathf.Max(0, armorClass), penetrationClass,
                shielding ? shield.CurrentDurability : vest ? _equipmentRuntime.CurrentArmorDurability : 1f,
                shielding ? shield.MaximumDurability : vest ? _equipmentRuntime.MaxArmorDurability : 1f, UnityEngine.Random.value);
            float status = _statusEffects != null ? _statusEffects.GetMultiplier(StatusEffectModifierType.DamageTakenMultiplier) : 1f;
            float afterProtection = vest
                ? _equipmentRuntime.AbsorbArmorHealthDamage(rawDamage, result.healthMultiplier)
                : rawDamage * result.healthMultiplier;
            // 적의 특수 실드가 아니라 착용 방탄복만 흡수량 1:1 내구도 풀로 처리합니다.
            if (shielding)
                shield.ApplyDurabilityDamage(rawDamage * result.wearMultiplier * Mathf.Max(0, armorHealthDurabilityLossMultiplier));
            float hp = afterProtection * FinalDamageTakenMultiplier * status;
            ApplyHealthHit(Mathf.Max(0, hp), attacker);
            return;
        }
        float afterShield = shield != null ? shield.Absorb(rawDamage, penetrationClass, armorHealthDurabilityLossMultiplier) : rawDamage;
        // Sample each armor layer once. Vest wear excludes damage absorbed by shield/base armor/status.
        float vestMultiplier = _equipmentRuntime != null && _equipmentRuntime.HasUsableArmor
            ? _equipmentRuntime.CurrentArmor.ResolveHealthDamageMultiplier(penetrationClass, 0f, 0f) : 1f;
        float afterVest = _equipmentRuntime != null && _equipmentRuntime.HasUsableArmor
            ? _equipmentRuntime.AbsorbArmorHealthDamage(afterShield, vestMultiplier)
            : afterShield;
        float baseMultiplier = ResolveBaseHealthArmorMultiplier(penetrationClass, ResolveBaseArmorClass(),
            ResolveArmorClassDelta(), _baseArmorHealthDamageMultiplier);
        if (_statusEffects == null) _statusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this);
        float statusMultiplier = _statusEffects != null ? _statusEffects.GetMultiplier(StatusEffectModifierType.DamageTakenMultiplier) : 1f;
        float finalDamage = Mathf.Max(0f, afterVest * baseMultiplier * FinalDamageTakenMultiplier * statusMultiplier);

        ApplyHealthHit(finalDamage, attacker);
    }

    /// <summary>Fixed HP hit: bypass armor and damage multipliers, but not dodge, death or invulnerability.</summary>
    public void ReceiveFixedMeleeAttack(float damage, GameObject attacker = null)
    {
        if (!CanReceiveDamage || TryDodgeIncomingAttack()) return;
        // No armor absorption means no armor durability damage either.
        ApplyHealthHit(Mathf.Max(0f, damage), attacker);
    }

    private void ApplyHealthHit(float finalDamage, GameObject attacker)
    {
        if (finalDamage <= 0f)
            return;

        float previousHealth = _currentHealth;
        float nextHealth = _currentHealth - finalDamage;

        _currentHealth = Mathf.Clamp(nextHealth, 0f, _maxHealth);
        _lastDamageAttacker = attacker;

        SyncRuntimeHealthDebug();
        GetHit = true;
        PublishDamageReceived(finalDamage);
        _hitReceivedEvent?.RaiseEvent(gameObject);
        ReportDamageNumber(finalDamage, DamageNumberKind.Health);
        QuestRuntime.ReportEntityHit(attacker, this);
        var infection=EntityComponentResolver.Resolve<PlayerShooterInfection>(this);
        if (infection != null) infection.AddHitExposure(previousHealth - _currentHealth);

        if (_currentHealth <= 0f)
        {
            Die(EntityDeathCause.HealthDepleted, "damage");
            return;
        }

        if (_applyPostHitInvulnerability && _invulnerabilityController != null)
            _invulnerabilityController.Begin(_postHitInvulnerabilityConfig);

        if (attacker != null)
        {
            Enemy enemy = GetComponent<Enemy>() ?? GetComponentInParent<Enemy>();
            enemy?.NotifyDamagedBy(attacker);
        }

        NotifyHealthChanged();
    }

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        // Preserved only for migration safety; direct health damage now uses
        // ArmorPenetrationDamageCurve instead of this old multiplicative value.
        _baseArmorHealthDamageMultiplierPerClassGap =
            Mathf.Clamp01(_baseArmorHealthDamageMultiplierPerClassGap);
    }

    /// <summary>
    /// Applies Infection Injection DOT directly to health. This deliberately
    /// bypasses armor, dodge, and hit-invulnerability because the projectile
    /// already had to land; high-difficulty resistance is data-driven through
    /// EntityStatConfigSO.Infection.
    /// </summary>
    public float ReceiveInfectionDamageOverTime(float rawDamage, GameObject attacker = null)
    {
        if (IsDead)
            return 0f;

        float finalDamage = Mathf.Max(
            0f,
            rawDamage * ResolveInfectionDamageOverTimeMultiplier());

        if (finalDamage <= 0f)
            return 0f;

        var playerInfection = EntityComponentResolver.Resolve<PlayerShooterInfection>(this);
        if (playerInfection != null)
        {
            playerInfection.AddExposure(finalDamage);
            return finalDamage; // Legacy infection injection feeds the gauge, never a second HP DOT.
        }

        _currentHealth = Mathf.Clamp(_currentHealth - finalDamage, 0f, _maxHealth);
        _lastDamageAttacker = attacker;

        SyncRuntimeHealthDebug();
        GetHit = true;
        PublishDamageReceived(finalDamage, periodicStatus: true);
        _hitReceivedEvent?.RaiseEvent(gameObject);
        ReportDamageNumber(finalDamage, DamageNumberKind.Infection);

        if (_currentHealth <= 0f)
        {
            Die(EntityDeathCause.HealthDepleted, "infection injection DOT");
            return finalDamage;
        }

        if (attacker != null)
        {
            Enemy enemy = GetComponent<Enemy>() ?? GetComponentInParent<Enemy>();
            enemy?.NotifyDamagedBy(attacker);
        }

        NotifyHealthChanged();
        return finalDamage;
    }

    // Purpose: Performs the kill workflow for this component.
    public void Kill()
    {
        Kill(EntityDeathCause.Forced);
    }

    // Purpose: Performs the kill workflow for this component.
    public void Kill(EntityDeathCause cause)
    {
        Die(cause == EntityDeathCause.None ? EntityDeathCause.Forced : cause, "forced kill");
    }

    // Purpose: Performs the revive workflow for this component.
    public void Revive()
    {
        if (_maxHealth <= 0f) return; // A depleted ceiling must be restored/reset before revival.
        _currentHealth = _maxHealth;
        IsDead = false;
        LastDeathCause = EntityDeathCause.None;
        _lastDamageAttacker = null;

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    // Purpose: Performs the reset health from config workflow for this component.
    public void ResetHealthFromConfig()
    {
        InitializeHealthFromConfig();
        IsDead = false;
        LastDeathCause = EntityDeathCause.None;
        _lastDamageAttacker = null;

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    // Purpose: Applies the requested max health from stats change.
    public void ApplyMaxHealthFromStats(float maxHealth, bool healToFull)
    {
        SetMaxHealth(maxHealth, healToFull);
    }

    // Purpose: Updates the max health state.
    public void SetMaxHealth(float maxHealth, bool healToFull)
    {
        _uninfectedMaxHealth = Mathf.Max(1f, maxHealth);
        _maxHealth = Mathf.Max(0f, _uninfectedMaxHealth - _infectionMaxHealthLoss);

        _currentHealth = healToFull
            ? _maxHealth
            : Mathf.Clamp(_currentHealth, 0f, _maxHealth);

        if (_currentHealth <= 0f)
        {
            Die(EntityDeathCause.HealthDepleted, "max health recalculation");
            return;
        }

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    // Purpose: Updates the current health state.
    public void SetInfectionMaxHealthLoss(float loss)
    {
        if (float.IsNaN(loss) || float.IsInfinity(loss)) return;
        if (_uninfectedMaxHealth <= 0f) _uninfectedMaxHealth = Mathf.Max(1f, _maxHealth);
        _infectionMaxHealthLoss = Mathf.Max(0f, loss);
        _maxHealth = Mathf.Max(0f, _uninfectedMaxHealth - _infectionMaxHealthLoss);
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
        // Maximum-health erosion is not a second weapon hit or HP damage tick.
        SyncRuntimeHealthDebug();
        if (!IsDead && _maxHealth <= 0f) Die(EntityDeathCause.InfectionOvertake, "infection exhausted maximum health");
        else NotifyHealthChanged();
    }

    public void SetCurrentHealth(float currentHealth)
    {
        float nextHealth = Mathf.Clamp(currentHealth, 0f, _maxHealth);

        if (nextHealth <= 0f)
        {
            Die(EntityDeathCause.HealthDepleted, "SetCurrentHealth");
            return;
        }

        _currentHealth = nextHealth;
        IsDead = false;
        LastDeathCause = EntityDeathCause.None;

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
    }

    // Purpose: Performs the heal workflow for this component.
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        float previousHealth = _currentHealth;
        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);
        float healedAmount = Mathf.Max(0f, _currentHealth - previousHealth);

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();
        ReportDamageNumber(healedAmount, DamageNumberKind.Heal);
    }

    // Purpose: Updates the damage taken multiplier state.
    public void SetDamageTakenMultiplier(float multiplier)
    {
        StopDamageMultiplierTimer();

        _damageTakenMultiplier = ClampDamageTakenMultiplier(multiplier);
        NotifyDamageMultiplierChanged();
    }

    // Purpose: Updates the damage taken multiplier for seconds state.
    public void SetDamageTakenMultiplierForSeconds(float multiplier, float duration)
    {
        StopDamageMultiplierTimer();

        _damageTakenMultiplier = ClampDamageTakenMultiplier(multiplier);
        NotifyDamageMultiplierChanged();

        if (duration > 0f)
            _damageMultiplierRoutine = StartCoroutine(ResetDamageMultiplierAfter(duration));
    }

    // Purpose: Performs the reset damage taken multiplier workflow for this component.
    public void ResetDamageTakenMultiplier()
    {
        StopDamageMultiplierTimer();

        _damageTakenMultiplier = ClampDamageTakenMultiplier(_defaultDamageTakenMultiplier);
        NotifyDamageMultiplierChanged();
    }

    // Purpose: Initializes health from config for use by this component.
    private void InitializeHealthFromConfig()
    {
        float initialHealth = ResolveInitialHealth();

        _maxHealth = initialHealth;
        _uninfectedMaxHealth = initialHealth;
        _infectionMaxHealthLoss = 0f;
        _currentHealth = initialHealth;
    }

    // Purpose: Performs the die workflow for this component.
    private void Die(EntityDeathCause cause, string reason)
    {
        if (IsDead)
            return;

#if UNITY_EDITOR
        if (CombatLabRuntime.HandlePlayerDeath(this))
        {
            _currentHealth = 1f;
            SyncRuntimeHealthDebug(); NotifyHealthChanged();
            return;
        }
#endif

        _currentHealth = 0f;
        IsDead = true;
        LastDeathCause = cause;

        QuestRuntime.ReportEntityKilled(_lastDamageAttacker, this);

        SyncRuntimeHealthDebug();
        NotifyHealthChanged();

        Debug.Log(
            $"[Damageable] Death. owner={name}, cause={cause}, reason={reason}\n{System.Environment.StackTrace}",
            this);

        OnDie?.Invoke();
        _deathEvent?.RaiseEvent();

        if (_destroyOnDeath)
            Destroy(gameObject, Mathf.Max(0f, _destroyDelay));
    }

    // Purpose: Resolves the initial health needed by this component.
    private float ResolveInitialHealth()
    {
        return _statConfig != null
            ? _statConfig.ResolveInitialHealth()
            : Mathf.Max(1f, _fallbackInitialHealth);
    }

    // Purpose: Performs the cure workflow for this component.
    private void Cure(float healthToAdd)
    {
        if (IsDead || healthToAdd <= 0f)
            return;

        Heal(healthToAdd);
    }

    // Purpose: Performs the clamp damage taken multiplier workflow for this component.
    private float ClampDamageTakenMultiplier(float multiplier)
    {
        float min = Mathf.Max(0f, _minDamageTakenMultiplier);
        float max = Mathf.Max(min, _maxDamageTakenMultiplier);

        return Mathf.Clamp(multiplier, min, max);
    }

    // Purpose: Attempts to dodge incoming attack and reports whether the request succeeded.
    private bool TryDodgeIncomingAttack()
    {
        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);

        float dodgeChance = _statsRuntime != null
            ? Mathf.Clamp01(_statsRuntime.DodgeChance)
            : 0f;

        return dodgeChance > 0f && UnityEngine.Random.value < dodgeChance;
    }

    // Purpose: Resolves the health damage multiplier for penetration needed by this component.
    public float ResolveHealthDamageMultiplierForPenetration(float penetrationClass)
    {
        if (_statusEffects == null)
            _statusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this);

        float equipmentMultiplier = _equipmentRuntime != null
            ? _equipmentRuntime.ResolveHealthDamageMultiplier(
                penetrationClass,
                ResolveBaseArmorClass(),
                ResolveArmorClassDelta())
            : ResolveBaseHealthArmorMultiplier(
                penetrationClass,
                ResolveBaseArmorClass(),
                ResolveArmorClassDelta(),
                _baseArmorHealthDamageMultiplier);

        float statusMultiplier = _statusEffects != null
            ? _statusEffects.GetMultiplier(StatusEffectModifierType.DamageTakenMultiplier)
            : 1f;

        ShieldArmorRuntime shield = EntityComponentResolver.Resolve<ShieldArmorRuntime>(this);
        return Mathf.Max(0f, FinalDamageTakenMultiplier * equipmentMultiplier * statusMultiplier *
            (shield != null ? shield.ResolveMultiplier(penetrationClass) : 1f));
    }

    // Purpose: Resolves the paint mark damage multiplier for penetration needed by this component.
    public float ResolvePaintMarkDamageMultiplierForPenetration(float penetrationClass)
    {
        return _equipmentRuntime != null
            ? _equipmentRuntime.ResolveMarkDamageMultiplier(
                penetrationClass,
                ResolveBaseArmorClass(),
                ResolveArmorClassDelta())
            : ResolveBaseArmorMultiplier(
                penetrationClass,
                ResolveBaseArmorClass(),
                ResolveArmorClassDelta(),
                _baseArmorMarkDamageMultiplier,
                _baseArmorMarkDamageMultiplierPerClassGap);
    }

    // Purpose: Resolves the infection damage multiplier for penetration needed by this component.
    public float ResolveInfectionDamageMultiplierForPenetration(float penetrationClass)
    {
        // Infection is the armor-ignoring secondary kill pressure. Penetration
        // and effective Armor Class only modify direct Health damage.
        return 1f;
    }

    // Purpose: Resolves the infection damage over time multiplier needed by this component.
    public float ResolveInfectionDamageOverTimeMultiplier()
    {
        if (_statusEffects == null)
            _statusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this);

        float resistanceMultiplier = _statConfig != null && _statConfig.Infection != null
            ? _statConfig.Infection.InfectionDamageOverTimeMultiplier
            : 1f;

        float statusMultiplier = _statusEffects != null
            ? _statusEffects.GetMultiplier(StatusEffectModifierType.DamageTakenMultiplier)
            : 1f;

        return Mathf.Max(
            0f,
            FinalDamageTakenMultiplier * resistanceMultiplier * statusMultiplier);
    }

    // Purpose: Resolves the final health damage needed by this component.
    private float ResolveFinalHealthDamage(float damage, float penetrationClass)
    {
        return Mathf.Max(
            0f,
            damage * ResolveHealthDamageMultiplierForPenetration(penetrationClass));
    }

    // Purpose: Applies the requested armor infection durability damage change.
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

    // Purpose: Resolves the armor class delta from paint mark needed by this component.
    private int ResolveArmorClassDeltaFromPaintMark()
    {
        return 0; // WIP: mark stacks never modify armor in current combat.
    }

    // Purpose: Resolves the armor class delta needed by this component.
    private int ResolveArmorClassDelta()
    {
        if (_statusEffects == null)
            _statusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this);

        int statusDelta = _statusEffects != null
            ? _statusEffects.ArmorClassDelta
            : 0;

        return ResolveArmorClassDeltaFromPaintMark() + statusDelta;
    }

    // Purpose: Resolves the base armor class needed by this component.
    private float ResolveBaseArmorClass()
    {
        if (_statsRuntime != null)
            return Mathf.Max(0f, _statsRuntime.ArmorClass);

        return _statConfig != null ? _statConfig.BaseArmorClass : 0f;
    }

    // Purpose: Resolves the base health armor multiplier needed by this component.
    private static float ResolveBaseHealthArmorMultiplier(
        float penetrationClass,
        float baseArmorClass,
        float armorClassDelta,
        float sameOrHigherMultiplier)
    {
        float effectiveArmorClass = Mathf.Max(0f, Mathf.Max(0f, baseArmorClass) + armorClassDelta);

        return Mathf.Max(
            0f,
            Mathf.Max(0f, sameOrHigherMultiplier) *
            ArmorPenetrationDamageCurve.RollMultiplier(
                effectiveArmorClass,
                penetrationClass));
    }

    // Purpose: Resolves the base armor multiplier needed by this component.
    private static float ResolveBaseArmorMultiplier(
        float penetrationClass,
        float baseArmorClass,
        float armorClassDelta,
        float sameOrHigherMultiplier,
        float perClassGapMultiplier)
    {
        float effectiveArmorClass = Mathf.Max(0f, Mathf.Max(0f, baseArmorClass) + armorClassDelta);
        float classGap = Mathf.Max(0f, effectiveArmorClass - Mathf.Max(0, penetrationClass));

        return Mathf.Max(
            0f,
            Mathf.Max(0f, sameOrHigherMultiplier) *
            Mathf.Pow(Mathf.Clamp01(perClassGapMultiplier), classGap));
    }

    // Purpose: Synchronizes runtime health debug with the current source state.
    private void SyncRuntimeHealthDebug()
    {
        _debugHealthNormalized = HealthNormalized;
        _debugDamageTakenMultiplier = _damageTakenMultiplier;
    }

    // Purpose: Performs the report damage number workflow for this component.
    public void ReportDamageNumber(float amount, DamageNumberKind kind)
    {
        if (_damageNumberEventChannel == null || amount <= 0f)
            return;

        _damageNumberEventChannel.RaiseDamage(gameObject, amount, kind);
    }

    // Purpose: Performs the reset damage multiplier after workflow for this component.
    private IEnumerator ResetDamageMultiplierAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        _damageMultiplierRoutine = null;
        _damageTakenMultiplier = ClampDamageTakenMultiplier(_defaultDamageTakenMultiplier);
        NotifyDamageMultiplierChanged();
    }

    // Purpose: Stops the active damage multiplier timer behavior.
    private void StopDamageMultiplierTimer()
    {
        if (_damageMultiplierRoutine == null)
            return;

        StopCoroutine(_damageMultiplierRoutine);
        _damageMultiplierRoutine = null;
    }

    // Purpose: Publishes the health changed notification.
    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(this);
        _updateHealthUI?.RaiseEvent();
    }

    // Purpose: Publishes the damage multiplier changed notification.
    private void NotifyDamageMultiplierChanged()
    {
        OnDamageMultiplierChanged?.Invoke(this);
        OnHealthChanged?.Invoke(this);
        _updateHealthUI?.RaiseEvent();
    }

}
