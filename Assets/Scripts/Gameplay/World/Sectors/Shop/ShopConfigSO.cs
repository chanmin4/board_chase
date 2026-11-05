/*
 * Role hierarchy: Gameplay > World > Sectors > Shop
 * Primary responsibility: Owns the single Home-shop catalog, offer rules, and progression unlock nodes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopConfig", menuName = "Game/Shop/Shop Config")]
public sealed class ShopConfigSO : ScriptableObject
{
    [Serializable]
    public sealed class ProductDefinition
    {
        [SerializeField] private string _productId = "shop_product";
        [SerializeField] private ItemSO _item;
        [SerializeField] private bool _availableByDefault;
        [Min(0f)] [SerializeField] private float _offerWeight = 1f;
        [Min(1)] [SerializeField] private int _stock = 1;
        [Min(1)] [SerializeField] private int _bundleAmount = 1;
        [Tooltip("Default-currency item count when this product has no explicit barter rows. -1 uses the ItemSO purchase price. This is never spent as a numerical runtime currency.")]
        [SerializeField] private int _priceOverride = -1;

        [Header("Physical Payment / Barter")]
        [Tooltip("Add one or more ItemSO requirements to make this product use a physical item trade. Leave empty only while this product still uses the temporary legacy numerical price.")]
        [SerializeField] private List<ShopTradeRequirement> _tradeRequirements = new List<ShopTradeRequirement>();

        public string ProductId => _productId;
        public ItemSO Item => _item;
        public bool AvailableByDefault => _availableByDefault;
        public float OfferWeight => Mathf.Max(0f, _offerWeight);
        public int Stock => Mathf.Max(1, _stock);
        public int BundleAmount => Mathf.Max(1, _bundleAmount);
        public bool IsBullet => _item is BulletSO;
        public bool IsValid => _item != null && _offerWeight > 0f && !(_item is InfectionCureItemSO medicine && medicine.IsSyringe);
        public IReadOnlyList<ShopTradeRequirement> TradeRequirements => _tradeRequirements;
        public bool UsesPhysicalPayment => _tradeRequirements != null && _tradeRequirements.Count > 0;

        /// <summary>
        /// Reports whether every authored physical payment row is usable. The
        /// runtime purchase transaction will reject malformed rows instead of
        /// silently falling back to the legacy number price.
        /// </summary>
        public bool HasValidPhysicalPayment
        {
            get
            {
                if (!UsesPhysicalPayment)
                    return false;

                for (int index = 0; index < _tradeRequirements.Count; index++)
                {
                    if (_tradeRequirements[index] == null || !_tradeRequirements[index].IsValid)
                        return false;
                }

                return true;
            }
        }

        // Purpose: Resolves the price needed by this component.
        public int ResolvePrice()
        {
            return _priceOverride >= 0
                ? _priceOverride
                : _item != null ? Mathf.Max(0, _item.PurchasePrice) : 0;
        }
    }

    [Serializable]
    public sealed class UnlockNode
    {
        [SerializeField] private string _nodeId = "shop_unlock";
        [SerializeField] private string _displayName = "Shop Unlock";
        [TextArea] [SerializeField] private string _description;
        [Tooltip("All listed unlock nodes must already be unlocked.")]
        [SerializeField] private List<string> _prerequisiteNodeIds = new List<string>();
        [Tooltip("Product IDs made eligible for Home-shop rolls by this node.")]
        [SerializeField] private List<string> _unlockedProductIds = new List<string>();

        public string NodeId => _nodeId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _nodeId : _displayName;
        public string Description => _description;
        public IReadOnlyList<string> PrerequisiteNodeIds => _prerequisiteNodeIds;
        public IReadOnlyList<string> UnlockedProductIds => _unlockedProductIds;
    }

    [Header("Identity")]
    [SerializeField] private string _configId = "home_shop";

    [Header("Home Shop Offer Counts")]
    [Min(0)] [SerializeField] private int _bulletOfferCount = 2;
    [Min(0)] [SerializeField] private int _itemOfferCount = 3;
    [SerializeField] private bool _allowDuplicateBulletOffers;
    [SerializeField] private bool _allowDuplicateItemOffers;

    [Header("Home Shop Restock")]
    [Tooltip("Real-time seconds between full Home-shop offer restocks. Zero disables timed restocking.")]
    [SerializeField, Min(0f)] private float _restockIntervalSeconds = 120f;

    [Header("Home Shop Economy")]
    [Tooltip("Physical Money Item used by catalog products that do not define their own explicit barter rows. Assign MoneyItem here so every offer uses the item-payment window.")]
    [SerializeField] private CurrencyItemSO _defaultCurrencyItem;
    [Min(0)] [SerializeField] private int _rerollCost;
    [Range(0f, 1f)] [SerializeField] private float _sellPriceRate = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float _armorRepairPriceRate = 0.5f;
    [SerializeField] private bool _allowReroll = true;

    [Header("Catalog")]
    [SerializeField] private List<ProductDefinition> _products = new List<ProductDefinition>();

    [Header("Progression Unlock Tree")]
    [SerializeField] private List<UnlockNode> _unlockNodes = new List<UnlockNode>();

    public string ConfigId => string.IsNullOrWhiteSpace(_configId) ? name : _configId;
    public int BulletOfferCount => Mathf.Max(0, _bulletOfferCount);
    public int ItemOfferCount => Mathf.Max(0, _itemOfferCount);
    public bool AllowDuplicateBulletOffers => _allowDuplicateBulletOffers;
    public bool AllowDuplicateItemOffers => _allowDuplicateItemOffers;
    public float RestockIntervalSeconds => Mathf.Max(0f, _restockIntervalSeconds);
    public bool HasTimedRestock => RestockIntervalSeconds > 0.01f;
    public int RerollCost => Mathf.Max(0, _rerollCost);
    public CurrencyItemSO DefaultCurrencyItem => _defaultCurrencyItem;
    public float SellPriceRate => Mathf.Clamp01(_sellPriceRate);
    public float ArmorRepairPriceRate => Mathf.Clamp01(_armorRepairPriceRate);
    public bool AllowReroll => _allowReroll;
    public IReadOnlyList<ProductDefinition> Products => _products;
    public IReadOnlyList<UnlockNode> UnlockNodes => _unlockNodes;

    // Purpose: Creates the bullet offers owned by this workflow.
    public List<PlayerShopOffer> CreateBulletOffers(
        int seed,
        Func<ProductDefinition, bool> isProductAvailable,
        int offerCountOverride = -1)
    {
        int count = offerCountOverride >= 0 ? offerCountOverride : BulletOfferCount;
        return CreateOffers(new System.Random(seed), true, count, isProductAvailable);
    }

    // Purpose: Creates the item offers owned by this workflow.
    public List<PlayerShopOffer> CreateItemOffers(
        int seed,
        Func<ProductDefinition, bool> isProductAvailable,
        int offerCountOverride = -1)
    {
        int count = offerCountOverride >= 0 ? offerCountOverride : ItemOfferCount;
        return CreateOffers(new System.Random(seed), false, count, isProductAvailable);
    }

    // Purpose: Attempts to create bullet offer and reports whether the request succeeded.
    public bool TryCreateBulletOffer(
        int seed,
        IReadOnlyList<ItemSO> excluded,
        Func<ProductDefinition, bool> isProductAvailable,
        out PlayerShopOffer offer)
    {
        return TryCreateOffer(
            new System.Random(seed),
            true,
            excluded,
            isProductAvailable,
            out offer);
    }

    // Purpose: Attempts to create item offer and reports whether the request succeeded.
    public bool TryCreateItemOffer(
        int seed,
        IReadOnlyList<ItemSO> excluded,
        Func<ProductDefinition, bool> isProductAvailable,
        out PlayerShopOffer offer)
    {
        return TryCreateOffer(
            new System.Random(seed),
            false,
            excluded,
            isProductAvailable,
            out offer);
    }

    // Purpose: Attempts to get node and reports whether the request succeeded.
    public bool TryGetNode(string nodeId, out UnlockNode node)
    {
        if (_unlockNodes != null)
        {
            for (int i = 0; i < _unlockNodes.Count; i++)
            {
                UnlockNode candidate = _unlockNodes[i];

                if (candidate != null && candidate.NodeId == nodeId)
                {
                    node = candidate;
                    return true;
                }
            }
        }

        node = null;
        return false;
    }

    // Purpose: Creates the offers owned by this workflow.
    private List<PlayerShopOffer> CreateOffers(
        System.Random rng,
        bool bullets,
        int count,
        Func<ProductDefinition, bool> isProductAvailable)
    {
        List<PlayerShopOffer> offers = new List<PlayerShopOffer>();
        List<ItemSO> excluded = new List<ItemSO>();
        bool allowDuplicates = bullets
            ? _allowDuplicateBulletOffers
            : _allowDuplicateItemOffers;

        for (int i = 0; i < count; i++)
        {
            IReadOnlyList<ItemSO> excludedForRoll = allowDuplicates ? null : excluded;

            if (!TryCreateOffer(rng, bullets, excludedForRoll, isProductAvailable, out PlayerShopOffer offer))
                break;

            offers.Add(offer);

            if (!allowDuplicates && offer.Item != null)
                excluded.Add(offer.Item);
        }

        return offers;
    }

    // Purpose: Attempts to create offer and reports whether the request succeeded.
    private bool TryCreateOffer(
        System.Random rng,
        bool bullets,
        IReadOnlyList<ItemSO> excluded,
        Func<ProductDefinition, bool> isProductAvailable,
        out PlayerShopOffer offer)
    {
        float totalWeight = 0f;

        if (_products != null)
        {
            for (int i = 0; i < _products.Count; i++)
            {
                ProductDefinition product = _products[i];

                if (IsCandidateValid(product, bullets, excluded, isProductAvailable))
                    totalWeight += product.OfferWeight;
            }
        }

        if (totalWeight <= 0f)
        {
            offer = null;
            return false;
        }

        float roll = (float)(rng.NextDouble() * totalWeight);
        float cursor = 0f;

        for (int i = 0; i < _products.Count; i++)
        {
            ProductDefinition product = _products[i];

            if (!IsCandidateValid(product, bullets, excluded, isProductAvailable))
                continue;

            cursor += product.OfferWeight;

            if (roll > cursor)
                continue;

            offer = CreateOffer(product);
            return offer != null;
        }

        offer = null;
        return false;
    }

    // Purpose: Checks whether candidate valid is currently true.
    private bool IsCandidateValid(
        ProductDefinition product,
        bool bullets,
        IReadOnlyList<ItemSO> excluded,
        Func<ProductDefinition, bool> isProductAvailable)
    {
        if (product == null || !product.IsValid || product.IsBullet != bullets)
            return false;

        if (isProductAvailable != null && !isProductAvailable(product))
            return false;

        if (!HasValidPhysicalPayment(product))
            return false;

        if (excluded == null)
            return true;

        for (int i = 0; i < excluded.Count; i++)
        {
            if (excluded[i] == product.Item)
                return false;
        }

        return true;
    }

    // Purpose: Resolves one product's explicit barter rows or its physical default-currency payment.
    public IReadOnlyList<ShopTradeRequirement> ResolveTradeRequirements(ProductDefinition product)
    {
        if (product == null)
            return Array.Empty<ShopTradeRequirement>();

        IReadOnlyList<ShopTradeRequirement> explicitRequirements = product.TradeRequirements;
        if (explicitRequirements != null && explicitRequirements.Count > 0)
            return explicitRequirements;

        return CreateDefaultCurrencyPayment(product.ResolvePrice());
    }

    // Purpose: Creates a single physical Money Item payment for generated or migrated shop offers.
    public IReadOnlyList<ShopTradeRequirement> CreateDefaultCurrencyPayment(int amount)
    {
        if (_defaultCurrencyItem == null || amount <= 0)
            return Array.Empty<ShopTradeRequirement>();

        return new[] { new ShopTradeRequirement(_defaultCurrencyItem, amount) };
    }

    // Purpose: Validates that a product will always open the physical item-payment flow.
    private bool HasValidPhysicalPayment(ProductDefinition product)
    {
        IReadOnlyList<ShopTradeRequirement> requirements = ResolveTradeRequirements(product);
        if (requirements == null || requirements.Count == 0)
            return false;

        for (int index = 0; index < requirements.Count; index++)
        {
            if (requirements[index] == null || !requirements[index].IsValid)
                return false;
        }

        return true;
    }

    // Purpose: Creates the offer owned by this workflow.
    private PlayerShopOffer CreateOffer(ProductDefinition product)
    {
        if (product == null || product.Item == null)
            return null;

        TreasureRewardKind kind = product.Item is BulletSO
            ? TreasureRewardKind.Bullet
            : product.Item is ArmorItemSO
                ? TreasureRewardKind.Armor
                : product.Item is CurrencyItemSO
                    ? TreasureRewardKind.Currency
                    : TreasureRewardKind.Passive;

        return new PlayerShopOffer(
            product.Item,
            kind,
            product.Item.Rarity,
            product.ResolvePrice(),
            product.Stock,
            product.IsBullet ? product.BundleAmount : 1,
            tradeRequirements: ResolveTradeRequirements(product));
    }
}
