using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayEnemyGetHitParticlesAction",
    menuName = "State Machines/Enemy Actions/Particles/Play Get Hit")]
public class PlayEnemyGetHitParticlesActionSO : StateActionSO<PlayEnemyGetHitParticlesAction>
{
}

public class PlayEnemyGetHitParticlesAction : StateAction
{
    private EnemyEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = stateMachine.GetComponent<EnemyEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.PlayGetHitParticles();
    }

    public override void OnUpdate()
    {
    }
}
