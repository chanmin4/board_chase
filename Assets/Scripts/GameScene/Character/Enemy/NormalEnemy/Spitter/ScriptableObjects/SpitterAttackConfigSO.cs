using UnityEngine;

[CreateAssetMenu(
    fileName = "SpitterAttackConfig",
    menuName = "Game/Enemy/Spitter Attack Config")]
public class SpitterAttackConfigSO : ScriptableObject
{
    [Header("Range")]
    [SerializeField] private float _preferredDistance = 7f;
    [SerializeField] private float _tooCloseDistance = 4.5f;
    [SerializeField] private float _tooFarDistance = 9f;

    [Header("Movement")]
    [SerializeField] private float _windupMoveSpeed = 2.2f;
    [SerializeField] private float _recoverMoveSpeed = 3f;
    [SerializeField] private float _strafeDistance = 3f;
    [SerializeField] private float _destinationRefreshInterval = 0.35f;
    [SerializeField] private float _navMeshSampleDistance = 2f;

    [Header("Rotation")]
    [SerializeField] private float _turnSpeedDegPerSecond = 720f;

    [Header("Aim")]
    [SerializeField] private bool _usePredictiveAim = true;

    [Tooltip("플레이어 이동속도를 몇 초 앞까지 예측해서 쏠지")]
    [Min(0f)]
    [SerializeField] private float _aimLeadTime = 0.3f;

    [Tooltip("예측점이 너무 멀리 튀지 않도록 제한")]
    [Min(0f)]
    [SerializeField] private float _maxAimLeadDistance = 2.5f;

    [Tooltip("최종 발사 방향에 좌우 랜덤 오차 각도. 5면 -5도~+5도")]
    [Min(0f)]
    [SerializeField] private float _randomSpreadAngle = 3f;

    [Header("Projectile")]
    [SerializeField] private EnemyProjectile _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 14f;
    [SerializeField] private float _projectileLifetime = 4f;
    [Tooltip("Projectile hit detection thickness in world units. Larger values make the projectile easier to hit targets, especially small or fast-moving targets. This is not the visual size.")]
    [SerializeField] private float _projectileCastRadius = 2f;
    [Tooltip("FireOrigin 기준 투사체 생성 Y 오프셋. 탄이 너무 낮거나 높게 지나갈 때 조정합니다.")]
    [SerializeField] private float _projectileSpawnYOffset = 0f;
    [Header("Projectile Damage")]
    [SerializeField] private float _healthDamage = 5f;
    [SerializeField] private float _infectionDamage = 12f;

    [Header("Projectile Collision")]
    [Tooltip("Player/Damageable layer. 맞으면 체력/감염 데미지를 줌")]
    [SerializeField] private LayerMask _damageTargetMask;

    [Tooltip("Wall, sector structure, obstacle layer. 맞으면 데미지 없이 그 자리에서 페인트")]
    [SerializeField] private LayerMask _impactMask;

    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Impact Paint")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.Virus;
    [SerializeField] private float _paintRadiusWorld = 1.25f;
    [SerializeField] private int _paintPriority = 0;

    [Header("Debug")]
    [SerializeField] private bool _debugDrawRecoverDistances = true;
    [SerializeField] private bool _debugLogRecoverDistance = false;
    [SerializeField] private float _debugLogInterval = 0.5f;
    [SerializeField] private float _debugDrawHeight = 0.1f;

    public float PreferredDistance => _preferredDistance;
    public float TooCloseDistance => _tooCloseDistance;
    public float TooFarDistance => _tooFarDistance;

    public float WindupMoveSpeed => _windupMoveSpeed;
    public float RecoverMoveSpeed => _recoverMoveSpeed;
    public float StrafeDistance => _strafeDistance;
    public float DestinationRefreshInterval => _destinationRefreshInterval;
    public float NavMeshSampleDistance => _navMeshSampleDistance;

    public float TurnSpeedDegPerSecond => _turnSpeedDegPerSecond;

    public bool UsePredictiveAim => _usePredictiveAim;
    public float AimLeadTime => _aimLeadTime;
    public float MaxAimLeadDistance => _maxAimLeadDistance;
    public float RandomSpreadAngle => _randomSpreadAngle;

    public EnemyProjectile ProjectilePrefab => _projectilePrefab;
    public float ProjectileSpeed => _projectileSpeed;
    public float ProjectileLifetime => _projectileLifetime;
    public float ProjectileCastRadius => _projectileCastRadius;
    public float ProjectileSpawnYOffset => _projectileSpawnYOffset;
    public float HealthDamage => _healthDamage;
    public float InfectionDamage => _infectionDamage;

    public LayerMask DamageTargetMask => _damageTargetMask;
    public LayerMask ImpactMask => _impactMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;

    public MaskRenderManager.PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => _paintRadiusWorld;
    public int PaintPriority => _paintPriority;

    public bool DebugDrawRecoverDistances => _debugDrawRecoverDistances;
    public bool DebugLogRecoverDistance => _debugLogRecoverDistance;
    public float DebugLogInterval => _debugLogInterval;
    public float DebugDrawHeight => _debugDrawHeight;
}
