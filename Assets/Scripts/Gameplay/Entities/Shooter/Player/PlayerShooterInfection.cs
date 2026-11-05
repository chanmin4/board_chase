/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Handles PlayerShooterInfection combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public partial class PlayerShooterInfection : ShooterInfection
{
    protected override PaintFaction FriendlyPaintFaction => PaintFaction.Vaccine;
    [Header("Difficulty")]
    [SerializeField] private DifficultyRulesSO _difficultyRules;

    [Header("Broadcasting On")]
    [SerializeField] private PlayerHealthEventChannelSO _playerHealthChanged;

    [SerializeField] private PlayerInfectionEventChannelSO _playerInfectionReadyChannel;
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;
    private float PassiveGasMultiplier => 1f; // Gas mitigation is equipment-owned, not a mandatory passive.
    protected override float InfectionGainMultiplier =>
        ActiveDifficultyRules != null ? ActiveDifficultyRules.PlayerInfectionGainMultiplier : 1f;

    // Gas masks/ampoules reduce incoming exposure. Only syringes and HOME purification restore erosion.
    protected override bool AllowsNaturalRecovery => false;

    private float _gasTickElapsed;
    private float _homeAmpouleSeconds;
    private float _homeAmpouleRate;
    private float _homeAmpouleMultiplier = .5f;
    private RaidRunSaveData MedicalState
    {
        get
        {
#if UNITY_EDITOR
            if (CombatLabRuntime.IsIsolatedSession) return null;
#endif
            return RaidRuntime.Active != null && RaidRuntime.Active.State != null &&
                RaidRuntime.Active.State.active && !RaidRuntime.Active.State.extractionCommitted
                ? RaidRuntime.Active.State : null;
        }
    }
    public bool IsDeployed => MedicalState != null;
    public float SuppressionRemainingSeconds => MedicalState != null
        ? MedicalState.suppressionRemainingSeconds : 180f;
    public bool IsSuppressionMaintained => SuppressionRemainingSeconds > 0f;
    public bool IsAmpouleActive => (MedicalState != null ? MedicalState.ampouleRemainingSeconds : _homeAmpouleSeconds) > 0f;
    public float AmpouleRemainingSeconds => Mathf.Max(0, MedicalState != null ? MedicalState.ampouleRemainingSeconds : _homeAmpouleSeconds);
    public float AmpouleExposureMultiplier => IsAmpouleActive ? Mathf.Clamp01(MedicalState?.ampouleExposureMultiplier ?? _homeAmpouleMultiplier) : 1f;
    public float AmpouleGaugeRecoveryPerSecond => IsAmpouleActive ? Mathf.Max(0, MedicalState?.ampouleRecoveryPerSecond ?? _homeAmpouleRate) : 0f;
    public int MedicalDose => MedicalState != null && RaidRuntime.Active.Definition != null
        ? RaidRuntime.Active.Definition.RaidRuntime.MedicalDose(RaidRuntime.Active.Definition.StageIndex) : 1;
    public float CurrentInfectionPressure => Mathf.Max(0f, RawGasPressure - GasDefense) * AmpouleExposureMultiplier;
    private float PressureRate => MedicalState != null && RaidRuntime.Active.Definition != null
        ? Mathf.Max(0f, RaidRuntime.Active.Definition.RaidRuntime.infectionPerPressureSecond) : 1f;
    private float RawGasPressure => IsDeployed
        ? SectorRoomTraitRuntime.CurrentPlayerBiochemicalGasInfectionPerSecond : 0f;
    private int GasDefense => _inventoryRuntime != null ? _inventoryRuntime.GasDefenseLevel : 0;

    public void ApplySuppression(float seconds)
    {
        var state = MedicalState;
        if (state == null || IsDead || !InfectionAggravationMath.FinitePositive(seconds)) return;
        float bonus = EntityComponentResolver.Resolve<PlayerStatsRuntime>(this)?.AdditionalStat(PlayerStatId.SuppressionDurationBonus) ?? 0f;
        state.suppressionRemainingSeconds = Mathf.Max(state.suppressionRemainingSeconds, seconds + Mathf.Max(0f, bonus));
    }

    public bool TryStartAmpoule(float rate, float seconds, float exposureMultiplier = .5f)
    {
        if (IsDead || IsAmpouleActive || !GameProgressionRuntime.InRaid ||
            !InfectionAggravationMath.FinitePositive(seconds) || float.IsNaN(rate) || float.IsInfinity(rate) || rate < 0 ||
            float.IsNaN(exposureMultiplier) || float.IsInfinity(exposureMultiplier))
            return false;
        float bonusDuration = EntityComponentResolver.Resolve<PlayerStatsRuntime>(this)?.AdditionalStat(PlayerStatId.SuppressionDurationBonus) ?? 0;
        if (!float.IsNaN(bonusDuration) && !float.IsInfinity(bonusDuration)) seconds += Mathf.Max(0, bonusDuration);
        if (MedicalState != null)
        {
            MedicalState.ampouleRecoveryPerSecond = rate;
            MedicalState.ampouleRemainingSeconds = seconds;
            MedicalState.ampouleExposureMultiplier = Mathf.Clamp01(exposureMultiplier);
        }
        else { _homeAmpouleRate = rate; _homeAmpouleSeconds = seconds; _homeAmpouleMultiplier = Mathf.Clamp01(exposureMultiplier); }
        return true;
    }

    public bool TryUseInfectionSyringe(float amount)
    {
        if (IsDead || CurrentInfection <= 0f || !InfectionAggravationMath.FinitePositive(amount)) return false;
        ReduceInfection(amount); // Restores the ceiling, not current HP.
        StatusEffects?.RefreshInfectionErosionStacks(CurrentInfection);
        return true;
    }

    public bool TryPurifyAtHome()
    {
        if (IsDead || GameProgressionRuntime.InRaid) return false;
        ClearInfection();
        StatusEffects?.Remove(InfectionAggravationStatusSO.Active);
        _homeAmpouleSeconds = 0f;
        return true;
    }

    private DifficultyRulesSO ActiveDifficultyRules =>
        _difficultyRules != null ? _difficultyRules : DifficultyRuntime.CurrentRules;

    /// <summary>
    /// The current room-gas Infection rate after the equipped gas mask has
    /// absorbed its share. This intentionally excludes natural recovery: HUD
    /// feedback should answer "what is this room doing to me right now?".
    /// </summary>
    public float CurrentBiochemicalGasInfectionPerSecond
    {
        get
        {
            if (_inventoryRuntime == null)
                _inventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);
            return Mathf.Max(0f, RawGasPressure - GasDefense) * PressureRate * AmpouleExposureMultiplier *
                (InfectionAggravationStatusSO.Active?.Settings.gasExposurePerPressureSecond ?? 1f);
        }
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    protected override void OnEnable()
    {
        base.OnEnable();

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.RaiseEvent(this);

    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected override void OnDisable()
    {
        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.Clear(this);
 
        base.OnDisable();
    }

    // Purpose: Processes this component's per-frame runtime work.
    protected override void Update()
    {
        base.Update();

        if (IsDead) return;
        if (_inventoryRuntime == null)
            _inventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);
        RaidRunSaveData state = MedicalState;
        if (state != null && !RaidRuntime.Active.IsPlayerInRaid) return; // Loading/teleporting pauses treatment.
        float delta = Time.deltaTime;
        if (state != null)
        {
            state.ampouleRemainingSeconds = Mathf.Max(0f, state.ampouleRemainingSeconds - delta);
            float overdue = Mathf.Max(0f, delta - state.suppressionRemainingSeconds);
            state.suppressionRemainingSeconds = Mathf.Max(0f, state.suppressionRemainingSeconds - delta);
            StatusEffects?.AddInfectionAggravation(
                overdue * (InfectionAggravationStatusSO.Active?.Settings.overdueSuppressionExposurePerSecond ?? 1f),
                applyAmpoule: false);
        }
        else _homeAmpouleSeconds = Mathf.Max(0f, _homeAmpouleSeconds - delta);

        // Filter wear and gas events are batched, not broadcast for every render frame.
        if (RawGasPressure <= 0f) { _gasTickElapsed = 0f; return; }
        _gasTickElapsed += delta;
        if (_gasTickElapsed >= 0.5f)
        {
            float exposure = _inventoryRuntime != null
                ? _inventoryRuntime.ResolveGasPressureExposure(RawGasPressure, _gasTickElapsed, PressureRate)
                : RawGasPressure * _gasTickElapsed * PressureRate;
            _gasTickElapsed = 0f;
            AddInfection(exposure * PassiveGasMultiplier*(InfectionAggravationStatusSO.Active?.Settings.gasExposurePerPressureSecond??1));
        }
    }

    // Purpose: Publishes the current snapshot notification.
    public override void PublishCurrentSnapshot()
    {
        base.PublishCurrentSnapshot();

        if (_playerHealthChanged == null)
            return;

        _playerHealthChanged.RaiseEvent(new PlayerHealthSnapshot(
            MaxHealth,
            CurrentHealth,
            CurrentInfection,
            NaturalRecoveryPerSecond,
            IsDead));
    }

    // Purpose: Performs the recover on sector captured workflow for this component.
    public override void RecoverOnSectorCaptured()
    {
    }

    // Purpose: Performs the recover on named killed workflow for this component.
    public override void RecoverOnNamedKilled()
    {
    }

    // Purpose: Performs the recover on boss killed workflow for this component.
    public override void RecoverOnBossKilled()
    {
    }
}
