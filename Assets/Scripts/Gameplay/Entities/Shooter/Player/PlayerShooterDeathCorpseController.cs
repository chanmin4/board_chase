/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Coordinates the PlayerShooterDeathCorpseController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerShooterDeathCorpseController : ShooterDeathCorpseController
{
    [Header("Broadcasting On")]
    [Tooltip("Raised only after the Player has finished converting to a corpse. SceneLoader can then apply its GameOver delay.")]
    [SerializeField] private VoidEventChannelSO _gameOverEvent;

    [Header("Run Loss")]
    [Tooltip("The ordinary player inventory cleared on death. The persistent inheritance warehouse is saved separately and is never cleared here.")]
    [SerializeField] private PlayerInventoryRuntime _playerInventoryRuntime;

    private bool _gameOverRaised;

    // Purpose: Performs the after convert to corpse workflow for this component.
    protected override void AfterConvertToCorpse()
    {
        base.AfterConvertToCorpse();

        if (_gameOverRaised)
            return;

        _gameOverRaised = true;

        if (_playerInventoryRuntime == null)
            _playerInventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);

        // PersistentManager is the owner of run-loss ordering. Isolated test
        // scenes without it retain the former local behavior as a fallback.
        if (!RunLifecycleCoordinator.TryHandlePlayerRunLoss(_playerInventoryRuntime))
        {
            ClearRunBoundPossessions();
            ShopRunRuntime.EnsureRuntime().ResetForRunLoss();
        }

        if (_gameOverEvent == null)
        {
            Debug.LogError(
                "[PlayerShooterDeathCorpseController] GameOver event channel is missing.",
                this);
            return;
        }

        _gameOverEvent.RaiseEvent();
    }

    // Purpose: Clears the current run bound possessions state.
    private void ClearRunBoundPossessions()
    {
        if (_playerInventoryRuntime == null)
            _playerInventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);

        if (_playerInventoryRuntime == null)
        {
            Debug.LogError(
                "[PlayerShooterDeathCorpseController] PlayerInventoryRuntime is missing; run possessions could not be cleared.",
                this);
            return;
        }

        _playerInventoryRuntime.ClearForRunLoss();
    }
}
