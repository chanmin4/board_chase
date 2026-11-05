/*
 * Role hierarchy: Gameplay > World > Sectors > Shop
 * Primary responsibility: Manages SectorShop stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// SectorShop.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SectorShop : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorRuntime _sector;

    [Header("Overlay")]
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;
    [SerializeField] private SectorShopOpenRequestEventChannelSO _shopOpenRequestChannel;

    [Header("Listening")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;

    [Header("Seed")]
    [SerializeField] private int _offerSeedSalt = 9419;

    [Header("Debug")]
    [SerializeField] private bool _logWarnings = true;

    private readonly List<PlayerShopOffer> _bulletOffers = new();
    private readonly List<PlayerShopOffer> _itemOffers = new();

    private SectorStateManager _sectorStateManager;
    private ShopConfigSO _activeConfig;
    private bool _offersRolled;
    private bool _allowStartSectorHub;
    private ShopRunRuntime _shopRunRuntime;
    private int _restockRevision;
    private float _nextRestockTime = -1f;
    private bool _raidMerchant;
    private int _raidSeed;
    private readonly MerchantFunds _funds = new();
    public bool IsRaidMerchant => _raidMerchant;
    public int MerchantMoney => _funds.balance;
    public bool TryReservePayout(int amount) => !_raidMerchant || (CanInteract && _funds.TryReserve(amount));
    public void RefundPayout(int amount) { if (_raidMerchant) _funds.Credit(amount); }
    public void ReceivePhysicalPayment(IReadOnlyList<ShopTradeRequirement> payment)
    {
        if (!_raidMerchant || payment == null) return;
        foreach (var row in payment) if (row.Item == _activeConfig.DefaultCurrencyItem) _funds.Credit(row.Amount);
    }

    public void ConfigureForRaid(SectorRuntime sector, ShopConfigSO config, int seed, int initialMoney,
        SectorShopOpenRequestEventChannelSO openChannel, UIOverlayRequestEventChannelSO overlay)
    {
        _sector = sector; _raidMerchant = true; _raidSeed = seed;
        _funds.balance = Mathf.Max(0, initialMoney);
        _shopOpenRequestChannel = openChannel; _overlayRequestChannel = overlay;
        Configure(config);
    }

    public bool CanInteract => IsShopRoom();

    // Purpose: Configures this component''s configure.
    public bool Configure(ShopConfigSO config)
    {
        return Configure(config, SaveSystem.Active);
    }

    // Purpose: Configures this component''s configure.
    public bool Configure(ShopConfigSO config, SaveSystem saveSystem)
    {
        if (config == null)
        {
            LogError("ShopConfigSO is missing. The Home Shop UI will still open with empty offer slots.");
        }

        _activeConfig = config;
        // Raid catalog is independent of permanent Home-shop unlock ownership.
        if (!_raidMerchant) ShopUnlockRuntime.EnsureRuntime().Configure(config, saveSystem);
        _offersRolled = false;
        _bulletOffers.Clear();
        _itemOffers.Clear();
        _restockRevision = 0;
        _nextRestockTime = -1f;
        _shopRunRuntime ??= ShopRunRuntime.EnsureRuntime();
        _shopRunRuntime.TryApplyPendingState(this);
        return true;
    }

    // Purpose: Configures this component''s for home.
    public bool ConfigureForHome(
        SectorRuntime sector,
        ShopConfigSO config)
    {
        return ConfigureForHome(sector, config, SaveSystem.Active);
    }

    // Purpose: Configures this component''s for home.
    public bool ConfigureForHome(
        SectorRuntime sector,
        ShopConfigSO config,
        SaveSystem saveSystem)
    {
        _sector = sector;
        _allowStartSectorHub = true;
        return Configure(config, saveSystem);
    }

#if UNITY_EDITOR
    public IReadOnlyList<PlayerShopOffer> DebugBulletOffers => _bulletOffers;
    public IReadOnlyList<PlayerShopOffer> DebugItemOffers => _itemOffers;
    public bool DebugOffersInitialized => _offersRolled;

    // Purpose: Performs the debug try ensure offers workflow for this component.
    public bool DebugTryEnsureOffers(out string message)
    {
        ResolveRefs();

        // Direct DebugMonitor edits deliberately do not roll the normal pool.
        // This makes it possible to test an otherwise empty shop with exactly
        // the offer selected in the monitor.
        if (!_offersRolled)
        {
            _bulletOffers.Clear();
            _itemOffers.Clear();
            _offersRolled = true;
        }

        message =
            $"Shop offers ready. bullets={_bulletOffers.Count}, items={_itemOffers.Count}";
        return true;
    }

    // Purpose: Performs the debug try add offer workflow for this component.
    public bool DebugTryAddOffer(
        ItemSO item,
        int price,
        int stock,
        int bundleAmount,
        out string message)
    {
        if (item == null)
        {
            message = "Select an ItemSO to add.";
            return false;
        }

        if (stock <= 0)
        {
            message = "Shop stock must be greater than zero.";
            return false;
        }

        if (bundleAmount <= 0)
        {
            message = "Shop bundle amount must be greater than zero.";
            return false;
        }

        // DebugMonitor must never roll the authored random pool as a side
        // effect of adding one explicit offer. An empty shop is a valid test
        // state, so initialize its runtime lists without generating offers.
        if (!DebugTryEnsureOffers(out _))
        {
            message = "Shop offers could not be initialized.";
            return false;
        }

        IReadOnlyList<ShopTradeRequirement> payment = _activeConfig != null
            ? _activeConfig.CreateDefaultCurrencyPayment(Mathf.Max(0, price))
            : null;
        if (payment == null || payment.Count == 0)
        {
            message = "Debug offer needs an assigned ShopConfigSO Default Money Item and a positive price.";
            return false;
        }

        TreasureRewardKind kind = ResolveDebugOfferKind(item);
        PlayerShopOffer offer = new PlayerShopOffer(
            item,
            kind,
            item.Rarity,
            Mathf.Max(0, price),
            stock,
            bundleAmount,
            tradeRequirements: payment);

        if (item is BulletSO)
            _bulletOffers.Add(offer);
        else
            _itemOffers.Add(offer);

        _offersRolled = true;
        message =
            $"Added shop offer. item={item.name}, price={offer.Price}, " +
            $"stock={offer.InitialStock}, bundle={offer.BundleAmount}";
        return true;
    }

    // Purpose: Performs the debug clear offers workflow for this component.
    public bool DebugClearOffers(out string message)
    {
        int removedCount = _bulletOffers.Count + _itemOffers.Count;
        _bulletOffers.Clear();
        _itemOffers.Clear();

        // Keep the shop empty until it is explicitly reconfigured or rerolled.
        _offersRolled = true;
        message = $"Cleared {removedCount} shop offer(s).";
        return true;
    }

    // Purpose: Resolves the debug offer kind needed by this component.
    private static TreasureRewardKind ResolveDebugOfferKind(ItemSO item)
    {
        if (item is BulletSO)
            return TreasureRewardKind.Bullet;

        if (item is ArmorItemSO)
            return TreasureRewardKind.Armor;

        if (item is CurrencyItemSO)
            return TreasureRewardKind.Currency;

        // The existing shop purchase path consumes offer.Item directly, so
        // weapons, consumables, and passive items safely share the item section.
        return TreasureRewardKind.Passive;
    }
#endif

    /// <summary>
    /// Captures this station's runtime offers for the active run. Only
    /// ShopRunRuntime calls this; gameplay continues to use the normal station
    /// interaction and stock-consumption paths.
    /// </summary>
    public bool CaptureRunState(PlayerRunShopStateSaveData target)
    {
        if (target == null || !TryGetRunStateId(out string stateId))
            return false;

        target.shopStateId = stateId;
        target.offersRolled = _offersRolled;
        target.restockRevision = Mathf.Max(0, _restockRevision);
        target.merchantMoney = _raidMerchant ? _funds.balance : -1;
        target.bulletOffers.Clear();
        target.itemOffers.Clear();
        CaptureOffers(_bulletOffers, target.bulletOffers);
        CaptureOffers(_itemOffers, target.itemOffers);
        target.EnsureDefaults();
        return true;
    }

    /// <summary>
    /// Rebuilds saved offers from the project content catalog. The timed
    /// restock clock restarts from app resume, so offline time is never counted
    /// as active game time.
    /// </summary>
    public bool TryRestoreRunState(
        PlayerRunShopStateSaveData source,
        RuntimeContentCatalogSO catalog,
        out string message)
    {
        if (source == null || catalog == null ||
            !TryGetRunStateId(out string stateId) ||
            !string.Equals(source.shopStateId, stateId, System.StringComparison.Ordinal))
        {
            message = "Saved shop identity or content catalog is missing.";
            return false;
        }

        List<PlayerShopOffer> restoredBullets = new();
        List<PlayerShopOffer> restoredItems = new();
        if (!TryRestoreOffers(source.bulletOffers, catalog, restoredBullets, out message) ||
            !TryRestoreOffers(source.itemOffers, catalog, restoredItems, out message))
        {
            return false;
        }

        _bulletOffers.Clear();
        _bulletOffers.AddRange(restoredBullets);
        _itemOffers.Clear();
        _itemOffers.AddRange(restoredItems);
        _offersRolled = source.offersRolled;
        _restockRevision = Mathf.Max(0, source.restockRevision);
        if (_raidMerchant && source.merchantMoney >= 0) _funds.balance = source.merchantMoney;
        _nextRestockTime = _offersRolled && _activeConfig != null && _activeConfig.HasTimedRestock
            ? Time.unscaledTime + _activeConfig.RestockIntervalSeconds
            : -1f;
        message = $"Restored bullets={_bulletOffers.Count}, items={_itemOffers.Count}, revision={_restockRevision}.";
        return true;
    }

    /// <summary>
    /// Builds a stable identity for this configured Home-shop station. The
    /// coordinate keeps it extensible if a future Home scene adds another shop
    /// using the same ShopConfigSO.
    /// </summary>
    public bool TryGetRunStateId(out string stateId)
    {
        ResolveRefs();
        if (_activeConfig == null)
        {
            stateId = string.Empty;
            return false;
        }

        Vector2Int coord = ResolveSectorCoord();
        stateId = _raidMerchant ? $"raid:{_raidSeed}:{_activeConfig.ConfigId}@{coord.x},{coord.y}" :
            $"{_activeConfig.ConfigId}@{coord.x},{coord.y}";
        return true;
    }

    private static void CaptureOffers(
        IReadOnlyList<PlayerShopOffer> source,
        List<PlayerRunShopOfferSaveData> target)
    {
        for (int index = 0; source != null && index < source.Count; index++)
        {
            PlayerShopOffer offer = source[index];
            if (offer?.Item == null || string.IsNullOrWhiteSpace(offer.Item.Guid))
                continue;

            target.Add(new PlayerRunShopOfferSaveData
            {
                itemGuid = offer.Item.Guid,
                kind = (int)offer.Kind,
                rarity = (int)offer.Rarity,
                price = offer.Price,
                initialStock = offer.InitialStock,
                remainingStock = offer.RemainingStock,
                bundleAmount = offer.BundleAmount,
                unlimitedStock = offer.IsUnlimitedStock,
                canReroll = offer.CanReroll,
                paymentRequirements = CapturePaymentRequirements(offer.TradeRequirements)
            });
        }
    }

    private static List<PlayerRunShopPaymentRequirementSaveData> CapturePaymentRequirements(
        IReadOnlyList<ShopTradeRequirement> source)
    {
        List<PlayerRunShopPaymentRequirementSaveData> requirements = new();

        for (int index = 0; source != null && index < source.Count; index++)
        {
            ShopTradeRequirement requirement = source[index];
            requirements.Add(new PlayerRunShopPaymentRequirementSaveData
            {
                // Preserve malformed authoring as an invalid saved row. The
                // restore path then stops safely instead of changing the offer
                // into a free or legacy-priced product after an app restart.
                itemGuid = requirement?.Item != null ? requirement.Item.Guid : string.Empty,
                amount = requirement != null ? requirement.Amount : 1
            });
        }

        return requirements;
    }

    private bool TryRestoreOffers(
        IReadOnlyList<PlayerRunShopOfferSaveData> source,
        RuntimeContentCatalogSO catalog,
        List<PlayerShopOffer> target,
        out string message)
    {
        for (int index = 0; source != null && index < source.Count; index++)
        {
            PlayerRunShopOfferSaveData saved = source[index];
            if (saved == null || !catalog.TryResolveItem(saved.itemGuid, out ItemSO item))
            {
                message = $"Shop offer ItemSO cannot be resolved. guid={saved?.itemGuid}";
                return false;
            }

            saved.EnsureDefaults();
            if (!TryRestorePaymentRequirements(saved.paymentRequirements, catalog, out List<ShopTradeRequirement> requirements, out message))
                return false;

            // Saved offers from before physical shop payment may have no rows.
            // Reconstruct them as a real MoneyItem trade instead of reviving the
            // retired numerical-currency purchase path.
            if (requirements.Count == 0 && _activeConfig != null)
                requirements = new List<ShopTradeRequirement>(_activeConfig.CreateDefaultCurrencyPayment(saved.price));

            if (requirements.Count == 0)
            {
                message = $"Saved shop offer has no valid physical payment. item={item.name}";
                return false;
            }

            PlayerShopOffer offer = new PlayerShopOffer(
                item,
                (TreasureRewardKind)saved.kind,
                (PlayerShopItemRarity)saved.rarity,
                saved.price,
                saved.initialStock,
                saved.bundleAmount,
                saved.unlimitedStock,
                saved.canReroll,
                requirements);
            offer.RestoreRemainingStock(saved.remainingStock);
            target.Add(offer);
        }

        message = string.Empty;
        return true;
    }

    private static bool TryRestorePaymentRequirements(
        IReadOnlyList<PlayerRunShopPaymentRequirementSaveData> source,
        RuntimeContentCatalogSO catalog,
        out List<ShopTradeRequirement> requirements,
        out string message)
    {
        requirements = new List<ShopTradeRequirement>();

        for (int index = 0; source != null && index < source.Count; index++)
        {
            PlayerRunShopPaymentRequirementSaveData saved = source[index];
            if (saved == null || string.IsNullOrWhiteSpace(saved.itemGuid) ||
                !catalog.TryResolveItem(saved.itemGuid, out ItemSO item))
            {
                message = $"Shop payment ItemSO cannot be resolved. guid={saved?.itemGuid}";
                return false;
            }

            saved.EnsureDefaults();
            requirements.Add(new ShopTradeRequirement(item, saved.amount));
        }

        message = string.Empty;
        return true;
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        _shopRunRuntime = ShopRunRuntime.EnsureRuntime();
        _shopRunRuntime.OnRunReset += HandleRunReset;
        _shopRunRuntime.TryApplyPendingState(this);

        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_shopRunRuntime != null)
            _shopRunRuntime.OnRunReset -= HandleRunReset;

        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;
    }

    // Purpose: Attempts to interact and reports whether the request succeeded.
    public bool TryInteract(Component actor)
    {
        ResolveRefs();

        if (!CanInteract)
            return false;

        if (!TryEnsureOffers())
            return false;

        if (_shopOpenRequestChannel == null || _overlayRequestChannel == null)
        {
            LogError(
                $"Cannot open Shop UI because an event channel is missing. " +
                $"shopRequestChannel={(_shopOpenRequestChannel != null)}, overlayRequestChannel={(_overlayRequestChannel != null)}");
            return false;
        }

        _shopOpenRequestChannel.RaiseEvent(
            new SectorShopOpenRequest(
                _bulletOffers,
                _itemOffers,
                _activeConfig,
                !_raidMerchant && _activeConfig != null && _activeConfig.AllowReroll, this));

        // Bind the offers first. PlayerPanelHub then prepares the sibling ShopHub.
        _overlayRequestChannel.Open(UIOverlayId.ShopHub);
        return true;
    }

    // Purpose: Handles the incoming sector state manager ready request or event.
    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager != null)
            _sectorStateManager = manager;
    }

    // Purpose: Handles the incoming run reset request or event.
    private void HandleRunReset(int _)
    {
        _offersRolled = false;
        _bulletOffers.Clear();
        _itemOffers.Clear();
        _restockRevision = 0;
        _nextRestockTime = -1f;
    }

    // Purpose: Attempts to ensure offers and reports whether the request succeeded.
    private bool TryEnsureOffers()
    {
        if (_offersRolled)
        {
            TryRestockIfDue();
            return true;
        }

        return RollOffers();
    }

    // Purpose: Attempts to restock if due and reports whether the request succeeded.
    private bool TryRestockIfDue()
    {
        if (_raidMerchant || _activeConfig == null ||
            !_activeConfig.HasTimedRestock ||
            _nextRestockTime < 0f ||
            Time.unscaledTime < _nextRestockTime)
        {
            return false;
        }

        _restockRevision++;
        return RollOffers();
    }

    // Purpose: Rolls the offers result from its configured probabilities.
    private bool RollOffers()
    {

        int stageIndex = _sectorStateManager != null
            ? _sectorStateManager.CurrentStage
            : 0;

        if (_activeConfig == null)
        {
            _bulletOffers.Clear();
            _itemOffers.Clear();
            _offersRolled = true;
            _nextRestockTime = -1f;
            LogError($"ShopConfigSO is missing. Opening an empty Home Shop UI. stage={stageIndex}");
            return true;
        }

        ShopUnlockRuntime unlockRuntime = _raidMerchant ? null : ShopUnlockRuntime.EnsureRuntime();
        if (unlockRuntime != null) unlockRuntime.Configure(_activeConfig);
        int seed = ResolveOfferSeed(
            stageIndex,
            ResolveSectorCoord(),
            _shopRunRuntime != null ? _shopRunRuntime.RunId : 0,
            _restockRevision);

        _bulletOffers.Clear();
        _itemOffers.Clear();

        _bulletOffers.AddRange(_activeConfig.CreateBulletOffers(
            seed,
            product => _raidMerchant ? product.AvailableByDefault : unlockRuntime.IsProductAvailable(_activeConfig, product)));
        _itemOffers.AddRange(_activeConfig.CreateItemOffers(
            seed + 1009,
            product => _raidMerchant ? product.AvailableByDefault : unlockRuntime.IsProductAvailable(_activeConfig, product),
            _activeConfig.ItemOfferCount));

        _offersRolled = true;
        _nextRestockTime = _activeConfig.HasTimedRestock
            ? Time.unscaledTime + _activeConfig.RestockIntervalSeconds
            : -1f;

        if (_bulletOffers.Count <= 0 && _itemOffers.Count <= 0)
        {
            LogWarning(
                $"No Shop offer could be rolled. Opening the Shop UI with empty slots. " +
                $"stage={stageIndex}, config={_activeConfig.name}");
        }

        return true;
    }

    // Purpose: Checks whether shop room is currently true.
    private bool IsShopRoom()
    {
        ResolveRefs();

        if (_sector == null)
            return false;

        if (_raidMerchant) return RaidRuntime.Active != null && RaidRuntime.Active.IsInSector(_sector) && _sector.IsCleared;

        return _allowStartSectorHub && _sector.IsStartSector;
    }

    // Purpose: Resolves the sector coord needed by this component.
    private Vector2Int ResolveSectorCoord()
    {
        if (_sectorStateManager != null &&
            _sectorStateManager.TryGetSectorCoord(_sector, out Vector2Int coord))
        {
            return coord;
        }

        return _sector != null ? _sector.Coord : default;
    }

    // Purpose: Resolves the offer seed needed by this component.
    private int ResolveOfferSeed(
        int stageIndex,
        Vector2Int coord,
        int runId,
        int restockRevision)
    {
        int stageSeed = stageIndex;
        if (_raidMerchant) stageSeed = _raidSeed;

        if (!_raidMerchant && _sectorStateManager != null &&
            _sectorStateManager.CurrentStageMapLayout != null)
        {
            stageSeed = _sectorStateManager.CurrentStageMapLayout.stageSeed;
        }

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + stageSeed;
            hash = hash * 31 + stageIndex;
            hash = hash * 31 + coord.x;
            hash = hash * 31 + coord.y;
            hash = hash * 31 + runId;
            hash = hash * 31 + restockRevision;
            hash = hash * 31 + _offerSeedSalt;
            return hash;
        }
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_sector == null)
            _sector = GetComponentInParent<SectorRuntime>();

        if (_sectorStateManager == null &&
            _sectorStateManagerReadyChannel != null &&
            _sectorStateManagerReadyChannel.HasCurrent)
        {
            _sectorStateManager = _sectorStateManagerReadyChannel.Current;
        }

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();
    }

    // Purpose: Performs the log warning workflow for this component.
    private void LogWarning(string message)
    {
        if (!_logWarnings)
            return;

        Debug.LogWarning($"[SectorShop] {message}", this);
    }

    // Purpose: Performs the log error workflow for this component.
    private void LogError(string message)
    {
        Debug.LogError($"[SectorShop] {message}", this);
    }
}
