/*
 * Role hierarchy: Presentation > UI > Gameplay > Slot
 * Primary responsibility: Presents AmmoSlotUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// AmmoSlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AmmoSlotUI : SlotUI
{
    [Header("Ammo")]
    [SerializeField] private Image _ammoTypeIcon;
    [SerializeField] private TextMeshProUGUI _currentAmmoText;
    [SerializeField] private TextMeshProUGUI _reserveAmmoText;

    [Header("Type Sprites")]
    [FormerlySerializedAs("_attackAndPaintTypeSprite")]
    [Tooltip("Icon used by every normal ammunition item. HP, Infection, Mark, and paint are values on the same ammo definition.")]
    [SerializeField] private Sprite _basicTypeSprite;
    [SerializeField] private Sprite _specialTypeSprite;

    [Header("Text")]
    [SerializeField] private string _emptyAmmoText = "-";
    [SerializeField] private string _infiniteReserveText = "\u221E";

    private int _quickSlotIndex;
    public int QuickSlotIndex => _quickSlotIndex;

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(SlotHUD owner, int quickSlotIndex)
    {
        base.Initialize(owner);
        _quickSlotIndex = Mathf.Max(0, quickSlotIndex);
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(PlayerQuickSlotSnapshot snapshot, string keyLabel)
    {
        SetKeyLabel(keyLabel);
        SetItemVisual(snapshot.item);
        RefreshSupplementalVisuals(snapshot);
        SetSelected(snapshot.isSelected && !snapshot.IsEmpty);
    }

    // Purpose: Clears the current clear state.
    public void Clear(int quickSlotIndex, string keyLabel)
    {
        _quickSlotIndex = Mathf.Max(0, quickSlotIndex);
        Bind(default, keyLabel);
    }

    // Purpose: Responds to the pointer click callback.
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null ||
            !HasItem)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.clickCount >= 2)
                Owner?.RequestUnequipQuickSlot(_quickSlotIndex);
            else
                Owner?.RequestActivateQuickSlot(_quickSlotIndex);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
            Owner?.RequestClearQuickSlot(_quickSlotIndex);
    }

    // Purpose: Responds to the begin drag callback.
    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        PlayerInventoryDragContext.BeginQuickSlotDrag(_quickSlotIndex);
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

        Owner?.RequestBindQuickSlotFromDrag(_quickSlotIndex);
        PlayerInventoryDragContext.Clear();
    }

    // Purpose: Refreshes the displayed or cached supplemental visuals.
    private void RefreshSupplementalVisuals(PlayerQuickSlotSnapshot snapshot)
    {
        if (snapshot.kind == PlayerQuickSlotBindingKind.Ammo && !snapshot.ammo.isEmpty)
        {
            RefreshAmmoTypeIcon(snapshot.ammo);
            RefreshAmmoTexts(snapshot.ammo);
            return;
        }

        if (_ammoTypeIcon != null)
        {
            _ammoTypeIcon.sprite = null;
            _ammoTypeIcon.enabled = false;
        }

        if (snapshot.maxDurability > 0.001f)
        {
            SetAmmoText(
                Mathf.CeilToInt(snapshot.currentDurability).ToString(),
                Mathf.CeilToInt(snapshot.maxDurability).ToString());
            return;
        }

        if (snapshot.IsEmpty)
        {
            SetAmmoText(_emptyAmmoText, string.Empty);
            return;
        }

        // A usable quick slot spends one item per activation. The authored
        // prefab already owns the slash separator, so display "1 / stack".
        SetAmmoText(
            "1",
            Mathf.Max(0, snapshot.amount).ToString());
    }

    // Purpose: Refreshes the displayed or cached ammo type icon.
    private void RefreshAmmoTypeIcon(WeaponAmmoSlotSnapshot snapshot)
    {
        if (_ammoTypeIcon == null)
            return;

        _ammoTypeIcon.sprite = GetAmmoTypeSprite(snapshot.bulletType);
        _ammoTypeIcon.enabled = _ammoTypeIcon.sprite != null;
    }

    // Purpose: Refreshes the displayed or cached ammo texts.
    private void RefreshAmmoTexts(WeaponAmmoSlotSnapshot snapshot)
    {
        if (snapshot.isEmpty)
        {
            SetAmmoText(_emptyAmmoText, _emptyAmmoText);
            return;
        }

        if (snapshot.infiniteReserve)
        {
            SetAmmoText(
                Mathf.Max(0, snapshot.currentAmmo).ToString(),
                _infiniteReserveText);
            return;
        }

        SetAmmoText(
            Mathf.Max(0, snapshot.currentAmmo).ToString(),
            Mathf.Max(0, snapshot.reserveAmmo).ToString());
    }

    // Purpose: Updates the ammo text state.
    private void SetAmmoText(string current, string reserve)
    {
        if (_currentAmmoText != null)
            _currentAmmoText.text = current;

        if (_reserveAmmoText != null)
            _reserveAmmoText.text = reserve;
    }

    // Purpose: Returns the current ammo type sprite.
    private Sprite GetAmmoTypeSprite(BulletAmmoType bulletType)
    {
        return bulletType switch
        {
            BulletAmmoType.Basic => _basicTypeSprite,
            BulletAmmoType.Special => _specialTypeSprite,
            _ => null
        };
    }

}
