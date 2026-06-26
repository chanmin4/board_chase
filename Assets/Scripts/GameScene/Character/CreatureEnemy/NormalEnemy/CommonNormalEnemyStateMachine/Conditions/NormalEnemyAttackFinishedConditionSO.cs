using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NormalEnemyAttackFinishedCondition",
    menuName = "State Machines/Enemy Conditions/Normal Enemy Attack Finished")]
public class NormalEnemyAttackFinishedConditionSO : StateConditionSO<NormalEnemyAttackFinishedCondition>
{
}

public class NormalEnemyAttackFinishedCondition : Condition
{
    private EnemyAttackExecutorController _runtimeController;

    public override void Awake(StateMachine stateMachine)
    {
        _runtimeController = stateMachine.GetOrAddComponent<EnemyAttackExecutorController>();
    }

    protected override bool Statement()
    {
        return _runtimeController == null || _runtimeController.AttackFinished;
    }
}
