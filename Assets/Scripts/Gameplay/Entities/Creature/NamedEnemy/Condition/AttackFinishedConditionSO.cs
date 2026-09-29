/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Condition
 * Primary responsibility: Defines AttackFinishedConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "AttackFinishedCondition",
    menuName = "State Machines/Named Enemy Conditions/Attack Finished")]
public class AttackFinishedConditionSO : StateConditionSO<AttackFinishedCondition>
{
}

public class AttackFinishedCondition : Condition
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
        return _blackboard != null && _blackboard.attackFinished;
    }
}
