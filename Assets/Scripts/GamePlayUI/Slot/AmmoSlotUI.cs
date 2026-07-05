// AmmoSlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AmmoSlotUI : SlotUI
{
    [Header("Ammo")]
    [SerializeField] private Image _ammoTypeIcon;
    [SerializeField] private TextMeshProUGUI _currentAmmoText;
    [SerializeField] private TextMeshProUGUI _reserveAmmoText;

    [Header("Type Sprites")]
    [SerializeField] private Sprite _attackAndPaintTypeSprite;
    [SerializeField] private Sprite _attackTypeSprite;
    [SerializeField] private Sprite _paintTypeSprite;
    [SerializeField] private Sprite _specialTypeSprite;

    [Header("Text")]
    [SerializeField] private string _emptyAmmoText = "-";
    [SerializeField] private string _infiniteReserveText = "\u221E";

    private static AmmoSlotUI _dragSource;

    private WeaponAmmoSlotSnapshot _snapshot;

    public int SlotIndex => _snapshot.slotIndex;
    public bool HasBullet => !_snapshot.isEmpty;

    public void Bind(WeaponAmmoSlotSnapshot snapshot, string keyLabel)
    {
        _snapshot = snapshot;

        SetKeyLabel(keyLabel);
        SetItemVisual(snapshot.bullet, snapshot.bulletIcon, snapshot.displayName);
        RefreshAmmoTypeIcon(snapshot);
        RefreshAmmoTexts();
        SetSelected(snapshot.isSelected && !snapshot.isEmpty);
    }

    public void Clear(int slotIndex, string keyLabel)
    {
        Bind(
            new WeaponAmmoSlotSnapshot(
                slotIndex,
                null,
                0,
                0,
                false,
                false,
                false,
                0),
            keyLabel);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Left ||
            !HasBullet)
        {
            return;
        }

        Owner?.RequestSelectAmmoSlot(SlotIndex);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasBullet)
            return;

        _dragSource = this;
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (_dragSource == this)
            _dragSource = null;
    }

    public override void OnDrop(PointerEventData eventData)
    {
        if (PlayerInventoryDragContext.HasPayload &&
            PlayerInventoryDragContext.SourceKind == PlayerInventoryDragSourceKind.Inventory)
        {
            Owner?.RequestEquipInventoryBulletToAmmoSlot(
                PlayerInventoryDragContext.InventoryIndex,
                SlotIndex);

            PlayerInventoryDragContext.Clear();
            return;
        }

        if (PlayerInventoryDragContext.HasPayload &&
            PlayerInventoryDragContext.SourceKind == PlayerInventoryDragSourceKind.EnemyInventory)
        {
            Owner?.RequestEquipEnemyInventoryAmmoToAmmoSlot(
                PlayerInventoryDragContext.EnemyInventoryOwner,
                PlayerInventoryDragContext.EnemyInventorySlotIndex,
                SlotIndex);

            PlayerInventoryDragContext.Clear();
            return;
        }

        if (_dragSource == null || _dragSource == this)
            return;

        Owner?.RequestSwapAmmoSlots(_dragSource.SlotIndex, SlotIndex);
        _dragSource = null;
    }

    private void RefreshAmmoTypeIcon(WeaponAmmoSlotSnapshot snapshot)
    {
        if (_ammoTypeIcon == null)
            return;

        if (snapshot.isEmpty)
        {
            _ammoTypeIcon.sprite = null;
            _ammoTypeIcon.enabled = false;
            return;
        }

        _ammoTypeIcon.sprite = GetAmmoTypeSprite(snapshot.bulletType);
        _ammoTypeIcon.enabled = _ammoTypeIcon.sprite != null;
    }

    private void RefreshAmmoTexts()
    {
        if (_snapshot.isEmpty)
        {
            SetAmmoText(_emptyAmmoText, _emptyAmmoText);
            return;
        }

        if (_snapshot.infiniteReserve)
        {
            SetAmmoText(
                Mathf.Max(0, _snapshot.currentAmmo).ToString(),
                _infiniteReserveText);
            return;
        }

        SetAmmoText(
            Mathf.Max(0, _snapshot.currentAmmo).ToString(),
            Mathf.Max(0, _snapshot.reserveAmmo).ToString());
    }

    private void SetAmmoText(string current, string reserve)
    {
        if (_currentAmmoText != null)
            _currentAmmoText.text = current;

        if (_reserveAmmoText != null)
            _reserveAmmoText.text = reserve;
    }

    private Sprite GetAmmoTypeSprite(BulletAmmoType bulletType)
    {
        return bulletType switch
        {
            BulletAmmoType.AttackAndPaint => _attackAndPaintTypeSprite != null
                ? _attackAndPaintTypeSprite
                : _attackTypeSprite,
            BulletAmmoType.Attack => _attackTypeSprite,
            BulletAmmoType.Paint => _paintTypeSprite,
            BulletAmmoType.Special => _specialTypeSprite,
            _ => null
        };
    }

}
