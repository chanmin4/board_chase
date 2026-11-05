/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles ShooterInfection combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class ShooterInfection : MonoBehaviour
{
    private enum ExposureKind
    {
        None,
        Virus,
        Vaccine,
        PoisonPuddle
    }

    private const float DefaultZoneTickInterval = 0.2f;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [Tooltip("Timed non-stacking status runtime shared by infection, movement, UI, VFX, and debug tools.")]
    [SerializeField] private EntityStatusEffectRuntime _statusEffects;
    
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Zone Exposure")]
    [SerializeField] private bool _enableZoneExposure = true;
    [SerializeField] private bool _requireOpenedSector = true;
    private MaskRenderManager _maskRenderManager;

    [Header("Runtime")]
    [SerializeField] private float _currentInfection;
    [ReadOnly] [SerializeField] private float _infectionRecoveryWaitRemaining;
    private Transform _samplePoint;
    private bool _isDead;
    private float _nextNaturalDecayTime;
    private float _naturalRecoveryAllowedTime;
    private float _zoneTickTimer;
    private ExposureKind _activeExposureKind;
    private PoisonPuddleDamageConfigSO _activePoisonPuddleConfig;
    private bool _loggedMissingStatusEffects;

    public event Action<ShooterInfection> OnChanged;

    public float CurrentInfection => _currentInfection;
    public bool IsDead => _isDead;
    public Damageable Damageable => _damageable;
    public float MaxHealth => _damageable != null ? _damageable.MaxHealth : 0f;
    public float CurrentHealth => _damageable != null ? _damageable.CurrentHealth : 0f;
    public float InfectionLimit => _damageable != null ? Mathf.Max(0f, _damageable.UninfectedMaxHealth) : 0f;
    public EntityStatusEffectRuntime StatusEffects => _statusEffects;
    public float InfectionRecoveryMultiplier =>
        Mathf.Max(0f, InfectionRecoverMultiplier) *
        (_statusEffects != null
            ? _statusEffects.GetMultiplier(StatusEffectModifierType.InfectionRecoveryMultiplier)
            : 1f);
    public float InfectionRecoveryWaitRemaining => Mathf.Max(0f, _naturalRecoveryAllowedTime - Time.time);
    public bool CanNaturallyRecover => AllowsNaturalRecovery && InfectionRecoveryWaitRemaining <= 0f;
    public float NaturalRecoveryPerSecond => Rules != null && CanNaturallyRecover
        ? Rules.NaturalDecayAmountPerTick / Rules.NaturalDecayInterval * InfectionRecoveryMultiplier
        : 0f;

    public float ZoneTickInterval =>
        Rules != null ? Rules.ZoneTickInterval : DefaultZoneTickInterval;

    protected virtual EntityInfectionSettings Rules =>
        _damageable != null && _damageable.StatConfig != null
            ? _damageable.StatConfig.Infection
            : null;

    protected virtual float InfectionGainMultiplier => 1f;
    protected virtual float InfectionRecoverMultiplier => 1f;
    // Override on entities whose Infection may only be reduced by an explicit
    // gameplay action, such as the player's Infection Cure consumable.
    protected virtual bool AllowsNaturalRecovery => true;
    protected virtual bool DebugInfectionLifecycle => false;
    protected abstract PaintFaction FriendlyPaintFaction { get; }

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveRefs();
        ClampInfectionToCurrentHealth();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    protected virtual void OnEnable()
    {
        ResolveRefs();

        if (_damageable != null)
        {
            _damageable.OnHealthChanged += HandleDamageableHealthChanged;
            _damageable.OnDamaged += HandleHealthDamageReceived;
        }
        if (_statusEffects != null)
            _statusEffects.OnChanged += HandleStatusEffectsChanged;
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManager = _maskRenderManagerReadyChannel.Current;

        StartRecoveryWait();
        PublishCurrentSnapshot();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected virtual void OnDisable()
    {
        if (_damageable != null)
        {
            _damageable.OnHealthChanged -= HandleDamageableHealthChanged;
            _damageable.OnDamaged -= HandleHealthDamageReceived;
        }
        if (_statusEffects != null)
            _statusEffects.OnChanged -= HandleStatusEffectsChanged;
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;
    }

    // Purpose: Processes this component's per-frame runtime work.
    protected virtual void Update()
    {
        _infectionRecoveryWaitRemaining = InfectionRecoveryWaitRemaining;

        // Synchronize normal death before processing natural recovery or contact effects.
        CheckDeathState();

        if (_isDead)
            return;

        TickNaturalDecay();
        TickZoneExposure();
    }
    // Purpose: Handles the incoming mask render manager ready request or event.
    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }
    // Purpose: Adds the requested infection to this component.
    public virtual void AddInfection(float amount)
        => ReceiveStatusInfectionDamage(amount);

    /// <summary>Applies the Infection resource directly without re-entering exposure accumulation.</summary>
    public void ReceiveStatusInfectionDamage(float amount, bool exactHealthLoss = false)
    {
        if (_isDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            return;

        StartRecoveryWait();
        float before = _currentInfection;
        _currentInfection += amount * (exactHealthLoss ? 1f : Mathf.Max(0f, InfectionGainMultiplier));
        _currentInfection = Mathf.Min(_currentInfection, InfectionLimit);
        _damageable?.SetInfectionMaxHealthLoss(_currentInfection);

        LogInfectionLifecycle(
            "AddInfection",
            $"requested={amount:0.###}, before={before:0.###}, afterRaw={_currentInfection:0.###}");

        // The health ceiling owns lethal erosion; current HP is not an infection threshold.
        CheckDeathState();

        if (_isDead)
            return;

        ClampInfectionToCurrentHealth();
        PublishCurrentSnapshot();
    }

    // Purpose: Performs the reduce infection workflow for this component.
    public void ReduceInfection(float amount)
    {
        if (_isDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            return;

        _currentInfection -= amount;
        ClampInfectionToCurrentHealth();
        _damageable?.SetInfectionMaxHealthLoss(_currentInfection);

        PublishCurrentSnapshot();
    }

    // Purpose: Clears the current infection state.
    public void ClearInfection()
    {
        _currentInfection = 0f;
        _damageable?.SetInfectionMaxHealthLoss(0f);
        PublishCurrentSnapshot();
    }

    /// <summary>
    /// Restores saved ceiling erosion before restoring current health.
    /// </summary>
    public void RestoreCurrentInfection(float infection)
    {
        if (_isDead)
            return;

        if (float.IsNaN(infection) || float.IsInfinity(infection)) return;
        _currentInfection = Mathf.Clamp(Mathf.Max(0f, infection), 0f, InfectionLimit);
        _damageable?.SetInfectionMaxHealthLoss(_currentInfection);
        CheckDeathState();

        if (!_isDead)
            PublishCurrentSnapshot();
    }

    // Purpose: Performs the recover on sector captured workflow for this component.
    public virtual void RecoverOnSectorCaptured()
    {
    }

    // Purpose: Performs the recover on named killed workflow for this component.
    public virtual void RecoverOnNamedKilled()
    {
    }

    // Purpose: Performs the recover on boss killed workflow for this component.
    public virtual void RecoverOnBossKilled()
    {
    }

    // Purpose: Publishes the current snapshot notification.
    public virtual void PublishCurrentSnapshot()
    {
        OnChanged?.Invoke(this);
    }

    // Purpose: Resolves the refs needed by this component.
    protected virtual void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        if (_statusEffects == null)
            _statusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this);

        if (_statusEffects == null && !_loggedMissingStatusEffects)
        {
            _loggedMissingStatusEffects = true;
            Debug.LogError(
                "[ShooterInfection] EntityStatusEffectRuntime is missing from the Shooter entity root.",
                this);
        }

        if (_samplePoint == null)
            _samplePoint = transform;

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();
    }

    // Purpose: Responds to the became dead callback.
    protected virtual void OnBecameDead(bool killedByInfection)
    {
    }

    // Purpose: Handles the incoming damageable health changed request or event.
    private void HandleDamageableHealthChanged(Damageable damageable)
    {
        // Ordinary wounds never turn existing erosion into a separate death threshold.
        CheckDeathState();

        if (_isDead)
            return;

        ClampInfectionToCurrentHealth();
        PublishCurrentSnapshot();
    }

    /// <summary>
    /// Forces this entity's Infection to its full maximum-health value and
    /// immediately resolves the normal InfectionOvertake death rule.
    /// Used by global Infection Control depletion so death keeps the same
    /// corpse, death-cause, and game-over path as other infection deaths.
    /// </summary>
    public void ForceInfectionToMaximumHealth()
    {
        if (_isDead)
            return;

        ResolveRefs();
        _currentInfection = InfectionLimit;
        _damageable?.SetInfectionMaxHealthLoss(_currentInfection);
        CheckDeathState();

        if (!_isDead)
            PublishCurrentSnapshot();
    }

    // Purpose: Handles the incoming health damage received request or event.
    private void HandleHealthDamageReceived(Damageable damageable, float damage)
    {
        if (damage > 0f)
            StartRecoveryWait();
    }

    // Purpose: Begins the recovery wait workflow.
    private void StartRecoveryWait()
    {
        EntityInfectionSettings rules = Rules;
        float waitSeconds = rules != null ? rules.InfectionRecoveryTimeWait : 0f;
        _naturalRecoveryAllowedTime = Time.time + Mathf.Max(0f, waitSeconds);
        _nextNaturalDecayTime = Mathf.Max(_nextNaturalDecayTime, _naturalRecoveryAllowedTime);
        _infectionRecoveryWaitRemaining = InfectionRecoveryWaitRemaining;
    }

    // Purpose: Processes one update step for natural decay.
    private void TickNaturalDecay()
    {
        EntityInfectionSettings rules = Rules;

        if (_isDead || rules == null)
            return;

        float interval = rules.NaturalDecayInterval;
        float amount = rules.NaturalDecayAmountPerTick;

        if (interval <= 0f || amount <= 0f)
            return;

        if (!CanNaturallyRecover)
            return;

        if (Time.time < _nextNaturalDecayTime)
            return;

        _nextNaturalDecayTime = Time.time + interval;
        ReduceInfection(amount * InfectionRecoveryMultiplier);
    }

    // Purpose: Processes one update step for zone exposure.
    private void TickZoneExposure()
    {
        if (!_enableZoneExposure || _isDead)
            return;

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_maskRenderManager == null || _samplePoint == null)
            return;

        ResolveCurrentExposure(out ExposureKind exposureKind, out PoisonPuddleDamageConfigSO poisonConfig);

        if (exposureKind == ExposureKind.None)
        {
            ResetZoneTick();
            return;
        }

        if (exposureKind != _activeExposureKind ||
            poisonConfig != _activePoisonPuddleConfig)
        {
            _activeExposureKind = exposureKind;
            _activePoisonPuddleConfig = poisonConfig;
            _zoneTickTimer = 0f;
            ApplyExposureTick(exposureKind, poisonConfig);
        }

        _zoneTickTimer += Time.deltaTime;

        while (_zoneTickTimer >= ZoneTickInterval)
        {
            _zoneTickTimer -= ZoneTickInterval;
            ApplyExposureTick(exposureKind, poisonConfig);
        }
    }

    // Purpose: Resolves the current exposure needed by this component.
    private void ResolveCurrentExposure(
        out ExposureKind exposureKind,
        out PoisonPuddleDamageConfigSO poisonConfig)
    {
        exposureKind = ExposureKind.None;
        poisonConfig = null;

        if (_maskRenderManager.TryGetPoisonPuddleAtWorld(
                _samplePoint.position,
                out poisonConfig,
                _requireOpenedSector) &&
            poisonConfig != null &&
            poisonConfig.HasDamage)
        {
            exposureKind = ExposureKind.PoisonPuddle;
            return;
        }

        if (!_maskRenderManager.TryGetStateAtWorld(
                _samplePoint.position,
                out PaintSurfaceState state,
                _requireOpenedSector))
        {
            return;
        }

        switch (state)
        {
            case PaintSurfaceState.Virus:
            case PaintSurfaceState.CoatedVirus:
                exposureKind = ExposureKind.Virus;
                break;

            case PaintSurfaceState.Vaccine:
            case PaintSurfaceState.CoatedVaccine:
                exposureKind = ExposureKind.Vaccine;
                break;

            case PaintSurfaceState.PoisonPuddle:
                exposureKind = ExposureKind.PoisonPuddle;
                break;
        }
    }

    // Purpose: Applies the requested exposure tick change.
    private void ApplyExposureTick(
        ExposureKind exposureKind,
        PoisonPuddleDamageConfigSO poisonConfig)
    {
        switch (exposureKind)
        {
            case ExposureKind.Virus:
                ApplyPaintContact(PaintChannel.Virus);
                break;

            case ExposureKind.Vaccine:
                ApplyPaintContact(PaintChannel.Vaccine);
                break;

            case ExposureKind.PoisonPuddle:
                ApplyPoisonPuddleTick(poisonConfig);
                break;
        }
    }

    // Purpose: Applies the requested poison puddle tick change.
    private void ApplyPoisonPuddleTick(PoisonPuddleDamageConfigSO config)
    {
        if (config == null)
            return;

        if (_damageable != null && _damageable.CanReceiveDamage)
        {
            float healthDamage = config.HealthDamagePerTick;

            if (healthDamage > 0f)
                _damageable.ReceiveAnAttack(healthDamage);
        }

        float infectionGain = config.InfectionGainPerTick;

        if (infectionGain > 0f)
            AddInfection(infectionGain);
    }

    // Purpose: Performs the reset zone tick workflow for this component.
    private void ResetZoneTick()
    {
        _activeExposureKind = ExposureKind.None;
        _activePoisonPuddleConfig = null;
        _zoneTickTimer = 0f;
    }

    // Purpose: Applies the requested paint contact change.
    private void ApplyPaintContact(PaintChannel channel)
    {
        if (_statusEffects == null)
            return;

        PaintFaction contactFaction = PaintTypeUtility.ToFaction(channel);

        if (contactFaction == FriendlyPaintFaction)
        {
            _statusEffects.RemoveEffectsClearedByFriendlyPaint();
            return;
        }

        if (contactFaction == PaintFaction.Neutral ||
            contactFaction == PaintFaction.Special ||
            _maskRenderManager == null ||
            !_maskRenderManager.TryGetMaskRender(channel, out MaskRenderSO maskRender) ||
            maskRender == null)
        {
            return;
        }

        StatusEffectDefinitionSO[] definitions = maskRender.HostileContactEffects;

        if (definitions == null || definitions.Length == 0)
        {
            Debug.LogError(
                $"[ShooterInfection] Hostile paint has no contact status definitions. channel={channel}, entity={name}",
                this);
            return;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            if (definitions[i] != null)
                _statusEffects.Apply(definitions[i]);
        }
    }

    // Purpose: Performs the clamp infection to current health workflow for this component.
    private void ClampInfectionToCurrentHealth()
    {
        _currentInfection = Mathf.Clamp(_currentInfection, 0f, InfectionLimit);
    }

    // Purpose: Performs the check death state workflow for this component.
    private void CheckDeathState()
    {
        if (_isDead)
            return;

        if (_damageable == null)
        {
            Debug.LogError(
                "[ShooterInfection] Damageable is required for infection death resolution.",
                this);
            return;
        }

        if (_damageable.IsDead)
        {
            MarkDead(_damageable.LastDeathCause == EntityDeathCause.InfectionOvertake);
            return;
        }

        if (CurrentHealth <= 0f)
        {
            _damageable.Kill(EntityDeathCause.HealthDepleted);
            return;
        }

        // No Infection >= current HP rule. Erosion is lethal only when the health cap reaches zero.
    }

    // Purpose: Performs the mark dead workflow for this component.
    private void MarkDead(bool killedByInfection)
    {
        if (_isDead)
            return;

        _isDead = true;
        OnBecameDead(killedByInfection);
        PublishCurrentSnapshot();
    }

    // Purpose: Handles the incoming status effects changed request or event.
    private void HandleStatusEffectsChanged(EntityStatusEffectRuntime runtime)
    {
        PublishCurrentSnapshot();
    }

    // Purpose: Performs the log infection lifecycle workflow for this component.
    private void LogInfectionLifecycle(string reason, string detail)
    {
        if (!DebugInfectionLifecycle)
            return;

        string damageableName = _damageable != null
            ? GetHierarchyPath(_damageable.transform)
            : "null";

        Debug.Log(
            $"[ShooterInfection][Lifecycle] reason={reason}, " +
            $"type={GetType().Name}, owner={GetHierarchyPath(transform)}, " +
            $"damageable={damageableName}, currentInfection={_currentInfection:0.###}, " +
            $"currentHealth={CurrentHealth:0.###}, maxHealth={MaxHealth:0.###}, " +
            $"infectionLimit={InfectionLimit:0.###}, infectionDead={_isDead}, " +
            $"damageableDead={(_damageable != null && _damageable.IsDead)}, " +
            $"deathCause={(_damageable != null ? _damageable.LastDeathCause : EntityDeathCause.None)}, " +
            $"detail={detail}",
            this);
    }

    // Purpose: Returns the current hierarchy path.
    private static string GetHierarchyPath(Transform current)
    {
        if (current == null)
            return "null";

        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}
