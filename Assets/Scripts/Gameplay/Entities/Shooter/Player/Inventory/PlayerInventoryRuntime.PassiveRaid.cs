using UnityEngine;

public partial class PlayerInventoryRuntime
{
    private bool _passiveOverflowPending;
    private bool _droppingPassiveOverflow;
    private float _nextPassiveOverflowRetry;

    /// <summary>Last occupied absolute slots first. Failed spawns stay owned and saved for retry.</summary>
    public void DropPassiveCapacityOverflow()
    {
        if (_droppingPassiveOverflow || GetComponent<Damageable>()?.IsDead == true) return;
        _droppingPassiveOverflow = true;
        bool changed = false;
        _passiveOverflowPending = false;
        try
        {
            for (int i = _items.Count - 1; i >= Mathf.Max(0, Capacity); i--)
            {
                var stack = _items[i];
                if (stack == null || stack.IsEmpty) continue;
                if (IsRaidLockedEquippedModule(stack.Item)) continue;
                if (!WorldItemPickupSpawner.TrySpawn(stack.Item, stack.Amount, stack.Amount, 0,
                    stack.ArmorDurability, stack.WeaponCurrentDurability, stack.WeaponMaxDurability,
                    stack.GasMaskFilterDurability, stack.ArmorMaxDurability, stack.GasMaskMaxFilterDurability,
                    transform.position, Quaternion.identity, out var pickup) || pickup == null)
                {
                    _passiveOverflowPending = true;
                    _nextPassiveOverflowRetry = Time.unscaledTime + 1f;
                    Debug.LogWarning("[Passive] 초과 아이템 드롭 실패. 원본은 인벤토리/저장에 보존하고 재시도합니다.", this);
                    break;
                }
                stack.Clear(); changed = true;
            }
        }
        finally { _droppingPassiveOverflow = false; }
        if (changed) NotifyChanged();
    }

    public void EndPassiveRaid()
    {
        // Successful extraction keeps both the owned instances and the equipped loadout.
        // Death still clears carried run inventory through the ordinary death pipeline.
        if (SaveSystem.Active != null) SaveSystem.Active.RunData.startingPassives = new();
        DropPassiveCapacityOverflow();
        NotifyChanged();
    }
}
