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
/// a table can appear. A shared runtime rule adjusts only armor/ammo LV within its authored candidates.
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
    // Container-only settings: enemy stage pools retain their own existing contract.
    public List<LootRarityCategorySettings> categorySettings = new();

    public LootRarityCategorySettings FindCategorySettings(PlayerShopItemRarity rarity) =>
        categorySettings?.Find(settings => settings != null && settings.rarity == rarity);

    public LootRarityCategorySettings EnsureCategorySettings(PlayerShopItemRarity rarity)
    {
        categorySettings ??= new();
        var settings = FindCategorySettings(rarity);
        if (settings == null) { settings = new LootRarityCategorySettings { rarity = rarity }; categorySettings.Add(settings); }
        return settings;
    }

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
/// Directly referenced contents for one searchable world object. Loot Maker
/// authors one independent table per box, containing exactly one fixed pool.
/// </summary>
[CreateAssetMenu(fileName = "LootTable_", menuName = "Game/Loot/Loot Table")]
public sealed class LootTableSO : ScriptableObject
{
    [Tooltip("Fixed authored candidates. The common Stage 1–5 rule adjusts armor/ammo LV weights within these candidates; it never adds items.")]
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
    /// Legacy roll-count API, retained for old callers. World containers use
    /// TryGenerateSlots with their actual stage and explicit occupied-slot range.
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

    public bool HasSlotItems(LootContainerRarityWeights weights, int stageIndex = 0)
    {
        if (StageEquipmentLootDistribution.AppliesToStage(stageIndex)) return GetStageDistribution(weights, stageIndex).HasCandidates;
        foreach (var tier in Pool.rarityTiers)
            if (HasValidItems(tier) && weights != null && weights.Get(tier.rarity) > 0f) return true;
        return false;
    }

    /// <summary>Actual per-filled-slot chance, excluding empty/disabled tiers. No hidden lock multipliers.</summary>
    public float GetEffectiveTierPercent(PlayerShopItemRarity rarity, LootContainerRarityWeights weights, int stageIndex = 0)
    {
        if (weights == null) return 0f;
        if (StageEquipmentLootDistribution.AppliesToStage(stageIndex)) return GetStageDistribution(weights, stageIndex).RarityPercent(rarity);
        double total = 0d, selected = 0d;
        foreach (var tier in Pool.rarityTiers)
        {
            if (!HasValidItems(tier)) continue;
            double weight = weights.Get(tier.rarity);
            total += weight;
            if (tier.rarity == rarity) selected += weight;
        }
        return total > 0d ? (float)(selected / total * 100d) : 0f;
    }

    public float GetEffectiveItemPercent(LootRarityTier tier, LootItemEntry entry, LootContainerRarityWeights weights, int stageIndex = 0)
    {
        if (tier == null) return 0f;
        if (StageEquipmentLootDistribution.AppliesToStage(stageIndex)) return GetStageDistribution(weights, stageIndex).ItemPercent(entry);
        return GetEffectiveTierPercent(tier.rarity, weights) * GetCategorySelection(tier).ItemPercentInTier(entry) / 100f;
    }

    public LootCategorySelection GetCategorySelection(LootRarityTier tier) =>
        new(tier, tier != null ? _pool?.FindCategorySettings(tier.rarity) : null);

    public StageEquipmentLootDistribution GetStageDistribution(LootContainerRarityWeights weights, int stageIndex)
    {
        var distribution = new StageEquipmentLootDistribution();
        foreach (var tier in Pool.rarityTiers)
            distribution.AddTier(tier, GetCategorySelection(tier), weights != null ? weights.Get(tier.rarity) : 0);
        distribution.ApplyStage(stageIndex);
        return distribution;
    }

    /// <summary>
    /// One result equals one occupied slot. Duplicate items remain separate results;
    /// amounts fit one stack. True includes an intentionally empty (zero-slot) result.
    /// Legacy attempt chances and lock bonus rolls do not affect this explicit box contract.
    /// </summary>
    public bool TryGenerateSlots(System.Random random, List<LootRollResult> results, LootObjectSO box, int stageIndex = 0)
    {
        if (results == null) return false;
        results.Clear();
        if (box == null) return false;
        if (box.MaxGeneratedSlots == 0) return true;
        random ??= new System.Random();
        if (StageEquipmentLootDistribution.AppliesToStage(stageIndex))
        {
            var distribution = GetStageDistribution(box.RarityWeights, stageIndex);
            if (distribution.HasLeveledEquipment)
            {
                if (!distribution.HasCandidates) return false;
                int slots = random.Next(box.MinGeneratedSlots, box.MaxGeneratedSlots + 1);
                for (int i = 0; i < slots; i++)
                {
                    var row = distribution.Pick(random);
                    var entry = row.Entry;
                    int limit = Mathf.Max(1, entry.item.MaxStack);
                    int min = Mathf.Clamp(entry.minAmount, 1, limit), max = Mathf.Clamp(entry.maxAmount, min, limit);
                    int amount = min + (int)(random.NextDouble() * ((long)max - min + 1));
                    PassiveInstanceRuntime.Append(results, entry.item, amount, row.Rarity, random);
                }
                return true;
            }
        }
        var choices = new List<(LootRarityTier tier, LootCategorySelection selection, float weight)>();
        double total = 0d;
        foreach (var tier in Pool.rarityTiers)
        {
            var selection = GetCategorySelection(tier);
            float weight = box.RarityWeights.Get(tier.rarity);
            if (!selection.HasCandidates || weight <= 0f) continue;
            choices.Add((tier, selection, weight)); total += weight;
        }
        if (total <= 0d) return false;
        int count = random.Next(box.MinGeneratedSlots, box.MaxGeneratedSlots + 1);
        for (int i = 0; i < count; i++)
        {
            double pick = random.NextDouble() * total;
            var chosen = choices[choices.Count - 1];
            foreach (var choice in choices)
            {
                chosen = choice;
                if (pick < choice.weight) break;
                pick -= choice.weight;
            }
            // Each slot has fresh rarity, category and item draws, with replacement.
            var entry = chosen.selection.Pick(random);
            if (entry?.item == null) { results.Clear(); return false; }
            int stackLimit = Mathf.Max(1, entry.item.MaxStack);
            int min = Mathf.Clamp(entry.minAmount, 1, stackLimit);
            int max = Mathf.Clamp(entry.maxAmount, min, stackLimit);
            // NextDouble avoids int.MaxValue + 1 overflow for large stack capacities.
            int amount = min + (int)(random.NextDouble() * ((long)max - min + 1));
            PassiveInstanceRuntime.Append(results, entry.item, amount, chosen.tier.rarity, random);
        }
        return true;
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
                PassiveInstanceRuntime.Append(results, result.Item, result.Amount, result.Rarity, random);
        }

        return results.Count > 0;
    }

    private bool TryRollOne(
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

    private LootRarityTier PickTier(
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
            if (!HasValidItems(tier) || GetTierWeight(tier, lockedLootProfile) <= 0f)
                continue;

            roll -= GetTierWeight(tier, lockedLootProfile);
            if (roll <= 0f)
                return tier;
        }

        return null;
    }

    private LootItemEntry PickItem(LootRarityTier tier, System.Random random) => GetCategorySelection(tier).Pick(random);

    private static float GetTierWeight(LootRarityTier tier, LockedLootProfileSO lockedLootProfile)
    {
        if (tier == null)
            return 0f;

        float multiplier = lockedLootProfile != null
            ? lockedLootProfile.GetRarityWeightMultiplier(tier.rarity)
            : 1f;
        return Mathf.Max(0f, tier.chancePercent) * Mathf.Max(0f, multiplier);
    }

    private bool HasValidItems(LootRarityTier tier) => GetCategorySelection(tier).HasCandidates;

    private void OnValidate()
    {
        EnsurePool();
    }
}
