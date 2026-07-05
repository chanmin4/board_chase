// Assets/Scripts/GameScene/Character/Enemy/CommonEnemyStateMachine/Action/PlayEnemyGetHitParticlesActionSO.cs

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayEnemyGetHitParticlesAction",
    menuName = "State Machines/EnemyCreature Actions/Particles/Play Get Hit")]
public class PlayEnemyCreatureGetHitParticlesActionSO : StateActionSO<PlayEnemyCreatureGetHitParticlesAction>
{
}

public class PlayEnemyCreatureGetHitParticlesAction : StateAction
{
    private EnemyEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        if (!stateMachine.TryGetComponent(out _effects))
        {
            Enemy enemy = stateMachine.GetComponentInParent<Enemy>();
            if (enemy != null)
                _effects = enemy.GetComponentInChildren<EnemyEffectController>(true);
        }

        if (_effects == null)
        {
            Debug.LogWarning(
                $"[PlayEnemyCreatureGetHitParticlesAction] EnemyEffectController not found. Action skipped. owner={stateMachine.name}",
                stateMachine);
        }
    }

    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.PlayGetHitParticles();
    }

    public override void OnUpdate()
    {
    }
}