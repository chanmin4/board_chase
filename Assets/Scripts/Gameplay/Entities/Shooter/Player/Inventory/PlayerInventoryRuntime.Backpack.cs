/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Owns the player's pocket and equipped-backpack boundary.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerInventoryRuntime
{
    [Header("Pocket & Backpack")]
    [Tooltip("Always-available inventory slots. Removing a bag never removes these slots.")]
    [SerializeField, Min(0)] private int _pocketCapacity = 2;

    [Tooltip("The bag currently worn by the player. Its contents are the inventory slots after the pocket range.")]
    [SerializeField] private BackpackItemSO _equippedBackpack;

    [Tooltip("Compatibility bridge for existing prefabs and saves created before backpacks. It is cleared as soon as the player equips or drops a bag.")]
    [SerializeField] private bool _useLegacyBackpackCapacityFallback = true;

    public event Action<BackpackItemSO> BackpackChanged;

    public BackpackItemSO EquippedBackpack => _equippedBackpack;
    public bool HasEquippedBackpack => _equippedBackpack != null;
    public int PocketCapacity => Mathf.Min(ResolveLegacyInventoryCapacity(), Mathf.Max(0, _pocketCapacity));
    public int BackpackCapacity =>
        _equippedBackpack != null
            ? _equippedBackpack.InventorySlotCapacity
            : _useLegacyBackpackCapacityFallback
                ? Mathf.Max(0, ResolveLegacyInventoryCapacity() - PocketCapacity)
                : 0;
    public float BackpackMoveSpeedMultiplier =>
        _equippedBackpack != null ? _equippedBackpack.MoveSpeedMultiplier : 1f;

    // Purpose: Resolves the total inventory capacity from pocket and equipped backpack state.
    private int ResolveBackpackInventoryCapacity()
    {
        return Mathf.Max(0, PocketCapacity + BackpackCapacity);
    }

    // Purpose: Resolves the capacity used by content authored before backpack equipment existed.
    private int ResolveLegacyInventoryCapacity()
    {
        return _statsRuntime != null && _statsRuntime.InventoryCapacity > 0
            ? _statsRuntime.InventoryCapacity
            : Mathf.Max(0, _inventoryCapacity);
    }

    // Purpose: Equips a backpack from a standard inventory slot without losing any owned stacks.
    private bool TryEquipBackpackFromInventory(int index, BackpackItemSO backpack, out string message)
    {
        if (backpack == null)
            return Fail("Backpack is missing.", out message);

        if (_equippedBackpack != null)
            return Fail("Drop the equipped backpack before equipping another one.", out message);

        if (!CanFitCurrentInventoryInCapacity(PocketCapacity + backpack.InventorySlotCapacity, index))
            return Fail("The backpack is too small for the items currently carried.", out message);

        _items[index].Clear();
        SetEquippedBackpack(backpack);
        message = $"{backpack.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    // Purpose: Equips the one backpack authored in the player's starting loadout.
    private bool TryApplyStartingLoadoutBackpack(BackpackItemSO backpack, out string message)
    {
        if (backpack == null)
            return Fail("Starting backpack is missing.", out message);

        if (_equippedBackpack == backpack)
        {
            // The Player prefab mirrors the default backpack so capacity is available
            // before Start. The loadout remains the single content source of truth.
            _useLegacyBackpackCapacityFallback = false;
            message = $"{backpack.DisplayName} already equipped.";
            return true;
        }

        if (_equippedBackpack != null)
            return Fail("A different backpack is already equipped.", out message);

        return TryEquipBackpackFromWorld(backpack, null, out message);
    }

    // Purpose: Removes the equipped backpack and returns its complete contained payload for a world pickup.
    public bool TryDetachEquippedBackpack(
        out BackpackItemSO backpack,
        out List<PlayerInventoryItemStack> cargo,
        out string message)
    {
        NormalizeStorage();
        backpack = _equippedBackpack;
        cargo = new List<PlayerInventoryItemStack>();

        if (backpack == null)
        {
            message = "No backpack is equipped.";
            return false;
        }

        for (int index = PocketCapacity; index < Capacity && index < _items.Count; index++)
        {
            PlayerInventoryItemStack stack = _items[index];

            if (stack == null || stack.IsEmpty)
                continue;

            cargo.Add(CloneStack(stack));
            stack.Clear();
        }

        _equippedBackpack = null;
        _useLegacyBackpackCapacityFallback = false;
        BackpackChanged?.Invoke(null);
        message = $"{backpack.DisplayName} detached with {cargo.Count} occupied cargo slot(s).";
        NotifyChanged();
        return true;
    }

    // Purpose: Restores a backpack and its preserved cargo from a world pickup atomically.
    public bool TryEquipBackpackFromWorld(
        BackpackItemSO backpack,
        IReadOnlyList<PlayerInventoryItemStack> cargo,
        out string message)
    {
        NormalizeStorage();

        if (backpack == null)
            return Fail("Backpack is missing.", out message);

        if (_equippedBackpack != null)
            return Fail("Drop the equipped backpack before picking up another one.", out message);

        int proposedCapacity = PocketCapacity + backpack.InventorySlotCapacity;
        List<PlayerInventoryItemStack> preservedCargo = new();

        if (cargo != null)
        {
            for (int index = 0; index < cargo.Count; index++)
            {
                if (cargo[index] != null && !cargo[index].IsEmpty)
                    preservedCargo.Add(cargo[index]);
            }
        }

        if (preservedCargo.Count > backpack.InventorySlotCapacity)
            return Fail("This backpack payload is larger than the backpack capacity.", out message);

        if (!CanFitCurrentInventoryInCapacity(proposedCapacity, -1))
            return Fail("The backpack is too small for the items currently carried.", out message);

        // Reserve every destination before mutating equipped-backpack state. This
        // covers legacy saves that still have items in the old capacity range
        // while no explicit backpack is equipped yet.
        List<int> cargoTargetIndices = new(preservedCargo.Count);
        int nextTargetIndex = PocketCapacity;

        for (int index = 0; index < preservedCargo.Count; index++)
        {
            while (nextTargetIndex < proposedCapacity &&
                   nextTargetIndex < _items.Count &&
                   _items[nextTargetIndex] != null &&
                   !_items[nextTargetIndex].IsEmpty)
            {
                nextTargetIndex++;
            }

            if (nextTargetIndex >= proposedCapacity)
            {
                return Fail(
                    "The backpack does not have enough free cargo slots for its preserved contents.",
                    out message);
            }

            cargoTargetIndices.Add(nextTargetIndex);
            nextTargetIndex++;
        }

        SetEquippedBackpack(backpack);
        NormalizeStorage();

        for (int index = 0; index < preservedCargo.Count; index++)
        {
            PlayerInventoryItemStack source = preservedCargo[index];
            int targetIndex = cargoTargetIndices[index];
            _items[targetIndex].Set(
                source.Item,
                source.Amount,
                source.ArmorDurability,
                source.WeaponCurrentDurability,
                source.WeaponMaxDurability,
                source.GasMaskFilterDurability);
        }

        message = $"{backpack.DisplayName} equipped with preserved cargo.";
        NotifyChanged();
        return true;
    }

    // Purpose: Updates the equipped backpack and publishes the movement-stat dependency.
    private void SetEquippedBackpack(BackpackItemSO backpack)
    {
        _equippedBackpack = backpack;
        _useLegacyBackpackCapacityFallback = false;
        BackpackChanged?.Invoke(_equippedBackpack);
    }

    // Purpose: Clears the run-bound equipped backpack after player death.
    private void ClearEquippedBackpackForRunLoss()
    {
        if (_equippedBackpack == null && !_useLegacyBackpackCapacityFallback)
            return;

        _equippedBackpack = null;
        _useLegacyBackpackCapacityFallback = false;
        BackpackChanged?.Invoke(null);
    }

    // Purpose: Checks whether current inventory entries remain valid in a proposed capacity.
    private bool CanFitCurrentInventoryInCapacity(int proposedCapacity, int ignoredIndex)
    {
        int safeCapacity = Mathf.Max(0, proposedCapacity);

        for (int index = 0; index < _items.Count; index++)
        {
            if (index == ignoredIndex)
                continue;

            PlayerInventoryItemStack stack = _items[index];

            if (stack != null && !stack.IsEmpty && index >= safeCapacity)
                return false;
        }

        return true;
    }

    // Purpose: Copies an inventory slot so bag world payloads never retain aliases into player storage.
    private static PlayerInventoryItemStack CloneStack(PlayerInventoryItemStack source)
    {
        var clone = new PlayerInventoryItemStack();
        clone.Set(
            source.Item,
            source.Amount,
            source.ArmorDurability,
            source.WeaponCurrentDurability,
            source.WeaponMaxDurability,
            source.GasMaskFilterDurability);
        return clone;
    }
}
