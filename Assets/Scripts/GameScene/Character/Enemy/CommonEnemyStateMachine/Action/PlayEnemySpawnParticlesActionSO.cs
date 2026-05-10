using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayEnemySpawnParticlesAction",
    menuName = "State Machines/Enemy Actions/Particles/Play Spawn")]
public class PlayEnemySpawnParticlesActionSO : StateActionSO<PlayEnemySpawnParticlesAction>
{
}

public class PlayEnemySpawnParticlesAction : StateAction
{
    private EnemyEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = stateMachine.GetComponent<EnemyEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.PlaySpawnParticles();
    }

    public override void OnUpdate()
    {
    }
}
