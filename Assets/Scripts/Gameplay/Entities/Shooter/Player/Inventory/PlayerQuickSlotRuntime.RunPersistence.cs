using System.Collections.Generic;

public partial class PlayerQuickSlotRuntime
{
    public void CaptureUsableRunSlots(List<PlayerRunInventorySlotSaveData> target)
    {
        target.Clear(); NormalizeBindings();
        for (int i = 2; i < SlotCount; i++)
        {
            var stack = CreateStoredStack(_bindings[i]);
            if (!stack.HasItem) continue;
            target.Add(new PlayerRunInventorySlotSaveData { slotIndex = i, itemGuid = stack.item.Guid,
                amount = stack.amount, weaponCurrentDurability = stack.weaponCurrentDurability,
                weaponMaxDurability = stack.weaponMaxDurability });
        }
    }

    public bool CanRestoreUsableRunSlots(List<PlayerRunInventorySlotSaveData> source, RuntimeContentCatalogSO catalog)
    {
        if (source == null) return true;
        if (catalog == null) return false;
        var slots = new HashSet<int>();
        foreach (var entry in source)
            if (entry == null || entry.slotIndex < 2 || entry.slotIndex >= SlotCount || entry.amount <= 0 ||
                !slots.Add(entry.slotIndex) || !catalog.TryResolveItem(entry.itemGuid, out var item) || item is not UsableItemSO)
                return false;
        return true;
    }

    public void RestoreUsableRunSlots(List<PlayerRunInventorySlotSaveData> source, RuntimeContentCatalogSO catalog)
    {
        if (!CanRestoreUsableRunSlots(source, catalog)) return;
        NormalizeBindings();
        for (int i = 2; i < SlotCount; i++) _bindings[i].Clear();
        if (source != null) foreach (var entry in source)
        {
            catalog.TryResolveItem(entry.itemGuid, out var item);
            _bindings[entry.slotIndex].SetUsable((UsableItemSO)item, entry.amount,
                entry.weaponCurrentDurability, entry.weaponMaxDurability);
        }
        _autoBindPassCompleted = true;
        EnsureEquippedItemsHaveBindings(); RefreshSelectionVisual(); OnChanged?.Invoke();
    }
}
