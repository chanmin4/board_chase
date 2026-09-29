/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > Action
 * Primary responsibility: Defines ControlEnemyMoveParticlesActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// Assets/Scripts/GameScene/Character/Enemy/CommonEnemyStateMachine/Action/ControlEnemyMoveParticlesActionSO.cs

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ControlEnemyMoveParticlesAction",
    menuName = "State Machines/Enemy Actions/Particles/Control Move")]
public class ControlEnemyMoveParticlesActionSO : StateActionSO<ControlEnemyMoveParticlesAction>
{
}

public class ControlEnemyMoveParticlesAction : StateAction
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
                $"[ControlEnemyMoveParticlesAction] EnemyEffectController or ShooterEffectController not found. Action skipped. owner={stateMachine.name}",
                stateMachine);
        }
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_effects != null)
        {
            _effects.PlayMoveParticles();
            return;
        }

        _shooterEffects?.PlayFootTrailParticles();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (_effects != null)
        {
            _effects.StopMoveParticles();
            return;
        }

        _shooterEffects?.StopFootTrailParticles();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
    }
}
