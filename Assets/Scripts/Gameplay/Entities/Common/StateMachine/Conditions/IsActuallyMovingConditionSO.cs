/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines IsActuallyMovingConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "IsActuallyMoving", menuName = "State Machines/Conditions/Is Actually Moving")]
public class IsActuallyMovingConditionSO : StateConditionSO
{
	[SerializeField] private float _treshold = 0.02f;

	// Purpose: Creates the condition owned by this workflow.
	protected override Condition CreateCondition() => new IsActuallyMovingCondition(_treshold);
}

public class IsActuallyMovingCondition : Condition
{
	private float _treshold;
	private CharacterController _characterController;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_characterController = stateMachine.GetComponent<CharacterController>();
	}

	public IsActuallyMovingCondition(float treshold)
	{
		_treshold = treshold;
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement()
	{
		return _characterController.velocity.sqrMagnitude > _treshold * _treshold;
	}
}
