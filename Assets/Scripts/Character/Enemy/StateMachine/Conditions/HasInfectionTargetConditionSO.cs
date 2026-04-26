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

	public override void Awake(StateMachine stateMachine)
	{
		_enemy = stateMachine.GetComponent<Enemy>();
	}

	protected override bool Statement()
	{
		return _enemy != null && _enemy.HasInfectionTarget;
	}
}
