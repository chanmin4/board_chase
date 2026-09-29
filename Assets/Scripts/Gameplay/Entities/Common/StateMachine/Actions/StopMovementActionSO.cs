/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines StopMovementActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

	// Purpose: Creates the action owned by this workflow.
	protected override StateAction CreateAction() => new StopMovement();
}

public class StopMovement : StateAction
{
	private PlayerCharacter _vsplatter;
	private new StopMovementActionSO OriginSO => (StopMovementActionSO)base.OriginSO;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_vsplatter = stateMachine.GetComponent<PlayerCharacter>();
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate()
	{
		if (OriginSO.Moment == SpecificMoment.OnUpdate)
			_vsplatter.movementVector = Vector3.zero;
	}

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		if (OriginSO.Moment == SpecificMoment.OnStateEnter)
			_vsplatter.movementVector = Vector3.zero;
	}

	// Purpose: Responds to the state exit callback.
	public override void OnStateExit()
	{
		if (OriginSO.Moment == SpecificMoment.OnStateExit)
			_vsplatter.movementVector = Vector3.zero;
	}
}
