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

    public void Bind(
        ArmorItemSO armor,
        float currentDurability,
        float maxDurability)
    {
        _currentDurability = currentDurability;
        _maxDurability = maxDurability;
        base.Bind(armor);
    }

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

    protected override void HandleRightClick(Vector2 screenPosition)
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            Owner?.HandleInventorySlotRightClicked(DragSlotIndex, screenPosition);
            return;
        }

        Owner?.HandleEquipmentSlotRightClicked(
            PlayerInventoryEquipmentSlotKind.Armor,
            screenPosition);
    }

    protected override void HandleLeftClick()
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            Owner?.HandleInventorySlotLeftClicked(DragSlotIndex);
            return;
        }

        Owner?.HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind.Armor);
    }

    protected override void BeginDragPayload()
    {
        if (TryBeginEnemyInventoryDrag())
            return;

        InventoryDragContext.BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind.Armor);
    }

    protected override void HandleDropPayload()
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
            return;

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
