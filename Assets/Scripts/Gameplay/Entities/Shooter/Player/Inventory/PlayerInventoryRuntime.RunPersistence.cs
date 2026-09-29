/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Captures and restores the run-bound inventory ownership model for ES3 persistence.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerInventoryRuntime
{
    /// <summary>
    /// Copies every run-bound ownership surface except virtual magazines and
    /// passives. Those are owned by their respective runtimes and are captured
    /// by PlayerRunStatePersistence after this method returns.
    /// </summary>
    public void CaptureRunState(PlayerRunStateSaveData target)
    {
        if (target == null)
            return;

        ResolveRefs();
        NormalizeStorage();
        NormalizeKeyToolStorage();

        target.inventorySlots.Clear();
        target.equippedWeapons.Clear();
        target.keyToolSlots.Clear();

        for (int slotIndex = 0; slotIndex < Capacity && slotIndex < _items.Count; slotIndex++)
        {
            PlayerInventoryItemStack stack = _items[slotIndex];
            if (stack == null || stack.IsEmpty || stack.Item == null || string.IsNullOrWhiteSpace(stack.Item.Guid))
                continue;

            target.inventorySlots.Add(new PlayerRunInventorySlotSaveData
            {
                slotIndex = slotIndex,
                itemGuid = stack.Item.Guid,
                amount = stack.Amount,
                armorDurability = stack.ArmorDurability,
                armorMaxDurability = stack.ArmorMaxDurability,
                weaponCurrentDurability = stack.WeaponCurrentDurability,
                weaponMaxDurability = stack.WeaponMaxDurability,
                gasMaskFilterDurability = stack.GasMaskFilterDurability,
                gasMaskMaxFilterDurability = stack.GasMaskMaxFilterDurability
            });
        }

        if (_equipmentRuntime != null)
        {
            target.activeWeaponSlotIndex = _equipmentRuntime.ActiveWeaponSlotIndex;

            for (int slotIndex = 0; slotIndex < _equipmentRuntime.WeaponSlotCount; slotIndex++)
            {
                WeaponSO weapon = _equipmentRuntime.GetWeapon(slotIndex);
                if (weapon == null || string.IsNullOrWhiteSpace(weapon.Guid))
                    continue;

                target.equippedWeapons.Add(new PlayerRunWeaponSaveData
                {
                    slotIndex = slotIndex,
                    weaponGuid = weapon.Guid,
                    amount = _equipmentRuntime.GetWeaponAmount(slotIndex),
                    currentDurability = _equipmentRuntime.GetWeaponCurrentDurability(slotIndex),
                    maxDurability = _equipmentRuntime.GetWeaponMaxDurability(slotIndex)
                });
            }

            ArmorItemSO armor = _equipmentRuntime.CurrentArmor;
            target.equippedArmorGuid = armor != null ? armor.Guid : string.Empty;
            target.equippedArmorDurability = armor != null
                ? _equipmentRuntime.CurrentArmorDurability
                : -1f;
            target.equippedArmorMaxDurability = armor != null
                ? _equipmentRuntime.MaxArmorDurability
                : -1f;
        }

        target.equippedBackpackGuid = _equippedBackpack != null ? _equippedBackpack.Guid : string.Empty;
        target.equippedGasMaskGuid = _equippedGasMask != null ? _equippedGasMask.Guid : string.Empty;
        target.equippedGasMaskFilterDurability = _equippedGasMask != null
            ? EquippedGasMaskFilterDurability
            : -1f;
        target.equippedGasMaskMaxFilterDurability = _equippedGasMask != null
            ? EquippedGasMaskMaxFilterDurability
            : -1f;

        for (int slotIndex = 0; slotIndex < KeyToolSlotCapacity; slotIndex++)
        {
            PlayerKeyToolSlotSnapshot key = GetKeyToolSlotSnapshot(slotIndex);
            if (!key.HasKey || key.Key == null || string.IsNullOrWhiteSpace(key.Key.Guid))
                continue;

            target.keyToolSlots.Add(new PlayerRunKeyToolSlotSaveData
            {
                slotIndex = slotIndex,
                keyGuid = key.Key.Guid,
                remainingUses = key.RemainingUses
            });
        }
    }

    /// <summary>
    /// Restores inventory, equipped gear, backpack, head equipment, and Key
    /// Tool contents only after every referenced authored asset has been
    /// resolved. The all-or-nothing preflight prevents a partial restore from
    /// turning a valid saved run into an item-loss event.
    /// </summary>
    public bool TryRestoreRunState(
        PlayerRunStateSaveData source,
        RuntimeContentCatalogSO catalog,
        out string message)
    {
        message = string.Empty;

        if (source == null || !source.hasSnapshot)
        {
            message = "No resumable player run exists.";
            return false;
        }

        if (catalog == null || catalog.ItemCount <= 0)
        {
            message = "Runtime content catalog is missing or has no ItemSO entries.";
            return false;
        }

        source.EnsureDefaults();
        ResolveRefs();

        if (!ValidateRunStateReferences(source, catalog, out message))
            return false;

        BackpackItemSO restoredBackpack = ResolveItem<BackpackItemSO>(source.equippedBackpackGuid, catalog);
        GasMaskItemSO restoredGasMask = ResolveItem<GasMaskItemSO>(source.equippedGasMaskGuid, catalog);

        ResetRunOwnershipForRestore();

        _equippedBackpack = restoredBackpack;
        _useLegacyBackpackCapacityFallback = false;
        NormalizeStorage();

        for (int index = 0; index < source.inventorySlots.Count; index++)
        {
            PlayerRunInventorySlotSaveData entry = source.inventorySlots[index];
            ItemSO item = ResolveItem<ItemSO>(entry.itemGuid, catalog);
            _items[entry.slotIndex] ??= new PlayerInventoryItemStack();
            _items[entry.slotIndex].Set(
                item,
                entry.amount,
                entry.armorDurability,
                entry.weaponCurrentDurability,
                entry.weaponMaxDurability,
                entry.gasMaskFilterDurability,
                entry.armorMaxDurability,
                entry.gasMaskMaxFilterDurability);
        }

        if (_equipmentRuntime != null)
        {
            for (int index = 0; index < source.equippedWeapons.Count; index++)
            {
                PlayerRunWeaponSaveData entry = source.equippedWeapons[index];
                WeaponSO weapon = ResolveItem<WeaponSO>(entry.weaponGuid, catalog);
                _equipmentRuntime.EquipWeapon(
                    entry.slotIndex,
                    weapon,
                    entry.amount,
                    entry.currentDurability,
                    entry.maxDurability);
            }

            ArmorItemSO armor = ResolveItem<ArmorItemSO>(source.equippedArmorGuid, catalog);
            if (armor != null)
                _equipmentRuntime.EquipArmor(
                    armor,
                    source.equippedArmorDurability,
                    source.equippedArmorMaxDurability);

            _equipmentRuntime.SetActiveWeaponSlot(source.activeWeaponSlotIndex);
        }

        _equippedGasMask = restoredGasMask;
        _equippedGasMaskMaxFilterDurability = restoredGasMask != null
            ? Mathf.Clamp(
                source.equippedGasMaskMaxFilterDurability >= 0f
                    ? source.equippedGasMaskMaxFilterDurability
                    : restoredGasMask.MaxFilterDurability,
                0f,
                restoredGasMask.MaxFilterDurability)
            : -1f;
        _equippedGasMaskFilterDurability = restoredGasMask != null
            ? Mathf.Clamp(
                source.equippedGasMaskFilterDurability >= 0f
                    ? source.equippedGasMaskFilterDurability
                    : restoredGasMask.MaxFilterDurability,
                0f,
                _equippedGasMaskMaxFilterDurability)
            : -1f;

        RestoreKeyToolSlots(source, catalog);
        _startingLoadoutInventoryApplied = true;

        BackpackChanged?.Invoke(_equippedBackpack);
        GasMaskChanged?.Invoke(_equippedGasMask);
        NotifyChanged();
        message = $"Restored {source.inventorySlots.Count} inventory slot(s), {source.equippedWeapons.Count} weapon slot(s), and Key Tool data.";
        return true;
    }

    private bool ValidateRunStateReferences(
        PlayerRunStateSaveData source,
        RuntimeContentCatalogSO catalog,
        out string message)
    {
        message = string.Empty;
        int capacity = PocketCapacity;

        if (!string.IsNullOrWhiteSpace(source.equippedBackpackGuid))
        {
            if (!catalog.TryResolveItem(source.equippedBackpackGuid, out ItemSO backpackItem) ||
                backpackItem is not BackpackItemSO backpack)
            {
                message = $"Saved backpack cannot be resolved. guid={source.equippedBackpackGuid}";
                return false;
            }

            capacity += backpack.InventorySlotCapacity;
        }

        for (int index = 0; index < source.inventorySlots.Count; index++)
        {
            PlayerRunInventorySlotSaveData entry = source.inventorySlots[index];
            if (entry.slotIndex < 0 || entry.slotIndex >= capacity || !catalog.TryResolveItem(entry.itemGuid, out _))
            {
                message = $"Saved inventory entry cannot be restored. slot={entry.slotIndex}, guid={entry.itemGuid}";
                return false;
            }
        }

        if (!ValidateTypedReference<GasMaskItemSO>(source.equippedGasMaskGuid, "gas mask", catalog, out message) ||
            !ValidateTypedReference<ArmorItemSO>(source.equippedArmorGuid, "armor", catalog, out message))
        {
            return false;
        }

        for (int index = 0; index < source.equippedWeapons.Count; index++)
        {
            PlayerRunWeaponSaveData entry = source.equippedWeapons[index];
            if (_equipmentRuntime != null && entry.slotIndex >= _equipmentRuntime.WeaponSlotCount)
            {
                message = $"Saved weapon slot is no longer available. slot={entry.slotIndex}";
                return false;
            }

            if (!ValidateTypedReference<WeaponSO>(entry.weaponGuid, "weapon", catalog, out message))
                return false;
        }

        for (int index = 0; index < source.keyToolSlots.Count; index++)
        {
            PlayerRunKeyToolSlotSaveData entry = source.keyToolSlots[index];
            if (!ValidateTypedReference<KeyItemSO>(entry.keyGuid, "key", catalog, out message))
                return false;
        }

        return true;
    }

    private static bool ValidateTypedReference<T>(
        string guid,
        string label,
        RuntimeContentCatalogSO catalog,
        out string message)
        where T : ItemSO
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(guid))
            return true;

        if (catalog.TryResolveItem(guid, out ItemSO item) && item is T)
            return true;

        message = $"Saved {label} cannot be resolved. guid={guid}";
        return false;
    }

    private static T ResolveItem<T>(string guid, RuntimeContentCatalogSO catalog)
        where T : ItemSO
    {
        return !string.IsNullOrWhiteSpace(guid) &&
               catalog.TryResolveItem(guid, out ItemSO item)
            ? item as T
            : null;
    }

    private void RestoreKeyToolSlots(PlayerRunStateSaveData source, RuntimeContentCatalogSO catalog)
    {
        _keyToolSlots.Clear();

        if (!HasKeyTool)
            return;

        int capacity = KeyToolSlotCapacity;
        for (int slotIndex = 0; slotIndex < capacity; slotIndex++)
        {
            PlayerRunKeyToolSlotSaveData saved = null;
            for (int index = 0; index < source.keyToolSlots.Count; index++)
            {
                if (source.keyToolSlots[index].slotIndex == slotIndex)
                {
                    saved = source.keyToolSlots[index];
                    break;
                }
            }

            if (saved == null)
                continue;

            KeyItemSO key = ResolveItem<KeyItemSO>(saved.keyGuid, catalog);
            if (key == null)
                continue;

            PlayerKeyToolSlotState restored = new PlayerKeyToolSlotState();
            restored.Set(key, saved.remainingUses);
            if (restored.HasKey)
                _keyToolSlots.Add(restored);
        }

        NormalizeKeyToolStorage();
        KeyToolChanged?.Invoke();
    }

    private void ResetRunOwnershipForRestore()
    {
        for (int index = 0; index < _items.Count; index++)
            _items[index]?.Clear();

        _keyToolSlots.Clear();
        _equippedBackpack = null;
        _useLegacyBackpackCapacityFallback = false;
        ClearEquippedGasMask();

        if (_equipmentRuntime != null)
        {
            for (int slotIndex = 0; slotIndex < _equipmentRuntime.WeaponSlotCount; slotIndex++)
                _equipmentRuntime.ClearWeapon(slotIndex);

            _equipmentRuntime.ClearArmor();
        }

        _quickSlotRuntime?.ClearAllBindingsForRunLoss();
    }
}
