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

    [Header("Drop Table")]
    [SerializeField] private ShopRoomDropTableSO _defaultDropTable;

    [Header("Runtime Ready")]
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _inventoryRuntimeReadyChannel;
    [SerializeField] private PlayerCurrencyRuntimeReadyEventChannelSO _currencyRuntimeReadyChannel;

    [Header("Currency")]
    [SerializeField] private bool _ignoreCurrencyUntilImplemented = true;
    [SerializeField] private PlayerCurrencyType _currencyType = PlayerCurrencyType.Run;

    [Tooltip("If true, reroll consumes currency.")]
    [FormerlySerializedAs("_rerollConsumesDebugCurrency")]
    [SerializeField] private bool _rerollConsumesCurrency = false;

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

    private readonly List<PlayerShopOffer> _bulletOffers = new();
    private readonly List<PlayerShopOffer> _itemOffers = new();

    private ShopRoomDropTableSO _activeShopRoomDropTable;
    private PlayerInventoryRuntime _inventoryRuntime;
    private PlayerCurrencyRuntime _currencyRuntime;
    private PlayerInventoryUI _inventoryPanel;
    private PlayerInventoryItemSelection _selectedPlayerItem = PlayerInventoryItemSelection.None;

    private int _debugCurrency;
    private float _activeSellPriceRate = 0.5f;
    private float _activeArmorRepairPriceRate = 0.5f;
    private bool _activeAllowReroll = true;
    private bool _initialized;

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

        if (_currencyRuntimeReadyChannel != null)
        {
            _currencyRuntimeReadyChannel.OnEventRaised += HandleCurrencyRuntimeReady;

            if (_currencyRuntimeReadyChannel.HasCurrent)
                HandleCurrencyRuntimeReady(_currencyRuntimeReadyChannel.Current);
        }

        if (_sellButton != null)
            _sellButton.onClick.AddListener(HandleSellClicked);

        if (_repairButton != null)
            _repairButton.onClick.AddListener(HandleRepairClicked);

        BindPlaceholder();
        RefreshPlayerItemSelectionView();
    }

    private void OnDisable()
    {
        if (_sectorShopOpenRequestChannel != null)
            _sectorShopOpenRequestChannel.OnEventRaised -= HandleSectorShopOpenRequested;

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.OnEventRaised -= HandleInventoryRuntimeReady;

        if (_currencyRuntimeReadyChannel != null)
            _currencyRuntimeReadyChannel.OnEventRaised -= HandleCurrencyRuntimeReady;

        if (_sellButton != null)
            _sellButton.onClick.RemoveListener(HandleSellClicked);

        if (_repairButton != null)
            _repairButton.onClick.RemoveListener(HandleRepairClicked);

        SetSellSelectionSource(null);
    }

    public void BindPlaceholder()
    {
        EnsureInitialized();
        RefreshAll();
    }

    public void OpenWithOffers(
        IReadOnlyList<PlayerShopOffer> bulletOffers,
        IReadOnlyList<PlayerShopOffer> itemOffers,
        ShopRoomDropTableSO dropTable,
        bool allowReroll)
    {
        _activeShopRoomDropTable = dropTable;
        _activeAllowReroll = allowReroll;
        _activeSellPriceRate = dropTable != null ? dropTable.SellPriceRate : 0.5f;
        _activeArmorRepairPriceRate = dropTable != null ? dropTable.ArmorRepairPriceRate : 0.5f;
        _initialized = true;

        ReplaceOffers(_bulletOffers, bulletOffers);
        ReplaceOffers(_itemOffers, itemOffers);

        ShowPanel();
        RefreshAll();
    }

    public void OpenWithOffers(
        IReadOnlyList<PlayerShopOffer> offers,
        ShopRoomDropTableSO dropTable,
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

        OpenWithOffers(bulletOffers, itemOffers, dropTable, allowReroll);
    }

    public void ClosePanel()
    {
        HidePanel();
    }

    public override void OnOverlayShown()
    {
    }

    protected override void OnPanelHidden()
    {
        SetSellSelectionSource(null);
    }

    public void SetSellSelectionSource(PlayerInventoryUI inventoryPanel)
    {
        SetPlayerItemSelectionSource(inventoryPanel);
    }

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

    public bool TrySellInventorySlot(int slotIndex, int amount = 1)
    {
        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Inventory is not connected.");
            return false;
        }

        ItemSO item = ResolveInventoryItem(slotIndex);
        int unitPrice = ResolveInventorySellUnitPrice(item);

        if (!_inventoryRuntime.TrySellInventoryItem(
                slotIndex,
                amount,
                unitPrice,
                out int currencyGained,
                out string message))
        {
            RaiseMessage(message);
            return false;
        }

        GrantCurrency(currencyGained);
        RaiseMessage(message);
        RefreshAll();
        return true;
    }

    public bool TrySellEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Inventory is not connected.");
            return false;
        }

        ItemSO item = slotKind == PlayerInventoryEquipmentSlotKind.Weapon
            ? (ItemSO)_inventoryRuntime.EquippedWeapon
            : _inventoryRuntime.EquippedArmor;

        int unitPrice = ResolveInventorySellUnitPrice(item);

        if (!_inventoryRuntime.TrySellEquipped(
                slotKind,
                unitPrice,
                out int currencyGained,
                out string message))
        {
            RaiseMessage(message);
            return false;
        }

        GrantCurrency(currencyGained);
        RaiseMessage(message);
        RefreshAll();
        return true;
    }

    public void ResetShopForNewRun()
    {
        _initialized = false;
        EnsureInitialized();
        RefreshAll();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _debugCurrency = 0;
        _activeShopRoomDropTable = _defaultDropTable;
        _activeAllowReroll = _defaultDropTable != null;
        _activeSellPriceRate = _defaultDropTable != null ? _defaultDropTable.SellPriceRate : 0.5f;
        _activeArmorRepairPriceRate = _defaultDropTable != null ? _defaultDropTable.ArmorRepairPriceRate : 0.5f;

        RollAllOffers();

        _initialized = true;
    }

    private void HandleSectorShopOpenRequested(SectorShopOpenRequest request)
    {
        OpenWithOffers(
            request.bulletOffers,
            request.itemOffers,
            request.dropTable,
            request.allowReroll);
    }

    private void HandleInventoryRuntimeReady(PlayerInventoryRuntime runtime)
    {
        _inventoryRuntime = runtime;
    }

    private void HandleCurrencyRuntimeReady(PlayerCurrencyRuntime runtime)
    {
        _currencyRuntime = runtime;
    }

    private void RollAllOffers()
    {
        _bulletOffers.Clear();
        _itemOffers.Clear();

        if (_defaultDropTable == null)
            return;

        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        _bulletOffers.AddRange(_defaultDropTable.CreateBulletOffers(seed));
        _itemOffers.AddRange(_defaultDropTable.CreateItemOffers(seed + 1009));
    }

    private void RefreshAll()
    {
        RefreshOfferViews();
        RefreshPlayerItemSelectionView();
    }

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

        if (!TrySpendCurrency(offer.Price))
        {
            RaiseMessage("Not enough currency.");
            return;
        }

        if (!_inventoryRuntime.TryPickup(
                offer.Item,
                1,
                offer.BundleAmount,
                _activeSellPriceRate,
                out string message))
        {
            RefundCurrency(offer.Price);
            RaiseMessage(message);
            return;
        }

        offer.TryConsumeStock();
        RaiseMessage(message);
        RefreshAll();
    }

    private void HandleRerollRequested(PlayerShopItemUI itemUI)
    {
        if (itemUI == null)
            return;

        if (!TryFindOfferSlot(itemUI, out OfferSection section, out int index))
            return;

        HandleDropTableReroll(section, index);
    }

    private void HandleDropTableReroll(OfferSection section, int index)
    {
        if (!_activeAllowReroll || _activeShopRoomDropTable == null)
        {
            RaiseMessage("Reroll is not available.");
            return;
        }

        int cost = ResolveRerollCost();

        if (_rerollConsumesCurrency && !TrySpendCurrency(cost))
        {
            RaiseMessage("Not enough currency.");
            return;
        }

        IReadOnlyList<ItemSO> excluded = ShouldAllowDuplicate(section)
            ? null
            : BuildExcludedItems(section, index);

        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        bool picked = section == OfferSection.Bullet
            ? _activeShopRoomDropTable.TryCreateBulletOffer(seed, excluded, out PlayerShopOffer offer)
            : _activeShopRoomDropTable.TryCreateItemOffer(seed, excluded, out offer);

        if (!picked)
        {
            RefundCurrency(_rerollConsumesCurrency ? cost : 0);
            RaiseMessage("No shop item available.");
            return;
        }

        List<PlayerShopOffer> target = ResolveOfferList(section);

        while (target.Count <= index)
            target.Add(null);

        target[index] = offer;
        RefreshAll();
    }

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

    private bool ShouldAllowDuplicate(OfferSection section)
    {
        if (_activeShopRoomDropTable == null)
            return true;

        return section == OfferSection.Bullet
            ? _activeShopRoomDropTable.AllowDuplicateBulletOffers
            : _activeShopRoomDropTable.AllowDuplicateItemOffers;
    }

    private List<PlayerShopOffer> ResolveOfferList(OfferSection section)
    {
        return section == OfferSection.Bullet
            ? _bulletOffers
            : _itemOffers;
    }

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

    private void HandlePlayerItemSelectionChanged(PlayerInventoryItemSelection selection)
    {
        _selectedPlayerItem = selection;
        RefreshPlayerItemSelectionView();
    }

    private void HandleSellClicked()
    {
        TrySellSelectedPlayerItem();
    }

    private void HandleRepairClicked()
    {
        TryRepairSelectedArmor();
    }

    public bool TrySellSelectedPlayerItem()
    {
        if (!_selectedPlayerItem.HasItem)
        {
            RaiseMessage("Select an item to sell.");
            RefreshPlayerItemSelectionView();
            return false;
        }

        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Inventory is not connected.");
            return false;
        }

        int unitPrice = ResolveInventorySellUnitPrice(_selectedPlayerItem.item);
        bool sold;
        int currencyGained;
        string message;

        switch (_selectedPlayerItem.kind)
        {
            case PlayerInventoryItemSelectionKind.Inventory:
                sold = _inventoryRuntime.TrySellInventoryItem(
                    _selectedPlayerItem.inventoryIndex,
                    _selectedPlayerItem.amount,
                    unitPrice,
                    out currencyGained,
                    out message);
                break;

            case PlayerInventoryItemSelectionKind.Equipment:
                sold = _inventoryRuntime.TrySellEquipped(
                    _selectedPlayerItem.equipmentSlotKind,
                    unitPrice,
                    out currencyGained,
                    out message);
                break;

            case PlayerInventoryItemSelectionKind.Ammo:
                sold = _inventoryRuntime.TrySellAmmoSlot(
                    _selectedPlayerItem.ammoSlotIndex,
                    unitPrice,
                    out currencyGained,
                    out message);
                break;

            default:
                RaiseMessage("Select an item to sell.");
                return false;
        }

        if (!sold)
        {
            RaiseMessage(message);
            return false;
        }

        GrantCurrency(currencyGained);
        _inventoryPanel?.ClearSelectedItemSelection();
        RaiseMessage(message);
        RefreshAll();
        return true;
    }

    public bool TryRepairSelectedArmor()
    {
        if (!_selectedPlayerItem.HasItem || !_selectedPlayerItem.IsArmor)
        {
            RaiseMessage("Select armor to repair.");
            RefreshPlayerItemSelectionView();
            return false;
        }

        int repairPrice = ResolveRepairPrice(_selectedPlayerItem);

        if (repairPrice <= 0)
        {
            RaiseMessage("Armor is already fully repaired.");
            return false;
        }

        ResolveInventoryRuntime();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Inventory is not connected.");
            return false;
        }

        if (!TrySpendCurrency(repairPrice))
        {
            RaiseMessage("Not enough currency.");
            return false;
        }

        bool repaired;
        string message;

        switch (_selectedPlayerItem.kind)
        {
            case PlayerInventoryItemSelectionKind.Inventory:
                repaired = _inventoryRuntime.TryRepairInventoryArmorToFull(
                    _selectedPlayerItem.inventoryIndex,
                    out _,
                    out message);
                break;

            case PlayerInventoryItemSelectionKind.Equipment:
                if (_selectedPlayerItem.equipmentSlotKind != PlayerInventoryEquipmentSlotKind.Armor)
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
            RefundCurrency(repairPrice);
            RaiseMessage(message);
            return false;
        }

        RaiseMessage(message);
        RefreshAll();
        return true;
    }

    private void RefreshPlayerItemSelectionView()
    {
        ItemSO item = _selectedPlayerItem.HasItem
            ? _selectedPlayerItem.item
            : null;

        bool hasItem = item != null;
        SetCanvasGroup(_playerItemSelectionGroup, hasItem);

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
        }

        if (_sellButton != null)
            _sellButton.interactable = hasItem;

        int repairPrice = ResolveRepairPrice(_selectedPlayerItem);
        bool canRepair = hasItem && _selectedPlayerItem.IsArmor && repairPrice > 0;
        SetCanvasGroup(_repairActionGroup, canRepair);

        if (_repairPriceText != null)
        {
            _repairPriceText.text = canRepair
                ? string.Format(_priceFormat, repairPrice)
                : _emptyPriceText;
        }

        if (_repairButton != null)
            _repairButton.interactable = canRepair;
    }
    private void RefreshPlayerItemNameText(ItemSO item)
    {
        if (_playerItemNameText == null)
            return;

        _playerItemNameText.text = item != null
            ? InventoryItemDisplayUtility.ResolveItemName(item)
            : string.Empty;

        _playerItemNameText.gameObject.SetActive(item != null);
    }
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

    private int ResolveRepairPrice(PlayerInventoryItemSelection selection)
    {
        if (!selection.HasItem || selection.item is not ArmorItemSO armor)
            return 0;

        float maxDurability = selection.armorMaxDurability > 0f
            ? selection.armorMaxDurability
            : armor.MaxDurability;

        if (maxDurability <= 0f)
            return 0;

        float currentDurability = Mathf.Clamp(selection.armorDurability, 0f, maxDurability);
        float missingRatio = Mathf.Clamp01((maxDurability - currentDurability) / maxDurability);

        if (missingRatio <= 0f)
            return 0;

        return Mathf.Max(
            1,
            Mathf.CeilToInt(armor.PurchasePrice * missingRatio * Mathf.Clamp01(_activeArmorRepairPriceRate)));
    }

    private int ResolveRerollCost()
    {
        return _activeShopRoomDropTable != null
            ? _activeShopRoomDropTable.RerollCost
            : 0;
    }

    private void ResolveInventoryRuntime()
    {
        if (_inventoryRuntime != null)
            return;

        if (_inventoryRuntimeReadyChannel != null && _inventoryRuntimeReadyChannel.HasCurrent)
            _inventoryRuntime = _inventoryRuntimeReadyChannel.Current;
    }

    private void ResolveCurrencyRuntime()
    {
        if (_currencyRuntime != null)
            return;

        if (_currencyRuntimeReadyChannel != null && _currencyRuntimeReadyChannel.HasCurrent)
            _currencyRuntime = _currencyRuntimeReadyChannel.Current;
    }

    private int ResolveInventorySellUnitPrice(ItemSO item)
    {
        if (item == null)
            return 0;

        return Mathf.Max(
            0,
            Mathf.RoundToInt(item.PurchasePrice * Mathf.Clamp01(_activeSellPriceRate)));
    }

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

    private bool TrySpendCurrency(int amount)
    {
        amount = Mathf.Max(0, amount);

        if (amount <= 0)
            return true;

        ResolveCurrencyRuntime();

        if (_currencyRuntime != null)
            return _currencyRuntime.TrySpendCurrency(_currencyType, amount);

        if (!_ignoreCurrencyUntilImplemented)
            return false;

        _debugCurrency = Mathf.Max(0, _debugCurrency - amount);
        return true;
    }

    private void RefundCurrency(int amount)
    {
        amount = Mathf.Max(0, amount);

        if (amount <= 0)
            return;

        ResolveCurrencyRuntime();

        if (_currencyRuntime != null)
        {
            _currencyRuntime.AddCurrency(_currencyType, amount);
            return;
        }

        if (_ignoreCurrencyUntilImplemented)
            _debugCurrency += amount;
    }

    private void GrantCurrency(int amount)
    {
        amount = Mathf.Max(0, amount);

        if (amount <= 0)
            return;

        ResolveCurrencyRuntime();

        if (_currencyRuntime != null)
            _currencyRuntime.AddCurrency(_currencyType, amount);
        else
            _debugCurrency += amount;
    }

    private void RaiseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_systemMessageChannel != null)
            _systemMessageChannel.RaiseEvent(message);
        else
            Debug.Log($"[Shop] {message}", this);
    }

    private static void SetCanvasGroup(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
