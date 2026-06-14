using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TreasureRoomRewardSpawner : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("Treasure reward table used when the player enters a Treasure room.")]
    [SerializeField] private TreasureRoomDropTableSO _dropTable;

    [Tooltip("If true, the Treasure room is marked cleared immediately after its reward is spawned so portals can be used.")]
    [SerializeField] private bool _completeRoomAfterRewardSpawn = true;

    [Tooltip("If true, the Treasure room is still marked cleared when the reward table is empty or misconfigured.")]
    [SerializeField] private bool _completeRoomWhenRewardUnavailable = true;

    [Header("Spawn Point")]
    [Tooltip("Preferred spawn point usage for Treasure rewards.")]
    [SerializeField] private SectorObjectSpawnPointUsage _spawnPointUsage =
        SectorObjectSpawnPointUsage.SectorObject;

    [Tooltip("Preferred spawn point tag. Empty accepts any tag.")]
    [SerializeField] private string _spawnPointTag = "Treasure";

    [Tooltip("If no tagged point is found, retry with any spawn point that supports the selected usage.")]
    [SerializeField] private bool _fallbackToAnyMatchingUsagePoint = true;

    [Tooltip("If no object spawn point exists, use SectorRuntime.PlayerStartPoint.")]
    [SerializeField] private bool _fallbackToPlayerStartPoint = true;

    [Tooltip("World offset added after resolving the spawn point.")]
    [SerializeField] private Vector3 _spawnWorldOffset = Vector3.zero;

    [Tooltip("Optional parent for spawned rewards. Empty parents under the active sector PatternObjectRoot.")]
    [SerializeField] private Transform _rewardRoot;

    [Header("Listening To")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;

    [Header("Debug")]
    [SerializeField] private bool _logWarnings = true;

    private readonly HashSet<Vector2Int> _spawnedTreasureCoords = new HashSet<Vector2Int>();
    private readonly List<Transform> _spawnPointBuffer = new List<Transform>();
    private SectorStateManager _sectorStateManager;
    private int _lastStageSeed = int.MinValue;

    private void Awake()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();
    }

    private void OnEnable()
    {
        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

        if (_currentSectorChangedEvent != null)
        {
            _currentSectorChangedEvent.OnEventRaised += HandleCurrentSectorChanged;

            if (_currentSectorChangedEvent.HasCurrent)
                HandleCurrentSectorChanged(_currentSectorChangedEvent.Current);
        }
    }

    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.OnEventRaised -= HandleCurrentSectorChanged;
    }

    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        SyncStageSeedCache();

        if (_sectorStateManager.CurrentSector != null)
            TrySpawnTreasureReward(_sectorStateManager.CurrentSector);
    }

    private void HandleCurrentSectorChanged(SectorRuntime sector)
    {
        TrySpawnTreasureReward(sector);
    }

    private void TrySpawnTreasureReward(SectorRuntime sector)
    {
        if (sector == null || _dropTable == null || _sectorStateManager == null)
            return;

        SyncStageSeedCache();

        if (!_sectorStateManager.TryGetStageRoomType(
                sector,
                out StageRoomType roomType) ||
            roomType != StageRoomType.Treasure)
        {
            return;
        }

        Vector2Int coord = sector.Coord;

        if (_spawnedTreasureCoords.Contains(coord))
            return;

        int seed = ResolveRewardSeed(coord);

        if (!_dropTable.TryRollReward(seed, out TreasureRoomReward reward))
        {
            LogWarning($"Failed to roll Treasure reward. sector={sector.name}, coord={coord}");
            _spawnedTreasureCoords.Add(coord);
            CompleteTreasureRoomIfConfigured(sector, _completeRoomWhenRewardUnavailable);
            return;
        }

        if (reward.PickupPrefab == null)
        {
            LogWarning($"Treasure reward has no pickup prefab. sector={sector.name}, coord={coord}");
            _spawnedTreasureCoords.Add(coord);
            CompleteTreasureRoomIfConfigured(sector, _completeRoomWhenRewardUnavailable);
            return;
        }

        Transform spawnPoint = ResolveSpawnPoint(sector, seed);
        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : sector.transform.position;

        Quaternion spawnRotation = spawnPoint != null
            ? spawnPoint.rotation
            : Quaternion.identity;

        Transform parent = _rewardRoot != null
            ? _rewardRoot
            : sector.PatternObjectRoot;

        GameObject instance = Instantiate(
            reward.PickupPrefab,
            spawnPosition + _spawnWorldOffset,
            spawnRotation,
            parent);

        if (instance == null)
            return;

        TreasureRoomRewardPickup pickup =
            instance.GetComponent<TreasureRoomRewardPickup>();

        if (pickup == null)
            pickup = instance.AddComponent<TreasureRoomRewardPickup>();

        pickup.Initialize(reward);
        _spawnedTreasureCoords.Add(coord);

        CompleteTreasureRoomIfConfigured(sector, _completeRoomAfterRewardSpawn);
    }

    private void CompleteTreasureRoomIfConfigured(SectorRuntime sector, bool shouldComplete)
    {
        if (!shouldComplete || sector == null || _sectorStateManager == null)
            return;

        _sectorStateManager.CompleteSector(sector);
    }

    private Transform ResolveSpawnPoint(SectorRuntime sector, int seed)
    {
        if (sector == null)
            return null;

        if (TryResolveSpawnPoint(
                sector,
                _spawnPointUsage,
                _spawnPointTag,
                seed,
                out Transform spawnPoint))
        {
            return spawnPoint;
        }

        if (_fallbackToAnyMatchingUsagePoint &&
            TryResolveSpawnPoint(
                sector,
                _spawnPointUsage,
                null,
                seed,
                out spawnPoint))
        {
            return spawnPoint;
        }

        if (_fallbackToPlayerStartPoint)
            return sector.PlayerStartPoint;

        return sector.transform;
    }

    private bool TryResolveSpawnPoint(
        SectorRuntime sector,
        SectorObjectSpawnPointUsage usage,
        string requiredTag,
        int seed,
        out Transform spawnPoint)
    {
        spawnPoint = null;
        _spawnPointBuffer.Clear();

        Transform[] points = sector.ObjectSpawnPoints;

        if (points != null)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Transform point = points[i];

                if (point == null)
                    continue;

                SectorObjectSpawnPoint metadata =
                    point.GetComponent<SectorObjectSpawnPoint>();

                if (metadata != null)
                {
                    if (!metadata.CanUseFor(usage, requiredTag))
                        continue;
                }
                else if (!string.IsNullOrWhiteSpace(requiredTag))
                {
                    continue;
                }

                _spawnPointBuffer.Add(point);
            }
        }

        SectorObjectSpawnPoint[] metadataPoints = sector.ObjectSpawnPointMetadata;

        if (metadataPoints != null)
        {
            for (int i = 0; i < metadataPoints.Length; i++)
            {
                SectorObjectSpawnPoint metadata = metadataPoints[i];

                if (metadata == null ||
                    !metadata.CanUseFor(usage, requiredTag) ||
                    _spawnPointBuffer.Contains(metadata.transform))
                {
                    continue;
                }

                _spawnPointBuffer.Add(metadata.transform);
            }
        }

        if (_spawnPointBuffer.Count <= 0)
            return false;

        int index = new System.Random(seed).Next(0, _spawnPointBuffer.Count);
        spawnPoint = _spawnPointBuffer[index];
        return spawnPoint != null;
    }

    private int ResolveRewardSeed(Vector2Int coord)
    {
        int stageSeed = _sectorStateManager != null &&
                        _sectorStateManager.CurrentStageMapLayout != null
            ? _sectorStateManager.CurrentStageMapLayout.stageSeed
            : 0;

        return StageBattleSettingsSO.BuildSectorSeed(stageSeed, coord, 1703);
    }

    private void SyncStageSeedCache()
    {
        int stageSeed = _sectorStateManager != null &&
                        _sectorStateManager.CurrentStageMapLayout != null
            ? _sectorStateManager.CurrentStageMapLayout.stageSeed
            : int.MinValue;

        if (_lastStageSeed == stageSeed)
            return;

        _lastStageSeed = stageSeed;
        _spawnedTreasureCoords.Clear();
    }

    private void LogWarning(string message)
    {
        if (!_logWarnings)
            return;

        Debug.LogWarning($"[TreasureRoomRewardSpawner] {message}", this);
    }
}
