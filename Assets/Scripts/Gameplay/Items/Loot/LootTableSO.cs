/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Defines one fixed rarity pool rolled by one LootObjectSO.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One fixed, object-owned loot pool. World-object placement determines where
/// a table can appear; this data never branches by stage.
/// </summary>
[Serializable]
public sealed class LootObjectPool
{
    public const float NormalChancePercent = 63f;
    public const float RareChancePercent = 30f;
    public const float UniqueChancePercent = 6f;
    public const float LegendaryChancePercent = 1f;

    [Min(1)] public int minRollCount = 1;
    [Min(1)] public int maxRollCount = 1;
    public List<LootRarityTier> rarityTiers = new();

    /// <summary>
    /// Restores the four project-wide rarity rows while preserving authored
    /// item lists. A LootTableSO never exposes per-object tier weights.
    /// </summary>
    public void EnsureStandardTiers()
    {
        rarityTiers ??= new List<LootRarityTier>();
        minRollCount = Mathf.Max(1, minRollCount);
        maxRollCount = Mathf.Max(minRollCount, maxRollCount);

        List<LootRarityTier> normalized = new(4);
        AddOrNormalizeTier(normalized, PlayerShopItemRarity.Normal, NormalChancePercent);
        AddOrNormalizeTier(normalized, PlayerShopItemRarity.Rare, RareChancePercent);
        AddOrNormalizeTier(normalized, PlayerShopItemRarity.Unique, UniqueChancePercent);
        AddOrNormalizeTier(normalized, PlayerShopItemRarity.Legendary, LegendaryChancePercent);
        rarityTiers = normalized;
    }

    /// <summary>
    /// Finds one of the standardized rarity rows.
    /// </summary>
    public LootRarityTier FindTier(PlayerShopItemRarity rarity)
    {
        if (rarityTiers == null)
            return null;

        for (int i = 0; i < rarityTiers.Count; i++)
        {
            LootRarityTier tier = rarityTiers[i];
            if (tier != null && tier.rarity == rarity)
                return tier;
        }

        return null;
    }

    private void AddOrNormalizeTier(
        List<LootRarityTier> destination,
        PlayerShopItemRarity rarity,
        float chancePercent)
    {
        LootRarityTier tier = FindTier(rarity) ?? new LootRarityTier
        {
            rarity = rarity,
            items = new List<LootItemEntry>()
        };
        tier.rarity = rarity;
        tier.chancePercent = chancePercent;
        tier.items ??= new List<LootItemEntry>();
        destination.Add(tier);
    }
}

/// <summary>
/// Directly referenced contents for one searchable world object. A table may
/// be intentionally shared, but it always has exactly one fixed loot pool.
/// </summary>
[CreateAssetMenu(fileName = "LootTable_", menuName = "Game/Loot/Loot Table")]
public sealed class LootTableSO : ScriptableObject
{
    [Tooltip("The one fixed pool for this object. Place different loot objects in different stages instead of creating stage overrides here.")]
    [SerializeField] private LootObjectPool _pool = new();

    public LootObjectPool Pool
    {
        get
        {
            EnsurePool();
            return _pool;
        }
    }

    /// <summary>
    /// Ensures the fixed pool and its project-wide rarity split are present.
    /// </summary>
    public void EnsurePool()
    {
        _pool ??= new LootObjectPool();
        _pool.EnsureStandardTiers();
    }

    /// <summary>
    /// Rolls the object's fixed pool into a caller-owned list. Lock profiles
    /// can still multiply rarity weights, but stage index is intentionally not
    /// part of the world-container contract.
    /// </summary>
    public bool TryRoll(
        System.Random random,
        List<LootRollResult> results,
        LockedLootProfileSO lockedLootProfile)
        => TryRoll(random, results, lockedLootProfile, null);

    public bool HasRollableItems(LockedLootProfileSO lockedLootProfile = null)
    {
        foreach (var tier in Pool.rarityTiers)
            if (HasValidItems(tier) && GetTierWeight(tier, lockedLootProfile) > 0f) return true;
        return false;
    }

    public bool TryRoll(
        System.Random random,
        List<LootRollResult> results,
        LockedLootProfileSO lockedLootProfile,
        LootContainerRollSettings variant)
    {
        if (results == null)
            return false;

        results.Clear();
        random ??= new System.Random();

        LootObjectPool pool = Pool;
        if (pool.rarityTiers == null || pool.rarityTiers.Count == 0)
            return false;

        int minRolls = variant?.overrideTableRollCount == true ? variant.MinRolls : pool.minRollCount;
        int maxRolls = variant?.overrideTableRollCount == true ? variant.MaxRolls : pool.maxRollCount;
        int rollCount = random.Next(minRolls, maxRolls + 1);
        if (lockedLootProfile != null)
            rollCount += lockedLootProfile.GetAdditionalRollCount(random);

        float successChance = variant?.SuccessChance ?? 100f;
        for (int i = 0; i < rollCount; i++)
        {
            // Do not consume another random value at 100%: old variants retain their seeded results.
            if (successChance <= 0f || (successChance < 100f && random.NextDouble() * 100d >= successChance))
                continue;
            if (TryRollOne(pool, random, lockedLootProfile, out LootRollResult result))
                results.Add(result);
        }

        return results.Count > 0;
    }

    private static bool TryRollOne(
        LootObjectPool pool,
        System.Random random,
        LockedLootProfileSO lockedLootProfile,
        out LootRollResult result)
    {
        result = default;
        LootRarityTier tier = PickTier(pool.rarityTiers, random, lockedLootProfile);
        if (tier == null)
            return false;

        LootItemEntry entry = PickItem(tier, random);
        if (entry?.item == null)
            return false;

        int min = Mathf.Max(1, entry.minAmount);
        int max = Mathf.Max(min, entry.maxAmount);
        result = new LootRollResult(entry.item, random.Next(min, max + 1), tier.rarity);
        return true;
    }

    private static LootRarityTier PickTier(
        List<LootRarityTier> tiers,
        System.Random random,
        LockedLootProfileSO lockedLootProfile)
    {
        if (tiers == null)
            return null;

        float total = 0f;
        for (int i = 0; i < tiers.Count; i++)
        {
            LootRarityTier tier = tiers[i];
            if (HasValidItems(tier))
                total += GetTierWeight(tier, lockedLootProfile);
        }

        if (total <= 0f)
            return null;

        float roll = (float)(random.NextDouble() * total);
        for (int i = 0; i < tiers.Count; i++)
        {
            LootRarityTier tier = tiers[i];
            if (!HasValidItems(tier))
                continue;

            roll -= GetTierWeight(tier, lockedLootProfile);
            if (roll <= 0f)
                return tier;
        }

        return null;
    }

    private static LootItemEntry PickItem(LootRarityTier tier, System.Random random)
    {
        if (tier?.items == null)
            return null;

        float total = 0f;
        for (int i = 0; i < tier.items.Count; i++)
            total += LootProgressionConfigSO.GetResolvedItemSharePercent(tier, tier.items[i]);

        if (total <= 0f)
            return null;

        float roll = (float)(random.NextDouble() * total);
        for (int i = 0; i < tier.items.Count; i++)
        {
            LootItemEntry entry = tier.items[i];
            float share = LootProgressionConfigSO.GetResolvedItemSharePercent(tier, entry);
            if (share <= 0f)
                continue;

            roll -= share;
            if (roll <= 0f)
                return entry;
        }

        return null;
    }

    private static float GetTierWeight(LootRarityTier tier, LockedLootProfileSO lockedLootProfile)
    {
        if (tier == null)
            return 0f;

        float multiplier = lockedLootProfile != null
            ? lockedLootProfile.GetRarityWeightMultiplier(tier.rarity)
            : 1f;
        return Mathf.Max(0f, tier.chancePercent) * Mathf.Max(0f, multiplier);
    }

    private static bool HasValidItems(LootRarityTier tier)
    {
        if (tier?.items == null)
            return false;

        for (int i = 0; i < tier.items.Count; i++)
        {
            LootItemEntry entry = tier.items[i];
            if (entry?.item != null && LootProgressionConfigSO.GetResolvedItemSharePercent(tier, entry) > 0f)
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        EnsurePool();
    }
}
