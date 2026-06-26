using UnityEngine;

public class EnemyArcBombAttackExecutor : EnemyAttackExecutor
{
    private readonly EnemyArcBombAttackConfigSO _config;

    private int _cycleCount;
    private int _shotsInCurrentCycle;
    private int _shotAnimatorTriggerHash;
    private float _shotTimer;
    private float _cycleTimer;
    private bool _cycleActive;
    private bool _waitingPostAttackDelay;

    public EnemyArcBombAttackExecutor(
        EnemyArcBombAttackConfigSO config,
        EnemyAttackExecutorContext context)
        : base(context)
    {
        _config = config;
    }

    public override void Enter()
    {
        _cycleCount = 0;
        _shotsInCurrentCycle = 0;
        _shotAnimatorTriggerHash = ResolveShotAnimatorTriggerHash(_config);
        _shotTimer = 0f;
        _cycleTimer = 0f;
        _cycleActive = false;
        _waitingPostAttackDelay = false;
        IsFinished = false;

        if (_config == null || Context.Enemy == null)
        {
            Finish();
            return;
        }

        FaceCurrentTarget(_config.SnapFacingOnAttackStart);

        if (_config.FireOnEnter)
            StartCycle();
    }

    public override void Tick(float deltaTime)
    {
        if (IsFinished)
            return;

        if (_waitingPostAttackDelay)
        {
            TickPostAttackDelay(deltaTime);
            return;
        }

        if (_config == null)
            return;

        if (_config.FaceTargetWhileAttacking)
            FaceCurrentTarget(false);

        if (_cycleActive)
        {
            TickCycle(deltaTime);
            return;
        }

        if (_config.MaxCycles > 0 && _cycleCount >= _config.MaxCycles)
        {
            BeginPostAttackDelay();
            return;
        }

        _cycleTimer += deltaTime;

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

    private void TickCycle(float deltaTime)
    {
        if (_shotsInCurrentCycle >= _config.ShotsPerCycle)
        {
            _cycleActive = false;
            _cycleCount++;
            _cycleTimer = 0f;
            return;
        }

        _shotTimer += deltaTime;

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
        Enemy enemy = Context.Enemy;

        if (_config.ProjectilePrefab == null || enemy == null)
            return false;

        Transform fireOrigin = Context.Rig != null ? Context.Rig.FireOrigin : enemy.transform;

        Transform projectileRoot = Context.Rig != null && Context.Rig.ProjectileRoot != null
            ? Context.Rig.ProjectileRoot
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
            enemy.gameObject,
            _config.DisableProjectileCollidersDuringFlight,
            _config.ShowImpactTelegraph,
            _config.ImpactTelegraphRadius,
            _config.ImpactTelegraphStyle);

        _shotsInCurrentCycle++;
        return true;
    }

    private void TriggerShotAnimator()
    {
        if (Context.Animator == null || _shotAnimatorTriggerHash == 0)
            return;

        Context.Animator.SetTrigger(_shotAnimatorTriggerHash);
    }

    private static int ResolveShotAnimatorTriggerHash(EnemyArcBombAttackConfigSO config)
    {
        if (config == null || !config.TriggerAnimatorOnEachShot)
            return 0;

        return string.IsNullOrWhiteSpace(config.ShotAnimatorTrigger)
            ? 0
            : Animator.StringToHash(config.ShotAnimatorTrigger);
    }

    private void FaceCurrentTarget(bool snap)
    {
        Enemy enemy = Context.Enemy;

        if (enemy == null || enemy.currentTarget == null)
            return;

        FaceTargetPoint(enemy.currentTarget.transform.position, snap);
    }

    private void FaceTargetPoint(Vector3 targetPoint, bool snap)
    {
        if (_config == null || Context.Enemy == null)
            return;

        _config.TryFaceWorldPoint(
            Context.Enemy.transform,
            targetPoint,
            snap,
            Time.deltaTime);
    }

    private Vector3 ResolveTargetPoint()
    {
        Enemy enemy = Context.Enemy;

        if (enemy.currentTarget != null)
        {
            Vector3 target = enemy.currentTarget.transform.position;
            target.y += _config.TargetYOffset;
            return target;
        }

        Vector3 fallback = enemy.transform.position + enemy.transform.forward * _config.FallbackDistance;
        fallback.y += _config.TargetYOffset;
        return fallback;
    }

    private void BeginPostAttackDelay()
    {
        if (IsFinished)
            return;

        if (_config.PostAttackDelaySeconds <= 0f)
        {
            Finish();
            return;
        }

        _cycleTimer = 0f;
        _waitingPostAttackDelay = true;
    }

    private void TickPostAttackDelay(float deltaTime)
    {
        _cycleTimer += deltaTime;

        if (_cycleTimer >= _config.PostAttackDelaySeconds)
            Finish();
    }
}
