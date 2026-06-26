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
    [Header("Stat Modifiers")]
    [Tooltip("Bullet-specific values on top of player stats. Supports damage, shots per second, paint radius, magazine size, and other player stats.")]
    [SerializeField] private PlayerStatModifier[] statModifiers;

    [Header("Paint Mark On Hit")]
    [Tooltip("Mark amount added to the damaged target when this bullet hits. The shooter context decides Vaccine or Virus.")]
    [FormerlySerializedAs("vaccineMarkAmountOnHit")]
    [SerializeField, Min(0f)] private float paintMarkAmountOnHit = 0f;

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
    public QueryTriggerInteraction ProjectileTriggerInteraction => projectileTriggerInteraction;    public PlayerStatModifier[] StatModifiers => statModifiers;
    public float PaintMarkAmountOnHit => Mathf.Max(0f, paintMarkAmountOnHit);

    public LayerMask ResolveDamageTargetMask(ShooterFaction faction)
    {
        if (!CanAttack)
            return 0;

        return faction == ShooterFaction.Enemy
            ? enemyDamageableTargetMask
            : playerDamageableTargetMask;
    }
}
