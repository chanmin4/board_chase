using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is Holding Attack Action")]
public class IsHoldingAttackActionConditionSO : StateConditionSO<IsHoldingAttackActionCondition> { }

public class IsHoldingAttackActionCondition : Condition
{
	//Component references
	private VSplatter_Character _protagonistScript;

	public override void Awake(StateMachine stateMachine)
	{
		_protagonistScript = stateMachine.GetComponent<VSplatter_Character>();
	}

	protected override bool Statement()
	{
		return _protagonistScript.attackInput;
	}
}
