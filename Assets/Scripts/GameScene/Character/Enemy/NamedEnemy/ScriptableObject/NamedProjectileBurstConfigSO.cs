using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedProjectileBurstConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Named Projectile Burst Config")]
public class NamedProjectileBurstConfigSO : ScriptableObject
{
    [Header("Projectile")]
    [SerializeField] private EnemyProjectile _projectilePrefab;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 8f;
    [SerializeField, Min(0f)] private float _healthDamage = 10f;
    [SerializeField, Min(0f)] private float _infectionDamage = 5f;
    [SerializeField, Min(0.001f)] private float _projectileCastRadius = 0.2f;
    [SerializeField, Min(0.01f)] private float _projectileLifetime = 5f;
    [SerializeField] private float _projectileSpawnYOffset = 0.2f;

    [Header("Burst")]
    [SerializeField, Min(1)] private int _burstCount = 1;
    [SerializeField, Min(0f)] private float _burstInterval = 0.12f;
    [SerializeField, Min(0f)] private float _randomSpreadAngle = 0f;
    [SerializeField] private bool _aimEachShotAtCurrentTarget = true;

    [Header("Hit")]
    [SerializeField] private LayerMask _damageTargetMask;
    [SerializeField] private LayerMask _impactMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Paint")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.Virus;
    [SerializeField, Min(0f)] private float _paintRadiusWorld = 0f;
    [SerializeField] private int _paintPriority = 0;

    public EnemyProjectile ProjectilePrefab => _projectilePrefab;
    public float ProjectileSpeed => DifficultyRuntime.ApplyEnemyProjectileSpeed(_projectileSpeed);
    public float HealthDamage => DifficultyRuntime.ApplyEnemyDamage(_healthDamage);
    public float InfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_infectionDamage);
    public float ProjectileCastRadius => Mathf.Max(0.001f, _projectileCastRadius);
    public float ProjectileLifetime => Mathf.Max(0.01f, _projectileLifetime);
    public float ProjectileSpawnYOffset => _projectileSpawnYOffset;
    public int BurstCount => Mathf.Max(1, _burstCount);
    public float BurstInterval => Mathf.Max(0f, _burstInterval);
    public float RandomSpreadAngle => Mathf.Max(0f, _randomSpreadAngle);
    public bool AimEachShotAtCurrentTarget => _aimEachShotAtCurrentTarget;
    public LayerMask DamageTargetMask => _damageTargetMask;
    public LayerMask ImpactMask => _impactMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public MaskRenderManager.PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_paintRadiusWorld);
    public int PaintPriority => _paintPriority;
}
