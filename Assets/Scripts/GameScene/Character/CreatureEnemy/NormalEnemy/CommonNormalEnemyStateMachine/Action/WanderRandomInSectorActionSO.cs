using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "WanderRandomInSectorAction",
    menuName = "State Machines/Enemy Actions/Wander Random In Sector")]
public class WanderRandomInSectorActionSO : StateActionSO
{
    [Header("Definition Config")]
    [Tooltip("Shared config for normal enemy wander/search/infection behavior.")]
    [SerializeField] private NormalEnemyBehaviorConfigSO _behaviorConfig;

    public bool HasBehaviorConfig => _behaviorConfig != null;
    public float MoveSpeed => _behaviorConfig.WanderMoveSpeed;
    public float MinTravelDistance => _behaviorConfig.WanderMinTravelDistance;
    public float EdgePadding => _behaviorConfig.WanderEdgePadding;
    public float NavMeshSampleMaxDistance => _behaviorConfig.WanderNavMeshSampleMaxDistance;
    public int CandidateCount => _behaviorConfig.WanderCandidateCount;
    public int MaxRepickCount => _behaviorConfig.WanderMaxRepickCount;
    public bool DebugLogs => _behaviorConfig.WanderDebugLogs;
    public bool DebugDraw => _behaviorConfig.WanderDebugDraw;
    public float DebugDrawDuration => _behaviorConfig.WanderDebugDrawDuration;
    
    protected override StateAction CreateAction() => new WanderRandomInSectorAction();
}

public class WanderRandomInSectorAction : StateAction
{
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private WanderRandomInSectorActionSO _config;
    private EnemyMovementStatsProvider _movementStatsProvider;
    private bool _isActiveAgent;
    private bool _hasDestination;
    private int _repickCount;
    private Vector3 _destination;
    private bool _hasConfig;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _config = (WanderRandomInSectorActionSO)OriginSO;
        stateMachine.TryGetComponent(out _movementStatsProvider);
    }

    public override void OnStateEnter()
    {
        _hasConfig = _config.HasBehaviorConfig;
        _isActiveAgent = _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
        _hasDestination = false;
        _repickCount = 0;

        if (!_hasConfig)
        {
            Debug.LogError("[WanderRandomInSectorAction] NormalEnemyBehaviorConfig is missing.", _enemy);
            return;
        }

        if (!_isActiveAgent || _enemy == null || _enemy.CurrentSector == null)
            return;

        float speed = _movementStatsProvider != null
            ? _movementStatsProvider.NormalMovementSpeed
            : _config.MoveSpeed;

        _agent.speed = speed;
        _agent.isStopped = false;

        TryPickAndApplyDestination();
    }

    public override void OnUpdate()
    {
        if (!_hasConfig)
            return;

        if (!_isActiveAgent || !_hasDestination)
            return;

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid && _repickCount < _config.MaxRepickCount)
        {
            _repickCount++;
            TryPickAndApplyDestination();
            return;
        }

        if (_config.DebugDraw)
            Debug.DrawLine(_enemy.transform.position, _destination, Color.cyan, _config.DebugDrawDuration);
    }

    public override void OnStateExit()
    {
        if (!_isActiveAgent)
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
        _hasDestination = false;
    }

    private void TryPickAndApplyDestination()
    {
        if (!TryPickDestination(out _destination))
        {
            _hasDestination = false;
            _agent.ResetPath();
            _agent.isStopped = true;

            if (_config.DebugLogs)
                Debug.Log("[WanderRandomInSector] failed to pick destination.");
            return;
        }

        _hasDestination = _agent.SetDestination(_destination);
        _agent.isStopped = !_hasDestination;

        if (_config.DebugLogs)
            Debug.Log($"[WanderRandomInSector] destination={_destination}, accepted={_hasDestination}");

        if (_config.DebugDraw)
        {
            Debug.DrawLine(_destination, _destination + Vector3.up * 0.8f, Color.cyan, _config.DebugDrawDuration);
            Debug.DrawLine(_enemy.transform.position, _destination, Color.cyan, _config.DebugDrawDuration);
        }
    }

    private bool TryPickDestination(out Vector3 destination)
    {
        destination = default;

        if (_enemy == null || _enemy.CurrentSector == null)
            return false;

        Bounds bounds = _enemy.CurrentSector.GetWorldBounds();
        Vector3 origin = _enemy.transform.position;

        float minX = bounds.min.x + _config.EdgePadding;
        float maxX = bounds.max.x - _config.EdgePadding;
        float minZ = bounds.min.z + _config.EdgePadding;
        float maxZ = bounds.max.z - _config.EdgePadding;

        if (minX >= maxX || minZ >= maxZ)
            return false;

        int candidateCount = Mathf.Max(1, _config.CandidateCount);

        for (int i = 0; i < candidateCount; i++)
        {
            Vector3 raw = new Vector3(
                Random.Range(minX, maxX),
                origin.y,
                Random.Range(minZ, maxZ));

            Vector3 flatOffset = raw - origin;
            flatOffset.y = 0f;

            if (flatOffset.sqrMagnitude < _config.MinTravelDistance * _config.MinTravelDistance)
                continue;

            if (!NavMesh.SamplePosition(raw, out NavMeshHit hit, _config.NavMeshSampleMaxDistance, NavMesh.AllAreas))
                continue;

            if (!IsInsideBoundsXZ(bounds, hit.position, _config.EdgePadding))
                continue;

            destination = hit.position;
            return true;
        }

        return false;
    }

    private static bool IsInsideBoundsXZ(Bounds bounds, Vector3 point, float padding)
    {
        return point.x >= bounds.min.x + padding &&
               point.x <= bounds.max.x - padding &&
               point.z >= bounds.min.z + padding &&
               point.z <= bounds.max.z - padding;
    }
}
