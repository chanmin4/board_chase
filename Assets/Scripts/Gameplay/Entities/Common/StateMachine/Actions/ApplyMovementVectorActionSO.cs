/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines ApplyMovementVectorActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "ApplyMovementVector", menuName = "State Machines/Actions/Apply Movement Vector")]
public class ApplyMovementVectorActionSO : StateActionSO<ApplyMovementVectorAction> { }

public class ApplyMovementVectorAction : StateAction
{
	//Component references
	private PlayerCharacter _vsplatterScript;
	private CharacterController _characterController;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		if (stateMachine == null)
			return;

		if (!stateMachine.TryGetComponent(out _vsplatterScript))
			_vsplatterScript = stateMachine.GetComponentInChildren<PlayerCharacter>(true);

		if (!stateMachine.TryGetComponent(out _characterController))
			_characterController = stateMachine.GetComponentInChildren<CharacterController>(true);
	}

	// Purpose: Responds to the update callback.
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
