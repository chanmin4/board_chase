/*
 * Role hierarchy: Gameplay > World > Sectors > Runtime
 * Primary responsibility: Manages SectorOccupancySummary stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    private readonly Dictionary<SectorRuntime, SectorOccupancySnapshot> _snapshots = new();
    private Vector2Int _currentSectorCoord;
    private bool _hasCurrentSectorCoord;
    public bool startSectorOpened;
    public SectorOwner startSectorOwner;
    public bool startSectorPlayerOwned;
    private bool _pinMapCenterToNamedSource;
    private Vector2Int _namedSourceMapCenterCoord;
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

    // Purpose: Releases active subscriptions and transient state while disabled.
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
    // Purpose: Responds to the sector state manager ready callback.
    private void OnSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();

        CacheInitialSnapshots();
    }
    // Purpose: Caches initial snapshots for later reuse.
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
    // Purpose: Responds to the current sector changed callback.
    private void OnCurrentSectorChanged(SectorRuntime currentSector)
    {
        if (currentSector == null)
            return;

        bool isStartSector = IsStartSector(currentSector);
        _sectorStateManager?.RevealSector(currentSector);

        _currentSectorCoord = GetSectorCoord(currentSector);
        _hasCurrentSectorCoord = true;

        PublishMapSnapshot();
    }
    // Purpose: Responds to the sector occupancy changed callback.
    private void OnSectorOccupancyChanged(SectorOccupancySnapshot snapshot)
    {
        if (snapshot.sector == null)
            return;

        _snapshots[snapshot.sector] = snapshot;
        PublishSummary();
        PublishMapSnapshot();
    }

    // Purpose: Publishes the summary notification.
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

    // Purpose: Publishes the map snapshot notification.
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
            bool isExtraction = _sectorStateManager != null &&
                                _sectorStateManager.CurrentStageMapLayout != null &&
                                _sectorStateManager.CurrentStageMapLayout.IsExtractionSector(coord);

            StageMapLayout raidLayout = _sectorStateManager?.CurrentStageMapLayout;
            RaidRoomRole raidRole = RaidRoomRole.None;
            bool publicMarker = false;
            if (raidLayout?.usesRaidLayout == true && raidLayout.TryGetRoom(coord, out var raidRoom))
            {
                raidRole = raidRoom.raidRole;
                bool hatchVisible = raidRole == RaidRoomRole.Hatch && RaidRuntime.IsHatchRevealedFor(raidLayout);
                publicMarker = isStartSector || isExtraction || hatchVisible ||
                    raidRole == RaidRoomRole.Generator || (raidRole == RaidRoomRole.Boss && raidLayout.bossSpawned);
                isExtraction |= isStartSector || hatchVisible;
                if (raidRole == RaidRoomRole.Boss && !raidLayout.bossSpawned) raidRole = RaidRoomRole.Normal;
                if (raidRole == RaidRoomRole.Hatch && !hatchVisible) raidRole = RaidRoomRole.None;
            }

            cells.Add(new SectorMapCellSnapshot
            {
                coord = coord,
                isOpened = sector.isOpened,
                isRevealed = isRevealed,
                isLocked = !isRevealed,
                isStartSector = isStartSector,
                isCleared = sector.IsCleared,
                isExtraction = isExtraction,
                raidRole = raidRole,
                hasPublicRaidMarker = publicMarker,
                roomType = ResolveRoomType(sector, isStartSector),
                mapIconKind = sector.MapIconKind,
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

        AddMissingRaidMapCells(cells);
        AddStartSectorMapCellIfMissing(cells);

        StageMapLayout presentationLayout = _sectorStateManager?.CurrentStageMapLayout;
        if (presentationLayout?.generationVersion == 3)
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (presentationLayout.TryGetRoom(cell.coord, out var room))
                    RaidMapPresentation.Apply(presentationLayout, room, GetMapSnapshotCenterCoord(), ref cell);
                cells[i] = cell;
            }

        SectorMapSnapshot snapshotMap = new SectorMapSnapshot
        {
            currentSectorCoord = GetMapSnapshotCenterCoord(),
            cells = cells.ToArray()
        };

        _mapSnapshotChangedChannel.RaiseEvent(snapshotMap);
    }

    public void RefreshRaidMarkers() => PublishMapSnapshot();

    // Map topology/landmarks must not depend on an occupancy component having published first.
    private void AddMissingRaidMapCells(List<SectorMapCellSnapshot> cells)
    {
        StageMapLayout layout = _sectorStateManager?.CurrentStageMapLayout;
        if (layout?.usesRaidLayout != true) return;
        var existing = new HashSet<Vector2Int>();
        foreach (var cell in cells) existing.Add(cell.coord);
        foreach (StageRoomNode room in layout.rooms)
        {
            if (room == null || !existing.Add(room.coord)) continue;
            bool start = room.coord == layout.startSectorCoord;
            bool hatch = room.raidRole == RaidRoomRole.Hatch && RaidRuntime.IsHatchRevealedFor(layout);
            bool boss = room.raidRole == RaidRoomRole.Boss && layout.bossSpawned;
            bool extraction = start || hatch || layout.IsExtractionSector(room.coord);
            _sectorStateManager.TryGetSector(room.coord, out var sector);
            cells.Add(new SectorMapCellSnapshot
            {
                coord = room.coord, roomType = room.roomType, isStartSector = start,
                mapIconKind = sector != null ? sector.MapIconKind : SectorMapIconPolicy.KindForRoom(room),
                isOpened = room.isOpened, isRevealed = start || room.isRevealed,
                isLocked = !start && !room.isRevealed, isCleared = room.isCleared,
                isExtraction = extraction,
                hasPublicRaidMarker = extraction || boss || room.raidRole == RaidRoomRole.Generator,
                raidRole = room.raidRole == RaidRoomRole.Hatch && !hatch ? RaidRoomRole.None :
                    room.raidRole == RaidRoomRole.Boss && !boss ? RaidRoomRole.Normal : room.raidRole
            });
        }
    }

    // Purpose: Adds the requested start sector map cell if missing to this component.
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
            isExtraction = false,
            roomType = StageRoomType.Start,
            mapIconKind = startSector.MapIconKind,
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

    // Purpose: Resolves the is revealed needed by this component.
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

    // Purpose: Checks whether start sector is currently true.
    private bool IsStartSector(SectorRuntime sector)
    {
        return _sectorStateManager != null && _sectorStateManager.IsStartSector(sector);
    }

    // Purpose: Returns the current sector coord.
    private Vector2Int GetSectorCoord(SectorRuntime sector)
    {
        if (_sectorStateManager != null)
            return _sectorStateManager.GetSectorCoord(sector);

        return sector != null ? sector.coord : default;
    }
    // Purpose: Performs the should hide from map workflow for this component.
    private bool ShouldHideFromMap(SectorRuntime sector, Vector2Int coord)
    {
        if (_sectorExclusionRules != null &&
            _sectorExclusionRules.HideFromMiniMapAlways(coord))
        {
            return true;
        }

        return false;
    }

    // Purpose: Resolves the room type needed by this component.
    private StageRoomType ResolveRoomType(SectorRuntime sector, bool isStartSector)
    {
        if (isStartSector)
            return StageRoomType.Start;

        if (_sectorStateManager != null &&
            _sectorStateManager.TryGetStageRoomType(sector, out StageRoomType roomType))
        {
            return roomType;
        }

        return StageRoomType.Normal;
    }

    // Purpose: Responds to the named sector phase changed callback.
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

    // Purpose: Returns the current map snapshot center coord.
    private Vector2Int GetMapSnapshotCenterCoord()
    {
        return _pinMapCenterToNamedSource
            ? _namedSourceMapCenterCoord
            : _currentSectorCoord;
    }

    // Purpose: Performs the should pin mini map to named source workflow for this component.
    private static bool ShouldPinMiniMapToNamedSource(NamedSectorPhase phase)
    {
        return phase == NamedSectorPhase.EnteringBattle ||
            phase == NamedSectorPhase.Battle ||
            phase == NamedSectorPhase.RewardPending ||
            phase == NamedSectorPhase.EndingBattle;
    }

    // Purpose: Performs the should release mini map named pin workflow for this component.
    private static bool ShouldReleaseMiniMapNamedPin(NamedSectorPhase phase)
    {
        return phase == NamedSectorPhase.None ||
            phase == NamedSectorPhase.WaitingForReservation ||
            phase == NamedSectorPhase.Reserved ||
            phase == NamedSectorPhase.Present ||
            phase == NamedSectorPhase.DefeatedCooldown;
    }
}
