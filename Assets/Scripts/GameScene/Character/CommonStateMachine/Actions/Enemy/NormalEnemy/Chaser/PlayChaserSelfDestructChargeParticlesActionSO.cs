using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayChaserSelfDestructChargeParticlesAction",
    menuName = "State Machines/Enemy Actions/Particles/Chaser Play Charge")]
public class PlayChaserSelfDestructChargeParticlesActionSO
    : StateActionSO<PlayChaserSelfDestructChargeParticlesAction>
{
}

public class PlayChaserSelfDestructChargeParticlesAction : StateAction
{
    private ChaserVirusEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = stateMachine.GetComponent<ChaserVirusEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.PlaySelfDestructChargeParticles();
    }

    public override void OnUpdate()
    {
    }
}
