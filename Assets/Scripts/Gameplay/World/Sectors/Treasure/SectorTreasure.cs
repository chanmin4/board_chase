/*
 * Role hierarchy: Gameplay > World > Sectors > Treasure
 * Primary responsibility: Manages SectorTreasure stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WIP legacy Treasure Room chest backed by a one-slot Structure Loot
/// inventory. Treasure rooms are no longer generated; use LootableObject
/// prefabs and the shared Loot Progression pool for new sector content.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("WIP Legacy/Sector Treasure")]
public sealed class SectorTreasure : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorRuntime _sector;

    [Tooltip("Dedicated chest interaction collider. Do not reuse a scaled visual collider.")]
    [SerializeField] private Collider _interactionCollider;
    [SerializeField] private Transform _treasureVisualAnchor;
    [SerializeField] private Transform _placeholderRoot;
    [SerializeField] private Transform _dropOrigin;

    [Header("Loot Hub")]
    [SerializeField] private EnemyLootOpenRequestEventChannelSO _openRequestChannel;
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;
    [SerializeField] private string _lootDisplayName = "Treasure";
    [SerializeField, Min(1)] private int _lootSlotCount = 1;

    [Header("Listening")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;

    [Header("World Visual")]
    [SerializeField] private GameObject _treasureVisualPrefab;
    [SerializeField, Min(0.01f)] private float _treasureVisualScale = 4f;

    [Header("Reward Rarity VFX")]
    [SerializeField] private GameObject _normalRewardParticlePrefab;
    [SerializeField] private GameObject _rareRewardParticlePrefab;
    [SerializeField] private GameObject _uniqueRewardParticlePrefab;
    [SerializeField] private GameObject _legendaryRewardParticlePrefab;

    [Header("Seed")]
    [SerializeField] private int _rewardSeedSalt = 7319;

    [Header("Runtime - Don't Touch")]
    [ReadOnly] [SerializeField] private bool _opened;
    [ReadOnly] [SerializeField] private bool _rollInProgress;

    [Header("Debug")]
    [SerializeField] private bool _logWarnings = true;

    private SectorStateManager _sectorStateManager;
    private TreasureRoomDropTableSO _dropTable;
    private StructureLootInventoryRuntime _lootInventory;
    private GameObject _treasureVisualInstance;
    private GameObject _spawnedRewardParticle;
    private VisionDiscoveredVisibilityTarget _discoveryVisibility;
    private TreasureRoomReward _reward;
    private readonly List<PassiveItemSO> _ownedPassiveScratch = new();

    public bool CanInteract =>
        isActiveAndEnabled &&
        !_rollInProgress &&
        _dropTable != null &&
        _lootInventory != null &&
        IsTreasureRoom();

    public TreasureRoomReward Reward => _reward;
    public bool IsOpened => _opened;
    public bool IsDiscovered => _discoveryVisibility != null && _discoveryVisibility.IsDiscovered;

    // Purpose: Configures this component''s configure.
    public bool Configure(TreasureRoomDropTableSO dropTable)
    {
        if (dropTable == null)
        {
            LogWarning("Cannot configure Treasure because its TreasureRoomDropTableSO is missing.");
            SetInteractionEnabled(false);
            return false;
        }

        _dropTable = dropTable;
        ResolveRefs();
        SetInteractionEnabled(CanInteract);
        return true;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        HidePlaceholder();
        EnsureTreasureVisual();
        RefreshDiscoveryVisibility();
        SetInteractionEnabled(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();

        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

        SetInteractionEnabled(CanInteract);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        ClearRewardParticle();
    }

    // Purpose: Attempts to interact and reports whether the request succeeded.
    public bool TryInteract(Component actor)
    {
        ResolveRefs();

        if (!CanInteract || actor == null)
            return false;

        if (!_opened && !TryRollRewardIntoLoot(actor))
            return false;

        bool opened = _lootInventory.TryInteract(actor);
        SetInteractionEnabled(CanInteract);
        return opened;
    }

    // Purpose: Attempts to roll reward into loot and reports whether the request succeeded.
    private bool TryRollRewardIntoLoot(Component actor)
    {
        _rollInProgress = true;

        int stageIndex = _sectorStateManager != null
            ? _sectorStateManager.CurrentStage
            : 0;
        Vector2Int coord = ResolveSectorCoord();
        int seed = ResolveRewardSeed(stageIndex, coord);

        if (!_dropTable.TryRollReward(
                seed,
                ResolveOwnedPassiveItems(actor),
                out TreasureRoomReward reward) ||
            reward.Item == null)
        {
            _rollInProgress = false;
            LogWarning($"Failed to roll a valid treasure reward. stage={stageIndex}, coord={coord}");
            return false;
        }

        int amount = reward.Kind switch
        {
            TreasureRewardKind.Currency => Mathf.Max(1, reward.ItemAmount),
            TreasureRewardKind.Bullet => Mathf.Max(1, reward.BulletBundleAmount),
            _ => 1
        };
        float durability = reward.ArmorItem != null
            ? reward.ArmorItem.MaxDurability
            : -1f;

        _lootInventory.ClearAllLoot();
        _lootInventory.SetItemStack(0, reward.Item, amount, durability);
        _reward = reward;
        _opened = true;
        _rollInProgress = false;
        SpawnRewardParticle(reward);
        return true;
    }

    // Purpose: Handles the incoming sector state manager ready request or event.
    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager != null)
            _sectorStateManager = manager;

        SetInteractionEnabled(CanInteract);
    }

    // Purpose: Checks whether treasure room is currently true.
    private bool IsTreasureRoom()
    {
        if (_sector == null)
            return false;

        if (_sectorStateManager == null ||
            !_sectorStateManager.TryGetStageRoomType(_sector, out StageRoomType roomType))
        {
            return true;
        }

        return roomType == StageRoomType.Treasure;
    }

    // Purpose: Resolves the sector coord needed by this component.
    private Vector2Int ResolveSectorCoord()
    {
        if (_sectorStateManager != null &&
            _sectorStateManager.TryGetSectorCoord(_sector, out Vector2Int coord))
        {
            return coord;
        }

        return _sector != null ? _sector.Coord : default;
    }

    // Purpose: Resolves the reward seed needed by this component.
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

    // Purpose: Resolves the owned passive items needed by this component.
    private IReadOnlyList<PassiveItemSO> ResolveOwnedPassiveItems(Component actor)
    {
        _ownedPassiveScratch.Clear();
        PlayerInventoryRuntime inventory =
            EntityComponentResolver.Resolve<PlayerInventoryRuntime>(actor);

        if (inventory != null)
            inventory.GetPassiveItems(_ownedPassiveScratch);

        return _ownedPassiveScratch;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_sector == null)
            _sector = GetComponentInParent<SectorRuntime>();

        if (_lootInventory == null)
            _lootInventory = GetComponent<StructureLootInventoryRuntime>();

        if (_lootInventory == null)
            _lootInventory = gameObject.AddComponent<StructureLootInventoryRuntime>();

        _lootInventory.Configure(
            _lootDisplayName,
            Mathf.Max(1, _lootSlotCount),
            _openRequestChannel,
            _overlayRequestChannel);

        if (_sectorStateManager == null &&
            _sectorStateManagerReadyChannel != null &&
            _sectorStateManagerReadyChannel.HasCurrent)
        {
            _sectorStateManager = _sectorStateManagerReadyChannel.Current;
        }

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();
    }

    // Purpose: Hides the placeholder UI or visual state.
    private void HidePlaceholder()
    {
        if (_placeholderRoot != null)
            _placeholderRoot.gameObject.SetActive(false);
    }

    // Purpose: Ensures treasure visual is ready before the workflow continues.
    private void EnsureTreasureVisual()
    {
        if (_treasureVisualInstance != null ||
            _treasureVisualPrefab == null ||
            _treasureVisualAnchor == null)
        {
            return;
        }

        _treasureVisualInstance = Instantiate(_treasureVisualPrefab, _treasureVisualAnchor);
        _treasureVisualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        _treasureVisualInstance.transform.localScale =
            Vector3.one * Mathf.Max(0.01f, _treasureVisualScale);
        DisableVisualPhysics(_treasureVisualInstance);
    }

    // Purpose: Refreshes the displayed or cached discovery visibility.
    private void RefreshDiscoveryVisibility()
    {
        if (_discoveryVisibility == null)
        {
            _discoveryVisibility = GetComponent<VisionDiscoveredVisibilityTarget>() ??
                gameObject.AddComponent<VisionDiscoveredVisibilityTarget>();
        }

        _discoveryVisibility.enabled = true;
        _discoveryVisibility.RefreshRenderers();
    }

    // Purpose: Disables visual physics for this component.
    private static void DisableVisualPhysics(GameObject visual)
    {
        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Rigidbody[] bodies = visual.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody body = bodies[i];
            if (body == null)
                continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.detectCollisions = false;
        }
    }

    // Purpose: Updates the interaction enabled state.
    private void SetInteractionEnabled(bool enabled)
    {
        if (_interactionCollider != null)
            _interactionCollider.enabled = enabled;
    }

    // Purpose: Spawns reward particle for the current runtime context.
    private void SpawnRewardParticle(TreasureRoomReward reward)
    {
        ClearRewardParticle();
        GameObject prefab = reward.Rarity switch
        {
            PlayerShopItemRarity.Rare => _rareRewardParticlePrefab,
            PlayerShopItemRarity.Unique => _uniqueRewardParticlePrefab,
            PlayerShopItemRarity.Legendary => _legendaryRewardParticlePrefab,
            _ => _normalRewardParticlePrefab
        };

        if (prefab == null)
            return;

        Transform parent = _dropOrigin != null ? _dropOrigin : transform;
        _spawnedRewardParticle = Instantiate(prefab, parent);
        _spawnedRewardParticle.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    // Purpose: Clears the current reward particle state.
    private void ClearRewardParticle()
    {
        if (_spawnedRewardParticle == null)
            return;

        Destroy(_spawnedRewardParticle);
        _spawnedRewardParticle = null;
    }

    // Purpose: Performs the log warning workflow for this component.
    private void LogWarning(string message)
    {
        if (_logWarnings)
            Debug.LogWarning($"[SectorTreasure] {message}", this);
    }
}
