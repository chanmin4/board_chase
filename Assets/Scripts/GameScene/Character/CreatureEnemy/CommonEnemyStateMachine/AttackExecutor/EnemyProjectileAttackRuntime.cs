using UnityEngine;
using UnityEngine.AI;

public class EnemyProjectileAttackExecutor : EnemyAttackExecutor
{
    private readonly EnemyFireProjectileConfigSO _config;

    private int _firedCount;
    private int _shotAnimatorTriggerHash;
    private float _timer;
    private Vector3 _cachedDirection;
    private bool _waitingPostAttackDelay;

    public EnemyProjectileAttackExecutor(
        EnemyFireProjectileConfigSO config,
        EnemyAttackExecutorContext context)
        : base(context)
    {
        _config = config;
    }

    public override void Enter()
    {
        _firedCount = 0;
        _shotAnimatorTriggerHash = ResolveShotAnimatorTriggerHash(_config);
        _timer = 0f;
        _cachedDirection = Vector3.zero;
        _waitingPostAttackDelay = false;
        IsFinished = false;

        if (_config == null || Context.Enemy == null)
        {
            Finish();
            return;
        }

        if (_config.StopAgentOnEnter)
            StopAgent();

        FaceCurrentTarget(_config.SnapFacingOnAttackStart);

        if (!_config.AimEachShotAtCurrentTarget)
            _cachedDirection = ResolveDirection(ResolveSpawnPosition());

        if (!TryFireOne() || _firedCount >= _config.BurstCount)
            BeginPostAttackDelay();
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

        if (_config == null || _firedCount >= _config.BurstCount)
            return;

        if (_config.FaceTargetWhileAttacking)
            FaceCurrentTarget(false);

        _timer += deltaTime;

        if (_timer < _config.BurstInterval)
            return;

        _timer = 0f;

        if (!TryFireOne() || _firedCount >= _config.BurstCount)
            BeginPostAttackDelay();
    }

    public override void Exit()
    {
        if (_config != null && _config.StopAgentOnEnter)
            StopAgent();
    }

    private bool TryFireOne()
    {
        Enemy enemy = Context.Enemy;

        if (_config.ProjectilePrefab == null || enemy == null)
            return false;

        Vector3 spawnPosition = ResolveSpawnPosition();

        Vector3 direction = _config.AimEachShotAtCurrentTarget
            ? ResolveDirection(spawnPosition)
            : _cachedDirection;

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction = ApplyRandomSpread(direction);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        _config.TryFaceDirection(enemy.transform, direction, _config.SnapFacingOnAttackStart, Time.deltaTime);

        Transform projectileRoot = Context.Rig != null && Context.Rig.ProjectileRoot != null
            ? Context.Rig.ProjectileRoot
            : ProjectileRootRegistry.Root;

        TriggerShotAnimator();

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
            enemy.gameObject);

        _firedCount++;
        return true;
    }

    private void TriggerShotAnimator()
    {
        if (Context.Animator == null || _shotAnimatorTriggerHash == 0)
            return;

        Context.Animator.SetTrigger(_shotAnimatorTriggerHash);
    }

    private static int ResolveShotAnimatorTriggerHash(EnemyFireProjectileConfigSO config)
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

        if (_config == null || enemy == null || enemy.currentTarget == null)
            return;

        _config.TryFaceWorldPoint(
            enemy.transform,
            enemy.currentTarget.transform.position,
            snap,
            Time.deltaTime);
    }

    private Vector3 ResolveSpawnPosition()
    {
        Enemy enemy = Context.Enemy;
        Transform fireOrigin = Context.Rig != null ? Context.Rig.FireOrigin : enemy.transform;

        Vector3 spawnPosition = fireOrigin.position;
        spawnPosition.y += _config.ProjectileSpawnYOffset;

        return spawnPosition;
    }

    private Vector3 ResolveDirection(Vector3 spawnPosition)
    {
        Enemy enemy = Context.Enemy;

        if (enemy.currentTarget == null)
        {
            if (_config.RequireTarget)
                return Vector3.zero;

            Vector3 fallback = enemy.transform.forward;
            fallback.y = 0f;
            return fallback.normalized;
        }

        Transform target = enemy.currentTarget.transform;
        Vector3 aimPoint = target.position;

        if (_config.UsePredictiveAim)
            aimPoint += ResolveTargetVelocity(target) * _config.AimLeadTime;

        Vector3 leadOffset = aimPoint - target.position;
        leadOffset.y = 0f;

        if (_config.MaxAimLeadDistance > 0f && leadOffset.magnitude > _config.MaxAimLeadDistance)
            aimPoint = target.position + leadOffset.normalized * _config.MaxAimLeadDistance;

        Vector3 direction = aimPoint - spawnPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = enemy.transform.forward;
            direction.y = 0f;
        }

        return direction.normalized;
    }

    private static Vector3 ResolveTargetVelocity(Transform target)
    {
        if (target == null)
            return Vector3.zero;

        CharacterController characterController =
            target.GetComponent<CharacterController>() ??
            target.GetComponentInParent<CharacterController>();

        if (characterController != null)
            return characterController.velocity;

        Rigidbody rigidbody =
            target.GetComponent<Rigidbody>() ??
            target.GetComponentInParent<Rigidbody>();

        if (rigidbody != null)
            return rigidbody.linearVelocity;

        NavMeshAgent navMeshAgent =
            target.GetComponent<NavMeshAgent>() ??
            target.GetComponentInParent<NavMeshAgent>();

        if (navMeshAgent != null)
            return navMeshAgent.velocity;

        return Vector3.zero;
    }

    private Vector3 ApplyRandomSpread(Vector3 direction)
    {
        float angle = _config.RandomSpreadAngle;

        if (angle <= 0f)
            return direction;

        Quaternion spread = Quaternion.AngleAxis(Random.Range(-angle, angle), Vector3.up);
        return (spread * direction).normalized;
    }

    private void BeginPostAttackDelay()
    {
        if (IsFinished)
            return;

        if (_config == null || _config.PostAttackDelaySeconds <= 0f)
        {
            Finish();
            return;
        }

        _timer = 0f;
        _waitingPostAttackDelay = true;
    }

    private void TickPostAttackDelay(float deltaTime)
    {
        _timer += deltaTime;

        if (_timer >= _config.PostAttackDelaySeconds)
            Finish();
    }

    private void StopAgent()
    {
        NavMeshAgent agent = Context.Agent;

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        agent.ResetPath();
        agent.isStopped = true;
    }
}
