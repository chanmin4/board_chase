/*
 * Role hierarchy: Gameplay > Entities > Common > Equipment
 * Primary responsibility: Resolves one-shot direct-health damage rolls from armor versus penetration.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Direct bullet damage is full at equal/higher penetration. When armor is
/// higher, a hit rolls between the curve minimum and full damage. The curve
/// has a 10% floor, avoiding the old multiplicative near-immunity state.
/// </summary>
public static class ArmorPenetrationDamageCurve
{
    public const float MinimumDamageFloor = 0.10f;
    public const float GapFalloff = 0.55f;

    // Purpose: Resolves the minimum multiplier needed by this component.
    /// <summary>
    /// Supports fractional effective armor so permanent bonuses such as
    /// <c>Armor Class +0.5</c> affect the same curve without rounding.
    /// Item armor and bullet penetration authoring remain integer classes.
    /// </summary>
    public static float ResolveMinimumMultiplier(float effectiveArmorClass, int penetrationClass)
    {
        float classGap = Mathf.Max(0f, effectiveArmorClass - Mathf.Max(0, penetrationClass));
        if (classGap <= 0f)
            return 1f;

        return Mathf.Clamp01(
            MinimumDamageFloor +
            (1f - MinimumDamageFloor) * Mathf.Exp(-GapFalloff * classGap));
    }

    // Purpose: Rolls the multiplier result from its configured probabilities.
    public static float RollMultiplier(float effectiveArmorClass, int penetrationClass)
    {
        float minimumMultiplier = ResolveMinimumMultiplier(
            effectiveArmorClass,
            penetrationClass);

        return minimumMultiplier >= 0.9999f
            ? 1f
            : Random.Range(minimumMultiplier, 1f);
    }
}
