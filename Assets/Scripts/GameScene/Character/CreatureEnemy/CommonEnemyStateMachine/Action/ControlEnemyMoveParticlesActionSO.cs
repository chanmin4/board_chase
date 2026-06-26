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
                $"[ControlEnemyMoveParticlesAction] EnemyEffectController not found. Action skipped. owner={stateMachine.name}",
                stateMachine);
        }
    }

    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.PlayMoveParticles();
    }

    public override void OnStateExit()
    {
        if (_effects != null)
            _effects.StopMoveParticles();
    }

    public override void OnUpdate()
    {
    }
}