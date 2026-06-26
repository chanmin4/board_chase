using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "CanEnterPatternCondition",
    menuName = "State Machines/Named Enemy Conditions/Can Enter Pattern")]
public class CanEnterPatternConditionSO : StateConditionSO<CanEnterPatternCondition>
{
}

public class CanEnterPatternCondition : Condition
{
    private NamedEnemyBlackboard _blackboard;

    public override void Awake(StateMachine stateMachine)
    {
        _blackboard = stateMachine.GetComponent<NamedEnemyBlackboard>();
    }

    protected override bool Statement()
    {
        return _blackboard != null && _blackboard.canEnterPattern;
    }
}
