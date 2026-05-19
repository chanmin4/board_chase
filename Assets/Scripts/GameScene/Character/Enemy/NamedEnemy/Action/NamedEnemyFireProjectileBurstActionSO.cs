using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NamedEnemyFireProjectileBurstAction",
    menuName = "State Machines/Named Enemy Actions/Fire Projectile Burst")]
public class NamedEnemyFireProjectileBurstActionSO : StateActionSO<NamedEnemyFireProjectileBurstAction>
{
    [Header("Definition Config")]
    [SerializeField] private NamedProjectileBurstConfigSO _definitionConfig;

    public bool HasDefinitionConfig => _definitionConfig != null;

    public EnemyProjectile ProjectilePrefab => _definitionConfig.ProjectilePrefab;
    public float ProjectileSpeed => _definitionConfig.ProjectileSpeed;
    public float HealthDamage => _definitionConfig.HealthDamage;
    public float InfectionDamage => _definitionConfig.InfectionDamage;
    public float ProjectileCastRadius => _definitionConfig.ProjectileCastRadius;
    public float ProjectileLifetime => _definitionConfig.ProjectileLifetime;
    public float ProjectileSpawnYOffset => _definitionConfig.ProjectileSpawnYOffset;
    public int BurstCount => _definitionConfig.BurstCount;
    public float BurstInterval => _definitionConfig.BurstInterval;
    public float RandomSpreadAngle => _definitionConfig.RandomSpreadAngle;
    public bool AimEachShotAtCurrentTarget => _definitionConfig.AimEachShotAtCurrentTarget;
    public LayerMask DamageTargetMask => _definitionConfig.DamageTargetMask;
    public LayerMask ImpactMask => _definitionConfig.ImpactMask;
    public QueryTriggerInteraction TriggerInteraction => _definitionConfig.TriggerInteraction;
    public MaskRenderManager.PaintChannel PaintChannel => _definitionConfig.PaintChannel;
    public float PaintRadiusWorld => _definitionConfig.PaintRadiusWorld;
    public int PaintPriority => _definitionConfig.PaintPriority;
}

public class NamedEnemyFireProjectileBurstAction : StateAction
{
    private NamedEnemyFireProjectileBurstActionSO _config;
    private Enemy _enemy;
    private EnemyAttackRig _rig;

    private int _firedCount;
    private float _timer;
    private Vector3 _cachedDirection;
    private bool _hasConfig;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (NamedEnemyFireProjectileBurstActionSO)OriginSO;
        _enemy = stateMachine.GetComponentInParent<Enemy>();
        _rig = stateMachine.GetComponentInParent<EnemyAttackRig>();
    }

    public override void OnStateEnter()
    {
        _hasConfig = _config.HasDefinitionConfig;
        _firedCount = 0;
        _timer = 0f;
        _cachedDirection = ResolveDirection();

        if (!_hasConfig)
        {
            Debug.LogError("[NamedEnemyFireProjectileBurstAction] Definition Config is missing.", _enemy);
            return;
        }

        FireOne();
    }

    public override void OnUpdate()
    {
        if (!_hasConfig)
            return;

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
