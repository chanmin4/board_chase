/*
 * Role hierarchy: Gameplay > World > Sectors > Runtime
 * Primary responsibility: Manages SectorStateManager stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class SectorStateManager : MonoBehaviour
{
    [Serializable]
    public struct RuntimeSectorStateEntry
    {
        public SectorRuntime sector;
        public Vector2Int coord;
        public StageRoomType roomType;
        public bool isStartSector;
        public bool isCurrentSector;
        public bool isActiveStageSector;
        public bool isOpened;
        public bool isCleared;
        public bool isRevealed;
        public bool isFailed;
    }
    [Header("Room Exploration")]
    [Tooltip("諛??대━?????쇰━留??곌껐 ?뺣낫???곕씪 ?몄젒 諛⑹쓣 opened ?곹깭濡??댁? ?щ??낅땲??")]
    [SerializeField] private bool _openAdjacentSectorsOnClear = true;
    [Header("Broadcasts")]
    [Tooltip("?꾩옱 ?뚮젅?댁뼱媛 ?랁븳 sector媛 諛붾???諛쒖깮/?섏떊?섎뒗 ?대깽?몄엯?덈떎.")]
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;

    [Tooltip("StartSector媛 以鍮꾨릺?덉쓬???뚮━???대깽?몄엯?덈떎. 珥덇린 移대찓???뚮젅?댁뼱 ?꾩튂 ?명똿?먯꽌 ?ъ슜?????덉뒿?덈떎.")]
    [SerializeField] private SectorRuntimeEventChannelSO _startSectorReadyEvent;

    [Tooltip("sector媛 opened ?곹깭媛 ?섏뿀????諛쒖깮?쒗궎???대깽?몄엯?덈떎. portal refresh??UI 媛깆떊?먯꽌 援щ룆?⑸땲??")]
    [SerializeField] private SectorRuntimeEventChannelSO _sectorOpenedEvent;

    [Tooltip("SectorStateManager媛 珥덇린?붾릺???ㅻⅨ ?쒖뒪?쒖뿉??李몄“ 媛?ν빐議뚯쓬???뚮━???대깽?몄엯?덈떎.")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;

    [Header("Runtime Debug")]
    [SerializeField, ReadOnly] private List<RuntimeSectorStateEntry> _runtimeSectorStates = new();

    public int CurrentStage { get; private set; }
    public SectorRuntime StartSector { get; private set; }
    public SectorRuntime CurrentSector { get; private set; }

    public IReadOnlyList<SectorRuntime> Sectors => _runtimeSectors;

    /// <summary>
    /// Moves the player-facing current-sector state without changing generated-map ownership.
    /// This supports both a generated stage entry room and an external Hideout.
    /// </summary>
    public bool SetPlayerCurrentSector(
        SectorRuntime sector,
        bool notifySpawnSystem)
    {
        if (sector == null)
            return false;

        CurrentSector = sector;
        SectorRoomTraitRuntime.SetCurrentPlayerSector(CurrentSector);

        if (notifySpawnSystem)
            _startSectorReadyEvent?.RaiseEvent(sector);

        _currentSectorChangedEvent?.RaiseEvent(sector);
        RefreshRuntimeSectorStates();
        return true;
    }

    /// <summary>
    /// Compatibility entry point for external sectors such as the persistent Hideout.
    /// </summary>
    public bool SetExternalCurrentSector(
        SectorRuntime sector,
        bool notifySpawnSystem)
    {
        return SetPlayerCurrentSector(sector, notifySpawnSystem);
    }
    public IReadOnlyList<RuntimeSectorStateEntry> RuntimeSectorStates => _runtimeSectorStates;
    public StageMapLayout CurrentStageMapLayout { get; private set; }
    public bool HasCurrentStageMap => CurrentStageMapLayout != null;

    private readonly Dictionary<Vector2Int, SectorRuntime> _sectorByCoord = new();
    private readonly Dictionary<SectorRuntime, Vector2Int> _coordBySector = new();
    private readonly List<SectorRuntime> _runtimeSectors = new();
    private readonly HashSet<SectorRuntime> _activeStageSectors = new();
    private readonly Dictionary<Vector2Int, StageRoomNode> _roomByCoord = new();
    private readonly Dictionary<SectorRuntime, StageRoomNode> _roomBySector = new();
    private readonly HashSet<SectorRuntime> _failedSectors = new();
    private bool _isInitialized;
    private bool _stageMapConfigured;
    private bool _allowStartSectorAccess;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.OnEventRaised += HandleCurrentSectorChanged;

        EnsureInitialized();

    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.OnEventRaised -= HandleCurrentSectorChanged;

        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.Clear(this);
        if (_startSectorReadyEvent != null && StartSector != null)
            _startSectorReadyEvent.Clear(StartSector);
        if (_currentSectorChangedEvent != null && CurrentSector != null)
            _currentSectorChangedEvent.Clear(CurrentSector);

        CurrentSector = null;
        SectorRoomTraitRuntime.SetCurrentPlayerSector(null);
        RefreshRuntimeSectorStates();
    }
    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureInitialized();
    }

    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.RaiseEvent(this);

        if (!_stageMapConfigured)
            return;

        if (CurrentSector == null)
            CurrentSector = StartSector;

        // The persistent Hideout can become CurrentSector before Start() runs.
        // Publish the actual current sector so the spawn system never moves the
        // player back into the generated stage's internal Start node.
        if (_startSectorReadyEvent != null && CurrentSector != null)
            _startSectorReadyEvent.RaiseEvent(CurrentSector);

        if (_currentSectorChangedEvent != null && CurrentSector != null)
            _currentSectorChangedEvent.RaiseEvent(CurrentSector);
    }

    // Purpose: Ensures initialized is ready before the workflow continues.
    public void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        InitializeGeneratedMapState();
        _isInitialized = true;
    }

    // Purpose: Initializes generated map state for use by this component.
    private void InitializeGeneratedMapState()
    {
        _failedSectors.Clear();
        _sectorByCoord.Clear();
        _coordBySector.Clear();
        _runtimeSectors.Clear();
        _activeStageSectors.Clear();
        _roomByCoord.Clear();
        _roomBySector.Clear();
        _runtimeSectorStates.Clear();
        StartSector = null;
        CurrentSector = null;
        CurrentStageMapLayout = null;
        _stageMapConfigured = false;
        _allowStartSectorAccess = false;
    }

    // Purpose: Applies the requested initial open state change.
    private void ApplyInitialOpenState()
    {
        Debug.LogError(
            "[SectorStateManager] ApplyInitialOpenState is disabled. StageSectorInstantiator must register generated sectors before play.",
            this);
    }

    // Purpose: Closes the active all sectors UI or workflow.
    private void CloseAllSectors()
    {
        for (int i = 0; i < _runtimeSectors.Count; i++)
        {
            if (_runtimeSectors[i] != null)
                _runtimeSectors[i].SetOpened(false);
        }

        RefreshRuntimeSectorStates();
    }

    // Purpose: Performs the progress next stage workflow for this component.
    public void ProgressNextStage()
    {
        Debug.LogError(
            "[SectorStateManager] ProgressNextStage is disabled. StageProgressionManager must build the next generated stage.",
            this);
    }

    // Purpose: Applies the requested stage change.
    public void ApplyStage(int stage)
    {
        Debug.LogError(
            $"[SectorStateManager] ApplyStage({stage}) is disabled. Register a generated stage map instead.",
            this);
    }

    // Purpose: Configures this component''s stage map.
    public void ConfigureStageMap(
        int stage,
        int roomGridSize,
        bool useStartSectorOnly)
    {
        Debug.LogError(
            $"[SectorStateManager] ConfigureStageMap(stage={stage}, roomGridSize={roomGridSize}, useStartSectorOnly={useStartSectorOnly}) is disabled. StageSectorInstantiator must generate and register sectors.",
            this);
    }

    // Purpose: Registers generated stage map with its owning runtime service.
    public void RegisterGeneratedStageMap(StageMapLayout layout,
        IReadOnlyList<SectorRuntime> generatedSectors)
    {
        EnsureInitialized();

        if (layout == null)
            return;

        _failedSectors.Clear();
        _sectorByCoord.Clear();
        _coordBySector.Clear();
        _runtimeSectors.Clear();
        _activeStageSectors.Clear();
        _roomByCoord.Clear();
        _roomBySector.Clear();

        CurrentStage = Mathf.Max(0, layout.stageIndex);
        CurrentStageMapLayout = layout;
        StartSector = null;
        CurrentSector = null;
        _stageMapConfigured = true;
        _allowStartSectorAccess = true;

        if (generatedSectors != null)
        {
            for (int i = 0; i < generatedSectors.Count; i++)
            {
                SectorRuntime sector = generatedSectors[i];

                if (sector == null)
                    continue;

                Vector2Int coord = sector.Coord;

                if (_sectorByCoord.ContainsKey(coord))
                {
                    Debug.LogWarning(
                        $"[SectorStateManager] Generated sector coord duplicated: {coord}. Sector={sector.name}",
                        sector);
                    continue;
                }

                bool isStartSector = coord == layout.startSectorCoord;
                sector.SetRuntimeInfo(coord, opened: false, isStartSector: isStartSector);

                _sectorByCoord.Add(coord, sector);
                _coordBySector.Add(sector, coord);
                _runtimeSectors.Add(sector);

                if (isStartSector)
                    StartSector = sector;
            }
        }

        if (StartSector == null &&
            _sectorByCoord.TryGetValue(layout.startSectorCoord, out SectorRuntime foundStartSector))
        {
            StartSector = foundStartSector;
            StartSector.SetRuntimeInfo(layout.startSectorCoord, opened: false, isStartSector: true);
        }

        if (StartSector == null)
        {
            Debug.LogWarning(
                $"[SectorStateManager] Generated StartSector not found. Coord={layout.startSectorCoord}",
                this);
        }

        for (int i = 0; i < layout.rooms.Count; i++)
        {
            StageRoomNode room = layout.rooms[i];

            if (room == null || room.roomType == StageRoomType.Empty)
                continue;

            _roomByCoord[room.coord] = room;

            if (_sectorByCoord.TryGetValue(room.coord, out SectorRuntime sector))
                _roomBySector[sector] = room;
        }

        CloseAllSectors();

        for (int i = 0; i < _runtimeSectors.Count; i++)
        {
            SectorRuntime sector = _runtimeSectors[i];

            if (sector == null)
                continue;

            if (sector == StartSector)
            {
                sector.SetCleared(true);
                SetSectorOpened(sector);
                continue;
            }

            if (!_roomBySector.TryGetValue(sector, out StageRoomNode room))
                continue;

            if (room.roomType == StageRoomType.Empty ||
                room.roomType == StageRoomType.Start)
            {
                continue;
            }

            _activeStageSectors.Add(sector);
            sector.SetCleared(room.isCleared);

            if (room.isOpened && !room.isLocked)
                SetSectorOpened(sector);
        }

        BroadcastGeneratedStartSectorReady();
        RefreshRuntimeSectorStates();
    }

    // Purpose: Publishes the generated start sector ready notification.
    private void BroadcastGeneratedStartSectorReady()
    {
        if (StartSector == null)
            return;

        CurrentSector = StartSector;
        SectorRoomTraitRuntime.SetCurrentPlayerSector(CurrentSector);

        if (_startSectorReadyEvent != null)
            _startSectorReadyEvent.RaiseEvent(StartSector);

        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.RaiseEvent(CurrentSector);

        RefreshRuntimeSectorStates();
    }

    // Purpose: Opens the sector UI or workflow.
    public void OpenSector(SectorRuntime sector)
    {
        if (sector == null)
            return;

        if (_stageMapConfigured && !IsActiveStageSector(sector))
            return;

        if (_roomBySector.TryGetValue(sector, out StageRoomNode room) &&
            room.isLocked)
        {
            return;
        }

        SetSectorOpened(sector);
    }

    // Purpose: Updates the sector opened state.
    private void SetSectorOpened(SectorRuntime sector)
    {
        if (sector == null)
            return;

        bool wasOpened = sector.IsOpened;
        sector.SetOpened(true);

        if (_roomBySector.TryGetValue(sector, out StageRoomNode room))
            room.isOpened = true;

        if (!wasOpened && _sectorOpenedEvent != null)
            _sectorOpenedEvent.RaiseEvent(sector);

        RefreshRuntimeSectorStates();
    }

    // Purpose: Performs the complete sector workflow for this component.
    public bool CompleteSector(SectorRuntime sector)
    {
        EnsureInitialized();

        if (!IsManagedSector(sector) ||
            !IsActiveStageSector(sector) ||
            sector.IsCleared)
        {
            return false;
        }
        _failedSectors.Remove(sector);
        sector.SetCleared(true);

        if (_roomBySector.TryGetValue(sector, out StageRoomNode room))
        {
            room.isCleared = true;
            room.isOpened = true;
            room.isRevealed = true;
        }

        if (_openAdjacentSectorsOnClear)
            OpenAdjacentSectors(sector);

        // Existing listeners use this channel to refresh runtime sector state.
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.RaiseEvent(sector);

        RefreshRuntimeSectorStates();
        return true;
    }

    // Purpose: Opens the adjacent sectors UI or workflow.
    private void OpenAdjacentSectors(SectorRuntime source)
    {
        if (source == null || !TryGetSectorCoord(source, out Vector2Int coord))
            return;

        if (_roomBySector.TryGetValue(source, out StageRoomNode room))
        {
            OpenConnectedRoom(coord, room, StageRoomConnectionFlags.Left, Vector2Int.left);
            OpenConnectedRoom(coord, room, StageRoomConnectionFlags.Right, Vector2Int.right);
            OpenConnectedRoom(coord, room, StageRoomConnectionFlags.Down, Vector2Int.down);
            OpenConnectedRoom(coord, room, StageRoomConnectionFlags.Up, Vector2Int.up);
            return;
        }

        OpenSectorAt(coord + Vector2Int.left);
        OpenSectorAt(coord + Vector2Int.right);
        OpenSectorAt(coord + Vector2Int.up);
        OpenSectorAt(coord + Vector2Int.down);
    }

    // Purpose: Opens the connected room UI or workflow.
    private void OpenConnectedRoom(
        Vector2Int sourceCoord,
        StageRoomNode sourceRoom,
        StageRoomConnectionFlags connection,
        Vector2Int offset)
    {
        if (sourceRoom == null || !sourceRoom.HasConnection(connection))
            return;

        OpenSectorAt(sourceCoord + offset);
    }

    // Purpose: Opens the sector at UI or workflow.
    private void OpenSectorAt(Vector2Int coord)
    {
        if (_sectorByCoord.TryGetValue(coord, out SectorRuntime sector))
            OpenSector(sector);
    }

    // Purpose: Handles the incoming current sector changed request or event.
    private void HandleCurrentSectorChanged(SectorRuntime sector)
    {
        if (sector == null)
            return;

        CurrentSector = sector;
        SectorRoomTraitRuntime.SetCurrentPlayerSector(CurrentSector);
        RevealSector(sector);
        OpenAdjacentSectorsForFreePassNormalSector(sector);
        CompleteNonCombatSectorOnEntry(sector);
        RefreshRuntimeSectorStates();
    }

    // Purpose: Opens the adjacent sectors for free pass normal sector UI or workflow.
    private void OpenAdjacentSectorsForFreePassNormalSector(SectorRuntime sector)
    {
        if (sector == null ||
            !_roomBySector.TryGetValue(sector, out StageRoomNode room) ||
            room == null ||
            room.roomType != StageRoomType.Normal ||
            !sector.AllowsPortalTravelBeforeClear)
        {
            return;
        }

        // Free-pass rooms may reveal their exits without marking themselves
        // cleared. This keeps optional patrol rooms visible as unresolved while
        // still allowing an exploration route to continue.
        OpenAdjacentSectors(sector);
    }

    // Purpose: Performs the complete non combat sector on entry workflow for this component.
    private void CompleteNonCombatSectorOnEntry(SectorRuntime sector)
    {
        if (sector == null || sector.IsCleared ||
            !_roomBySector.TryGetValue(sector, out StageRoomNode room) ||
            room == null)
        {
            return;
        }

        if (room.roomType == StageRoomType.Shop ||
            room.roomType == StageRoomType.Treasure)
        {
            CompleteSector(sector);
        }
    }

    // Purpose: Performs the reveal sector workflow for this component.
    public bool RevealSector(SectorRuntime sector)
    {
        EnsureInitialized();

        if (!IsManagedSector(sector))
            return false;

        bool changed = false;

        if (!sector.IsOpened)
        {
            sector.SetOpened(true);
            changed = true;
        }

        if (_roomBySector.TryGetValue(sector, out StageRoomNode room))
        {
            if (!room.isOpened)
            {
                room.isOpened = true;
                changed = true;
            }

            if (!room.isRevealed)
            {
                room.isRevealed = true;
                changed = true;
            }
        }

        if (sector == StartSector && !sector.IsCleared)
        {
            sector.SetCleared(true);
            changed = true;
        }

        if (changed && _sectorOpenedEvent != null)
            _sectorOpenedEvent.RaiseEvent(sector);

        if (changed)
            RefreshRuntimeSectorStates();

        return changed;
    }

    // Purpose: Attempts to get sector and reports whether the request succeeded.
    public bool TryGetSector(Vector2Int coord, out SectorRuntime sector)
    {
        EnsureInitialized();
        return _sectorByCoord.TryGetValue(coord, out sector);
    }

    // Purpose: Attempts to get sector coord and reports whether the request succeeded.
    public bool TryGetSectorCoord(SectorRuntime sector, out Vector2Int coord)
    {
        EnsureInitialized();

        if (sector != null)
            return _coordBySector.TryGetValue(sector, out coord);

        coord = default;
        return false;
    }

    // Purpose: Checks whether managed sector is currently true.
    public bool IsManagedSector(SectorRuntime sector)
    {
        EnsureInitialized();
        return sector != null && _coordBySector.ContainsKey(sector);
    }

    // Purpose: Checks whether active stage sector is currently true.
    public bool IsActiveStageSector(SectorRuntime sector)
    {
        EnsureInitialized();

        if (sector == null)
            return false;

        if (!_stageMapConfigured)
            return IsManagedSector(sector);

        return _activeStageSectors.Contains(sector) ||
               (_allowStartSectorAccess && sector == StartSector);
    }

    // Purpose: Attempts to get stage room type and reports whether the request succeeded.
    public bool TryGetStageRoomType(
        SectorRuntime sector,
        out StageRoomType roomType)
    {
        EnsureInitialized();

        if (sector != null &&
            _roomBySector.TryGetValue(sector, out StageRoomNode room))
        {
            roomType = room.roomType;
            return true;
        }

        roomType = StageRoomType.Empty;
        return false;
    }

    // Purpose: Attempts to get stage goal sector and reports whether the request succeeded.
    public bool TryGetStageGoalSector(out SectorRuntime sector)
    {
        EnsureInitialized();

        sector = null;

        if (CurrentStageMapLayout == null)
            return false;

        return _sectorByCoord.TryGetValue(
            CurrentStageMapLayout.goalRoomCoord,
            out sector);
    }

    // Purpose: Returns the current sector coord.
    public Vector2Int GetSectorCoord(SectorRuntime sector)
    {
        EnsureInitialized();

        if (sector != null && _coordBySector.TryGetValue(sector, out Vector2Int coord))
            return coord;

        return sector != null ? sector.coord : default;
    }

    // Purpose: Checks whether start sector is currently true.
    public bool IsStartSector(SectorRuntime sector)
    {
        return sector != null && sector == StartSector;
    }

    // Purpose: Checks whether start sector coord is currently true.
    public bool IsStartSectorCoord(Vector2Int coord)
    {
        return CurrentStageMapLayout != null &&
               coord == CurrentStageMapLayout.startSectorCoord;
    }

    // Purpose: Checks whether opened is currently true.
    public bool IsOpened(SectorRuntime sector)
    {
        return sector != null && sector.IsOpened;
    }

    // Purpose: Checks whether opened is currently true.
    public bool IsOpened(Vector2Int coord)
    {
        EnsureInitialized();

        return _sectorByCoord.TryGetValue(coord, out SectorRuntime sector) &&
               sector.IsOpened;
    }

    // Purpose: Attempts to get sector revealed and reports whether the request succeeded.
    public bool TryGetSectorRevealed(SectorRuntime sector, out bool isRevealed)
    {
        EnsureInitialized();

        isRevealed = false;

        if (sector == null)
            return false;

        if (sector == StartSector || IsStartSector(sector))
        {
            isRevealed = true;
            return true;
        }

        if (_roomBySector.TryGetValue(sector, out StageRoomNode room))
        {
            isRevealed = room.isRevealed;
            return true;
        }

        if (IsManagedSector(sector))
        {
            isRevealed = sector.IsOpened;
            return true;
        }

        return false;
    }

    // Purpose: Marks the active sector workflow as failed.
    public bool FailSector(SectorRuntime sector)
    {
        EnsureInitialized();

        if (!IsManagedSector(sector) ||
            !IsActiveStageSector(sector) ||
            sector == StartSector ||
            sector.IsCleared)
        {
            return false;
        }

        _failedSectors.Add(sector);
        sector.SetCleared(false);

        if (_roomBySector.TryGetValue(sector, out StageRoomNode room))
        {
            room.isOpened = true;
            room.isRevealed = true;
            room.isCleared = false;
        }

        // Failed combat rooms are resolved for portal use, so they must reveal
        // the same connected progression routes as cleared rooms.
        if (_openAdjacentSectorsOnClear)
            OpenAdjacentSectors(sector);

        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.RaiseEvent(sector);

        RefreshRuntimeSectorStates();
        return true;
    }

    // Purpose: Checks whether sector failed is currently true.
    public bool IsSectorFailed(SectorRuntime sector)
    {
        EnsureInitialized();
        return sector != null && _failedSectors.Contains(sector);
    }

    // Purpose: Clears the current sector failure state.
    public void ClearSectorFailure(SectorRuntime sector)
    {
        EnsureInitialized();

        if (sector == null)
            return;

        _failedSectors.Remove(sector);
        RefreshRuntimeSectorStates();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only entry point used by DebugMonitor. It deliberately raises the
    /// shared current-sector channel so every runtime listener observes the same
    /// transition as a normal portal move.
    /// </summary>
    public bool DebugTryEnterSector(SectorRuntime sector)
    {
        EnsureInitialized();

        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[SectorStateManager] Debug sector movement is available only in PlayMode.",
                this);
            return false;
        }

        if (!IsManagedSector(sector))
        {
            Debug.LogError(
                "[SectorStateManager] Debug sector movement rejected because the sector is not managed by the current stage.",
                sector);
            return false;
        }

        if (TryGetStageRoomType(sector, out StageRoomType roomType) &&
            roomType == StageRoomType.Empty)
        {
            Debug.LogError(
                $"[SectorStateManager] Debug sector movement rejected because coord={sector.Coord} is an Empty room.",
                sector);
            return false;
        }

        if (_currentSectorChangedEvent == null)
        {
            Debug.LogError(
                "[SectorStateManager] Current Sector Changed Event is not assigned. Debug movement cannot keep runtime systems synchronized.",
                this);
            return false;
        }

        _currentSectorChangedEvent.RaiseEvent(sector);
        RefreshRuntimeSectorStates();
        return CurrentSector == sector;
    }
#endif

    // Purpose: Refreshes the displayed or cached runtime sector states.
    private void RefreshRuntimeSectorStates()
    {
        _runtimeSectorStates.Clear();

        for (int i = 0; i < _runtimeSectors.Count; i++)
        {
            SectorRuntime sector = _runtimeSectors[i];

            if (sector == null)
                continue;

            StageRoomType roomType = StageRoomType.Empty;
            bool isRevealed = sector == StartSector || sector.IsOpened;

            if (_roomBySector.TryGetValue(sector, out StageRoomNode room) &&
                room != null)
            {
                roomType = room.roomType;
                isRevealed = room.isRevealed;
            }

            _runtimeSectorStates.Add(new RuntimeSectorStateEntry
            {
                sector = sector,
                coord = sector.Coord,
                roomType = roomType,
                isStartSector = sector == StartSector,
                isCurrentSector = sector == CurrentSector,
                isActiveStageSector = _activeStageSectors.Contains(sector) ||
                                      (_allowStartSectorAccess && sector == StartSector),
                isOpened = sector.IsOpened,
                isCleared = sector.IsCleared,
                isRevealed = isRevealed,
                isFailed = _failedSectors.Contains(sector)
            });
        }
    }

#if UNITY_EDITOR
    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
    }
#endif
}

