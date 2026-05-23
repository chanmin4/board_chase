using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyFireArcBombAction",
    menuName = "State Machines/Enemy Actions/Fire Arc Bomb")]
public class EnemyFireArcBombActionSO : StateActionSO<EnemyFireArcBombAction>
{
    [Header("Definition Config")]
    [SerializeField] private EnemyArcBombAttackConfigSO _definitionConfig;

    public bool HasDefinitionConfig => _definitionConfig != null;

    public EnemyArcBombProjectile ProjectilePrefab => _definitionConfig.ProjectilePrefab;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _definitionConfig.MaskRenderManagerReadyChannel;

    public float TravelTime => _definitionConfig.TravelTime;
    public float ArcHeight => _definitionConfig.ArcHeight;
    public float SpawnYOffset => _definitionConfig.SpawnYOffset;
    public float TargetYOffset => _definitionConfig.TargetYOffset;
    public float FallbackDistance => _definitionConfig.FallbackDistance;

    public bool DisableProjectileCollidersDuringFlight => _definitionConfig.DisableProjectileCollidersDuringFlight;
    public bool ShowImpactTelegraph => _definitionConfig.ShowImpactTelegraph;
    public float ImpactTelegraphRadius => _definitionConfig.ImpactTelegraphRadius;
    public AreaAttackTelegraphStyle ImpactTelegraphStyle => _definitionConfig.ImpactTelegraphStyle;

    public bool FireOnEnter => _definitionConfig.FireOnEnter;
    public int ShotsPerCycle => _definitionConfig.ShotsPerCycle;
    public float ShotInterval => _definitionConfig.ShotInterval;
    public int MaxCycles => _definitionConfig.MaxCycles;
    public float CycleInterval => _definitionConfig.CycleInterval;

    public float DamageRadius => _definitionConfig.DamageRadius;
    public float ImpactHealthDamage => _definitionConfig.ImpactHealthDamage;
    public float ImpactInfectionDamage => _definitionConfig.ImpactInfectionDamage;
    public LayerMask DamageTargetMask => _definitionConfig.DamageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => _definitionConfig.TriggerInteraction;

    public MaskRenderManager.PaintChannel PaintChannel => _definitionConfig.PaintChannel;
    public float PaintRadiusWorld => _definitionConfig.PaintRadiusWorld;
    public int PaintPriority => _definitionConfig.PaintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => _definitionConfig.PoisonPuddleDamageConfig;
}

public class EnemyFireArcBombAction : StateAction
{
    private EnemyFireArcBombActionSO _config;
    private Enemy _enemy;
    private EnemyAttackRig _rig;

    private int _cycleCount;
    private int _shotsInCurrentCycle;
    private float _shotTimer;
    private float _cycleTimer;
    private bool _cycleActive;
    private bool _hasConfig;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (EnemyFireArcBombActionSO)OriginSO;
        _enemy = stateMachine.GetComponentInParent<Enemy>();
        _rig = stateMachine.GetComponentInParent<EnemyAttackRig>();
    }

    public override void OnStateEnter()
    {
        _hasConfig = _config.HasDefinitionConfig;
        _cycleCount = 0;
        _shotsInCurrentCycle = 0;
        _shotTimer = 0f;
        _cycleTimer = 0f;
        _cycleActive = false;

        if (!_hasConfig)
        {
            Debug.LogError("[EnemyFireArcBombAction] Definition Config is missing.", _enemy);
            return;
        }

        if (_config.FireOnEnter)
            StartCycle();
    }

    public override void OnUpdate()
    {
        if (!_hasConfig)
            return;

        if (_cycleActive)
        {
            TickCycle();
            return;
        }

        if (_config.MaxCycles > 0 && _cycleCount >= _config.MaxCycles)
            return;

        _cycleTimer += Time.deltaTime;

        if (_cycleTimer < _config.CycleInterval)
            return;

        StartCycle();
    }

    private void StartCycle()
    {
        _cycleActive = true;
        _shotsInCurrentCycle = 0;
        _shotTimer = 0f;
        _cycleTimer = 0f;

        FireOne();
    }

    private void TickCycle()
    {
        if (_shotsInCurrentCycle >= _config.ShotsPerCycle)
        {
            _cycleActive = false;
            _cycleCount++;
            _cycleTimer = 0f;
            return;
        }

        _shotTimer += Time.deltaTime;

        if (_shotTimer < _config.ShotInterval)
            return;

        _shotTimer = 0f;
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

        Vector3 start = fireOrigin.position + Vector3.up * _config.SpawnYOffset;
        Vector3 target = ResolveTargetPoint();

        EnemyArcBombProjectile projectile = projectileRoot != null
            ? Object.Instantiate(_config.ProjectilePrefab, start, Quaternion.identity, projectileRoot)
            : Object.Instantiate(_config.ProjectilePrefab, start, Quaternion.identity);

        projectile.Init(
            start,
            target,
            _config.TravelTime,
            _config.ArcHeight,
            _config.DamageRadius,
            _config.ImpactHealthDamage,
            _config.ImpactInfectionDamage,
            _config.DamageTargetMask,
            _config.TriggerInteraction,
            _config.MaskRenderManagerReadyChannel,
            _config.PaintChannel,
            _config.PaintRadiusWorld,
            _config.PaintPriority,
            _config.PoisonPuddleDamageConfig,
            _enemy.gameObject,
            _config.DisableProjectileCollidersDuringFlight,
            _config.ShowImpactTelegraph,
            _config.ImpactTelegraphRadius,
            _config.ImpactTelegraphStyle);

        _shotsInCurrentCycle++;
    }

    private Vector3 ResolveTargetPoint()
    {
        if (_enemy.currentTarget != null)
        {
            Vector3 target = _enemy.currentTarget.transform.position;
            target.y += _config.TargetYOffset;
            return target;
        }

        Vector3 fallback = _enemy.transform.position + _enemy.transform.forward * _config.FallbackDistance;
        fallback.y += _config.TargetYOffset;
        return fallback;
    }
}