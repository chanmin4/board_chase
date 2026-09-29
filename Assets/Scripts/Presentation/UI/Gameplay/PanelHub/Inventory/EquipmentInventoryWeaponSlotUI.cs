/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents EquipmentInventoryWeaponSlotUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EquipmentInventoryWeaponSlotUI : EquipmentInventorySlotUI
{
    [Header("Weapon Slot")]
    [Tooltip("Weapon equipment slot index. 0 is primary weapon, 1 is secondary/throwable by convention.")]
    [SerializeField, Min(0)] private int _weaponSlotIndex;

    [Header("Durability")]
    [Tooltip("Current/max firearm durability shown on this equipment slot. When empty, the widget is created once under the slot so every existing Inspector connection remains valid.")]
    [SerializeField] private TextMeshProUGUI _durabilityText;
    [SerializeField] private Color _normalDurabilityTextColor = Color.white;
    [SerializeField] private Color _depletedDurabilityTextColor = Color.red;

    private float _currentDurability = -1f;
    private float _maxDurability = -1f;
    private bool _isQuickSlot;
    private int _quickSlotIndex = -1;
    private string _quickSlotSupplementalText;

    public int WeaponSlotIndex => _weaponSlotIndex;

    // Purpose: Initializes initialize for use by this component.
    public override void Initialize(IInventorySlotOwner owner)
    {
        _isQuickSlot = false;
        _quickSlotIndex = -1;
        _quickSlotSupplementalText = null;
        SetRuntimeSlotTypeOverride(null);
        base.Initialize(owner);
    }

    // Purpose: Initializes quick slot for use by this component.
    public void InitializeQuickSlot(PlayerInventoryUI owner, int quickSlotIndex)
    {
        _isQuickSlot = true;
        _quickSlotIndex = Mathf.Max(0, quickSlotIndex);
        SetRuntimeSlotTypeOverride($"Quick {_quickSlotIndex + 1}");
        base.Initialize(owner, InventoryDragSourceKind.QuickSlot, _quickSlotIndex);
    }

    // Purpose: Binds quick slot to the active runtime or UI target.
    public void BindQuickSlot(PlayerQuickSlotSnapshot snapshot)
    {
        _currentDurability = snapshot.currentDurability;
        _maxDurability = snapshot.maxDurability;
        _quickSlotSupplementalText = ResolveQuickSlotSupplementalText(snapshot);
        base.Bind(snapshot.item);
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(WeaponSO weapon, float currentDurability, float maxDurability)
    {
        _currentDurability = currentDurability;
        _maxDurability = maxDurability;
        _quickSlotSupplementalText = null;
        base.Bind(weapon);
    }

    // Purpose: Responds to the bound callback.
    protected override void OnBound(ItemSO item)
    {
        EnsureDurabilityText();

        if (_durabilityText == null)
            return;

        if (item is not RangedWeaponSO rangedWeapon)
        {
            bool showSupplemental =
                _isQuickSlot &&
                !string.IsNullOrWhiteSpace(_quickSlotSupplementalText);
            _durabilityText.text = showSupplemental
                ? _quickSlotSupplementalText
                : string.Empty;
            _durabilityText.color = _normalDurabilityTextColor;
            _durabilityText.gameObject.SetActive(showSupplemental);
            return;
        }

        float max = _maxDurability > 0f ? _maxDurability : rangedWeapon.MaxDurability;
        float current = _currentDurability >= 0f ? _currentDurability : max;
        _durabilityText.text = $"{Mathf.Max(0f, current):0}/{Mathf.Max(0f, max):0}";
        _durabilityText.color = max > 0f && current <= 0f
            ? _depletedDurabilityTextColor
            : _normalDurabilityTextColor;
        _durabilityText.gameObject.SetActive(true);
    }

    // Purpose: Resolves the quick slot supplemental text needed by this component.
    private static string ResolveQuickSlotSupplementalText(PlayerQuickSlotSnapshot snapshot)
    {
        if (snapshot.IsEmpty)
            return string.Empty;

        if (snapshot.kind == PlayerQuickSlotBindingKind.Ammo && !snapshot.ammo.isEmpty)
        {
            string reserve = snapshot.ammo.infiniteReserve
                ? "\u221E"
                : Mathf.Max(0, snapshot.ammo.reserveAmmo).ToString();
            return $"{Mathf.Max(0, snapshot.ammo.currentAmmo)}/{reserve}";
        }

        return snapshot.kind == PlayerQuickSlotBindingKind.Usable
            ? $"x{Mathf.Max(0, snapshot.amount)}"
            : string.Empty;
    }

    // Purpose: Publishes the item detail selection notification.
    protected override void PublishItemDetailSelection()
    {
        if (Item == null)
            return;

        ItemDetailSelectionBus.Publish(new ItemDetailSelection(
            Item,
            1,
            -1f,
            -1f,
            _currentDurability,
            _maxDurability));
    }

    // Purpose: Ensures durability text is ready before the workflow continues.
    private void EnsureDurabilityText()
    {
        if (_durabilityText != null)
            return;

        GameObject textObject = new("WeaponDurability", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.05f, 0f);
        rect.anchorMax = new Vector2(0.95f, 0.25f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _durabilityText = textObject.GetComponent<TextMeshProUGUI>();
        _durabilityText.alignment = TextAlignmentOptions.Center;
        _durabilityText.fontSize = 15f;
        _durabilityText.enableAutoSizing = true;
        _durabilityText.fontSizeMin = 8f;
        _durabilityText.fontSizeMax = 15f;
        _durabilityText.raycastTarget = false;
    }

    // Purpose: Handles the incoming right click request or event.
    protected override void HandleRightClick(Vector2 screenPosition)
    {
        if (_isQuickSlot && Owner is PlayerInventoryUI quickSlotOwner)
        {
            quickSlotOwner.HandleQuickSlotRightClicked(_quickSlotIndex);
            return;
        }

        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            if (Owner is EnemyInventoryUI enemyInventory)
                enemyInventory.HandleWeaponSlotRightClicked(_weaponSlotIndex, screenPosition);

            return;
        }

        if (Owner is PlayerInventoryUI playerInventory)
            playerInventory.HandleWeaponSlotRightClicked(_weaponSlotIndex, screenPosition);
        else
            Owner?.HandleEquipmentSlotRightClicked(
                PlayerInventoryEquipmentSlotKind.Weapon,
                screenPosition);
    }

    // Purpose: Handles the incoming left click request or event.
    protected override void HandleLeftClick()
    {
        if (_isQuickSlot && Owner is PlayerInventoryUI quickSlotOwner)
        {
            quickSlotOwner.HandleQuickSlotLeftClicked(_quickSlotIndex);
            return;
        }

        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            Owner?.HandleInventorySlotLeftClicked(DragSlotIndex);
            return;
        }

        if (Owner is PlayerInventoryUI playerInventory)
            playerInventory.HandleWeaponSlotLeftClicked(_weaponSlotIndex);
        else
            Owner?.HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind.Weapon);
    }

    // Purpose: Handles the incoming double left click request or event.
    protected override void HandleDoubleLeftClick()
    {
        if (_isQuickSlot && Owner is PlayerInventoryUI quickSlotOwner)
        {
            quickSlotOwner.HandleQuickSlotDoubleClicked(_quickSlotIndex);
            return;
        }

        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            HandleLeftClick();
            return;
        }

        if (Owner is PlayerInventoryUI playerInventory)
        {
            playerInventory.HandleEquipmentSlotDoubleClicked(
                PlayerInventoryEquipmentSlotKind.Weapon,
                _weaponSlotIndex);
            return;
        }

        HandleLeftClick();
    }

    // Purpose: Begins the drag payload workflow.
    protected override void BeginDragPayload()
    {
        if (_isQuickSlot)
        {
            InventoryDragContext.BeginQuickSlotDrag(_quickSlotIndex);
            return;
        }

        if (TryBeginEnemyInventoryDrag())
            return;

        InventoryDragContext.BeginEquipmentDrag(
            PlayerInventoryEquipmentSlotKind.Weapon,
            _weaponSlotIndex);
    }

    // Purpose: Handles the incoming drop payload request or event.
    protected override void HandleDropPayload()
    {
        if (_isQuickSlot && Owner is PlayerInventoryUI quickSlotOwner)
        {
            quickSlotOwner.HandleQuickSlotDrop(_quickSlotIndex);
            return;
        }

        if (IsEnemyInventoryTarget)
        {
            if (Owner is EnemyInventoryUI enemyInventory)
            {
                enemyInventory.TryReceiveDragPayloadToEnemyEquipmentSlot(
                    PlayerInventoryEquipmentSlotKind.Weapon,
                    _weaponSlotIndex);
            }

            return;
        }

        if (InventoryDragContext.SourceKind == InventoryDragSourceKind.Inventory)
        {
            if (Owner is PlayerInventoryUI playerInventory)
            {
                playerInventory.HandleInventorySlotDroppedOnWeaponSlot(
                    InventoryDragContext.InventoryIndex,
                    _weaponSlotIndex);
            }
            else
            {
                Owner?.HandleInventorySlotDroppedOnEquipment(
                    InventoryDragContext.InventoryIndex,
                    PlayerInventoryEquipmentSlotKind.Weapon);
            }
        }
        else if (InventoryDragContext.SourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            InventoryDragContext.EnemyInventoryOwner?.TryTakeSlotToPlayerEquipmentSlot(
                InventoryDragContext.EnemyInventorySlotIndex,
                PlayerInventoryEquipmentSlotKind.Weapon,
                _weaponSlotIndex);
        }
        else if (InventoryDragContext.SourceKind == InventoryDragSourceKind.Equipment &&
                 InventoryDragContext.EquipmentSlotKind == PlayerInventoryEquipmentSlotKind.Weapon &&
                 Owner is PlayerInventoryUI playerInventory)
        {
            playerInventory.HandleWeaponSlotDroppedOnWeaponSlot(
                InventoryDragContext.EquipmentSlotIndex,
                _weaponSlotIndex);
        }
    }
}
