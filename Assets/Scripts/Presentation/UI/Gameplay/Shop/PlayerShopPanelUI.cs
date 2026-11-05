/*
 * Role hierarchy: Presentation > UI > Gameplay > Shop
 * Primary responsibility: Presents PlayerShopPanelUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerShopPanelUI : PanelUI
{
    private enum OfferSection
    {
        Bullet,
        Item
    }

    [Header("Home Shop Config")]
    [SerializeField] private ShopConfigSO _defaultConfig;

    [Header("Runtime Ready")]
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _inventoryRuntimeReadyChannel;

    [Header("Shop Bullet Slots")]
    [SerializeField] private PlayerShopItemUI[] _bulletSlots;

    [Header("Shop Item Slots")]
    [FormerlySerializedAs("_itemSlots")]
    [SerializeField] private PlayerShopItemUI[] _itemSlots;

    [Header("Player Item Selection")]
    [FormerlySerializedAs("_sellSelectionGroup")]
    [SerializeField] private CanvasGroup _playerItemSelectionGroup;

    [FormerlySerializedAs("_sellSelectionIcon")]
    [SerializeField] private Image _playerItemSelectionIcon;
    [SerializeField] private TextMeshProUGUI _playerItemNameText;

    [SerializeField] private TextMeshProUGUI _playerItemAmountText;

    [SerializeField] private Button _sellButton;

    [FormerlySerializedAs("_sellSelectionPriceText")]
    [SerializeField] private TextMeshProUGUI _sellPriceText;

    [SerializeField] private CanvasGroup _repairActionGroup;
    [SerializeField] private Button _repairButton;
    [SerializeField] private TextMeshProUGUI _repairPriceText;

    [FormerlySerializedAs("_sellSelectionPriceFormat")]
    [SerializeField] private string _priceFormat = "{0}";

    [FormerlySerializedAs("_emptySellSelectionPriceText")]
    [SerializeField] private string _emptyPriceText = "-";

    [Header("Events")]
    [SerializeField] private SectorShopOpenRequestEventChannelSO _sectorShopOpenRequestChannel;
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;
    [Tooltip("Publishes confirmed shop transaction feedback to the global UI audio router.")]
    [SerializeField] private UIAudioEventChannelSO _uiAudioEventChannel;

    private readonly List<PlayerShopOffer> _bulletOffers = new();
    private readonly List<PlayerShopOffer> _itemOffers = new();

    private ShopConfigSO _activeShopConfig;
    private SectorShop _activeStation;
    private bool _raidTrade;
    private PlayerInventoryRuntime _inventoryRuntime;
    private PlayerInventoryUI _inventoryPanel;
    private PlayerInventoryItemSelection _selectedPlayerItem = PlayerInventoryItemSelection.None;

    private float _activeSellPriceRate = 0.5f;
    private float _activeArmorRepairPriceRate = 0.5f;
    private bool _activeAllowReroll = true;
    private bool _initialized;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_sectorShopOpenRequestChannel != null)
            _sectorShopOpenRequestChannel.OnEventRaised += HandleSectorShopOpenRequested;

        if (_inventoryRuntimeReadyChannel != null)
        {
            _inventoryRuntimeReadyChannel.OnEventRaised += HandleInventoryRuntimeReady;

            if (_inventoryRuntimeReadyChannel.HasCurrent)
                HandleInventoryRuntimeReady(_inventoryRuntimeReadyChannel.Current);
        }

        if (_sellButton != null)
            _sellButton.onClick.AddListener(HandleSellClicked);

        if (_repairButton != null)
            _repairButton.onClick.AddListener(HandleRepairClicked);

        BindPlaceholder();
        RefreshPlayerItemSelectionView();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        ShopPurchaseTradePanel.CloseIfOwned(this);

        if (_sectorShopOpenRequestChannel != null)
            _sectorShopOpenRequestChannel.OnEventRaised -= HandleSectorShopOpenRequested;

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.OnEventRaised -= HandleInventoryRuntimeReady;

        if (_sellButton != null)
            _sellButton.onClick.RemoveListener(HandleSellClicked);

        if (_repairButton != null)
            _repairButton.onClick.RemoveListener(HandleRepairClicked);

        SetSellSelectionSource(null);
    }

    // Purpose: Binds placeholder to the active runtime or UI target.
    public void BindPlaceholder()
    {
        EnsureInitialized();
        RefreshAll();
    }

    /// <summary>
    /// Reflows the authored ShopPanel into the right-hand Shop/Loot workspace.
    /// This keeps purchase offers large while preserving the existing sell and
    /// repair transaction controls in a dedicated bottom action strip.
    /// </summary>
    public void ApplyContextPresentation()
    {
        RectTransform background = FindChildRect("OpaqueBackground");
        RectTransform header = FindChildRect("Header");
        RectTransform content = FindChildRect("ShopContent");
        RectTransform bulletColumn = FindChildRect("BulletColumn");
        RectTransform itemColumn = FindChildRect("ItemColuimn");
        RectTransform actionStrip = FindChildRect("PlayerItemSelection");
        RectTransform sellSection = FindChildRect("SellSection");
        RectTransform repairSection = FindChildRect("RepairSection");

        Stretch(background, new Vector2(12f, 12f), new Vector2(-12f, -12f));
        AnchorTop(header, 16f, 52f, 20f);
        Stretch(content, new Vector2(20f, 138f), new Vector2(-20f, -82f));
        AnchorBottom(actionStrip, 18f, 104f, 20f);

        if (content != null)
        {
            HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 12f;
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }
        }

        ConfigureOfferColumn(bulletColumn);
        ConfigureOfferColumn(itemColumn);
        if (EquipmentRepairPolicy.Enabled) AnchorFillHalf(sellSection, true);
        else Stretch(sellSection, Vector2.zero, Vector2.zero);
        AnchorFillHalf(repairSection, false);
        ApplyRepairAvailability();

        Canvas.ForceUpdateCanvases();
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    // Purpose: Finds one authored RectTransform by name without adding inspector-only layout references.
    private RectTransform FindChildRect(string childName)
    {
        foreach (RectTransform rect in GetComponentsInChildren<RectTransform>(true))
        {
            if (rect != null && rect.name == childName)
                return rect;
        }

        return null;
    }

    // Purpose: Configures one Shop offer category to fill half of the offer surface.
    private static void ConfigureOfferColumn(RectTransform column)
    {
        if (column == null)
            return;

        LayoutElement layoutElement = column.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = column.gameObject.AddComponent<LayoutElement>();

        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 1f;

        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }
    }

    // Purpose: Applies a filled RectTransform layout with symmetric offsets.
    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    // Purpose: Anchors one rect as a full-width top strip.
    private static void AnchorTop(RectTransform rect, float topInset, float height, float sideInset)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topInset);
        rect.sizeDelta = new Vector2(-sideInset * 2f, height);
    }

    // Purpose: Anchors one rect as a full-width bottom strip.
    private static void AnchorBottom(RectTransform rect, float bottomInset, float height, float sideInset)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottomInset);
        rect.sizeDelta = new Vector2(-sideInset * 2f, height);
    }

    // Purpose: Splits the action strip into sell and repair halves.
    private static void AnchorFillHalf(RectTransform rect, bool leftHalf)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(leftHalf ? 0f : 0.5f, 0f);
        rect.anchorMax = new Vector2(leftHalf ? 0.5f : 1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(leftHalf ? 0f : 6f, 0f);
        rect.offsetMax = new Vector2(leftHalf ? -6f : 0f, 0f);
    }

    // Purpose: Opens the with offers UI or workflow.
    public void OpenWithOffers(
        IReadOnlyList<PlayerShopOffer> bulletOffers,
        IReadOnlyList<PlayerShopOffer> itemOffers,
        ShopConfigSO config,
        bool allowReroll, SectorShop station = null)
    {
        _activeStation = station;
        _raidTrade = station != null && station.IsRaidMerchant;
        _activeShopConfig = config;
        _activeAllowReroll = allowReroll;
        _activeSellPriceRate = config != null ? config.SellPriceRate : 0.5f;
        _activeArmorRepairPriceRate = config != null ? config.ArmorRepairPriceRate : 0.5f;
        _initialized = true;

        ReplaceOffers(_bulletOffers, bulletOffers);
        ReplaceOffers(_itemOffers, itemOffers);

        ShowPanel();
        RefreshAll();
    }

    // Purpose: Opens the with offers UI or workflow.
    public void OpenWithOffers(
        IReadOnlyList<PlayerShopOffer> offers,
        ShopConfigSO config,
        bool allowReroll)
    {
        List<PlayerShopOffer> bulletOffers = new();
        List<PlayerShopOffer> itemOffers = new();

        if (offers != null)
        {
            for (int i = 0; i < offers.Count; i++)
            {
                PlayerShopOffer offer = offers[i];

                if (offer == null)
                    continue;

                if (offer.IsBullet)
                    bulletOffers.Add(offer);
                else
                    itemOffers.Add(offer);
            }
        }

        OpenWithOffers(bulletOffers, itemOffers, config, allowReroll);
    }

    // Purpose: Closes the active panel UI or workflow.
    public void ClosePanel()
    {
        HidePanel();
    }

    // Purpose: Responds to the overlay shown callback.
    public override void OnOverlayShown()
    {
    }

    // Purpose: Responds to the panel hidden callback.
    protected override void OnPanelHidden()
    {
        ShopPurchaseTradePanel.CloseIfOwned(this);
        SetSellSelectionSource(null);
    }

    // Purpose: Updates the sell selection source state.
    public void SetSellSelectionSource(PlayerInventoryUI inventoryPanel)
    {
        SetPlayerItemSelectionSource(inventoryPanel);
    }

    // Purpose: Updates the player item selection source state.
    public void SetPlayerItemSelectionSource(PlayerInventoryUI inventoryPanel)
    {
        if (_inventoryPanel == inventoryPanel)
        {
            RefreshPlayerItemSelectionView();
            return;
        }

        if (_inventoryPanel != null)
            _inventoryPanel.OnItemSelectionChanged -= HandlePlayerItemSelectionChanged;

        _inventoryPanel = inventoryPanel;
        _selectedPlayerItem = PlayerInventoryItemSelection.None;

        if (_inventoryPanel != null)
        {
            _inventoryPanel.OnItemSelectionChanged += HandlePlayerItemSelectionChanged;

            if (_inventoryPanel.TryGetSelectedItemSelection(out PlayerInventoryItemSelection selection))
                _selectedPlayerItem = selection;
        }

        RefreshPlayerItemSelectionView();
    }

    // Purpose: Attempts to sell inventory slot and reports whether the request succeeded.
    public bool TrySellInventorySlot(int slotIndex, int amount = 1)
    {
        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Inventory is not connected.");
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        ItemSO item = ResolveInventoryItem(slotIndex);
        if (item is PassiveItemSO) { RaiseMessage("패시브는 판매할 수 없습니다. 분해하거나 리롤에 사용하세요."); return false; }
        int unitPrice = ResolveInventorySellUnitPrice(item);

        if (!CanMerchantPaySale(unitPrice, amount)) return false;

        if (!_inventoryRuntime.TryRemoveInventoryItem(
                slotIndex,
                amount,
                out PlayerInventoryRemovedStack removed,
                out string message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        return TryCompletePhysicalSale(removed, unitPrice, out _);
    }

    // Purpose: Attempts to sell equipment slot and reports whether the request succeeded.
    public bool TrySellEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        return TrySellEquipmentSlot(slotKind, 0);
    }

    // Purpose: Attempts to sell equipment slot and reports whether the request succeeded.
    public bool TrySellEquipmentSlot(
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex)
    {
        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Inventory is not connected.");
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        ItemSO item = slotKind switch
        {
            PlayerInventoryEquipmentSlotKind.Weapon => _inventoryRuntime.GetEquippedWeapon(equipmentSlotIndex),
            PlayerInventoryEquipmentSlotKind.Armor => _inventoryRuntime.EquippedArmor,
            _ => null
        };

        if (item == null)
        {
            RaiseMessage("Select the equipped item from the inventory panel before selling it.");
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        int unitPrice = ResolveInventorySellUnitPrice(item);

        if (!CanMerchantPaySale(unitPrice, slotKind == PlayerInventoryEquipmentSlotKind.Weapon ?
            _inventoryRuntime.GetEquippedWeaponAmount(equipmentSlotIndex) : 1)) return false;

        if (!_inventoryRuntime.TryRemoveEquipped(
                slotKind,
                equipmentSlotIndex,
                out PlayerInventoryRemovedStack removed,
                out string message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        return TryCompletePhysicalSale(removed, unitPrice, out _);
    }

    // Purpose: Performs the reset shop for new run workflow for this component.
    public void ResetShopForNewRun()
    {
        _initialized = false;
        EnsureInitialized();
        RefreshAll();
    }

    // Purpose: Ensures initialized is ready before the workflow continues.
    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _activeShopConfig = _defaultConfig;
        _activeAllowReroll = _defaultConfig != null && _defaultConfig.AllowReroll;
        _activeSellPriceRate = _defaultConfig != null ? _defaultConfig.SellPriceRate : 0.5f;
        _activeArmorRepairPriceRate = _defaultConfig != null ? _defaultConfig.ArmorRepairPriceRate : 0.5f;

        RollAllOffers();

        _initialized = true;
    }

    // Purpose: Handles the incoming sector shop open requested request or event.
    private void HandleSectorShopOpenRequested(SectorShopOpenRequest request)
    {
        OpenWithOffers(
            request.bulletOffers,
            request.itemOffers,
            request.config,
            request.allowReroll, request.station);
    }

    // Purpose: Handles the incoming inventory runtime ready request or event.
    private void HandleInventoryRuntimeReady(PlayerInventoryRuntime runtime)
    {
        _inventoryRuntime = runtime;
    }

    // Purpose: Rolls the all offers result from its configured probabilities.
    private void RollAllOffers()
    {
        _bulletOffers.Clear();
        _itemOffers.Clear();

        if (_defaultConfig == null)
            return;

        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        ShopUnlockRuntime unlockRuntime = ShopUnlockRuntime.EnsureRuntime();
        unlockRuntime.Configure(_defaultConfig);

        _bulletOffers.AddRange(_defaultConfig.CreateBulletOffers(
            seed,
            product => unlockRuntime.IsProductAvailable(_defaultConfig, product)));
        _itemOffers.AddRange(_defaultConfig.CreateItemOffers(
            seed + 1009,
            product => unlockRuntime.IsProductAvailable(_defaultConfig, product)));
    }

    // Purpose: Refreshes the displayed or cached all.
    private void RefreshAll()
    {
        RefreshOfferViews();
        RefreshPlayerItemSelectionView();
    }

    // Purpose: Refreshes the displayed or cached offer views.
    private void RefreshOfferViews()
    {
        int rerollCost = ResolveRerollCost();

        RefreshOfferViews(
            _bulletSlots,
            _bulletOffers,
            rerollCost);

        RefreshOfferViews(
            _itemSlots,
            _itemOffers,
            rerollCost);
    }

    // Purpose: Refreshes the displayed or cached offer views.
    private void RefreshOfferViews(
        PlayerShopItemUI[] slots,
        IReadOnlyList<PlayerShopOffer> offers,
        int rerollCost)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            PlayerShopItemUI view = slots[i];

            if (view == null)
                continue;

            if (offers != null && i < offers.Count)
            {
                view.Bind(
                    offers[i],
                    rerollCost,
                    HandleBuyRequested,
                    HandleRerollRequested);
            }
            else
            {
                view.Clear();
            }
        }
    }

    // Purpose: Handles the incoming buy requested request or event.
    private void HandleBuyRequested(PlayerShopOffer offer)
    {
        if (offer == null || !offer.IsValid || offer.IsSoldOut)
            return;

        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Inventory is not connected.");
            return;
        }

        if (!offer.UsesPhysicalPayment || !offer.HasValidPhysicalPayment)
        {
            RaiseMessage("This shop offer has no valid item-payment setup.");
            return;
        }

        if (!ShopPurchaseTradePanel.TryOpen(this, offer, _inventoryRuntime, _inventoryPanel))
            RaiseMessage("Could not open the payment window.");
    }

    /// <summary>
    /// Completes a confirmed physical-currency/barter purchase. Payment is
    /// fully validated first, then removed atomically. A failed product grant
    /// restores the payment before the method returns failure.
    /// </summary>
    public bool TryCompletePhysicalPurchase(PlayerShopOffer offer, out string message)
    {
        ResolveInventoryRuntime();
        if (_raidTrade && (_activeStation == null || !_activeStation.CanInteract))
        { message = "상인이 있는 섹터에서만 거래할 수 있습니다."; return false; }

        if (offer == null || !offer.IsValid || offer.IsSoldOut)
        {
            message = "This shop offer is no longer available.";
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (!offer.UsesPhysicalPayment || !offer.HasValidPhysicalPayment)
        {
            message = "This shop offer has no valid physical payment setup.";
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (_inventoryRuntime == null)
        {
            message = "Inventory is not connected.";
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (!_inventoryRuntime.TryValidateInventoryItemRequirements(offer.TradeRequirements, out message))
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);

        // Reserve finite stock before consuming a payment. Every later failure
        // below restores this exact reservation, preventing a completed trade
        // from producing a free item or consuming the last stock twice.
        if (!offer.TryConsumeStock())
        {
            message = "This shop offer is no longer available.";
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (!_inventoryRuntime.TryConsumeInventoryItemRequirements(offer.TradeRequirements, out var paymentReceipt, out message))
        {
            offer.RestoreConsumedStock();
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (!_inventoryRuntime.TryPickup(
                offer.Item,
                1,
                offer.BundleAmount,
                _activeSellPriceRate,
                out string pickupMessage))
        {
            bool restored = _inventoryRuntime.TryRestoreInventoryItemRequirements(
                paymentReceipt,
                out string restoreMessage);
            offer.RestoreConsumedStock();

            if (!restored)
            {
                Debug.LogError(
                    $"[Shop] Physical payment rollback failed. pickupError={pickupMessage}, rollbackError={restoreMessage}",
                    this);
            }

            message = restored
                ? pickupMessage
                : $"{pickupMessage} Payment restore also failed: {restoreMessage}";
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        message = pickupMessage;
        _activeStation?.ReceivePhysicalPayment(offer.TradeRequirements);
        RefreshAll();
        PlayUiAudio(UIAudioCueType.Buy);
        return true;
    }

    // Purpose: Handles the incoming reroll requested request or event.
    private void HandleRerollRequested(PlayerShopItemUI itemUI)
    {
        if (itemUI == null)
            return;

        if (!TryFindOfferSlot(itemUI, out OfferSection section, out int index))
            return;

        HandleConfigReroll(section, index);
    }

    // Purpose: Handles the incoming config reroll request or event.
    private void HandleConfigReroll(OfferSection section, int index)
    {
        if (!_activeAllowReroll || _activeShopConfig == null)
        {
            RaiseMessage("Reroll is not available.");
            return;
        }

        int cost = ResolveRerollCost();

        if (!TryConsumePhysicalMoney(cost, out List<ShopTradeRequirement> payment, out string paymentMessage))
        {
            RaiseMessage(paymentMessage);
            return;
        }

        IReadOnlyList<ItemSO> excluded = ShouldAllowDuplicate(section)
            ? null
            : BuildExcludedItems(section, index);

        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        ShopUnlockRuntime unlockRuntime = ShopUnlockRuntime.EnsureRuntime();
        unlockRuntime.Configure(_activeShopConfig);
        bool picked = section == OfferSection.Bullet
            ? _activeShopConfig.TryCreateBulletOffer(
                seed,
                excluded,
                product => unlockRuntime.IsProductAvailable(_activeShopConfig, product),
                out PlayerShopOffer offer)
            : _activeShopConfig.TryCreateItemOffer(
                seed,
                excluded,
                product => unlockRuntime.IsProductAvailable(_activeShopConfig, product),
                out offer);

        if (!picked)
        {
            RefundPhysicalPayment(payment);
            RaiseMessage("No shop item available.");
            return;
        }

        List<PlayerShopOffer> target = ResolveOfferList(section);

        while (target.Count <= index)
            target.Add(null);

        target[index] = offer;
        RefreshAll();
    }

    // Purpose: Attempts to find offer slot and reports whether the request succeeded.
    private bool TryFindOfferSlot(
        PlayerShopItemUI itemUI,
        out OfferSection section,
        out int index)
    {
        section = OfferSection.Item;
        index = -1;

        if (TryFindOfferSlot(_bulletSlots, itemUI, out index))
        {
            section = OfferSection.Bullet;
            return true;
        }

        if (TryFindOfferSlot(_itemSlots, itemUI, out index))
        {
            section = OfferSection.Item;
            return true;
        }

        return false;
    }

    // Purpose: Attempts to find offer slot and reports whether the request succeeded.
    private static bool TryFindOfferSlot(
        PlayerShopItemUI[] slots,
        PlayerShopItemUI itemUI,
        out int index)
    {
        index = -1;

        if (slots == null || itemUI == null)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == itemUI)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    // Purpose: Performs the should allow duplicate workflow for this component.
    private bool ShouldAllowDuplicate(OfferSection section)
    {
        if (_activeShopConfig == null)
            return true;

        return section == OfferSection.Bullet
            ? _activeShopConfig.AllowDuplicateBulletOffers
            : _activeShopConfig.AllowDuplicateItemOffers;
    }

    // Purpose: Resolves the offer list needed by this component.
    private List<PlayerShopOffer> ResolveOfferList(OfferSection section)
    {
        return section == OfferSection.Bullet
            ? _bulletOffers
            : _itemOffers;
    }

    // Purpose: Builds the excluded items from the current authored data.
    private List<ItemSO> BuildExcludedItems(OfferSection section, int ignoreIndex)
    {
        List<PlayerShopOffer> offers = ResolveOfferList(section);
        List<ItemSO> excluded = new();

        for (int i = 0; i < offers.Count; i++)
        {
            if (i == ignoreIndex)
                continue;

            PlayerShopOffer offer = offers[i];

            if (offer != null && offer.Item != null)
                excluded.Add(offer.Item);
        }

        return excluded;
    }

    // Purpose: Performs the replace offers workflow for this component.
    private static void ReplaceOffers(
        List<PlayerShopOffer> target,
        IReadOnlyList<PlayerShopOffer> source)
    {
        target.Clear();

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
                target.Add(source[i]);
        }
    }

    // Purpose: Handles the incoming player item selection changed request or event.
    private void HandlePlayerItemSelectionChanged(PlayerInventoryItemSelection selection)
    {
        _selectedPlayerItem = selection;
        RefreshPlayerItemSelectionView();
    }

    // Purpose: Handles the incoming sell clicked request or event.
    private void HandleSellClicked()
    {
        TrySellSelectedPlayerItem();
    }

    // Purpose: Handles the incoming repair clicked request or event.
    private void HandleRepairClicked()
    {
        TryRepairSelectedItem();
    }

    // Purpose: Attempts to sell selected player item and reports whether the request succeeded.
    public bool TrySellSelectedPlayerItem()
    {
        if (_selectedPlayerItem.item is PassiveItemSO) { RaiseMessage("패시브는 판매할 수 없습니다."); return false; }
        if (!_selectedPlayerItem.HasItem)
        {
            RaiseMessage("Select an item to sell.");
            RefreshPlayerItemSelectionView();
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Inventory is not connected.");
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        int unitPrice = ResolveInventorySellUnitPrice(_selectedPlayerItem.item);
        if (!CanMerchantPaySale(unitPrice, _selectedPlayerItem.amount)) return false;
        PlayerInventoryRemovedStack removed;
        string message;

        switch (_selectedPlayerItem.kind)
        {
            case PlayerInventoryItemSelectionKind.Inventory:
                if (!_inventoryRuntime.TryRemoveInventoryItem(
                    _selectedPlayerItem.inventoryIndex,
                    _selectedPlayerItem.amount,
                    out removed,
                    out message))
                {
                    RaiseMessage(message);
                    return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
                }
                break;

            case PlayerInventoryItemSelectionKind.Equipment:
                if (!_inventoryRuntime.TryRemoveEquipped(
                    _selectedPlayerItem.equipmentSlotKind,
                    _selectedPlayerItem.equipmentSlotIndex,
                    out removed,
                    out message))
                {
                    RaiseMessage(message);
                    return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
                }
                break;

            case PlayerInventoryItemSelectionKind.Ammo:
                RaiseMessage("Legacy ammo slots cannot be sold. Move ammunition to the inventory first.");
                return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);

            default:
                RaiseMessage("Select an item to sell.");
                return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (!TryCompletePhysicalSale(removed, unitPrice, out message))
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);

        _inventoryPanel?.ClearSelectedItemSelection();
        return true;
    }

    // Purpose: Attempts to repair selected armor and reports whether the request succeeded.
    public bool TryRepairSelectedArmor()
    {
        return TryRepairSelectedItem();
    }

    // Purpose: Attempts to repair selected item and reports whether the request succeeded.
    public bool TryRepairSelectedItem()
    {
        // Reject before resolving price, consuming currency, touching inventory or playing audio.
        if (!EquipmentRepairPolicy.Enabled)
        {
            RaiseMessage(EquipmentRepairPolicy.DisabledMessage);
            return false;
        }
        if (!_selectedPlayerItem.HasItem || !_selectedPlayerItem.IsRepairable)
        {
            RaiseMessage("Select armor or a firearm to repair.");
            RefreshPlayerItemSelectionView();
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        int repairPrice = ResolveRepairPrice(_selectedPlayerItem);

        if (repairPrice <= 0)
        {
            RaiseMessage("Item is already fully repaired.");
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Inventory is not connected.");
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (!TryConsumePhysicalMoney(repairPrice, out List<ShopTradeRequirement> payment, out string paymentMessage))
        {
            RaiseMessage(paymentMessage);
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        bool repaired;
        string message;

        switch (_selectedPlayerItem.kind)
        {
            case PlayerInventoryItemSelectionKind.Inventory:
                repaired = _selectedPlayerItem.IsRangedWeapon
                    ? _inventoryRuntime.TryRepairInventoryWeaponToFull(
                        _selectedPlayerItem.inventoryIndex,
                        out _,
                        out message)
                    : _inventoryRuntime.TryRepairInventoryArmorToFull(
                        _selectedPlayerItem.inventoryIndex,
                        out _,
                        out message);
                break;

            case PlayerInventoryItemSelectionKind.Equipment:
                if (_selectedPlayerItem.IsRangedWeapon &&
                    _selectedPlayerItem.equipmentSlotKind == PlayerInventoryEquipmentSlotKind.Weapon)
                {
                    repaired = _inventoryRuntime.TryRepairEquippedWeaponToFull(
                        _selectedPlayerItem.equipmentSlotIndex,
                        out _,
                        out message);
                    break;
                }

                if (!_selectedPlayerItem.IsArmor ||
                    _selectedPlayerItem.equipmentSlotKind != PlayerInventoryEquipmentSlotKind.Armor)
                {
                    repaired = false;
                    message = "Selected item cannot be repaired.";
                    break;
                }

                repaired = _inventoryRuntime.TryRepairEquippedArmorToFull(out _, out message);
                break;

            default:
                repaired = false;
                message = "Selected item cannot be repaired.";
                break;
        }

        if (!repaired)
        {
            RefundPhysicalPayment(payment);
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        RaiseMessage(message);
        RefreshAll();
        PlayUiAudio(UIAudioCueType.Repair);
        return true;
    }

    // Purpose: Refreshes the displayed or cached player item selection view.
    private void RefreshPlayerItemSelectionView()
    {
        ItemSO item = _selectedPlayerItem.HasItem
            ? _selectedPlayerItem.item
            : null;

        bool hasItem = item != null;
        // Keep the sell action visible even before
        // selecting an item. Their buttons communicate validity through interactable.
        SetCanvasGroup(_playerItemSelectionGroup, true);

        if (_playerItemSelectionIcon != null)
        {
            _playerItemSelectionIcon.sprite = hasItem ? item.PreviewImage : null;
            _playerItemSelectionIcon.enabled = hasItem && _playerItemSelectionIcon.sprite != null;
        }
        RefreshPlayerItemNameText(item);
        RefreshPlayerItemAmountText(item, _selectedPlayerItem.amount);

        int sellPrice = hasItem
            ? ResolveInventorySellUnitPrice(item) * Mathf.Max(1, _selectedPlayerItem.amount)
            : 0;

        if (_sellPriceText != null)
        {
            _sellPriceText.text = hasItem
                ? string.Format(_priceFormat, sellPrice)
                : _emptyPriceText;
            if (_raidTrade && _activeStation != null)
                _sellPriceText.text += $"\n상인 소지금: {_activeStation.MerchantMoney:N0}";
        }

        if (_sellButton != null)
            _sellButton.interactable = hasItem;

        int repairPrice = ResolveRepairPrice(_selectedPlayerItem);
        bool canRepair = hasItem && _selectedPlayerItem.IsRepairable && repairPrice > 0;
        ApplyRepairAvailability();

        if (_repairPriceText != null)
        {
            _repairPriceText.text = canRepair
                ? string.Format(_priceFormat, repairPrice)
                : _emptyPriceText;
        }

        if (_repairButton != null)
            _repairButton.interactable = canRepair;
    }

    private void ApplyRepairAvailability()
    {
        bool enabled = EquipmentRepairPolicy.Enabled;
        var section = FindChildRect("RepairSection");
        if (section != null) section.gameObject.SetActive(enabled);
        SetCanvasGroup(_repairActionGroup, enabled);
        if (_repairButton != null)
        {
            _repairButton.gameObject.SetActive(enabled);
            if (!enabled) _repairButton.interactable = false;
        }
        if (_repairPriceText != null) _repairPriceText.gameObject.SetActive(enabled);
        if (!enabled) Stretch(FindChildRect("SellSection"), Vector2.zero, Vector2.zero);
    }
    // Purpose: Refreshes the displayed or cached player item name text.
    private void RefreshPlayerItemNameText(ItemSO item)
    {
        if (_playerItemNameText == null)
            return;

        _playerItemNameText.text = item != null
            ? InventoryItemDisplayUtility.ResolveItemName(item)
            : string.Empty;

        _playerItemNameText.gameObject.SetActive(item != null);
    }
    // Purpose: Refreshes the displayed or cached player item amount text.
    private void RefreshPlayerItemAmountText(ItemSO item, int amount)
    {
        if (_playerItemAmountText == null)
            return;

        bool showAmount = item != null && item.MaxStack > 1 && amount > 1;

        _playerItemAmountText.text = showAmount
            ? amount.ToString()
            : string.Empty;

        _playerItemAmountText.gameObject.SetActive(showAmount);
    }

    // Purpose: Resolves the repair price needed by this component.
    private int ResolveRepairPrice(PlayerInventoryItemSelection selection)
    {
        if (!EquipmentRepairPolicy.Enabled) return 0;
        if (!selection.HasItem || !selection.IsRepairable)
            return 0;

        float maxDurability;
        float currentDurability;

        if (selection.item is ArmorItemSO armor)
        {
            maxDurability = selection.armorMaxDurability > 0f
                ? selection.armorMaxDurability
                : armor.MaxDurability;
            currentDurability = selection.armorDurability;
        }
        else if (selection.item is RangedWeaponSO weapon)
        {
            maxDurability = selection.weaponMaxDurability > 0f
                ? selection.weaponMaxDurability
                : weapon.MaxDurability;
            currentDurability = selection.weaponCurrentDurability;
        }
        else
        {
            return 0;
        }

        if (maxDurability <= 0f)
            return 0;

        currentDurability = Mathf.Clamp(currentDurability, 0f, maxDurability);
        float missingRatio = Mathf.Clamp01((maxDurability - currentDurability) / maxDurability);

        if (missingRatio <= 0f)
            return 0;

        return Mathf.Max(
            1,
            Mathf.CeilToInt(selection.item.PurchasePrice * missingRatio * Mathf.Clamp01(_activeArmorRepairPriceRate)));
    }

    // Purpose: Resolves the reroll cost needed by this component.
    private int ResolveRerollCost()
    {
        return _activeShopConfig != null
            ? _activeShopConfig.RerollCost
            : 0;
    }

    // Purpose: Resolves the inventory runtime needed by this component.
    private void ResolveInventoryRuntime()
    {
        if (_inventoryRuntime != null)
            return;

        if (_inventoryRuntimeReadyChannel != null && _inventoryRuntimeReadyChannel.HasCurrent)
            _inventoryRuntime = _inventoryRuntimeReadyChannel.Current;
    }

    // Purpose: Resolves the inventory sell unit price needed by this component.
    private int ResolveInventorySellUnitPrice(ItemSO item)
    {
        if (item == null)
            return 0;

        return Mathf.Max(
            0,
            Mathf.RoundToInt(item.PurchasePrice * Mathf.Clamp01(_activeSellPriceRate)));
    }

    // Purpose: Resolves the inventory item needed by this component.
    private ItemSO ResolveInventoryItem(int slotIndex)
    {
        ResolveInventoryRuntime();

        if (_inventoryRuntime == null ||
            _inventoryRuntime.Items == null ||
            slotIndex < 0 ||
            slotIndex >= _inventoryRuntime.Items.Count)
        {
            return null;
        }

        PlayerInventoryItemStack stack = _inventoryRuntime.Items[slotIndex];
        return stack != null && !stack.IsEmpty ? stack.Item : null;
    }

    // Purpose: Removes the configured physical Money Item before a paid shop service runs.
    private bool TryConsumePhysicalMoney(
        int amount,
        out List<ShopTradeRequirement> payment,
        out string message)
    {
        payment = new List<ShopTradeRequirement>();
        amount = Mathf.Max(0, amount);

        if (amount <= 0)
        {
            message = string.Empty;
            return true;
        }

        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            message = "Inventory is not connected.";
            return false;
        }

        if (!TryResolvePhysicalMoneyItem(out CurrencyItemSO moneyItem, out message))
            return false;

        payment.Add(new ShopTradeRequirement(moneyItem, amount));

        if (_inventoryRuntime.TryConsumeInventoryItemRequirements(payment, out message))
            return true;

        payment.Clear();
        return false;
    }

    // Purpose: Restores a failed service payment without ever returning to a numerical currency wallet.
    private void RefundPhysicalPayment(IReadOnlyList<ShopTradeRequirement> payment)
    {
        if (payment == null || payment.Count == 0 || _inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryRestoreInventoryItemRequirements(payment, out _))
            return;

        for (int index = 0; index < payment.Count; index++)
        {
            ShopTradeRequirement requirement = payment[index];

            if (requirement == null || !requirement.IsValid)
                continue;

            if (TrySpawnMoneyWorldDrop(requirement.Item, requirement.Amount, out _))
                continue;

            Debug.LogError(
                $"[Shop] Physical payment refund failed. item={requirement.Item.name}, amount={requirement.Amount}",
                this);
        }
    }

    // Purpose: Pays a completed sale as an inventory Money Item, then safely falls back to a floor pickup.
    private bool TryGrantPhysicalMoney(int amount, out string message)
    {
        amount = Mathf.Max(0, amount);

        if (amount <= 0)
        {
            message = string.Empty;
            return true;
        }

        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            message = "Inventory is not connected.";
            return false;
        }

        if (!TryResolvePhysicalMoneyItem(out CurrencyItemSO moneyItem, out message))
            return false;

        if (_inventoryRuntime.TryPickup(moneyItem, amount, out _))
        {
            message = $"Received {InventoryItemDisplayUtility.ResolveItemName(moneyItem)} x{amount}.";
            return true;
        }

        if (TrySpawnMoneyWorldDrop(moneyItem, amount, out message))
            return true;

        return false;
    }

    // Purpose: Completes a sale only after its physical payout is secured; otherwise restores the sold item.
    private bool TryCompletePhysicalSale(
        PlayerInventoryRemovedStack removed,
        int unitPrice,
        out string message)
    {
        int payout = (int)System.Math.Min(int.MaxValue, (long)Mathf.Max(0, unitPrice) * Mathf.Max(1, removed.amount));
        bool reserved = !_raidTrade || (_activeStation != null && _activeStation.TryReservePayout(payout));

        string payoutMessage = "상인의 소지금이 부족하거나 거래 가능한 위치가 아닙니다.";
        if (reserved && TryGrantPhysicalMoney(payout, out payoutMessage))
        {
            message = $"Sold {InventoryItemDisplayUtility.ResolveItemName(removed.item)} x{removed.amount}. {payoutMessage}";
            RaiseMessage(message);
            RefreshAll();
            PlayUiAudio(UIAudioCueType.Sell);
            return true;
        }

        if (_raidTrade && reserved) _activeStation?.RefundPayout(payout);

        string restoreMessage = string.Empty;
        bool restored = _inventoryRuntime != null &&
                        _inventoryRuntime.TryRestoreRemovedStack(removed, out restoreMessage);

        message = restored
            ? $"Sale cancelled. Could not grant Money Item; the item was returned to inventory. {payoutMessage}"
            : $"Sale payout failed and item restoration also failed. {payoutMessage} {restoreMessage}";

        Debug.LogError($"[Shop] {message}", this);
        RaiseMessage(message);
        RefreshAll();
        return false;
    }

    private bool CanMerchantPaySale(int unitPrice, int amount)
    {
        if (!_raidTrade) return true;
        long payout = (long)Mathf.Max(0, unitPrice) * Mathf.Max(1, amount);
        if (_activeStation != null && _activeStation.CanInteract && payout <= _activeStation.MerchantMoney) return true;
        RaiseMessage($"상인 소지금 부족 / 거래 불가. 남은 돈: {(_activeStation != null ? _activeStation.MerchantMoney : 0)}");
        return false;
    }

    // Purpose: Resolves the one physical Money Item configured by the active Home shop.
    private bool TryResolvePhysicalMoneyItem(out CurrencyItemSO moneyItem, out string message)
    {
        ShopConfigSO config = _activeShopConfig != null ? _activeShopConfig : _defaultConfig;
        moneyItem = config != null ? config.DefaultCurrencyItem : null;

        if (moneyItem != null)
        {
            message = string.Empty;
            return true;
        }

        message = "Home Shop Config is missing its Default Money Item.";
        Debug.LogError($"[Shop] {message}", this);
        return false;
    }

    // Purpose: Drops a payout at the owning player's feet when inventory space cannot accept more money.
    private bool TrySpawnMoneyWorldDrop(ItemSO moneyItem, int amount, out string message)
    {
        Transform dropSource = _inventoryRuntime != null ? _inventoryRuntime.transform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(dropSource.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        Vector3 position = dropSource.position + forward * 0.55f + Vector3.up * 0.15f;
        bool spawned = WorldItemPickupSpawner.TrySpawn(
            moneyItem,
            amount,
            amount,
            0f,
            -1f,
            position,
            Quaternion.identity,
            out WorldItemPickup pickup);

        if (spawned && pickup != null)
        {
            message = $"Inventory is full. {InventoryItemDisplayUtility.ResolveItemName(moneyItem)} x{amount} was dropped at your feet.";
            return true;
        }

        message = "Inventory is full and the Money Item world drop could not be created.";
        return false;
    }

    // Purpose: Publishes the message notification.
    private void RaiseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_systemMessageChannel != null)
            _systemMessageChannel.RaiseEvent(message);
        else
            Debug.Log($"[Shop] {message}", this);
    }

    // Purpose: Publishes confirmed shop feedback to the project-wide UI audio router.
    private void PlayUiAudio(UIAudioCueType cueType)
    {
        _uiAudioEventChannel?.RaiseEvent(cueType);
    }

    // Purpose: Updates the canvas group state.
    private static void SetCanvasGroup(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
