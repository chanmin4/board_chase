/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents EquipmentInventoryArmorSlotUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EquipmentInventoryArmorSlotUI : EquipmentInventorySlotUI
{
    [Header("Durability Text")]
    [SerializeField] private TextMeshProUGUI _currentDurabilityText;
    [SerializeField] private TextMeshProUGUI _fullDurabilityText;

    [Header("Durability Color")]
    [SerializeField] private Color _normalDurabilityTextColor = Color.white;
    [SerializeField] private Color _depletedDurabilityTextColor = Color.red;

    private float _currentDurability = -1f;
    private float _maxDurability = -1f;

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(
        ArmorItemSO armor,
        float currentDurability,
        float maxDurability)
    {
        _currentDurability = currentDurability;
        _maxDurability = maxDurability;
        base.Bind(armor);
    }

    // Purpose: Responds to the bound callback.
    protected override void OnBound(ItemSO item)
    {
        ArmorItemSO armor = item as ArmorItemSO;

        if (armor == null)
        {
            SetDurabilityText(string.Empty, string.Empty, _normalDurabilityTextColor);
            return;
        }

        float maxDurability = _maxDurability >= 0f
            ? _maxDurability
            : armor.MaxDurability;

        float currentDurability = _currentDurability >= 0f
            ? _currentDurability
            : maxDurability;

        Color color = maxDurability > 0f && currentDurability <= 0f
            ? _depletedDurabilityTextColor
            : _normalDurabilityTextColor;

        SetDurabilityText(
            Mathf.Max(0f, currentDurability).ToString("0"),
            Mathf.Max(0f, maxDurability).ToString("0"),
            color);
    }

    // Purpose: Publishes the item detail selection notification.
    protected override void PublishItemDetailSelection()
    {
        if (Item == null)
            return;

        ItemDetailSelectionBus.Publish(new ItemDetailSelection(
            Item,
            1,
            _currentDurability,
            _maxDurability));
    }

    // Purpose: Handles the incoming right click request or event.
    protected override void HandleRightClick(Vector2 screenPosition)
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            Owner?.HandleEquipmentSlotRightClicked(
                PlayerInventoryEquipmentSlotKind.Armor,
                screenPosition);
            return;
        }

        Owner?.HandleEquipmentSlotRightClicked(
            PlayerInventoryEquipmentSlotKind.Armor,
            screenPosition);
    }

    // Purpose: Handles the incoming left click request or event.
    protected override void HandleLeftClick()
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            Owner?.HandleInventorySlotLeftClicked(DragSlotIndex);
            return;
        }

        Owner?.HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind.Armor);
    }

    // Purpose: Handles the incoming double left click request or event.
    protected override void HandleDoubleLeftClick()
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            HandleLeftClick();
            return;
        }

        if (Owner is PlayerInventoryUI playerInventory)
        {
            playerInventory.HandleEquipmentSlotDoubleClicked(
                PlayerInventoryEquipmentSlotKind.Armor);
            return;
        }

        HandleLeftClick();
    }

    // Purpose: Begins the drag payload workflow.
    protected override void BeginDragPayload()
    {
        if (TryBeginEnemyInventoryDrag())
            return;

        InventoryDragContext.BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind.Armor);
    }

    // Purpose: Handles the incoming drop payload request or event.
    protected override void HandleDropPayload()
    {
        if (IsEnemyInventoryTarget)
        {
            if (Owner is EnemyInventoryUI enemyInventory)
            {
                enemyInventory.TryReceiveDragPayloadToEnemyEquipmentSlot(
                    PlayerInventoryEquipmentSlotKind.Armor);
            }

            return;
        }

        if (InventoryDragContext.SourceKind == InventoryDragSourceKind.Inventory)
        {
            Owner?.HandleInventorySlotDroppedOnEquipment(
                InventoryDragContext.InventoryIndex,
                PlayerInventoryEquipmentSlotKind.Armor);
        }
        else if (InventoryDragContext.SourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            InventoryDragContext.EnemyInventoryOwner?.TryTakeSlotToPlayerEquipmentSlot(
                InventoryDragContext.EnemyInventorySlotIndex,
                PlayerInventoryEquipmentSlotKind.Armor);
        }
    }

    // Purpose: Updates the durability text state.
    private void SetDurabilityText(string current, string full, Color color)
    {
        if (_currentDurabilityText != null)
        {
            _currentDurabilityText.text = current ?? string.Empty;
            _currentDurabilityText.color = color;
        }

        if (_fullDurabilityText != null)
        {
            _fullDurabilityText.text = full ?? string.Empty;
            _fullDurabilityText.color = color;
        }
    }
}
