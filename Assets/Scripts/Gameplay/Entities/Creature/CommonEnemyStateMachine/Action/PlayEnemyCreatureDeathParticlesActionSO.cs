/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > Action
 * Primary responsibility: Defines PlayEnemyCreatureDeathParticlesActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// Assets/Scripts/GameScene/Character/Enemy/CommonEnemyStateMachine/Action/PlayEnemyDeathParticlesActionSO.cs

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayEnemyDeathParticlesAction",
    menuName = "State Machines/EnemyCreature Actions/Particles/Play Death")]
public class PlayEnemyCreatureDeathParticlesActionSO : StateActionSO<PlayEnemyCreatureDeathParticlesAction>
{
}

public class PlayEnemyCreatureDeathParticlesAction : StateAction
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

        if (_effects == null)
            _effects = stateMachine.GetComponentInChildren<EnemyEffectController>(true);

        if (_effects == null)
        {
            Transform current = stateMachine.transform.parent;

            while (current != null && _effects == null && _shooterEffects == null)
            {
                _effects = current.GetComponentInChildren<EnemyEffectController>(true);
                _shooterEffects = current.GetComponentInChildren<ShooterEffectController>(true);
                current = current.parent;
            }
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
                $"[PlayEnemyCreatureDeathParticlesAction] EnemyEffectController or ShooterEffectController not found. Action skipped. owner={stateMachine.name}",
                stateMachine);
        }
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_effects != null)
        {
            _effects.PlayDeathParticles();
            return;
        }

        _shooterEffects?.PlayDeathParticles();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
    }
}
