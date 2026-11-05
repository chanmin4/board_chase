using UnityEngine;

/// <summary>
/// One ballistic roll at the active outer protection layer, not three compounded rolls.
/// Successful hits retain 50–100% HP damage; failed hits retain 5–30%.
/// </summary>
public static class RaidArmorPenetration
{
    public const float MaximumPenetrationProbability = .90f;
    public readonly struct Result
    {
        public readonly float probability, healthMultiplier, wearMultiplier;
        public readonly bool penetrated;
        public Result(float probability, float healthMultiplier, float wearMultiplier, bool penetrated)
        { this.probability = probability; this.healthMultiplier = healthMultiplier; this.wearMultiplier = wearMultiplier; this.penetrated = penetrated; }
    }
    public static bool Enabled => RaidRuntime.Active?.State?.active == true && RaidRuntime.Active.State.layout?.generationVersion == 3;
    public static Result Resolve(float armorClass, float penetrationClass, float current, float maximum, float roll)
    {
        if (armorClass <= 0f) return new Result(1, 1, 0, true);
        float gap = Mathf.Max(0f, armorClass - Mathf.Max(0, penetrationClass));
        float condition = maximum > 0f ? Mathf.Clamp01(current / maximum) : 1f;
        float probability = Mathf.Min(MaximumPenetrationProbability,
            Mathf.Max(.05f, MaximumPenetrationProbability * Mathf.Pow(.54f, gap)) + (1f - condition) * .15f);
        bool penetrated = Mathf.Clamp01(roll) < probability;
        float healthMultiplier = penetrated
            ? Mathf.Clamp(1f - .1f * gap, .5f, 1f)
            : Mathf.Clamp(.3f - .05f * gap, .05f, .3f);
        return new Result(probability, healthMultiplier, penetrated ? 1f : .35f, penetrated);
    }
}
