
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
	private VSplatter_Character _vsplatterScript;

	private GroundGravityActionSO _originSO => (GroundGravityActionSO)base.OriginSO; // The SO this StateAction spawned from

	public override void Awake(StateMachine stateMachine)
	{
		if (stateMachine == null)
			return;

		if (!stateMachine.TryGetComponent(out _vsplatterScript))
			_vsplatterScript = stateMachine.GetComponentInChildren<VSplatter_Character>(true);
	}

	public override void OnUpdate()
	{
		if (GamePause.IsPaused || Time.timeScale <= 0f)
			return;

		if (_vsplatterScript == null)
			return;

		_vsplatterScript.movementVector.y = _originSO.verticalPull;
	}
	
}
