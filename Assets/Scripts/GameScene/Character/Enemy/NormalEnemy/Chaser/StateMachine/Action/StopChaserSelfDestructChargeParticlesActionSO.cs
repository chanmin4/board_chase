using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "StopChaserSelfDestructChargeParticlesAction",
    menuName = "State Machines/Enemy Actions/Particles/Chaser Stop Charge")]
public class StopChaserSelfDestructChargeParticlesActionSO
    : StateActionSO<StopChaserSelfDestructChargeParticlesAction>
{
}

public class StopChaserSelfDestructChargeParticlesAction : StateAction
{
    private ChaserVirusEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = stateMachine.GetComponent<ChaserVirusEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.StopSelfDestructChargeParticles();
    }

    public override void OnUpdate()
    {
    }
}
