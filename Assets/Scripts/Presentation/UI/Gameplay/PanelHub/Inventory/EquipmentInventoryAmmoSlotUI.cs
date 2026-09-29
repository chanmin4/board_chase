/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents EquipmentInventoryAmmoSlotUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EquipmentInventoryAmmoSlotUI : EquipmentInventorySlotUI
{
    [Header("Ammo Slot")]
    [SerializeField, Min(0)] private int _ammoSlotIndex;

    [Header("Ammo Text")]
    [SerializeField] private TextMeshProUGUI _currentAmmoText;
    [SerializeField] private TextMeshProUGUI _reserveAmmoText;
    [SerializeField] private string _emptyAmmoText = "-";
    [SerializeField] private string _infiniteReserveText = "\u221E";

    private bool _isQuickSlot;
    private int _quickSlotIndex = -1;

    public int AmmoSlotIndex => _ammoSlotIndex;

    // Purpose: Initializes quick slot for use by this component.
    public void InitializeQuickSlot(PlayerInventoryUI owner, int quickSlotIndex)
    {
        _isQuickSlot = true;
        _quickSlotIndex = Mathf.Max(0, quickSlotIndex);
        SetSlotTypeTextVisible(true);
        SetDisplayNameTextVisible(true);
        SetRuntimeSlotTypeOverride($"Quick {_quickSlotIndex + 1}");
        base.Initialize(owner, InventoryDragSourceKind.QuickSlot, _quickSlotIndex);
    }

    // Purpose: Binds quick slot to the active runtime or UI target.
    public void BindQuickSlot(PlayerQuickSlotSnapshot snapshot)
    {
        base.Bind(snapshot.item);

        if (snapshot.IsEmpty)
        {
            SetAmmoText(_emptyAmmoText, string.Empty);
            return;
        }

        if (snapshot.kind == PlayerQuickSlotBindingKind.Ammo && !snapshot.ammo.isEmpty)
        {
            string current = Mathf.Max(0, snapshot.ammo.currentAmmo).ToString();
            string reserve = snapshot.ammo.infiniteReserve
                ? _infiniteReserveText
                : Mathf.Max(0, snapshot.ammo.reserveAmmo).ToString();
            SetAmmoText(current, reserve);
            return;
        }

        if (snapshot.maxDurability > 0.001f)
        {
            SetAmmoText(
                Mathf.CeilToInt(snapshot.currentDurability).ToString(),
                $"/{Mathf.CeilToInt(snapshot.maxDurability)}");
            return;
        }

        int maximum = snapshot.item != null
            ? Mathf.Max(1, snapshot.item.MaxStack)
            : Mathf.Max(1, snapshot.amount);
        SetAmmoText(
            Mathf.Max(0, snapshot.amount).ToString(),
            $"/{maximum}");
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(IInventorySlotOwner owner, int ammoSlotIndex)
    {
        _isQuickSlot = false;
        _quickSlotIndex = -1;
        SetSlotTypeTextVisible(true);
        SetDisplayNameTextVisible(true);
        SetRuntimeSlotTypeOverride(null);
        _ammoSlotIndex = Mathf.Max(0, ammoSlotIndex);
        base.Initialize(owner, InventoryDragSourceKind.EquipmentAmmo, _ammoSlotIndex);
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(
        IInventorySlotOwner owner,
        int ammoSlotIndex,
        InventoryDragSourceKind dragSourceKind,
        int dragSlotIndex)
    {
        _isQuickSlot = false;
        _quickSlotIndex = -1;
        SetSlotTypeTextVisible(true);
        SetDisplayNameTextVisible(true);
        SetRuntimeSlotTypeOverride(null);
        _ammoSlotIndex = Mathf.Max(0, ammoSlotIndex);
        base.Initialize(owner, dragSourceKind, dragSlotIndex);
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(WeaponAmmoSlotSnapshot snapshot)
    {
        base.Bind(snapshot.bullet);

        if (snapshot.bullet == null)
        {
            SetAmmoText(_emptyAmmoText, _emptyAmmoText);
            return;
        }

        string current = Mathf.Max(0, snapshot.currentAmmo).ToString();
        string reserve = snapshot.infiniteReserve
            ? _infiniteReserveText
            : Mathf.Max(0, snapshot.reserveAmmo).ToString();

        SetAmmoText(current, reserve);
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(BulletSO bullet, int amount)
    {
        base.Bind(bullet);

        if (bullet == null)
        {
            SetAmmoText(_emptyAmmoText, _emptyAmmoText);
            return;
        }

        SetAmmoText(Mathf.Max(0, amount).ToString(), string.Empty);
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
            Owner?.HandleAmmoSlotRightClicked(_ammoSlotIndex, screenPosition);
            return;
        }

        Owner?.HandleAmmoSlotRightClicked(_ammoSlotIndex, screenPosition);
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

        Owner?.HandleAmmoSlotLeftClicked(_ammoSlotIndex);
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
            playerInventory.HandleAmmoSlotDoubleClicked(_ammoSlotIndex);
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

        InventoryDragContext.BeginEquipmentAmmoDrag(_ammoSlotIndex);
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
                enemyInventory.TryReceiveDragPayloadToEnemyAmmoSlot(_ammoSlotIndex);

            return;
        }

        if (InventoryDragContext.SourceKind == InventoryDragSourceKind.Inventory)
        {
            Owner?.HandleInventorySlotDroppedOnAmmo(
                InventoryDragContext.InventoryIndex,
                _ammoSlotIndex);
        }
        else if (InventoryDragContext.SourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            InventoryDragContext.EnemyInventoryOwner?.TryTakeAmmoToPlayerAmmoSlot(
                InventoryDragContext.EnemyInventorySlotIndex,
                _ammoSlotIndex);
        }
        else if (InventoryDragContext.SourceKind == InventoryDragSourceKind.EquipmentAmmo)
        {
            Owner?.HandleAmmoSlotDroppedOnAmmo(
                InventoryDragContext.EquipmentAmmoSlotIndex,
                _ammoSlotIndex);
        }
    }

    // Purpose: Updates the ammo text state.
    private void SetAmmoText(string current, string reserve)
    {
        if (_currentAmmoText != null)
            _currentAmmoText.text = current ?? string.Empty;

        if (_reserveAmmoText != null)
            _reserveAmmoText.text = reserve ?? string.Empty;
    }

}
