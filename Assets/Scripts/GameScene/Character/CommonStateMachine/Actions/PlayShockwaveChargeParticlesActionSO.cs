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
        _effects = stateMachine.GetComponent<PlayerEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.PlayShockwaveChargeParticles();
    }

    public override void OnUpdate()
    {
    }
}
