/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines the ClearInputCache_OnEnterSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;
//interaction 전용 
[CreateAssetMenu(fileName = "ClearInputCache_OnEnter", menuName = "State Machines/Actions/Clear Input Cache On Enter")]
public class ClearInputCache_OnEnterSO : StateActionSO
{
	// Purpose: Creates the action owned by this workflow.
	protected override StateAction CreateAction() => new ClearInputCache_OnEnter();
}

public class ClearInputCache_OnEnter : StateAction
{
	private PlayerCharacter _vsplatter;
	private InteractionManager _interactionManager;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_vsplatter = stateMachine.GetComponent<PlayerCharacter>();
		_interactionManager = stateMachine.GetComponentInChildren<InteractionManager>();
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate()
	{
	}

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		_interactionManager.currentInteractionType = InteractionType.None;
	}
}
