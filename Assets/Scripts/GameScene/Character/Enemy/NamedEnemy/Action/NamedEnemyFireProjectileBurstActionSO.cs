using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NamedEnemyFireProjectileBurstAction",
    menuName = "State Machines/Named Enemy Actions/Fire Projectile Burst")]
public class NamedEnemyFireProjectileBurstActionSO : StateActionSO<NamedEnemyFireProjectileBurstAction>
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
    public float ProjectileSpeed => _projectileSpeed;
    public float HealthDamage => _healthDamage;
    public float InfectionDamage => _infectionDamage;
    public float ProjectileCastRadius => _projectileCastRadius;
    public float ProjectileLifetime => _projectileLifetime;
    public float ProjectileSpawnYOffset => _projectileSpawnYOffset;
    public int BurstCount => _burstCount;
    public float BurstInterval => _burstInterval;
    public float RandomSpreadAngle => _randomSpreadAngle;
    public bool AimEachShotAtCurrentTarget => _aimEachShotAtCurrentTarget;
    public LayerMask DamageTargetMask => _damageTargetMask;
    public LayerMask ImpactMask => _impactMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public MaskRenderManager.PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => _paintRadiusWorld;
    public int PaintPriority => _paintPriority;
}

public class NamedEnemyFireProjectileBurstAction : StateAction
{
    private NamedEnemyFireProjectileBurstActionSO _config;
    private Enemy _enemy;
    private EnemyAttackRig _rig;

    private int _firedCount;
    private float _timer;
    private Vector3 _cachedDirection;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (NamedEnemyFireProjectileBurstActionSO)OriginSO;
        _enemy = stateMachine.GetComponentInParent<Enemy>();
        _rig = stateMachine.GetComponentInParent<EnemyAttackRig>();
    }

    public override void OnStateEnter()
    {
        _firedCount = 0;
        _timer = 0f;
        _cachedDirection = ResolveDirection();

        FireOne();
    }

    public override void OnUpdate()
    {
        if (_firedCount >= _config.BurstCount)
            return;

        _timer += Time.deltaTime;

        if (_timer < _config.BurstInterval)
            return;

        _timer = 0f;
        FireOne();
    }

    private void FireOne()
    {
        if (_config.ProjectilePrefab == null || _enemy == null)
            return;

        Transform fireOrigin = _rig != null ? _rig.FireOrigin : _enemy.transform;

        Transform projectileRoot = _rig != null && _rig.ProjectileRoot != null
            ? _rig.ProjectileRoot
            : ProjectileRootRegistry.Root;

        Vector3 spawnPosition = fireOrigin.position;
        spawnPosition.y += _config.ProjectileSpawnYOffset;

        Vector3 direction = _config.AimEachShotAtCurrentTarget
            ? ResolveDirection()
            : _cachedDirection;

        direction = ApplyRandomSpread(direction);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = _enemy.transform.forward;

        direction.y = 0f;
        direction.Normalize();

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        _enemy.transform.rotation = rotation;

        EnemyProjectile projectile = projectileRoot != null
            ? Object.Instantiate(_config.ProjectilePrefab, spawnPosition, rotation, projectileRoot)
            : Object.Instantiate(_config.ProjectilePrefab, spawnPosition, rotation);

        projectile.Init(
            direction,
            _config.ProjectileSpeed,
            _config.HealthDamage,
            _config.InfectionDamage,
            _config.ProjectileCastRadius,
            _config.ProjectileLifetime,
            _config.DamageTargetMask,
            _config.ImpactMask,
            _config.TriggerInteraction,
            null,
            _config.PaintChannel,
            _config.PaintRadiusWorld,
            _config.PaintPriority,
            _enemy.gameObject);

        _firedCount++;
    }

    private Vector3 ResolveDirection()
    {
        if (_enemy != null && _enemy.currentTarget != null)
        {
            Vector3 direction = _enemy.currentTarget.transform.position - _enemy.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        return _enemy != null ? _enemy.transform.forward : Vector3.forward;
    }

    private Vector3 ApplyRandomSpread(Vector3 direction)
    {
        float angle = _config.RandomSpreadAngle;

        if (angle <= 0f)
            return direction;

        Quaternion spread = Quaternion.AngleAxis(Random.Range(-angle, angle), Vector3.up);
        return (spread * direction).normalized;
    }
}
