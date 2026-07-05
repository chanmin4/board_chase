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

    public int AmmoSlotIndex => _ammoSlotIndex;

    public void Initialize(IInventorySlotOwner owner, int ammoSlotIndex)
    {
        _ammoSlotIndex = Mathf.Max(0, ammoSlotIndex);
        base.Initialize(owner, InventoryDragSourceKind.EquipmentAmmo, _ammoSlotIndex);
    }

    public void Initialize(
        IInventorySlotOwner owner,
        int ammoSlotIndex,
        InventoryDragSourceKind dragSourceKind,
        int dragSlotIndex)
    {
        _ammoSlotIndex = Mathf.Max(0, ammoSlotIndex);
        base.Initialize(owner, dragSourceKind, dragSlotIndex);
    }

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

    protected override void HandleRightClick(Vector2 screenPosition)
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            Owner?.HandleInventorySlotRightClicked(DragSlotIndex, screenPosition);
            return;
        }

        Owner?.HandleAmmoSlotRightClicked(_ammoSlotIndex, screenPosition);
    }

    protected override void HandleLeftClick()
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
        {
            Owner?.HandleInventorySlotLeftClicked(DragSlotIndex);
            return;
        }

        Owner?.HandleAmmoSlotLeftClicked(_ammoSlotIndex);
    }

    protected override void BeginDragPayload()
    {
        if (TryBeginEnemyInventoryDrag())
            return;

        InventoryDragContext.BeginEquipmentAmmoDrag(_ammoSlotIndex);
    }

    protected override void HandleDropPayload()
    {
        if (DragSourceKind == InventoryDragSourceKind.EnemyInventory)
            return;

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

    private void SetAmmoText(string current, string reserve)
    {
        if (_currentAmmoText != null)
            _currentAmmoText.text = current ?? string.Empty;

        if (_reserveAmmoText != null)
            _reserveAmmoText.text = reserve ?? string.Empty;
    }
}
