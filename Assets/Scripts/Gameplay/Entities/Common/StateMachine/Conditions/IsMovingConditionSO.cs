/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines IsMovingConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Started Moving")]
public class IsMovingConditionSO : StateConditionSO<IsMovingCondition>
{
	public float treshold = 0.02f;
}

public class IsMovingCondition : Condition
{
	private PlayerCharacter _vsplatterScript;
	private IsMovingConditionSO _originSO => (IsMovingConditionSO)base.OriginSO; // The SO this Condition spawned from

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<PlayerCharacter>();
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement()
	{
		Vector3 movementVector = _vsplatterScript.movementInput;
		movementVector.y = 0f;
		return movementVector.sqrMagnitude > _originSO.treshold;
	}
}
