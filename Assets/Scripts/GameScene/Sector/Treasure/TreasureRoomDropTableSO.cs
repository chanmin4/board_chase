using System;
using System.Collections.Generic;
using UnityEngine;

public enum TreasureRewardKind
{
    Passive,
    Bullet,
    Armor
}

public readonly struct TreasureRoomReward
{
    public TreasureRewardKind Kind { get; }
    public PlayerShopItemRarity Rarity { get; }
    public ItemSO Item { get; }
    public PassiveItemSO PassiveItem { get; }
    public BulletSO Bullet { get; }
    public ArmorItemSO ArmorItem { get; }
    public int BulletBundleAmount { get; }
    public float BulletSellPriceRate { get; }
    public GameObject PickupPrefab { get; }

    public TreasureRoomReward(
        TreasureRewardKind kind,
        PlayerShopItemRarity rarity,
        PassiveItemSO passiveItem,
        BulletSO bullet,
        ArmorItemSO armorItem,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        GameObject pickupPrefab)
    {
        Kind = kind;
        Rarity = rarity;
        Item = passiveItem != null
            ? (ItemSO)passiveItem
            : bullet != null ? (ItemSO)bullet : armorItem;
        PassiveItem = passiveItem;
        Bullet = bullet;
        ArmorItem = armorItem;
        BulletBundleAmount = Mathf.Max(1, bulletBundleAmount);
        BulletSellPriceRate = Mathf.Clamp01(bulletSellPriceRate);
        PickupPrefab = pickupPrefab;
    }
}

[CreateAssetMenu(
    fileName = "TreasureRoomDropTable",
    menuName = "Game/Sector/Treasure Room Drop Table")]
public class TreasureRoomDropTableSO : ScriptableObject
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
    public sealed class PassiveItemEntry
    {
        public PassiveItemSO item;
        [Min(0f)] public float weight = 1f;
    }

    [Serializable]
    public sealed class BulletItemEntry
    {
        public BulletSO bullet;
        [Min(0f)] public float weight = 1f;
        [Min(1)] public int bundleAmount = 20;
        [Range(0f, 1f)] public float sellPriceRate = 0f;
        public GameObject pickupPrefabOverride;
    }

    [Serializable]
    public sealed class ArmorItemEntry
    {
        public ArmorItemSO item;
        [Min(0f)] public float weight = 1f;
        public GameObject pickupPrefabOverride;
    }

    [Serializable]
    public sealed class RarityRewardPool
    {
        public PassiveItemEntry[] passiveItems;
        public BulletItemEntry[] bulletItems;
        public ArmorItemEntry[] armorItems;
    }

    private readonly struct PassiveCandidate
    {
        public readonly PassiveItemEntry Entry;
        public readonly PlayerShopItemRarity Rarity;

        public PassiveCandidate(PassiveItemEntry entry, PlayerShopItemRarity rarity)
        {
            Entry = entry;
            Rarity = rarity;
        }
    }

    private readonly struct BulletCandidate
    {
        public readonly BulletItemEntry Entry;
        public readonly PlayerShopItemRarity Rarity;

        public BulletCandidate(BulletItemEntry entry, PlayerShopItemRarity rarity)
        {
            Entry = entry;
            Rarity = rarity;
        }
    }

    private readonly struct ArmorCandidate
    {
        public readonly ArmorItemEntry Entry;
        public readonly PlayerShopItemRarity Rarity;

        public ArmorCandidate(ArmorItemEntry entry, PlayerShopItemRarity rarity)
        {
            Entry = entry;
            Rarity = rarity;
        }
    }

    [Header("Roll")]
    [SerializeField, Min(1)] private int _maxRollAttempts = 100;

    [Header("Pickup Prefabs")]
    [SerializeField] private GameObject _defaultPassivePickupPrefab;
    [SerializeField] private GameObject _defaultBulletPickupPrefab;

    [Header("Rarity Weights")]
    [SerializeField] private RarityWeight[] _rarityWeights =
    {
        new RarityWeight { rarity = PlayerShopItemRarity.Normal, weight = 65f },
        new RarityWeight { rarity = PlayerShopItemRarity.Rare, weight = 25f },
        new RarityWeight { rarity = PlayerShopItemRarity.Unique, weight = 8f },
        new RarityWeight { rarity = PlayerShopItemRarity.Legendary, weight = 2f },
    };

    [Header("Reward Type Weights")]
    [SerializeField] private RewardKindWeight[] _rewardKindWeights =
    {
        new RewardKindWeight { kind = TreasureRewardKind.Passive, weight = 80f },
        new RewardKindWeight { kind = TreasureRewardKind.Bullet, weight = 20f },
        new RewardKindWeight { kind = TreasureRewardKind.Armor, weight = 0f },
    };

    [Header("Normal Pool")]
    [SerializeField] private RarityRewardPool _normalPool = new();

    [Header("Rare Pool")]
    [SerializeField] private RarityRewardPool _rarePool = new();

    [Header("Unique Pool")]
    [SerializeField] private RarityRewardPool _uniquePool = new();

    [Header("Legendary Pool")]
    [SerializeField] private RarityRewardPool _legendaryPool = new();

    public int MaxRollAttempts => Mathf.Max(1, _maxRollAttempts);

    public bool TryRollReward(int seed, out TreasureRoomReward reward)
    {
        return TryRollReward(seed, ownedPassiveItems: null, out reward);
    }

    public bool TryRollReward(
        int seed,
        IReadOnlyList<PassiveItemSO> ownedPassiveItems,
        out TreasureRoomReward reward)
    {
        System.Random rng = new(seed);
        int attempts = MaxRollAttempts;

        for (int i = 0; i < attempts; i++)
        {
            PlayerShopItemRarity rarity = RollRarity(rng);
            TreasureRewardKind kind = RollRewardKind(rng);

            if (TryCreateReward(kind, rarity, ownedPassiveItems, rng, out reward))
                return true;
        }

        if (TryCreateFallbackReward(ownedPassiveItems, rng, out reward))
            return true;

        reward = default;
        return false;
    }

    private bool TryCreateFallbackReward(
        IReadOnlyList<PassiveItemSO> ownedPassiveItems,
        System.Random rng,
        out TreasureRoomReward reward)
    {
        TreasureRewardKind firstKind = RollRewardKind(rng);
        TreasureRewardKind[] fallbackKinds =
        {
            firstKind,
            TreasureRewardKind.Passive,
            TreasureRewardKind.Bullet,
            TreasureRewardKind.Armor
        };

        for (int i = 0; i < fallbackKinds.Length; i++)
        {
            TreasureRewardKind kind = fallbackKinds[i];
            bool alreadyTried = false;

            for (int j = 0; j < i; j++)
            {
                if (fallbackKinds[j] == kind)
                {
                    alreadyTried = true;
                    break;
                }
            }

            if (alreadyTried)
                continue;

            if (TryCreateReward(kind, null, ownedPassiveItems, rng, out reward))
                return true;
        }

        reward = default;
        return false;
    }

    private bool TryCreateReward(
        TreasureRewardKind kind,
        PlayerShopItemRarity? rarity,
        IReadOnlyList<PassiveItemSO> ownedPassiveItems,
        System.Random rng,
        out TreasureRoomReward reward)
    {
        switch (kind)
        {
            case TreasureRewardKind.Passive:
                return TryCreatePassiveReward(rarity, ownedPassiveItems, rng, out reward);

            case TreasureRewardKind.Bullet:
                return TryCreateBulletReward(rarity, rng, out reward);

            case TreasureRewardKind.Armor:
                return TryCreateArmorReward(rarity, rng, out reward);

            default:
                reward = default;
                return false;
        }
    }

    private bool TryCreatePassiveReward(
        PlayerShopItemRarity? rarity,
        IReadOnlyList<PassiveItemSO> ownedPassiveItems,
        System.Random rng,
        out TreasureRoomReward reward)
    {
        reward = default;

        if (!TryPickPassive(
                rarity,
                ownedPassiveItems,
                rng,
                out PassiveItemEntry picked,
                out PlayerShopItemRarity resolvedRarity))
        {
            return false;
        }

        GameObject pickupPrefab = _defaultPassivePickupPrefab;

        reward = new TreasureRoomReward(
            TreasureRewardKind.Passive,
            resolvedRarity,
            picked.item,
            null,
            null,
            1,
            0f,
            pickupPrefab);

        return pickupPrefab != null;
    }

    private bool TryCreateBulletReward(
        PlayerShopItemRarity? rarity,
        System.Random rng,
        out TreasureRoomReward reward)
    {
        reward = default;

        if (!TryPickBullet(
                rarity,
                rng,
                out BulletItemEntry picked,
                out PlayerShopItemRarity resolvedRarity))
        {
            return false;
        }

        GameObject pickupPrefab = picked.pickupPrefabOverride != null
            ? picked.pickupPrefabOverride
            : _defaultBulletPickupPrefab;

        reward = new TreasureRoomReward(
            TreasureRewardKind.Bullet,
            resolvedRarity,
            null,
            picked.bullet,
            null,
            picked.bundleAmount,
            picked.sellPriceRate,
            pickupPrefab);

        return pickupPrefab != null;
    }

    private bool TryCreateArmorReward(
        PlayerShopItemRarity? rarity,
        System.Random rng,
        out TreasureRoomReward reward)
    {
        reward = default;

        if (!TryPickArmor(
                rarity,
                rng,
                out ArmorItemEntry picked,
                out PlayerShopItemRarity resolvedRarity))
        {
            return false;
        }

        GameObject pickupPrefab = picked.pickupPrefabOverride != null
            ? picked.pickupPrefabOverride
            : picked.item.WorldItemPrefab;

        reward = new TreasureRoomReward(
            TreasureRewardKind.Armor,
            resolvedRarity,
            null,
            null,
            picked.item,
            1,
            0f,
            pickupPrefab);

        return pickupPrefab != null;
    }

    private bool TryPickPassive(
        PlayerShopItemRarity? rarity,
        IReadOnlyList<PassiveItemSO> ownedPassiveItems,
        System.Random rng,
        out PassiveItemEntry picked,
        out PlayerShopItemRarity resolvedRarity)
    {
        if (rarity.HasValue)
        {
            List<PassiveCandidate> candidates = BuildPassiveCandidates(ownedPassiveItems);

            if (TryPickWeighted(
                candidates,
                candidate => candidate.Rarity == rarity.Value,
                candidate => candidate.Entry.weight,
                rng,
                out PassiveCandidate result))
            {
                picked = result.Entry;
                resolvedRarity = result.Rarity;
                return true;
            }

            picked = null;
            resolvedRarity = rarity.Value;
            return false;
        }

        return TryPickPassiveFromAnyRarity(
            ownedPassiveItems,
            rng,
            out picked,
            out resolvedRarity);
    }

    private bool TryPickBullet(
        PlayerShopItemRarity? rarity,
        System.Random rng,
        out BulletItemEntry picked,
        out PlayerShopItemRarity resolvedRarity)
    {
        if (rarity.HasValue)
        {
            List<BulletCandidate> candidates = BuildBulletCandidates();

            if (TryPickWeighted(
                candidates,
                candidate => candidate.Rarity == rarity.Value,
                candidate => candidate.Entry.weight,
                rng,
                out BulletCandidate result))
            {
                picked = result.Entry;
                resolvedRarity = result.Rarity;
                return true;
            }

            picked = null;
            resolvedRarity = rarity.Value;
            return false;
        }

        return TryPickBulletFromAnyRarity(rng, out picked, out resolvedRarity);
    }

    private bool TryPickArmor(
        PlayerShopItemRarity? rarity,
        System.Random rng,
        out ArmorItemEntry picked,
        out PlayerShopItemRarity resolvedRarity)
    {
        if (rarity.HasValue)
        {
            List<ArmorCandidate> candidates = BuildArmorCandidates();

            if (TryPickWeighted(
                candidates,
                candidate => candidate.Rarity == rarity.Value,
                candidate => candidate.Entry.weight,
                rng,
                out ArmorCandidate result))
            {
                picked = result.Entry;
                resolvedRarity = result.Rarity;
                return true;
            }

            picked = null;
            resolvedRarity = rarity.Value;
            return false;
        }

        return TryPickArmorFromAnyRarity(rng, out picked, out resolvedRarity);
    }

    private bool TryPickPassiveFromAnyRarity(
        IReadOnlyList<PassiveItemSO> ownedPassiveItems,
        System.Random rng,
        out PassiveItemEntry picked,
        out PlayerShopItemRarity rarity)
    {
        List<PassiveCandidate> candidates = BuildPassiveCandidates(ownedPassiveItems);

        if (TryPickWeighted(
                candidates,
                candidate => true,
                candidate => candidate.Entry.weight,
                rng,
                out PassiveCandidate result))
        {
            picked = result.Entry;
            rarity = result.Rarity;
            return true;
        }

        picked = null;
        rarity = PlayerShopItemRarity.Normal;
        return false;
    }

    private bool TryPickBulletFromAnyRarity(
        System.Random rng,
        out BulletItemEntry picked,
        out PlayerShopItemRarity rarity)
    {
        List<BulletCandidate> candidates = BuildBulletCandidates();

        if (TryPickWeighted(
                candidates,
                candidate => true,
                candidate => candidate.Entry.weight,
                rng,
                out BulletCandidate result))
        {
            picked = result.Entry;
            rarity = result.Rarity;
            return true;
        }

        picked = null;
        rarity = PlayerShopItemRarity.Normal;
        return false;
    }

    private bool TryPickArmorFromAnyRarity(
        System.Random rng,
        out ArmorItemEntry picked,
        out PlayerShopItemRarity rarity)
    {
        List<ArmorCandidate> candidates = BuildArmorCandidates();

        if (TryPickWeighted(
                candidates,
                candidate => true,
                candidate => candidate.Entry.weight,
                rng,
                out ArmorCandidate result))
        {
            picked = result.Entry;
            rarity = result.Rarity;
            return true;
        }

        picked = null;
        rarity = PlayerShopItemRarity.Normal;
        return false;
    }

    private static void AddPassiveCandidates(
        List<PassiveCandidate> candidates,
        RarityRewardPool pool,
        PlayerShopItemRarity rarity,
        IReadOnlyList<PassiveItemSO> ownedPassiveItems)
    {
        if (candidates == null || pool?.passiveItems == null)
            return;

        for (int i = 0; i < pool.passiveItems.Length; i++)
        {
            PassiveItemEntry entry = pool.passiveItems[i];

            if (IsPassiveCandidateValid(entry, ownedPassiveItems))
                candidates.Add(new PassiveCandidate(entry, entry.item.Rarity));
        }
    }

    private static void AddBulletCandidates(
        List<BulletCandidate> candidates,
        RarityRewardPool pool,
        PlayerShopItemRarity rarity)
    {
        if (candidates == null || pool?.bulletItems == null)
            return;

        for (int i = 0; i < pool.bulletItems.Length; i++)
        {
            BulletItemEntry entry = pool.bulletItems[i];

            if (IsBulletCandidateValid(entry))
                candidates.Add(new BulletCandidate(entry, entry.bullet.Rarity));
        }
    }

    private static void AddArmorCandidates(
        List<ArmorCandidate> candidates,
        RarityRewardPool pool,
        PlayerShopItemRarity rarity)
    {
        if (candidates == null || pool?.armorItems == null)
            return;

        for (int i = 0; i < pool.armorItems.Length; i++)
        {
            ArmorItemEntry entry = pool.armorItems[i];

            if (IsArmorCandidateValid(entry))
                candidates.Add(new ArmorCandidate(entry, entry.item.Rarity));
        }
    }

    private List<PassiveCandidate> BuildPassiveCandidates(IReadOnlyList<PassiveItemSO> ownedPassiveItems)
    {
        List<PassiveCandidate> candidates = new();
        AddPassiveCandidates(candidates, _normalPool, PlayerShopItemRarity.Normal, ownedPassiveItems);
        AddPassiveCandidates(candidates, _rarePool, PlayerShopItemRarity.Rare, ownedPassiveItems);
        AddPassiveCandidates(candidates, _uniquePool, PlayerShopItemRarity.Unique, ownedPassiveItems);
        AddPassiveCandidates(candidates, _legendaryPool, PlayerShopItemRarity.Legendary, ownedPassiveItems);
        return candidates;
    }

    private List<BulletCandidate> BuildBulletCandidates()
    {
        List<BulletCandidate> candidates = new();
        AddBulletCandidates(candidates, _normalPool, PlayerShopItemRarity.Normal);
        AddBulletCandidates(candidates, _rarePool, PlayerShopItemRarity.Rare);
        AddBulletCandidates(candidates, _uniquePool, PlayerShopItemRarity.Unique);
        AddBulletCandidates(candidates, _legendaryPool, PlayerShopItemRarity.Legendary);
        return candidates;
    }

    private List<ArmorCandidate> BuildArmorCandidates()
    {
        List<ArmorCandidate> candidates = new();
        AddArmorCandidates(candidates, _normalPool, PlayerShopItemRarity.Normal);
        AddArmorCandidates(candidates, _rarePool, PlayerShopItemRarity.Rare);
        AddArmorCandidates(candidates, _uniquePool, PlayerShopItemRarity.Unique);
        AddArmorCandidates(candidates, _legendaryPool, PlayerShopItemRarity.Legendary);
        return candidates;
    }

    private RarityRewardPool GetPool(PlayerShopItemRarity rarity)
    {
        return rarity switch
        {
            PlayerShopItemRarity.Rare => _rarePool,
            PlayerShopItemRarity.Unique => _uniquePool,
            PlayerShopItemRarity.Legendary => _legendaryPool,
            _ => _normalPool
        };
    }

    private PlayerShopItemRarity RollRarity(System.Random rng)
    {
        if (TryPickWeighted(
                _rarityWeights,
                entry => entry.weight > 0f,
                entry => entry.weight,
                rng,
                out RarityWeight picked))
        {
            return picked.rarity;
        }

        return PlayerShopItemRarity.Normal;
    }

    private TreasureRewardKind RollRewardKind(System.Random rng)
    {
        if (TryPickWeighted(
                _rewardKindWeights,
                entry => entry.weight > 0f,
                entry => entry.weight,
                rng,
                out RewardKindWeight picked))
        {
            return picked.kind;
        }

        return TreasureRewardKind.Passive;
    }

    private static bool IsPassiveCandidateValid(
        PassiveItemEntry entry,
        IReadOnlyList<PassiveItemSO> ownedPassiveItems)
    {
        if (entry == null || entry.item == null || entry.weight <= 0f)
            return false;

        if (entry.item.AllowDuplicatePickup)
            return true;

        if (ownedPassiveItems == null)
            return true;

        for (int i = 0; i < ownedPassiveItems.Count; i++)
        {
            if (ownedPassiveItems[i] == entry.item)
                return false;
        }

        return true;
    }

    private static bool IsBulletCandidateValid(BulletItemEntry entry)
    {
        return entry != null &&
               entry.bullet != null &&
               entry.weight > 0f;
    }

    private static bool IsArmorCandidateValid(ArmorItemEntry entry)
    {
        return entry != null &&
               entry.item != null &&
               entry.weight > 0f;
    }

    private static bool TryPickWeighted<T>(
        IReadOnlyList<T> entries,
        Predicate<T> predicate,
        Func<T, float> getWeight,
        System.Random rng,
        out T picked)
    {
        picked = default;

        if (entries == null || entries.Count <= 0)
            return false;

        float totalWeight = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            T entry = entries[i];

            if (predicate != null && !predicate(entry))
                continue;

            totalWeight += Mathf.Max(0f, getWeight(entry));
        }

        if (totalWeight <= 0f)
            return false;

        float roll = (float)(rng.NextDouble() * totalWeight);

        for (int i = 0; i < entries.Count; i++)
        {
            T entry = entries[i];

            if (predicate != null && !predicate(entry))
                continue;

            float weight = Mathf.Max(0f, getWeight(entry));

            if (roll < weight)
            {
                picked = entry;
                return true;
            }

            roll -= weight;
        }

        return false;
    }
}
