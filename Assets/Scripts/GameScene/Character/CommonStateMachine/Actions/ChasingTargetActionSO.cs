using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ChasingTargetAction",
    menuName = "State Machines/Enemy Actions/Chasing Target")]
public class ChasingTargetActionSO : StateActionSO
{
    public enum TargetSource
    {
        EnemyCurrentTarget,
        TransformAnchorFallbackOnly
    }

    public enum SpeedSource
    {
        FixedValue,
        MovementStatsSO,
        AgentCurrentValue
    }

    [Header("Target")]
    [SerializeField] private TargetSource _targetSource = TargetSource.EnemyCurrentTarget;
    [SerializeField] private TransformAnchor _fallbackTarget = default;

    [Header("Speed")]
    [SerializeField] private SpeedSource _speedSource = SpeedSource.FixedValue;
    [SerializeField] private float _fixedChasingSpeed = 3.5f;
    [SerializeField] private EnemyMovementStatsSO _movementStats = default;

    public TargetSource TargetMode => _targetSource;
    public TransformAnchor FallbackTarget => _fallbackTarget;

    public SpeedSource SpeedMode => _speedSource;
    public float FixedChasingSpeed => _fixedChasingSpeed;
    public EnemyMovementStatsSO MovementStats => _movementStats;

    protected override StateAction CreateAction() => new ChasingTargetAction();
}

public class ChasingTargetAction : StateAction
{
    private ChasingTargetActionSO _config;
    private NavMeshAgent _agent;
    private Enemy _enemy;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (ChasingTargetActionSO)OriginSO;
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _enemy = stateMachine.GetComponent<Enemy>();
    }

    public override void OnStateEnter()
    {
        if (!CanUseAgent())
            return;

        _agent.speed = ResolveSpeed();
        _agent.isStopped = false;
    }

    public override void OnUpdate()
    {
        if (!CanUseAgent())
            return;

        Transform target = ResolveTarget();
        if (target == null)
        {
            _agent.isStopped = true;
            return;
        }

        _agent.isStopped = false;
        _agent.SetDestination(target.position);
    }

    public override void OnStateExit()
    {
        if (!CanUseAgent())
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }

    private bool CanUseAgent()
    {
        return _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    }

    private Transform ResolveTarget()
    {
        if (_config.TargetMode == ChasingTargetActionSO.TargetSource.EnemyCurrentTarget)
        {
            if (_enemy != null && _enemy.currentTarget != null)
                return _enemy.currentTarget.transform;
        }

        if (_config.FallbackTarget != null && _config.FallbackTarget.Value != null)
            return _config.FallbackTarget.Value;

        return null;
    }

    private float ResolveSpeed()
    {
        switch (_config.SpeedMode)
        {
            case ChasingTargetActionSO.SpeedSource.MovementStatsSO:
                if (_config.MovementStats != null)
                    return _config.MovementStats.ChaseSpeed;
                break;

            case ChasingTargetActionSO.SpeedSource.AgentCurrentValue:
                return _agent.speed;
        }

        return _config.FixedChasingSpeed;
    }
}
