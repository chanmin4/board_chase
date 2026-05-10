using System.Collections.Generic;
using UnityEngine;

public class NamedWorldLockManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorStateManager _sectorStateManager;
    [SerializeField] private SectorCleanupApplier _cleanupApplier;

    [Header("Listening")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleStartedEvent;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleEndedEvent;

    [Header("Broadcasting")]
    [SerializeField] private BoolEventChannelSO _namedWorldLockedmanagerchannel;

    [Header("Options")]
    [SerializeField] private bool _cleanupOpenedOutsideSectorsOnBattleStart = true;

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
        _namedWorldLockedmanagerchannel?.RaiseEvent(true);

        if (_cleanupOpenedOutsideSectorsOnBattleStart)
            CleanupOpenedOutsideSectors();
    }

    private void HandleBattleEnded(SectorRuntime namedSourceSector)
    {
        _namedWorldLockedmanagerchannel?.RaiseEvent(false);
    }

    private void CleanupOpenedOutsideSectors()
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
