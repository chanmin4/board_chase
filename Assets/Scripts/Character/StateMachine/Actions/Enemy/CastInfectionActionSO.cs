using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "CastInfectionAction",
    menuName = "State Machines/Enemy Actions/Cast Infection")]
public class CastInfectionActionSO : StateActionSO
{
    [SerializeField] private float _infectionRadius = 1.5f;
    [SerializeField] private bool _applyOnStateExit = true;

    public float InfectionRadius => _infectionRadius;
    public bool ApplyOnStateExit => _applyOnStateExit;

    protected override StateAction CreateAction() => new CastInfectionAction();
}

public class CastInfectionAction : StateAction
{
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private CastInfectionActionSO _config;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _config = (CastInfectionActionSO)OriginSO;
    }

    public override void OnStateEnter()
    {
        if (_agent != null && _agent.isActiveAndEnabled)
            _agent.isStopped = true;
    }

    public override void OnUpdate() { }

    public override void OnStateExit()
    {
        if (_config.ApplyOnStateExit)
            ApplyInfection();

        if (_enemy != null)
        {
            _enemy.ClearInfectionTarget();
            _enemy.SetSearchSucceeded(false);
            _enemy.SetInfectionTargetvalid(false);
        }
    }

    private void ApplyInfection()
    {
        if (_enemy == null || !_enemy.HasInfectionTarget)
            return;

        Vector3 center = _enemy.InfectionTargetPosition;

        // TODO:
        // 여기서 MaskRenderManager / stamp 시스템 호출
        // 반경: _config.InfectionRadius
        // 중심: center
    }
}
