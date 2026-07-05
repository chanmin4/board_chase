// ShopRoomDropTableSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShopRoomItemKind
{
    Passive = 0,
    Armor = 1,
    Weapon = 2
}

[CreateAssetMenu(
    fileName = "ShopRoomDropTable",
    menuName = "Game/Shop Room Drop Table")]
public class ShopRoomDropTableSO : ScriptableObject
{
    [Serializable]
    public struct RarityWeight
    {
        public PlayerShopItemRarity rarity;
        [Min(0f)] public float weight;
    }

    [Serializable]
    public struct ItemKindWeight
    {
        public ShopRoomItemKind kind;
        [Min(0f)] public float weight;
    }

    [Serializable]
    public sealed class BulletShopEntry
    {
        public BulletSO bullet;

        [Min(0f)]
        public float weight = 1f;

        [Min(1)]
        public int stock = 1;

        [Min(1)]
        public int bulletBundleAmount = 20;
    }

    [Serializable]
    public sealed class ItemShopEntry
    {
        [Tooltip("PassiveItemSO, ArmorItemSO, or WeaponSO sold by this shop.")]
        public ItemSO item;

        [Min(0f)]
        public float weight = 1f;

        [Min(1)]
        public int stock = 1;
    }

    [Header("Shop")]
    [SerializeField, Min(0)] private int _bulletOfferCount = 2;
    [SerializeField, Min(0)] private int _itemOfferCount = 3;
    [SerializeField, Min(0)] private int _rerollCost = 0;
    [SerializeField, Range(0f, 1f)] private float _sellPriceRate = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _armorRepairPriceRate = 0.5f;

    [Header("Duplicate")]
    [SerializeField] private bool _allowDuplicateBulletOffers = false;
    [SerializeField] private bool _allowDuplicateItemOffers = false;

    [Header("Item Kind Roll")]
    [SerializeField] private ItemKindWeight[] _itemKindWeights =
    {
        new ItemKindWeight { kind = ShopRoomItemKind.Passive, weight = 40f },
        new ItemKindWeight { kind = ShopRoomItemKind.Weapon, weight = 35f },
        new ItemKindWeight { kind = ShopRoomItemKind.Armor, weight = 25f }
    };

    [Header("Rarity Roll")]
    [SerializeField] private RarityWeight[] _rarityWeights =
    {
        new RarityWeight { rarity = PlayerShopItemRarity.Normal, weight = 65f },
        new RarityWeight { rarity = PlayerShopItemRarity.Rare, weight = 25f },
        new RarityWeight { rarity = PlayerShopItemRarity.Unique, weight = 8f },
        new RarityWeight { rarity = PlayerShopItemRarity.Legendary, weight = 2f }
    };

    [Header("Bullet Pools")]
    [SerializeField] private BulletShopEntry[] _normalBulletPool;
    [SerializeField] private BulletShopEntry[] _rareBulletPool;
    [SerializeField] private BulletShopEntry[] _uniqueBulletPool;
    [SerializeField] private BulletShopEntry[] _legendaryBulletPool;

    [Header("Item Pools")]
    [SerializeField] private ItemShopEntry[] _normalItemPool;
    [SerializeField] private ItemShopEntry[] _rareItemPool;
    [SerializeField] private ItemShopEntry[] _uniqueItemPool;
    [SerializeField] private ItemShopEntry[] _legendaryItemPool;

    public int BulletOfferCount => Mathf.Max(0, _bulletOfferCount);
    public int ItemOfferCount => Mathf.Max(0, _itemOfferCount);
    public bool AllowDuplicateBulletOffers => _allowDuplicateBulletOffers;
    public bool AllowDuplicateItemOffers => _allowDuplicateItemOffers;
    public int RerollCost => Mathf.Max(0, _rerollCost);
    public float SellPriceRate => _sellPriceRate;
    public float ArmorRepairPriceRate => _armorRepairPriceRate;

    public List<PlayerShopOffer> CreateBulletOffers(int seed, int offerCountOverride = -1)
    {
        int count = offerCountOverride >= 0 ? offerCountOverride : BulletOfferCount;
        List<PlayerShopOffer> offers = new();
        List<ItemSO> excluded = new();
        System.Random rng = new(seed);

        for (int i = 0; i < count; i++)
        {
            IReadOnlyList<ItemSO> excludedForRoll =
                _allowDuplicateBulletOffers ? null : excluded;

            if (!TryCreateBulletOffer(rng, excludedForRoll, out PlayerShopOffer offer))
                break;

            offers.Add(offer);

            if (!_allowDuplicateBulletOffers && offer.Item != null)
                excluded.Add(offer.Item);
        }

        return offers;
    }

    public List<PlayerShopOffer> CreateItemOffers(int seed, int offerCountOverride = -1)
    {
        int count = offerCountOverride >= 0 ? offerCountOverride : ItemOfferCount;
        List<PlayerShopOffer> offers = new();
        List<ItemSO> excluded = new();
        System.Random rng = new(seed);

        for (int i = 0; i < count; i++)
        {
            IReadOnlyList<ItemSO> excludedForRoll =
                _allowDuplicateItemOffers ? null : excluded;

            if (!TryCreateItemOffer(rng, excludedForRoll, out PlayerShopOffer offer))
                break;

            offers.Add(offer);

            if (!_allowDuplicateItemOffers && offer.Item != null)
                excluded.Add(offer.Item);
        }

        return offers;
    }

    public List<PlayerShopOffer> CreateOffers(int seed, int offerCountOverride = -1)
    {
        List<PlayerShopOffer> offers = new();
        offers.AddRange(CreateBulletOffers(seed, offerCountOverride < 0 ? -1 : offerCountOverride));
        offers.AddRange(CreateItemOffers(seed + 1009, offerCountOverride < 0 ? -1 : offerCountOverride));
        return offers;
    }

    public bool TryCreateBulletOffer(int seed, IReadOnlyList<ItemSO> excluded, out PlayerShopOffer offer)
    {
        return TryCreateBulletOffer(new System.Random(seed), excluded, out offer);
    }

    public bool TryCreateItemOffer(int seed, IReadOnlyList<ItemSO> excluded, out PlayerShopOffer offer)
    {
        return TryCreateItemOffer(new System.Random(seed), excluded, out offer);
    }

    public bool TryCreateBulletOffer(
        System.Random rng,
        IReadOnlyList<ItemSO> excluded,
        out PlayerShopOffer offer)
    {
        offer = null;
        rng ??= new System.Random();

        for (int attempt = 0; attempt < 16; attempt++)
        {
            PlayerShopItemRarity rarity = RollRarity(rng);

            if (TryPickBulletEntry(rarity, excluded, rng, out BulletShopEntry entry))
            {
                offer = CreateBulletOffer(entry);
                return offer != null && offer.IsValid;
            }
        }

        if (TryPickBulletEntry(null, excluded, rng, out BulletShopEntry fallback))
        {
            offer = CreateBulletOffer(fallback);
            return offer != null && offer.IsValid;
        }

        return false;
    }

    public bool TryCreateItemOffer(
        System.Random rng,
        IReadOnlyList<ItemSO> excluded,
        out PlayerShopOffer offer)
    {
        offer = null;
        rng ??= new System.Random();

        for (int attempt = 0; attempt < 16; attempt++)
        {
            ShopRoomItemKind kind = RollItemKind(rng);
            PlayerShopItemRarity rarity = RollRarity(rng);

            if (TryPickItemEntry(kind, rarity, excluded, rng, out ItemShopEntry entry))
            {
                offer = CreateItemOffer(entry);
                return offer != null && offer.IsValid;
            }
        }

        if (TryPickItemEntry(null, null, excluded, rng, out ItemShopEntry fallback))
        {
            offer = CreateItemOffer(fallback);
            return offer != null && offer.IsValid;
        }

        return false;
    }

    private PlayerShopOffer CreateBulletOffer(BulletShopEntry entry)
    {
        if (entry == null || entry.bullet == null)
            return null;

        return new PlayerShopOffer(
            entry.bullet,
            TreasureRewardKind.Bullet,
            entry.bullet.Rarity,
            entry.bullet.PurchasePrice,
            Mathf.Max(1, entry.stock),
            Mathf.Max(1, entry.bulletBundleAmount));
    }

    private PlayerShopOffer CreateItemOffer(ItemShopEntry entry)
    {
        if (entry == null || entry.item == null)
            return null;

        TreasureRewardKind kind = ResolveTreasureKind(entry.item);

        return new PlayerShopOffer(
            entry.item,
            kind,
            entry.item.Rarity,
            entry.item.PurchasePrice,
            Mathf.Max(1, entry.stock),
            1);
    }

    private bool TryPickBulletEntry(
        PlayerShopItemRarity? rarity,
        IReadOnlyList<ItemSO> excluded,
        System.Random rng,
        out BulletShopEntry entry)
    {
        entry = null;
        BulletShopEntry[] pool = BuildMergedBulletPool();

        if (pool == null || pool.Length <= 0)
            return false;

        float totalWeight = 0f;

        for (int i = 0; i < pool.Length; i++)
        {
            if (IsBulletCandidateValid(pool[i], rarity, excluded))
                totalWeight += Mathf.Max(0f, pool[i].weight);
        }

        if (totalWeight <= 0f)
            return false;

        float roll = (float)(rng.NextDouble() * totalWeight);
        float cursor = 0f;

        for (int i = 0; i < pool.Length; i++)
        {
            BulletShopEntry candidate = pool[i];

            if (!IsBulletCandidateValid(candidate, rarity, excluded))
                continue;

            cursor += Mathf.Max(0f, candidate.weight);

            if (roll <= cursor)
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryPickItemEntry(
        ShopRoomItemKind? kind,
        PlayerShopItemRarity? rarity,
        IReadOnlyList<ItemSO> excluded,
        System.Random rng,
        out ItemShopEntry entry)
    {
        entry = null;
        ItemShopEntry[] pool = BuildMergedItemPool();

        if (pool == null || pool.Length <= 0)
            return false;

        float totalWeight = 0f;

        for (int i = 0; i < pool.Length; i++)
        {
            if (IsItemCandidateValid(pool[i], kind, rarity, excluded))
                totalWeight += Mathf.Max(0f, pool[i].weight);
        }

        if (totalWeight <= 0f)
            return false;

        float roll = (float)(rng.NextDouble() * totalWeight);
        float cursor = 0f;

        for (int i = 0; i < pool.Length; i++)
        {
            ItemShopEntry candidate = pool[i];

            if (!IsItemCandidateValid(candidate, kind, rarity, excluded))
                continue;

            cursor += Mathf.Max(0f, candidate.weight);

            if (roll <= cursor)
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }

    private BulletShopEntry[] BuildMergedBulletPool()
    {
        List<BulletShopEntry> merged = new();
        AddBulletPool(merged, _normalBulletPool);
        AddBulletPool(merged, _rareBulletPool);
        AddBulletPool(merged, _uniqueBulletPool);
        AddBulletPool(merged, _legendaryBulletPool);
        return merged.ToArray();
    }

    private ItemShopEntry[] BuildMergedItemPool()
    {
        List<ItemShopEntry> merged = new();
        AddItemPool(merged, _normalItemPool);
        AddItemPool(merged, _rareItemPool);
        AddItemPool(merged, _uniqueItemPool);
        AddItemPool(merged, _legendaryItemPool);
        return merged.ToArray();
    }

    private static void AddBulletPool(List<BulletShopEntry> target, BulletShopEntry[] source)
    {
        if (target == null || source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
                target.Add(source[i]);
        }
    }

    private static void AddItemPool(List<ItemShopEntry> target, ItemShopEntry[] source)
    {
        if (target == null || source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
                target.Add(source[i]);
        }
    }

    private PlayerShopItemRarity RollRarity(System.Random rng)
    {
        float totalWeight = 0f;

        if (_rarityWeights != null)
        {
            for (int i = 0; i < _rarityWeights.Length; i++)
                totalWeight += Mathf.Max(0f, _rarityWeights[i].weight);
        }

        if (totalWeight <= 0f)
            return PlayerShopItemRarity.Normal;

        float roll = (float)(rng.NextDouble() * totalWeight);
        float cursor = 0f;

        for (int i = 0; i < _rarityWeights.Length; i++)
        {
            cursor += Mathf.Max(0f, _rarityWeights[i].weight);

            if (roll <= cursor)
                return _rarityWeights[i].rarity;
        }

        return PlayerShopItemRarity.Normal;
    }

    private ShopRoomItemKind RollItemKind(System.Random rng)
    {
        float totalWeight = 0f;

        if (_itemKindWeights != null)
        {
            for (int i = 0; i < _itemKindWeights.Length; i++)
                totalWeight += Mathf.Max(0f, _itemKindWeights[i].weight);
        }

        if (totalWeight <= 0f)
            return ShopRoomItemKind.Passive;

        float roll = (float)(rng.NextDouble() * totalWeight);
        float cursor = 0f;

        for (int i = 0; i < _itemKindWeights.Length; i++)
        {
            cursor += Mathf.Max(0f, _itemKindWeights[i].weight);

            if (roll <= cursor)
                return _itemKindWeights[i].kind;
        }

        return ShopRoomItemKind.Passive;
    }

    private static bool IsBulletCandidateValid(
        BulletShopEntry candidate,
        PlayerShopItemRarity? rarity,
        IReadOnlyList<ItemSO> excluded)
    {
        if (candidate == null || candidate.bullet == null)
            return false;

        if (rarity.HasValue && candidate.bullet.Rarity != rarity.Value)
            return false;

        return !IsExcluded(candidate.bullet, excluded);
    }

    private static bool IsItemCandidateValid(
        ItemShopEntry candidate,
        ShopRoomItemKind? kind,
        PlayerShopItemRarity? rarity,
        IReadOnlyList<ItemSO> excluded)
    {
        if (candidate == null || candidate.item == null)
            return false;

        if (!IsSupportedItem(candidate.item))
            return false;

        if (kind.HasValue && ResolveItemKind(candidate.item) != kind.Value)
            return false;

        if (rarity.HasValue && candidate.item.Rarity != rarity.Value)
            return false;

        return !IsExcluded(candidate.item, excluded);
    }

    private static bool IsSupportedItem(ItemSO item)
    {
        return item is PassiveItemSO ||
               item is ArmorItemSO ||
               item is WeaponSO;
    }

    private static bool IsExcluded(ItemSO item, IReadOnlyList<ItemSO> excluded)
    {
        if (item == null || excluded == null)
            return false;

        for (int i = 0; i < excluded.Count; i++)
        {
            if (excluded[i] == item)
                return true;
        }

        return false;
    }

    private static ShopRoomItemKind ResolveItemKind(ItemSO item)
    {
        if (item is ArmorItemSO)
            return ShopRoomItemKind.Armor;

        if (item is WeaponSO)
            return ShopRoomItemKind.Weapon;

        return ShopRoomItemKind.Passive;
    }

    private static TreasureRewardKind ResolveTreasureKind(ItemSO item)
    {
        if (item is ArmorItemSO)
            return TreasureRewardKind.Armor;

        return TreasureRewardKind.Passive;
    }
}