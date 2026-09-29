/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Condition
 * Primary responsibility: Defines HasSelectedAttackConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "HasSelectedAttackCondition",
    menuName = "State Machines/Named Enemy Conditions/Has Selected Attack")]
public class HasSelectedAttackConditionSO : StateConditionSO<HasSelectedAttackCondition>
{
}

public class HasSelectedAttackCondition : Condition
{
    private NamedEnemyBlackboard _blackboard;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _blackboard = stateMachine.GetComponent<NamedEnemyBlackboard>();
    }

    // Purpose: Performs the statement workflow for this component.
    protected override bool Statement()
    {
        return _blackboard != null && _blackboard.HasSelectedAttack;
    }
}
