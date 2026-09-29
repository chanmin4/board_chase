/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > CommonNormalEnemyStateMachine > Conditions
 * Primary responsibility: Defines HasInfectionTargetConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "HasInfectionTargetCondition", menuName = "State Machines/Enemy Conditions/Has Infection Target")]
public class HasInfectionTargetConditionSO : StateConditionSO<HasInfectionTargetCondition>
{
}

public class HasInfectionTargetCondition : Condition
{
	private Enemy _enemy;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_enemy = stateMachine.GetComponent<Enemy>();
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement()
	{
		return _enemy != null && _enemy.HasInfectionTarget;
	}
}
