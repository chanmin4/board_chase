/*
 * Role hierarchy: Presentation > UI > Gameplay > Shop
 * Primary responsibility: Presents PlayerShopItemUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
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

    [Tooltip("Shop offers open their tooltip to the left so the box grows away from the right-side Shop panel.")]
    [SerializeField] private TooltipPlacementSide _tooltipSide = TooltipPlacementSide.Left;

    [Tooltip("Pixel gap between the hovered offer and the tooltip box.")]
    [SerializeField, Min(0f)] private float _tooltipGap;

    [Header("Options - Tooltip Text")]
    [SerializeField] private string _priceFormat = "Price: {0}";
    [SerializeField] private string _bundleFormat = "Bundle: {0}";

    [Header("Options - Rarity Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _rareColor = new Color(0.25f, 0.55f, 1f);
    [SerializeField] private Color _uniqueColor = new Color(0.65f, 0.3f, 1f);
    [SerializeField] private Color _legendaryColor = new Color(1f, 0.75f, 0.15f);

    private PlayerShopOffer _offer;
    private Action<PlayerShopOffer> _buyRequested;
    private Action<PlayerShopItemUI> _rerollRequested;
    private PlayerTooltipUI _tooltip;
    private LocalizedString _boundNameString;
    private string _boundNameFallback;
    private LocalizedString _boundDescriptionString;
    private string _localizedDescription;

    private EventTrigger _tooltipEventTrigger;
    private EventTrigger.Entry _pointerEnterEntry;
    private EventTrigger.Entry _pointerExitEntry;
    private EventTrigger.Entry _pointerClickEntry;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        SetTooltipHoverAlpha(_hoverNormalAlpha);
        SetSoldOutVisible(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        BindItemDetailClickEvent();

        if (_buyButton != null)
            _buyButton.onClick.AddListener(HandleBuyClicked);

        if (_rerollButton != null)
            _rerollButton.onClick.AddListener(HandleRerollClicked);

        // Item details are shown by the click-selected detail panel.
        // Hover tooltip creation is intentionally disabled.
        RefreshSoldOut();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(HandleBuyClicked);

        if (_rerollButton != null)
            _rerollButton.onClick.RemoveListener(HandleRerollClicked);

        UnbindTooltipEvents();
        UnbindLocalizedText();
        HideTooltip();

        SetTooltipHoverAlpha(_hoverNormalAlpha);
    }

    // Purpose: Binds bind to the active runtime or UI target.
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

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        _offer = null;
        _buyRequested = null;
        _rerollRequested = null;

        HideTooltip();
        ClearVisuals();
    }

    // Purpose: Refreshes the displayed or cached refresh.
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
            ModuleRuneView.Bind(_bulletIcon, _offer.Item);
        }

        if (_nameText != null)
            BindLocalizedName(_offer);

        BindLocalizedDescription(_offer);

        if (_rarityText != null)
            _rarityText.text = _offer.Rarity.ToString();

        if (_priceText != null)
            _priceText.text = _offer.UsesPhysicalPayment
                ? UIRuntimeLocalization.Get("UI_Inventory", "Inventory_ShopTrade", "Trade")
                : UIRuntimeLocalization.Get("UI_Inventory", "Inventory_ShopPaymentError", "Payment Error");

        if (_stockText != null)
            _stockText.text = _offer.StockDisplay;

        if (_bundleText != null)
            _bundleText.text = _offer.IsBullet ? $"x{_offer.BundleAmount}" : string.Empty;

        if (_rarityBackground != null)
            _rarityBackground.color = GetRarityColor(_offer.Rarity);

        RefreshSoldOut();
    }

    // Purpose: Clears the current visuals state.
    private void ClearVisuals()
    {
        if (_bulletIcon != null)
        {
            _bulletIcon.sprite = null;
            _bulletIcon.enabled = false;
            ModuleRuneView.Bind(_bulletIcon, null);
        }

        if (_nameText != null)
            _nameText.text = "-";

        UnbindLocalizedText();

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

    // Purpose: Refreshes the displayed or cached sold out.
    private void RefreshSoldOut()
    {
        bool hasValidOffer = _offer != null && _offer.IsValid;
        bool soldOut = hasValidOffer && _offer.IsSoldOut;

        SetButtonsInteractable(hasValidOffer && !soldOut, _offer != null && _offer.CanReroll);
        SetContentAlpha(soldOut ? _soldOutContentAlpha : 1f);

        if (_soldOutText != null)
            _soldOutText.text = _soldOutLabel;

        SetSoldOutVisible(soldOut);
    }

    // Purpose: Updates the buttons interactable state.
    private void SetButtonsInteractable(bool interactable, bool rerollInteractable = false)
    {
        if (_buyButton != null)
            _buyButton.interactable = interactable;

        if (_rerollButton != null)
            _rerollButton.interactable = interactable && rerollInteractable;
    }

    // Purpose: Updates the content alpha state.
    private void SetContentAlpha(float alpha)
    {
        if (_contentGroup == null)
            return;

        _contentGroup.alpha = alpha;
        _contentGroup.interactable = true;
        _contentGroup.blocksRaycasts = true;
    }

    // Purpose: Updates the sold out visible state.
    private void SetSoldOutVisible(bool visible)
    {
        if (_soldOutOverlayGroup == null)
            return;

        _soldOutOverlayGroup.alpha = visible ? 1f : 0f;
        _soldOutOverlayGroup.interactable = false;
        _soldOutOverlayGroup.blocksRaycasts = false;
    }

    // Purpose: Binds item detail click event to the active runtime or UI target.
    private void BindItemDetailClickEvent()
    {
        if (_tooltipHoverTarget == null)
            return;

        _tooltipHoverTarget.raycastTarget = true;

        _tooltipEventTrigger = _tooltipHoverTarget.GetComponent<EventTrigger>();

        if (_tooltipEventTrigger == null)
            _tooltipEventTrigger = _tooltipHoverTarget.gameObject.AddComponent<EventTrigger>();

        _pointerClickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        _pointerClickEntry.callback.AddListener(_ => PublishItemDetailSelection());

        _tooltipEventTrigger.triggers.Add(_pointerClickEntry);
    }

    // Purpose: Removes the tooltip events binding.
    private void UnbindTooltipEvents()
    {
        if (_tooltipEventTrigger == null)
            return;

        if (_pointerEnterEntry != null)
            _tooltipEventTrigger.triggers.Remove(_pointerEnterEntry);

        if (_pointerExitEntry != null)
            _tooltipEventTrigger.triggers.Remove(_pointerExitEntry);

        if (_pointerClickEntry != null)
            _tooltipEventTrigger.triggers.Remove(_pointerClickEntry);

        _pointerEnterEntry = null;
        _pointerExitEntry = null;
        _pointerClickEntry = null;
        _tooltipEventTrigger = null;
    }

    // Purpose: Handles the incoming tooltip pointer enter request or event.
    private void HandleTooltipPointerEnter()
    {
        SetTooltipHoverAlpha(_hoverAlpha);
        PublishItemDetailSelection();
        ShowTooltip();
    }

    // Purpose: Handles the incoming tooltip pointer exit request or event.
    private void HandleTooltipPointerExit()
    {
        SetTooltipHoverAlpha(_hoverNormalAlpha);
        HideTooltip();
    }

    // Purpose: Updates the tooltip hover alpha state.
    private void SetTooltipHoverAlpha(float alpha)
    {
        if (_tooltipHoverTarget == null)
            return;

        Color color = _tooltipHoverTarget.color;
        color.a = alpha;
        _tooltipHoverTarget.color = color;
    }

    // Purpose: Shows the tooltip UI or visual state.
    private void ShowTooltip()
    {
        if (_offer == null || !_offer.IsValid)
            return;

        PlayerTooltipUI tooltip = GetOrCreateTooltip();

        if (tooltip == null)
            return;

        string title = _nameText != null && !string.IsNullOrWhiteSpace(_nameText.text)
            ? _nameText.text
            : _offer.DisplayName;
        string description = !string.IsNullOrWhiteSpace(_localizedDescription)
            ? _localizedDescription
            : _offer.Description;
        string price = _offer.UsesPhysicalPayment
            ? string.Format(_priceFormat, _offer.BuildTradeSummary())
            : "INVALID ITEM PAYMENT";
        string bundle = _offer.IsBullet
            ? string.Format(_bundleFormat, _offer.BundleAmount)
            : string.Empty;

        tooltip.ShowText(
            title,
            description,
            price,
            bundle,
            ResolveTooltipAnchor(),
            _tooltipSide,
            _tooltipGap);
    }

    // Purpose: Hides the tooltip UI or visual state.
    private void HideTooltip()
    {
        if (_tooltip == null)
            return;

        PlayerTooltipUI tooltip = _tooltip;
        _tooltip = null;
        Destroy(tooltip.gameObject);
    }

    // Purpose: Returns the current or create tooltip.
    private PlayerTooltipUI GetOrCreateTooltip()
    {
        if (_tooltipPrefab == null)
            return null;

        HideTooltip();
        Transform root = PlayerTooltipUI.GetOrCreatePanelTooltipRoot(this, _tooltipRoot);
        if (root == null)
            return null;

        _tooltip = Instantiate(_tooltipPrefab, root);
        _tooltip.transform.SetAsLastSibling();
        return _tooltip;
    }

    // Purpose: Resolves the tooltip anchor needed by this component.
    private RectTransform ResolveTooltipAnchor()
    {
        RectTransform anchor = _tooltipAnchor != null
            ? _tooltipAnchor
            : _tooltipHoverTarget != null
                ? _tooltipHoverTarget.transform as RectTransform
                : transform as RectTransform;

        return anchor;
    }

    // Purpose: Handles the incoming buy clicked request or event.
    private void HandleBuyClicked()
    {
        if (_offer == null || !_offer.IsValid || _offer.IsSoldOut)
            return;

        PublishItemDetailSelection();
        _buyRequested?.Invoke(_offer);
    }

    // Purpose: Publishes the item detail selection notification.
    private void PublishItemDetailSelection()
    {
        if (_offer == null || !_offer.IsValid)
            return;

        float weaponMaximum = _offer.Item is RangedWeaponSO rangedWeapon
            ? rangedWeapon.MaxDurability
            : -1f;
        float armorMaximum = _offer.Item is ArmorItemSO armor
            ? armor.MaxDurability
            : -1f;

        ItemDetailSelectionBus.Publish(new ItemDetailSelection(
            _offer.Item,
            _offer.IsBullet ? _offer.BundleAmount : 1,
            armorMaximum,
            armorMaximum,
            weaponMaximum,
            weaponMaximum));
    }

    // Purpose: Handles the incoming reroll clicked request or event.
    private void HandleRerollClicked()
    {
        if (_offer == null || !_offer.IsValid || _offer.IsSoldOut)
            return;

        _rerollRequested?.Invoke(this);
    }

    // Purpose: Returns the current rarity color.
    private Color GetRarityColor(PlayerShopItemRarity rarity)
    {
        if(ItemRarityStyleSO.Active!=null)return ItemRarityStyleSO.Active.RarityColor(rarity);
        return rarity switch
        {
            PlayerShopItemRarity.Rare => _rareColor,
            PlayerShopItemRarity.Unique => _uniqueColor,
            PlayerShopItemRarity.Legendary => _legendaryColor,
            _ => _normalColor
        };
    }

    // Purpose: Binds localized name to the active runtime or UI target.
    private void BindLocalizedName(PlayerShopOffer offer)
    {
        UnbindLocalizedName();

        if (_nameText == null)
            return;

        _boundNameFallback = offer != null ? offer.DisplayName : "-";
        _nameText.text = _boundNameFallback;

        if (offer == null ||
            !InventoryItemDisplayUtility.TryGetItemNameReference(offer.Item, out _boundNameString))
        {
            return;
        }

        _boundNameString.StringChanged += HandleNameStringChanged;
        _boundNameString.RefreshString();
    }

    // Purpose: Binds localized description to the active runtime or UI target.
    private void BindLocalizedDescription(PlayerShopOffer offer)
    {
        UnbindLocalizedDescription();
        _localizedDescription = offer != null ? offer.Description : string.Empty;

        if (offer == null ||
            offer.DescriptionReference == null ||
            offer.DescriptionReference.IsEmpty)
        {
            return;
        }

        _boundDescriptionString = offer.DescriptionReference;
        _boundDescriptionString.StringChanged += HandleDescriptionStringChanged;
        _boundDescriptionString.RefreshString();
    }

    // Purpose: Removes the localized text binding.
    private void UnbindLocalizedText()
    {
        UnbindLocalizedName();
        UnbindLocalizedDescription();
    }

    // Purpose: Removes the localized name binding.
    private void UnbindLocalizedName()
    {
        if (_boundNameString != null)
            _boundNameString.StringChanged -= HandleNameStringChanged;

        _boundNameString = null;
    }

    // Purpose: Removes the localized description binding.
    private void UnbindLocalizedDescription()
    {
        if (_boundDescriptionString != null)
            _boundDescriptionString.StringChanged -= HandleDescriptionStringChanged;

        _boundDescriptionString = null;
        _localizedDescription = string.Empty;
    }

    // Purpose: Handles the incoming name string changed request or event.
    private void HandleNameStringChanged(string value)
    {
        if (_nameText == null)
            return;

        _nameText.text = string.IsNullOrWhiteSpace(value)
            ? _boundNameFallback ?? string.Empty
            : value;
    }

    // Purpose: Handles the incoming description string changed request or event.
    private void HandleDescriptionStringChanged(string value)
    {
        _localizedDescription = value ?? string.Empty;
    }
}
