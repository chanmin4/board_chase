using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "StopShockwaveChargeParticlesAction",
    menuName = "State Machines/Player Actions/Stop Shockwave Charge Particles")]
public class StopShockwaveChargeParticlesActionSO
    : StateActionSO<StopShockwaveChargeParticlesAction>
{
}

public class StopShockwaveChargeParticlesAction : StateAction
{
    private PlayerEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = ResolveEffects(stateMachine);
    }

    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.StopShockwaveChargeParticles();
    }

    public override void OnUpdate()
    {
    }

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
