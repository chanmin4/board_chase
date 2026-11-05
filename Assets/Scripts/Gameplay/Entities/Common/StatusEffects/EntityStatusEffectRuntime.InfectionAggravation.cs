using UnityEngine;

public sealed partial class EntityStatusEffectRuntime
{
    private readonly InfectionExposureSaveData _localAggravation = new();
    private int _notifiedAggravationStacks = -1;
    private InfectionExposureSaveData AggravationState
    {
        get
        {
#if UNITY_EDITOR
            if (CombatLabRuntime.IsIsolatedSession) return _localAggravation;
#endif
            return GetComponent<PlayerShooterInfection>() != null && SaveSystem.Active != null
                ? SaveSystem.Active.RunData.infectionExposure ??= new() : _localAggravation;
        }
    }
    public float InfectionAggravationGauge => Mathf.Max(0, AggravationState.gauge);
    public int InfectionAggravationStacks => Mathf.Max(0, AggravationState.stacks);
    public float InfectionAggravationCapacity => Mathf.Max(1,
        (InfectionAggravationStatusSO.Active?.Settings.baseGaugeCapacity ?? 100) +
        (GetComponent<PlayerInventoryRuntime>()?.InfectionCapacityBonus ?? 0) +
        (GetComponent<PlayerStatsRuntime>()?.AdditionalStat(PlayerStatId.InfectionDefense) ?? 0));

    private void ApplyCompletedInfectionCycles(int completed, InfectionAggravationStatusSO definition)
    {
        if (completed <= 0) return;
        var health = EntityComponentResolver.Resolve<Damageable>(this);
        if (health == null) return;
        float loss = health.UninfectedMaxHealth * Mathf.Clamp01(definition.Settings.maxHealthLossFraction) * completed;
        var shooter = EntityComponentResolver.Resolve<ShooterInfection>(this);
        if (shooter != null) shooter.ReceiveStatusInfectionDamage(loss, exactHealthLoss: true);
        else health.SetInfectionMaxHealthLoss(health.InfectionMaxHealthLoss + loss);
    }

    public void AddInfectionAggravation(float amount, bool applyAmpoule = true)
    {
        var definition = InfectionAggravationStatusSO.Active;
        if (definition == null || !definition.Settings.enabled || !InfectionAggravationMath.FinitePositive(amount)) return;
        if (EntityComponentResolver.Resolve<Damageable>(this)?.IsDead == true) return;
        var data = AggravationState;
        var player = GetComponent<PlayerShooterInfection>();
        if (player != null && applyAmpoule) amount *= player.AmpouleExposureMultiplier;
        int completed = InfectionAggravationMath.Accumulate(data, amount, InfectionAggravationCapacity, definition.MaximumStacks, definition.Settings.naturalRecoveryDelaySeconds);
        ApplyCompletedInfectionCycles(completed, definition);
        SyncInfectionAggravation();
    }

    public void TreatInfectionAggravation(float amount)
    {
        if (!InfectionAggravationMath.FinitePositive(amount)) return;
        AggravationState.gauge = Mathf.Max(0, AggravationState.gauge - amount);
        SyncInfectionAggravation();
    }

    public void RefreshInfectionErosionStacks(float remainingLoss)
    {
        float perStack = (EntityComponentResolver.Resolve<Damageable>(this)?.UninfectedMaxHealth ?? 0) *
            (InfectionAggravationStatusSO.Active?.Settings.maxHealthLossFraction ?? 0);
        if (perStack > 0)
            AggravationState.stacks = Mathf.Clamp(Mathf.CeilToInt(remainingLoss / perStack), 0, InfectionAggravationStatusSO.Active.MaximumStacks);
        SyncInfectionAggravation();
    }

    private void ClearInfectionAggravation()
    {
        var data = AggravationState;
        data.gauge = 0; data.stacks = 0; data.recoveryDelayRemaining = 0;
        data.nextTick = InfectionAggravationStatusSO.Active?.DamageTickInterval ?? 5f;
    }

    private void TickInfectionAggravation(float delta)
    {
        var definition = InfectionAggravationStatusSO.Active;
        if (definition == null || !definition.Settings.enabled || !InfectionAggravationMath.FinitePositive(delta)) return;
        var player = GetComponent<PlayerShooterInfection>();
        if (player != null && (!GameProgressionRuntime.InRaid || player.IsDead || (player.IsDeployed && !RaidRuntime.Active.IsPlayerInRaid))) return;
        var data = AggravationState;
        float capacity = InfectionAggravationCapacity;
        if (data.gauge >= capacity)
            ApplyCompletedInfectionCycles(InfectionAggravationMath.Accumulate(data, 0, capacity, definition.MaximumStacks, 0), definition);
        InfectionAggravationMath.Tick(data, delta, capacity, 0, 0, player != null ? player.AmpouleGaugeRecoveryPerSecond : 0);
        SyncInfectionAggravation();
    }

    private void SyncInfectionAggravation()
    {
        var definition = InfectionAggravationStatusSO.Active;
        if (definition == null) return;
        var data = AggravationState;
        var active = _activeEffects.Find(effect => effect.Definition is InfectionAggravationStatusSO);
        if (active != null && !active.UsesAccumulationState(data))
        {
            _activeEffects.Remove(active); active = null;
        }
        if (data.stacks <= 0 && data.gauge <= 0)
        {
            if (active != null) { _activeEffects.Remove(active); OnChanged?.Invoke(this); }
            return;
        }
        if (active == null)
        {
            _activeEffects.Add(new ActiveStatusEffect(definition, float.PositiveInfinity, data));
            _notifiedAggravationStacks = data.stacks;
            OnChanged?.Invoke(this);
        }
        else if (_notifiedAggravationStacks != data.stacks)
        {
            _notifiedAggravationStacks = data.stacks;
            OnChanged?.Invoke(this);
        }
    }
}
