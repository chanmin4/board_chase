using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "MoveToInfectionTargetAction",
    menuName = "State Machines/Enemy Actions/Move To Infection Target")]
public class MoveToInfectionTargetActionSO : StateActionSO
{
    [Header("Definition Config")]
    [SerializeField] private NormalEnemyMoveToInfectionTargetConfigSO _definitionConfig;

    public bool HasDefinitionConfig => _definitionConfig != null;
    public float MoveSpeed => _definitionConfig.MoveSpeed;
    public float StoppingDistance => _definitionConfig.StoppingDistance;
    protected override StateAction CreateAction() => new MoveToInfectionTargetAction();
}

public class MoveToInfectionTargetAction : StateAction
{
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private MoveToInfectionTargetActionSO _config;
    private bool _isActiveAgent;
    private EnemyMovementStatsProvider _movementStatsProvider;
    private bool _hasConfig;
    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _config = (MoveToInfectionTargetActionSO)OriginSO;
        stateMachine.TryGetComponent(out _movementStatsProvider);

        _isActiveAgent = _agent != null && _agent.isActiveAndEnabled;
    }
    public override void OnStateEnter()
    {
        _hasConfig = _config.HasDefinitionConfig;

        if (!_hasConfig)
        {
            Debug.LogError("[MoveToInfectionTargetAction] Definition Config is missing.", _enemy);
            return;
        }

        if (!_isActiveAgent)
            return;
        float speed = _movementStatsProvider != null
            ? _movementStatsProvider.NormalMovementSpeed
            : _config.MoveSpeed;

        _agent.speed = speed;
        _agent.stoppingDistance = _config.StoppingDistance;
        _agent.isStopped = false;

        if (!_enemy.HasInfectionTarget)
            _enemy.SetInfectionTargetvalid(false);
    }
    public override void OnUpdate()
    {
        if (!_hasConfig)
            return;

        if (!_isActiveAgent || _enemy == null)
            return;

        if (!_enemy.HasInfectionTarget)
        {
            _enemy.SetInfectionTargetvalid(false);
            return;
        }

        _agent.SetDestination(_enemy.InfectionTargetPosition);

        if (_agent.pathPending)
            return;

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            _enemy.SetInfectionTargetvalid(false);
            return;
        }

        _enemy.SetInfectionTargetvalid(true);
    }
    public override void OnStateExit()
    {
        if (_isActiveAgent)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
    }
}
