/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines AlwaysFalseConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

//[CreateAssetMenu(menuName = "State Machines/Conditions/Always False")]
public class AlwaysFalseConditionSO : StateConditionSO<AlwaysFalseCondition> { }

public class AlwaysFalseCondition : Condition
{
	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement() => false;
}
