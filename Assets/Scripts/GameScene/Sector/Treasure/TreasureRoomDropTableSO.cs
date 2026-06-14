using System;
using System.Collections.Generic;
using UnityEngine;

public enum TreasureRewardKind
{
    Passive,
    Bullet
}

public readonly struct TreasureRoomReward
{
    public TreasureRewardKind Kind { get; }
    public PlayerShopItemRarity Rarity { get; }
    public PassiveItemSO PassiveItem { get; }
    public BulletSO Bullet { get; }
    public int BulletBundleAmount { get; }
    public float BulletSellPriceRate { get; }
    public GameObject PickupPrefab { get; }

    public TreasureRoomReward(
        TreasureRewardKind kind,
        PlayerShopItemRarity rarity,
        PassiveItemSO passiveItem,
        BulletSO bullet,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        GameObject pickupPrefab)
    {
        Kind = kind;
        Rarity = rarity;
        PassiveItem = passiveItem;
        Bullet = bullet;
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
        [Tooltip("Passive item reward. Its stat modifiers are applied by PlayerPassiveInventoryRuntime.")]
        public PassiveItemSO item;

        [Tooltip("Rarity used by the Treasure room rarity roll.")]
        public PlayerShopItemRarity rarity;

        [Min(0f)]
        [Tooltip("Relative weight among passive rewards with the rolled rarity.")]
        public float weight = 1f;

        [Tooltip("Optional pickup prefab override. Uses Default Passive Pickup Prefab when empty.")]
        public GameObject pickupPrefabOverride;
    }

    [Serializable]
    public sealed class BulletItemEntry
    {
        [Tooltip("Bullet reward granted to an empty compatible player ammo slot.")]
        public BulletSO bullet;

        [Tooltip("Rarity used by the Treasure room rarity roll.")]
        public PlayerShopItemRarity rarity;

        [Min(0f)]
        [Tooltip("Relative weight among bullet rewards with the rolled rarity.")]
        public float weight = 1f;

        [Min(1)]
        [Tooltip("Total ammo bundle granted with this bullet.")]
        public int bundleAmount = 20;

        [Range(0f, 1f)]
        [Tooltip("Sell value ratio if this reward should have resale value. 0 means no sell value.")]
        public float sellPriceRate = 0f;

        [Tooltip("Optional pickup prefab override. Uses Default Bullet Pickup Prefab when empty.")]
        public GameObject pickupPrefabOverride;
    }

    [Header("Pickup Prefabs")]
    [SerializeField] private GameObject _defaultPassivePickupPrefab;
    [SerializeField] private GameObject _defaultBulletPickupPrefab;

    [Header("Rarity Weights")]
    [SerializeField] private RarityWeight[] _rarityWeights =
    {
        new RarityWeight { rarity = PlayerShopItemRarity.Normal, weight = 65f },
        new RarityWeight { rarity = PlayerShopItemRarity.Rare, weight = 30f },
        new RarityWeight { rarity = PlayerShopItemRarity.Legendary, weight = 5f },
    };

    [Header("Reward Type Weights")]
    [SerializeField] private RewardKindWeight[] _rewardKindWeights =
    {
        new RewardKindWeight { kind = TreasureRewardKind.Passive, weight = 80f },
        new RewardKindWeight { kind = TreasureRewardKind.Bullet, weight = 20f },
    };

    [Header("Passive Pool")]
    [SerializeField] private PassiveItemEntry[] _passiveItems;

    [Header("Bullet Pool")]
    [SerializeField] private BulletItemEntry[] _bulletItems;

    public bool TryRollReward(int seed, out TreasureRoomReward reward)
    {
        System.Random rng = new(seed);
        PlayerShopItemRarity rarity = RollRarity(rng);
        TreasureRewardKind kind = RollRewardKind(rng);

        if (TryCreateReward(kind, rarity, rng, out reward))
            return true;

        TreasureRewardKind fallbackKind = kind == TreasureRewardKind.Passive
            ? TreasureRewardKind.Bullet
            : TreasureRewardKind.Passive;

        if (TryCreateReward(fallbackKind, rarity, rng, out reward))
            return true;

        if (TryCreateReward(kind, null, rng, out reward))
            return true;

        if (TryCreateReward(fallbackKind, null, rng, out reward))
            return true;

        reward = default(TreasureRoomReward);
        return false;
    }

    private bool TryCreateReward(
        TreasureRewardKind kind,
        PlayerShopItemRarity? rarity,
        System.Random rng,
        out TreasureRoomReward reward)
    {
        return kind == TreasureRewardKind.Passive
            ? TryCreatePassiveReward(rarity, rng, out reward)
            : TryCreateBulletReward(rarity, rng, out reward);
    }

    private bool TryCreatePassiveReward(
        PlayerShopItemRarity? rarity,
        System.Random rng,
        out TreasureRoomReward reward)
    {
        reward = default(TreasureRoomReward);

        if (!TryPickWeighted(
                _passiveItems,
                entry => entry != null &&
                         entry.item != null &&
                         entry.weight > 0f &&
                         (!rarity.HasValue || entry.rarity == rarity.Value),
                entry => entry.weight,
                rng,
                out PassiveItemEntry picked))
        {
            return false;
        }

        GameObject pickupPrefab = picked.pickupPrefabOverride != null
            ? picked.pickupPrefabOverride
            : _defaultPassivePickupPrefab;

        reward = new TreasureRoomReward(
            TreasureRewardKind.Passive,
            picked.rarity,
            picked.item,
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
        reward = default(TreasureRoomReward);

        if (!TryPickWeighted(
                _bulletItems,
                entry => entry != null &&
                         entry.bullet != null &&
                         entry.weight > 0f &&
                         (!rarity.HasValue || entry.rarity == rarity.Value),
                entry => entry.weight,
                rng,
                out BulletItemEntry picked))
        {
            return false;
        }

        GameObject pickupPrefab = picked.pickupPrefabOverride != null
            ? picked.pickupPrefabOverride
            : _defaultBulletPickupPrefab;

        reward = new TreasureRoomReward(
            TreasureRewardKind.Bullet,
            picked.rarity,
            null,
            picked.bullet,
            picked.bundleAmount,
            picked.sellPriceRate,
            pickupPrefab);

        return pickupPrefab != null;
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

    private static bool TryPickWeighted<T>(
        IReadOnlyList<T> entries,
        Predicate<T> predicate,
        Func<T, float> getWeight,
        System.Random rng,
        out T picked)
    {
        picked = default(T);

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
