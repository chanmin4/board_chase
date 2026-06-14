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
    public bool startSectorOpened;
    public SectorOwner startSectorOwner;
    public bool startSectorPlayerOwned;
}


public class SectorOccupancyManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorStateManager _sectorStateManager;

    [Header("Manager Ready Broadcast")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [Header("Named Sector")]
    [SerializeField] private NamedSectorPhaseEventChannelSO _namedSectorPhaseChannel;
    
    [Header("Map Snapshot")]
    [SerializeField] private SectorMapSnapshotEventChannelSO _mapSnapshotChangedChannel;
    [SerializeField] private VoidEventChannelSO _requestMapSnapshotChannel;
    [SerializeField] private SectorOccupancyEventChannelSO _sectorOccupancyChangedChannel;
    [SerializeField] private SectorOccupancySummaryEventChannelSO _summaryChangedChannel;
    [Header("Current Sector")]
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;
    [Header("Exclusion")]
    [SerializeField] private SectorExclusionRulesSO _sectorExclusionRules;

    [Tooltip("Legacy option. Keep false for the current stage-map flow so the start sector stays visible on MiniMap and FullMap.")]
    [SerializeField] private bool _hideStartSectorFromMapAfterLeaving = false;
    private readonly Dictionary<SectorRuntime, SectorOccupancySnapshot> _snapshots = new();
    private Vector2Int _currentSectorCoord;
    private bool _hasCurrentSectorCoord;
    public bool startSectorOpened;
    public SectorOwner startSectorOwner;
    public bool startSectorPlayerOwned;
    private bool _hasLeftStartSector;
    private bool _pinMapCenterToNamedSource;
    private Vector2Int _namedSourceMapCenterCoord;
    private void Awake()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorStateManager != null)
            _sectorStateManager.EnsureInitialized();
     
    }

    private void OnEnable()
    {
        if (_sectorOccupancyChangedChannel != null)
            _sectorOccupancyChangedChannel.OnEventRaised += OnSectorOccupancyChanged;

        if (_requestMapSnapshotChannel != null)
            _requestMapSnapshotChannel.OnEventRaised += PublishMapSnapshot;

        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.OnEventRaised += OnCurrentSectorChanged;

        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += OnSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                OnSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }
        else if (_sectorStateManager != null)
        {
            OnSectorStateManagerReady(_sectorStateManager);
        }
        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised += OnNamedSectorPhaseChanged;
    }

    private void OnDisable()
    {
        if (_sectorOccupancyChangedChannel != null)
            _sectorOccupancyChangedChannel.OnEventRaised -= OnSectorOccupancyChanged;

        if (_requestMapSnapshotChannel != null)
            _requestMapSnapshotChannel.OnEventRaised -= PublishMapSnapshot;

        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.OnEventRaised -= OnCurrentSectorChanged;

        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= OnSectorStateManagerReady;
        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised -= OnNamedSectorPhaseChanged;
    }
    private void OnSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();

        CacheInitialSnapshots();
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

            if (!_hasCurrentSectorCoord || IsStartSector(sector))
            {
                _currentSectorCoord = GetSectorCoord(sector);
                _hasCurrentSectorCoord = true;
            }
        }

        if (_sectorStateManager != null && _sectorStateManager.CurrentSector != null)
        {
            _sectorStateManager.RevealSector(_sectorStateManager.CurrentSector);
            _currentSectorCoord = GetSectorCoord(_sectorStateManager.CurrentSector);
            _hasCurrentSectorCoord = true;
        }
        else if (!_hasCurrentSectorCoord &&
            _sectorStateManager != null &&
            _sectorStateManager.StartSector != null)
        {
            _sectorStateManager.RevealSector(_sectorStateManager.StartSector);
            _currentSectorCoord = GetSectorCoord(_sectorStateManager.StartSector);
            _hasCurrentSectorCoord = true;
        }

        PublishSummary();
        PublishMapSnapshot();
    }
    private void OnCurrentSectorChanged(SectorRuntime currentSector)
    {
        if (currentSector == null)
            return;

        bool isStartSector = IsStartSector(currentSector);
        _sectorStateManager?.RevealSector(currentSector);

        if (_hideStartSectorFromMapAfterLeaving && !isStartSector)
            _hasLeftStartSector = true;

        _currentSectorCoord = GetSectorCoord(currentSector);
        _hasCurrentSectorCoord = true;

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

            Vector2Int coord = GetSectorCoord(sector);

            if (IsStartSector(sector))
            {
                summary.startSectorOpened = true;
                summary.startSectorOwner = snapshot.owner;
                summary.startSectorPlayerOwned = snapshot.owner == SectorOwner.Player;
                continue;
            }

            if (_sectorExclusionRules != null &&
                _sectorExclusionRules.ExcludeFromOccupancySummary(coord))
            {
                continue;
            }

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

            Vector2Int coord = GetSectorCoord(sector);

            if (ShouldHideFromMap(sector, coord))
                continue;

            bool isStartSector = IsStartSector(sector);
            bool isRevealed = ResolveIsRevealed(sector, isStartSector);

            cells.Add(new SectorMapCellSnapshot
            {
                coord = coord,
                isOpened = sector.isOpened,
                isRevealed = isRevealed,
                isLocked = !isRevealed,
                isStartSector = isStartSector,
                isCleared = sector.IsCleared,
                roomType = ResolveRoomType(sector, isStartSector),
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

        AddStartSectorMapCellIfMissing(cells);

        SectorMapSnapshot snapshotMap = new SectorMapSnapshot
        {
            currentSectorCoord = GetMapSnapshotCenterCoord(),
            cells = cells.ToArray()
        };

        _mapSnapshotChangedChannel.RaiseEvent(snapshotMap);
    }

    private void AddStartSectorMapCellIfMissing(List<SectorMapCellSnapshot> cells)
    {
        if (_sectorStateManager == null || _sectorStateManager.StartSector == null)
            return;

        SectorRuntime startSector = _sectorStateManager.StartSector;
        Vector2Int startCoord = GetSectorCoord(startSector);

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].coord == startCoord)
                return;
        }

        cells.Add(new SectorMapCellSnapshot
        {
            coord = startCoord,
            isOpened = startSector.isOpened,
            isRevealed = true,
            isLocked = false,
            isStartSector = true,
            isCleared = startSector.IsCleared,
            roomType = StageRoomType.Start,
            owner = SectorOwner.Neutral,
            dominantOwner = SectorOwner.Neutral,
            contestState = SectorContestState.None,
            specialState = SectorSpecialState.None,
            playerRatio = 0f,
            virusRatio = 0f,
            contestElapsed = 0f,
            contestRequired = 0f
        });
    }

    private bool ResolveIsRevealed(SectorRuntime sector, bool isStartSector)
    {
        if (sector == null)
            return false;

        if (isStartSector)
            return true;

        if (_sectorStateManager != null &&
            _sectorStateManager.TryGetSectorRevealed(sector, out bool isRevealed))
        {
            return isRevealed;
        }

        return sector.isOpened;
    }

    private bool IsStartSector(SectorRuntime sector)
    {
        return _sectorStateManager != null && _sectorStateManager.IsStartSector(sector);
    }

    private Vector2Int GetSectorCoord(SectorRuntime sector)
    {
        if (_sectorStateManager != null)
            return _sectorStateManager.GetSectorCoord(sector);

        return sector != null ? sector.coord : default;
    }
    private bool ShouldHideFromMap(SectorRuntime sector, Vector2Int coord)
    {
        if (_sectorExclusionRules != null &&
            _sectorExclusionRules.HideFromMiniMapAlways(coord))
        {
            return true;
        }

        return false;
    }

    private StageRoomType ResolveRoomType(SectorRuntime sector, bool isStartSector)
    {
        if (isStartSector)
            return StageRoomType.Start;

        if (_sectorStateManager != null &&
            _sectorStateManager.TryGetStageRoomType(sector, out StageRoomType roomType))
        {
            return roomType;
        }

        return StageRoomType.NormalBattle;
    }

    private void OnNamedSectorPhaseChanged(NamedSectorPhaseChange change)
    {
        if (ShouldPinMiniMapToNamedSource(change.Phase) && change.Sector != null)
        {
            _namedSourceMapCenterCoord = GetSectorCoord(change.Sector);
            _pinMapCenterToNamedSource = true;
            PublishMapSnapshot();
            return;
        }

        if (ShouldReleaseMiniMapNamedPin(change.Phase))
        {
            _pinMapCenterToNamedSource = false;
            PublishMapSnapshot();
        }
    }

    private Vector2Int GetMapSnapshotCenterCoord()
    {
        return _pinMapCenterToNamedSource
            ? _namedSourceMapCenterCoord
            : _currentSectorCoord;
    }

    private static bool ShouldPinMiniMapToNamedSource(NamedSectorPhase phase)
    {
        return phase == NamedSectorPhase.EnteringBattle ||
            phase == NamedSectorPhase.Battle ||
            phase == NamedSectorPhase.RewardPending ||
            phase == NamedSectorPhase.EndingBattle;
    }

    private static bool ShouldReleaseMiniMapNamedPin(NamedSectorPhase phase)
    {
        return phase == NamedSectorPhase.None ||
            phase == NamedSectorPhase.WaitingForReservation ||
            phase == NamedSectorPhase.Reserved ||
            phase == NamedSectorPhase.Present ||
            phase == NamedSectorPhase.DefeatedCooldown;
    }
}
