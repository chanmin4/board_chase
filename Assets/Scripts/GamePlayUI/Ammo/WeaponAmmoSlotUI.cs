using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI for one weapon ammo slot.
/// Shows bullet icon, slot key, current magazine ammo, reserve ammo,
/// selected outline, and bullet type icon.
/// Empty slots cannot be selected, but can receive dropped bullets if runtime rules allow it.
/// </summary>
[DisallowMultipleComponent]
public class WeaponAmmoSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("Bullet")]
    [Tooltip("Main bullet icon image. Uses BulletSO.Icon.")]
    [SerializeField] private Image _bulletIcon;

    [Tooltip("Small icon that represents bullet type: Attack, Paint, Special.")]
    [SerializeField] private Image _ammoTypeIcon;

    [Header("Slot Key")]
    [Tooltip("Slot key label. Example: 1, 2, Left Shift.")]
    [SerializeField] private TextMeshProUGUI _keyText;

    [Header("Ammo Text")]
    [Tooltip("Current magazine ammo text. Example: 3 in 3 / 15.")]
    [SerializeField] private TextMeshProUGUI _currentAmmoText;

    [Tooltip("Total reserve ammo text. Example: 15 in 3 / 15, or infinity symbol.")]
    [SerializeField] private TextMeshProUGUI _reserveAmmoText;

    [Header("Selection")]
    [Tooltip("Outline enabled only when this slot is selected. Set color/thickness in the Outline component.")]
    [SerializeField] private Outline _selectedOutline;

    [Header("Type Sprites")]
    [SerializeField] private Sprite _attackTypeSprite;
    [SerializeField] private Sprite _paintTypeSprite;
    [SerializeField] private Sprite _specialTypeSprite;

    [Header("Text Values")]
    [SerializeField] private string _emptyAmmoText = "-";
    [SerializeField] private string _infiniteReserveText = "∞";
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
        RefreshAmmoTexts(snapshot);
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

    private void RefreshAmmoTexts(WeaponAmmoSlotSnapshot snapshot)
    {
        if (snapshot.isEmpty)
        {
            if (_currentAmmoText != null)
                _currentAmmoText.text = _emptyAmmoText;

            if (_reserveAmmoText != null)
                _reserveAmmoText.text = _emptyAmmoText;

            return;
        }

        if (_currentAmmoText != null)
            _currentAmmoText.text = Mathf.Max(0, snapshot.currentAmmo).ToString();

        if (_reserveAmmoText != null)
        {
            _reserveAmmoText.text = snapshot.infiniteReserve
                ? _infiniteReserveText
                : Mathf.Max(0, snapshot.reserveAmmo).ToString();
        }
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

    private void HandleSellClicked()
    {
        if (!_snapshot.canSell)
            return;

        _owner?.RequestSellSlot(_snapshot);
    }


}