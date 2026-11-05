/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Defines authored common loot pools used by enemies and world loot objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

public enum LootSourceKind
{
    EnemyShooter = 0,
    LootableObject = 1
}

[Serializable]
public sealed class LootItemEntry
{
    public ItemSO item;
    [Min(1)] public int minAmount = 1;
    [Min(1)] public int maxAmount = 1;

    [Tooltip("When enabled, this item's exact share within its rarity tier is reserved. The remaining share is split evenly between Automatic entries.")]
    public bool useManualShare;

    [Range(0f, 100f)] public float manualSharePercent;
}

[Serializable]
public sealed class LootRarityTier
{
    public PlayerShopItemRarity rarity = PlayerShopItemRarity.Normal;
    [Range(0f, 100f)] public float chancePercent = 65f;
    public List<LootItemEntry> items = new();
}

[Serializable]
public sealed class LootStagePool
{
    [Tooltip("0 = Default fallback used by every stage without its own override. 1-5 = an exact Stage override.")]
    [Min(0)] public int stageIndex;
    [Min(1)] public int minRollCount = 1;
    [Min(1)] public int maxRollCount = 1;
    public List<LootRarityTier> rarityTiers = new();

    // Purpose: Ensures default tiers is ready before the workflow continues.
    public void EnsureDefaultTiers()
    {
        if (rarityTiers == null)
            rarityTiers = new List<LootRarityTier>();

        EnsureTier(PlayerShopItemRarity.Normal, 65f);
        EnsureTier(PlayerShopItemRarity.Rare, 25f);
        EnsureTier(PlayerShopItemRarity.Unique, 8f);
        EnsureTier(PlayerShopItemRarity.Legendary, 2f);
    }

    // Purpose: Finds the requested tier.
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

    // Purpose: Ensures tier is ready before the workflow continues.
    private void EnsureTier(PlayerShopItemRarity rarity, float defaultChance)
    {
        LootRarityTier tier = FindTier(rarity);
        if (tier != null)
            return;

        rarityTiers.Add(
            new LootRarityTier
            {
                rarity = rarity,
                chancePercent = defaultChance,
                items = new List<LootItemEntry>()
            });
    }

    // Purpose: Creates a detached copy so authoring actions never share mutable tier or item lists.
    public static LootStagePool Clone(LootStagePool source, int targetStageIndex)
    {
        LootStagePool clone = new LootStagePool();
        clone.CopyFrom(source, targetStageIndex);
        return clone;
    }

    // Purpose: Replaces this table with the supplied table while preserving the requested owning stage.
    public void CopyFrom(LootStagePool source, int targetStageIndex)
    {
        stageIndex = Mathf.Max(0, targetStageIndex);
        minRollCount = Mathf.Max(1, source != null ? source.minRollCount : 1);
        maxRollCount = Mathf.Max(minRollCount, source != null ? source.maxRollCount : minRollCount);
        rarityTiers = new List<LootRarityTier>();

        if (source?.rarityTiers != null)
        {
            for (int tierIndex = 0; tierIndex < source.rarityTiers.Count; tierIndex++)
            {
                LootRarityTier sourceTier = source.rarityTiers[tierIndex];
                if (sourceTier == null)
                    continue;

                LootRarityTier copiedTier = new LootRarityTier
                {
                    rarity = sourceTier.rarity,
                    chancePercent = Mathf.Clamp(sourceTier.chancePercent, 0f, 100f),
                    items = new List<LootItemEntry>()
                };

                if (sourceTier.items != null)
                {
                    for (int itemIndex = 0; itemIndex < sourceTier.items.Count; itemIndex++)
                    {
                        LootItemEntry sourceEntry = sourceTier.items[itemIndex];
                        if (sourceEntry == null)
                            continue;

                        copiedTier.items.Add(
                            new LootItemEntry
                            {
                                item = sourceEntry.item,
                                minAmount = Mathf.Max(1, sourceEntry.minAmount),
                                maxAmount = Mathf.Max(Mathf.Max(1, sourceEntry.minAmount), sourceEntry.maxAmount),
                                useManualShare = sourceEntry.useManualShare,
                                manualSharePercent = Mathf.Clamp(sourceEntry.manualSharePercent, 0f, 100f)
                            });
                    }
                }

                rarityTiers.Add(copiedTier);
            }
        }

        EnsureDefaultTiers();
    }
}

[Serializable]
public sealed class LootSourceDefinition
{
    [Tooltip("Stable identifier for LootableObject components. Keep this unique inside the config.")]
    public string sourceId = "loot_source";

    [Header("Loot Hub Presentation")]
    [Tooltip("Fallback label used by the Loot Hub until a localized label is assigned below. This is authored here in the Loot Table, never on the world prefab.")]
    public string displayName = "Loot Source";

    [Tooltip("Optional localized label for the Loot Hub. When populated, it takes precedence over the fallback label above at runtime.")]
    public LocalizedString localizedDisplayName = default;

    [Tooltip("Number of item slots shown by this search container. This is authored in the Loot Table, never on the world prefab.")]
    [Min(1)] public int lootSlotCount = 6;

    public LootSourceKind sourceKind;
    public EnemyShooterConfigSO enemyShooterConfig;
    public GameObject lootObjectPrefab;
    public List<LootStagePool> stagePools = new();

    public int LootSlotCount => Mathf.Max(1, lootSlotCount);

    /// <summary>
    /// Resolves the presentation label owned by this source. Localized content
    /// is preferred when the author has assigned a table entry; the plain SO
    /// label remains a safe fallback while localization is being authored.
    /// </summary>
    public string ResolveDisplayName()
    {
        if (localizedDisplayName != null && !localizedDisplayName.IsEmpty)
        {
            string localized = localizedDisplayName.GetLocalizedString();
            if (!string.IsNullOrWhiteSpace(localized))
                return localized;
        }

        return string.IsNullOrWhiteSpace(displayName) ? "Loot" : displayName;
    }

    /// <summary>
    /// Default fallback table. Stages 1-5 use it only when they do not contain
    /// their own exact override.
    /// </summary>
    public LootStagePool CommonPool
    {
        get
        {
            EnsureDefaultPool();
            return FindExactPool(0);
        }
    }

    // Purpose: Ensures default pool is ready before the workflow continues.
    public void EnsureDefaultPool()
    {
        if (stagePools == null)
            stagePools = new List<LootStagePool>();

        for (int i = 0; i < stagePools.Count; i++)
        {
            if (stagePools[i] != null)
                stagePools[i].stageIndex = Mathf.Max(0, stagePools[i].stageIndex);
        }

        LootStagePool defaultPool = FindExactPool(0);
        if (defaultPool == null)
        {
            defaultPool = new LootStagePool();
            stagePools.Add(defaultPool);
        }

        defaultPool.EnsureDefaultTiers();
    }

    // Purpose: Resolves one stage's exact override, falling back to the default table when none exists.
    public LootStagePool ResolvePool(int stageIndex)
    {
        EnsureDefaultPool();
        int normalizedStageIndex = Mathf.Max(0, stageIndex);
        LootStagePool exactPool = normalizedStageIndex > 0
            ? FindExactPool(normalizedStageIndex)
            : null;
        LootStagePool resolvedPool = exactPool ?? FindExactPool(0);
        resolvedPool?.EnsureDefaultTiers();
        return resolvedPool;
    }

    // Purpose: Returns whether this source owns an exact override for the requested stage.
    public bool HasStageOverride(int stageIndex)
    {
        return stageIndex > 0 && FindExactPool(stageIndex) != null;
    }

    // Purpose: Finds or creates one editable table for the requested stage.
    public LootStagePool GetOrCreatePool(int stageIndex, bool copyDefaultPool)
    {
        EnsureDefaultPool();

        int normalizedStageIndex = Mathf.Max(0, stageIndex);
        LootStagePool existing = FindExactPool(normalizedStageIndex);
        if (existing != null)
        {
            existing.EnsureDefaultTiers();
            return existing;
        }

        LootStagePool source = copyDefaultPool ? FindExactPool(0) : null;
        LootStagePool created = source != null
            ? LootStagePool.Clone(source, normalizedStageIndex)
            : new LootStagePool { stageIndex = normalizedStageIndex };
        created.EnsureDefaultTiers();
        stagePools.Add(created);
        return created;
    }

    // Purpose: Removes one exact stage override while retaining the default fallback table.
    public bool RemoveStageOverride(int stageIndex)
    {
        int normalizedStageIndex = Mathf.Max(0, stageIndex);
        if (normalizedStageIndex == 0 || stagePools == null)
            return false;

        for (int i = stagePools.Count - 1; i >= 0; i--)
        {
            LootStagePool candidate = stagePools[i];
            if (candidate != null && candidate.stageIndex == normalizedStageIndex)
            {
                stagePools.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    // Purpose: Copies an authored source table into an exact destination table.
    public LootStagePool CopyPool(int fromStageIndex, int toStageIndex)
    {
        LootStagePool source = ResolvePool(fromStageIndex);
        if (source == null)
            return null;

        LootStagePool destination = GetOrCreatePool(toStageIndex, copyDefaultPool: false);
        destination.CopyFrom(source, toStageIndex);
        return destination;
    }

    // Purpose: Finds the requested exact authored table without applying a fallback.
    public LootStagePool FindExactPool(int stageIndex)
    {
        if (stagePools == null)
            return null;

        for (int i = 0; i < stagePools.Count; i++)
        {
            LootStagePool pool = stagePools[i];
            if (pool != null && pool.stageIndex == Mathf.Max(0, stageIndex))
                return pool;
        }

        return null;
    }

}

public readonly struct LootRollResult
{
    public ItemSO Item { get; }
    public int Amount { get; }
    public PlayerShopItemRarity Rarity { get; }

    public LootRollResult(ItemSO item, int amount, PlayerShopItemRarity rarity)
    {
        Item = item;
        Amount = Mathf.Max(1, amount);
        Rarity = rarity;
    }
}

[CreateAssetMenu(
    fileName = "LootProgressionConfig",
    menuName = "Game/Loot/Loot Progression Config")]
public sealed class LootProgressionConfigSO : ScriptableObject
{
    [SerializeField] private List<LootSourceDefinition> _sources = new();

    [Header("Global Valuable Loot")]
    [Tooltip("Pure sale-only items shared by every registered enemy and lootable-object source. These entries are rolled in addition to that source's authored loot pool, so adding a valuable never replaces a weapon, bullet, or quest drop.")]
    [SerializeField] private List<ItemSO> _valuableLootItems = new();

    [Tooltip("Chance for one additional sale-only valuable roll after a valid enemy or lootable object is opened. Set to zero to temporarily disable the shared valuable catalog without deleting it.")]
    [SerializeField, Range(0f, 1f)] private float _valuableLootRollChance = 0.35f;

    public IReadOnlyList<LootSourceDefinition> Sources => _sources;
    public IReadOnlyList<ItemSO> ValuableLootItems => _valuableLootItems;
    public float ValuableLootRollChance => Mathf.Clamp01(_valuableLootRollChance);

    // Purpose: Creates the source owned by this workflow.
    public LootSourceDefinition CreateSource(LootSourceKind sourceKind)
    {
        _sources ??= new List<LootSourceDefinition>();

        LootSourceDefinition source = new LootSourceDefinition
        {
            sourceKind = sourceKind,
            sourceId = sourceKind == LootSourceKind.EnemyShooter
                ? "enemy_shooter"
                : "loot_object",
            displayName = sourceKind == LootSourceKind.EnemyShooter
                ? "Enemy Shooter Loot"
                : "Lootable Object"
        };
        source.EnsureDefaultPool();
        _sources.Add(source);
        return source;
    }

    // Purpose: Removes the requested source from this component.
    public bool RemoveSource(LootSourceDefinition source)
    {
        return source != null && _sources != null && _sources.Remove(source);
    }

    // Purpose: Finds the requested enemy source.
    public LootSourceDefinition FindEnemySource(EnemyShooterConfigSO enemyConfig)
    {
        if (enemyConfig == null || _sources == null)
            return null;

        for (int i = 0; i < _sources.Count; i++)
        {
            LootSourceDefinition source = _sources[i];
            if (source != null &&
                source.sourceKind == LootSourceKind.EnemyShooter &&
                source.enemyShooterConfig == enemyConfig)
            {
                return source;
            }
        }

        return null;
    }

    // Purpose: Finds the requested loot object source.
    public LootSourceDefinition FindLootObjectSource(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || _sources == null)
            return null;

        for (int i = 0; i < _sources.Count; i++)
        {
            LootSourceDefinition source = _sources[i];
            if (source != null &&
                source.sourceKind == LootSourceKind.LootableObject &&
                string.Equals(source.sourceId, sourceId, StringComparison.Ordinal))
            {
                return source;
            }
        }

        return null;
    }

    // Purpose: Attempts to roll enemy and reports whether the request succeeded.
    public bool TryRollEnemy(
        EnemyShooterConfigSO enemyConfig,
        System.Random random,
        List<LootRollResult> results)
    {
        return TryRollEnemy(enemyConfig, random, results, 0);
    }

    // Purpose: Rolls one enemy source using the exact current-stage override when authored.
    public bool TryRollEnemy(
        EnemyShooterConfigSO enemyConfig,
        System.Random random,
        List<LootRollResult> results,
        int stageIndex)
    {
        return TryRoll(FindEnemySource(enemyConfig), random, results, null, stageIndex);
    }

    // Purpose: Attempts to roll loot object and reports whether the request succeeded.
    public bool TryRollLootObject(
        string sourceId,
        System.Random random,
        List<LootRollResult> results)
    {
        return TryRollLootObject(sourceId, random, results, null, 0);
    }

    /// <summary>
    /// Rolls one authored loot-object source using an optional locked reward
    /// profile. The profile never swaps the source or its item entries; it only
    /// applies extra source-pool rolls and rarity weights to that same pool.
    /// </summary>
    public bool TryRollLootObject(
        string sourceId,
        System.Random random,
        List<LootRollResult> results,
        LockedLootProfileSO lockedLootProfile)
    {
        return TryRollLootObject(sourceId, random, results, lockedLootProfile, 0);
    }

    // Purpose: Rolls one loot object source using the exact current-stage override when authored.
    public bool TryRollLootObject(
        string sourceId,
        System.Random random,
        List<LootRollResult> results,
        LockedLootProfileSO lockedLootProfile,
        int stageIndex)
    {
        return TryRoll(FindLootObjectSource(sourceId), random, results, lockedLootProfile, stageIndex);
    }

    // Purpose: Attempts to roll and reports whether the request succeeded.
    public bool TryRoll(
        LootSourceDefinition source,
        System.Random random,
        List<LootRollResult> results)
    {
        return TryRoll(source, random, results, null, 0);
    }

    // Purpose: Applies an optional locked-container modifier to one source roll.
    private bool TryRoll(
        LootSourceDefinition source,
        System.Random random,
        List<LootRollResult> results,
        LockedLootProfileSO lockedLootProfile,
        int stageIndex)
    {
        if (results == null)
            return false;

        results.Clear();

        if (source == null)
            return false;

        random ??= new System.Random();

        // Source-specific items remain the primary roll. Some template sources
        // intentionally start empty; they can still produce a valuable through
        // the shared catalog below while their bespoke loot is authored later.
        LootStagePool pool = source.ResolvePool(stageIndex);
        if (pool?.rarityTiers != null && pool.rarityTiers.Count > 0)
        {
            int minRolls = Mathf.Max(1, pool.minRollCount);
            int maxRolls = Mathf.Max(minRolls, pool.maxRollCount);
            int rollCount = random.Next(minRolls, maxRolls + 1);
            if (lockedLootProfile != null)
                rollCount += lockedLootProfile.GetAdditionalRollCount(random);

            StageEquipmentLootDistribution equipment = null;
            if (StageEquipmentLootDistribution.AppliesToStage(stageIndex))
            {
                equipment = GetStageEquipmentDistribution(pool, stageIndex, lockedLootProfile);
                if (!equipment.HasLeveledEquipment) equipment = null;
            }

            for (int i = 0; i < rollCount; i++)
            {
                if (equipment != null)
                {
                    var row = equipment.Pick(random);
                    if (row == null) continue;
                    int min = Mathf.Max(1, row.Entry.minAmount), max = Mathf.Max(min, row.Entry.maxAmount);
                    int amount = min + (int)(random.NextDouble() * ((long)max - min + 1));
                    PassiveInstanceRuntime.Append(results, row.Entry.item, amount, row.Rarity, random);
                    continue;
                }
                if (!TryRollOne(pool, random, lockedLootProfile, out LootRollResult result))
                    continue;

                PassiveInstanceRuntime.Append(results, result.Item, result.Amount, result.Rarity, random);
            }
        }

        // A valuable is deliberately an additive roll. It is not a shop offer,
        // does not grant gameplay effects, and cannot consume another item's
        // source-pool chance.
        TryAppendGlobalValuable(random, results);

        return results.Count > 0;
    }

    public static StageEquipmentLootDistribution GetStageEquipmentDistribution(LootStagePool pool, int stageIndex, LockedLootProfileSO profile = null)
    {
        var distribution = new StageEquipmentLootDistribution();
        if (pool?.rarityTiers != null)
            foreach (var tier in pool.rarityTiers)
                distribution.AddTier(tier, new LootCategorySelection(tier, null), GetTierWeight(tier, profile));
        distribution.ApplyStage(stageIndex);
        return distribution;
    }

    /// <summary>
    /// Appends the optional global sale-only roll for a world LootObjectSO.
    /// Object-specific drops are now owned by LootTableSO; this method keeps
    /// the intentionally shared valuable catalogue in one place.
    /// </summary>
    public bool TryAppendGlobalValuable(System.Random random, List<LootRollResult> results)
    {
        if (results == null)
            return false;

        random ??= new System.Random();
        if (random.NextDouble() > ValuableLootRollChance ||
            !TryRollValuable(random, out LootRollResult valuableResult))
        {
            return false;
        }

        results.Add(valuableResult);
        return true;
    }

    /// <summary>
    /// Rolls the common sale-only catalog with the same Normal/Rare/Unique/
    /// Legendary ratio used by authored source pools. Items inside a rarity are
    /// split evenly, so designers can add/remove template valuables without
    /// recalculating individual shares.
    /// </summary>
    private bool TryRollValuable(System.Random random, out LootRollResult result)
    {
        result = default;

        if (_valuableLootItems == null || _valuableLootItems.Count == 0)
            return false;

        PlayerShopItemRarity[] rarities =
        {
            PlayerShopItemRarity.Normal,
            PlayerShopItemRarity.Rare,
            PlayerShopItemRarity.Unique,
            PlayerShopItemRarity.Legendary
        };

        float[] weights = { 65f, 25f, 8f, 2f };
        float totalWeight = 0f;

        for (int i = 0; i < rarities.Length; i++)
        {
            if (CountValuables(rarities[i]) > 0)
                totalWeight += weights[i];
        }

        if (totalWeight <= 0f)
            return false;

        float rarityRoll = (float)(random.NextDouble() * totalWeight);
        PlayerShopItemRarity selectedRarity = PlayerShopItemRarity.Normal;

        for (int i = 0; i < rarities.Length; i++)
        {
            if (CountValuables(rarities[i]) <= 0)
                continue;

            rarityRoll -= weights[i];
            if (rarityRoll <= 0f)
            {
                selectedRarity = rarities[i];
                break;
            }
        }

        int selectedIndex = random.Next(CountValuables(selectedRarity));
        for (int i = 0; i < _valuableLootItems.Count; i++)
        {
            ItemSO item = _valuableLootItems[i];
            if (item == null || item.Rarity != selectedRarity)
                continue;

            if (selectedIndex-- > 0)
                continue;

            result = new LootRollResult(item, 1, selectedRarity);
            return true;
        }

        return false;
    }

    // Purpose: Counts the available valuables.
    private int CountValuables(PlayerShopItemRarity rarity)
    {
        if (_valuableLootItems == null)
            return 0;

        int count = 0;
        for (int i = 0; i < _valuableLootItems.Count; i++)
        {
            ItemSO item = _valuableLootItems[i];
            if (item != null && item.Rarity == rarity)
                count++;
        }

        return count;
    }

    // Purpose: Returns the current resolved item share percent.
    public static float GetResolvedItemSharePercent(LootRarityTier tier, LootItemEntry entry)
    {
        if (tier == null || entry == null || tier.items == null || !tier.items.Contains(entry))
            return 0f;

        float manualTotal = 0f;
        int automaticCount = 0;

        for (int i = 0; i < tier.items.Count; i++)
        {
            LootItemEntry candidate = tier.items[i];
            if (candidate == null || candidate.item == null)
                continue;

            if (candidate.useManualShare)
                manualTotal += Mathf.Max(0f, candidate.manualSharePercent);
            else
                automaticCount++;
        }

        if (entry.item == null)
            return 0f;

        if (manualTotal > 100f)
        {
            return entry.useManualShare && manualTotal > 0f
                ? Mathf.Max(0f, entry.manualSharePercent) / manualTotal * 100f
                : 0f;
        }

        if (entry.useManualShare)
            return Mathf.Max(0f, entry.manualSharePercent);

        return automaticCount > 0
            ? Mathf.Max(0f, 100f - manualTotal) / automaticCount
            : 0f;
    }

    // Purpose: Attempts to roll one and reports whether the request succeeded.
    private static bool TryRollOne(
        LootStagePool pool,
        System.Random random,
        LockedLootProfileSO lockedLootProfile,
        out LootRollResult result)
    {
        result = default;
        LootRarityTier tier = PickTier(pool.rarityTiers, random, lockedLootProfile);

        if (tier == null)
            return false;

        LootItemEntry entry = PickItem(tier, random);
        if (entry == null || entry.item == null)
            return false;

        int min = Mathf.Max(1, entry.minAmount);
        int max = Mathf.Max(min, entry.maxAmount);
        int amount = random.Next(min, max + 1);
        result = new LootRollResult(entry.item, amount, tier.rarity);
        return true;
    }

    // Purpose: Performs the pick tier workflow for this component.
    private static LootRarityTier PickTier(
        List<LootRarityTier> tiers,
        System.Random random,
        LockedLootProfileSO lockedLootProfile)
    {
        float total = 0f;

        for (int i = 0; i < tiers.Count; i++)
        {
            LootRarityTier tier = tiers[i];
            if (!HasValidItems(tier))
                continue;

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

    // Purpose: Resolves one tier's chance after an optional locked-loot profile modifier.
    private static float GetTierWeight(LootRarityTier tier, LockedLootProfileSO lockedLootProfile)
    {
        if (tier == null)
            return 0f;

        float multiplier = lockedLootProfile != null
            ? lockedLootProfile.GetRarityWeightMultiplier(tier.rarity)
            : 1f;
        return Mathf.Max(0f, tier.chancePercent) * Mathf.Max(0f, multiplier);
    }

    // Purpose: Performs the pick item workflow for this component.
    private static LootItemEntry PickItem(LootRarityTier tier, System.Random random)
    {
        if (tier?.items == null)
            return null;

        float total = 0f;
        for (int i = 0; i < tier.items.Count; i++)
            total += GetResolvedItemSharePercent(tier, tier.items[i]);

        if (total <= 0f)
            return null;

        float roll = (float)(random.NextDouble() * total);
        for (int i = 0; i < tier.items.Count; i++)
        {
            LootItemEntry entry = tier.items[i];
            float share = GetResolvedItemSharePercent(tier, entry);
            if (share <= 0f)
                continue;

            roll -= share;
            if (roll <= 0f)
                return entry;
        }

        return null;
    }

    // Purpose: Checks whether this component has valid items.
    private static bool HasValidItems(LootRarityTier tier)
    {
        if (tier?.items == null || tier.items.Count == 0)
            return false;

        for (int i = 0; i < tier.items.Count; i++)
        {
            if (tier.items[i]?.item != null &&
                GetResolvedItemSharePercent(tier, tier.items[i]) > 0f)
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        if (_sources == null)
            _sources = new List<LootSourceDefinition>();

        if (_valuableLootItems == null)
            _valuableLootItems = new List<ItemSO>();

        _valuableLootRollChance = Mathf.Clamp01(_valuableLootRollChance);

        for (int i = 0; i < _sources.Count; i++)
        {
            LootSourceDefinition source = _sources[i];
            if (source == null)
                continue;

            source.EnsureDefaultPool();
        }
    }
}
