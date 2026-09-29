/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > Action
 * Primary responsibility: Defines EnemyFireProjectileActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyFireProjectileAction",
    menuName = "State Machines/Enemy Actions/Fire Projectile")]
public class EnemyFireProjectileActionSO : StateActionSO<EnemyFireProjectileAction>
{
    [Header("Definition Config")]
    [FormerlySerializedAs("_config")]
    [SerializeField] private EnemyFireProjectileConfigSO _definitionConfig;

    public EnemyFireProjectileConfigSO DefinitionConfig => _definitionConfig;
}

public class EnemyFireProjectileAction : StateAction
{
    private EnemyFireProjectileActionSO _origin;
    private EnemyFireProjectileConfigSO _config;
    private NamedEnemyBlackboard _blackboard;
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private EnemyAttackRig _rig;
    private Animator _animator;

    private int _firedCount;
    private int _shotAnimatorTriggerHash;
    private float _timer;
    private Vector3 _cachedDirection;
    private bool _waitingPostAttackDelay;
    private bool _finishedNotified;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyFireProjectileActionSO)OriginSO;
        stateMachine.TryGetComponent(out _blackboard);
        stateMachine.TryGetComponent(out _enemy);
        stateMachine.TryGetComponent(out _agent);
        stateMachine.TryGetComponent(out _rig);
        _animator = ResolveAnimator(stateMachine, _enemy);
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        _config = _origin.DefinitionConfig;
        _firedCount = 0;
        _shotAnimatorTriggerHash = 0;
        _timer = 0f;
        _cachedDirection = Vector3.zero;
        _waitingPostAttackDelay = false;
        _finishedNotified = false;

        if (_config == null)
        {
            Debug.LogError("[EnemyFireProjectileAction] Definition Config is missing.", _enemy);
            NotifyAttackFinished();
            return;
        }

        if (_enemy == null)
        {
            Debug.LogError("[EnemyFireProjectileAction] Enemy is missing.");
            NotifyAttackFinished();
            return;
        }

        _shotAnimatorTriggerHash = ResolveShotAnimatorTriggerHash(_config);

        if (_config.StopAgentOnEnter)
            StopAgent();

        FaceCurrentTarget(_config.SnapFacingOnAttackStart);

        if (!_config.AimEachShotAtCurrentTarget)
            _cachedDirection = ResolveDirection(ResolveSpawnPosition());

        if (!TryFireOne() || _firedCount >= _config.BurstCount)
            BeginPostAttackDelay();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_waitingPostAttackDelay)
        {
            TickPostAttackDelay();
            return;
        }

        if (_config == null || _firedCount >= _config.BurstCount)
            return;

        if (_config.FaceTargetWhileAttacking)
            FaceCurrentTarget(false);

        _timer += Time.deltaTime;

        if (_timer < _config.BurstInterval)
            return;

        _timer = 0f;

        if (!TryFireOne() || _firedCount >= _config.BurstCount)
            BeginPostAttackDelay();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (_config != null && _config.StopAgentOnEnter)
            StopAgent();
    }

    // Purpose: Attempts to fire one and reports whether the request succeeded.
    private bool TryFireOne()
    {
        if (_config.ProjectilePrefab == null || _enemy == null)
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
        _config.TryFaceDirection(_enemy.transform, direction, _config.SnapFacingOnAttackStart, Time.deltaTime);

        Transform projectileRoot = _rig != null && _rig.ProjectileRoot != null
            ? _rig.ProjectileRoot
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
            _enemy.gameObject);

        _firedCount++;
        return true;
    }

    // Purpose: Triggers the shot animator response.
    private void TriggerShotAnimator()
    {
        if (_animator == null || _shotAnimatorTriggerHash == 0)
            return;

        _animator.SetTrigger(_shotAnimatorTriggerHash);
    }

    // Purpose: Resolves the shot animator trigger hash needed by this component.
    private static int ResolveShotAnimatorTriggerHash(EnemyFireProjectileConfigSO config)
    {
        if (config == null || !config.TriggerAnimatorOnEachShot)
            return 0;

        return string.IsNullOrWhiteSpace(config.ShotAnimatorTrigger)
            ? 0
            : Animator.StringToHash(config.ShotAnimatorTrigger);
    }

    // Purpose: Resolves the animator needed by this component.
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

    // Purpose: Aligns this component toward current target.
    private void FaceCurrentTarget(bool snap)
    {
        if (_config == null || _enemy == null || _enemy.currentTarget == null)
            return;

        _config.TryFaceWorldPoint(
            _enemy.transform,
            _enemy.currentTarget.transform.position,
            snap,
            Time.deltaTime);
    }

    // Purpose: Resolves the spawn position needed by this component.
    private Vector3 ResolveSpawnPosition()
    {
        Transform fireOrigin = _rig != null ? _rig.FireOrigin : _enemy.transform;

        Vector3 spawnPosition = fireOrigin.position;
        spawnPosition.y += _config.ProjectileSpawnYOffset;

        return spawnPosition;
    }

    // Purpose: Resolves the direction needed by this component.
    private Vector3 ResolveDirection(Vector3 spawnPosition)
    {
        if (_enemy.currentTarget == null)
        {
            if (_config.RequireTarget)
                return Vector3.zero;

            Vector3 fallback = _enemy.transform.forward;
            fallback.y = 0f;
            return fallback.normalized;
        }

        Transform target = _enemy.currentTarget.transform;
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
            direction = _enemy.transform.forward;
            direction.y = 0f;
        }

        return direction.normalized;
    }

    // Purpose: Resolves the target velocity needed by this component.
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

    // Purpose: Applies the requested random spread change.
    private Vector3 ApplyRandomSpread(Vector3 direction)
    {
        float angle = _config.RandomSpreadAngle;

        if (angle <= 0f)
            return direction;

        Quaternion spread = Quaternion.AngleAxis(Random.Range(-angle, angle), Vector3.up);
        return (spread * direction).normalized;
    }

    // Purpose: Begins the post attack delay workflow.
    private void BeginPostAttackDelay()
    {
        if (_finishedNotified)
            return;

        if (_config == null || _config.PostAttackDelaySeconds <= 0f)
        {
            NotifyAttackFinished();
            return;
        }

        _timer = 0f;
        _waitingPostAttackDelay = true;
    }

    // Purpose: Processes one update step for post attack delay.
    private void TickPostAttackDelay()
    {
        _timer += Time.deltaTime;

        if (_timer >= _config.PostAttackDelaySeconds)
            NotifyAttackFinished();
    }

    // Purpose: Publishes the attack finished notification.
    private void NotifyAttackFinished()
    {
        if (_finishedNotified)
            return;

        _finishedNotified = true;
        _waitingPostAttackDelay = false;

        if (_blackboard != null)
            _blackboard.FinishSelectedAttack();
    }

    // Purpose: Stops the active agent behavior.
    private void StopAgent()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }
}
