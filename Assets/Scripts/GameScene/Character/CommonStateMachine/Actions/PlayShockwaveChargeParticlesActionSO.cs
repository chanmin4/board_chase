using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayShockwaveChargeParticlesAction",
    menuName = "State Machines/Player Actions/Play Shockwave Charge Particles")]
public class PlayShockwaveChargeParticlesActionSO
    : StateActionSO<PlayShockwaveChargeParticlesAction>
{
}

public class PlayShockwaveChargeParticlesAction : StateAction
{
    private PlayerEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = ResolveEffects(stateMachine);
    }

    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.PlayShockwaveChargeParticles();
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
