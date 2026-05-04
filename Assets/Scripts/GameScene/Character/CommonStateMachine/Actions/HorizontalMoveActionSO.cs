using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "HorizontalMove", menuName = "State Machines/Actions/Horizontal Move")]
public class HorizontalMoveActionSO : StateActionSO<HorizontalMoveAction>
{
	[Tooltip("Horizontal XZ plane speed multiplier")]
	public float speed = 8f;
}

public class HorizontalMoveAction : StateAction
{
	//Component references
	private VSplatter_Character _vsplatterScript;
	private HorizontalMoveActionSO _originSO => (HorizontalMoveActionSO)base.OriginSO; // The SO this StateAction spawned from

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
	}

	public override void OnUpdate()
	{
		//delta.Time is used when the movement is applied (ApplyMovementVectorAction)
		_vsplatterScript.movementVector.x = _vsplatterScript.movementInput.x * _originSO.speed;
		_vsplatterScript.movementVector.z = _vsplatterScript.movementInput.z * _originSO.speed;
	}
}
