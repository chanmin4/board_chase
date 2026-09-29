/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines IsUsingDashConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is Using Dash")]
public class IsUsingDashConditionSO : StateConditionSO<IsUsingDashCondition> { }

public class IsUsingDashCondition: Condition
{
	private ShooterDashController _dashController;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		if (!stateMachine.TryGetComponent(out _dashController))
			_dashController = stateMachine.GetComponentInChildren<ShooterDashController>(true);
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement() => _dashController != null && _dashController.IsDashing;
}
