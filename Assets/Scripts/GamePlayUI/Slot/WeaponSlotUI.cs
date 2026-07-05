// WeaponSlotUI.cs
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class WeaponSlotUI : SlotUI
{
    [Header("Slot")]
    [SerializeField] private PlayerInventoryEquipmentSlotKind _slotKind = PlayerInventoryEquipmentSlotKind.Weapon;

    public void Bind(WeaponSO weapon)
    {
        SetItemVisual(weapon);
        SetSelected(weapon != null);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Right ||
            !HasItem)
        {
            return;
        }

        Owner?.RequestUnequipEquipmentSlot(_slotKind);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        PlayerInventoryDragContext.BeginEquipmentDrag(_slotKind);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        PlayerInventoryDragContext.Clear();
    }

    public override void OnDrop(PointerEventData eventData)
    {
        if (!PlayerInventoryDragContext.HasPayload)
            return;

        if (PlayerInventoryDragContext.SourceKind == PlayerInventoryDragSourceKind.Inventory)
        {
            Owner?.RequestEquipInventoryItemToEquipmentSlot(
                PlayerInventoryDragContext.InventoryIndex,
                _slotKind);
        }

        PlayerInventoryDragContext.Clear();
    }
}