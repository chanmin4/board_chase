using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is Using Dash")]
public class IsUsingDashConditionSO : StateConditionSO<IsUsingDashCondition> { }

public class IsUsingDashCondition: Condition
{
	//Component references
	private VSplatter_Character _vsplatterScript;

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
	}

	protected override bool Statement() => _vsplatterScript.DashInput;
}
