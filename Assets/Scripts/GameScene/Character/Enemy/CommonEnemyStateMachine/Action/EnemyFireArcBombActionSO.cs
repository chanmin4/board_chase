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

    public EnemyArcBombAttackConfigSO DefinitionConfig => _definitionConfig;
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
    public float PostAttackDelaySeconds => _definitionConfig.PostAttackDelaySeconds;
    public bool SnapFacingOnAttackStart => _definitionConfig.SnapFacingOnAttackStart;
    public bool FaceTargetWhileAttacking => _definitionConfig.FaceTargetWhileAttacking;
}

public class EnemyFireArcBombAction : StateAction
{
    private EnemyFireArcBombActionSO _config;
    private NamedEnemyBlackboard _blackboard;
    private Enemy _enemy;
    private EnemyAttackRig _rig;
    private Animator _animator;

    private int _cycleCount;
    private int _shotsInCurrentCycle;
    private int _shotAnimatorTriggerHash;
    private float _shotTimer;
    private float _cycleTimer;
    private bool _cycleActive;
    private bool _hasConfig;
    private bool _waitingPostAttackDelay;
    private bool _finishedNotified;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (EnemyFireArcBombActionSO)OriginSO;
        stateMachine.TryGetComponent(out _blackboard);
        _enemy = stateMachine.GetComponentInParent<Enemy>();
        _rig = stateMachine.GetComponentInParent<EnemyAttackRig>();
        _animator = ResolveAnimator(stateMachine, _enemy);
    }

    public override void OnStateEnter()
    {
        _hasConfig = _config.HasDefinitionConfig;
        _cycleCount = 0;
        _shotsInCurrentCycle = 0;
        _shotAnimatorTriggerHash = 0;
        _shotTimer = 0f;
        _cycleTimer = 0f;
        _cycleActive = false;
        _waitingPostAttackDelay = false;
        _finishedNotified = false;

        if (!_hasConfig)
        {
            Debug.LogError("[EnemyFireArcBombAction] Definition Config is missing.", _enemy);
            NotifyAttackFinished();
            return;
        }

        _shotAnimatorTriggerHash = ResolveShotAnimatorTriggerHash(_config.DefinitionConfig);

        FaceCurrentTarget(_config.SnapFacingOnAttackStart);

        if (_config.FireOnEnter)
            StartCycle();
    }

    public override void OnUpdate()
    {
        if (_waitingPostAttackDelay)
        {
            TickPostAttackDelay();
            return;
        }

        if (!_hasConfig)
            return;

        if (_config.FaceTargetWhileAttacking)
            FaceCurrentTarget(false);

        if (_cycleActive)
        {
            TickCycle();
            return;
        }

        if (_config.MaxCycles > 0 && _cycleCount >= _config.MaxCycles)
        {
            BeginPostAttackDelay();
            return;
        }

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

        if (!TryFireOne())
        {
            _cycleActive = false;

            if (_config.MaxCycles > 0)
                _cycleCount = _config.MaxCycles;

            BeginPostAttackDelay();
        }
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

        if (!TryFireOne())
        {
            _cycleActive = false;

            if (_config.MaxCycles > 0)
                _cycleCount = _config.MaxCycles;

            BeginPostAttackDelay();
        }
    }

    private bool TryFireOne()
    {
        if (_config.ProjectilePrefab == null || _enemy == null)
            return false;

        Transform fireOrigin = _rig != null ? _rig.FireOrigin : _enemy.transform;

        Transform projectileRoot = _rig != null && _rig.ProjectileRoot != null
            ? _rig.ProjectileRoot
            : ProjectileRootRegistry.Root;

        Vector3 start = fireOrigin.position + Vector3.up * _config.SpawnYOffset;
        Vector3 target = ResolveTargetPoint();

        FaceTargetPoint(target, _config.SnapFacingOnAttackStart);
        TriggerShotAnimator();

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
        return true;
    }

    private void TriggerShotAnimator()
    {
        if (_animator == null || _shotAnimatorTriggerHash == 0)
            return;

        _animator.SetTrigger(_shotAnimatorTriggerHash);
    }

    private static int ResolveShotAnimatorTriggerHash(EnemyArcBombAttackConfigSO config)
    {
        if (config == null || !config.TriggerAnimatorOnEachShot)
            return 0;

        return string.IsNullOrWhiteSpace(config.ShotAnimatorTrigger)
            ? 0
            : Animator.StringToHash(config.ShotAnimatorTrigger);
    }

    private static Animator ResolveAnimator(StateMachine stateMachine, Enemy enemy)
    {
        if (stateMachine == null)
            return null;

        if (stateMachine.TryGetComponent(out Animator animator))
            return animator;

        animator = stateMachine.GetComponentInChildren<Animator>(true);

        if (animator != null)
            return animator;

        return enemy != null
            ? enemy.GetComponentInChildren<Animator>(true)
            : stateMachine.GetComponentInParent<Animator>();
    }

    private void FaceCurrentTarget(bool snap)
    {
        if (_enemy == null || _enemy.currentTarget == null)
            return;

        FaceTargetPoint(_enemy.currentTarget.transform.position, snap);
    }

    private void FaceTargetPoint(Vector3 targetPoint, bool snap)
    {
        EnemyArcBombAttackConfigSO definitionConfig = _config.DefinitionConfig;

        if (definitionConfig == null || _enemy == null)
            return;

        definitionConfig.TryFaceWorldPoint(
            _enemy.transform,
            targetPoint,
            snap,
            Time.deltaTime);
    }

    private void BeginPostAttackDelay()
    {
        if (_finishedNotified)
            return;

        if (_config.PostAttackDelaySeconds <= 0f)
        {
            NotifyAttackFinished();
            return;
        }

        _cycleTimer = 0f;
        _waitingPostAttackDelay = true;
    }

    private void TickPostAttackDelay()
    {
        _cycleTimer += Time.deltaTime;

        if (_cycleTimer >= _config.PostAttackDelaySeconds)
            NotifyAttackFinished();
    }

    private void NotifyAttackFinished()
    {
        if (_finishedNotified)
            return;

        _finishedNotified = true;
        _waitingPostAttackDelay = false;
        _blackboard?.FinishSelectedAttack();
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
