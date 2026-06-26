using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyAcquirePlayerTargetAction",
    menuName = "State Machines/Enemy Actions/Acquire Player Target")]
public class EnemyAcquirePlayerTargetActionSO : StateActionSO<EnemyAcquirePlayerTargetAction>
{
    [Header("Refs")]
    [SerializeField] private PlayerRuntimeReadyEventChannelSO _playerRuntimeReadyChannel;
    [SerializeField] private PlayerIsInZoneSO _alertCondition;

    [Header("Timing")]
    [SerializeField] private bool _syncOnEnter = true;
    [SerializeField] private bool _syncOnUpdate = true;

    [Header("Behavior")]
    [SerializeField] private bool _clearTargetWhenAlertFalse = true;

    public bool SyncOnEnter => _syncOnEnter;
    public bool SyncOnUpdate => _syncOnUpdate;
    public bool ClearTargetWhenAlertFalse => _clearTargetWhenAlertFalse;

    public bool IsAlert(Enemy enemy)
    {
        if (_alertCondition != null)
            return _alertCondition.EvaluateForEnemy(enemy);

        return enemy != null && enemy.isPlayerInAlertZone;
    }

    public Damageable ResolvePlayerDamageable()
    {
        if (_playerRuntimeReadyChannel == null || _playerRuntimeReadyChannel.Current == null)
            return null;

        Transform player = _playerRuntimeReadyChannel.Current;

        return player.GetComponent<Damageable>() ??
               player.GetComponentInParent<Damageable>();
    }
}

public class EnemyAcquirePlayerTargetAction : StateAction
{
    private EnemyAcquirePlayerTargetActionSO _origin;
    private Enemy _enemy;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyAcquirePlayerTargetActionSO)OriginSO;

        if (!stateMachine.TryGetComponent(out _enemy))
            _enemy = stateMachine.GetComponentInChildren<Enemy>(true);
    }

    public override void OnStateEnter()
    {
        if (_origin.SyncOnEnter)
            SyncTarget();
    }

    public override void OnUpdate()
    {
        if (_origin.SyncOnUpdate)
            SyncTarget();
    }

    private void SyncTarget()
    {
        if (_enemy == null)
            return;

        Damageable playerDamageable = _origin.ResolvePlayerDamageable();
        if (playerDamageable == null)
            return;

        GameObject playerObject = playerDamageable.gameObject;

        if (_origin.IsAlert(_enemy))
        {
            if (!playerDamageable.IsDead)
                _enemy.OnAlertTriggerChange(true, playerObject);

            return;
        }

        if (_origin.ClearTargetWhenAlertFalse)
            _enemy.OnAlertTriggerChange(false, playerObject);
    }
}
