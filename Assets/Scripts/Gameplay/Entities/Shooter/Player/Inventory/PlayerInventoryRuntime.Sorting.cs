using System;
using System.Collections.Generic;

public enum InventorySortMode { Compact, Rarity, Category }

public partial class PlayerInventoryRuntime
{
    // Reorder stack instances, never rebuild/merge them: condition and stack identity survive.
    public bool SortInventory(bool byRarity)
        => SortInventory(byRarity ? InventorySortMode.Rarity : InventorySortMode.Compact);

    public bool SortInventory(InventorySortMode mode)
    {
        if (GetComponent<PlayerConsumableUseRuntime>()?.IsUsing == true) return false;
        bool changed = SortStackRange(_items, 0, PocketCapacity, mode);
        changed |= SortStackRange(_items, PocketCapacity, _items.Count - PocketCapacity, mode);
        if (changed) NotifyChanged();
        return changed;
    }

    public static bool SortStackRange(List<PlayerInventoryItemStack> stacks, int start, int count, bool byRarity)
        => SortStackRange(stacks, start, count, byRarity ? InventorySortMode.Rarity : InventorySortMode.Compact);

    public static bool SortStackRange(List<PlayerInventoryItemStack> stacks, int start, int count, InventorySortMode mode)
    {
        if (stacks == null || start < 0 || count <= 0 || start >= stacks.Count) return false;
        count = Math.Min(count, stacks.Count - start);
        var ordered = new List<(PlayerInventoryItemStack stack, int index)>(count);
        for (int i = 0; i < count; i++) ordered.Add((stacks[start + i], i));
        ordered.Sort((a, b) =>
        {
            bool emptyA = a.stack == null || a.stack.IsEmpty;
            bool emptyB = b.stack == null || b.stack.IsEmpty;
            if (emptyA != emptyB) return emptyA ? 1 : -1;
            if (mode == InventorySortMode.Category && !emptyA)
            {
                ItemSO itemA = a.stack.Item, itemB = b.stack.Item;
                int category = itemA.Kind.CompareTo(itemB.Kind);
                if (category == 0 && itemA is ValuableItemSO va && itemB is ValuableItemSO vb)
                {
                    category = (ValuableCategoryCatalogSO.Active?.Order(va.ValuableCategoryId) ?? int.MaxValue)
                        .CompareTo(ValuableCategoryCatalogSO.Active?.Order(vb.ValuableCategoryId) ?? int.MaxValue);
                    if (category == 0) category = StringComparer.Ordinal.Compare(va.ValuableCategoryId, vb.ValuableCategoryId);
                }
                if (category == 0) category = StringComparer.Ordinal.Compare(LootItemCategories.GetKey(itemA), LootItemCategories.GetKey(itemB));
                if (category != 0) return category;
            }
            if (mode != InventorySortMode.Compact && !emptyA)
            {
                int rank = RarityRank(b.stack.Item.Rarity).CompareTo(RarityRank(a.stack.Item.Rarity));
                if (rank != 0) return rank;
                if (mode == InventorySortMode.Category)
                {
                    int name = StringComparer.OrdinalIgnoreCase.Compare(a.stack.Item.name, b.stack.Item.name);
                    if (name != 0) return name;
                }
            }
            return a.index.CompareTo(b.index);
        });
        bool changed = false;
        for (int i = 0; i < count; i++)
        {
            changed |= !ReferenceEquals(stacks[start + i], ordered[i].stack);
            stacks[start + i] = ordered[i].stack;
        }
        return changed;
    }

    private static int RarityRank(PlayerShopItemRarity rarity) => rarity switch
    {
        PlayerShopItemRarity.Mythic => 4,
        PlayerShopItemRarity.Legendary => 3,
        PlayerShopItemRarity.Unique => 2,
        PlayerShopItemRarity.Rare => 1,
        _ => 0
    };
}
