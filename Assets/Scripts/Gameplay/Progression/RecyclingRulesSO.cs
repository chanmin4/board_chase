using System;
using UnityEngine;

/// <summary>Independent 시간제 분해기 source. Runtime never reads the retired aggregate.</summary>
[CreateAssetMenu(fileName = "RecyclingRules", menuName = "Game/Systems/시간제 분해기")]
public sealed class RecyclingRulesSO : ScriptableObject
{
    private static RecyclingRulesSO _active;
    public static RecyclingRulesSO Active => _active != null ? _active : _active = Resources.Load<RecyclingRulesSO>("RecyclingRules");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => _active = null;
    public RecyclingRules recycling = new();
}

[Serializable] public sealed class RecyclingRules
{
    [Min(1)] public int baseSlots = 2;
    [Min(1)] public int maximumSlots = 10;
    [Tooltip("무기·방탄 등 비패시브 레시피. 패시브는 PassiveModuleRules의 재료·분해 시간·아이템별 수량을 사용합니다.")]
    public RecyclingRecipe[] recipes = Array.Empty<RecyclingRecipe>();
    public RecyclingRecipe Find(ItemSO item) => item is PassiveItemSO passive
        ? PassiveModuleRulesSO.Active?.RecyclingRecipeFor(passive)
        : Array.Find(recipes ?? Array.Empty<RecyclingRecipe>(), r => r != null && r.Matches(item));
}
[Serializable] public sealed class RecyclingRecipe
{
    public ItemKind kind;
    public PlayerShopItemRarity rarity;
    public ItemSO exactInput;
    public ItemSO output;
    [Min(1)] public int outputAmount = 1;
    [Min(.1f)] public float seconds = 30;
    public bool Matches(ItemSO item) => item != null && (exactInput != null ? PassiveInstanceRuntime.MatchesDefinition(item,exactInput) : item.Kind == kind && item.Rarity == rarity);
}
