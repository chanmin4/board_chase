using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Has Hit the Head")]
public class HasHitHeadConditionSO : StateConditionSO<HasHitHeadCondition> { }

public class HasHitHeadCondition : Condition
{
	//Component references
	private VSplatter_Character _vsplatterScript;
	private CharacterController _characterController;
	private Transform _transform;

	public override void Awake(StateMachine stateMachine)
	{
		_transform = stateMachine.GetComponent<Transform>();
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
		_characterController = stateMachine.GetComponent<CharacterController>();
	}

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
