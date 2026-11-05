using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StartingPassivePreparationSaveData
{
    public int drawsUsed;
    public bool committed;
}

public partial class PlayerInventoryRuntime
{
    public int StartingPassiveDrawsRemaining => SaveSystem.Active?.RunData.startingPassives?.committed == true ? 0 : Mathf.Max(0,
        (PassiveModuleRulesSO.Active?.DrawLimit(GameProgressionRuntime.Level) ?? 0) - (SaveSystem.Active?.RunData.startingPassives?.drawsUsed ?? 0));

    public bool TryDrawStartingPassive(int drawIndex, out string message)
    {
        if (!GameProgressionRuntime.IsHomePreparation || GetComponent<Damageable>()?.IsDead == true || SaveSystem.Active == null)
            return Fail("시작 패시브 추첨은 HOME 출발 준비 중에만 가능합니다.", out message);
        var rules = PassiveModuleRulesSO.Active;
        var preparation = SaveSystem.Active.RunData.startingPassives ??= new();
        if (preparation.committed || StartingPassiveDrawsRemaining <= 0)
            return Fail("이번 원정의 시작 추첨 횟수를 모두 사용했습니다.", out message);
        if (rules?.startingDraws == null || drawIndex < 0 || drawIndex >= rules.startingDraws.Length)
            return Fail("시작 추첨 설정이 없습니다.", out message);
        var draw = rules.startingDraws[drawIndex];
        if (draw == null || !draw.enabled || GameProgressionRuntime.Level < draw.requiredLevel)
            return Fail("해금되지 않은 추첨입니다.", out message);
        int cost = Mathf.Max(2, draw.moduleCost);
        if (rules.unifiedModule == null || GetInventoryItemAmount(rules.unifiedModule) < cost)
            return Fail($"통합 모듈 {cost}개가 필요합니다.", out message);
        NormalizeStorage();
        int free = -1;
        for (int i = 0; i < Capacity; i++) if (_items[i].IsEmpty) { free = i; break; }
        if (free < 0) return Fail("추첨 결과를 받을 빈 가방/포켓 칸이 필요합니다. 모듈은 소비하지 않았습니다.", out message);

        var rarities = new[] { PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare, PlayerShopItemRarity.Unique, PlayerShopItemRarity.Legendary, PlayerShopItemRarity.Mythic };
        var pools = new List<PassiveItemSO>[rarities.Length];
        double total = 0;
        var weights = new double[rarities.Length];
        for (int i = 0; i < rarities.Length; i++)
        {
            pools[i] = new();
            float weight = draw.rarityWeights != null && i < draw.rarityWeights.Length ? draw.rarityWeights[i] : 0;
            if (weight <= 0 || float.IsNaN(weight) || float.IsInfinity(weight)) continue;
            foreach (var candidate in rules.startingPool ?? Array.Empty<PassiveItemSO>())
                if (candidate != null && !candidate.IsRolledInstance && !pools[i].Contains(candidate) &&
                    CanAcquireOwnedAmount(candidate, 1, out _) && PassiveInstanceRuntime.CanCreate(candidate, out _, rarities[i])) pools[i].Add(candidate);
            if (pools[i].Count > 0) { weights[i] = weight; total += weight; }
        }
        if (total <= 0) return Fail("시작 패시브 후보/등급 확률/랜덤 수치를 Game System Tool에 등록하세요. 모듈은 소비하지 않았습니다.", out message);
        var random = new System.Random();
        double cursor = random.NextDouble() * total;
        int chosen = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0) continue;
            chosen = i; cursor -= weights[i]; if (cursor < 0) break;
        }
        var source = pools[chosen][random.Next(pools[chosen].Count)];
        var instance = PassiveInstanceRuntime.Create(source, random, rarities[chosen]) as PassiveItemSO;
        if (instance == null || !instance.IsRolledInstance || !PassiveInstanceRuntime.LimitDismantleYield(instance, cost - 1))
            return Fail("추첨 개체 생성에 실패했습니다. 모듈은 소비하지 않았습니다.", out message);

        // Validated, non-callback transaction. Never consumes a draw on an empty pool/full inventory.
        int remaining = cost;
        foreach (var stack in _items)
        {
            if (remaining == 0) break;
            if (stack.IsEmpty || !IsSameItemIdentity(stack.Item, rules.unifiedModule)) continue;
            int take = Mathf.Min(remaining, stack.Amount); if (stack.Remove(take)) stack.Clear(); remaining -= take;
        }
        _items[free].Set(instance, 1);
        preparation.drawsUsed++;
        NotifyChanged(); SaveSystem.Active.SaveDataToDisk();
        message = $"{draw.label} 시작 패시브: {InventoryItemDisplayUtility.ResolveItemName(instance)} · 남은 추첨 {StartingPassiveDrawsRemaining}회";
        return true;
    }
}
