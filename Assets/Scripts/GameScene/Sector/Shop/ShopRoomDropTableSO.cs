using System;
using System.Collections.Generic;
using UnityEngine;

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
    public struct RewardKindWeight
    {
        public TreasureRewardKind kind;
        [Min(0f)] public float weight;
    }

    [Serializable]
    public sealed class ShopItemEntry
    {
        [Tooltip("PassiveItemSO, BulletSO, or ArmorItemSO sold by this shop.")]
        public ItemSO item;

        [Min(0f)]
        [Tooltip("Weight inside its rarity/kind pool.")]
        public float weight = 1f;

        [Min(0)]
        [Tooltip("Purchase price.")]
        public int price = 10;

        [Min(1)]
        [Tooltip("How many times this offer can be bought.")]
        public int stock = 1;

        [Min(1)]
        [Tooltip("Bullet bundle amount. Ignored for passive items.")]
        public int bulletBundleAmount = 20;

    }

    [Header("Shop")]
    [SerializeField, Min(1)] private int _offerCount = 3;
    [SerializeField] private bool _allowDuplicateOffers = false;
    [SerializeField, Min(0)] private int _rerollCost = 0;
    [SerializeField, Range(0f, 1f)] private float _sellPriceRate = 0.5f;

    [Header("Kind Roll")]
    [SerializeField] private RewardKindWeight[] _rewardKindWeights =
    {
        new RewardKindWeight { kind = TreasureRewardKind.Passive, weight = 50f },
        new RewardKindWeight { kind = TreasureRewardKind.Bullet, weight = 50f },
        new RewardKindWeight { kind = TreasureRewardKind.Armor, weight = 0f }
    };

    [Header("Rarity Roll")]
    [SerializeField] private RarityWeight[] _rarityWeights =
    {
        new RarityWeight { rarity = PlayerShopItemRarity.Normal, weight = 65f },
        new RarityWeight { rarity = PlayerShopItemRarity.Rare, weight = 25f },
        new RarityWeight { rarity = PlayerShopItemRarity.Unique, weight = 8f },
        new RarityWeight { rarity = PlayerShopItemRarity.Legendary, weight = 2f }
    };

    [Header("Item Pools")]
    [SerializeField] private ShopItemEntry[] _normalPool;
    [SerializeField] private ShopItemEntry[] _rarePool;
    [SerializeField] private ShopItemEntry[] _uniquePool;
    [SerializeField] private ShopItemEntry[] _legendaryPool;

    public int OfferCount => Mathf.Max(1, _offerCount);
    public bool AllowDuplicateOffers => _allowDuplicateOffers;
    public int RerollCost => Mathf.Max(0, _rerollCost);
    public float SellPriceRate => _sellPriceRate;

    public List<PlayerShopOffer> CreateOffers(int seed, int offerCountOverride = 0)
    {
        int count = offerCountOverride > 0 ? offerCountOverride : OfferCount;
        List<PlayerShopOffer> offers = new();
        List<ItemSO> excluded = new();
        System.Random rng = new System.Random(seed);

        for (int i = 0; i < count; i++)
        {
            IReadOnlyList<ItemSO> excludedForRoll =
                _allowDuplicateOffers ? null : excluded;

            if (!TryCreateOffer(rng, excludedForRoll, out PlayerShopOffer offer))
                break;

            offers.Add(offer);

            if (!_allowDuplicateOffers && offer.Item != null)
                excluded.Add(offer.Item);
        }

        return offers;
    }

    public bool TryCreateOffer(int seed, IReadOnlyList<ItemSO> excluded, out PlayerShopOffer offer)
    {
        return TryCreateOffer(new System.Random(seed), excluded, out offer);
    }

    public bool TryCreateOffer(System.Random rng, IReadOnlyList<ItemSO> excluded, out PlayerShopOffer offer)
    {
        offer = null;
        rng ??= new System.Random();

        for (int attempt = 0; attempt < 16; attempt++)
        {
            TreasureRewardKind kind = RollRewardKind(rng);
            PlayerShopItemRarity rarity = RollRarity(rng);

            if (TryPickEntry(kind, rarity, excluded, rng, out ShopItemEntry entry))
            {
                offer = CreateOffer(entry, kind);
                return offer != null && offer.IsValid;
            }
        }

        if (TryPickEntry(null, null, excluded, rng, out ShopItemEntry fallback))
        {
            offer = CreateOffer(fallback, ResolveKind(fallback.item));
            return offer != null && offer.IsValid;
        }

        return false;
    }

    private PlayerShopOffer CreateOffer(ShopItemEntry entry, TreasureRewardKind kind)
    {
        if (entry == null || entry.item == null)
            return null;

        return new PlayerShopOffer(
            entry.item,
            kind,
            entry.item.Rarity,
            Mathf.Max(0, entry.price),
            Mathf.Max(1, entry.stock),
            Mathf.Max(1, entry.bulletBundleAmount)
        );
    }

    private bool TryPickEntry(
        TreasureRewardKind? kind,
        PlayerShopItemRarity? rarity,
        IReadOnlyList<ItemSO> excluded,
        System.Random rng,
        out ShopItemEntry entry)
    {
        entry = null;

        ShopItemEntry[] pool = BuildMergedPool();

        if (pool == null || pool.Length <= 0)
            return false;

        float totalWeight = 0f;

        for (int i = 0; i < pool.Length; i++)
        {
            if (IsCandidateValid(pool[i], kind, rarity, excluded))
                totalWeight += Mathf.Max(0f, pool[i].weight);
        }

        if (totalWeight <= 0f)
            return false;

        float roll = (float)(rng.NextDouble() * totalWeight);
        float cursor = 0f;

        for (int i = 0; i < pool.Length; i++)
        {
            ShopItemEntry candidate = pool[i];

            if (!IsCandidateValid(candidate, kind, rarity, excluded))
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

    private ShopItemEntry[] BuildMergedPool()
    {
        List<ShopItemEntry> merged = new();
        AddPool(merged, _normalPool);
        AddPool(merged, _rarePool);
        AddPool(merged, _uniquePool);
        AddPool(merged, _legendaryPool);
        return merged.ToArray();
    }

    private static void AddPool(List<ShopItemEntry> target, ShopItemEntry[] source)
    {
        if (target == null || source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
                target.Add(source[i]);
        }
    }

    private TreasureRewardKind RollRewardKind(System.Random rng)
    {
        float totalWeight = 0f;

        if (_rewardKindWeights != null)
        {
            for (int i = 0; i < _rewardKindWeights.Length; i++)
                totalWeight += Mathf.Max(0f, _rewardKindWeights[i].weight);
        }

        if (totalWeight <= 0f)
            return TreasureRewardKind.Passive;

        float roll = (float)(rng.NextDouble() * totalWeight);
        float cursor = 0f;

        for (int i = 0; i < _rewardKindWeights.Length; i++)
        {
            cursor += Mathf.Max(0f, _rewardKindWeights[i].weight);

            if (roll <= cursor)
                return _rewardKindWeights[i].kind;
        }

        return TreasureRewardKind.Passive;
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

    private static bool IsCandidateValid(
        ShopItemEntry candidate,
        TreasureRewardKind? kind,
        PlayerShopItemRarity? rarity,
        IReadOnlyList<ItemSO> excluded)
    {
        if (candidate == null || candidate.item == null)
            return false;

        if (kind.HasValue && ResolveKind(candidate.item) != kind.Value)
            return false;

        if (rarity.HasValue && candidate.item.Rarity != rarity.Value)
            return false;

        if (excluded != null)
        {
            for (int i = 0; i < excluded.Count; i++)
            {
                if (excluded[i] == candidate.item)
                    return false;
            }
        }

        return candidate.item is PassiveItemSO ||
               candidate.item is BulletSO ||
               candidate.item is ArmorItemSO;
    }

    private static TreasureRewardKind ResolveKind(ItemSO item)
    {
        if (item is BulletSO)
            return TreasureRewardKind.Bullet;

        if (item is ArmorItemSO)
            return TreasureRewardKind.Armor;

        return TreasureRewardKind.Passive;
    }
}
