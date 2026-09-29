/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines AscendActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "Ascend", menuName = "State Machines/Actions/Ascend")]
public class AscendActionSO : StateActionSO<AscendAction>
{
	[Tooltip("The initial upwards push when pressing jump. This is injected into verticalMovement, and gradually cancelled by gravity")]
	public float initialJumpForce = 6f;
}

public class AscendAction : StateAction
{
	//Component references
	private PlayerCharacter _vsplatterScript;

	private float _verticalMovement;
	private float _gravityContributionMultiplier;
	private AscendActionSO _originSO => (AscendActionSO)base.OriginSO; // The SO this StateAction spawned from

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<PlayerCharacter>();
	}

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		_verticalMovement = _originSO.initialJumpForce;
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate()
	{
		if (GamePause.IsPaused || Time.timeScale <= 0f)
			return;

		if (_vsplatterScript == null)
			return;

		_gravityContributionMultiplier += PlayerCharacter.GRAVITY_COMEBACK_MULTIPLIER;
		_gravityContributionMultiplier *= PlayerCharacter.GRAVITY_DIVIDER; //Reduce the gravity effect

		//Note that deltaTime is used even though it's going to be used in ApplyMovementVectorAction, this is because it represents an acceleration, not a speed
		_verticalMovement += Physics.gravity.y * PlayerCharacter.GRAVITY_MULTIPLIER * _gravityContributionMultiplier * Time.deltaTime;
		//Note that even if it's added, the above value is negative due to Physics.gravity.y

		_vsplatterScript.movementVector.y = _verticalMovement;
	}
}
