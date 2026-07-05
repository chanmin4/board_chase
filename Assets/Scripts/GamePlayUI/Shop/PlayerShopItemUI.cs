using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One shop item view.
/// Displays a shop offer, handles buy/reroll clicks, SOLD OUT visual state,
/// and shows tooltip only when hovering the assigned info hover graphic.
/// </summary>
[DisallowMultipleComponent]
public class PlayerShopItemUI : MonoBehaviour
{
    [Header("Need Ref - Tooltip Hover")]
    [Tooltip("Assign InfoHoverBG Image here. Raycast Target must be enabled. Hovering this graphic shows tooltip and changes only this graphic alpha.")]
    [SerializeField] private Graphic _tooltipHoverTarget;

    [Tooltip("Normal alpha for Tooltip Hover Target.")]
    [SerializeField, Range(0f, 1f)] private float _hoverNormalAlpha = 0.35f;

    [Tooltip("Hover alpha for Tooltip Hover Target.")]
    [SerializeField, Range(0f, 1f)] private float _hoverAlpha = 0.75f;

    [Header("Need Ref - Content")]
    [Tooltip("CanvasGroup for normal shop content. This is dimmed when SOLD OUT. Example: ShopContent.")]
    [SerializeField] private CanvasGroup _contentGroup;

    [Tooltip("Alpha applied to Content Group when SOLD OUT.")]
    [SerializeField, Range(0f, 1f)] private float _soldOutContentAlpha = 0.35f;

    [Header("Need Ref - Images")]
    [Tooltip("Item icon image.")]
    [SerializeField] private Image _bulletIcon;

    [Tooltip("Optional rarity background image.")]
    [SerializeField] private Image _rarityBackground;

    [Header("Need Ref - Texts")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _rarityText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private TextMeshProUGUI _stockText;
    [SerializeField] private TextMeshProUGUI _bundleText;

    [Header("Need Ref - Buttons")]
    [Tooltip("Buy button. Disabled when offer is invalid or SOLD OUT.")]
    [SerializeField] private Button _buyButton;

    [Tooltip("Per-item reroll button. Disabled when offer is invalid or SOLD OUT.")]
    [SerializeField] private Button _rerollButton;

    [Tooltip("Reroll cost text. Shows number only.")]
    [SerializeField] private TextMeshProUGUI _rerollCostText;

    [Header("Need Ref - Sold Out Overlay")]
    [Tooltip("CanvasGroup on SOLD OUT overlay only. Do not assign ShopContent here.")]
    [SerializeField] private CanvasGroup _soldOutOverlayGroup;

    [Tooltip("Text shown on SOLD OUT overlay.")]
    [SerializeField] private TextMeshProUGUI _soldOutText;

    [SerializeField] private string _soldOutLabel = "SOLD OUT";

    [Header("Need Ref - Tooltip")]
    [SerializeField] private PlayerTooltipUI _tooltipPrefab;
    [SerializeField] private Transform _tooltipRoot;

    [Tooltip("Optional. If empty, Tooltip Hover Target rect is used.")]
    [SerializeField] private RectTransform _tooltipAnchor;

    [Header("Options - Tooltip Text")]
    [SerializeField] private string _priceFormat = "Price: {0}";
    [SerializeField] private string _bundleFormat = "Bundle: {0}";

    [Header("Options - Rarity Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _rareColor = new Color(0.25f, 0.55f, 1f);
    [SerializeField] private Color _uniqueColor = new Color(0.65f, 0.3f, 1f);
    [SerializeField] private Color _legendaryColor = new Color(1f, 0.75f, 0.15f);

    [Header("Don't Touch Ref Auto")]
    [SerializeField] private bool _isHovering;

    private PlayerShopOffer _offer;
    private Action<PlayerShopOffer> _buyRequested;
    private Action<PlayerShopItemUI> _rerollRequested;
    private PlayerTooltipUI _tooltip;

    private EventTrigger _tooltipEventTrigger;
    private EventTrigger.Entry _pointerEnterEntry;
    private EventTrigger.Entry _pointerExitEntry;

    private void Awake()
    {
        SetTooltipHoverAlpha(_hoverNormalAlpha);
        SetSoldOutVisible(false);
    }

    private void OnEnable()
    {
        if (_buyButton != null)
            _buyButton.onClick.AddListener(HandleBuyClicked);

        if (_rerollButton != null)
            _rerollButton.onClick.AddListener(HandleRerollClicked);

        BindTooltipEvents();
        RefreshSoldOut();
    }

    private void OnDisable()
    {
        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(HandleBuyClicked);

        if (_rerollButton != null)
            _rerollButton.onClick.RemoveListener(HandleRerollClicked);

        UnbindTooltipEvents();
        HideTooltip();

        _isHovering = false;
        SetTooltipHoverAlpha(_hoverNormalAlpha);
    }

    public void Bind(
        PlayerShopOffer offer,
        int rerollCost,
        Action<PlayerShopOffer> buyRequested,
        Action<PlayerShopItemUI> rerollRequested)
    {
        _offer = offer;
        _buyRequested = buyRequested;
        _rerollRequested = rerollRequested;

        if (_rerollCostText != null)
            _rerollCostText.text = Mathf.Max(0, rerollCost).ToString();

        Refresh();
    }

    public void Clear()
    {
        _offer = null;
        _buyRequested = null;
        _rerollRequested = null;

        HideTooltip();
        ClearVisuals();
    }

    public void Refresh()
    {
        if (_offer == null || !_offer.IsValid)
        {
            ClearVisuals();
            return;
        }

        if (_bulletIcon != null)
        {
            _bulletIcon.sprite = _offer.Icon;
            _bulletIcon.enabled = _bulletIcon.sprite != null;
        }

        if (_nameText != null)
            _nameText.text = _offer.DisplayName;

        if (_rarityText != null)
            _rarityText.text = _offer.Rarity.ToString();

        if (_priceText != null)
            _priceText.text = _offer.Price.ToString();

        if (_stockText != null)
            _stockText.text = _offer.RemainingStock.ToString();

        if (_bundleText != null)
            _bundleText.text = _offer.IsBullet ? $"x{_offer.BundleAmount}" : string.Empty;

        if (_rarityBackground != null)
            _rarityBackground.color = GetRarityColor(_offer.Rarity);

        RefreshSoldOut();
    }

    private void ClearVisuals()
    {
        if (_bulletIcon != null)
        {
            _bulletIcon.sprite = null;
            _bulletIcon.enabled = false;
        }

        if (_nameText != null)
            _nameText.text = "-";

        if (_rarityText != null)
            _rarityText.text = string.Empty;

        if (_priceText != null)
            _priceText.text = string.Empty;

        if (_stockText != null)
            _stockText.text = string.Empty;

        if (_bundleText != null)
            _bundleText.text = string.Empty;

        SetButtonsInteractable(false);
        SetContentAlpha(1f);
        SetSoldOutVisible(false);
        SetTooltipHoverAlpha(_hoverNormalAlpha);
    }

    private void RefreshSoldOut()
    {
        bool hasValidOffer = _offer != null && _offer.IsValid;
        bool soldOut = hasValidOffer && _offer.IsSoldOut;

        SetButtonsInteractable(hasValidOffer && !soldOut);
        SetContentAlpha(soldOut ? _soldOutContentAlpha : 1f);

        if (_soldOutText != null)
            _soldOutText.text = _soldOutLabel;

        SetSoldOutVisible(soldOut);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_buyButton != null)
            _buyButton.interactable = interactable;

        if (_rerollButton != null)
            _rerollButton.interactable = interactable;
    }

    private void SetContentAlpha(float alpha)
    {
        if (_contentGroup == null)
            return;

        _contentGroup.alpha = alpha;
        _contentGroup.interactable = true;
        _contentGroup.blocksRaycasts = true;
    }

    private void SetSoldOutVisible(bool visible)
    {
        if (_soldOutOverlayGroup == null)
            return;

        _soldOutOverlayGroup.alpha = visible ? 1f : 0f;
        _soldOutOverlayGroup.interactable = false;
        _soldOutOverlayGroup.blocksRaycasts = false;
    }

    private void BindTooltipEvents()
    {
        if (_tooltipHoverTarget == null)
            return;

        _tooltipHoverTarget.raycastTarget = true;

        _tooltipEventTrigger = _tooltipHoverTarget.GetComponent<EventTrigger>();

        if (_tooltipEventTrigger == null)
            _tooltipEventTrigger = _tooltipHoverTarget.gameObject.AddComponent<EventTrigger>();

        _pointerEnterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        _pointerEnterEntry.callback.AddListener(_ => HandleTooltipPointerEnter());

        _pointerExitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        _pointerExitEntry.callback.AddListener(_ => HandleTooltipPointerExit());

        _tooltipEventTrigger.triggers.Add(_pointerEnterEntry);
        _tooltipEventTrigger.triggers.Add(_pointerExitEntry);
    }

    private void UnbindTooltipEvents()
    {
        if (_tooltipEventTrigger == null)
            return;

        if (_pointerEnterEntry != null)
            _tooltipEventTrigger.triggers.Remove(_pointerEnterEntry);

        if (_pointerExitEntry != null)
            _tooltipEventTrigger.triggers.Remove(_pointerExitEntry);

        _pointerEnterEntry = null;
        _pointerExitEntry = null;
        _tooltipEventTrigger = null;
    }

    private void HandleTooltipPointerEnter()
    {
        _isHovering = true;
        SetTooltipHoverAlpha(_hoverAlpha);
        ShowTooltip();
    }

    private void HandleTooltipPointerExit()
    {
        _isHovering = false;
        SetTooltipHoverAlpha(_hoverNormalAlpha);
        HideTooltip();
    }

    private void SetTooltipHoverAlpha(float alpha)
    {
        if (_tooltipHoverTarget == null)
            return;

        Color color = _tooltipHoverTarget.color;
        color.a = alpha;
        _tooltipHoverTarget.color = color;
    }

    private void ShowTooltip()
    {
        if (_offer == null || !_offer.IsValid)
            return;

        PlayerTooltipUI tooltip = GetOrCreateTooltip();

        if (tooltip == null)
            return;

        string title = _offer.DisplayName;
        string price = string.Format(_priceFormat, _offer.Price);
        string bundle = _offer.IsBullet
            ? string.Format(_bundleFormat, _offer.BundleAmount)
            : string.Empty;

        tooltip.ShowText(
            title,
            _offer.Description,
            price,
            bundle,
            ResolveTooltipPosition());
    }

    private void HideTooltip()
    {
        if (_tooltip != null)
            _tooltip.Hide();
    }

    private PlayerTooltipUI GetOrCreateTooltip()
    {
        if (_tooltip != null)
            return _tooltip;

        if (_tooltipPrefab == null)
            return null;

        Transform root = _tooltipRoot != null ? _tooltipRoot : transform;

        _tooltip = Instantiate(_tooltipPrefab, root);
        _tooltip.Hide();

        return _tooltip;
    }

    private Vector2 ResolveTooltipPosition()
    {
        RectTransform anchor = _tooltipAnchor != null
            ? _tooltipAnchor
            : _tooltipHoverTarget != null
                ? _tooltipHoverTarget.transform as RectTransform
                : transform as RectTransform;

        if (anchor == null)
            return Input.mousePosition;

        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);

        return corners[2];
    }

    private void HandleBuyClicked()
    {
        if (_offer == null || !_offer.IsValid || _offer.IsSoldOut)
            return;

        _buyRequested?.Invoke(_offer);
    }

    private void HandleRerollClicked()
    {
        if (_offer == null || !_offer.IsValid || _offer.IsSoldOut)
            return;

        _rerollRequested?.Invoke(this);
    }

    private Color GetRarityColor(PlayerShopItemRarity rarity)
    {
        return rarity switch
        {
            PlayerShopItemRarity.Rare => _rareColor,
            PlayerShopItemRarity.Unique => _uniqueColor,
            PlayerShopItemRarity.Legendary => _legendaryColor,
            _ => _normalColor
        };
    }
}
