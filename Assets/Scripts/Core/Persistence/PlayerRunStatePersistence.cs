/*
 * Role hierarchy: Core > Persistence
 * Primary responsibility: Captures and restores the player's resumable run state around ES3 profile writes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bridges the persistent ES3 profile with live player-owned state. Authoring
/// objects stay out of the save: stable ItemSO GUIDs and status-effect IDs are
/// resolved through <see cref="RuntimeContentCatalogSO"/> after the Player
/// prefab exists. Death clears this snapshot without touching meta progress.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerRunStatePersistence : MonoBehaviour
{
    [Header("Runtime Content")]
    [Tooltip("Build-safe ItemSO and StatusEffectDefinitionSO lookup used to restore a saved player run. If omitted, Resources/RuntimeContentCatalog is used.")]
    [SerializeField] private RuntimeContentCatalogSO _runtimeContentCatalog;

    [Header("Runtime State (Read Only)")]
    [SerializeField, ReadOnly] private bool _restorePending;
    [SerializeField, ReadOnly] private string _lastRunStateMessage = "Not initialized";

    private SaveSystem _saveSystem;

    public bool IsRestorePending => _restorePending;
    public string LastRunStateMessage => _lastRunStateMessage;

    /// <summary>
    /// Receives the authoritative profile after SaveSystem has completed its
    /// ES3/legacy migration load. A snapshot remains pending until the player
    /// prefab and its component graph are alive.
    /// </summary>
    public void Initialize(SaveSystem saveSystem, RuntimeContentCatalogSO runtimeContentCatalog)
    {
        _saveSystem = saveSystem;
        if (runtimeContentCatalog != null)
            _runtimeContentCatalog = runtimeContentCatalog;

        RunSaveData runData = _saveSystem != null ? _saveSystem.RunData : null;
        runData?.EnsureDefaults();

        _restorePending = runData != null && runData.PlayerRunState.hasSnapshot;
        _lastRunStateMessage = _restorePending
            ? "Waiting for Player runtime to restore the saved run."
            : "No saved player run needs restoration.";
    }

    /// <summary>
    /// Captures the current alive player at an explicit lifecycle save point.
    /// While a previous snapshot is waiting for its player prefab, it is never
    /// overwritten by an empty Home/scene-transition frame.
    /// </summary>
    public bool CaptureCurrentPlayerState()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        if (_restorePending)
        {
            _lastRunStateMessage = "Skipped capture while a saved run is waiting to restore.";
            return false;
        }

        RunSaveData runData = ResolveRunData();
        PlayerInventoryRuntime inventory = FindFirstObjectByType<PlayerInventoryRuntime>();

        if (runData == null || inventory == null)
        {
            _lastRunStateMessage = "Skipped capture because no active PlayerInventoryRuntime exists.";
            return false;
        }

        Damageable damageable = inventory.GetComponent<Damageable>();
        if (damageable != null && damageable.IsDead)
        {
            _lastRunStateMessage = "Skipped capture because the active player is dead.";
            return false;
        }

        PlayerRunStateSaveData target = runData.PlayerRunState;
        target.Clear();

        inventory.CaptureRunState(target);
        inventory.GetComponent<PlayerQuickSlotRuntime>()?.CaptureUsableRunSlots(target.usableQuickSlots);
        inventory.GetComponent<PlayerBulletLoadoutRuntime>()?.CaptureRunState(target);
        inventory.GetComponent<PlayerPassiveInventoryRuntime>()?.CaptureRunState(target);
        inventory.GetComponent<EntityStatusEffectRuntime>()?.CaptureRunState(target);
        ShopRunRuntime.EnsureRuntime().CaptureRunState(target);

        // WIP migration field only. Physical MoneyItem stacks are already
        // captured by PlayerInventoryRuntime with the rest of the inventory.
        target.runCurrency = 0;
        target.currentHealth = damageable != null ? damageable.CurrentHealth : -1f;

        PlayerShooterInfection infection = inventory.GetComponent<PlayerShooterInfection>();
        target.currentInfection = infection != null ? infection.CurrentInfection : 0f;
        target.hasSnapshot = true;
        target.EnsureDefaults();
        RaidRuntime.Active?.Capture(runData);

        _lastRunStateMessage =
            $"Captured run state: inventory={target.inventorySlots.Count}, weapons={target.equippedWeapons.Count}, magazines={target.weaponMagazines.Count}, effects={target.statusEffects.Count}.";
        return true;
    }

    /// <summary>
    /// Clears only the run-bound snapshot. Quest progress, currency, shop
    /// unlocks, settings, and inheritance data remain in the same profile.
    /// </summary>
    public void ClearRunState()
    {
        RunSaveData runData = ResolveRunData();
        if (runData == null)
            return;

        runData.PlayerRunState.Clear();
        runData.startingPassives = new();
        RaidRuntime.Active?.EndRaid();
        if(SaveSystem.Active!=null)SaveSystem.Active.RunData.infectionExposure=new();
        runData.raid = new RaidRunSaveData();
        _restorePending = false;
        _lastRunStateMessage = "Cleared resumable player run after run loss.";
    }

    /// <summary>
    /// Called every frame by RunLifecycleCoordinator until a scene-created
    /// player is available. It intentionally waits instead of creating a
    /// player or mutating scene ownership.
    /// </summary>
    public void TryRestorePendingRunState()
    {
        if (!_restorePending)
            return;

        RunSaveData runData = ResolveRunData();
        if (runData == null || !runData.PlayerRunState.hasSnapshot)
        {
            _restorePending = false;
            _lastRunStateMessage = "Saved run snapshot is no longer available.";
            return;
        }

        PlayerInventoryRuntime inventory = FindFirstObjectByType<PlayerInventoryRuntime>();
        if (inventory == null)
            return;

        RuntimeContentCatalogSO catalog = ResolveCatalog();
        if (catalog == null || catalog.ItemCount <= 0)
        {
            _lastRunStateMessage = "Restore is waiting for Resources/RuntimeContentCatalog with ItemSO entries.";
            return;
        }

        if (!TryGetRequiredPlayerComponents(
                inventory,
                out PlayerBulletLoadoutRuntime bullets,
                out PlayerPassiveInventoryRuntime passives,
                out EntityStatusEffectRuntime effects,
                out Damageable damageable,
                out PlayerShooterInfection infection))
        {
            return;
        }

        PlayerRunStateSaveData source = runData.PlayerRunState;
        source.EnsureDefaults();

        EntityEquipmentRuntime equipment = inventory.GetComponent<EntityEquipmentRuntime>();
        int removedReferenceCount = PruneUnavailableReferences(
            source,
            catalog,
            inventory.CapacityWithoutBackpack,
            equipment != null ? equipment.WeaponSlotCount : int.MaxValue,
            out string pruneMessage);
        if (removedReferenceCount > 0)
        {
            // A content update must not make the whole run unusable just because
            // one ItemSO or status-effect definition was removed. The rest of
            // this snapshot is still authoritative and can be restored safely.
            Debug.LogWarning(
                $"[PlayerRunStatePersistence] Restoring run after removing {removedReferenceCount} unavailable saved reference(s): {pruneMessage}",
                this);
        }

        string inventoryMessage = "not attempted";
        var quickSlots = inventory.GetComponent<PlayerQuickSlotRuntime>();
        if (source.usableQuickSlots.Count > 0 && (quickSlots == null || !quickSlots.CanRestoreUsableRunSlots(source.usableQuickSlots, catalog)))
        { _restorePending = false; Debug.LogError("[Run restore] Invalid usable quick-slot snapshot; no inventory restored.", this); return; }
        string bulletMessage = "not attempted";
        string passiveMessage = "not attempted";
        string effectMessage = "not attempted";
        bool inventoryRestored = inventory.TryRestoreRunState(source, catalog, out inventoryMessage);
        bool bulletsRestored = inventoryRestored && bullets.TryRestoreRunState(source, catalog, out bulletMessage);
        bool passivesRestored = bulletsRestored && passives.TryRestoreRunState(source, catalog, out passiveMessage);
        bool effectsRestored = passivesRestored && effects.TryRestoreRunState(source, catalog, out effectMessage);

        if (!inventoryRestored || !bulletsRestored || !passivesRestored || !effectsRestored)
        {
            _restorePending = false;
            _lastRunStateMessage =
                $"Run restore did not complete. inventory={inventoryMessage}; bullets={bulletMessage}; passives={passiveMessage}; effects={effectMessage}";
            Debug.LogError($"[PlayerRunStatePersistence] {_lastRunStateMessage}", this);
            return;
        }

        infection.RestoreCurrentInfection(source.currentInfection);
        if (source.currentHealth > 0f)
            damageable.SetCurrentHealth(source.currentHealth);
        quickSlots?.RestoreUsableRunSlots(source.usableQuickSlots, catalog);

        ShopRunRuntime.EnsureRuntime().RestoreRunState(source, catalog);

        _restorePending = false;
        _lastRunStateMessage = removedReferenceCount > 0
            ? $"Restored saved player run; removed {removedReferenceCount} unavailable reference(s): {pruneMessage}"
            : "Restored saved player run successfully.";
        Debug.Log($"[PlayerRunStatePersistence] {_lastRunStateMessage}", this);
    }

    private RunSaveData ResolveRunData()
    {
        if (_saveSystem == null)
            _saveSystem = SaveSystem.Active;

        return _saveSystem != null ? _saveSystem.RunData : null;
    }

    private RuntimeContentCatalogSO ResolveCatalog()
    {
        if (_runtimeContentCatalog == null)
            _runtimeContentCatalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");

        return _runtimeContentCatalog;
    }

    private bool TryGetRequiredPlayerComponents(
        PlayerInventoryRuntime inventory,
        out PlayerBulletLoadoutRuntime bullets,
        out PlayerPassiveInventoryRuntime passives,
        out EntityStatusEffectRuntime effects,
        out Damageable damageable,
        out PlayerShooterInfection infection)
    {
        bullets = inventory.GetComponent<PlayerBulletLoadoutRuntime>();
        passives = inventory.GetComponent<PlayerPassiveInventoryRuntime>();
        effects = inventory.GetComponent<EntityStatusEffectRuntime>();
        damageable = inventory.GetComponent<Damageable>();
        infection = inventory.GetComponent<PlayerShooterInfection>();

        if (bullets != null && passives != null && effects != null && damageable != null && infection != null)
            return true;

        _lastRunStateMessage = "Restore is waiting for one or more Player runtime components.";
        return false;
    }

    /// <summary>
    /// Removes only entries that no longer point at an authored asset. This is
    /// deliberately performed before any runtime mutation so the existing
    /// restore methods can remain strict about the cleaned snapshot they get.
    /// </summary>
    private static int PruneUnavailableReferences(
        PlayerRunStateSaveData source,
        RuntimeContentCatalogSO catalog,
        int capacityWithoutBackpack,
        int weaponSlotCapacity,
        out string message)
    {
        int removedCount = 0;
        List<string> removedKinds = new List<string>();

        if (!TryResolveOptionalTyped<ArmorItemSO>(catalog, source.equippedArmorGuid))
        {
            source.equippedArmorGuid = string.Empty;
            source.equippedArmorDurability = -1f;
            source.equippedArmorMaxDurability = -1f;
            removedCount++;
            AddRemovedKind("armor", removedKinds);
        }

        int restoredInventoryCapacity = Mathf.Max(0, capacityWithoutBackpack);
        if (!TryResolveOptionalTyped<BackpackItemSO>(catalog, source.equippedBackpackGuid))
        {
            source.equippedBackpackGuid = string.Empty;
            removedCount++;
            AddRemovedKind("backpack", removedKinds);
        }
        else if (TryResolveTyped<BackpackItemSO>(catalog, source.equippedBackpackGuid))
        {
            catalog.TryResolveItem(source.equippedBackpackGuid, out ItemSO backpackItem);
            restoredInventoryCapacity += ((BackpackItemSO)backpackItem).InventorySlotCapacity;
        }

        if (!TryResolveOptionalTyped<GasMaskItemSO>(catalog, source.equippedGasMaskGuid))
        {
            source.equippedGasMaskGuid = string.Empty;
            source.equippedGasMaskFilterDurability = -1f;
            source.equippedGasMaskMaxFilterDurability = -1f;
            removedCount++;
            AddRemovedKind("gas mask", removedKinds);
        }

        removedCount += RemoveInvalidEntries(
            source.inventorySlots,
            entry => entry == null ||
                     entry.slotIndex < 0 ||
                     entry.slotIndex >= restoredInventoryCapacity ||
                     !catalog.TryResolveItem(entry.itemGuid, out _),
            "inventory",
            removedKinds);

        removedCount += RemoveInvalidEntries(
            source.equippedWeapons,
            entry => entry == null ||
                     entry.slotIndex < 0 ||
                     entry.slotIndex >= weaponSlotCapacity ||
                     !TryResolveTyped<WeaponSO>(catalog, entry.weaponGuid),
            "weapon",
            removedKinds);

        removedCount += RemoveInvalidEntries(
            source.keyToolSlots,
            entry => entry == null || !TryResolveTyped<KeyItemSO>(catalog, entry.keyGuid),
            "key tool",
            removedKinds);

        removedCount += RemoveInvalidEntries(
            source.weaponMagazines,
            entry => entry == null ||
                     entry.weaponSlotIndex < 0 ||
                     entry.weaponSlotIndex >= weaponSlotCapacity ||
                     !TryResolveTyped<BulletSO>(catalog, entry.bulletGuid),
            "magazine",
            removedKinds);

        for (int index = 0; index < source.equippedPassiveGuids.Count; index++)
        {
            string guid = source.equippedPassiveGuids[index];
            if (!string.IsNullOrWhiteSpace(guid) && !TryResolveTyped<PassiveItemSO>(catalog, guid))
            {
                source.equippedPassiveGuids[index] = string.Empty;
                removedCount++;
                AddRemovedKind("passive", removedKinds);
            }
        }

        removedCount += RemoveInvalidEntries(
            source.statusEffects,
            entry => entry == null || !catalog.TryResolveStatusEffect(entry.statusEffectId, out _),
            "status effect",
            removedKinds);

        source.EnsureDefaults();
        message = removedKinds.Count > 0 ? string.Join(", ", removedKinds) : string.Empty;
        return removedCount;
    }

    private static int RemoveInvalidEntries<T>(
        List<T> entries,
        System.Predicate<T> shouldRemove,
        string kind,
        List<string> removedKinds)
    {
        int removedCount = 0;
        for (int index = entries.Count - 1; index >= 0; index--)
        {
            if (!shouldRemove(entries[index]))
                continue;

            entries.RemoveAt(index);
            removedCount++;
            AddRemovedKind(kind, removedKinds);
        }

        return removedCount;
    }

    private static void AddRemovedKind(string kind, List<string> removedKinds)
    {
        if (!removedKinds.Contains(kind))
            removedKinds.Add(kind);
    }

    private static bool TryResolveTyped<T>(RuntimeContentCatalogSO catalog, string guid)
        where T : ItemSO
    {
        return catalog.TryResolveItem(guid, out ItemSO item) && item is T;
    }

    private static bool TryResolveOptionalTyped<T>(RuntimeContentCatalogSO catalog, string guid)
        where T : ItemSO
    {
        return string.IsNullOrWhiteSpace(guid) || TryResolveTyped<T>(catalog, guid);
    }
}
