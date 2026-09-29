/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines ChasingTargetActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
        MovementStatsProvider,
        AgentCurrentValue
    }

    [Header("Target")]
    [SerializeField] private TargetSource _targetSource = TargetSource.EnemyCurrentTarget;
    [SerializeField] private TransformAnchor _fallbackTarget = default;

    [Header("Speed")]
    [SerializeField] private SpeedSource _speedSource = SpeedSource.FixedValue;
    [SerializeField] private float _fixedChasingSpeed = 3.5f;

    public TargetSource TargetMode => _targetSource;
    public TransformAnchor FallbackTarget => _fallbackTarget;
    public SpeedSource SpeedMode => _speedSource;
    public float FixedChasingSpeed => _fixedChasingSpeed;

    // Purpose: Creates the action owned by this workflow.
    protected override StateAction CreateAction() => new ChasingTargetAction();
}

public class ChasingTargetAction : StateAction
{
    private ChasingTargetActionSO _config;
    private NavMeshAgent _agent;
    private Enemy _enemy;
    private EnemyMovementStatsProvider _movementStatsProvider;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _config = (ChasingTargetActionSO)OriginSO;
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _enemy = stateMachine.GetComponent<Enemy>();
        stateMachine.TryGetComponent(out _movementStatsProvider);
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (!CanUseAgent())
            return;

        _agent.speed = ResolveSpeed();
        _agent.isStopped = false;
    }

    // Purpose: Responds to the update callback.
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

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (!CanUseAgent())
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }

    // Purpose: Checks whether use agent is currently permitted.
    private bool CanUseAgent()
    {
        return _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    }

    // Purpose: Resolves the target needed by this component.
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

    // Purpose: Resolves the speed needed by this component.
    private float ResolveSpeed()
    {
        switch (_config.SpeedMode)
        {
            case ChasingTargetActionSO.SpeedSource.MovementStatsProvider:
                if (_movementStatsProvider != null)
                    return _movementStatsProvider.PlayerChaseMovementSpeed;
                break;

            case ChasingTargetActionSO.SpeedSource.AgentCurrentValue:
                return _agent.speed;
        }

        return _config.FixedChasingSpeed;
    }
}
