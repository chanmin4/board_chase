/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines RaiseVoidEventActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "RaiseVoidEventAction", menuName = "State Machines/Actions/Raise Void Event Action")]
public class RaiseVoidEventActionSO : StateActionSO
{
	public VoidEventChannelSO voidEvent;

	// Purpose: Creates the action owned by this workflow.
	protected override StateAction CreateAction() => new RaiseVoidEventAction();
}

public class RaiseVoidEventAction : StateAction
{
	private VoidEventChannelSO _voidEvent;
	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_voidEvent = ((RaiseVoidEventActionSO)OriginSO).voidEvent;
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate()
	{

	}

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		_voidEvent.RaiseEvent();
	}

	// Purpose: Responds to the state exit callback.
	public override void OnStateExit()
	{

	}
}
