using System;
using System.Collections.Generic;
using UnityEngine;

public class SectorStateManager : MonoBehaviour
{
    [Serializable]
    public struct SectorEntry
    {
        [Tooltip("씬에 배치된 SectorRuntime입니다. 생성형 stage에서도 StartSector는 이 테이블에 등록된 sector를 사용합니다.")]
        public SectorRuntime sector;

        [Tooltip("sector의 논리 좌표입니다. Vector2Int.x는 월드 X축, Vector2Int.y는 월드 Z축 방향으로 사용합니다.")]
        public Vector2Int coord;
    }

    [Header("Sector Table")]
    [Tooltip("씬에 미리 배치된 sector와 논리 좌표를 연결하는 테이블입니다. 생성형 stage에서도 StartSector는 이 테이블에서 가져옵니다.")]
    [SerializeField] private SectorEntry[] _sectors;

    [Header("Stage Unlock")]
    [Tooltip("구형 stage별 sector 해금 규칙입니다. 생성형 StageMap 흐름에서는 보통 사용하지 않습니다.")]
    [SerializeField] private StageSectorUnlockSO _stageSectorUnlock;

    [Tooltip("구형 StageSectorUnlockSO 기반 해금 흐름을 사용할 때만 켭니다. 생성형 StageMap을 쓰는 동안은 보통 꺼둡니다.")]
    [SerializeField] private bool _useStageSectorUnlocks = false;

    [Header("Room Exploration")]
    [Tooltip("방 클리어 시 논리맵 연결 정보에 따라 인접 방을 opened 상태로 열지 여부입니다.")]
    [SerializeField] private bool _openAdjacentSectorsOnClear = true;

    [Header("Start Sector")]
    [Tooltip("고정 StartSector 좌표입니다. 현재 설계에서는 (-1, 0)을 시작 섹터로 사용하고, 생성 방은 0,0부터 시작합니다.")]
    [SerializeField] private Vector2Int _startSectorCoord = new Vector2Int(-1, 0);

    [Header("Start")]
    [Tooltip("게임 시작 시 적용할 stage index입니다. 보통 0으로 두고 StageProgressionRulesSO의 stage 0 규칙을 사용합니다.")]
    [SerializeField] private int _initialStage = 0;

    [Tooltip("부팅 직후 StartSector만 열어둘지 여부입니다. 생성형 stage가 먼저 등록되면 해당 설정은 덮어쓰지 않습니다.")]
    [SerializeField] private bool _openOnlyStartSectorOnBoot = true;

    [Header("Requests")]
    [Tooltip("외부에서 특정 stage index를 바로 적용하라고 요청하는 이벤트입니다.")]
    [SerializeField] private IntEventChannelSO _requestUnlockStageEvent;

    [Tooltip("외부에서 현재 stage 다음 stage로 진행하라고 요청하는 이벤트입니다.")]
    [SerializeField] private VoidEventChannelSO _requestProgressNextStageEvent;

    [Header("Broadcasts")]
    [Tooltip("현재 플레이어가 속한 sector가 바뀔 때 발생/수신하는 이벤트입니다.")]
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;

    [Tooltip("StartSector가 준비되었음을 알리는 이벤트입니다. 초기 카메라/플레이어 위치 세팅에서 사용할 수 있습니다.")]
    [SerializeField] private SectorRuntimeEventChannelSO _startSectorReadyEvent;

    [Tooltip("sector가 opened 상태가 되었을 때 발생시키는 이벤트입니다. portal refresh나 UI 갱신에서 구독합니다.")]
    [SerializeField] private SectorRuntimeEventChannelSO _sectorOpenedEvent;

    [Tooltip("현재 stage index가 적용되었음을 알리는 이벤트입니다.")]
    [SerializeField] private IntEventChannelSO _stageAppliedEvent;

    [Tooltip("SectorStateManager가 초기화되어 다른 시스템에서 참조 가능해졌음을 알리는 이벤트입니다.")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    public int CurrentStage { get; private set; }
    public SectorRuntime StartSector { get; private set; }
    public SectorRuntime CurrentSector { get; private set; }

    public IReadOnlyList<SectorRuntime> Sectors => _runtimeSectors;
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

    private void OnEnable()
    {
        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.OnEventRaised += HandleCurrentSectorChanged;

        if (_requestUnlockStageEvent != null)
            _requestUnlockStageEvent.OnEventRaised += ApplyStage;

        if (_requestProgressNextStageEvent != null)
            _requestProgressNextStageEvent.OnEventRaised += ProgressNextStage;

        EnsureInitialized();

    }

    private void OnDisable()
    {
        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.OnEventRaised -= HandleCurrentSectorChanged;

        if (_requestUnlockStageEvent != null)
            _requestUnlockStageEvent.OnEventRaised -= ApplyStage;

        if (_requestProgressNextStageEvent != null)
            _requestProgressNextStageEvent.OnEventRaised -= ProgressNextStage;

        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.Clear(this);
        if (_currentSectorChangedEvent != null && CurrentSector != null)
            _currentSectorChangedEvent.Clear(CurrentSector);

        CurrentSector = null;
    }
    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        if (!_stageMapConfigured)
        {
            CurrentStage = Mathf.Max(0, _initialStage);
            ApplyInitialOpenState();
        }
        else if (CurrentSector == null)
        {
            CurrentSector = StartSector;
        }

        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.RaiseEvent(this);

        if (_startSectorReadyEvent != null && StartSector != null)
            _startSectorReadyEvent.RaiseEvent(StartSector);

        if (CurrentSector == null)
            CurrentSector = StartSector;

        if (_currentSectorChangedEvent != null && CurrentSector != null)
            _currentSectorChangedEvent.RaiseEvent(CurrentSector);
    }

    public void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        BuildSectorTable();
        _isInitialized = true;
    }

    private void BuildSectorTable()
    {
        _failedSectors.Clear();
        _sectorByCoord.Clear();
        _coordBySector.Clear();
        _runtimeSectors.Clear();
        _activeStageSectors.Clear();
        _roomByCoord.Clear();
        _roomBySector.Clear();
        StartSector = null;
        CurrentSector = null;
        CurrentStageMapLayout = null;
        _stageMapConfigured = false;
        _allowStartSectorAccess = false;

        for (int i = 0; i < _sectors.Length; i++)
        {
            SectorEntry entry = _sectors[i];

            if (entry.sector == null)
            {
                Debug.LogWarning($"[SectorStateManager] Sector entry {i} has no SectorRuntime.");
                continue;
            }

            if (_sectorByCoord.ContainsKey(entry.coord))
            {
                Debug.LogWarning($"[SectorStateManager] Duplicate coord detected: {entry.coord}. Sector: {entry.sector.name}");
                continue;
            }

            if (_coordBySector.ContainsKey(entry.sector))
            {
                Debug.LogWarning($"[SectorStateManager] Duplicate sector detected: {entry.sector.name}");
                continue;
            }

            _sectorByCoord.Add(entry.coord, entry.sector);
            _coordBySector.Add(entry.sector, entry.coord);
            _runtimeSectors.Add(entry.sector);

            bool isStartSector = entry.coord == _startSectorCoord;

            entry.sector.SetRuntimeInfo(
                entry.coord,
                opened: false,
                isStartSector: isStartSector
            );

            if (isStartSector)
            {
                if (StartSector != null)
                    Debug.LogWarning("[SectorStateManager] Multiple start sectors found.");

                StartSector = entry.sector;
            }
        }

        if (StartSector == null)
            Debug.LogWarning($"[SectorStateManager] Start sector not found. Coord: {_startSectorCoord}");
    }

    private void ApplyInitialOpenState()
    {
        CloseAllSectors();

        if (_openOnlyStartSectorOnBoot)
        {
            if (StartSector != null)
                OpenSector(StartSector);

            return;
        }

        ApplyStage(CurrentStage);
    }

    private void CloseAllSectors()
    {
        for (int i = 0; i < _runtimeSectors.Count; i++)
        {
            if (_runtimeSectors[i] != null)
                _runtimeSectors[i].SetOpened(false);
        }
    }

    public void ProgressNextStage()
    {
        ApplyStage(CurrentStage + 1);
    }

    public void ApplyStage(int stage)
    {
        EnsureInitialized();

        CurrentStage = Mathf.Max(0, stage);

        if (_useStageSectorUnlocks)
        {
            _stageMapConfigured = false;

            if (StartSector != null)
                OpenSector(StartSector);

            ApplyStageUnlocksUpTo(CurrentStage);
        }

        if (_stageAppliedEvent != null)
            _stageAppliedEvent.RaiseEvent(CurrentStage);
    }

    public void ConfigureStageMap(
        int stage,
        int roomGridSize,
        bool useStartSectorOnly)
    {
        EnsureInitialized();

        CurrentStage = Mathf.Max(0, stage);
        CurrentStageMapLayout = null;
        _stageMapConfigured = true;
        _activeStageSectors.Clear();
        _roomByCoord.Clear();
        _roomBySector.Clear();

        SectorRuntime preferredEntry = CurrentSector;
        _allowStartSectorAccess =
            useStartSectorOnly ||
            preferredEntry == StartSector;

        CloseAllSectors();

        if (useStartSectorOnly)
        {
            if (StartSector != null)
            {
                StartSector.SetCleared(false);
                _activeStageSectors.Add(StartSector);
                SetSectorOpened(StartSector);
            }

            return;
        }

        int gridSize = Mathf.Max(0, roomGridSize);
        int foundRoomCount = 0;

        for (int i = 0; i < _runtimeSectors.Count; i++)
        {
            SectorRuntime sector = _runtimeSectors[i];

            if (sector == null || sector == StartSector)
                continue;

            sector.SetCleared(false);

            if (!TryGetSectorCoord(sector, out Vector2Int coord))
                continue;

            bool isInsideStageGrid =
                coord.x >= 0 &&
                coord.y >= 0 &&
                coord.x < gridSize &&
                coord.y < gridSize;

            if (!isInsideStageGrid)
                continue;

            _activeStageSectors.Add(sector);
            foundRoomCount++;
        }

        if (_allowStartSectorAccess && StartSector != null)
            SetSectorOpened(StartSector);

        SectorRuntime entrySector =
            preferredEntry != null && _activeStageSectors.Contains(preferredEntry)
                ? preferredEntry
                : null;

        if (entrySector == null &&
            _sectorByCoord.TryGetValue(Vector2Int.zero, out SectorRuntime firstSector) &&
            _activeStageSectors.Contains(firstSector))
        {
            entrySector = firstSector;
        }

        if (entrySector != null)
            SetSectorOpened(entrySector);

        int expectedRoomCount = gridSize * gridSize;

        if (foundRoomCount < expectedRoomCount)
        {
            Debug.LogWarning(
                $"[SectorStateManager] Stage {CurrentStage} requests a {gridSize} x {gridSize} map, " +
                $"but only {foundRoomCount}/{expectedRoomCount} matching sectors exist in the scene.",
                this);
        }
    }

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
            sector.SetCleared(false);

            if (room.isOpened && !room.isLocked)
                SetSectorOpened(sector);
        }

        CurrentSector = StartSector;

        if (_startSectorReadyEvent != null && StartSector != null)
            _startSectorReadyEvent.RaiseEvent(StartSector);

        if (_currentSectorChangedEvent != null && CurrentSector != null)
            _currentSectorChangedEvent.RaiseEvent(CurrentSector);
    }

    private void ApplyStageUnlocksUpTo(int stage)
    {
        if (_stageSectorUnlock == null)
        {
            Debug.LogWarning("[SectorStateManager] StageSectorUnlockSO is missing.");
            return;
        }

        for (int stageIndex = 0; stageIndex <= stage; stageIndex++)
        {
            if (!_stageSectorUnlock.TryGetStep(stageIndex, out StageSectorUnlockSO.StageUnlockStep step))
                continue;

            if (step == null || step.sectorCoordsToOpen == null)
                continue;

            for (int i = 0; i < step.sectorCoordsToOpen.Length; i++)
            {
                Vector2Int coord = step.sectorCoordsToOpen[i];

                if (coord == _startSectorCoord)
                    continue;

                if (_sectorByCoord.TryGetValue(coord, out SectorRuntime sector))
                    OpenSector(sector);
                else
                    Debug.LogWarning($"[SectorStateManager] Unlock coord has no sector: {coord}");
            }
        }
    }

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
    }

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

        return true;
    }

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

    private void OpenSectorAt(Vector2Int coord)
    {
        if (_sectorByCoord.TryGetValue(coord, out SectorRuntime sector))
            OpenSector(sector);
    }

    private void HandleCurrentSectorChanged(SectorRuntime sector)
    {
        if (sector == null)
            return;

        CurrentSector = sector;
        RevealSector(sector);
    }

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

        return changed;
    }

    public bool TryGetSector(Vector2Int coord, out SectorRuntime sector)
    {
        EnsureInitialized();
        return _sectorByCoord.TryGetValue(coord, out sector);
    }

    public bool TryGetSectorCoord(SectorRuntime sector, out Vector2Int coord)
    {
        EnsureInitialized();

        if (sector != null)
            return _coordBySector.TryGetValue(sector, out coord);

        coord = default;
        return false;
    }

    public bool IsManagedSector(SectorRuntime sector)
    {
        EnsureInitialized();
        return sector != null && _coordBySector.ContainsKey(sector);
    }

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

    public Vector2Int GetSectorCoord(SectorRuntime sector)
    {
        EnsureInitialized();

        if (sector != null && _coordBySector.TryGetValue(sector, out Vector2Int coord))
            return coord;

        return sector != null ? sector.coord : default;
    }

    public bool IsStartSector(SectorRuntime sector)
    {
        return TryGetSectorCoord(sector, out Vector2Int coord) &&
               coord == _startSectorCoord;
    }

    public bool IsStartSectorCoord(Vector2Int coord)
    {
        return coord == _startSectorCoord;
    }

    public bool IsOpened(SectorRuntime sector)
    {
        return sector != null && sector.IsOpened;
    }

    public bool IsOpened(Vector2Int coord)
    {
        EnsureInitialized();

        return _sectorByCoord.TryGetValue(coord, out SectorRuntime sector) &&
               sector.IsOpened;
    }

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

        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.RaiseEvent(sector);

        return true;
    }

    public bool IsSectorFailed(SectorRuntime sector)
    {
        EnsureInitialized();
        return sector != null && _failedSectors.Contains(sector);
    }

    public void ClearSectorFailure(SectorRuntime sector)
    {
        EnsureInitialized();

        if (sector == null)
            return;

        _failedSectors.Remove(sector);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_sectors == null)
            return;

        for (int i = 0; i < _sectors.Length; i++)
        {
            if (_sectors[i].sector == null)
                continue;

            _sectors[i].sector.SetRuntimeInfo(
                _sectors[i].coord,
                opened: _sectors[i].sector.isOpened,
                isStartSector: _sectors[i].coord == _startSectorCoord
            );
        }
    }
#endif
}
