/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines the IsPickingUpSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is Picking Up")]
public class IsPickingUpSO : StateConditionSO<IsPickingUpCondition> { }

public class IsPickingUpCondition : Condition
{
	//Component references
	private InteractionManager _interactScript;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_interactScript = stateMachine.GetComponent<InteractionManager>();
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement()
	{
		if (_interactScript.currentInteractionType == InteractionType.PickUp)
		{
			// Consume it
			_interactScript.currentInteractionType = InteractionType.None;
			return true;
		}
		else
		{
			return false;
		}
	}
}
