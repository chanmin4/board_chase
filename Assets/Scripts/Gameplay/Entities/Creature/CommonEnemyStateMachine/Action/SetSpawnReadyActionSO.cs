/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > Action
 * Primary responsibility: Defines SetSpawnReadyActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SetSpawnReadyAction",
    menuName = "State Machines/Enemy Actions/Set Spawn Ready")]
public class SetSpawnReadyActionSO : StateActionSO
{
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;
    [SerializeField] private bool _isSpawnReady = true;

    public StateAction.SpecificMoment Moment => _moment;
    public bool IsSpawnReady => _isSpawnReady;

    // Purpose: Creates the action owned by this workflow.
    protected override StateAction CreateAction() => new SetSpawnReadyAction();
}

public class SetSpawnReadyAction : StateAction
{
    private Enemy _enemy;
    private new SetSpawnReadyActionSO OriginSO => (SetSpawnReadyActionSO)base.OriginSO;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        if (!stateMachine.TryGetComponent(out _enemy))
            _enemy = stateMachine.GetComponentInChildren<Enemy>(true);
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
        if (_enemy != null)
            _enemy.SetSpawnReady(OriginSO.IsSpawnReady);
    }
}
