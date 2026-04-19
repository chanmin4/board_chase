using System;
using System.Collections.Generic;
using UnityEngine;

public class SectorStateManager : MonoBehaviour
{
    [Serializable]
    public struct SectorEntry
    {
        [Tooltip("SectorRuntime placed in the scene.")]
        public SectorRuntime sector;

        [Tooltip("Sector coord. Vector2Int.x = X, Vector2Int.y = Z.")]
        public Vector2Int coord;
    }

    [Header("Sector Table")]
    [SerializeField] private SectorEntry[] _sectors;

    [Header("Stage Unlock")]
    [SerializeField] private StageSectorUnlockSO _stageSectorUnlock;

    [Header("Start Sector")]
    [SerializeField] private Vector2Int _startSectorCoord = new Vector2Int(-1, 0);

    [Header("Start")]
    [SerializeField] private int _initialStage = 0;
    [SerializeField] private bool _openOnlyStartSectorOnBoot = true;

    [Header("Requests")]
    [SerializeField] private IntEventChannelSO _requestUnlockStageEvent;
    [SerializeField] private VoidEventChannelSO _requestProgressNextStageEvent;

    [Header("Broadcasts")]
    [SerializeField] private SectorRuntimeEventChannelSO _startSectorReadyEvent;
    [SerializeField] private SectorRuntimeEventChannelSO _sectorOpenedEvent;
    [SerializeField] private IntEventChannelSO _stageAppliedEvent;

    public int CurrentStage { get; private set; }
    public SectorRuntime StartSector { get; private set; }

    public IReadOnlyList<SectorRuntime> Sectors => _runtimeSectors;

    private readonly Dictionary<Vector2Int, SectorRuntime> _sectorByCoord = new();
    private readonly Dictionary<SectorRuntime, Vector2Int> _coordBySector = new();
    private readonly List<SectorRuntime> _runtimeSectors = new();

    private bool _isInitialized;

    private void OnEnable()
    {
        if (_requestUnlockStageEvent != null)
            _requestUnlockStageEvent.OnEventRaised += ApplyStage;

        if (_requestProgressNextStageEvent != null)
            _requestProgressNextStageEvent.OnEventRaised += ProgressNextStage;
    }

    private void OnDisable()
    {
        if (_requestUnlockStageEvent != null)
            _requestUnlockStageEvent.OnEventRaised -= ApplyStage;

        if (_requestProgressNextStageEvent != null)
            _requestProgressNextStageEvent.OnEventRaised -= ProgressNextStage;
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        CurrentStage = Mathf.Max(0, _initialStage);
        ApplyInitialOpenState();

        if (_startSectorReadyEvent != null && StartSector != null)
            _startSectorReadyEvent.RaiseEvent(StartSector);
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
        _sectorByCoord.Clear();
        _coordBySector.Clear();
        _runtimeSectors.Clear();
        StartSector = null;

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

        if (StartSector != null)
            OpenSector(StartSector);

        ApplyStageUnlocksUpTo(CurrentStage);

        if (_stageAppliedEvent != null)
            _stageAppliedEvent.RaiseEvent(CurrentStage);
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

        bool wasOpened = sector.IsOpened;
        sector.SetOpened(true);

        if (!wasOpened && _sectorOpenedEvent != null)
            _sectorOpenedEvent.RaiseEvent(sector);
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
