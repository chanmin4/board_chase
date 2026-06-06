using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayShockwaveParticlesAction",
    menuName = "State Machines/Player Actions/Play Shockwave Particles")]
public class PlayShockwaveParticlesActionSO
    : StateActionSO<PlayShockwaveParticlesAction>
{
}

public class PlayShockwaveParticlesAction : StateAction
{
    private PlayerEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = ResolveEffects(stateMachine);
    }

    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.PlayShockwaveParticles();
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
