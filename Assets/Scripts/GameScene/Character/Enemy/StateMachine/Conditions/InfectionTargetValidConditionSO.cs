using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "InfectionTargetValidCondition", menuName = "State Machines/Enemy Conditions/Infection Target Valid")]
public class InfectionTargetValidConditionSO : StateConditionSO<InfectionTargetValidCondition>
{
}

public class InfectionTargetValidCondition : Condition
{
	private Enemy _enemy;

	public override void Awake(StateMachine stateMachine)
	{
		_enemy = stateMachine.GetComponent<Enemy>();
	}

	protected override bool Statement()
	{
		return _enemy != null && _enemy.InfectionTargetValid;
	}
}
