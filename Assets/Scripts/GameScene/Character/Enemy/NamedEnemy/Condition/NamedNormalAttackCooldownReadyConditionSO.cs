using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NamedNormalAttackCooldownReadyCondition",
    menuName = "State Machines/Named Enemy Conditions/Normal Attack Cooldown Ready")]
public class NamedNormalAttackCooldownReadyConditionSO
    : StateConditionSO<NamedNormalAttackCooldownReadyCondition>
{
}

public class NamedNormalAttackCooldownReadyCondition : Condition
{
    private NamedEnemyBlackboard _blackboard;

    public override void Awake(StateMachine stateMachine)
    {
        _blackboard = stateMachine.GetComponent<NamedEnemyBlackboard>();
    }

    protected override bool Statement()
    {
        return _blackboard != null &&
               _blackboard.IsNormalAttackCooldownReady;
    }
}
