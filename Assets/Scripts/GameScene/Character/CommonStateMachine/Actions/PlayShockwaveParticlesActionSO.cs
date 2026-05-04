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
        _effects = stateMachine.GetComponent<PlayerEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.PlayShockwaveParticles();
    }

    public override void OnUpdate()
    {
    }
}
