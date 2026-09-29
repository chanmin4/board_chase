/*
 * Role hierarchy: Gameplay > Items > Weapons > Runtime
 * Primary responsibility: Gates unfinished legacy projectile secondary effects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

/// <summary>
/// Keeps the old normal-bullet Infection and Mark paths compiled while those
/// systems are being redesigned. Do not enable either flag until their
/// dedicated combat rules have been authored and tested again.
/// </summary>
public static class ProjectileCombatFeatureFlags
{
    public static bool EnableLegacyBasicBulletMark => false;
    public static bool EnableLegacyBasicBulletInfectionDamage => false;
}
