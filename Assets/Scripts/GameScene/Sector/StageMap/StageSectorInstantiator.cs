// Assets/Scripts/GameScene/Sector/StageMap/StageSectorInstantiator.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class StageSectorInstantiator : MonoBehaviour
{
    [Header("Seed")]
    [Tooltip("If enabled, a new run seed is generated when this component awakes.")]
    [SerializeField] private bool _randomizeRunSeedOnAwake = true;

    [Tooltip("Run seed used to generate deterministic stage maps when randomize is disabled.")]
    [SerializeField] private int _runSeed = 1;

    [Header("Stage Coord")]
    [Tooltip("Logical coord of the generated StartSector. Current design uses (-1, 0).")]
    [SerializeField] private Vector2Int _startSectorCoord = new Vector2Int(-1, 0);

    [Header("Prefabs")]
    [Tooltip("Prefab used for the generated StartSector.")]
    [SerializeField] private SectorRuntime _startSectorPrefab;

    [Tooltip("Prefab used for NormalBattle rooms.")]
    [SerializeField] private SectorRuntime _normalBattleSectorPrefab;

    [Tooltip("Prefab used for Treasure rooms. Falls back to NormalBattle when empty.")]
    [SerializeField] private SectorRuntime _treasureSectorPrefab;

    [Tooltip("Prefab used for Shop rooms. Falls back to NormalBattle when empty.")]
    [SerializeField] private SectorRuntime _shopSectorPrefab;

    [Tooltip("Prefab used for Named goal rooms. Falls back to NormalBattle when empty.")]
    [SerializeField] private SectorRuntime _namedSectorPrefab;

    [Tooltip("Prefab used for Boss goal rooms. Falls back to Named, then NormalBattle when empty.")]
    [SerializeField] private SectorRuntime _bossSectorPrefab;

    [Tooltip("Prefab used for BigMonsterWave goal rooms. Falls back to NormalBattle when empty.")]
    [SerializeField] private SectorRuntime _bigMonsterWaveSectorPrefab;

    [Header("Placement")]
    [Tooltip("Parent transform for generated sectors. Uses this transform when empty.")]
    [SerializeField] private Transform _generatedRoot;

    [Tooltip("World position for logical room (0,0). Uses this transform when empty.")]
    [SerializeField] private Transform _gridOrigin;

    [Tooltip("World spacing between generated logical rooms. X is world X spacing, Y is world Z spacing.")]
    [SerializeField] private Vector2 _roomSpacingXZ = new Vector2(40f, 40f);

    [Header("Refs")]
    [Tooltip("Stage-specific Treasure room generation and reward settings.")]
    [SerializeField] private StageTreasureSettingsSO _stageTreasureSettings;

    [Tooltip("Stage-specific Shop room generation and inventory settings.")]
    [SerializeField] private StageShopSettingsSO _stageShopSettings;

    [Tooltip("Receives generated sectors and owns opened/cleared/current sector state.")]
    [SerializeField] private SectorStateManager _sectorStateManager;

    [Tooltip("Rebuilds portal links after generated sectors are registered.")]
    [SerializeField] private SectorPortalManager _sectorPortalManager;

    [Tooltip("Optional player anchor. If the player is parented under a generated sector, it is detached before sectors are rebuilt.")]
    [SerializeField] private TransformAnchor _playerTransformAnchor;

    private readonly List<SectorRuntime> _generatedSectors = new();

    public StageMapLayout CurrentLayout { get; private set; }
    public int RunSeed => _runSeed;

    private void Awake()
    {
        if (_randomizeRunSeedOnAwake)
            _runSeed = Random.Range(int.MinValue, int.MaxValue);

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorPortalManager == null)
            _sectorPortalManager = FindAnyObjectByType<SectorPortalManager>();
    }

    public bool BuildStage(StageProgressionRulesSO.StageProgressRule rule)
    {
        return BuildStage(rule, _sectorStateManager);
    }

    public bool BuildStage(
        StageProgressionRulesSO.StageProgressRule rule,
        SectorStateManager sectorStateManager)
    {
        return BuildStage(
            rule,
            sectorStateManager,
            consecutiveNoHitStageCount: 0);
    }

    public bool BuildStage(
        StageProgressionRulesSO.StageProgressRule rule,
        SectorStateManager sectorStateManager,
        int consecutiveNoHitStageCount)
    {
        if (rule == null)
        {
            Debug.LogError("[StageSectorInstantiator] Stage rule is missing.", this);
            return false;
        }

        if (sectorStateManager == null)
        {
            Debug.LogError("[StageSectorInstantiator] SectorStateManager is missing.", this);
            return false;
        }

        if (_startSectorPrefab == null)
        {
            Debug.LogError("[StageSectorInstantiator] StartSector prefab is missing.", this);
            return false;
        }

        bool startOnly = rule.useStartSectorOnly || rule.roomGridSize <= 0;
        int gridSize = startOnly ? 0 : Mathf.Max(1, rule.roomGridSize);

        if (!startOnly && _normalBattleSectorPrefab == null)
        {
            Debug.LogError("[StageSectorInstantiator] NormalBattle sector prefab is missing.", this);
            return false;
        }

        _sectorStateManager = sectorStateManager;

        ClearGeneratedSectors();

        CurrentLayout = StageMapGenerator.GenerateFullGrid(
            _runSeed,
            rule.stageIndex,
            gridSize,
            _startSectorCoord,
            rule.GoalStageRoomType,
            _stageTreasureSettings != null
                ? _stageTreasureSettings.CreateTreasureRoomGenerationSettings(
                    rule.stageIndex,
                    consecutiveNoHitStageCount)
                : StageTreasureRoomGenerationSettings.Disabled,
            _stageShopSettings != null
                ? _stageShopSettings.CreateShopRoomGenerationSettings(rule.stageIndex)
                : StageShopRoomGenerationSettings.Disabled);

        Vector3 roomStep = ResolveRoomStep();

        for (int i = 0; i < CurrentLayout.rooms.Count; i++)
        {
            StageRoomNode node = CurrentLayout.rooms[i];

            if (node == null || node.roomType == StageRoomType.Empty)
                continue;

            SectorRuntime prefab = ResolvePrefab(node.roomType);

            if (prefab == null)
            {
                Debug.LogError($"[StageSectorInstantiator] No prefab for roomType={node.roomType}.", this);
                ClearGeneratedSectors();
                CurrentLayout = null;
                return false;
            }

            bool isStartSector =
                node.roomType == StageRoomType.Start ||
                node.coord == _startSectorCoord;

            SectorRuntime sector = Instantiate(
                prefab,
                ResolveWorldPosition(node.coord, roomStep),
                prefab.transform.rotation,
                _generatedRoot != null ? _generatedRoot : transform);

            sector.name = $"Stage_{rule.stageIndex}_{node.roomType}_{node.coord.x}_{node.coord.y}";
            sector.SetRuntimeInfo(node.coord, node.isOpened, isStartSector);
            sector.SetCleared(isStartSector);

            _generatedSectors.Add(sector);
        }

        _sectorStateManager.RegisterGeneratedStageMap(CurrentLayout, _generatedSectors);

        if (_sectorPortalManager != null)
            _sectorPortalManager.RebuildPortalLinks(resetStartSectorConsumed: true);

        return true;
    }

    public void ClearGeneratedSectors()
    {
        DetachPlayerFromGeneratedSectors();

        for (int i = _generatedSectors.Count - 1; i >= 0; i--)
        {
            SectorRuntime sector = _generatedSectors[i];

            if (sector == null)
                continue;

            if (Application.isPlaying)
                Destroy(sector.gameObject);
            else
                DestroyImmediate(sector.gameObject);
        }

        _generatedSectors.Clear();
    }

    private void DetachPlayerFromGeneratedSectors()
    {
        if (_playerTransformAnchor == null ||
            !_playerTransformAnchor.isSet ||
            _playerTransformAnchor.Value == null)
        {
            return;
        }

        Transform player = _playerTransformAnchor.Value;

        for (int i = 0; i < _generatedSectors.Count; i++)
        {
            SectorRuntime sector = _generatedSectors[i];

            if (sector == null || !player.IsChildOf(sector.transform))
                continue;

            player.SetParent(null, worldPositionStays: true);
            return;
        }
    }

    private SectorRuntime ResolvePrefab(StageRoomType roomType)
    {
        switch (roomType)
        {
            case StageRoomType.Start:
                return _startSectorPrefab;

            case StageRoomType.Treasure:
                return _treasureSectorPrefab != null
                    ? _treasureSectorPrefab
                    : _normalBattleSectorPrefab;

            case StageRoomType.Shop:
                return _shopSectorPrefab != null
                    ? _shopSectorPrefab
                    : _normalBattleSectorPrefab;

            case StageRoomType.Named:
                return _namedSectorPrefab != null
                    ? _namedSectorPrefab
                    : _normalBattleSectorPrefab;

            case StageRoomType.Boss:
                if (_bossSectorPrefab != null)
                    return _bossSectorPrefab;

                return _namedSectorPrefab != null
                    ? _namedSectorPrefab
                    : _normalBattleSectorPrefab;

            case StageRoomType.BigMonsterWave:
                return _bigMonsterWaveSectorPrefab != null
                    ? _bigMonsterWaveSectorPrefab
                    : _normalBattleSectorPrefab;

            default:
                return _normalBattleSectorPrefab;
        }
    }

    private Vector3 ResolveRoomStep()
    {
        float x = Mathf.Max(0.01f, _roomSpacingXZ.x);
        float z = Mathf.Max(0.01f, _roomSpacingXZ.y);

        return new Vector3(x, 0f, z);
    }

    private Vector3 ResolveWorldPosition(Vector2Int coord, Vector3 roomStep)
    {
        Vector3 origin = _gridOrigin != null
            ? _gridOrigin.position
            : transform.position;

        return origin + new Vector3(
            coord.x * roomStep.x,
            0f,
            coord.y * roomStep.z);
    }
}
