using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "MoveToInfectionTargetAction",
    menuName = "State Machines/Enemy Actions/Move To Infection Target")]
public class MoveToInfectionTargetActionSO : StateActionSO
{
    [SerializeField] private float _moveSpeed = 3.5f;

    public float MoveSpeed => _moveSpeed;

    protected override StateAction CreateAction() => new MoveToInfectionTargetAction();
}

public class MoveToInfectionTargetAction : StateAction
{
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private MoveToInfectionTargetActionSO _config;
    private bool _isActiveAgent;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _config = (MoveToInfectionTargetActionSO)OriginSO;

        _isActiveAgent = _agent != null && _agent.isActiveAndEnabled;
    }

    public override void OnStateEnter()
    {
        if (!_isActiveAgent)
            return;

        _agent.speed = _config.MoveSpeed;
        _agent.isStopped = false;

        if (!_enemy.HasInfectionTarget)
            _enemy.SetInfectionTargetvalid(false);
    }

    public override void OnUpdate()
    {
        if (!_isActiveAgent || _enemy == null)
            return;

        if (!_enemy.HasInfectionTarget)
        {
            _enemy.SetInfectionTargetvalid(false);
            return;
        }

        _agent.SetDestination(_enemy.InfectionTargetPosition);

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            _enemy.SetInfectionTargetvalid(false);
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
