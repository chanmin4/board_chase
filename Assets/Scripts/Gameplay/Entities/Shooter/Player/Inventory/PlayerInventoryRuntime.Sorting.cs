using System;
using System.Collections.Generic;

public partial class PlayerInventoryRuntime
{
    // Reorder stack instances, never rebuild/merge them: condition and stack identity survive.
    public bool SortInventory(bool byRarity)
    {
        if (GetComponent<PlayerConsumableUseRuntime>()?.IsUsing == true) return false;
        bool changed = SortStackRange(_items, 0, PocketCapacity, byRarity);
        changed |= SortStackRange(_items, PocketCapacity, _items.Count - PocketCapacity, byRarity);
        if (changed) NotifyChanged();
        return changed;
    }

    public static bool SortStackRange(List<PlayerInventoryItemStack> stacks, int start, int count, bool byRarity)
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
            if (byRarity && !emptyA)
            {
                int rank = RarityRank(b.stack.Item.Rarity).CompareTo(RarityRank(a.stack.Item.Rarity));
                if (rank != 0) return rank;
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
        PlayerShopItemRarity.Legendary => 3,
        PlayerShopItemRarity.Unique => 2,
        PlayerShopItemRarity.Rare => 1,
        _ => 0
    };
}
