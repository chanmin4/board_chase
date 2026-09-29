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
    [SerializeField, Min(0)] private int penetrationClass = 1;

    [Header("Direct Combat Values")]
    [Tooltip("Balance tier used by loot, shops, and debug TTK tables. It adds no hidden damage.")]
    [SerializeField] private BulletTier _tier = BulletTier.Tier1;

    [Tooltip("Raw Health damage before weapon and Shooter modifiers.")]
    [SerializeField, Min(0f)] private float _healthDamage = 30f;

    [Tooltip("Legacy normal-bullet Infection value retained only so existing assets deserialize safely. It is disabled while the Infection Injection ammo WIP is active.")]
    [SerializeField, HideInInspector, Min(0f)] private float _infectionDamage = 10f;

    public override BulletAmmoType AmmoType => BulletAmmoType.Basic;
    // Existing BasicBulletSO assets remain light ammunition. Use a concrete
    // Medium/Heavy/Sniper/Shotgun/Rocket SO type for the other families.
    public override BulletFamily Family => BulletFamily.Light;
    public override int PenetrationClass => Mathf.Max(0, penetrationClass);
    public override BulletTier Tier => _tier;
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
        return faction == ShooterFaction.Enemy
            ? enemyDamageableTargetMask
            : playerDamageableTargetMask;
    }
}
