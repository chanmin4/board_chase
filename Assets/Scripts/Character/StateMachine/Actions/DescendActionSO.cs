using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Actions/Descend")]
public class DescendActionSO : StateActionSO<DescendAction> { }

public class DescendAction : StateAction
{
	//Component references
	private VSplatter_Character _vsplatterScript;

	private float _verticalMovement;

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
	}

	public override void OnStateEnter()
	{
		_verticalMovement = _vsplatterScript.movementVector.y;

		//Prevents a double jump if the player keeps holding the jump button
		//Basically it "consumes" the input
		_vsplatterScript.DashInput = false;
	}

	public override void OnUpdate()
	{
		//Note that deltaTime is used even though it's going to be used in ApplyMovementVectorAction, this is because it represents an acceleration, not a speed
		_verticalMovement += Physics.gravity.y * VSplatter_Character.GRAVITY_MULTIPLIER * Time.deltaTime;
		//Note that even if it's added, the above value is negative due to Physics.gravity.y

		//Cap the maximum so the player doesn't reach incredible speeds when freefalling from high positions
		_verticalMovement = Mathf.Clamp(_verticalMovement, VSplatter_Character.MAX_FALL_SPEED, VSplatter_Character.MAX_RISE_SPEED);

		_vsplatterScript.movementVector.y = _verticalMovement;
	}
}
