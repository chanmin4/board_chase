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

    public override void Awake(StateMachine stateMachine)
    {
        stateMachine.TryGetComponent(out _knockbackReceiver);
    }

    protected override bool Statement()
    {
        return _knockbackReceiver != null && _knockbackReceiver.IsKnockbackActive;
    }
}
