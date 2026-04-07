using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is Holding Attack Action")]
public class IsHoldingAttackActionConditionSO : StateConditionSO<IsHoldingAttackActionCondition> { }

public class IsHoldingAttackActionCondition : Condition
{
	//Component references
	private VSplatter_Character _vsplatterScript;

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
	}

	protected override bool Statement()
	{
		Debug.Log($"Attack input: {_vsplatterScript.attackInput}");
		return _vsplatterScript.attackInput;
	}
}
