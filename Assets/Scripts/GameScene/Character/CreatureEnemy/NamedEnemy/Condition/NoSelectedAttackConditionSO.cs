using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NoSelectedAttackCondition",
    menuName = "State Machines/Named Enemy Conditions/No Selected Attack")]
public class NoSelectedAttackConditionSO : StateConditionSO<NoSelectedAttackCondition>
{
}

public class NoSelectedAttackCondition : Condition
{
    private NamedEnemyBlackboard _blackboard;

    public override void Awake(StateMachine stateMachine)
    {
        _blackboard = stateMachine.GetComponent<NamedEnemyBlackboard>();
    }

    protected override bool Statement()
    {
        return _blackboard == null || !_blackboard.HasSelectedAttack;
    }
}
