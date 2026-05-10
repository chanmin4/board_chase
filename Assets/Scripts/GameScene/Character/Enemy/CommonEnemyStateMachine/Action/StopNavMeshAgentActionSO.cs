using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "StopNavMeshAgentAction",
    menuName = "State Machines/Enemy Actions/Stop NavMeshAgent")]
public class StopNavMeshAgentActionSO : StateActionSO
{
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;
    [SerializeField] private bool _isStopped = true;
    [SerializeField] private bool _resetPath = true;

    public StateAction.SpecificMoment Moment => _moment;
    public bool IsStopped => _isStopped;
    public bool ResetPath => _resetPath;

    protected override StateAction CreateAction() => new StopNavMeshAgentAction();
}

public class StopNavMeshAgentAction : StateAction
{
    private NavMeshAgent _agent;
    private new StopNavMeshAgentActionSO OriginSO => (StopNavMeshAgentActionSO)base.OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        _agent = stateMachine.GetComponent<NavMeshAgent>();
    }

    public override void OnUpdate()
    {
        if (OriginSO.Moment == SpecificMoment.OnUpdate)
            Apply();
    }

    public override void OnStateEnter()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateEnter)
            Apply();
    }

    public override void OnStateExit()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    private void Apply()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        _agent.isStopped = OriginSO.IsStopped;

        if (OriginSO.ResetPath)
            _agent.ResetPath();
    }
}
