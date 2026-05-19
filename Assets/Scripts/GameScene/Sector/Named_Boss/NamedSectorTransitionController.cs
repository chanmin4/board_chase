using System;
using System.Collections;
using UnityEngine;
using PixeLadder.EasyTransition;

public class NamedSectorTransitionController : MonoBehaviour
{
    [Header("Transition")]
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

    private void OnEnable()
    {
        if (_playerRuntimeReadyChannel != null)
        {
            _playerRuntimeReadyChannel.OnEventRaised += HandlePlayerRuntimeReady;

            if (_playerRuntimeReadyChannel.HasCurrent)
                HandlePlayerRuntimeReady(_playerRuntimeReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_playerRuntimeReadyChannel != null)
            _playerRuntimeReadyChannel.OnEventRaised -= HandlePlayerRuntimeReady;
    }
    private void HandlePlayerRuntimeReady(Transform playerRoot)
    {
        _playerRoot = playerRoot;
    }
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

    private IEnumerator PlayTransition(
        TransitionEffect effect,
        Action onCovered,
        Action onComplete)
    {
        if (_inputReader != null)
            _inputReader.DisableAllInput();

        bool completed = false;

        ScreenTransitionRequest request = new ScreenTransitionRequest(
            effect,
            onCovered,
            () =>
            {
                if (_inputReader != null)
                    _inputReader.EnableGameplayInput();

                onComplete?.Invoke();
                completed = true;
            },
            _coveredHoldSeconds
        );

        bool accepted = _transitionRequestChannel != null &&
                        _transitionRequestChannel.RaiseEvent(request);

        if (!accepted)
        {
            onCovered?.Invoke();

            if (_inputReader != null)
                _inputReader.EnableGameplayInput();

            onComplete?.Invoke();
            yield break;
        }

        float timeoutAt = Time.unscaledTime + Mathf.Max(0.5f, _transitionTimeoutSeconds);

        while (!completed)
        {
            if (Time.unscaledTime >= timeoutAt)
            {
                Debug.LogWarning("[NamedSectorTransitionController] Transition timed out. Forcing covered/complete callbacks.", this);

                onCovered?.Invoke();

                if (_inputReader != null)
                    _inputReader.EnableGameplayInput();

                onComplete?.Invoke();
                completed = true;
                yield break;
            }

            yield return null;
        }
    }

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
