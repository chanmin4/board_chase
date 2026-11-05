using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>View-only ordering and foldouts. Never changes an item's serialized rarity or name.</summary>
internal static class ItemMakerCatalogView
{
    internal static int RarityOrder(PlayerShopItemRarity rarity) => rarity switch
    {
        PlayerShopItemRarity.Normal => 0,
        PlayerShopItemRarity.Rare => 1,
        PlayerShopItemRarity.Unique => 2,
        PlayerShopItemRarity.Legendary => 3,
        _ => 4
    };

    internal static int Compare(ItemSO left, ItemSO right)
    {
        if (left == right) return 0;
        if (left == null) return 1;
        if (right == null) return -1;
        int order = RarityOrder(left.Rarity).CompareTo(RarityOrder(right.Rarity));
        if (order == 0) order = ((int)left.Rarity).CompareTo((int)right.Rarity);
        if (order == 0 && left is ValuableItemSO a && right is ValuableItemSO b)
        {
            order = (ValuableCategoryCatalogSO.Active?.Order(a.ValuableCategoryId) ?? int.MaxValue)
                .CompareTo(ValuableCategoryCatalogSO.Active?.Order(b.ValuableCategoryId) ?? int.MaxValue);
            if (order == 0) order = StringComparer.Ordinal.Compare(a.ValuableCategoryId, b.ValuableCategoryId);
        }
        if (order == 0) order = StringComparer.OrdinalIgnoreCase.Compare(left.name, right.name);
        if (order == 0) order = StringComparer.Ordinal.Compare(left.name, right.name);
        if (order == 0) order = StringComparer.Ordinal.Compare(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right));
        return order;
    }

    internal static string PreferenceKey(PlayerShopItemRarity rarity, string scope = "Catalog") =>
        "BoardChase.ItemMaker.RarityGroups.v1." + Application.dataPath + "." + scope + "." + rarity;

    internal static bool IsExpanded(PlayerShopItemRarity rarity, string scope = "Catalog") =>
        EditorPrefs.GetBool(PreferenceKey(rarity, scope), true);

    internal static void SetExpanded(PlayerShopItemRarity rarity, bool expanded, string scope = "Catalog") =>
        EditorPrefs.SetBool(PreferenceKey(rarity, scope), expanded);

    internal static List<ItemSO> ExpandedItems(IEnumerable<ItemSO> sortedItems, string scope = "Catalog") =>
        sortedItems.Where(item => item != null && IsExpanded(item.Rarity, scope)).ToList();

    internal static void SetAllExpanded(bool expanded, IEnumerable<ItemSO> items, string scope = "Catalog")
    {
        // Include known empty groups too, so switching categories preserves Expand/Collapse All.
        foreach (var rarity in Enum.GetValues(typeof(PlayerShopItemRarity)).Cast<PlayerShopItemRarity>()
                     .Concat(items.Where(item => item != null).Select(item => item.Rarity)).Distinct())
            SetExpanded(rarity, expanded, scope);
    }

    internal static Color RarityColor(PlayerShopItemRarity rarity) => rarity switch
    {
        PlayerShopItemRarity.Normal => new Color(.38f, .55f, .57f),
        PlayerShopItemRarity.Rare => new Color(.28f, .52f, .86f),
        PlayerShopItemRarity.Unique => new Color(.65f, .40f, .82f),
        PlayerShopItemRarity.Legendary => new Color(.88f, .60f, .22f),
        _ => Color.gray
    };
}
