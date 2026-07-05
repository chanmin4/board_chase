using UnityEngine;

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

public static class InventoryItemDisplayUtility
{
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

        return item.name;
    }
}