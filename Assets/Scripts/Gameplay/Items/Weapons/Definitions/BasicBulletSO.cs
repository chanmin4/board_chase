/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the BasicBulletSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "BasicBulletItem",
    menuName = "Game/Bullet Item/Basic")]
public class BasicBulletSO : BulletSO
{
    [Header("Damage Targets")]
    [Tooltip("Damageable layers when this bullet is fired by the player.")]
    [SerializeField] private LayerMask playerDamageableTargetMask;

    [Tooltip("Damageable layers when this bullet is fired by an enemy.")]
    [SerializeField] private LayerMask enemyDamageableTargetMask;

    [Header("Penetration")]
    [Tooltip("Equal or higher than the effective Armor Class deals full Health damage. Infection and Mark ignore armor penetration.")]
    [SerializeField, HideInInspector, Min(0)] private float penetrationClass = 1;

    [Header("Direct Combat Values")]
    [Tooltip("Balance tier used by loot, shops, and debug TTK tables. It adds no hidden damage.")]
    [SerializeField] private BulletTier _tier = BulletTier.Tier1;

    [Tooltip("Raw Health damage before weapon and Shooter modifiers.")]
    [SerializeField, HideInInspector] private float _healthDamage = 30f; // WIP authored payload; new damage is character baseline + tier adjustment.

    [Tooltip("Legacy normal-bullet Infection value retained only so existing assets deserialize safely. It is disabled while the Infection Injection ammo WIP is active.")]
    [SerializeField, HideInInspector, Min(0f)] private float _infectionDamage = 10f;
    [SerializeField, Min(0f)] private float _infectionExposureOnHit;
    public override float InfectionExposureOnHit => Mathf.Max(0f, _infectionExposureOnHit);

    public override BulletAmmoType AmmoType => BulletAmmoType.Basic;
    // Existing BasicBulletSO assets remain light ammunition. Use a concrete
    // Medium/Heavy/Sniper/Shotgun/Rocket SO type for the other families.
    public override BulletFamily Family => BulletFamily.Light;
    public override float PenetrationClass => CombatBalanceRules.Penetration((int)Tier);
    public override BulletTier Tier => _tier;
    // Legacy generic shooters still consume an absolute payload. Character stats use the tier adjustment separately.
    public override float HealthDamage => Mathf.Max(0f, _healthDamage);
    public override float InfectionDamage =>
        ProjectileCombatFeatureFlags.EnableLegacyBasicBulletInfectionDamage
            ? Mathf.Max(0f, _infectionDamage)
            : 0f;
    public override float MarkAmount =>
        ProjectileCombatFeatureFlags.EnableLegacyBasicBulletMark
            ? InfectionDamage
            : 0f;

    // Purpose: Resolves the damage target mask needed by this component.
    public override LayerMask ResolveDamageTargetMask(ShooterFaction faction)
    {
        return faction != ShooterFaction.Player
            ? enemyDamageableTargetMask
            : playerDamageableTargetMask;
    }
}
