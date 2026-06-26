using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerShopPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private CanvasGroup _panelGroup;

    [Header("Catalog")]
    [SerializeField] private PlayerShopCatalogSO _shopCatalog;

    [Header("Player")]
    [SerializeField] private PlayerPassiveInventoryRuntime _passiveInventory;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;

    [Header("Currency Placeholder")]
    [SerializeField] private TextMeshProUGUI _currencyText;
    [SerializeField] private string _currencyFormat = "{0}";

    [Tooltip("If true, purchase ignores money until the real currency system exists.")]
    [SerializeField] private bool _ignoreCurrencyUntilImplemented = true;

    [Tooltip("If true, reroll consumes debug money. Usually false until currency is implemented.")]
    [SerializeField] private bool _rerollConsumesDebugCurrency = false;

    [Header("Shop Items")]
    [Tooltip("Assign the already placed shop item prefabs here. Usually 3 items.")]
    [SerializeField] private PlayerShopItemUI[] _itemSlots;

    [Header("Events")]
    [SerializeField] private WeaponAmmoShopPurchaseRequestEventChannelSO _purchaseRequestChannel;
    [SerializeField] private WeaponAmmoShopPurchaseResultEventChannelSO _purchaseResultChannel;
    [SerializeField] private WeaponAmmoSellResultEventChannelSO _sellResultChannel;
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;

    private readonly List<PlayerShopOffer> _offers = new();
    private readonly Dictionary<int, PlayerShopOffer> _pendingPurchases = new();

    private ShopRoomDropTableSO _activeShopRoomDropTable;
    private int _debugCurrency;
    private int _nextRequestId;
    private float _activeSellPriceRate = 0.5f;
    private bool _usingExternalOffers;
    private bool _activeAllowReroll = true;
    private bool _initialized;

    private void Reset()
    {
        _panelGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (_purchaseResultChannel != null)
            _purchaseResultChannel.OnEventRaised += HandlePurchaseResult;

        if (_sellResultChannel != null)
            _sellResultChannel.OnEventRaised += HandleSellResult;

        BindPlaceholder();
    }

    private void OnDisable()
    {
        if (_purchaseResultChannel != null)
            _purchaseResultChannel.OnEventRaised -= HandlePurchaseResult;

        if (_sellResultChannel != null)
            _sellResultChannel.OnEventRaised -= HandleSellResult;
    }

    public void BindPlaceholder()
    {
        EnsureInitialized();
        RefreshAll();
    }

    public void OpenWithOffers(
        IReadOnlyList<PlayerShopOffer> offers,
        ShopRoomDropTableSO dropTable,
        bool allowReroll)
    {
        _usingExternalOffers = true;
        _activeShopRoomDropTable = dropTable;
        _activeAllowReroll = allowReroll;
        _activeSellPriceRate = dropTable != null ? dropTable.SellPriceRate : 0.5f;
        _initialized = true;

        _offers.Clear();
        _pendingPurchases.Clear();

        if (offers != null)
        {
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i] != null)
                    _offers.Add(offers[i]);
            }
        }

        SetPanelVisible(true);
        RefreshAll();
    }

    public void ClosePanel()
    {
        SetPanelVisible(false);
    }

    public void ResetShopForNewRun()
    {
        _initialized = false;
        EnsureInitialized();
        RollAllOffers();
        RefreshAll();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _debugCurrency = _shopCatalog != null
            ? _shopCatalog.StartingDebugCurrency
            : 0;

        _usingExternalOffers = false;
        _activeShopRoomDropTable = null;
        _activeAllowReroll = true;
        _activeSellPriceRate = _shopCatalog != null ? _shopCatalog.SellPriceRate : 0.5f;

        RollAllOffers();

        _initialized = true;
    }

    private void RollAllOffers()
    {
        _offers.Clear();

        if (_shopCatalog == null)
            return;

        List<PlayerShopCatalogSO.BulletShopEntry> entries =
            _shopCatalog.CreateRandomEntries();

        for (int i = 0; i < entries.Count; i++)
            _offers.Add(new PlayerShopOffer(entries[i]));
        Debug.Log($"[PlayerShopPanelUI] Rolled offers: {_offers.Count}", this);
    }

    private void RefreshAll()
    {
        RefreshCurrency();
        RefreshOfferViews();
    }

    private void RefreshCurrency()
    {
        if (_currencyText != null)
            _currencyText.text = string.Format(_currencyFormat, _debugCurrency);
    }

    private void RefreshOfferViews()
    {
        if (_itemSlots == null)
            return;

        int rerollCost = ResolveRerollCost();

        for (int i = 0; i < _itemSlots.Length; i++)
        {
            PlayerShopItemUI view = _itemSlots[i];

            if (view == null)
                continue;

            if (i < _offers.Count)
            {
                view.Bind(
                    _offers[i],
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

        if (!_ignoreCurrencyUntilImplemented && _debugCurrency < offer.Price)
        {
            RaiseMessage("Not enough currency.");
            return;
        }

        if (offer.IsPassive)
        {
            TryPurchasePassive(offer);
            return;
        }

        if (offer.IsArmor)
        {
            TryPurchaseArmor(offer);
            return;
        }

        if (!offer.IsBullet)
        {
            RaiseMessage("Invalid shop item.");
            return;
        }

        if (_purchaseRequestChannel == null)
        {
            RaiseMessage("Purchase system is not connected.");
            return;
        }

        int requestId = ++_nextRequestId;
        _pendingPurchases[requestId] = offer;

        _purchaseRequestChannel.RaiseEvent(
            new WeaponAmmoShopPurchaseRequest(
                requestId,
                offer.Bullet,
                offer.BundleAmount,
                offer.Price,
                _activeSellPriceRate));
    }

    private void HandleRerollRequested(PlayerShopItemUI itemUI)
    {
        if (itemUI == null)
            return;

        int index = FindItemSlotIndex(itemUI);
        if (index < 0)
            return;

        if (_usingExternalOffers)
        {
            HandleExternalReroll(index);
            return;
        }

        if (_shopCatalog == null)
            return;

        int cost = ResolveRerollCost();

        if (_rerollConsumesDebugCurrency && _debugCurrency < cost)
        {
            RaiseMessage("Not enough currency.");
            return;
        }

        List<PlayerShopCatalogSO.BulletShopEntry> excluded = BuildExcludedEntries(index);

        bool picked = _shopCatalog.AllowDuplicateOffers
            ? _shopCatalog.TryCreateRandomEntry(out PlayerShopCatalogSO.BulletShopEntry entry)
            : _shopCatalog.TryCreateRandomEntry(excluded, out entry);

        if (!picked)
        {
            RaiseMessage("No shop item available.");
            return;
        }

        if (_rerollConsumesDebugCurrency)
            _debugCurrency -= cost;

        while (_offers.Count <= index)
            _offers.Add(null);

        _offers[index] = new PlayerShopOffer(entry);

        RefreshAll();
    }

    private void HandleExternalReroll(int index)
    {
        if (!_activeAllowReroll || _activeShopRoomDropTable == null)
        {
            RaiseMessage("Reroll is not available.");
            return;
        }

        int cost = ResolveRerollCost();

        if (_rerollConsumesDebugCurrency && _debugCurrency < cost)
        {
            RaiseMessage("Not enough currency.");
            return;
        }

        IReadOnlyList<ItemSO> excluded = _activeShopRoomDropTable.AllowDuplicateOffers
            ? null
            : BuildExcludedItems(index);

        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        if (!_activeShopRoomDropTable.TryCreateOffer(seed, excluded, out PlayerShopOffer offer))
        {
            RaiseMessage("No shop item available.");
            return;
        }

        if (_rerollConsumesDebugCurrency)
            _debugCurrency -= cost;

        while (_offers.Count <= index)
            _offers.Add(null);

        _offers[index] = offer;
        RefreshAll();
    }

    private List<PlayerShopCatalogSO.BulletShopEntry> BuildExcludedEntries(int ignoreIndex)
    {
        List<PlayerShopCatalogSO.BulletShopEntry> excluded = new();

        for (int i = 0; i < _offers.Count; i++)
        {
            if (i == ignoreIndex)
                continue;

            PlayerShopOffer offer = _offers[i];

            if (offer != null && offer.Entry != null)
                excluded.Add(offer.Entry);
        }

        return excluded;
    }

    private int FindItemSlotIndex(PlayerShopItemUI itemUI)
    {
        if (_itemSlots == null)
            return -1;

        for (int i = 0; i < _itemSlots.Length; i++)
        {
            if (_itemSlots[i] == itemUI)
                return i;
        }

        return -1;
    }

    private List<ItemSO> BuildExcludedItems(int ignoreIndex)
    {
        List<ItemSO> excluded = new();

        for (int i = 0; i < _offers.Count; i++)
        {
            if (i == ignoreIndex)
                continue;

            PlayerShopOffer offer = _offers[i];

            if (offer != null && offer.Item != null)
                excluded.Add(offer.Item);
        }

        return excluded;
    }

    private void TryPurchasePassive(PlayerShopOffer offer)
    {
        if (offer == null || !offer.IsPassive)
            return;

        ResolvePassiveInventory();

        if (_passiveInventory == null)
        {
            RaiseMessage("Passive inventory is not connected.");
            return;
        }

        if (!_passiveInventory.TryAdd(offer.PassiveItem))
        {
            RaiseMessage("Passive item already owned or rejected.");
            return;
        }

        if (!_ignoreCurrencyUntilImplemented)
            _debugCurrency -= offer.Price;

        offer.TryConsumeStock();
        RaiseMessage($"{offer.DisplayName} purchased.");
        RefreshAll();
    }

    private void TryPurchaseArmor(PlayerShopOffer offer)
    {
        if (offer == null || !offer.IsArmor)
            return;

        ResolveEquipmentRuntime();

        if (_equipmentRuntime == null)
        {
            RaiseMessage("Equipment runtime is not connected.");
            return;
        }

        _equipmentRuntime.EquipArmor(offer.ArmorItem);

        if (!_ignoreCurrencyUntilImplemented)
            _debugCurrency -= offer.Price;

        offer.TryConsumeStock();
        RaiseMessage($"{offer.DisplayName} equipped.");
        RefreshAll();
    }

    private void HandlePurchaseResult(WeaponAmmoShopPurchaseResult result)
    {
        if (!_pendingPurchases.TryGetValue(result.requestId, out PlayerShopOffer offer))
            return;

        _pendingPurchases.Remove(result.requestId);

        if (!result.success)
        {
            RaiseMessage(result.message);
            return;
        }

        if (!_ignoreCurrencyUntilImplemented)
            _debugCurrency -= offer.Price;

        offer.TryConsumeStock();

        RaiseMessage(result.message);
        RefreshAll();
    }

    private void HandleSellResult(WeaponAmmoSellResult result)
    {
        if (!result.success)
        {
            RaiseMessage(result.message);
            return;
        }

        _debugCurrency += Mathf.Max(0, result.currencyGained);
        RaiseMessage(result.message);
        RefreshAll();
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

    private int ResolveRerollCost()
    {
        if (_usingExternalOffers)
            return _activeShopRoomDropTable != null ? _activeShopRoomDropTable.RerollCost : 0;

        return _shopCatalog != null ? _shopCatalog.RerollCost : 0;
    }

    private void ResolvePassiveInventory()
    {
        if (_passiveInventory != null)
            return;

        _passiveInventory = FindAnyObjectByType<PlayerPassiveInventoryRuntime>();
    }

    private void ResolveEquipmentRuntime()
    {
        if (_equipmentRuntime != null)
            return;

        _equipmentRuntime = FindAnyObjectByType<EntityEquipmentRuntime>();
    }

    private void SetPanelVisible(bool visible)
    {
        if (_panelGroup == null)
            _panelGroup = GetComponent<CanvasGroup>();

        if (_panelGroup == null)
            return;

        _panelGroup.alpha = visible ? 1f : 0f;
        _panelGroup.interactable = visible;
        _panelGroup.blocksRaycasts = visible;
    }
}
