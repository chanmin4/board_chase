using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

/// <summary>
/// An Action to clear a <see cref="Protagonist.movementVector"/> at the <see cref="StateAction.SpecificMoment"/> <see cref="StopMovementActionSO.Moment"/>
/// </summary>
[CreateAssetMenu(fileName = "StopMovementAction", menuName = "State Machines/Actions/Stop Movement")]
public class StopMovementActionSO : StateActionSO
{
	[SerializeField] private StateAction.SpecificMoment _moment = default;
	public StateAction.SpecificMoment Moment => _moment;

	protected override StateAction CreateAction() => new StopMovement();
}

public class StopMovement : StateAction
{
	private VSplatter_Character _vsplatter;
	private new StopMovementActionSO OriginSO => (StopMovementActionSO)base.OriginSO;

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatter = stateMachine.GetComponent<VSplatter_Character>();
	}

	public override void OnUpdate()
	{
		if (OriginSO.Moment == SpecificMoment.OnUpdate)
			_vsplatter.movementVector = Vector3.zero;
	}

	public override void OnStateEnter()
	{
		if (OriginSO.Moment == SpecificMoment.OnStateEnter)
			_vsplatter.movementVector = Vector3.zero;
	}

	public override void OnStateExit()
	{
		if (OriginSO.Moment == SpecificMoment.OnStateExit)
			_vsplatter.movementVector = Vector3.zero;
	}
}
