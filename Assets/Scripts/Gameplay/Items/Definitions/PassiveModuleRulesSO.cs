using System;
using UnityEngine;

[Serializable]
public sealed class PassivePartsTier
{
    public PlayerShopItemRarity rarity;
    public ItemSO parts;
    [Min(1)] public int rerollCost = 1;
    [Min(1)] public int moduleYield = 1;
}

public enum PassiveCategory { Combat, Survival }

[Serializable]
public sealed class StartingPassiveDraw
{
    public string label;
    public bool enabled = true;
    [Min(1)] public int requiredLevel = 1;
    [Min(2)] public int moduleCost = 10;
    [Tooltip("Normal, Rare, Unique, Legendary, Mythic 순서의 상대 비중입니다.")]
    public float[] rarityWeights = { 80, 20, 0, 0, 0 };
}

[CreateAssetMenu(menuName = "Game/Passive Module Rules")]
public sealed class PassiveModuleRulesSO : ScriptableObject
{
    [HideInInspector] public bool homeOnlyPassiveChanges; // Legacy serialization only. Raid switching is now supported.
    [Header("레이드 패시브 · 전투/생존 각 4칸")]
    public int[] combatSlotLevels = { 1, 5, 10, 15 };
    public int[] survivalSlotLevels = { 1, 7, 12, 18 };
    [Tooltip("미확정 밸런스값. 희귀도·개체별 원본 수치는 유지하고 적용 강도만 조절합니다.")]
    [Min(0)] public float combatEffectMultiplier = 1.5f;
    [Min(0)] public float survivalEffectMultiplier = 1.5f;
    [Min(0)] public float changeCooldownSeconds = 10f;
    [Header("시작 추첨 · HOME 전용 · 임시 수치")]
    public ItemSO unifiedModule;
    public StartingPassiveDraw[] startingDraws = {
        new() { label = "저등급", moduleCost = 10 },
        new() { label = "중등급", requiredLevel = 10, moduleCost = 25, rarityWeights = new float[] { 50, 35, 15, 0, 0 } },
        new() { label = "고등급 · 데모 제외", enabled = false, requiredLevel = 20, moduleCost = 50, rarityWeights = new float[] { 35, 30, 25, 10, 0 } }
    };
    public int[] drawCountLevels = { 1, 10, 20 };
    public PassiveItemSO[] startingPool = Array.Empty<PassiveItemSO>();
    [Tooltip("모든 패시브의 시간제 분해 시간. 통합 모듈을 결과로 지급하며, 등급별 parts는 구형 재료 호환용입니다.")]
    [Min(.1f)] public float recyclingSeconds = 30f;
    public PassivePartsTier[] tiers = Array.Empty<PassivePartsTier>();
    public PassiveItemSO[] enemyPool = Array.Empty<PassiveItemSO>();
    public static PassiveModuleRulesSO Active => Resources.Load<PassiveModuleRulesSO>("PassiveModuleRules");
    public PassivePartsTier Find(PlayerShopItemRarity rarity) => Array.Find(tiers ?? Array.Empty<PassivePartsTier>(), x => x != null && x.rarity == rarity);
    public int SlotLevel(PassiveCategory category, int position)
    {
        if (position <= 0) return 1;
        var levels = category == PassiveCategory.Combat ? combatSlotLevels : survivalSlotLevels;
        return levels != null && position < levels.Length ? Mathf.Max(1, levels[position]) : int.MaxValue;
    }
    public bool IsSlotUnlocked(int index, int level) => index >= 0 && index < 8 && level >= SlotLevel((PassiveCategory)(index % 2), index / 2);
    public int DrawLimit(int level)
    {
        int count = 0;
        foreach (int threshold in drawCountLevels ?? Array.Empty<int>()) if (level >= Mathf.Max(1, threshold)) count++;
        return count;
    }
    public int ModuleYield(PassiveItemSO item) => Mathf.Max(1, Find(item.Rarity)?.moduleYield ?? item.DismantlePartAmount);
    public float EffectMultiplier(PassiveCategory category)
    {
        float value = category == PassiveCategory.Combat ? combatEffectMultiplier : survivalEffectMultiplier;
        return float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Max(0, value);
    }
    public PlayerStatModifier ApplyStrength(PassiveItemSO item, PlayerStatModifier modifier)
    {
        float scale = EffectMultiplier(item.Category);
        if (modifier.type == StatModifierType.PercentMultiply) modifier.value = 1f + (modifier.value - 1f) * scale;
        else if (modifier.type != StatModifierType.Override) modifier.value *= scale;
        return modifier;
    }
    public RecyclingRecipe RecyclingRecipeFor(PassiveItemSO item)
    {
        if (item == null) return null;
        var tier=item!=null?Find(item.Rarity):null;
        var output = unifiedModule != null ? unifiedModule : tier?.parts;
        return output==null?null:new RecyclingRecipe {
            kind=ItemKind.Passive,rarity=item.Rarity,output=output,
            outputAmount=item.MaximumDismantleYield > 0 ? Mathf.Min(ModuleYield(item), item.MaximumDismantleYield) : ModuleYield(item),seconds=Mathf.Max(.1f,recyclingSeconds)
        };
    }
}
