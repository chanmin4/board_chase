/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines HasHitHeadConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Has Hit the Head")]
public class HasHitHeadConditionSO : StateConditionSO<HasHitHeadCondition> { }

public class HasHitHeadCondition : Condition
{
	//Component references
	private PlayerCharacter _vsplatterScript;
	private CharacterController _characterController;
	private Transform _transform;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_transform = stateMachine.GetComponent<Transform>();
		_vsplatterScript = stateMachine.GetComponent<PlayerCharacter>();
		_characterController = stateMachine.GetComponent<CharacterController>();
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement()
	{
		bool isMovingUpwards = _vsplatterScript.movementVector.y > 0f;
		if (isMovingUpwards)
		{
			if(_characterController.collisionFlags == CollisionFlags.Above)
			{
				_vsplatterScript.DashInput = false;
				_vsplatterScript.movementVector.y = 0f;

				return true;
			}
		}

		return false;
	}
}
