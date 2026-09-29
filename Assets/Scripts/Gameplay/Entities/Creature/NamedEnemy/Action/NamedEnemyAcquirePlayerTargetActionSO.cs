/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Action
 * Primary responsibility: Defines NamedEnemyAcquirePlayerTargetActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NamedEnemyAcquirePlayerTargetAction",
    menuName = "State Machines/Named Enemy Actions/Acquire Player Target")]
public class NamedEnemyAcquirePlayerTargetActionSO : StateActionSO<NamedEnemyAcquirePlayerTargetAction>
{
    [SerializeField] private PlayerRuntimeReadyEventChannelSO _playerRuntimeReadyChannel;
    [SerializeField] private bool _acquireOnUpdate = true;

    public bool AcquireOnUpdate => _acquireOnUpdate;

    // Purpose: Resolves the player needed by this component.
    public Transform ResolvePlayer()
    {
        if (_playerRuntimeReadyChannel != null && _playerRuntimeReadyChannel.Current != null)
            return _playerRuntimeReadyChannel.Current;

        return null;
    }
}

public class NamedEnemyAcquirePlayerTargetAction : StateAction
{
    private NamedEnemyAcquirePlayerTargetActionSO _origin;
    private Enemy _enemy;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (NamedEnemyAcquirePlayerTargetActionSO)OriginSO;
        _enemy = stateMachine.GetComponent<Enemy>();
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        TryAcquire();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_origin.AcquireOnUpdate)
            TryAcquire();
    }

    // Purpose: Attempts to acquire and reports whether the request succeeded.
    private void TryAcquire()
    {
        if (_enemy == null)
            return;

        if (_enemy.currentTarget != null && !_enemy.currentTarget.IsDead)
            return;

        Transform player = _origin.ResolvePlayer();
        if (player == null)
            return;

        Damageable damageable =
            player.GetComponent<Damageable>() ??
            player.GetComponentInParent<Damageable>();

        if (damageable == null || damageable.IsDead)
            return;

        _enemy.OnAlertTriggerChange(true, damageable.gameObject);
    }
}
