using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyShooterCombatController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyShooterShoot _shoot;
    [SerializeField] private EnemyShooterStatsRuntime _statsRuntime;
    [SerializeField] private EntityVisionController _vision;
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Damageable _damageable;

    [Header("Optional Events")]
    [Tooltip("Optional global hit event. If assigned, this shooter alerts when this object receives a hit event.")]
    [SerializeField] private HitReceivedEventChannelSO _hitReceivedEvent;
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private Transform _target;
    [ReadOnly] [SerializeField] private bool _isAlerted;
    [ReadOnly] [SerializeField] private bool _canSeeTarget;
    [ReadOnly] [SerializeField] private bool _hasLastKnownPosition;
    [ReadOnly] [SerializeField] private Vector3 _lastKnownPosition;
    [ReadOnly] [SerializeField] private float _lastSeenTime = -1f;
    [ReadOnly] [SerializeField] private bool _attackActive;
    [ReadOnly] [SerializeField] private bool _hasSoundInvestigation;
    [ReadOnly] [SerializeField] private Vector3 _soundInvestigationPosition;
    [ReadOnly] [SerializeField] private float _soundInvestigationMoveTime;

    private Damageable _targetDamageable;
    private Rigidbody _targetRigidbody;
    private Vector3 _homePosition;
    private Vector3 _patrolDestination;
    private bool _hasPatrolDestination;
    private float _nextPatrolRefreshTime;
    private float _nextTargetSearchTime;
    private float _seenSinceTime = -1f;
    private float _nextFireTime;
    private int _burstShotsRemaining;
    private float _nextBurstShotTime;
    private float _nextCombatMoveRefreshTime;
    private Vector3 _aimDirection;
    private float _lastHealth;
    private MaskRenderManager _maskRenderManager;
    private float _nextPaintAvoidanceCheckTime;

    public EnemyShooterConfigSO Config => _shoot != null ? _shoot.Config : null;
    public Transform Target => _target;
    public bool HasTarget => _target != null && (_targetDamageable == null || !_targetDamageable.IsDead);
    public bool IsAlerted => _isAlerted && (HasTarget || _hasSoundInvestigation);
    public bool CanSeeTarget => HasTarget && _canSeeTarget;
    public bool HasLastKnownPosition => _hasLastKnownPosition;
    public Vector3 LastKnownPosition => _lastKnownPosition;

    public bool ShouldAttack
    {
        get
        {
            if (!HasTarget || !CanSeeTarget)
                return false;

            EnemyShooterConfigSO config = Config;
            if (config == null)
                return false;

            float range = _attackActive ? config.AttackKeepRange : config.AttackStartRange;
            return GetPlanarDistanceToTarget() <= range;
        }
    }

    public bool ShouldChase
    {
        get
        {
            if (!IsAlerted)
                return false;

            if (ShouldAttack)
                return false;

            if (_hasSoundInvestigation && Time.time >= _soundInvestigationMoveTime)
                return true;

            if (CanSeeTarget)
                return true;

            EnemyShooterConfigSO config = Config;
            return config != null &&
                   config.ChaseLastKnownPosition &&
                   _hasLastKnownPosition &&
                   !HasForgottenTarget();
        }
    }

    public bool TargetLost => _isAlerted && !_hasSoundInvestigation && (!HasTarget || HasForgottenTarget());

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        _homePosition = transform.position;
        _lastHealth = _damageable != null ? _damageable.CurrentHealth : 0f;
    }

    private void OnEnable()
    {
        if (_damageable != null)
            _damageable.OnHealthChanged += OnHealthChanged;

        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised += OnHitReceived;

        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised += OnSoundStimulus;

        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += OnMaskRenderManagerReady;

            if (_maskRenderManagerReadyChannel.Current != null)
                OnMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnHealthChanged -= OnHealthChanged;

        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised -= OnHitReceived;

        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised -= OnSoundStimulus;

        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= OnMaskRenderManagerReady;
    }

    public void TickAwareness()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null)
            return;

        if (!HasTarget)
        {
            ClearTarget();
            TryAcquireVisiblePlayer(false);
            return;
        }

        _canSeeTarget = _vision == null || _vision.CanSeeTarget(_target);

        if (_canSeeTarget)
        {
            _isAlerted = true;
            _lastSeenTime = Time.time;
            _lastKnownPosition = _target.position;
            _hasLastKnownPosition = true;

            if (_seenSinceTime < 0f)
                _seenSinceTime = Time.time;

            return;
        }

        _seenSinceTime = -1f;

        if (HasForgottenTarget())
            ClearTarget();
    }

    public void ForceAcquirePlayer()
    {
        TryAcquireVisiblePlayer(true);
    }

    public void AlertToPlayer()
    {
        PlayerStatsRuntime player = FindAnyObjectByType<PlayerStatsRuntime>();

        if (player == null)
            return;

        SetTarget(player.transform);
        _isAlerted = true;
        _lastKnownPosition = player.transform.position;
        _hasLastKnownPosition = true;
        _lastSeenTime = Time.time;
        _hasSoundInvestigation = false;
    }

    public bool TryAlertToPlayerFromDamage(GameObject attacker)
    {
        EnemyShooterConfigSO config = Config;

        if (config != null && !config.AlertOnDamaged)
            return false;

        if (!TryResolvePlayerTransform(attacker, out Transform playerTransform))
            return false;

        if (!IsWithinAwarenessRange(playerTransform.position))
            return false;

        SetTarget(playerTransform);
        _isAlerted = true;
        _canSeeTarget = _vision == null || _vision.CanSeeTarget(playerTransform);
        _lastKnownPosition = playerTransform.position;
        _hasLastKnownPosition = true;
        _lastSeenTime = Time.time;
        _seenSinceTime = _canSeeTarget ? Time.time : -1f;
        _hasSoundInvestigation = false;

        FaceDirection(playerTransform.position - transform.position);
        return true;
    }

    public void ClearCombat()
    {
        ClearTarget();
        ClearSoundInvestigation();
        _aimDirection = Vector3.zero;
        _burstShotsRemaining = 0;
        _nextFireTime = 0f;
        _nextBurstShotTime = 0f;
    }

    public void BeginPatrol()
    {
        _attackActive = false;
        _hasPatrolDestination = false;
        _nextPatrolRefreshTime = 0f;

        if (CanUseAgent())
        {
            _agent.speed = ResolveMoveSpeed();
            _agent.stoppingDistance = 0f;
            _agent.isStopped = false;
        }
    }

    public void TickPatrol()
    {
        if (!CanUseAgent())
            return;

        EnemyShooterConfigSO config = Config;
        if (config == null)
            return;

        _agent.speed = ResolveMoveSpeed();
        _agent.stoppingDistance = 0f;

        if (TryGetPaintAvoidanceDestination(out Vector3 avoidanceDestination))
        {
            _patrolDestination = avoidanceDestination;
            _hasPatrolDestination = true;
            _nextPatrolRefreshTime = Time.time + config.PatrolDestinationRefreshInterval;
            _agent.isStopped = false;
            _agent.SetDestination(_patrolDestination);
            return;
        }

        bool needsDestination =
            !_hasPatrolDestination ||
            Time.time >= _nextPatrolRefreshTime ||
            HasReached(_patrolDestination, config.PatrolArriveDistance);

        if (!needsDestination)
            return;

        if (TryPickPatrolDestination(out _patrolDestination))
        {
            _hasPatrolDestination = true;
            _nextPatrolRefreshTime = Time.time + config.PatrolDestinationRefreshInterval;
            _agent.isStopped = false;
            _agent.SetDestination(_patrolDestination);
        }
        else
        {
            _agent.isStopped = true;
        }
    }

    public void BeginChase()
    {
        _attackActive = false;

        if (CanUseAgent())
        {
            _agent.speed = ResolveMoveSpeed();
            _agent.stoppingDistance = ResolveChaseStoppingDistance();
            _agent.isStopped = false;
        }
    }

    public void TickChase()
    {
        if (!CanUseAgent())
            return;

        Vector3 destination;

        if (TryGetPaintAvoidanceDestination(out Vector3 avoidanceDestination))
        {
            destination = avoidanceDestination;
            _agent.speed = ResolveMoveSpeed();
            _agent.stoppingDistance = 0f;
            _agent.isStopped = false;
            _agent.SetDestination(destination);
            return;
        }

        if (CanSeeTarget && _target != null)
        {
            destination = _target.position;
        }
        else if (_hasSoundInvestigation && Time.time >= _soundInvestigationMoveTime)
        {
            destination = _soundInvestigationPosition;

            EnemyShooterConfigSO config = Config;
            float arriveDistance = config != null ? config.PatrolArriveDistance : 0.5f;

            if (HasReached(destination, arriveDistance))
            {
                ClearSoundInvestigation();
                _agent.isStopped = true;
                return;
            }
        }
        else if (_hasLastKnownPosition)
        {
            destination = _lastKnownPosition;
        }
        else
        {
            _agent.isStopped = true;
            return;
        }

        _agent.speed = ResolveMoveSpeed();
        _agent.stoppingDistance = ResolveChaseStoppingDistance();
        _agent.isStopped = false;
        _agent.SetDestination(destination);
    }

    public void StopMovement()
    {
        if (!CanUseAgent())
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }

    public void BeginAttack()
    {
        _attackActive = true;
        _burstShotsRemaining = 0;

        if (CanUseAgent())
        {
            EnemyShooterConfigSO config = Config;
            _agent.speed = ResolveMoveSpeed();
            _agent.stoppingDistance = ResolveChaseStoppingDistance();
            _agent.isStopped = config == null || !config.MoveWhileAttacking;

            if (_agent.isStopped)
                _agent.ResetPath();
        }
    }

    public void TickAttack()
    {
        if (!ShouldAttack)
            return;

        TickCombatMovement();
        UpdateAimDirection();

        EnemyShooterConfigSO config = Config;
        if (config == null || Time.time < _seenSinceTime + config.ReactionTime)
            return;

        UpdateBurstFire();
    }

    public void EndAttack()
    {
        _attackActive = false;
        _burstShotsRemaining = 0;
        _nextCombatMoveRefreshTime = 0f;
    }

    private void TryAcquireVisiblePlayer(bool force)
    {
        EnemyShooterConfigSO config = Config;

        if (config == null)
            return;

        if (!force && Time.time < _nextTargetSearchTime)
            return;

        _nextTargetSearchTime = Time.time + config.TargetSearchInterval;

        PlayerStatsRuntime player = FindAnyObjectByType<PlayerStatsRuntime>();

        if (player == null)
            return;

        Transform playerTransform = player.transform;
        bool canSeePlayer = _vision == null || _vision.CanSeeTarget(playerTransform);

        if (!canSeePlayer)
            return;

        SetTarget(playerTransform);
        _isAlerted = true;
        _canSeeTarget = true;
        _lastSeenTime = Time.time;
        _seenSinceTime = Time.time;
        _lastKnownPosition = playerTransform.position;
        _hasLastKnownPosition = true;
    }

    private void SetTarget(Transform target)
    {
        if (_target == target)
            return;

        ClearTarget();

        _target = target;

        if (_target != null)
        {
            _targetDamageable = _target.GetComponent<Damageable>() ?? _target.GetComponentInParent<Damageable>();
            _targetRigidbody = _target.GetComponent<Rigidbody>() ?? _target.GetComponentInParent<Rigidbody>();
        }
    }

    private void ClearTarget()
    {
        _target = null;
        _targetDamageable = null;
        _targetRigidbody = null;
        _isAlerted = _hasSoundInvestigation;
        _canSeeTarget = false;
        _hasLastKnownPosition = _hasSoundInvestigation;
        _seenSinceTime = -1f;
        _lastSeenTime = -1f;
        _burstShotsRemaining = 0;
    }

    private void ClearSoundInvestigation()
    {
        _hasSoundInvestigation = false;
        _soundInvestigationPosition = Vector3.zero;
        _soundInvestigationMoveTime = 0f;

        if (!HasTarget)
        {
            _isAlerted = false;
            _hasLastKnownPosition = false;
        }
    }

    private void UpdateAimDirection()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || _target == null)
            return;

        Vector3 targetPoint = ResolveTargetPoint();
        Vector3 desired = targetPoint - transform.position;
        desired.y = 0f;

        if (desired.sqrMagnitude < 0.0001f)
            return;

        desired.Normalize();

        if (_aimDirection.sqrMagnitude < 0.0001f ||
            config.AimTrackingSpeedDegPerSecond <= 0f)
        {
            _aimDirection = desired;
            FaceDirection(_aimDirection);
            return;
        }

        _aimDirection = Vector3.RotateTowards(
            _aimDirection,
            desired,
            config.AimTrackingSpeedDegPerSecond * Mathf.Deg2Rad * Time.deltaTime,
            0f);

        FaceDirection(_aimDirection);
    }

    private Vector3 ResolveTargetPoint()
    {
        EnemyShooterConfigSO config = Config;
        Vector3 targetPoint = _target != null ? _target.position : transform.position;

        if (_targetRigidbody != null &&
            config != null &&
            config.TargetLeadSeconds > 0f)
        {
            targetPoint += _targetRigidbody.linearVelocity * config.TargetLeadSeconds;
        }

        return targetPoint;
    }

    private void UpdateBurstFire()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null)
            return;

        if (_burstShotsRemaining <= 0)
        {
            if (Time.time < _nextFireTime)
                return;

            _burstShotsRemaining = config.BurstCount;
            _nextBurstShotTime = Time.time;
        }

        if (Time.time < _nextBurstShotTime)
            return;

        if (TryFireOnce())
        {
            _burstShotsRemaining--;
            _nextBurstShotTime = Time.time + config.BurstInterval;

            if (_burstShotsRemaining <= 0)
                _nextFireTime = Time.time + 1f / ResolveShotsPerSecond();
        }
        else
        {
            _burstShotsRemaining = 0;
            _nextFireTime = Time.time + 0.1f;
        }
    }

    private void TickCombatMovement()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || !config.MoveWhileAttacking || !CanUseAgent() || _target == null)
            return;

        float stoppingDistance = ResolveChaseStoppingDistance();
        _agent.speed = ResolveMoveSpeed();
        _agent.stoppingDistance = stoppingDistance;

        if (GetPlanarDistanceToTarget() <= stoppingDistance + 0.05f)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
            return;
        }

        if (Time.time < _nextCombatMoveRefreshTime)
            return;

        _nextCombatMoveRefreshTime = Time.time + config.CombatMoveRefreshInterval;
        _agent.isStopped = false;
        _agent.SetDestination(_target.position);
    }

    private bool TryFireOnce()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || _shoot == null || _shoot.CurrentBullet == null || _target == null)
            return false;

        Vector3 direction = _aimDirection;

        if (direction.sqrMagnitude < 0.0001f)
            direction = (_target.position - transform.position).normalized;

        if (config.AimErrorAngleDeg > 0f)
        {
            float error = Random.Range(-config.AimErrorAngleDeg, config.AimErrorAngleDeg);
            direction = Quaternion.AngleAxis(error, Vector3.up) * direction;
        }

        Vector3 aimPoint = transform.position + direction.normalized * config.MaxRange;
        return _shoot.TryFireAt(aimPoint);
    }

    private float ResolveShotsPerSecond()
    {
        EnemyShooterConfigSO config = Config;

        if (_statsRuntime != null)
            return Mathf.Max(0.01f, _statsRuntime.ResolveShotsPerSecond(_shoot != null ? _shoot.CurrentBullet : null));

        return config != null ? config.ShotsPerSecond : 0.01f;
    }

    private bool TryPickPatrolDestination(out Vector3 destination)
    {
        destination = transform.position;

        EnemyShooterConfigSO config = Config;

        if (config == null)
            return false;

        float radius = Mathf.Max(0f, config.PatrolRadius);

        if (radius <= 0f)
            return false;

        for (int i = 0; i < 8; i++)
        {
            Vector2 random = Random.insideUnitCircle * radius;
            Vector3 candidate = _homePosition + new Vector3(random.x, 0f, random.y);

            if (NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    Mathf.Max(0.1f, config.NavMeshSampleDistance),
                    NavMesh.AllAreas))
            {
                destination = hit.position;
                return true;
            }
        }

        return false;
    }

    private bool TryGetPaintAvoidanceDestination(out Vector3 destination)
    {
        destination = default;

        EnemyShooterConfigSO config = Config;

        if (config == null || !config.AvoidVaccinePaint)
            return false;

        if (_attackActive && !config.AvoidVaccinePaintWhileAttacking)
            return false;

        if (Time.time < _nextPaintAvoidanceCheckTime)
            return false;

        _nextPaintAvoidanceCheckTime = Time.time + config.PaintAvoidanceCheckInterval;

        if (!IsOnVaccinePaint(transform.position))
            return false;

        ResolveMaskRenderManager();

        if (_maskRenderManager == null)
            return false;

        int sampleCount = Mathf.Max(1, config.PaintAvoidanceSampleCount);
        float radius = Mathf.Max(0.1f, config.PaintAvoidanceSearchRadius);

        for (int i = 0; i < sampleCount; i++)
        {
            float angle = (i / (float)sampleCount) * Mathf.PI * 2f;
            Vector3 raw = transform.position + new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)) * radius;

            if (!NavMesh.SamplePosition(
                    raw,
                    out NavMeshHit hit,
                    config.PaintAvoidanceNavMeshSampleDistance,
                    NavMesh.AllAreas))
            {
                continue;
            }

            if (IsOnVaccinePaint(hit.position))
                continue;

            destination = hit.position;
            return true;
        }

        return false;
    }

    private bool IsOnVaccinePaint(Vector3 worldPosition)
    {
        ResolveMaskRenderManager();

        if (_maskRenderManager == null)
            return false;

        if (!_maskRenderManager.TryGetStateAtWorld(
                worldPosition,
                out PaintSurfaceState state,
                true))
        {
            return false;
        }

        return state == PaintSurfaceState.Vaccine ||
               state == PaintSurfaceState.CoatedVaccine;
    }

    private bool HasReached(Vector3 point, float arriveDistance)
    {
        Vector3 from = transform.position;
        from.y = 0f;
        point.y = 0f;

        return Vector3.Distance(from, point) <= Mathf.Max(0f, arriveDistance);
    }

    private bool HasForgottenTarget()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || _lastSeenTime < 0f)
            return false;

        return config.TargetMemorySeconds > 0f &&
               Time.time >= _lastSeenTime + config.TargetMemorySeconds;
    }

    private float GetPlanarDistanceToTarget()
    {
        if (_target == null)
            return float.PositiveInfinity;

        Vector3 from = transform.position;
        Vector3 to = _target.position;
        from.y = 0f;
        to.y = 0f;

        return Vector3.Distance(from, to);
    }

    private float ResolveMoveSpeed()
    {
        EnemyShooterConfigSO config = Config;
        return config != null ? config.MoveSpeed : (_agent != null ? _agent.speed : 0f);
    }

    private float ResolveChaseStoppingDistance()
    {
        EnemyShooterConfigSO config = Config;
        return config != null ? config.ChaseStoppingDistance : 0f;
    }

    private bool CanUseAgent()
    {
        return _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void OnHealthChanged(Damageable damageable)
    {
        if (damageable == null || damageable != _damageable)
            return;

        _lastHealth = damageable.CurrentHealth;

        // Damage alert needs attacker context, so it is handled by EnemyShooter.NotifyDamagedBy.
    }

    private void OnHitReceived(GameObject hitTarget)
    {
        if (hitTarget == null)
            return;

        if (!IsThisShooter(hitTarget))
            return;

        // HitReceivedEvent has no attacker context. Do not globally alert from here.
    }

    private void OnSoundStimulus(SoundStimulus stimulus)
    {
        if (!stimulus.IsValid || IsOwnSource(stimulus.source))
            return;

        Vector3 from = transform.position;
        Vector3 to = stimulus.position;
        from.y = 0f;
        to.y = 0f;

        if (Vector3.Distance(from, to) > stimulus.radius)
            return;

        _hasSoundInvestigation = true;
        _soundInvestigationPosition = stimulus.position;
        _soundInvestigationMoveTime = Time.time + stimulus.investigateDelaySeconds;
        _lastKnownPosition = stimulus.position;
        _hasLastKnownPosition = true;
        _isAlerted = true;

        FaceDirection(stimulus.position - transform.position);
    }

    private void ResolveMaskRenderManager()
    {
        if (_maskRenderManager != null)
            return;

        if (_maskRenderManagerReadyChannel != null && _maskRenderManagerReadyChannel.Current != null)
        {
            _maskRenderManager = _maskRenderManagerReadyChannel.Current;
            return;
        }

        _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();
    }

    private void OnMaskRenderManagerReady(MaskRenderManager manager)
    {
        if (manager != null)
            _maskRenderManager = manager;
    }

    private bool IsThisShooter(GameObject target)
    {
        if (target == gameObject)
            return true;

        Transform current = target.transform;

        while (current != null)
        {
            if (current == transform)
                return true;

            current = current.parent;
        }

        return false;
    }

    private bool IsOwnSource(GameObject source)
    {
        if (source == null)
            return false;

        if (source == gameObject)
            return true;

        return source.transform.IsChildOf(transform) || transform.IsChildOf(source.transform);
    }

    private bool TryResolvePlayerTransform(GameObject attacker, out Transform playerTransform)
    {
        playerTransform = null;

        if (attacker == null)
            return false;

        VSplatter_Character player =
            attacker.GetComponent<VSplatter_Character>() ??
            attacker.GetComponentInParent<VSplatter_Character>();

        if (player == null)
            return false;

        Damageable playerDamageable =
            player.GetComponent<Damageable>() ??
            player.GetComponentInParent<Damageable>();

        if (playerDamageable == null || playerDamageable.IsDead)
            return false;

        playerTransform = player.transform;
        return playerTransform != null;
    }

    private bool IsWithinAwarenessRange(Vector3 position)
    {
        EnemyShooterConfigSO config = Config;
        float range = config != null ? config.Vision.VisionRange : 0f;

        if (range <= 0f)
            return false;

        Vector3 from = transform.position;
        from.y = 0f;
        position.y = 0f;

        return Vector3.Distance(from, position) <= range;
    }

    private void ResolveRefs()
    {
        if (_shoot == null)
            _shoot = ResolveComponentInOwnerHierarchy<EnemyShooterShoot>();

        if (_statsRuntime == null)
            _statsRuntime = ResolveComponentInOwnerHierarchy<EnemyShooterStatsRuntime>();

        if (_vision == null)
            _vision = ResolveComponentInOwnerHierarchy<EntityVisionController>();

        if (_agent == null)
            _agent = ResolveComponentInOwnerHierarchy<NavMeshAgent>();

        if (_damageable == null)
            _damageable = ResolveComponentInOwnerHierarchy<Damageable>();
    }

    private T ResolveComponentInOwnerHierarchy<T>() where T : Component
    {
        T component = GetComponent<T>();

        if (component != null)
            return component;

        component = GetComponentInChildren<T>();

        if (component != null)
            return component;

        Transform current = transform.parent;

        while (current != null)
        {
            component = current.GetComponent<T>();

            if (component != null)
                return component;

            component = current.GetComponentInChildren<T>();

            if (component != null)
                return component;

            current = current.parent;
        }

        return null;
    }
}
