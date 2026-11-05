using UnityEngine;

public partial class PlayerInventoryRuntime
{
    public bool TryPickupMedicalCondition(ConsumableItemSO item, int amount, float current, float max, out string message)
    {
        ResolveRefs();
        if (item == null || !item.UsesDurability || amount <= 0 || current == 0f)
        { message = "의료 아이템의 수량 또는 내구도가 올바르지 않습니다."; return false; }
        if (!CanAcquireOwnedAmount(item, amount, out message)) return false;
        return TryStoreItem(item, amount, -1f, current, max, out message);
    }

    public bool TrySpendConsumableDurability(int slotIndex, ConsumableItemSO item, out string message)
    {
        if (!IsValidIndex(slotIndex) || !_items[slotIndex].TrySpendConsumableDurability(item))
        { message = "Usable item is missing or has no condition left."; return false; }
        NotifyChanged();
        message = $"{item.name}: 내구도 소모";
        return true;
    }
}
