/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Displays and operates the player's dedicated head gas-mask slot.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EquipmentInventoryGasMaskSlotUI : EquipmentInventorySlotUI
{
    [Header("Filter Durability Text")]
    [SerializeField] private TextMeshProUGUI _currentFilterText;
    [SerializeField] private TextMeshProUGUI _maxFilterText;

    private float _currentFilter = -1f;
    private float _maxFilter = -1f;

    // Purpose: Applies the fixed head-slot label before first UI binding.
    protected override void Awake()
    {
        base.Awake();
        SetRuntimeSlotTypeOverride("HEAD");
    }

    // Purpose: Binds the equipped gas mask and its current filter condition.
    public void Bind(GasMaskItemSO gasMask, float currentFilter, float maxFilter)
    {
        _currentFilter = currentFilter;
        _maxFilter = maxFilter;
        base.Bind(gasMask);
    }

    // Purpose: Updates the filter text after the slot binding changes.
    protected override void OnBound(ItemSO item)
    {
        GasMaskItemSO gasMask = item as GasMaskItemSO;
        if (gasMask == null)
        {
            SetFilterText(string.Empty, string.Empty);
            return;
        }

        float max = _maxFilter >= 0f ? _maxFilter : gasMask.MaxFilterDurability;
        float current = _currentFilter >= 0f ? _currentFilter : max;
        SetFilterText(Mathf.Max(0f, current).ToString("0"), Mathf.Max(0f, max).ToString("0"));
    }

    // Purpose: Publishes a detail selection that includes current filter condition.
    protected override void PublishItemDetailSelection()
    {
        if (Item == null)
            return;

        ItemDetailSelectionBus.Publish(new ItemDetailSelection(
            Item,
            1,
            -1f,
            -1f,
            -1f,
            -1f,
            _currentFilter,
            _maxFilter));
    }

    // Purpose: Selects the head slot for sell or equipment UI workflows.
    protected override void HandleLeftClick()
    {
        Owner?.HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind.Head);
    }

    // Purpose: Double-click unequips the head item into the first valid inventory slot.
    protected override void HandleDoubleLeftClick()
    {
        if (Owner is PlayerInventoryUI playerInventory)
        {
            playerInventory.HandleEquipmentSlotDoubleClicked(PlayerInventoryEquipmentSlotKind.Head);
            return;
        }

        HandleLeftClick();
    }

    // Purpose: Opens the normal equipment context menu for the head slot.
    protected override void HandleRightClick(Vector2 screenPosition)
    {
        Owner?.HandleEquipmentSlotRightClicked(PlayerInventoryEquipmentSlotKind.Head, screenPosition);
    }

    // Purpose: Starts a head-equipment drag payload.
    protected override void BeginDragPayload()
    {
        InventoryDragContext.BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind.Head);
    }

    // Purpose: Accepts an inventory gas mask dropped onto the head slot.
    protected override void HandleDropPayload()
    {
        if (InventoryDragContext.SourceKind == InventoryDragSourceKind.Inventory)
        {
            Owner?.HandleInventorySlotDroppedOnEquipment(
                InventoryDragContext.InventoryIndex,
                PlayerInventoryEquipmentSlotKind.Head);
        }
    }

    // Purpose: Updates optional filter-condition labels without requiring a specific visual prefab.
    private void SetFilterText(string current, string max)
    {
        if (_currentFilterText != null)
            _currentFilterText.text = current ?? string.Empty;

        if (_maxFilterText != null)
            _maxFilterText.text = max ?? string.Empty;
    }
}
