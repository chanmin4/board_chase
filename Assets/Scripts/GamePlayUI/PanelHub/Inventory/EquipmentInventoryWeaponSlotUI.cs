using UnityEngine;

[DisallowMultipleComponent]
public sealed class EquipmentInventoryWeaponSlotUI : EquipmentInventorySlotUI
{
    protected override void HandleRightClick(Vector2 screenPosition)
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            Owner?.HandleInventorySlotRightClicked(DragSlotIndex, screenPosition);
            return;
        }

        Owner?.HandleEquipmentSlotRightClicked(
            PlayerInventoryEquipmentSlotKind.Weapon,
            screenPosition);
    }

    protected override void HandleLeftClick()
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            Owner?.HandleInventorySlotLeftClicked(DragSlotIndex);
            return;
        }

        Owner?.HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind.Weapon);
    }

    protected override void BeginDragPayload()
    {
        if (TryBeginEnemyInventoryDrag())
            return;

        InventoryDragContext.BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind.Weapon);
    }

    protected override void HandleDropPayload()
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
            return;

        if (InventoryDragContext.SourceKind == InventoryDragSourceKind.Inventory)
        {
            Owner?.HandleInventorySlotDroppedOnEquipment(
                InventoryDragContext.InventoryIndex,
                PlayerInventoryEquipmentSlotKind.Weapon);
        }
        else if (InventoryDragContext.SourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            InventoryDragContext.EnemyInventoryOwner?.TryTakeSlotToPlayerEquipmentSlot(
                InventoryDragContext.EnemyInventorySlotIndex,
                PlayerInventoryEquipmentSlotKind.Weapon);
        }
    }
}
