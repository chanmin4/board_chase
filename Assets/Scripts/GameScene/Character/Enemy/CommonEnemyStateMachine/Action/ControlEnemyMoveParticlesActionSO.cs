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
        _effects = stateMachine.GetComponent<EnemyEffectController>();
    }

    public override void OnStateEnter()
    {
        _effects.PlayMoveParticles();
    }

    public override void OnStateExit()
    {
        _effects.StopMoveParticles();
    }

    public override void OnUpdate()
    {
    }
}
