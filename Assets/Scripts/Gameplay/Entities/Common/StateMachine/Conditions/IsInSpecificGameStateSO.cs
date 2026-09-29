/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines the IsInSpecificGameStateSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is In Particular GameState")]
public class IsInSpecificGameStateSO : StateConditionSO<IsInSpecificGameStateCondition>
{
	[UnityEngine.Serialization.FormerlySerializedAs("_currentGameState")] public GameState gameStateToCheck;
	[UnityEngine.Serialization.FormerlySerializedAs("gameStateManager")] public GameStateSO gameStateSO;

	// Purpose: Creates the condition owned by this workflow.
	protected override Condition CreateCondition() => new IsInSpecificGameStateCondition();
}

public class IsInSpecificGameStateCondition : Condition
{
	private IsInSpecificGameStateSO _originSO => (IsInSpecificGameStateSO)base.OriginSO; // The SO this Condition spawned from

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement()
	{
		return _originSO.gameStateToCheck == _originSO.gameStateSO.CurrentGameState;
	}
}
