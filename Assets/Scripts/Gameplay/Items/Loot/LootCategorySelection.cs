using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LootCategoryWeight
{
    public string categoryKey;
    [Min(0)] public float weight = 1f;
}

[Serializable]
public sealed class LootRarityCategorySettings
{
    public PlayerShopItemRarity rarity;
    public bool useCustomCategoryWeights;
    public List<LootCategoryWeight> categories = new();

    public LootCategoryWeight Find(string category)
        => categories?.Find(rule => rule != null && rule.categoryKey == category);
    public LootCategoryWeight FindEffective(string category) => Find(category) ??
        (category == LootItemCategories.CurrencyKey ? Find("Currency") : null);
    public float GetWeight(string category) => (float)LootCategorySelection.Positive(FindEffective(category)?.weight ?? 1f);
    public void SetWeight(string category, float value)
    {
        categories ??= new();
        var rule = Find(category);
        if (rule == null) { rule = new LootCategoryWeight { categoryKey = category }; categories.Add(rule); }
        rule.weight = (float)LootCategorySelection.Positive(value);
    }
}

/// <summary>Conditional category then item distribution. Read-only; never mutates an authored tier.</summary>
public sealed class LootCategorySelection
{
    public sealed class Group
    {
        public string Key;
        public readonly List<LootItemEntry> Entries = new();
        public readonly List<double> ItemWeights = new();
        public double ItemTotal, CategoryWeight;
        public bool Eligible => ItemTotal > 0d && CategoryWeight > 0d;
    }
    public readonly List<Group> Groups = new();
    public double TotalWeight { get; private set; }
    public bool HasCandidates => TotalWeight > 0d;
    public static double Positive(float value) => float.IsNaN(value) || float.IsInfinity(value) || value <= 0f ? 0d : value;

    public LootCategorySelection(LootRarityTier tier, LootRarityCategorySettings settings)
    {
        if (tier?.items == null) return;
        foreach (var entry in tier.items)
        {
            if (entry?.item == null || entry.item is PassiveItemSO) continue;
            string key = LootItemCategories.GetKey(entry.item);
            var group = Groups.Find(g => g.Key == key);
            if (group == null) { group = new Group { Key = key }; Groups.Add(group); }
            group.Entries.Add(entry);
        }
        bool custom = settings?.useCustomCategoryWeights == true;
        foreach (var group in Groups)
        {
            // Auto mode keeps the old tier-wide marginal probabilities. Custom mode
            // reserves each manual item share inside ONLY its category.
            var scope = custom ? group.Entries : tier.items;
            if (custom && settings.Find("Valuable") != null &&
                (group.Key == "Valuable" || group.Key.StartsWith("Valuable:", StringComparison.Ordinal)) &&
                (group.Key == "Valuable" || settings.FindEffective(group.Key) == null))
                scope = tier.items.FindAll(e => e?.item is ValuableItemSO &&
                    (LootItemCategories.GetKey(e.item) == "Valuable" || settings.FindEffective(LootItemCategories.GetKey(e.item)) == null));
            foreach (var entry in group.Entries)
            {
                double weight = ItemShare(scope, entry);
                group.ItemWeights.Add(weight); group.ItemTotal += weight;
            }
            group.CategoryWeight = custom ? settings.GetWeight(group.Key) : group.ItemTotal;
        }
        // Old tables authored one aggregate Valuable weight. Split that budget over
        // its new children until an author explicitly edits a child weight.
        double legacyTotal = 0d;
        foreach (var group in Groups)
            if ((group.Key == "Valuable" || group.Key.StartsWith("Valuable:", StringComparison.Ordinal)) &&
                (group.Key == "Valuable" || settings?.FindEffective(group.Key) == null)) legacyTotal += group.ItemTotal;
        foreach (var group in Groups)
        {
            if (custom && settings.Find("Valuable") != null && legacyTotal > 0d &&
                (group.Key == "Valuable" || group.Key.StartsWith("Valuable:", StringComparison.Ordinal)) &&
                (group.Key == "Valuable" || settings.FindEffective(group.Key) == null))
                group.CategoryWeight = settings.GetWeight("Valuable") * group.ItemTotal / legacyTotal;
            if (group.Eligible) TotalWeight += group.CategoryWeight;
        }
    }

    private static double ItemShare(List<LootItemEntry> entries, LootItemEntry entry)
    {
        if (entry?.item == null) return 0d;
        double manual = 0d; int automatic = 0;
        foreach (var candidate in entries)
        {
            if (candidate?.item == null || candidate.item is PassiveItemSO) continue;
            if (candidate.useManualShare) manual += Positive(candidate.manualSharePercent); else automatic++;
        }
        if (manual > 100d) return entry.useManualShare ? Positive(entry.manualSharePercent) / manual * 100d : 0d;
        if (entry.useManualShare) return Positive(entry.manualSharePercent);
        return automatic > 0 ? Math.Max(0d, 100d - manual) / automatic : 0d;
    }

    public float CategoryPercent(string key)
    {
        var group = Groups.Find(g => g.Key == key);
        return group?.Eligible == true && TotalWeight > 0d ? (float)(group.CategoryWeight / TotalWeight * 100d) : 0f;
    }
    public float ItemPercentInCategory(LootItemEntry entry)
    {
        if (entry?.item == null) return 0f;
        var group = Groups.Find(g => g.Key == LootItemCategories.GetKey(entry.item));
        int index = group?.Entries.IndexOf(entry) ?? -1;
        return index >= 0 && group.ItemTotal > 0d ? (float)(group.ItemWeights[index] / group.ItemTotal * 100d) : 0f;
    }
    public float ItemPercentInTier(LootItemEntry entry) => entry?.item != null
        ? CategoryPercent(LootItemCategories.GetKey(entry.item)) * ItemPercentInCategory(entry) / 100f : 0f;

    public LootItemEntry Pick(System.Random random)
    {
        if (!HasCandidates) return null;
        double pick = random.NextDouble() * TotalWeight;
        Group selected = null;
        foreach (var group in Groups)
        {
            if (!group.Eligible) continue;
            selected = group;
            if (pick < group.CategoryWeight) break;
            pick -= group.CategoryWeight;
        }
        if (selected == null) return null;
        pick = random.NextDouble() * selected.ItemTotal;
        LootItemEntry fallback = null;
        for (int i = 0; i < selected.Entries.Count; i++)
        {
            double weight = selected.ItemWeights[i];
            if (weight <= 0d) continue;
            fallback = selected.Entries[i];
            if (pick < weight) return fallback;
            pick -= weight;
        }
        return fallback;
    }
}
