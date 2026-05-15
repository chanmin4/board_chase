using UnityEngine;
using UnityEngine.Serialization;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyFireArcBombAction",
    menuName = "State Machines/Enemy Actions/Fire Arc Bomb")]
public class EnemyFireArcBombActionSO : StateActionSO<EnemyFireArcBombAction>
{
    [Header("Projectile")]
    [SerializeField] private EnemyArcBombProjectile _projectilePrefab;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Arc")]
    [SerializeField, Min(0.01f)] private float _travelTime = 1.2f;
    [SerializeField, Min(0f)] private float _arcHeight = 4f;
    [SerializeField] private float _spawnYOffset = 0.5f;
    [SerializeField] private float _targetYOffset = 0f;
    [SerializeField, Min(0f)] private float _fallbackDistance = 6f;

    [Header("Cycle")]
    [SerializeField] private bool _fireOnEnter = true;
    [SerializeField, Min(1)] private int _shotsPerCycle = 1;
    [SerializeField, Min(0f)] private float _shotInterval = 0.15f;

    [Tooltip("0 means infinite while the state is active.")]
    [SerializeField, Min(0)] private int _maxCycles = 1;

    [SerializeField, Min(0f)] private float _cycleInterval = 5f;

    [Header("Impact Damage")]
    [SerializeField, Min(0f)] private float _damageRadius = 1.5f;

    [FormerlySerializedAs("_healthDamage")]
    [SerializeField, Min(0f)] private float _impactHealthDamage = 10f;

    [FormerlySerializedAs("_infectionDamage")]
    [SerializeField, Min(0f)] private float _impactInfectionDamage = 5f;

    [Header("Damage Target")]
    [SerializeField] private LayerMask _damageTargetMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Paint")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.Virus;
    [SerializeField, Min(0f)] private float _paintRadiusWorld = 1.5f;
    [SerializeField] private int _paintPriority = 0;

    [Header("Poison Puddle Damage Config")]
    [SerializeField] private PoisonPuddleDamageConfigSO _poisonPuddleDamageConfig;

    public EnemyArcBombProjectile ProjectilePrefab => _projectilePrefab;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;

    public float TravelTime => _travelTime;
    public float ArcHeight => _arcHeight;
    public float SpawnYOffset => _spawnYOffset;
    public float TargetYOffset => _targetYOffset;
    public float FallbackDistance => _fallbackDistance;

    public bool FireOnEnter => _fireOnEnter;
    public int ShotsPerCycle => Mathf.Max(1, _shotsPerCycle);
    public float ShotInterval => Mathf.Max(0f, _shotInterval);
    public int MaxCycles => Mathf.Max(0, _maxCycles);
    public float CycleInterval => Mathf.Max(0f, _cycleInterval);

    public float DamageRadius => Mathf.Max(0f, _damageRadius);
    public float ImpactHealthDamage => Mathf.Max(0f, _impactHealthDamage);
    public float ImpactInfectionDamage => Mathf.Max(0f, _impactInfectionDamage);
    public LayerMask DamageTargetMask => _damageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;

    public MaskRenderManager.PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => Mathf.Max(0f, _paintRadiusWorld);
    public int PaintPriority => _paintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => _poisonPuddleDamageConfig;
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

    public override void Awake(StateMachine stateMachine)
    {
        _config = (EnemyFireArcBombActionSO)OriginSO;
        _enemy = stateMachine.GetComponentInParent<Enemy>();
        _rig = stateMachine.GetComponentInParent<EnemyAttackRig>();
    }

    public override void OnStateEnter()
    {
        _cycleCount = 0;
        _shotsInCurrentCycle = 0;
        _shotTimer = 0f;
        _cycleTimer = 0f;
        _cycleActive = false;

        if (_config.FireOnEnter)
            StartCycle();
    }

    public override void OnUpdate()
    {
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
            _enemy.gameObject);

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
