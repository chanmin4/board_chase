/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > Action
 * Primary responsibility: Defines PlayEnemyCreatureGetHitParticlesActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
    private ShooterEffectController _shooterEffects;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        if (!stateMachine.TryGetComponent(out _effects))
        {
            Enemy enemy = stateMachine.GetComponentInParent<Enemy>();
            if (enemy != null)
                _effects = enemy.GetComponentInChildren<EnemyEffectController>(true);
        }

        if (_shooterEffects == null)
        {
            _shooterEffects =
                stateMachine.GetComponentInChildren<ShooterEffectController>(true) ??
                stateMachine.GetComponentInParent<ShooterEffectController>();
        }

        if (_effects == null && _shooterEffects == null)
        {
            Debug.LogWarning(
                $"[PlayEnemyCreatureGetHitParticlesAction] EnemyEffectController or ShooterEffectController not found. Action skipped. owner={stateMachine.name}",
                stateMachine);
        }
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_effects != null)
        {
            _effects.PlayGetHitParticles();
            return;
        }

        _shooterEffects?.PlayGetHitParticles();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
    }
}
