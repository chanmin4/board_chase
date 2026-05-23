using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerShopPanelUI : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private PlayerShopCatalogSO _shopCatalog;

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

    private int _debugCurrency;
    private int _nextRequestId;
    private bool _initialized;

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

        int rerollCost = _shopCatalog != null ? _shopCatalog.RerollCost : 0;

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
                _shopCatalog != null ? _shopCatalog.SellPriceRate : 0.5f));
    }

    private void HandleRerollRequested(PlayerShopItemUI itemUI)
    {
        if (_shopCatalog == null || itemUI == null)
            return;

        int index = FindItemSlotIndex(itemUI);
        if (index < 0)
            return;

        int cost = _shopCatalog.RerollCost;

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
}