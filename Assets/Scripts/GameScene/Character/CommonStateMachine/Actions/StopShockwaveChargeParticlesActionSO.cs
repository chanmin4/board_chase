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
        _effects = stateMachine.GetComponent<PlayerEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.StopShockwaveChargeParticles();
    }

    public override void OnUpdate()
    {
    }
}
