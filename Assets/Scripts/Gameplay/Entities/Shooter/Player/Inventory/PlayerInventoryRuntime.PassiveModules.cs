using UnityEngine;

public partial class PlayerInventoryRuntime
{
    private bool IsRaidLockedEquippedModule(ItemSO item)
    {
        if (item is not PassiveItemSO || GameProgressionRuntime.IsHomePreparation) return false;
        var slots = GetComponent<PlayerPassiveInventoryRuntime>()?.EquippedSlots;
        if (slots != null)
            foreach (var equipped in slots)
                if (equipped == item) return true;
        return false;
    }

    public bool TryBindOwnedPassive(PassiveItemSO source, out PassiveItemSO bound, out string message)
    {
        bound = source;
        message = "소유한 패시브가 없습니다.";
        if (source == null) return false;
        NormalizeStorage();
        var stack = _items.Find(x => x != null && !x.IsEmpty && x.Item == source);
        if (stack == null && !source.IsRolledInstance)
        {
            var owner = GetComponent<PlayerPassiveInventoryRuntime>();
            bool Assigned(PassiveItemSO passive)
            {
                if (owner != null) foreach (var equipped in owner.EquippedSlots) if (equipped == passive) return true;
                return false;
            }
            stack = _items.Find(x => x != null && !x.IsEmpty && x.Item is PassiveItemSO candidate &&
                !candidate.DisabledForRaid && !Assigned(candidate) && PassiveInstanceRuntime.MatchesDefinition(candidate, source));
            if (stack?.Item is PassiveItemSO owned) source = bound = owned;
        }
        if (stack == null) return false;
        if (!source.IsRolledInstance)
        {
            if (stack.Amount != 1) { message = "이전 저장의 중첩 패시브를 먼저 분리하세요."; return false; }
            bound = PassiveInstanceRuntime.Create(source) as PassiveItemSO;
            if (bound == null || !bound.IsRolledInstance) { message = "패시브 개체를 생성하지 못했습니다."; return false; }
            stack.Set(bound, 1);
        }
        if (!PassiveInstanceRuntime.Bind(bound)) return false;
        message = "패시브 귀속 완료. 해제하면 이번 원정에서 재장착·분해할 수 없습니다.";
        return true;
    }

    public bool TryDismantlePassive(int inventoryIndex, out string message)
    {
        return Fail("즉시 분해는 폐기되었습니다. 인벤토리 시간제 분해기를 사용하세요.",out message);
    }

    public bool TryRerollPassive(int inventoryIndex, out string message)
    {
        if (!GameProgressionRuntime.IsHomePreparation || GetComponent<Damageable>()?.IsDead == true) return Fail("수치 리롤은 살아 있는 플레이어가 HOME에서만 할 수 있습니다.", out message);
        NormalizeStorage();
        if (!IsValidIndex(inventoryIndex) || _items[inventoryIndex].Item is not PassiveItemSO passive)
            return Fail("리롤할 패시브가 없습니다.", out message);
        var tier = PassiveModuleRulesSO.Active?.Find(passive.Rarity);
        if (passive.DisabledForRaid) return Fail("해제한 패시브는 리롤로 다시 활성화할 수 없습니다.", out message);
        int cost = tier != null ? Mathf.Max(1, tier.rerollCost) : 1;
        var material = PassiveModuleRulesSO.Active?.unifiedModule;
        if (material == null || GetInventoryItemAmount(material) < cost) material = tier?.parts; // Preserve usable legacy parts.
        if (material == null || GetInventoryItemAmount(material) < cost)
            return Fail("통합 모듈 또는 호환되는 구형 파츠가 부족합니다.", out message);
        var authoredRanges = passive.SourceDefinition.FindRoll(passive.Rarity)?.stats;
        if(!passive.AllowNumericReroll)return Fail("이 패시브는 고정 효과라 리롤할 수 없습니다.",out message);
        if (authoredRanges == null || authoredRanges.Count == 0)
            return Fail("Item Maker에서 해당 등급의 리롤 수치 범위를 먼저 설정하세요. 파츠는 소비하지 않았습니다.", out message);
        var original = passive;
        if (!passive.IsRolledInstance)
        {
            if (_items[inventoryIndex].Amount != 1) return Fail("중첩 패시브를 먼저 분리하세요.", out message);
            passive = PassiveInstanceRuntime.Create(passive) as PassiveItemSO;
            if (passive == null || !passive.IsRolledInstance) return Fail("패시브 개체 생성 실패.", out message);
        }
        if (!PassiveInstanceRuntime.TryReroll(passive)) return Fail("Item Maker에서 같은 등급·효과의 리롤 수치 범위를 설정하세요. 파츠는 소비하지 않았습니다.", out message);
        if (original != passive) _items[inventoryIndex].Set(passive, 1);
        // Availability was checked before mutating. Consume through the non-callback storage operation.
        int remaining = cost;
        foreach (var stack in _items)
        {
            if (remaining == 0) break;
            if (stack == null || stack.IsEmpty || !IsSameItemIdentity(stack.Item, material)) continue;
            int take = Mathf.Min(remaining, stack.Amount);
            if (stack.Remove(take)) stack.Clear();
            remaining -= take;
        }
        NotifyChanged();
        GetComponent<PlayerPassiveInventoryRuntime>()?.RefreshModuleEffects();
        message = "리롤 완료. 효과 종류·등급·귀속은 유지하고 수치만 변경했습니다.";
        return true;
    }
}
