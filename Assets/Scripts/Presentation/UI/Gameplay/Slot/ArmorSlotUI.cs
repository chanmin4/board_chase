/*
 * Role hierarchy: Presentation > UI > Gameplay > Slot
 * Primary responsibility: Presents ArmorSlotUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// ArmorSlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ArmorSlotUI : SlotUI
{
    [Header("Slot")]
    [SerializeField] private PlayerInventoryEquipmentSlotKind _slotKind =
        PlayerInventoryEquipmentSlotKind.Armor;

    [Header("Armor Text")]
    [SerializeField] private TextMeshProUGUI _currentDurabilityText;
    [SerializeField] private TextMeshProUGUI _fullDurabilityText;
    [SerializeField] private TextMeshProUGUI _finalArmorClassText;

    [Header("Text Format")]
    [SerializeField] private string _emptyDurabilityText = "-";
    [SerializeField] private string _finalArmorClassFormat = "DEF {0}";

    [Header("Durability Color")]
    [SerializeField] private Color _normalDurabilityTextColor = Color.white;
    [SerializeField] private Color _depletedDurabilityTextColor = Color.red;


    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(
        ArmorItemSO armor,
        float currentDurability,
        float maxDurability,
        float finalArmorClass)
    {
        SetItemVisual(armor);
        SetSelected(armor != null);

        RefreshDurability(
            armor,
            currentDurability,
            maxDurability);

        RefreshFinalArmorClass(finalArmorClass);
    }

    // Purpose: Responds to the pointer click callback.
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || !HasItem)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right ||
            (eventData.button == PointerEventData.InputButton.Left &&
             eventData.clickCount >= 2))
        {
            Owner?.RequestUnequipEquipmentSlot(_slotKind);
        }
    }

    // Purpose: Responds to the begin drag callback.
    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        PlayerInventoryDragContext.BeginEquipmentDrag(_slotKind);
    }

    // Purpose: Responds to the end drag callback.
    public override void OnEndDrag(PointerEventData eventData)
    {
        PlayerInventoryDragContext.Clear();
    }

    // Purpose: Responds to the drop callback.
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

    // Purpose: Refreshes the displayed or cached durability.
    private void RefreshDurability(
        ArmorItemSO armor,
        float currentDurability,
        float maxDurability)
    {
        bool hasArmor = armor != null;
        bool hasDurability = hasArmor && maxDurability > 0f;
        bool armorBroken = hasArmor && hasDurability && currentDurability <= 0f;

        string currentText = hasDurability
            ? Mathf.Max(0f, currentDurability).ToString("0")
            : _emptyDurabilityText;

        string fullText = hasDurability
            ? Mathf.Max(0f, maxDurability).ToString("0")
            : _emptyDurabilityText;

        Color durabilityTextColor = armorBroken
            ? _depletedDurabilityTextColor
            : _normalDurabilityTextColor;

        if (_currentDurabilityText != null)
        {
            _currentDurabilityText.text = currentText;
            _currentDurabilityText.color = durabilityTextColor;
        }

        if (_fullDurabilityText != null)
        {
            _fullDurabilityText.text = fullText;
            _fullDurabilityText.color = durabilityTextColor;
        }
    }
    // Purpose: Refreshes the displayed or cached final armor class.
    private void RefreshFinalArmorClass(float finalArmorClass)
    {
        if (_finalArmorClassText == null)
            return;

        _finalArmorClassText.text = string.Format(
            _finalArmorClassFormat,
            Mathf.Max(0f, finalArmorClass).ToString("0.#"));
    }


}
