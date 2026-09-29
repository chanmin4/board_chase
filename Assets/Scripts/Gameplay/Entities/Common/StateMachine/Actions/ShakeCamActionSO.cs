/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines ShakeCamActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "ShakeCamAction", menuName = "State Machines/Actions/Shake Cam Action")]
public class ShakeCamActionSO : StateActionSO
{
	public VoidEventChannelSO camShakeEvent;
	// Purpose: Creates the action owned by this workflow.
	protected override StateAction CreateAction() => new ShakeCamAction();
}

public class ShakeCamAction : StateAction
{
	protected new ShakeCamActionSO OriginSO => (ShakeCamActionSO)base.OriginSO;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
	}
	
	// Purpose: Responds to the update callback.
	public override void OnUpdate()
	{
	}
	
	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		OriginSO.camShakeEvent.RaiseEvent();
	}
	
	// Purpose: Responds to the state exit callback.
	public override void OnStateExit()
	{
	}
}
