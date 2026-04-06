using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "ApplyMovementVector", menuName = "State Machines/Actions/Apply Movement Vector")]
public class ApplyMovementVectorActionSO : StateActionSO<ApplyMovementVectorAction> { }

public class ApplyMovementVectorAction : StateAction
{
	//Component references
	private VSplatter_Character _vsplatterScript;
	private CharacterController _characterController;

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
		_characterController = stateMachine.GetComponent<CharacterController>();
	}

	public override void OnUpdate()
	{
		Debug.Log($"grounded={_characterController.isGrounded}, pos={_characterController.transform.position}, moveVec={_vsplatterScript.movementVector}");
		_characterController.Move(_vsplatterScript.movementVector * Time.deltaTime);
		_vsplatterScript.movementVector = _characterController.velocity;
	}
}
