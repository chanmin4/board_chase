/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines ControlWalkingParticlesActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "ControlWalkingParticlesAction", menuName = "State Machines/Actions/Control Walking Particles")]
public class ControlWalkingParticlesActionSO : StateActionSO<ControlWalkingParticlesAction> { }

public class ControlWalkingParticlesAction : StateAction
{
	//Component references
	private PlayerEffectController _dustController;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_dustController = ResolveEffects(stateMachine);
	}

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		//_dustController.EnableWalkParticles();
	}

	// Purpose: Responds to the state exit callback.
	public override void OnStateExit()
	{
		//_dustController.DisableWalkParticles();
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate() { }

	// Purpose: Resolves the effects needed by this component.
	private static PlayerEffectController ResolveEffects(StateMachine stateMachine)
	{
		if (stateMachine == null)
			return null;

		if (stateMachine.TryGetComponent(out PlayerEffectController effects))
			return effects;

		effects = stateMachine.GetComponentInChildren<PlayerEffectController>(true);

		if (effects != null)
			return effects;

		return stateMachine.GetComponentInParent<PlayerEffectController>();
	}
}
