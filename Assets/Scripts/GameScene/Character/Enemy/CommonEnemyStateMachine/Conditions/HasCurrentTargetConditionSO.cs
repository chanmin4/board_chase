using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "HasCurrentTargetCondition",
    menuName = "State Machines/Enemy Conditions/Has Current Target")]
public class HasCurrentTargetConditionSO : StateConditionSO<HasCurrentTargetCondition>
{
}

public class HasCurrentTargetCondition : Condition
{
    private Enemy _enemy;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
    }

    protected override bool Statement()
    {
        return _enemy != null &&
               _enemy.currentTarget != null &&
               !_enemy.currentTarget.IsDead;
    }
}
