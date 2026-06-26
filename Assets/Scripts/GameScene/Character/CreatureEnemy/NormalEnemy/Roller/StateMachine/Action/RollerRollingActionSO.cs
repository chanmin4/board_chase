using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "RollerRollingAction",
    menuName = "State Machines/Enemy Actions/Roller/Rolling")]
public class RollerRollingActionSO : StateActionSO<RollerRollingAction>
{
    [SerializeField] private RollerAttackConfigSO _config;

    public RollerAttackConfigSO Config => _config;
}

public class RollerRollingAction : StateAction
{
    private const float MinDirectionSqrMagnitude = 0.0001f;

    private RollerRollingActionSO _origin;
    private RollerAttackConfigSO _config;
    private Enemy _enemy;
    private Transform _transform;
    private Rigidbody _rigidbody;
    private NavMeshAgent _agent;
    private RaycastHit[] _wallHits;

    private Vector3 _direction;
    private bool _hasDirection;
    private bool _usingAttackSpeed;
    private float _nextAttackReadyTime;

    private bool _wasAgentEnabled;
    private bool _previousAgentUpdatePosition;
    private bool _previousAgentUpdateRotation;
    private bool _previousAgentStopped;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (RollerRollingActionSO)OriginSO;
        _config = _origin.Config;
        _enemy = stateMachine.GetComponent<Enemy>();
        _transform = stateMachine.transform;

        stateMachine.TryGetComponent(out _rigidbody);
        stateMachine.TryGetComponent(out _agent);
    }

    public override void OnStateEnter()
    {
        _config = _origin.Config;
        _usingAttackSpeed = false;

        if (_config == null)
        {
            Debug.LogError("[RollerRollingAction] RollerAttackConfig is missing.", _transform);
            return;
        }

        EnsureHitBuffer();
        StopRigidbodyDrift();
        DisableAgentIfNeeded();
        PickInitialDirection();
        ScheduleNextAttackReadyTime();
    }

    public override void OnUpdate()
    {
        if (_config == null || _transform == null)
            return;

        if (!_hasDirection)
            PickInitialDirection();

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        MoveWithBounces(ResolveCurrentSpeed() * dt);
        RotateAlongDirection(dt);

        if (_config.DebugDrawDirection)
        {
            Debug.DrawRay(
                _transform.position,
                _direction * 2f,
                _usingAttackSpeed ? Color.red : Color.yellow,
                _config.DebugDrawDuration);
        }
    }

    public override void OnStateExit()
    {
        RestoreAgentIfNeeded();
        StopRigidbodyDrift();
    }

    private void MoveWithBounces(float distance)
    {
        int bounceLimit = _config.MaxBouncesPerFrame;
        int bounceCount = 0;
        float remainingDistance = Mathf.Max(0f, distance);

        while (remainingDistance > 0.0001f && bounceCount <= bounceLimit)
        {
            Vector3 origin = _transform.position;

            if (!TryGetBounce(origin, _direction, remainingDistance, out float hitDistance, out Vector3 hitNormal))
            {
                SetPosition(origin + _direction * remainingDistance);
                break;
            }

            float skin = _config.CollisionSkin;
            float travelDistance = Mathf.Max(0f, hitDistance - skin);

            SetPosition(origin + _direction * travelDistance);

            remainingDistance = Mathf.Max(0f, remainingDistance - Mathf.Max(hitDistance, skin));
            HandleBounce(hitNormal);
            bounceCount++;
        }
    }

    private bool TryGetBounce(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        out float hitDistance,
        out Vector3 hitNormal)
    {
        hitDistance = float.PositiveInfinity;
        hitNormal = Vector3.zero;

        bool found = false;

        if (TryGetPhysicsBounce(origin, direction, maxDistance, out float physicsDistance, out Vector3 physicsNormal))
        {
            hitDistance = physicsDistance;
            hitNormal = physicsNormal;
            found = true;
        }

        if (TryGetSectorBoundsBounce(origin, direction, maxDistance, out float boundsDistance, out Vector3 boundsNormal) &&
            boundsDistance < hitDistance)
        {
            hitDistance = boundsDistance;
            hitNormal = boundsNormal;
            found = true;
        }

        return found && hitNormal.sqrMagnitude > MinDirectionSqrMagnitude;
    }

    private bool TryGetPhysicsBounce(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        out float hitDistance,
        out Vector3 hitNormal)
    {
        hitDistance = 0f;
        hitNormal = Vector3.zero;

        if (_config.WallMask.value == 0 || _wallHits == null || _wallHits.Length == 0)
            return false;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _config.CollisionRadius,
            direction,
            _wallHits,
            maxDistance + _config.CollisionSkin,
            _config.WallMask,
            _config.WallTriggerInteraction);

        float bestDistance = float.PositiveInfinity;
        Vector3 bestNormal = Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _wallHits[i];
            Collider hitCollider = hit.collider;

            if (hitCollider == null || IsSelfCollider(hitCollider))
                continue;

            if (hit.distance >= bestDistance)
                continue;

            bestDistance = Mathf.Max(0f, hit.distance - _config.CollisionSkin);
            bestNormal = hit.normal;
        }

        if (float.IsInfinity(bestDistance) || bestNormal.sqrMagnitude <= MinDirectionSqrMagnitude)
            return false;

        hitDistance = bestDistance;
        hitNormal = FlattenDirection(bestNormal).normalized;
        return true;
    }

    private bool TryGetSectorBoundsBounce(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        out float hitDistance,
        out Vector3 hitNormal)
    {
        hitDistance = 0f;
        hitNormal = Vector3.zero;

        if (!_config.UseCurrentSectorBounds || _enemy == null || _enemy.CurrentSector == null)
            return false;

        Bounds bounds = _enemy.CurrentSector.GetWorldBounds();
        float padding = _config.SectorEdgePadding + _config.CollisionRadius;

        float minX = bounds.min.x + padding;
        float maxX = bounds.max.x - padding;
        float minZ = bounds.min.z + padding;
        float maxZ = bounds.max.z - padding;

        if (minX >= maxX || minZ >= maxZ)
            return false;

        float bestDistance = float.PositiveInfinity;
        Vector3 bestNormal = Vector3.zero;

        TestAxisBoundary(origin.x, direction.x, minX, maxX, Vector3.right, Vector3.left, maxDistance, ref bestDistance, ref bestNormal);
        TestAxisBoundary(origin.z, direction.z, minZ, maxZ, Vector3.forward, Vector3.back, maxDistance, ref bestDistance, ref bestNormal);

        if (float.IsInfinity(bestDistance))
            return false;

        hitDistance = Mathf.Max(0f, bestDistance);
        hitNormal = bestNormal;
        return true;
    }

    private static void TestAxisBoundary(
        float position,
        float direction,
        float min,
        float max,
        Vector3 minNormal,
        Vector3 maxNormal,
        float maxDistance,
        ref float bestDistance,
        ref Vector3 bestNormal)
    {
        if (Mathf.Abs(direction) <= 0.0001f)
            return;

        float boundary = direction > 0f ? max : min;
        float distance = (boundary - position) / direction;

        if (distance < 0f || distance > maxDistance || distance >= bestDistance)
            return;

        bestDistance = distance;
        bestNormal = direction > 0f ? maxNormal : minNormal;
    }

    private void HandleBounce(Vector3 normal)
    {
        if (TryRetargetToPlayerOnBounce(out Vector3 targetDirection))
        {
            _direction = targetDirection;
            _usingAttackSpeed = true;
            ScheduleNextAttackReadyTime();
            return;
        }

        Vector3 reflected = Vector3.Reflect(_direction, normal);
        reflected = FlattenDirection(reflected);

        if (reflected.sqrMagnitude <= MinDirectionSqrMagnitude)
            reflected = PickRandomPlanarDirection();

        _direction = reflected.normalized;
        _usingAttackSpeed = false;
        _hasDirection = true;
    }

    private bool TryRetargetToPlayerOnBounce(out Vector3 targetDirection)
    {
        targetDirection = Vector3.zero;

        if (Time.time < _nextAttackReadyTime)
            return false;

        if (TryGetTargetDirection(out targetDirection))
            return true;

        if (_config.RetargetOnlyWhenTargetExists)
            return false;

        targetDirection = PickRandomPlanarDirection();
        return true;
    }

    private bool TryGetTargetDirection(out Vector3 targetDirection)
    {
        targetDirection = Vector3.zero;

        Damageable target = _enemy != null ? _enemy.currentTarget : null;

        if (target == null || target.IsDead)
        {
            VSplatter_Character player = Object.FindAnyObjectByType<VSplatter_Character>();
            if (player != null)
                target = player.GetComponent<Damageable>() ?? player.GetComponentInParent<Damageable>();
        }

        if (target == null || target.IsDead)
            return false;

        Vector3 direction = target.transform.position - _transform.position;
        direction = FlattenDirection(direction);

        if (direction.sqrMagnitude <= MinDirectionSqrMagnitude)
            return false;

        targetDirection = direction.normalized;
        return true;
    }

    private void PickInitialDirection()
    {
        Vector3 direction = _config.PickRandomDirectionOnEnter
            ? PickRandomPlanarDirection()
            : FlattenDirection(_transform.forward);

        if (direction.sqrMagnitude <= MinDirectionSqrMagnitude)
            direction = PickRandomPlanarDirection();

        _direction = direction.normalized;
        _hasDirection = true;
    }

    private void ScheduleNextAttackReadyTime()
    {
        _nextAttackReadyTime = Time.time + Random.Range(
            _config.MinAttackReadyDelay,
            _config.MaxAttackReadyDelay);
    }

    private float ResolveCurrentSpeed()
    {
        return _usingAttackSpeed
            ? _config.AttackRollMoveSpeed
            : _config.RollMoveSpeed;
    }

    private void RotateAlongDirection(float dt)
    {
        if (!_config.FaceMoveDirection || _direction.sqrMagnitude <= MinDirectionSqrMagnitude)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(_direction, Vector3.up);
        float turnSpeed = _config.TurnSpeedDegPerSecond;

        _transform.rotation = turnSpeed <= 0f
            ? targetRotation
            : Quaternion.RotateTowards(_transform.rotation, targetRotation, turnSpeed * dt);
    }

    private void SetPosition(Vector3 position)
    {
        if (_rigidbody != null)
        {
            _rigidbody.MovePosition(position);
            return;
        }

        _transform.position = position;
    }

    private void EnsureHitBuffer()
    {
        int size = _config.MaxWallHitBufferSize;

        if (_wallHits == null || _wallHits.Length != size)
            _wallHits = new RaycastHit[size];
    }

    private void StopRigidbodyDrift()
    {
        if (_rigidbody == null)
            return;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    private void DisableAgentIfNeeded()
    {
        if (_agent == null || !_config.DisableNavMeshAgentWhileRolling)
            return;

        _wasAgentEnabled = _agent.enabled;
        _previousAgentUpdatePosition = _agent.updatePosition;
        _previousAgentUpdateRotation = _agent.updateRotation;
        _previousAgentStopped = _agent.isStopped;

        if (!_agent.enabled)
            return;

        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        _agent.enabled = false;
    }

    private void RestoreAgentIfNeeded()
    {
        if (_agent == null || !_config.DisableNavMeshAgentWhileRolling)
            return;

        if (_agent.enabled != _wasAgentEnabled)
            _agent.enabled = _wasAgentEnabled;

        _agent.updatePosition = _previousAgentUpdatePosition;
        _agent.updateRotation = _previousAgentUpdateRotation;

        if (_agent.enabled)
        {
            if (_agent.isOnNavMesh)
                _agent.Warp(_transform.position);

            _agent.isStopped = _previousAgentStopped;
        }
    }

    private bool IsSelfCollider(Collider hitCollider)
    {
        if (hitCollider == null || _transform == null)
            return false;

        Transform hitTransform = hitCollider.transform;
        return hitTransform == _transform || hitTransform.IsChildOf(_transform);
    }

    private static Vector3 PickRandomPlanarDirection()
    {
        Vector2 circle = Random.insideUnitCircle;

        if (circle.sqrMagnitude <= MinDirectionSqrMagnitude)
            circle = Vector2.right;

        return new Vector3(circle.x, 0f, circle.y).normalized;
    }

    private static Vector3 FlattenDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction;
    }
}
