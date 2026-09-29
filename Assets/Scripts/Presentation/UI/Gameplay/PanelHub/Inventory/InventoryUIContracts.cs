/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Manages IInventorySlotOwner item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Localization;

public interface IInventorySlotOwner
{
    void HandleInventorySlotLeftClicked(int slotIndex);
    void HandleInventorySlotRightClicked(int slotIndex, Vector2 screenPosition);
    void HandleInventorySlotDroppedOnInventory(int fromIndex, int toIndex);
    void HandleInventorySlotDroppedOnEquipment(int inventoryIndex, PlayerInventoryEquipmentSlotKind slotKind);
    void HandleEquipmentSlotDroppedOnInventory(PlayerInventoryEquipmentSlotKind slotKind, int targetInventoryIndex);
    void HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind slotKind);
    void HandleEquipmentSlotRightClicked(PlayerInventoryEquipmentSlotKind slotKind, Vector2 screenPosition);
    void HandleInventorySlotDroppedOnAmmo(int inventoryIndex, int ammoSlotIndex);
    void HandleAmmoSlotDroppedOnInventory(int ammoSlotIndex, int targetInventoryIndex);
    void HandleAmmoSlotDroppedOnAmmo(int fromAmmoSlotIndex, int toAmmoSlotIndex);
    void HandleAmmoSlotLeftClicked(int ammoSlotIndex);
    void HandleAmmoSlotRightClicked(int ammoSlotIndex, Vector2 screenPosition);
    void HandleItemTooltipEntered(ItemSO item, RectTransform anchor);
    void HandleItemTooltipExited();
}

public interface IInventoryItemContextMenuOwner
{
    void HandleContextMenuEquipInventoryItem(int inventoryIndex);
    void HandleContextMenuSplitInventoryItem(int inventoryIndex, int amount);
    void HandleContextMenuDiscardInventoryItem(int inventoryIndex);
    void HandleContextMenuUnequipEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind);
    void HandleContextMenuDiscardEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind);
    void HandleContextMenuUnequipAmmoSlot(int ammoSlotIndex);
    void HandleContextMenuSplitAmmoSlot(int ammoSlotIndex, int amount);
    void HandleContextMenuDiscardAmmoSlot(int ammoSlotIndex);
}

public interface IConsumableInventoryContextMenuOwner
{
    void HandleContextMenuUseInventoryItem(int inventoryIndex);
}

public static class InventoryItemDisplayUtility
{
    // Purpose: Attempts to get item name reference and reports whether the request succeeded.
    public static bool TryGetItemNameReference(ItemSO item, out LocalizedString itemName)
    {
        itemName = null;

        if (item == null || item.Name == null || item.Name.IsEmpty)
            return false;

        itemName = item.Name;
        return true;
    }

    // Purpose: Resolves the item name needed by this component.
    public static string ResolveItemName(ItemSO item)
    {
        if (item == null)
            return string.Empty;

        if (item is BulletSO bullet)
            return bullet.DisplayName;

        if (item is WeaponSO weapon)
            return weapon.DisplayName;

        if (item is ArmorItemSO armor)
            return armor.DisplayName;

        if (item is KeyItemSO key)
            return key.DisplayName;

        if (item is KeyToolItemSO keyTool)
            return keyTool.DisplayName;

        return item.name;
    }
}
