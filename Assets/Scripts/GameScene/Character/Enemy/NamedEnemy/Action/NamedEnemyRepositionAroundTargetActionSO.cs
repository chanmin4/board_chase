// Assets/Scripts/GameScene/Character/Enemy/NamedEnemy/Action/NamedEnemyRepositionAroundTargetActionSO.cs

using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NamedEnemyRepositionAroundTargetAction",
    menuName = "State Machines/Named Enemy Actions/Reposition Around Target")]
public class NamedEnemyRepositionAroundTargetActionSO : StateActionSO<NamedEnemyRepositionAroundTargetAction>
{
    public enum SpeedSource
    {
        FixedValue,
        MovementStatsNormal,
        AgentCurrentValue
    }

    [Header("Definition Config")]
    [SerializeField] private NamedRepositionConfigSO _definitionConfig;

    public bool HasDefinitionConfig => _definitionConfig != null;

    public SpeedSource MoveSpeedSource => ConvertSpeedSource(_definitionConfig.MoveSpeedSource);
    public float FixedMoveSpeed => _definitionConfig.FixedMoveSpeed;
    public float TooCloseDistance => _definitionConfig.TooCloseDistance;
    public float PreferredDistance => _definitionConfig.PreferredDistance;
    public float TooFarDistance => _definitionConfig.TooFarDistance;
    public float StrafeDistance => _definitionConfig.StrafeDistance;
    public float DestinationRefreshInterval => _definitionConfig.DestinationRefreshInterval;
    public float NavMeshSampleDistance => _definitionConfig.NavMeshSampleDistance;
    public bool FaceTarget => _definitionConfig.FaceTarget;
    public float TurnSpeedDegPerSecond => _definitionConfig.TurnSpeedDegPerSecond;
    public bool DebugDrawDistances => _definitionConfig.DebugDrawDistances;
    public float DebugDrawHeight => _definitionConfig.DebugDrawHeight;

    private static SpeedSource ConvertSpeedSource(NamedRepositionConfigSO.SpeedSource source)
    {
        switch (source)
        {
            case NamedRepositionConfigSO.SpeedSource.MovementStatsNormal:
                return SpeedSource.MovementStatsNormal;

            case NamedRepositionConfigSO.SpeedSource.AgentCurrentValue:
                return SpeedSource.AgentCurrentValue;

            default:
                return SpeedSource.FixedValue;
        }
    }
}

public class NamedEnemyRepositionAroundTargetAction : StateAction
{
    private NamedEnemyRepositionAroundTargetActionSO _origin;
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private EnemyMovementStatsProvider _movementStatsProvider;
    private Transform _owner;

    private float _side;
    private float _nextRefreshTime;
    private bool _hasConfig;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (NamedEnemyRepositionAroundTargetActionSO)OriginSO;
        _enemy = stateMachine.GetComponent<Enemy>();
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        stateMachine.TryGetComponent(out _movementStatsProvider);

        _owner = _enemy != null ? _enemy.transform : stateMachine.transform;
    }

    public override void OnStateEnter()
    {
        _hasConfig = _origin.HasDefinitionConfig;
        _side = Random.value < 0.5f ? -1f : 1f;
        _nextRefreshTime = 0f;

        if (!_hasConfig)
        {
            Debug.LogError("[NamedEnemyRepositionAroundTargetAction] Definition Config is missing.", _owner);
            return;
        }

        if (!IsAgentReady())
            return;

        _agent.speed = ResolveMoveSpeed();
        _agent.isStopped = false;
    }

    public override void OnUpdate()
    {
        if (!_hasConfig)
            return;

        if (_enemy == null || _enemy.currentTarget == null)
        {
            StopAgent();
            return;
        }

        Vector3 targetPos = _enemy.currentTarget.transform.position;

        if (_origin.FaceTarget)
            RotateToward(targetPos);

        if (_origin.DebugDrawDistances)
            DrawDistanceDebug(targetPos);

        if (!IsAgentReady())
            return;

        if (Time.time < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.time + _origin.DestinationRefreshInterval;

        Vector3 destination = PickDestination(targetPos);

        if (NavMesh.SamplePosition(
                destination,
                out NavMeshHit hit,
                _origin.NavMeshSampleDistance,
                NavMesh.AllAreas))
        {
            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
        }
    }

    public override void OnStateExit()
    {
        StopAgent();
    }

    private Vector3 PickDestination(Vector3 targetPos)
    {
        Vector3 selfPos = _owner.position;

        Vector3 fromTarget = selfPos - targetPos;
        fromTarget.y = 0f;

        if (fromTarget.sqrMagnitude < 0.0001f)
            fromTarget = -_owner.forward;

        float distance = fromTarget.magnitude;
        Vector3 away = fromTarget.normalized;
        Vector3 toward = -away;
        Vector3 tangent = new Vector3(-away.z, 0f, away.x) * _side;

        if (distance < _origin.TooCloseDistance)
        {
            Vector3 move = (away + tangent * 0.55f).normalized;
            return selfPos + move * _origin.StrafeDistance;
        }

        if (distance > _origin.TooFarDistance)
        {
            Vector3 move = (toward + tangent * 0.35f).normalized;
            return selfPos + move * _origin.StrafeDistance;
        }

        return targetPos +
               away * _origin.PreferredDistance +
               tangent * _origin.StrafeDistance;
    }

    private void RotateToward(Vector3 targetPos)
    {
        Vector3 direction = targetPos - _owner.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        _owner.rotation = Quaternion.RotateTowards(
            _owner.rotation,
            targetRotation,
            _origin.TurnSpeedDegPerSecond * Time.deltaTime);
    }

    private float ResolveMoveSpeed()
    {
        switch (_origin.MoveSpeedSource)
        {
            case NamedEnemyRepositionAroundTargetActionSO.SpeedSource.MovementStatsNormal:
                if (_movementStatsProvider != null)
                    return _movementStatsProvider.NormalMovementSpeed;
                break;

            case NamedEnemyRepositionAroundTargetActionSO.SpeedSource.AgentCurrentValue:
                if (_agent != null)
                    return _agent.speed;
                break;
        }

        return _origin.FixedMoveSpeed;
    }

    private bool IsAgentReady()
    {
        return _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    }

    private void StopAgent()
    {
        if (!IsAgentReady())
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }

    private void DrawDistanceDebug(Vector3 targetPos)
    {
        Vector3 center = targetPos;
        center.y = _owner.position.y + _origin.DebugDrawHeight;

        DrawCircle(center, _origin.TooCloseDistance, Color.red);
        DrawCircle(center, _origin.PreferredDistance, Color.green);
        DrawCircle(center, _origin.TooFarDistance, Color.yellow);

        Vector3 self = _owner.position;
        self.y = center.y;

        Debug.DrawLine(center, self, Color.cyan);
    }

    private static void DrawCircle(Vector3 center, float radius, Color color)
    {
        const int segments = 64;
        Vector3 previous = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            Debug.DrawLine(previous, next, color);
            previous = next;
        }
    }
}
