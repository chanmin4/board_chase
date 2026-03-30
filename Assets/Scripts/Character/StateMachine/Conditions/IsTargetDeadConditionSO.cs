using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "IsTargetDeadCondition", menuName = "State Machines/Conditions/Is Target Dead Condition")]
public class IsTargetDeadConditionSO : StateConditionSO
{
	protected override Condition CreateCondition() => new IsTargetDeadCondition();
}

public class IsTargetDeadCondition : Condition
{
	private Creature _creatureScript;

	public override void Awake(StateMachine stateMachine)
	{
		_creatureScript = stateMachine.GetComponent<Creature>();
	}

	protected override bool Statement()
	{
		return _creatureScript.currentTarget == null || _creatureScript.currentTarget.IsDead;
	}
}
