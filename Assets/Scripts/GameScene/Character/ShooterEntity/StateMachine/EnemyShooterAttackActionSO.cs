using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterAttackAction",
    menuName = "State Machines/Enemy Shooter/Actions/Attack")]
public class EnemyShooterAttackActionSO : StateActionSO<EnemyShooterAttackAction>
{
}

public class EnemyShooterAttackAction : StateAction
{
    private EnemyShooterCombatController _combat;

    public override void Awake(StateMachine stateMachine)
    {
        _combat = stateMachine.GetOrAddComponent<EnemyShooterCombatController>();
    }

    public override void OnStateEnter()
    {
        _combat?.BeginAttack();
    }

    public override void OnUpdate()
    {
        if (_combat == null)
            return;

        _combat.TickAwareness();
        _combat.TickAttack();
    }

    public override void OnStateExit()
    {
        _combat?.EndAttack();
    }
}
