using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "ControlWalkingParticlesAction", menuName = "State Machines/Actions/Control Walking Particles")]
public class ControlWalkingParticlesActionSO : StateActionSO<ControlWalkingParticlesAction> { }

public class ControlWalkingParticlesAction : StateAction
{
	//Component references
	private PlayerEffectController _dustController;

	public override void Awake(StateMachine stateMachine)
	{
		_dustController = ResolveEffects(stateMachine);
	}

	public override void OnStateEnter()
	{
		//_dustController.EnableWalkParticles();
	}

	public override void OnStateExit()
	{
		//_dustController.DisableWalkParticles();
	}

	public override void OnUpdate() { }

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
