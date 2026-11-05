using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>View-only ordering. Source indices stay attached to rows so delete/edit target the right entry.</summary>
internal static class LootMakerItemView
{
    internal readonly struct Row
    {
        internal Row(LootItemEntry entry, int sourceIndex)
        {
            Entry = entry;
            SourceIndex = sourceIndex;
            CategoryOrder = entry.item != null ? ItemMakerWindow.LootCategoryOrder(entry.item) : int.MaxValue;
            CategoryLabel = entry.item != null ? ItemMakerWindow.LootCategoryLabel(entry.item) : "Missing Items";
            CategoryKey = entry.item != null ? ItemMakerWindow.LootCategoryKey(entry.item) : "MissingItems";
        }
        internal LootItemEntry Entry { get; }
        internal int SourceIndex { get; }
        internal int CategoryOrder { get; }
        internal string CategoryLabel { get; }
        internal string CategoryKey { get; }
    }

    internal static List<LootRarityTier> SortedTiers(IEnumerable<LootRarityTier> tiers) =>
        (tiers ?? Enumerable.Empty<LootRarityTier>()).Where(t => t != null)
            .OrderBy(t => ItemMakerCatalogView.RarityOrder(t.rarity)).ThenBy(t => (int)t.rarity).ToList();

    internal static List<Row> SortedRows(LootRarityTier tier) =>
        (tier?.items ?? new List<LootItemEntry>()).Select((entry, index) => (entry, index))
            .Where(pair => pair.entry != null).Select(pair => new Row(pair.entry, pair.index))
            .OrderBy(row => row.CategoryOrder)
            .ThenBy(row => row.Entry.item is ValuableItemSO valuable ? ValuableCategoryCatalogSO.Active?.Order(valuable.ValuableCategoryId) ?? int.MaxValue : 0)
            .ThenBy(row => row.CategoryKey, StringComparer.Ordinal)
            .ThenBy(row => row.Entry.item != null ? row.Entry.item.name : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Entry.item != null ? row.Entry.item.name : string.Empty, StringComparer.Ordinal)
            .ThenBy(row => AssetDatabase.GetAssetPath(row.Entry.item), StringComparer.Ordinal)
            .ThenBy(row => row.SourceIndex).ToList();

    internal sealed class CandidateGroup
    {
        internal string Key, Label;
        internal ItemSO[] Items;
    }

    // Candidate browsing is independent from the already-authored tier list.
    internal static List<CandidateGroup> CandidateGroups(IEnumerable<ItemSO> items,
        ISet<ItemSO> registered, bool onlyUnregistered, string search, string categoryKey = "")
    {
        string term = (search ?? "").Trim();
        return (items ?? Enumerable.Empty<ItemSO>()).Where(item => item != null).Distinct()
            .Where(item => !onlyUnregistered || registered == null || !registered.Contains(item))
            .Where(item => string.IsNullOrEmpty(categoryKey) || ItemMakerWindow.LootCategoryKey(item) == categoryKey)
            .Where(item => term.Length == 0 || item.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                CandidateLabel(item).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                ItemMakerWindow.LootCategoryKey(item).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            .GroupBy(ItemMakerWindow.LootCategoryKey)
            .OrderBy(group => ItemMakerWindow.LootCategoryOrder(group.First()))
            .ThenBy(group => group.First() is ValuableItemSO valuable ? ValuableCategoryCatalogSO.Active?.Order(valuable.ValuableCategoryId) ?? int.MaxValue : 0)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new CandidateGroup
            {
                Key = group.Key, Label = CandidateLabel(group.First()),
                Items = group.OrderBy(item => ItemMakerCatalogView.RarityOrder(item.Rarity))
                    .ThenBy(item => (int)item.Rarity)
                    .ThenBy(item => item.name, Comparer<string>.Create(EditorUtility.NaturalCompare))
                    .ThenBy(item => AssetDatabase.GetAssetPath(item), StringComparer.Ordinal).ToArray()
            }).ToList();
    }

    private static string CandidateLabel(ItemSO item)
    {
        string label = ItemMakerWindow.LootCategoryLabel(item);
        return item is BulletSO ? "Bullets / " + label : item is RangedWeaponSO ? "Weapons / " + label : label;
    }

    internal static IEnumerable<ItemSO> ExpandedCandidates(IEnumerable<CandidateGroup> groups, Func<string, bool> expanded) =>
        groups.Where(group => expanded(group.Key)).SelectMany(group => group.Items);

    internal static string CategoryPreferenceKey(LootTableSO table, PlayerShopItemRarity rarity, string category)
    {
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(table));
        string identity = string.IsNullOrEmpty(guid) ? "temporary-" + table.GetInstanceID() : guid;
        return "BoardChase.LootMaker.ItemCategories.v1." + Application.dataPath + "." + identity + "." + rarity + "." + category;
    }

    internal static bool IsCategoryExpanded(LootTableSO table, PlayerShopItemRarity rarity, string category) =>
        EditorPrefs.GetBool(CategoryPreferenceKey(table, rarity, category), true);

    internal static void SetCategoryExpanded(LootTableSO table, PlayerShopItemRarity rarity, string category, bool expanded) =>
        EditorPrefs.SetBool(CategoryPreferenceKey(table, rarity, category), expanded);

    internal static void SetAllCategoriesExpanded(LootTableSO table, bool expanded)
    {
        foreach (var tier in SortedTiers(table.Pool.rarityTiers))
            foreach (string category in SortedRows(tier).Select(row => row.CategoryKey).Distinct())
                SetCategoryExpanded(table, tier.rarity, category, expanded);
    }
}
