/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines the ResetHealthSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "ResetHealth", menuName = "State Machines/Actions/Reset Health")]
public class ResetHealthSO : StateActionSO
{
	// Purpose: Creates the action owned by this workflow.
	protected override StateAction CreateAction() => new ResetHealth();
}

public class ResetHealth : StateAction
{
	private Damageable _damageableEntity;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_damageableEntity = stateMachine.GetComponent<Damageable>();
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate()
	{

	}

	// Purpose: Responds to the state exit callback.
	public override void OnStateExit()
	{
		_damageableEntity.Revive();
	}
}
