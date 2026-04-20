using UnityEngine;

public class WeaponSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId = "weapon_pistol";
    [SerializeField] private string displayName = "Pistol";
    [Header("UI")]
    [SerializeField] private Sprite icon;

    public Sprite Icon => icon;

    [Header("Shared")]
    [Min(0.01f)]
    [SerializeField] private float shotsPerSecond = 2f;

    [Min(0.1f)]
    [SerializeField] private float maxRange = 12f;

    [Min(1f)]
    [SerializeField] private int magazineSize = 6;

    [Min(1f)]
    [SerializeField] private float reloadDuration = 3;

    [SerializeField] private LayerMask aimHitMask = ~0;
    [SerializeField] private bool allowFallbackPlane = true;
    [SerializeField] private float fallbackPlaneY = 0f;

    [Header("Attack")]
    [Min(0f)]
    [SerializeField] private float damage = 10f;

    [SerializeField] private LayerMask damageHitMask = ~0;

    [Header("Paint")]
    [Min(0.05f)]
    [SerializeField] private float paintRadiusWorld = 1.25f;

    [SerializeField] private int paintPriority = 0;

    [SerializeField] private LayerMask paintHitMask = 0;

    [Header("Projectile")]
    [SerializeField] private AttackBullet attackBulletPrefab;
    [SerializeField] private PaintBullet paintBulletPrefab;

    [Min(0.1f)]
    [SerializeField] private float projectileSpeed = 18f;

    [Min(0.001f)]
    [SerializeField] private float projectileCastRadius = 0.08f;

    [Min(0.01f)]
    [SerializeField] private float projectileMaxLifetime = 2f;

    [SerializeField] private LayerMask projectileBlockHitMask = 0;
    [SerializeField] private QueryTriggerInteraction projectileTriggerInteraction = QueryTriggerInteraction.Ignore;
    [Header("Bullets")]
    [SerializeField] private AttackBulletSO attackBullet;
    [SerializeField] private PaintBulletSO paintBullet;
    public AttackBulletSO AttackBullet => attackBullet;
    public PaintBulletSO PaintBullet => paintBullet;
    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public float ShotsPerSecond => shotsPerSecond;
    public int MagazineSize => magazineSize;
    public float ReloadDuration => reloadDuration;
    public float MaxRange => maxRange;
    public LayerMask AimHitMask => aimHitMask;
    public bool AllowFallbackPlane => allowFallbackPlane;
    public float FallbackPlaneY => fallbackPlaneY;
    public float Damage => damage;
    public LayerMask DamageHitMask => damageHitMask;
    public float PaintRadiusWorld => paintRadiusWorld;
    public int PaintPriority => paintPriority;
    public LayerMask PaintHitMask => paintHitMask;
    public AttackBullet AttackBulletPrefab => attackBulletPrefab;
    public PaintBullet PaintBulletPrefab => paintBulletPrefab;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileCastRadius => projectileCastRadius;
    public float ProjectileMaxLifetime => projectileMaxLifetime;
    public LayerMask ProjectileBlockHitMask => projectileBlockHitMask;
    public QueryTriggerInteraction ProjectileTriggerInteraction => projectileTriggerInteraction;
}
