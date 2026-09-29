/*
 * Role hierarchy: Gameplay > World > Sectors > Named_Boss
 * Primary responsibility: Coordinates the NamedBattleManager subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Broadcasts named battle active state for systems that still need battle-flow awareness.
/// This does not own named enemy spawning or named battle room logic.
/// </summary>
public class NamedBattleManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorStateManager _sectorStateManager;

    [Header("Listening")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleStartedEvent;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleEndedEvent;

    [Header("Broadcasting")]
    [SerializeField] private BoolEventChannelSO _namedBattleActiveChannel;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorStateManager != null)
            _sectorStateManager.EnsureInitialized();

    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

        if (_battleStartedEvent != null)
            _battleStartedEvent.OnEventRaised += HandleBattleStarted;

        if (_battleEndedEvent != null)
            _battleEndedEvent.OnEventRaised += HandleBattleEnded;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        if (_battleStartedEvent != null)
            _battleStartedEvent.OnEventRaised -= HandleBattleStarted;

        if (_battleEndedEvent != null)
            _battleEndedEvent.OnEventRaised -= HandleBattleEnded;
    }

    // Purpose: Handles the incoming sector state manager ready request or event.
    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();
    }

    // Purpose: Handles the incoming battle started request or event.
    private void HandleBattleStarted(SectorRuntime namedSourceSector)
    {
        _namedBattleActiveChannel?.RaiseEvent(true);
    }

    // Purpose: Handles the incoming battle ended request or event.
    private void HandleBattleEnded(SectorRuntime namedSourceSector)
    {
        _namedBattleActiveChannel?.RaiseEvent(false);
    }

}
