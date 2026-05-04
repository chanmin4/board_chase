using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayEnemyDeathParticlesAction",
    menuName = "State Machines/Enemy Actions/Particles/Play Death")]
public class PlayEnemyDeathParticlesActionSO : StateActionSO<PlayEnemyDeathParticlesAction>
{
}

public class PlayEnemyDeathParticlesAction : StateAction
{
    private EnemyEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = stateMachine.GetComponent<EnemyEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.PlayDeathParticles();
    }

    public override void OnUpdate()
    {
    }
}
