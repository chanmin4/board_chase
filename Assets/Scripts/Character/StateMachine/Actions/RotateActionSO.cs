using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "RotateAction", menuName = "State Machines/Actions/Rotate")]
public class RotateActionSO : StateActionSO<RotateAction>
{
	[Tooltip("Smoothing for rotating the character to their movement direction")]
	public float turnSmoothTime = 0.2f;
}

public class RotateAction : StateAction
{
	//Component references
	private VSplatter_Character _vsplatterScript;
	private VSplatterAimAction _aimAction;
	private Transform _transform;

	private float _turnSmoothSpeed; //Used by Mathf.SmoothDampAngle to smoothly rotate the character to their movement direction
	private const float ROTATION_TRESHOLD = .02f; // Used to prevent NaN result causing rotation in a non direction
	private RotateActionSO _originSO => (RotateActionSO)base.OriginSO; // The SO this StateAction spawned from

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
		_transform = stateMachine.GetComponent<Transform>();
		stateMachine.TryGetComponent(out _aimAction);

		if (_aimAction == null)
			_aimAction = stateMachine.GetComponentInChildren<VSplatterAimAction>();

		if (_aimAction == null)
			_aimAction = stateMachine.GetComponentInParent<VSplatterAimAction>();
	}
	public override void OnUpdate()
	{
		Vector3 lookDirection = _vsplatterScript.movementVector;
		lookDirection.y = 0f;

		bool shouldAimRotate =
			(_vsplatterScript.attackInput || _vsplatterScript.paintInput) &&
			_aimAction != null &&
			_aimAction.HasAimPoint;

		if (shouldAimRotate)
		{
			lookDirection = _aimAction.AimWorldPoint - _transform.position;
			lookDirection.y = 0f;
		}

		if (lookDirection.sqrMagnitude >= ROTATION_TRESHOLD)
		{
			float targetRotation = Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg;

			_transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(
				_transform.eulerAngles.y,
				targetRotation,
				ref _turnSmoothSpeed,
				_originSO.turnSmoothTime);
		}
	}
}
