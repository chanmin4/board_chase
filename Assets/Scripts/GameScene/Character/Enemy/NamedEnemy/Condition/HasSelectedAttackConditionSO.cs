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

    public override void Awake(StateMachine stateMachine)
    {
        _blackboard = stateMachine.GetComponent<NamedEnemyBlackboard>();
    }

    protected override bool Statement()
    {
        return _blackboard != null && _blackboard.HasSelectedAttack;
    }
}
