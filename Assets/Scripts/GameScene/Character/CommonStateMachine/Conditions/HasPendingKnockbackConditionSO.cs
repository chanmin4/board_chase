using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "HasPendingKnockbackCondition",
    menuName = "State Machines/Common Conditions/Has Pending Knockback")]
public class HasPendingKnockbackConditionSO : StateConditionSO<HasPendingKnockbackCondition>
{
}

public class HasPendingKnockbackCondition : Condition
{
    private KnockbackReceiver _knockbackReceiver;

    public override void Awake(StateMachine stateMachine)
    {
        stateMachine.TryGetComponent(out _knockbackReceiver);
    }

    protected override bool Statement()
    {
        return _knockbackReceiver != null && _knockbackReceiver.HasPendingRequest;
    }
}
