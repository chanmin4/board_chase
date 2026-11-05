/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Defines the shared contract implemented by the ItemDetailSelection role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public readonly struct ItemDetailSelection
{
    public readonly ItemSO item;
    public readonly int amount;
    public readonly float armorDurability;
    public readonly float armorMaxDurability;
    public readonly float weaponCurrentDurability;
    public readonly float weaponMaxDurability;
    public readonly float gasMaskFilterDurability;
    public readonly float gasMaskMaxFilterDurability;
    public readonly PlayerInventoryRuntime playerEquipmentOwner;
    public readonly int playerWeaponSlotIndex;

    public bool HasItem => item != null && amount > 0;

    public ItemDetailSelection(
        ItemSO item,
        int amount,
        float armorDurability = -1f,
        float armorMaxDurability = -1f,
        float weaponCurrentDurability = -1f,
        float weaponMaxDurability = -1f,
        float gasMaskFilterDurability = -1f,
        float gasMaskMaxFilterDurability = -1f,
        PlayerInventoryRuntime playerEquipmentOwner = null,
        int playerWeaponSlotIndex = -1)
    {
        this.item = item;
        this.amount = Mathf.Max(0, amount);
        this.armorDurability = armorDurability;
        this.armorMaxDurability = armorMaxDurability;
        this.weaponCurrentDurability = weaponCurrentDurability;
        this.weaponMaxDurability = weaponMaxDurability;
        this.gasMaskFilterDurability = gasMaskFilterDurability;
        this.gasMaskMaxFilterDurability = gasMaskMaxFilterDurability;
        this.playerEquipmentOwner = playerEquipmentOwner;
        this.playerWeaponSlotIndex = playerWeaponSlotIndex;
    }
}

/// <summary>
/// Shared selection route for the fixed item-detail panel. Player inventory,
/// shops, enemy corpses, and structure loot all publish through this route.
/// </summary>
public static class ItemDetailSelectionBus
{
    public static event Action<ItemDetailSelection> SelectionChanged;

    public static ItemDetailSelection Current { get; private set; }

    // Purpose: Publishes the publish notification.
    public static void Publish(ItemDetailSelection selection)
    {
        if (!selection.HasItem)
            return;

        Current = selection;
        SelectionChanged?.Invoke(Current);
    }

    // Purpose: Clears the current clear state.
    public static void Clear()
    {
        Current = default;
        SelectionChanged?.Invoke(Current);
    }

    // Purpose: Publishes the publish notification.
    public static void Publish(PlayerInventoryItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
            return;

        float armorMax = stack.Item is ArmorItemSO armor
            ? stack.HasArmorCondition ? stack.ArmorMaxDurability : armor.MaxDurability
            : -1f;
        float armorCurrent = stack.Item is ArmorItemSO
            ? stack.HasArmorDurability ? stack.ArmorDurability : armorMax
            : -1f;

        Publish(new ItemDetailSelection(
            stack.Item,
            stack.Amount,
            armorCurrent,
            armorMax,
            stack.WeaponCurrentDurability,
            stack.WeaponMaxDurability,
            stack.GasMaskFilterDurability,
            stack.GasMaskMaxFilterDurability));
    }
}
