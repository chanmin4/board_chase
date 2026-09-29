/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Condition
 * Primary responsibility: Defines SelectedNamedAttackConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SelectedNamedAttackCondition",
    menuName = "State Machines/Named Enemy Conditions/Selected Named Attack")]
public class SelectedNamedAttackConditionSO : StateConditionSO<SelectedNamedAttackCondition>
{
    [SerializeField] private EnemyAttackConfigSO _expectedAttack;

    public EnemyAttackConfigSO ExpectedAttack => _expectedAttack;
}

public class SelectedNamedAttackCondition : Condition
{
    private SelectedNamedAttackConditionSO _origin;
    private NamedEnemyBlackboard _blackboard;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (SelectedNamedAttackConditionSO)OriginSO;
        _blackboard = stateMachine.GetComponentInParent<NamedEnemyBlackboard>();
    }

    // Purpose: Performs the statement workflow for this component.
    protected override bool Statement()
    {
        if (_blackboard == null || _origin.ExpectedAttack == null)
            return false;

        return _blackboard.SelectedAttackIs(_origin.ExpectedAttack);
    }
}
