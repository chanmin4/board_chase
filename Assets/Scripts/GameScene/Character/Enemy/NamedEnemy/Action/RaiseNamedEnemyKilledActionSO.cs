using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "RaiseNamedEnemyKilledAction",
    menuName = "State Machines/Named Enemy Actions/Raise Named Enemy Killed")]
public class RaiseNamedEnemyKilledActionSO : StateActionSO<RaiseNamedEnemyKilledAction>
{
    [SerializeField] private NamedEnemyKilledEventChannelSO _killedEventChannel;

    public NamedEnemyKilledEventChannelSO KilledEventChannel => _killedEventChannel;
}

public class RaiseNamedEnemyKilledAction : StateAction
{
    private RaiseNamedEnemyKilledActionSO _config;
    private NamedEnemy _namedEnemy;
    private bool _raised;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (RaiseNamedEnemyKilledActionSO)OriginSO;
        _namedEnemy = stateMachine.GetComponentInParent<NamedEnemy>();
    }

    public override void OnStateEnter()
    {
        if (_raised)
            return;

        _raised = true;

        if (_config.KilledEventChannel != null)
            _config.KilledEventChannel.RaiseEvent(_namedEnemy);
    }

    public override void OnUpdate()
    {
    }
}
