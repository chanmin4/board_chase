using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;
/// <summary>
/// "선택한 타겟이 죽었는지
/// </summary>
[CreateAssetMenu(fileName = "IsTargetDeadCondition", menuName = "State Machines/Conditions/Is Target Dead Condition")]
public class IsTargetDeadConditionSO : StateConditionSO
{
	protected override Condition CreateCondition() => new IsTargetDeadCondition();
}

public class IsTargetDeadCondition : Condition
{
	private Enemy _enemy;

	public override void Awake(StateMachine stateMachine)
	{
		_enemy = stateMachine.GetComponent<Enemy>();
	}

	protected override bool Statement()
	{
		return _enemy.currentTarget == null || _enemy.currentTarget.IsDead;
	}
}
