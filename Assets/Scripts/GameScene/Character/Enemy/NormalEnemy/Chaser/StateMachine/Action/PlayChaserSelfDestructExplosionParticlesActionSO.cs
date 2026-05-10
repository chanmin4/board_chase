using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayChaserSelfDestructExplosionParticlesAction",
    menuName = "State Machines/Enemy Actions/Particles/Chaser Play Explosion")]
public class PlayChaserSelfDestructExplosionParticlesActionSO
    : StateActionSO<PlayChaserSelfDestructExplosionParticlesAction>
{
}

public class PlayChaserSelfDestructExplosionParticlesAction : StateAction
{
    private ChaserVirusEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = stateMachine.GetComponent<ChaserVirusEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.PlaySelfDestructExplosionParticles();
    }

    public override void OnUpdate()
    {
    }
}
