using UnityEngine;

/// <summary>
/// One ballistic roll at the active outer protection layer, not three compounded rolls.
/// k=2.5 ratio probability, 5–95%. Success retains 50–100%, failure 15%.
/// </summary>
public static class RaidArmorPenetration
{
    public const float MaximumPenetrationProbability = .95f;
    public readonly struct Result
    {
        public readonly float probability, healthMultiplier, wearMultiplier;
        public readonly bool penetrated;
        public Result(float probability, float healthMultiplier, float wearMultiplier, bool penetrated)
        { this.probability = probability; this.healthMultiplier = healthMultiplier; this.wearMultiplier = wearMultiplier; this.penetrated = penetrated; }
    }
    public static bool Enabled => true; // Shared ballistic policy in HOME, raids and test encounters.
    public static Result Resolve(float armorClass, float penetrationClass, float current, float maximum, float roll)
    {
        if (armorClass <= 0f) return new Result(1, 1, 0, true);
        float ratio = Mathf.Clamp01(Mathf.Max(0f, penetrationClass) / armorClass);
        var rules=CombatBalanceRulesSO.Active;
        float k=Mathf.Max(.01f,rules?.penetrationCurve??2.5f);
        float minimum=Mathf.Clamp01(rules?.minimumPenetration??.05f);
        float maximumProbability=Mathf.Max(minimum,Mathf.Clamp01(rules?.maximumPenetration??.95f));
        float probability = minimum + (maximumProbability-minimum) * (Mathf.Exp(k * ratio) - 1f) / (Mathf.Exp(k) - 1f);
        bool penetrated = Mathf.Clamp01(roll) < probability;
        float healthMultiplier = penetrated ? .5f + .5f * ratio : Mathf.Clamp01(rules?.failedHitDamage??.15f);
        return new Result(probability, healthMultiplier, penetrated ? 1f : .35f, penetrated);
    }
}
