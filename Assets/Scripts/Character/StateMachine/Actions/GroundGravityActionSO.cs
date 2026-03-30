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
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
	}

	public override void OnUpdate()
	{
		_vsplatterScript.movementVector.y = _originSO.verticalPull;
	}
}
