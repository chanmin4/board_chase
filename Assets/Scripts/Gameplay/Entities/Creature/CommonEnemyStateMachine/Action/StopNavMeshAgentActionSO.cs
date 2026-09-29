/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > Action
 * Primary responsibility: Defines StopNavMeshAgentActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Creates the action owned by this workflow.
    protected override StateAction CreateAction() => new StopNavMeshAgentAction();
}

public class StopNavMeshAgentAction : StateAction
{
    private NavMeshAgent _agent;
    private new StopNavMeshAgentActionSO OriginSO => (StopNavMeshAgentActionSO)base.OriginSO;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _agent = ResolveInOwnerHierarchy<NavMeshAgent>(stateMachine);
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (OriginSO.Moment == SpecificMoment.OnUpdate)
            Apply();
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateEnter)
            Apply();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    // Purpose: Applies the requested apply change.
    private void Apply()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        _agent.isStopped = OriginSO.IsStopped;

        if (OriginSO.ResetPath)
            _agent.ResetPath();
    }

    private static T ResolveInOwnerHierarchy<T>(StateMachine stateMachine) where T : Component
    {
        if (stateMachine.TryGetComponent(out T component))
            return component;

        component = stateMachine.GetComponentInChildren<T>(true);
        if (component != null)
            return component;

        Transform current = stateMachine.transform.parent;

        while (current != null)
        {
            component = current.GetComponentInChildren<T>(true);
            if (component != null)
                return component;

            current = current.parent;
        }

        return null;
    }
}
