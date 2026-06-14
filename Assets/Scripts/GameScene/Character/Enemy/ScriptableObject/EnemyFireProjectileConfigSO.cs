using UnityEngine;

public abstract class EnemyFireProjectileConfigSO : EnemyAttackConfigSO
{
    [Header("Target")]
    [SerializeField] private bool _requireTarget = true;
    [SerializeField] private bool _stopAgentOnEnter = true;

    [Header("Aim")]
    [SerializeField] private bool _aimEachShotAtCurrentTarget = true;
    [SerializeField] private bool _usePredictiveAim = false;
    [SerializeField, Min(0f)] private float _aimLeadTime = 0.3f;
    [SerializeField, Min(0f)] private float _maxAimLeadDistance = 2.5f;
    [SerializeField, Min(0f)] private float _randomSpreadAngle = 0f;

    [Header("Projectile")]
    [SerializeField] private EnemyProjectile _projectilePrefab;
    [SerializeField, Min(0.01f)] private float _projectileSpeed = 14f;
    [SerializeField, Min(0.01f)] private float _projectileLifetime = 4f;
    [SerializeField, Min(0.001f)] private float _projectileCastRadius = 0.2f;
    [SerializeField] private float _projectileSpawnYOffset = 0f;

    [Header("Burst")]
    [SerializeField, Min(1)] private int _burstCount = 1;
    [SerializeField, Min(0f)] private float _burstInterval = 0.12f;

    [Header("Damage")]
    [SerializeField, Min(0f)] private float _healthDamage = 5f;
    [SerializeField, Min(0f)] private float _infectionDamage = 5f;

    [Header("Collision")]
    [SerializeField] private LayerMask _damageTargetMask;
    [SerializeField] private LayerMask _impactMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Impact Paint")]
    [SerializeField] private PaintChannel _paintChannel = PaintChannel.Virus;
    [SerializeField, Min(0f)] private float _paintRadiusWorld = 1.25f;
    [SerializeField] private int _paintPriority = 0;

    public bool RequireTarget => _requireTarget;
    public bool StopAgentOnEnter => _stopAgentOnEnter;

    public bool AimEachShotAtCurrentTarget => _aimEachShotAtCurrentTarget;
    public bool UsePredictiveAim => _usePredictiveAim;
    public float AimLeadTime => Mathf.Max(0f, _aimLeadTime);
    public float MaxAimLeadDistance => Mathf.Max(0f, _maxAimLeadDistance);
    public float RandomSpreadAngle => Mathf.Max(0f, _randomSpreadAngle);

    public EnemyProjectile ProjectilePrefab => _projectilePrefab;
    public float ProjectileSpeed => DifficultyRuntime.ApplyEnemyProjectileSpeed(_projectileSpeed);
    public float ProjectileLifetime => Mathf.Max(0.01f, _projectileLifetime);
    public float ProjectileCastRadius => Mathf.Max(0.001f, _projectileCastRadius);
    public float ProjectileSpawnYOffset => _projectileSpawnYOffset;

    public int BurstCount => Mathf.Max(1, _burstCount);
    public float BurstInterval => Mathf.Max(0f, _burstInterval);

    public float HealthDamage => DifficultyRuntime.ApplyEnemyDamage(_healthDamage);
    public float InfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_infectionDamage);

    public LayerMask DamageTargetMask => _damageTargetMask;
    public LayerMask ImpactMask => _impactMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;

    public PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_paintRadiusWorld);
    public int PaintPriority => _paintPriority;
}