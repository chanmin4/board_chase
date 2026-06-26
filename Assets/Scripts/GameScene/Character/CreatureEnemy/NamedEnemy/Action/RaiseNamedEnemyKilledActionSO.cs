using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "RaiseNamedEnemyKilledAction",
    menuName = "State Machines/Named Enemy Actions/Raise Named Enemy Killed")]
public class RaiseNamedEnemyKilledActionSO : StateActionSO<RaiseNamedEnemyKilledAction>
{
    [SerializeField] private NamedEnemyKilledEventChannelSO _killedEventChannel;

    [Header("Delay")]
    [SerializeField, Min(0f)] private float _delaySeconds = 2f;
    [Header("Before Raise")]
    [SerializeField] private NamedBattleSectorResetRequestEventChannelSO _battleSectorResetRequestChannel;
    public NamedEnemyKilledEventChannelSO KilledEventChannel => _killedEventChannel;
    public float DelaySeconds => _delaySeconds;
    public NamedBattleSectorResetRequestEventChannelSO BattleSectorResetRequestChannel => _battleSectorResetRequestChannel;
}

public class RaiseNamedEnemyKilledAction : StateAction
{
    private RaiseNamedEnemyKilledActionSO _config;
    private NamedEnemy _namedEnemy;

    private float _timer;
    private bool _raised;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (RaiseNamedEnemyKilledActionSO)OriginSO;
        _namedEnemy = stateMachine.GetComponentInParent<NamedEnemy>();
    }

    public override void OnStateEnter()
    {
        _timer = 0f;
        _raised = false;

        if (_config.DelaySeconds <= 0f)
            Raise();
    }

    public override void OnUpdate()
    {
        if (_raised)
            return;

        _timer += Time.deltaTime;

        if (_timer >= _config.DelaySeconds)
            Raise();
    }

    private void Raise()
    {
        if (_raised)
            return;

        _raised = true;

        if (_config.BattleSectorResetRequestChannel != null)
            _config.BattleSectorResetRequestChannel.RaiseEvent();

        if (_config.KilledEventChannel != null)
            _config.KilledEventChannel.RaiseEvent(_namedEnemy);
    }
}
