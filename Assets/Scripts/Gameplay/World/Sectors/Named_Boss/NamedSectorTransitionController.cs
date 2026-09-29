/*
 * Role hierarchy: Gameplay > World > Sectors > Named_Boss
 * Primary responsibility: Manages NamedSectorTransitionController stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using UnityEngine;
using PixeLadder.EasyTransition;

public class NamedSectorTransitionController : MonoBehaviour
{
    [Header("Transition")]
    [Tooltip("Runs the shared screen transition request, input lock, and timeout flow.")]
    [SerializeField] private GameplayScreenTransitionRunner _transitionRunner;

    [Tooltip("Persistent transition interface request channel.")]
    [SerializeField] private ScreenTransitionRequestEventChannelSO _transitionRequestChannel;
    [SerializeField] private float _transitionTimeoutSeconds = 5f;
    [SerializeField] private TransitionEffect _enterEffect;
    [SerializeField] private TransitionEffect _exitEffect;
    [SerializeField] private float _coveredHoldSeconds = 0.1f;
    [Header("Listening")]
    [SerializeField] private PlayerRuntimeReadyEventChannelSO _playerRuntimeReadyChannel;
    [Header("Sector Tracking")]
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;
    [SerializeField] private SectorRuntime _battleSector;
    [Header("Player")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _playerRoot;

    [Tooltip("Where the player appears inside the named battle sector.")]
    [SerializeField] private Transform _battlePlayerSpawnPoint;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_playerRuntimeReadyChannel != null)
        {
            _playerRuntimeReadyChannel.OnEventRaised += HandlePlayerRuntimeReady;

            if (_playerRuntimeReadyChannel.HasCurrent)
                HandlePlayerRuntimeReady(_playerRuntimeReadyChannel.Current);
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_playerRuntimeReadyChannel != null)
            _playerRuntimeReadyChannel.OnEventRaised -= HandlePlayerRuntimeReady;
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _transitionRunner = GetComponent<GameplayScreenTransitionRunner>();
    }

    // Purpose: Handles the incoming player runtime ready request or event.
    private void HandlePlayerRuntimeReady(Transform playerRoot)
    {
        _playerRoot = playerRoot;
    }
    // Purpose: Plays the requested enter transition action or effect.
    public IEnumerator PlayEnterTransition(
        SectorRuntime sourceSector,
        Action onCovered,
        Action onComplete)
    {
        yield return PlayTransition(
            _enterEffect,
            () =>
            {
                TeleportToBattleSector();
                onCovered?.Invoke();
            },
            onComplete
        );
    }

    // Purpose: Plays the requested exit transition action or effect.
    public IEnumerator PlayExitTransition(
        SectorRuntime sourceSector,
        Action onCovered,
        Action onComplete)
    {
        yield return PlayTransition(
            _exitEffect,
            () =>
            {
                TeleportBackToSourceSector(sourceSector);
                onCovered?.Invoke();
            },
            onComplete
        );
    }

    // Purpose: Plays the requested transition action or effect.
    private IEnumerator PlayTransition(
        TransitionEffect effect,
        Action onCovered,
        Action onComplete)
    {
        if (_transitionRunner == null)
        {
            Debug.LogError(
                "[NamedSectorTransitionController] GameplayScreenTransitionRunner is missing. " +
                "Assign the shared transition runner in the Inspector.",
                this);
            yield break;
        }

        yield return _transitionRunner.PlayTransition(
            _transitionRequestChannel,
            _inputReader,
            effect,
            _coveredHoldSeconds,
            _transitionTimeoutSeconds,
            onCovered,
            onComplete);
    }

    // Purpose: Performs the teleport to battle sector workflow for this component.
    private void TeleportToBattleSector()
    {
        if (!TryResolvePlayerRoot() || _battlePlayerSpawnPoint == null)
        {
            Debug.LogWarning(
                $"[NamedSectorTransitionController] Cannot teleport. " +
                $"player={_playerRoot?.name}, spawn={_battlePlayerSpawnPoint?.name}",
                this);
            return;
        }

        TeleportPlayer(_battlePlayerSpawnPoint.position, _battlePlayerSpawnPoint.rotation);
        if (_currentSectorChangedEvent != null && _battleSector != null)
            _currentSectorChangedEvent.RaiseEvent(_battleSector);
    
    }
    // Purpose: Attempts to resolve player root and reports whether the request succeeded.
    private bool TryResolvePlayerRoot()
    {
        if (_playerRoot != null)
            return true;

        if (_playerRuntimeReadyChannel != null && _playerRuntimeReadyChannel.HasCurrent)
        {
            _playerRoot = _playerRuntimeReadyChannel.Current;
            return _playerRoot != null;
        }

        return false;
    }
    // Purpose: Performs the teleport back to source sector workflow for this component.
    private void TeleportBackToSourceSector(SectorRuntime sourceSector)
    {
        if (_playerRoot == null || sourceSector == null)
            return;

        Bounds bounds = sourceSector.GetWorldBounds();

        Vector3 targetPosition = bounds.center;
        targetPosition.y = _playerRoot.position.y;

        TeleportPlayer(targetPosition, _playerRoot.rotation);
        if (_currentSectorChangedEvent != null && sourceSector != null)
            _currentSectorChangedEvent.RaiseEvent(sourceSector);
    }
    // Purpose: Performs the teleport player workflow for this component.
    private void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[NamedSectorTransitionController] TeleportPlayer to {position}", this);
        CharacterController controller = _playerRoot.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        _playerRoot.SetPositionAndRotation(position, rotation);

        if (controller != null)
            controller.enabled = true;
    }
}
