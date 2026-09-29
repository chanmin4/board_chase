/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines GroundGravityActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */


using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "GroundGravity", menuName = "State Machines/Actions/Ground Gravity")]
public class GroundGravityActionSO : StateActionSO<GroundGravityAction>
{
	[Tooltip("Vertical movement pulling down the player to keep it anchored to the ground.")]
	public float verticalPull = -5f;
}

public class GroundGravityAction : StateAction
{
	//Component references
	private PlayerCharacter _vsplatterScript;

	private GroundGravityActionSO _originSO => (GroundGravityActionSO)base.OriginSO; // The SO this StateAction spawned from

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		if (stateMachine == null)
			return;

		if (!stateMachine.TryGetComponent(out _vsplatterScript))
			_vsplatterScript = stateMachine.GetComponentInChildren<PlayerCharacter>(true);
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate()
	{
		if (GamePause.IsPaused || Time.timeScale <= 0f)
			return;

		if (_vsplatterScript == null)
			return;

		_vsplatterScript.movementVector.y = _originSO.verticalPull;
	}
	
}
