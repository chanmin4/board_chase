/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Handles BulletAmmoType combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

public enum BulletAmmoType
{
    Basic = 0,
    Special = 1
}

public enum ShooterFaction
{
    Player = 0,
    // Keep the historical non-player value (1) valid for serialized data.
    Scraper = 1,
    Raider = 2,
    Agent = 3
}

public enum BulletTier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3,
    Tier4 = 4,
    Tier5 = 5,
    Tier6 = 6,
    Tier7 = 7,
    Tier8 = 8
}

public enum BulletFamily
{
    Universal = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3,
    Sniper = 4,
    ShotgunShell = 5,
    Rocket = 6
}

public enum RangedWeaponType
{
    Pistol = 0,
    Revolver = 1,
    SubmachineGun = 2,
    AssaultRifle = 3,
    LightMachineGun = 4,
    SniperRifle = 5,
    Shotgun = 6,
    RocketLauncher = 7
}

public enum RangedFirePattern
{
    SingleProjectile = 0,
    PelletSpread = 1
}

public abstract class BulletSO : ItemSO
{
    [Header("Identity")]
    [SerializeField] private string bulletId = "bullet_default";
    [SerializeField] private string displayName = "Bullet";

    [Header("Projectile Prefab")]
    [Tooltip("Projectile prefab fired by this bullet item. This is not the world pickup prefab.")]
    [FormerlySerializedAs("bulletPrefab")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Projectile Visuals")]
    [Tooltip("Optional visual trail prefab parented to the fired projectile. Use this for red attack trails, blue paint trails, etc.")]
    [SerializeField] private GameObject projectileTrailPrefab;

    [Header("Flight")]
    [Min(0.1f)] [SerializeField] private float speed = 18f;
    [Tooltip("Radius of the gameplay SphereCast used for hit detection. Typical bullets should stay near 0.08; large values can collide with the shooter or nearby cover even when the visual projectile looks clear.")]
    [Min(0.001f)] [SerializeField] private float castRadius = 0.08f;
    [Min(0.01f)] [SerializeField] private float maxLifetime = 2f;
    [Min(0f)] [SerializeField] private float spawnOffset = 0.12f;

    [Header("Projectile Sound Stimuli")]
    [Tooltip("AI hearing radius of the projectile while it travels. 0 disables flyby investigation for this ammo.")]
    [SerializeField, Min(0f)] private float _projectileFlybySoundRadius = 4f;

    [Tooltip("World distance travelled between flyby sound samples. This is distance-based so projectile speed and frame rate do not multiply events.")]
    [SerializeField, Min(0.1f)] private float _projectileFlybyEmitDistance = 3f;

    [Tooltip("Strength of the projectile flyby clue. Values above 0 enable the clue when its radius is also above 0.")]
    [SerializeField, Min(0f)] private float _projectileFlybySoundIntensity = 0.65f;

    [Tooltip("AI hearing radius emitted only when the projectile hits a collider. Range/lifetime expiry does not emit impact sound.")]
    [SerializeField, Min(0f)] private float _projectileImpactSoundRadius = 8f;

    [Tooltip("Strength of the projectile impact clue.")]
    [SerializeField, Min(0f)] private float _projectileImpactSoundIntensity = 1f;

    [Tooltip("Reaction delay before an enemy may investigate projectile flyby or impact sound.")]
    [SerializeField, Min(0f)] private float _projectileSoundInvestigateDelaySeconds = 0.05f;


    [Header("Projectile Collision")]
    [Tooltip("Layers this projectile raycast/sweep can physically collide with.")]
    [SerializeField] private LayerMask projectileCollisionMask = 0;

    [SerializeField] private QueryTriggerInteraction projectileTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Stat Modifiers")]
    [Tooltip("Bullet-specific values on top of Shooter stats, such as direct damage, shots per second, or magazine size.")]
    [SerializeField] private PlayerStatModifier[] statModifiers;

    public string BulletId => bulletId;
    public string DisplayName => displayName;
    public abstract BulletAmmoType AmmoType { get; }
    public virtual BulletFamily Family => BulletFamily.Universal;
    public bool IsBasicAmmo => IsBasicAmmoType(AmmoType);
    public bool CanAttack => IsBasicAmmo;
    public bool IsPrimary => IsBasicAmmo;
    public GameObject ProjectilePrefab => projectilePrefab;
    public GameObject ProjectileTrailPrefab => projectileTrailPrefab;
    public float Speed => speed;
    public float CastRadius => castRadius;
    public float MaxLifetime => maxLifetime;
    public float SpawnOffset => spawnOffset;
    public float ProjectileFlybySoundRadius => Mathf.Max(0f, _projectileFlybySoundRadius);
    public float ProjectileFlybyEmitDistance => Mathf.Max(0.1f, _projectileFlybyEmitDistance);
    public float ProjectileFlybySoundIntensity => Mathf.Max(0f, _projectileFlybySoundIntensity);
    public float ProjectileImpactSoundRadius => Mathf.Max(0f, _projectileImpactSoundRadius);
    public float ProjectileImpactSoundIntensity => Mathf.Max(0f, _projectileImpactSoundIntensity);
    public float ProjectileSoundInvestigateDelaySeconds => Mathf.Max(0f, _projectileSoundInvestigateDelaySeconds);
    public LayerMask ProjectileCollisionMask => projectileCollisionMask;
    public QueryTriggerInteraction ProjectileTriggerInteraction => projectileTriggerInteraction;
    public virtual float PenetrationClass => 0;
    public virtual BulletTier Tier => BulletTier.Tier1;
    public virtual float HealthDamage => 0f;
    public virtual float InfectionDamage => 0f;
    public virtual float InfectionExposureOnHit => 0f;
    public virtual float MarkAmount => 0f;
    public PlayerStatModifier[] StatModifiers => statModifiers;
    public override ItemKind Kind => ItemKind.Bullet;

    // Purpose: Checks whether basic ammo type is currently true.
    public static bool IsBasicAmmoType(BulletAmmoType ammoType)
    {
        return ammoType == BulletAmmoType.Basic;
    }

    // Purpose: Resolves the damage target mask needed by this component.
    public virtual LayerMask ResolveDamageTargetMask(ShooterFaction faction)
    {
        return 0;
    }
}
