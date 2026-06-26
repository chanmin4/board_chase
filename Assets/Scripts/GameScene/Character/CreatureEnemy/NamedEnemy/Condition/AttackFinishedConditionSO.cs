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

    public override void Awake(StateMachine stateMachine)
    {
        _blackboard = stateMachine.GetComponent<NamedEnemyBlackboard>();
    }

    protected override bool Statement()
    {
        return _blackboard != null && _blackboard.attackFinished;
    }
}
