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
		if (stateMachine == null)
			return;

		if (!stateMachine.TryGetComponent(out _vsplatterScript))
			_vsplatterScript = stateMachine.GetComponentInChildren<VSplatter_Character>(true);

		if (!stateMachine.TryGetComponent(out _characterController))
			_characterController = stateMachine.GetComponentInChildren<CharacterController>(true);
	}

	public override void OnUpdate()
	{
		if (GamePause.IsPaused || Time.timeScale <= 0f)
			return;

		if (_vsplatterScript == null ||
			_characterController == null ||
			!_characterController.enabled)
		{
			return;
		}
//		Debug.Log($"[MoveApply] vec={_vsplatterScript.movementVector}, cc={_characterController.enabled}, pos={_characterController.transform.position}", _characterController);
		//Debug.Log($"grounded={_characterController.isGrounded}, pos={_characterController.transform.position}, moveVec={_vsplatterScript.movementVector}");
		_characterController.Move(_vsplatterScript.movementVector * Time.deltaTime);
	}
}
