using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerShopItemRarity
{
    Normal = 0,
    Rare = 1,
    Unique = 3,
    Legendary = 2
}

[CreateAssetMenu(
    fileName = "PlayerShopCatalog",
    menuName = "Game/Shop/Bullet Catalog")]
public class PlayerShopCatalogSO : ScriptableObject
{
    [Serializable]
    public struct RarityWeight
    {
        public PlayerShopItemRarity rarity;
        [Min(0f)] public float weight;

        public RarityWeight(PlayerShopItemRarity rarity, float weight)
        {
            this.rarity = rarity;
            this.weight = weight;
        }
    }

    [Serializable]
    public sealed class BulletShopEntry
    {
        [SerializeField] private BulletSO _bullet;
        [SerializeField, Min(0)] private int _price = 10;
        [SerializeField, Min(1)] private int _stock = 1;
        [SerializeField, Min(1)] private int _bundleAmount = 20;
        [SerializeField, TextArea] private string _description;

        public BulletSO Bullet => _bullet;
        public PlayerShopItemRarity Rarity =>
            _bullet != null ? _bullet.Rarity : PlayerShopItemRarity.Normal;
        public int Price => Mathf.Max(0, _price);
        public int Stock => Mathf.Max(1, _stock);
        public int BundleAmount => Mathf.Max(1, _bundleAmount);
        public string Description => _description;
        public bool IsValid => _bullet != null;
    }

    [Header("Shop")]
    [SerializeField, Min(1)] private int _offerCount = 3;
    [SerializeField, Min(0)] private int _startingDebugCurrency = 0;
    [SerializeField, Min(0)] private int _rerollCost = 0;
    [SerializeField] private bool _allowDuplicateOffers = false;

    [Header("Sell")]
    [SerializeField, Range(0f, 1f)] private float _sellPriceRate = 0.5f;

    [Header("Rarity Roll")]
    [SerializeField] private RarityWeight[] _rarityWeights =
    {
        new RarityWeight(PlayerShopItemRarity.Normal, 65f),
        new RarityWeight(PlayerShopItemRarity.Rare, 25f),
        new RarityWeight(PlayerShopItemRarity.Unique, 8f),
        new RarityWeight(PlayerShopItemRarity.Legendary, 2f),
    };

    [Header("Bullet Pool")]
    [SerializeField] private BulletShopEntry[] _entries;

    public int OfferCount => Mathf.Max(1, _offerCount);
    public int StartingDebugCurrency => Mathf.Max(0, _startingDebugCurrency);
    public int RerollCost => Mathf.Max(0, _rerollCost);
    public float SellPriceRate => _sellPriceRate;
    public bool AllowDuplicateOffers => _allowDuplicateOffers;

    public List<BulletShopEntry> CreateRandomEntries()
    {
        List<BulletShopEntry> result = new();
        List<BulletShopEntry> excluded = new();

        for (int i = 0; i < OfferCount; i++)
        {
            List<BulletShopEntry> excludedForRoll = _allowDuplicateOffers ? null : excluded;

            if (!TryCreateRandomEntry(excludedForRoll, out BulletShopEntry entry))
                break;

            result.Add(entry);

            if (!_allowDuplicateOffers)
                excluded.Add(entry);
        }

        return result;
    }

    public bool TryCreateRandomEntry(out BulletShopEntry entry)
    {
        return TryCreateRandomEntry(null, out entry);
    }

    public bool TryCreateRandomEntry(
        IReadOnlyList<BulletShopEntry> excluded,
        out BulletShopEntry entry)
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            PlayerShopItemRarity rarity = RollRarity();

            if (TryPickEntry(candidate => candidate.Rarity == rarity, excluded, out entry))
                return true;
        }

        return TryPickEntry(_ => true, excluded, out entry);
    }

    private PlayerShopItemRarity RollRarity()
    {
        float totalWeight = 0f;

        if (_rarityWeights != null)
        {
            for (int i = 0; i < _rarityWeights.Length; i++)
                totalWeight += Mathf.Max(0f, _rarityWeights[i].weight);
        }

        if (totalWeight <= 0f)
            return PlayerShopItemRarity.Normal;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cursor = 0f;

        for (int i = 0; i < _rarityWeights.Length; i++)
        {
            cursor += Mathf.Max(0f, _rarityWeights[i].weight);

            if (roll <= cursor)
                return _rarityWeights[i].rarity;
        }

        return PlayerShopItemRarity.Normal;
    }

    private bool TryPickEntry(
        Predicate<BulletShopEntry> filter,
        IReadOnlyList<BulletShopEntry> excluded,
        out BulletShopEntry entry)
    {
        entry = null;

        if (_entries == null)
            return false;

        int candidateCount = 0;

        for (int i = 0; i < _entries.Length; i++)
        {
            if (IsCandidateValid(_entries[i], excluded, filter))
                candidateCount++;
        }

        if (candidateCount <= 0)
            return false;

        int pickedIndex = UnityEngine.Random.Range(0, candidateCount);
        int cursor = 0;

        for (int i = 0; i < _entries.Length; i++)
        {
            BulletShopEntry candidate = _entries[i];

            if (!IsCandidateValid(candidate, excluded, filter))
                continue;

            if (cursor == pickedIndex)
            {
                entry = candidate;
                return true;
            }

            cursor++;
        }

        return false;
    }

    private static bool IsCandidateValid(
        BulletShopEntry candidate,
        IReadOnlyList<BulletShopEntry> excluded,
        Predicate<BulletShopEntry> filter)
    {
        if (candidate == null || !candidate.IsValid)
            return false;

        if (excluded != null)
        {
            for (int i = 0; i < excluded.Count; i++)
            {
                if (excluded[i] == candidate)
                    return false;
            }
        }

        return filter == null || filter(candidate);
    }
}
