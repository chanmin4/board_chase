/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Owns pocket, permanent inventory, and backpack bonus-slot boundaries.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerInventoryRuntime
{
    [Header("Pocket & Backpack")]
    [Tooltip("Always-available inventory slots. Removing a bag never removes these slots.")]
    [SerializeField, HideInInspector, Min(0)] private int _pocketCapacity = 2;

    [Tooltip("The equipped bag adds its SO slot count to the character Config's base Inventory capacity; Pocket is separate.")]
    [SerializeField] private BackpackItemSO _equippedBackpack;

    public event Action<BackpackItemSO> BackpackChanged;

    public BackpackItemSO EquippedBackpack => _equippedBackpack;
    public bool HasEquippedBackpack => _equippedBackpack != null;
    private ShooterInventorySettings InventorySettings => _statsRuntime != null ? _statsRuntime.InventorySettings : null;
    public int PocketCapacity => Mathf.Clamp((InventorySettings?.PocketCapacity ?? 6) + (SaveSystem.Active?.Profile.PocketSlotBonus ?? 0), 6, 10);
    public int BaseInventoryCapacity => (InventorySettings?.BaseInventoryCapacity ?? 4)+Mathf.FloorToInt(_statsRuntime?.AdditionalStat(PlayerStatId.InventorySlots)??0);
    /// <summary>Bonus slots supplied by the equipped bag, not the whole Inventory grid.</summary>
    public int BackpackCapacity => _equippedBackpack != null ? _equippedBackpack.InventorySlotCapacity : 0;
    public int InventoryCapacity => BaseInventoryCapacity + BackpackCapacity;
    public int CapacityWithoutBackpack => PocketCapacity + BaseInventoryCapacity;
    public float BackpackMoveSpeedMultiplier =>
        _equippedBackpack != null ? _equippedBackpack.MoveSpeedMultiplier : 1f;

    // Absolute slots: [Config Pocket][Config base Inventory][equipped backpack bonus].
    // Only the final range belongs to the detachable backpack payload.
    private int ResolveBackpackInventoryCapacity()
    {
        return PocketCapacity + InventoryCapacity;
    }

    // Purpose: Equips a backpack from a standard inventory slot without losing any owned stacks.
    private bool TryEquipBackpackFromInventory(int index, BackpackItemSO backpack, out string message)
    {
        if (backpack == null)
            return Fail("Backpack is missing.", out message);

        if (_equippedBackpack != null)
            return Fail("Drop the equipped backpack before equipping another one.", out message);

        if (!CanFitCurrentInventoryInCapacity(CapacityWithoutBackpack + backpack.InventorySlotCapacity, index))
            return Fail("The backpack is too small for the items currently carried.", out message);

        _items[index].Clear();
        SetEquippedBackpack(backpack);
        message = $"{backpack.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    // Resolve equipment before any starting inventory/ammo transaction. Loadout
    // row order must not decide whether carried items have enough bag slots.
    internal bool TryPrepareStartingLoadoutBackpack(ShooterLoadoutSnapshot snapshot, out string message)
    {
        message = string.Empty;
        if (_startingLoadoutInventoryApplied || snapshot.inventoryStacks == null)
            return true; // A restored run is authoritative; never re-equip its starter bag.

        BackpackItemSO startingBackpack = null;
        for (int i = 0; i < snapshot.inventoryStacks.Length; i++)
        {
            var stack = snapshot.inventoryStacks[i];
            if (!stack.HasItem || stack.item is not BackpackItemSO backpack)
                continue;
            if (startingBackpack != null && startingBackpack != backpack)
                return Fail("Starting loadout contains more than one equipped backpack.", out message);
            startingBackpack = backpack;
        }

        return startingBackpack == null || TryApplyStartingLoadoutBackpack(startingBackpack, out message);
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

        for (int index = CapacityWithoutBackpack; index < Capacity && index < _items.Count; index++)
        {
            if (IsRaidLockedEquippedModule(_items[index]?.Item))
                return Fail("가방을 내려놓으려면 장착 중인 모듈을 포켓이나 기본 인벤토리 칸으로 먼저 옮기세요.", out message);
        }

        for (int index = CapacityWithoutBackpack; index < Capacity && index < _items.Count; index++)
        {
            PlayerInventoryItemStack stack = _items[index];

            if (stack == null || stack.IsEmpty)
                continue;

            cargo.Add(CloneStack(stack));
            stack.Clear();
        }

        _equippedBackpack = null;
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

        int proposedCapacity = CapacityWithoutBackpack + backpack.InventorySlotCapacity;
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
        int nextTargetIndex = CapacityWithoutBackpack;

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
                source.GasMaskFilterDurability,
                source.ArmorMaxDurability,
                source.GasMaskMaxFilterDurability);
        }

        message = $"{backpack.DisplayName} equipped with preserved cargo.";
        NotifyChanged();
        return true;
    }

    // Purpose: Updates the equipped backpack and publishes the movement-stat dependency.
    private void SetEquippedBackpack(BackpackItemSO backpack)
    {
        _equippedBackpack = backpack;
        BackpackChanged?.Invoke(_equippedBackpack);
    }

    // Purpose: Clears the run-bound equipped backpack after player death.
    private void ClearEquippedBackpackForRunLoss()
    {
        if (_equippedBackpack == null)
            return;

        _equippedBackpack = null;
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
            source.GasMaskFilterDurability,
            source.ArmorMaxDurability,
            source.GasMaskMaxFilterDurability);
        return clone;
    }
}
