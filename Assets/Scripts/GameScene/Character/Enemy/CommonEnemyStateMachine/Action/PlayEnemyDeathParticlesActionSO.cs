// Assets/Scripts/GameScene/Character/Enemy/CommonEnemyStateMachine/Action/PlayEnemyDeathParticlesActionSO.cs

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
        if (!stateMachine.TryGetComponent(out _effects))
        {
            Enemy enemy = stateMachine.GetComponentInParent<Enemy>();
            if (enemy != null)
                _effects = enemy.GetComponentInChildren<EnemyEffectController>(true);
        }

        if (_effects == null)
        {
            Debug.LogWarning(
                $"[PlayEnemyDeathParticlesAction] EnemyEffectController not found. Action skipped. owner={stateMachine.name}",
                stateMachine);
        }
    }

    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.PlayDeathParticles();
    }

    public override void OnUpdate()
    {
    }
}