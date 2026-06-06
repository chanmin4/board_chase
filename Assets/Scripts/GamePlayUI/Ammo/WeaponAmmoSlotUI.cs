using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI for one weapon ammo slot.
/// Shows bullet icon, slot key, current ammo, total ammo,
/// active outline, bullet type icon, and sell button.
/// </summary>
[DisallowMultipleComponent]
public class WeaponAmmoSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("Bullet")]
    [SerializeField] private Image _bulletIcon;
    [SerializeField] private Image _ammoTypeIcon;

    [Header("Slot Key")]
    [SerializeField] private TextMeshProUGUI _keyText;

    [Header("Ammo Text")]
    [SerializeField] private TextMeshProUGUI _currentAmmoText;
    [SerializeField] private TextMeshProUGUI _reserveAmmoText;

    [Header("Active Outline")]
    [Tooltip("Enabled when this slot is the active bullet for its ammo type.")]
    [SerializeField] private Outline _selectedOutline;

    [Header("Type Sprites")]
    [Tooltip("Optional. Falls back to Attack Type Sprite when empty.")]
    [SerializeField] private Sprite _attackAndPaintTypeSprite;
    [SerializeField] private Sprite _attackTypeSprite;
    [SerializeField] private Sprite _paintTypeSprite;
    [SerializeField] private Sprite _specialTypeSprite;

    [Header("Text Values")]
    [SerializeField] private string _emptyAmmoText = "-";
    [SerializeField] private string _infiniteReserveText = "\u221E";

    [Header("Sell")]
    [SerializeField] private CanvasGroup _sellButtonGroup;
    [SerializeField] private Button _sellButton;

    private static WeaponAmmoSlotUI _dragSource;

    private WeaponAmmoSlotSnapshot _snapshot;
    private WeaponAmmoHUD _owner;

    public int SlotIndex => _snapshot.slotIndex;
    public bool HasBullet => !_snapshot.isEmpty;

    private void Awake()
    {
        if (_selectedOutline != null)
            _selectedOutline.enabled = false;
    }

    private void OnEnable()
    {
        if (_sellButton != null)
            _sellButton.onClick.AddListener(HandleSellClicked);
    }

    private void OnDisable()
    {
        if (_sellButton != null)
            _sellButton.onClick.RemoveListener(HandleSellClicked);
    }

    public void Initialize(WeaponAmmoHUD owner)
    {
        _owner = owner;
    }

    public void Bind(WeaponAmmoSlotSnapshot snapshot, string keyLabel)
    {
        _snapshot = snapshot;

        if (_keyText != null)
            _keyText.text = keyLabel;

        RefreshBulletIcon(snapshot);
        RefreshAmmoTypeIcon(snapshot);
        RefreshAmmoTexts();
        RefreshSelectedOutline(snapshot);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!HasBullet)
            return;

        _owner?.RequestSelectSlot(SlotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasBullet)
            return;

        _dragSource = this;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragSource == this)
            _dragSource = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_dragSource == null || _dragSource == this)
            return;

        _owner?.RequestSwapSlots(_dragSource.SlotIndex, SlotIndex);
        _dragSource = null;
    }

    public void SetSellModeVisible(bool visible)
    {
        bool canShow = visible && _snapshot.canSell;

        if (_sellButtonGroup != null)
        {
            _sellButtonGroup.alpha = canShow ? 1f : 0f;
            _sellButtonGroup.interactable = canShow;
            _sellButtonGroup.blocksRaycasts = canShow;
        }

        if (_sellButton != null)
            _sellButton.interactable = canShow;
    }

    private void RefreshBulletIcon(WeaponAmmoSlotSnapshot snapshot)
    {
        if (_bulletIcon == null)
            return;

        _bulletIcon.sprite = snapshot.bulletIcon;
        _bulletIcon.enabled = snapshot.bulletIcon != null;
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

    private void SetAmmoText(string current, string total)
    {
        if (_currentAmmoText != null)
            _currentAmmoText.text = current;

        if (_reserveAmmoText != null)
            _reserveAmmoText.text = total;
    }

    private void RefreshSelectedOutline(WeaponAmmoSlotSnapshot snapshot)
    {
        if (_selectedOutline == null)
            return;

        _selectedOutline.enabled = snapshot.isSelected && !snapshot.isEmpty;
    }

    private Sprite GetAmmoTypeSprite(BulletAmmoType bulletType)
    {
        switch (bulletType)
        {
            case BulletAmmoType.AttackAndPaint:
                return _attackAndPaintTypeSprite != null
                    ? _attackAndPaintTypeSprite
                    : _attackTypeSprite;

            case BulletAmmoType.Attack:
                return _attackTypeSprite;

            case BulletAmmoType.Paint:
                return _paintTypeSprite;

            case BulletAmmoType.Special:
                return _specialTypeSprite;

            default:
                return null;
        }
    }

    private void HandleSellClicked()
    {
        if (!_snapshot.canSell)
            return;

        _owner?.RequestSellSlot(_snapshot);
    }
}
