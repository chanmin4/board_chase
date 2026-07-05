// Assets/Scripts/GameScene/Character/Enemy/CommonEnemyStateMachine/Action/PlayEnemySpawnParticlesActionSO.cs

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayEnemySpawnParticlesAction",
    menuName = "State Machines/EnemyCreature Actions/Particles/Play Spawn")]
public class PlayEnemyCreatureSpawnParticlesActionSO : StateActionSO<PlayEnemyCreatureSpawnParticlesAction>
{
}

public class PlayEnemyCreatureSpawnParticlesAction : StateAction
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
            _effects = stateMachine.GetComponentInChildren<EnemyEffectController>(true);

        if (_effects == null)
        {
            Transform current = stateMachine.transform.parent;

            while (current != null && _effects == null)
            {
                _effects = current.GetComponentInChildren<EnemyEffectController>(true);
                current = current.parent;
            }
        }

        if (_effects == null)
        {
            Debug.LogWarning(
                $"[PlayEnemyCreatureSpawnParticlesAction] EnemyEffectController not found. Action skipped. owner={stateMachine.name}",
                stateMachine);
        }
    }

    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.PlaySpawnParticles();
    }

    public override void OnUpdate()
    {
    }
}
