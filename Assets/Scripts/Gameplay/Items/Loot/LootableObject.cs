/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Turns an authored world object into a one-time LootHub container.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LootableObject : MonoBehaviour
{
    [Header("Loot Object (Authoring)")]
    [Tooltip("The complete authored record for this searchable world object. It owns its display data, slot count, and direct LootTableSO reference.")]
    [SerializeField] private LootObjectSO _lootObject;

    [Header("Shared Loot UI Channels (Required)")]
    [Tooltip("Raises the Loot Hub request after this container has successfully prepared its table-driven loot.")]
    [SerializeField] private EnemyLootOpenRequestEventChannelSO _openRequestChannel;
    [Tooltip("Opens the Loot Hub overlay after the open-request event is raised.")]
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;

    [Header("World Drop Fallback")]
    [SerializeField] private Transform _worldDropOrigin;

    [Header("Runtime (Auto at Play - Don't Edit)")]
    [ReadOnly] [SerializeField] private bool _rolled;

    private readonly List<LootRollResult> _rollResults = new();
    private StructureLootInventoryRuntime _lootInventory;
    private LockableLootObject _lockableLoot;
    private string _raidSaveKey;
    private int? _raidRollSeed;

    public void BindRaidState(string key, int seed, RaidLootContainerSaveData saved, RuntimeContentCatalogSO catalog)
    {
        _raidSaveKey = key;
        _raidRollSeed = seed;
        ResolveRefs();
        if (saved == null) return;
        _rolled = saved.rolled;
        _lockableLoot?.RestoreRaidUnlocked(saved.unlocked);
        if (!_rolled) return;
        int restoredCapacity = _lootInventory.InventoryCapacity;
        if (saved.items != null)
            foreach (var entry in saved.items)
                if (entry != null) restoredCapacity = Mathf.Max(restoredCapacity, entry.slotIndex + 1);
        _lootInventory.ConfigureInventoryCapacity(restoredCapacity);
        _lootInventory.ClearAllLoot();
        if (saved.items == null) return;
        foreach (var entry in saved.items)
        {
            if (entry == null || entry.amount <= 0) continue;
            if (catalog == null || !catalog.TryResolveItem(entry.itemGuid, out ItemSO item))
            {
                Debug.LogWarning($"[Raid] Missing container item '{entry.itemGuid}' in {key}; skipped without reroll.");
                continue;
            }
            _lootInventory.SetItemStack(entry.slotIndex, item, entry.amount, entry.armorDurability,
                entry.weaponCurrentDurability, entry.weaponMaxDurability, entry.gasMaskFilterDurability,
                entry.armorMaxDurability, entry.gasMaskMaxFilterDurability);
        }
    }

    public RaidLootContainerSaveData CaptureRaidState()
    {
        if (string.IsNullOrEmpty(_raidSaveKey)) return null;
        var saved = new RaidLootContainerSaveData
        {
            key = _raidSaveKey, rolled = _rolled,
            unlocked = _lockableLoot != null && !_lockableLoot.IsLocked
        };
        if (!_rolled || _lootInventory == null) return saved;
        for (int i = 0; i < _lootInventory.ItemStacks.Count; i++)
        {
            var stack = _lootInventory.ItemStacks[i];
            if (stack == null || stack.IsEmpty) continue;
            saved.items.Add(new PlayerRunInventorySlotSaveData
            {
                slotIndex = i, itemGuid = stack.Item.Guid, amount = stack.Amount,
                armorDurability = stack.ArmorDurability, armorMaxDurability = stack.ArmorMaxDurability,
                weaponCurrentDurability = stack.WeaponCurrentDurability, weaponMaxDurability = stack.WeaponMaxDurability,
                gasMaskFilterDurability = stack.GasMaskFilterDurability, gasMaskMaxFilterDurability = stack.GasMaskMaxFilterDurability
            });
        }
        return saved;
    }

    public LootObjectSO LootObject => _lootObject;
    public LootTableSO LootTable => _lootObject != null ? _lootObject.LootTable : null;
    public bool IsRolled => _rolled;
    public void ConfigureRuntimeSource(LootObjectSO source)
    {
        if (_rolled) return;
        _lootObject = source;
        ResolveRefs();
        ConfigureLootInventoryFromTable(logMissingBinding: true);
    }
    public bool CanInteract => isActiveAndEnabled && _lootInventory != null;

    /// <summary>
    /// Exposes the direct authoring record to editor diagnostics without
    /// duplicating presentation or slot data on the world prefab.
    /// </summary>
    public bool TryGetLootObject(out LootObjectSO lootObject)
    {
        lootObject = _lootObject;
        return lootObject != null;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        ConfigureLootInventoryFromTable(logMissingBinding: false);
    }

    // Purpose: Attempts to interact and reports whether the request succeeded.
    public bool TryInteract(Component actor)
    {
        if (actor == null || !CanInteract)
            return false;

        ResolveRefs();

        if (_lockableLoot != null && _lockableLoot.IsLocked)
            return _lockableLoot.TryInteract(actor);

        return TryOpenLootHub(actor);
    }

    /// <summary>
    /// Prepares the generated loot before a lock spends a key use. This keeps a
    /// missing loot configuration from consuming a key just to reveal an empty
    /// or invalid loot panel.
    /// </summary>
    public bool TryPrepareLoot()
    {
        ResolveRefs();

        if (!ConfigureLootInventoryFromTable(logMissingBinding: true))
            return false;

        return _rolled || TryRollIntoLootHub();
    }

    /// <summary>
    /// Opens an already prepared LootHub. LockableLootObject calls this only
    /// after its key transaction has succeeded.
    /// </summary>
    public bool TryOpenPreparedLoot(Component actor)
    {
        return actor != null && _lootInventory != null &&
               _lootInventory.CanInteract && _lootInventory.TryInteract(actor);
    }

    /// <summary>
    /// Performs the normal unlocked object interaction using the existing
    /// LootHub pipeline.
    /// </summary>
    public bool TryOpenLootHub(Component actor)
    {
        if (actor == null || !CanInteract)
            return false;

        if (!TryPrepareLoot())
            return false;

        bool opened = TryOpenPreparedLoot(actor);

        if (opened)
            PlayLootOpenFeedback(actor);

        return opened;
    }

    // Purpose: Attempts to roll into loot hub and reports whether the request succeeded.
    private bool TryRollIntoLootHub()
    {
        LootTableSO table = LootTable;
        if (table == null)
        {
            Debug.LogWarning(
                $"[LootableObject] LootObjectSO or its direct LootTableSO is not assigned. object={name}",
                this);
            return false;
        }

        var lockProfile = _lockableLoot != null ? _lockableLoot.LockedLootProfile : null;
        if (!table.HasRollableItems(lockProfile))
        {
            Debug.LogWarning(
                $"[LootableObject] Loot Table '{table.name}' has no rollable item entries. object={name}",
                this);
            return false;
        }

        int seed = ResolveRollSeed();
        System.Random random = new System.Random(seed);
        table.TryRoll(random, _rollResults, lockProfile, _lootObject.RollSettings);
        // A configured container may legitimately roll empty. Commit that result once,
        // including to raid saves, instead of rerolling or rejecting the lock transaction.
        _lootInventory.ClearAllLoot();

        for (int i = 0; i < _rollResults.Count; i++)
        {
            LootRollResult result = _rollResults[i];
            if (result.Item == null)
                continue;

            if (_lootInventory.TryAddItemStack(result.Item, result.Amount, out string _))
                continue;

            Vector3 position = _worldDropOrigin != null
                ? _worldDropOrigin.position
                : transform.position;
            if (!WorldItemPickupSpawner.TrySpawn(
                    result.Item,
                    result.Amount,
                    result.Item is BulletSO ? result.Amount : 1,
                    0f,
                    result.Item is ArmorItemSO armor ? armor.MaxDurability : -1f,
                    position,
                    Quaternion.identity,
                    out _))
            {
                Debug.LogError(
                    $"[LootableObject] Rolled loot was lost because the LootHub was full and the WorldItemPickupSpawner is unavailable. " +
                    $"object={name}, item={result.Item.name}",
                    this);
            }
        }

        _rolled = true;
        return true;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_lootInventory == null)
            _lootInventory = GetComponent<StructureLootInventoryRuntime>();

        if (_lockableLoot == null)
            _lockableLoot = GetComponent<LockableLootObject>();

        if (_lootInventory == null)
            _lootInventory = gameObject.AddComponent<StructureLootInventoryRuntime>();

    }

    /// <summary>
    /// Applies the presentation, capacity, and shared UI channels owned by the
    /// selected LootObjectSO. StructureLootInventoryRuntime intentionally has no
    /// authored display/channel fields of its own.
    /// </summary>
    private bool ConfigureLootInventoryFromTable(bool logMissingBinding)
    {
        if (_lootInventory == null)
            return false;

        if (_lootObject == null)
        {
            if (logMissingBinding)
            {
                Debug.LogWarning(
                    $"[LootableObject] LootObjectSO is not assigned. object={name}",
                    this);
            }

            return false;
        }

        if (_lootObject.LootTable == null)
        {
            if (logMissingBinding)
            {
                Debug.LogWarning(
                    $"[LootableObject] LootObjectSO '{_lootObject.name}' has no direct LootTableSO. object={name}",
                    this);
            }

            return false;
        }

        _lootInventory.Configure(
            _lootObject.ResolveDisplayName(),
            _lootObject.LootSlotCount,
            _openRequestChannel,
            _overlayRequestChannel);
        return true;
    }

    // Purpose: Plays the player-owned feedback after an ordinary loot container opens successfully.
    private static void PlayLootOpenFeedback(Component actor)
    {
        if (actor == null)
            return;

        PlayerShooterCharacterAudio playerAudio = actor.GetComponent<PlayerShooterCharacterAudio>() ??
            actor.GetComponentInParent<PlayerShooterCharacterAudio>();
        playerAudio?.PlayGenericInteract();
    }

    // Purpose: Resolves the roll seed needed by this component.
    private int ResolveRollSeed()
    {
        if (_raidRollSeed.HasValue) return _raidRollSeed.Value;
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (_lootObject != null ? _lootObject.name.GetHashCode() : 0);
            hash = hash * 31 + transform.position.GetHashCode();
            return hash;
        }
    }
}
