/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines the HasReceivedEventSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Has Received Event")]
public class HasReceivedEventSO : StateConditionSO<HasReceivedEventCondition>
{
	public VoidEventChannelSO voidEvent;
}

public class HasReceivedEventCondition : Condition
{
	private HasReceivedEventSO _originSO => (HasReceivedEventSO)base.OriginSO; // The SO this Condition spawned from

	private bool _eventTriggered;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_eventTriggered = false;
		_originSO.voidEvent.OnEventRaised += EventReceived;
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement()
	{
		return _eventTriggered;
	}

	// Purpose: Performs the event received workflow for this component.
	private void EventReceived()
	{
		_eventTriggered = true;
	}

	// Purpose: Responds to the state exit callback.
	public override void OnStateExit()
	{
		_eventTriggered = false;
	}
}
