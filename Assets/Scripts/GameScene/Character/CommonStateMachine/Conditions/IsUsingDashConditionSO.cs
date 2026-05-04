using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is Using Dash")]
public class IsUsingDashConditionSO : StateConditionSO<IsUsingDashCondition> { }

public class IsUsingDashCondition: Condition
{
	private VSplatterDashController _dashController;

	public override void Awake(StateMachine stateMachine)
	{
		_dashController = stateMachine.GetOrAddComponent<VSplatterDashController>();
	}

	protected override bool Statement() => _dashController != null && _dashController.IsDashing;
}
