/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > Action
 * Primary responsibility: Defines ClearEnemyRuntimeFlagsActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ClearEnemyRuntimeFlagsAction",
    menuName = "State Machines/Enemy Actions/Clear Enemy Runtime Flags")]
public class ClearEnemyRuntimeFlagsActionSO : StateActionSO
{
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;

    [Header("Flags")]
    [SerializeField] private bool _clearInfectionTarget;
    [SerializeField] private bool _clearSearchSucceeded;
    [SerializeField] private bool _clearInfectionTargetValid;
    [SerializeField] private bool _clearKnockbackRequest;

    public StateAction.SpecificMoment Moment => _moment;
    public bool ClearInfectionTarget => _clearInfectionTarget;
    public bool ClearSearchSucceeded => _clearSearchSucceeded;
    public bool ClearInfectionTargetValid => _clearInfectionTargetValid;
    public bool ClearKnockbackRequest => _clearKnockbackRequest;

    // Purpose: Creates the action owned by this workflow.
    protected override StateAction CreateAction() => new ClearEnemyRuntimeFlagsAction();
}

public class ClearEnemyRuntimeFlagsAction : StateAction
{
    private Enemy _enemy;
    private new ClearEnemyRuntimeFlagsActionSO OriginSO => (ClearEnemyRuntimeFlagsActionSO)base.OriginSO;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
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
        if (_enemy == null)
            return;

        if (OriginSO.ClearInfectionTarget)
            _enemy.ClearInfectionTarget();

        if (OriginSO.ClearSearchSucceeded)
            _enemy.SetSearchSucceeded(false);

        if (OriginSO.ClearInfectionTargetValid)
            _enemy.SetInfectionTargetvalid(false);

        if (OriginSO.ClearKnockbackRequest)
            _enemy.ClearKnockbackRequest();
    }
}
