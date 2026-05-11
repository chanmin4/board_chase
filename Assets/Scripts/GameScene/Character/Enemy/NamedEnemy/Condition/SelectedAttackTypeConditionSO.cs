using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SelectedAttackTypeCondition",
    menuName = "State Machines/Named Enemy Conditions/Selected Attack Type")]
public class SelectedAttackTypeConditionSO : StateConditionSO<SelectedAttackTypeCondition>
{
    [Tooltip("Transition is true when blackboard.selectedAttack equals this value.")]
    public NamedEnemyAttackType expectedAttack = NamedEnemyAttackType.Bite;
}

public class SelectedAttackTypeCondition : Condition
{
    private NamedEnemyBlackboard _blackboard;
    private SelectedAttackTypeConditionSO _config;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (SelectedAttackTypeConditionSO)OriginSO;
        _blackboard = stateMachine.GetComponent<NamedEnemyBlackboard>();
    }

    protected override bool Statement()
    {
        return _blackboard != null &&
               _blackboard.selectedAttack == _config.expectedAttack;
    }
}
