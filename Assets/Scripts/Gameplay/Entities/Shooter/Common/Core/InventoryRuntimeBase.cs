/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Manages InventoryRuntimeBase item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

public abstract class InventoryRuntimeBase : MonoBehaviour
{
    // Purpose: Normalizes inventory storage to a safe authored value.
    protected void NormalizeInventoryStorage(
        List<PlayerInventoryItemStack> items,
        int capacity)
    {
        if (items == null)
            return;

        capacity = Mathf.Max(0, capacity);

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
            {
                PlayerInventoryItemStack empty = new();
                empty.Clear();
                items[i] = empty;
            }
        }

        while (items.Count < capacity)
        {
            PlayerInventoryItemStack empty = new();
            empty.Clear();
            items.Add(empty);
        }

    }

    // Purpose: Attempts to find empty inventory slot and reports whether the request succeeded.
    protected bool TryFindEmptyInventorySlot(
        List<PlayerInventoryItemStack> items,
        int capacity,
        out int index)
    {
        NormalizeInventoryStorage(items, capacity);

        for (int i = 0; i < capacity; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    // Purpose: Counts the available used inventory slots.
    protected int CountUsedInventorySlots(List<PlayerInventoryItemStack> items)
    {
        if (items == null)
            return 0;

        int count = 0;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && !items[i].IsEmpty)
                count++;
        }

        return count;
    }

    // Purpose: Resolves the inventory max stack needed by this component.
    protected static int ResolveInventoryMaxStack(ItemSO item)
    {
        return item != null ? Mathf.Max(1, item.MaxStack) : 1;
    }

    // Purpose: Checks whether stack inventory item is currently permitted.
    protected static bool CanStackInventoryItem(ItemSO item)
    {
        return ResolveInventoryMaxStack(item) > 1;
    }
}
