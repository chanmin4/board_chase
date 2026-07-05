using UnityEngine;
using UnityEngine.Serialization;

public enum BulletAmmoType
{
    AttackAndPaint = 3,
    Attack = 0,
    Paint = 1,
    Special = 2
}

public enum ShooterFaction
{
    Player,
    Enemy
}

public enum BulletCombatRole
{
    Attacker=0,
    Painter=1,
    Balance=2
}

[CreateAssetMenu(
    fileName = "BulletItem",
    menuName = "Game/Bullet Item")]
public abstract class BulletSO : ItemSO
{
    [Header("Identity")]
    [SerializeField] private string bulletId = "bullet_default";
    [SerializeField] private string displayName = "Bullet";

    [Header("Projectile Prefab")]
    [Tooltip("Projectile prefab fired by this bullet item. This is not the world pickup prefab.")]
    [FormerlySerializedAs("bulletPrefab")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Flight")]
    [Min(0.1f)] [SerializeField] private float speed = 18f;
    [Min(0.001f)] [SerializeField] private float castRadius = 0.08f;
    [Min(0.01f)] [SerializeField] private float maxLifetime = 2f;
    [Min(0f)] [SerializeField] private float spawnOffset = 0.12f;


    [Header("Projectile Collision")]
    [Tooltip("Layers this projectile raycast/sweep can physically collide with.")]
    [SerializeField] private LayerMask projectileCollisionMask = 0;

    [SerializeField] private QueryTriggerInteraction projectileTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Damage Targets")]
    [Tooltip("Damageable layers when this bullet is fired by the player.")]
    [SerializeField] private LayerMask playerDamageableTargetMask = 0;
    [SerializeField] private LayerMask enemyDamageableTargetMask = 0;

    [Header("Penetration")]
    [Tooltip("Bullet penetration class. Equal or higher than armor class deals full health/mark damage.")]
    [SerializeField, Min(0)] private int penetrationClass = 1;

    [Tooltip("Used by UI to decide whether this ammo should show health damage ratio or mark damage ratio.")]
    [SerializeField] private BulletCombatRole combatRole = BulletCombatRole.Attacker;

    [Header("Stat Modifiers")]
    [Tooltip("Bullet-specific values on top of player stats. Supports damage, shots per second, paint radius, magazine size, and other player stats.")]
    [SerializeField] private PlayerStatModifier[] statModifiers;

    public string BulletId => bulletId;
    public string DisplayName => displayName;
    public abstract BulletAmmoType AmmoType { get; }
    public bool CanAttack =>
        AmmoType == BulletAmmoType.AttackAndPaint ||
        AmmoType == BulletAmmoType.Attack;
    public bool CanPaint =>
        AmmoType == BulletAmmoType.AttackAndPaint ||
        AmmoType == BulletAmmoType.Paint;
    public bool IsPrimary =>
        AmmoType == BulletAmmoType.AttackAndPaint ||
        AmmoType == BulletAmmoType.Attack;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float Speed => speed;
    public float CastRadius => castRadius;
    public float MaxLifetime => maxLifetime;
    public float SpawnOffset => spawnOffset;
    public LayerMask ProjectileCollisionMask => projectileCollisionMask;
    public QueryTriggerInteraction ProjectileTriggerInteraction => projectileTriggerInteraction;
    public int PenetrationClass => Mathf.Max(0, penetrationClass);
    public BulletCombatRole CombatRole => combatRole;
    public bool IsPainterRole => combatRole == BulletCombatRole.Painter;
    public bool IsBalanceRole => combatRole == BulletCombatRole.Balance;
    public PlayerStatModifier[] StatModifiers => statModifiers;
    public LayerMask ResolveDamageTargetMask(ShooterFaction faction)
    {
        if (!CanAttack)
            return 0;

        return faction == ShooterFaction.Enemy
            ? enemyDamageableTargetMask
            : playerDamageableTargetMask;
    }
}
