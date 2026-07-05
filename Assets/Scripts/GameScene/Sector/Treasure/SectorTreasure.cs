using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SectorTreasure : MonoBehaviour
{
    [Header("Rules")]
    [SerializeField] private StageTreasureSettingsSO _treasureSettings;

    [Header("Refs")]
    [SerializeField] private SectorRuntime _sector;
    

    [Header("Listening")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedChannel;
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _playerInventoryRuntimeReadyChannel;

    [Header("Reward Spawn")]
    [SerializeField] private Transform _rewardSpawnPoint;
    [SerializeField] private Transform _rewardRoot;
    [SerializeField] private Vector3 _rewardWorldOffset = Vector3.zero;
    [SerializeField] private Collider _interactionCollider;

    [Header("Reward Rarity VFX")]
    [SerializeField] private GameObject _normalRewardParticlePrefab;
    [SerializeField] private GameObject _rareRewardParticlePrefab;
    [SerializeField] private GameObject _uniqueRewardParticlePrefab;
    [SerializeField] private GameObject _legendaryRewardParticlePrefab;

    [Header("Seed")]
    [SerializeField] private int _rewardSeedSalt = 7319;

    [Header("Lifecycle")]
    [SerializeField] private bool _spawnOnlyOnce = true;
    [SerializeField] private bool _destroyPreviousRewardOnRespawn = true;
    [SerializeField] private bool _pollCurrentSectorWhenNoEventChannel = true;
    [SerializeField, Min(0.05f)] private float _pollInterval = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool _logWarnings = true;
    private PlayerInventoryRuntime _playerInventory;
    private SectorStateManager _sectorStateManager;
    private GameObject _spawnedReward;
    private GameObject _spawnedRewardParticle;
    private TreasureRoomReward _reward;
    private bool _hasSpawnedReward;
    private float _pollTimer;
    private readonly List<PassiveItemSO> _ownedPassiveScratch = new();

    public GameObject SpawnedReward => _spawnedReward;
    public TreasureRoomReward Reward => _reward;
    public bool HasSpawnedReward => _hasSpawnedReward;

    private void OnEnable()
    {
        ResolveRefs();

        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

        if (_currentSectorChangedChannel != null)
        {
            _currentSectorChangedChannel.OnEventRaised += HandleCurrentSectorChanged;

            if (_currentSectorChangedChannel.HasCurrent)
                HandleCurrentSectorChanged(_currentSectorChangedChannel.Current);
        }

        if (_playerInventoryRuntimeReadyChannel != null)
        {
            _playerInventoryRuntimeReadyChannel.OnEventRaised += HandlePlayerInventoryRuntimeReady;

            if (_playerInventoryRuntimeReadyChannel.HasCurrent)
                HandlePlayerInventoryRuntimeReady(_playerInventoryRuntimeReadyChannel.Current);
        }
    }

    private void Start()
    {
        ResolveRefs();

        if (_sectorStateManager != null)
            TrySpawnForCurrentSector(_sectorStateManager.CurrentSector);
    }

    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        if (_currentSectorChangedChannel != null)
            _currentSectorChangedChannel.OnEventRaised -= HandleCurrentSectorChanged;

        if (_playerInventoryRuntimeReadyChannel != null)
            _playerInventoryRuntimeReadyChannel.OnEventRaised -= HandlePlayerInventoryRuntimeReady;
    }

    private void Update()
    {
        if (!_pollCurrentSectorWhenNoEventChannel ||
            _currentSectorChangedChannel != null ||
            (_spawnOnlyOnce && _hasSpawnedReward))
        {
            return;
        }

        _pollTimer -= Time.deltaTime;

        if (_pollTimer > 0f)
            return;

        _pollTimer = _pollInterval;

        ResolveRefs();

        if (_sectorStateManager != null)
            TrySpawnForCurrentSector(_sectorStateManager.CurrentSector);
    }

    public bool TrySpawnNow()
    {
        ResolveRefs();

        if (_sector == null)
        {
            LogWarning("SectorRuntime is missing.");
            return false;
        }

        return TryRollAndSpawnReward();
    }

    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        TrySpawnForCurrentSector(manager.CurrentSector);
    }

    private void HandleCurrentSectorChanged(SectorRuntime currentSector)
    {
        TrySpawnForCurrentSector(currentSector);
    }

    private void HandlePlayerInventoryRuntimeReady(PlayerInventoryRuntime inventoryRuntime)
    {
        if (inventoryRuntime != null)
            _playerInventory = inventoryRuntime;
    }

    private void TrySpawnForCurrentSector(SectorRuntime currentSector)
    {
        ResolveRefs();

        if (_sector == null || currentSector == null)
            return;

        if (currentSector != _sector)
            return;

        if (_spawnOnlyOnce && _hasSpawnedReward)
            return;

        if (!IsTreasureRoom())
            return;

        TryRollAndSpawnReward();
    }

    private bool TryRollAndSpawnReward()
    {
        if (_spawnOnlyOnce && _hasSpawnedReward)
            return false;

        if (_treasureSettings == null)
        {
            LogWarning("StageTreasureSettingsSO is missing.");
            SetInteractionEnabled(false);
            return false;
        }

        int stageIndex = _sectorStateManager != null
            ? _sectorStateManager.CurrentStage
            : 0;

        Vector2Int coord = ResolveSectorCoord();
        int seed = ResolveRewardSeed(stageIndex, coord);
        IReadOnlyList<PassiveItemSO> ownedPassiveItems = ResolveOwnedPassiveItems();

        if (!_treasureSettings.TryRollReward(
                stageIndex,
                seed,
                ownedPassiveItems,
                out TreasureRoomReward reward))
        {
            LogWarning($"Failed to roll Treasure reward. stage={stageIndex}, coord={coord}");
            SetInteractionEnabled(false);
            return false;
        }

        if (!SpawnRewardPickup(reward))
        {
            LogWarning($"Failed to spawn Treasure reward pickup. stage={stageIndex}, coord={coord}");
            SetInteractionEnabled(false);
            return false;
        }

        _reward = reward;
        _hasSpawnedReward = true;

        SetInteractionEnabled(true);
        return true;
    }

    private bool SpawnRewardPickup(TreasureRoomReward reward)
    {
        if (reward.PickupPrefab == null)
            return false;

        if (_destroyPreviousRewardOnRespawn && _spawnedReward != null)
        {
            UnbindSpawnedRewardPickup();
            Destroy(_spawnedReward);
            _spawnedReward = null;
        }

        ClearRewardParticle();

        Transform spawnPoint = _rewardSpawnPoint != null
            ? _rewardSpawnPoint
            : transform;

        Transform parent = _rewardRoot != null
            ? _rewardRoot
            : spawnPoint;

        _spawnedReward = Instantiate(
            reward.PickupPrefab,
            spawnPoint.position + _rewardWorldOffset,
            Quaternion.identity,
            parent);

        if (_spawnedReward == null)
            return false;

        TreasureRoomRewardPickup pickup =
            _spawnedReward.GetComponent<TreasureRoomRewardPickup>();

        if (pickup == null)
            pickup = _spawnedReward.AddComponent<TreasureRoomRewardPickup>();

        pickup.Initialize(reward);
        pickup.PickedUp += HandleRewardPickedUp;

        SpawnRewardParticle(reward, _spawnedReward.transform);
        return true;
    }

    private bool IsTreasureRoom()
    {
        if (_sector == null)
            return false;

        if (_sectorStateManager == null)
            return true;

        if (!_sectorStateManager.TryGetStageRoomType(_sector, out StageRoomType roomType))
            return true;

        return roomType == StageRoomType.Treasure;
    }

    private Vector2Int ResolveSectorCoord()
    {
        if (_sectorStateManager != null &&
            _sectorStateManager.TryGetSectorCoord(_sector, out Vector2Int coord))
        {
            return coord;
        }

        return _sector != null ? _sector.Coord : default;
    }

    private int ResolveRewardSeed(int stageIndex, Vector2Int coord)
    {
        int stageSeed = stageIndex;

        if (_sectorStateManager != null &&
            _sectorStateManager.CurrentStageMapLayout != null)
        {
            stageSeed = _sectorStateManager.CurrentStageMapLayout.stageSeed;
        }

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + stageSeed;
            hash = hash * 31 + stageIndex;
            hash = hash * 31 + coord.x;
            hash = hash * 31 + coord.y;
            hash = hash * 31 + _rewardSeedSalt;
            return hash;
        }
    }

    private void ResolveRefs()
    {
        if (_sector == null)
            _sector = GetComponentInParent<SectorRuntime>();

        if (_interactionCollider == null)
            _interactionCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>(true);

        if (_sectorStateManager == null &&
            _sectorStateManagerReadyChannel != null &&
            _sectorStateManagerReadyChannel.HasCurrent)
        {
            _sectorStateManager = _sectorStateManagerReadyChannel.Current;
        }

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_playerInventory == null &&
            _playerInventoryRuntimeReadyChannel != null &&
            _playerInventoryRuntimeReadyChannel.HasCurrent)
        {
            _playerInventory = _playerInventoryRuntimeReadyChannel.Current;
        }

        if (_playerInventory == null && _playerInventoryRuntimeReadyChannel == null)
            _playerInventory = FindAnyObjectByType<PlayerInventoryRuntime>();
    }

    private IReadOnlyList<PassiveItemSO> ResolveOwnedPassiveItems()
    {
        _ownedPassiveScratch.Clear();

        if (_playerInventory != null)
            _playerInventory.GetPassiveItems(_ownedPassiveScratch);

        return _ownedPassiveScratch;
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (_interactionCollider != null)
            _interactionCollider.enabled = enabled && _spawnedReward != null;
    }

    private void HandleRewardPickedUp(TreasureRoomRewardPickup pickup)
    {
        if (pickup != null)
            pickup.PickedUp -= HandleRewardPickedUp;

        _spawnedReward = null;
        SetInteractionEnabled(false);
        ClearRewardParticle();
    }

    private void UnbindSpawnedRewardPickup()
    {
        if (_spawnedReward == null)
            return;

        TreasureRoomRewardPickup pickup =
            _spawnedReward.GetComponent<TreasureRoomRewardPickup>();

        if (pickup != null)
            pickup.PickedUp -= HandleRewardPickedUp;
    }

    private void SpawnRewardParticle(TreasureRoomReward reward, Transform parent)
    {
        GameObject prefab = ResolveRewardParticlePrefab(reward.Rarity);

        if (prefab == null || parent == null)
            return;

        _spawnedRewardParticle = Instantiate(prefab, parent);

        _spawnedRewardParticle.transform.localPosition = Vector3.zero;
        _spawnedRewardParticle.transform.localRotation = Quaternion.identity;

        ParticleSystem[] particleSystems =
            _spawnedRewardParticle.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Play(false);
        }
    }

    private GameObject ResolveRewardParticlePrefab(PlayerShopItemRarity rarity)
    {
        return rarity switch
        {
            PlayerShopItemRarity.Rare => _rareRewardParticlePrefab,
            PlayerShopItemRarity.Unique => _uniqueRewardParticlePrefab,
            PlayerShopItemRarity.Legendary => _legendaryRewardParticlePrefab,
            _ => _normalRewardParticlePrefab
        };
    }

    private void ClearRewardParticle()
    {
        if (_spawnedRewardParticle == null)
            return;

        Destroy(_spawnedRewardParticle);
        _spawnedRewardParticle = null;
    }

    private void LogWarning(string message)
    {
        if (!_logWarnings)
            return;

        Debug.LogWarning($"[SectorTreasure] {message}", this);
    }
}
