/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines IsKnockbackActiveConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "IsKnockbackActiveCondition",
    menuName = "State Machines/Common Conditions/Is Knockback Active")]
public class IsKnockbackActiveConditionSO : StateConditionSO<IsKnockbackActiveCondition>
{
}

public class IsKnockbackActiveCondition : Condition
{
    private KnockbackReceiver _knockbackReceiver;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        stateMachine.TryGetComponent(out _knockbackReceiver);
    }

    // Purpose: Performs the statement workflow for this component.
    protected override bool Statement()
    {
        return _knockbackReceiver != null && _knockbackReceiver.IsKnockbackActive;
    }
}
