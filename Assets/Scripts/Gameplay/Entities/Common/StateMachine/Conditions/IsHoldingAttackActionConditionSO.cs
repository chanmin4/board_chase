/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines IsHoldingAttackActionConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is Holding Attack Action")]
public class IsHoldingAttackActionConditionSO : StateConditionSO<IsHoldingAttackActionCondition> { }

public class IsHoldingAttackActionCondition : Condition
{
	//Component references
	private PlayerCharacter _vsplatterScript;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<PlayerCharacter>();
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement()
	{
		Debug.Log($"Attack input: {_vsplatterScript.shootInput}");
		return _vsplatterScript.shootInput;
	}
}
