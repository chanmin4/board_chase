using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "SlideAction", menuName = "State Machines/Actions/Slide")]
public class SlideActionSO : StateActionSO<SlideAction> { }

public class SlideAction : StateAction
{
	private VSplatter_Character _vsplatter;

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatter = stateMachine.GetComponent<VSplatter_Character>();
	}

	public override void OnUpdate()
	{
		float speed = -Physics.gravity.y * VSplatter_Character.GRAVITY_MULTIPLIER * .4f;
		Vector3 hitNormal = _vsplatter.lastHit.normal;
		Vector3 slideDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
		Vector3.OrthoNormalize(ref hitNormal, ref slideDirection);

		//Trick below has been commented because it was pushing the character "into" the ground much too often,
		//producing a collision, which would result in the character being stuck while in the Sliding state

		//Vector3 slidingMovement = _protagonist.movementVector;
		//// Cheap way to avoid overshooting the character, which causes it to move away from the slope
		//if (Mathf.Sign(slideDirection.x) == Mathf.Sign(slidingMovement.x))
		//	slideDirection.x *= 0.5f;
		//if (Mathf.Sign(slideDirection.z) == Mathf.Sign(slidingMovement.z))
		//	slideDirection.z *= 0.5f;

		//slidingMovement += slideDirection * speed;

		_vsplatter.movementVector = slideDirection * speed;
	}
}
