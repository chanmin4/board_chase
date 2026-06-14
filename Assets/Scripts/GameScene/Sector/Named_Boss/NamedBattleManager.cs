using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles global side effects when a named battle starts or ends.
/// Current responsibilities:
/// - Optionally cleans up opened sectors when named battle starts.
/// - Broadcasts named battle active state for systems that still need battle-flow awareness.
/// This does not own named enemy spawning or named battle room logic.
/// </summary>
public class NamedBattleManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorStateManager _sectorStateManager;
    [SerializeField] private SectorCleanupApplier _cleanupApplier;

    [Header("Listening")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleStartedEvent;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleEndedEvent;

    [Header("Broadcasting")]
    [SerializeField] private BoolEventChannelSO _namedBattleActiveChannel;

    [Header("Options")]
    [SerializeField] private bool _cleanupOpenedSectorsOnBattleStart = true;

    private void Awake()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorStateManager != null)
            _sectorStateManager.EnsureInitialized();

        if (_cleanupApplier == null)
            _cleanupApplier = FindAnyObjectByType<SectorCleanupApplier>();
    }

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

    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        if (_battleStartedEvent != null)
            _battleStartedEvent.OnEventRaised -= HandleBattleStarted;

        if (_battleEndedEvent != null)
            _battleEndedEvent.OnEventRaised -= HandleBattleEnded;
    }

    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();
    }

    private void HandleBattleStarted(SectorRuntime namedSourceSector)
    {
        _namedBattleActiveChannel?.RaiseEvent(true);

        if (_cleanupOpenedSectorsOnBattleStart)
            CleanupOpenedSectors();
    }

    private void HandleBattleEnded(SectorRuntime namedSourceSector)
    {
        _namedBattleActiveChannel?.RaiseEvent(false);
    }

    private void CleanupOpenedSectors()
    {
        if (_sectorStateManager == null || _cleanupApplier == null)
            return;

        IReadOnlyList<SectorRuntime> sectors = _sectorStateManager.Sectors;

        if (sectors == null)
            return;

        for (int i = 0; i < sectors.Count; i++)
        {
            SectorRuntime sector = sectors[i];

            if (sector == null || !sector.IsOpened)
                continue;

            _cleanupApplier.CleanupSector(sector);
        }
    }
}