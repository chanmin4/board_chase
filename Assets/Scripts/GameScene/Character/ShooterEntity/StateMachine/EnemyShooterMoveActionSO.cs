using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterMoveAction",
    menuName = "State Machines/Enemy Shooter/Actions/Move")]
public class EnemyShooterMoveActionSO : StateActionSO<EnemyShooterMoveAction>
{
    [Tooltip("If true, stops the NavMeshAgent when this state exits.")]
    [SerializeField] private bool _stopMovementOnExit = false;

    public bool StopMovementOnExit => _stopMovementOnExit;
}

public class EnemyShooterMoveAction : StateAction
{
    private EnemyShooterMoveActionSO _origin;
    private EnemyShooterCombatController _combat;
    private bool _wasChasing;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterMoveActionSO)OriginSO;
        _combat = stateMachine.GetOrAddComponent<EnemyShooterCombatController>();
    }

    public override void OnStateEnter()
    {
        _wasChasing = false;
        _combat?.BeginPatrol();
    }

    public override void OnUpdate()
    {
        if (_combat == null)
            return;

        _combat.TickAwareness();

        if (_combat.ShouldChase)
        {
            if (!_wasChasing)
            {
                _combat.BeginChase();
                _wasChasing = true;
            }

            _combat.TickChase();
            return;
        }

        if (_wasChasing)
        {
            _combat.BeginPatrol();
            _wasChasing = false;
        }

        _combat.TickPatrol();
    }

    public override void OnStateExit()
    {
        if (_origin.StopMovementOnExit)
            _combat?.StopMovement();
    }
}
