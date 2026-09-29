/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > CommonNormalEnemyStateMachine > Conditions
 * Primary responsibility: Defines NormalEnemyAttackFinishedConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _runtimeController = stateMachine.GetOrAddComponent<EnemyAttackExecutorController>();
    }

    // Purpose: Performs the statement workflow for this component.
    protected override bool Statement()
    {
        return _runtimeController == null || _runtimeController.AttackFinished;
    }
}
