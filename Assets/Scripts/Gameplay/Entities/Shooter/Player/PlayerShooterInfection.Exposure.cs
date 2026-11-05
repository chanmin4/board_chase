using UnityEngine;

/// <summary>Compatibility input adapter. Accumulation, stacks and ticking are owned by EntityStatusEffectRuntime.</summary>
public partial class PlayerShooterInfection
{
    public float ExposureGauge => StatusEffects != null ? StatusEffects.InfectionAggravationGauge : 0f;
    public int ExposureStacks => StatusEffects != null ? StatusEffects.InfectionAggravationStacks : 0;
    public float ExposureCapacity => StatusEffects != null ? StatusEffects.InfectionAggravationCapacity : 100f;

    public override void AddInfection(float amount)
    {
        if (InfectionAggravationStatusSO.Active?.Settings.enabled != true) { base.AddInfection(amount); return; }
        AddExposure(amount);
    }

    public void AddExposure(float amount)
    {
        if (!IsDead && GameProgressionRuntime.InRaid) StatusEffects?.AddInfectionAggravation(amount);
    }

    public void TreatExposure(float amount) => StatusEffects?.TreatInfectionAggravation(amount);

    public void AddHitExposure(float actualDamage)
    {
        var settings = InfectionAggravationStatusSO.Active?.Settings;
        if (settings == null || !InfectionAggravationMath.FinitePositive(actualDamage)) return;
        AddExposure(actualDamage * settings.currentHealthLossExposureFraction);
    }
}
