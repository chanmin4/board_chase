using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SectorOccupancySummary
{
    public int openedCount;
    public int playerOwnedCount;
    public int virusOwnedCount;
    public int neutralCount;
    public int namedActiveCount;
    public int bossActiveCount;
    public float virusPressure;
}


public class SectorOccupancyManager : MonoBehaviour
{
    [Header("Map Snapshot")]
    [SerializeField] private SectorMapSnapshotEventChannelSO _mapSnapshotChangedChannel;
    [SerializeField] private VoidEventChannelSO _requestMapSnapshotChannel;
    [SerializeField] private SectorOccupancyEventChannelSO _sectorOccupancyChangedChannel;
    [SerializeField] private SectorOccupancySummaryEventChannelSO _summaryChangedChannel;

    private readonly Dictionary<SectorRuntime, SectorOccupancySnapshot> _snapshots = new();
    private Vector2Int _currentSectorCoord;
    private bool _hasCurrentSectorCoord;

    private void OnEnable()
    {
        if (_sectorOccupancyChangedChannel != null)
            _sectorOccupancyChangedChannel.OnEventRaised += OnSectorOccupancyChanged;
        if( _requestMapSnapshotChannel != null)
            _requestMapSnapshotChannel.OnEventRaised += PublishMapSnapshot;     
        CacheInitialSnapshots();
    }

    private void OnDisable()
    {
        if (_sectorOccupancyChangedChannel != null)
            _sectorOccupancyChangedChannel.OnEventRaised -= OnSectorOccupancyChanged;
        if (_requestMapSnapshotChannel != null)     
            _requestMapSnapshotChannel.OnEventRaised -= PublishMapSnapshot;
    }

    private void CacheInitialSnapshots()
    {
        _snapshots.Clear();
        _hasCurrentSectorCoord = false;

        SectorOccupancy[] occupancies = FindObjectsByType<SectorOccupancy>(FindObjectsSortMode.None);
        for (int i = 0; i < occupancies.Length; i++)
        {
            SectorOccupancySnapshot snapshot = occupancies[i].CurrentSnapshot;
            SectorRuntime sector = snapshot.sector;

            if (sector == null)
                continue;

            _snapshots[sector] = snapshot;

            if (!_hasCurrentSectorCoord || sector.isStartSector)
            {
                _currentSectorCoord = sector.coord;
                _hasCurrentSectorCoord = true;
            }
        }

        PublishSummary();
        PublishMapSnapshot();
    }

    private void OnSectorOccupancyChanged(SectorOccupancySnapshot snapshot)
    {
        if (snapshot.sector == null)
            return;

        _snapshots[snapshot.sector] = snapshot;
        PublishSummary();
        PublishMapSnapshot();
    }

    private void PublishSummary()
    {
        SectorOccupancySummary summary = new SectorOccupancySummary();

        foreach (var pair in _snapshots)
        {
            SectorRuntime sector = pair.Key;
            SectorOccupancySnapshot snapshot = pair.Value;

            if (sector == null || !sector.isOpened)
                continue;

            summary.openedCount++;

            if (snapshot.owner == SectorOwner.Player) summary.playerOwnedCount++;
            else if (snapshot.owner == SectorOwner.Virus) summary.virusOwnedCount++;
            else summary.neutralCount++;

            if ((snapshot.specialState & SectorSpecialState.NamedActive) != 0) summary.namedActiveCount++;
            if ((snapshot.specialState & SectorSpecialState.BossActive) != 0) summary.bossActiveCount++;
        }

        summary.virusPressure = summary.openedCount > 0
            ? (float)summary.virusOwnedCount / summary.openedCount
            : 0f;

        if (_summaryChangedChannel != null)
            _summaryChangedChannel.RaiseEvent(summary);
    }

    private void PublishMapSnapshot()
    {
        if (_mapSnapshotChangedChannel == null)
            return;

        List<SectorMapCellSnapshot> cells = new List<SectorMapCellSnapshot>(_snapshots.Count);

        foreach (var pair in _snapshots)
        {
            SectorRuntime sector = pair.Key;
            SectorOccupancySnapshot snapshot = pair.Value;

            if (sector == null)
                continue;

            cells.Add(new SectorMapCellSnapshot
            {
                coord = sector.coord,
                isOpened = sector.isOpened,
                isLocked = !sector.isOpened,
                owner = snapshot.owner,
                dominantOwner = snapshot.dominantOwner,
                contestState = snapshot.contestState,
                specialState = snapshot.specialState,
                playerRatio = snapshot.playerRatio,
                virusRatio = snapshot.virusRatio,
                contestElapsed = snapshot.contestElapsed,
                contestRequired = snapshot.contestRequired
            });
        }

        SectorMapSnapshot snapshotMap = new SectorMapSnapshot
        {
            currentSectorCoord = _currentSectorCoord,
            cells = cells.ToArray()
        };

        _mapSnapshotChangedChannel.RaiseEvent(snapshotMap);
    }
}
