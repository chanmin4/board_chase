using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterAcquireTargetAction",
    menuName = "State Machines/Enemy Shooter/Actions/Acquire Target")]
public class EnemyShooterAcquireTargetActionSO : StateActionSO<EnemyShooterAcquireTargetAction>
{
    [Tooltip("If true, scans immediately when entering the state instead of waiting for Target Search Interval.")]
    [SerializeField] private bool _forceScanOnEnter = true;

    public bool ForceScanOnEnter => _forceScanOnEnter;
}

public class EnemyShooterAcquireTargetAction : StateAction
{
    private EnemyShooterAcquireTargetActionSO _origin;
    private EnemyShooterCombatController _combat;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterAcquireTargetActionSO)OriginSO;
        _combat = stateMachine.GetOrAddComponent<EnemyShooterCombatController>();
    }

    public override void OnStateEnter()
    {
        if (_origin.ForceScanOnEnter)
            _combat?.ForceAcquirePlayer();
    }

    public override void OnUpdate()
    {
        _combat?.TickAwareness();
    }
}
