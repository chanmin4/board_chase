using System;
using UnityEngine;

/// <summary>Independent 경험치 · 레벨 source. Runtime never reads the retired aggregate.</summary>
[CreateAssetMenu(fileName = "ExperienceRules", menuName = "Game/Systems/경험치 · 레벨")]
public sealed class ExperienceRulesSO : ScriptableObject
{
    private static ExperienceRulesSO _active;
    public static ExperienceRulesSO Active => _active != null ? _active : _active = Resources.Load<ExperienceRulesSO>("ExperienceRules");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => _active = null;
    public ExperienceRules experience = new();
}

[Serializable]
public sealed class ExperienceRules
{
    [Tooltip("Lv1→2부터 순서. 배열 이후는 마지막 필요량을 사용. 임시 밸런스.")]
    public int[] experiencePerLevel = { 300,500,750,1000,1250,1500,1800,2100,2400,2800,3200,3600,4000,4500,5000,5500,6000,6600,7200,7800,8500,9200,10000,11000,12000,13000,14000,15000,16000 };
    [Min(1)] public int maximumLevel = 30;
    [Min(0)] public int normalKill = 50;
    [Min(0)] public int bossKill = 500;
    [Min(0)] public int sectorDiscovery = 30;
    [Min(0)] public int defaultQuest = 200;
    public int[] discoveryByRarity = { 5,15,75,35,150 }; // Serialized rarity enum order, not rarity rank.
    [Range(0,2)] public float extractionMultiplier = 1f;
    [Range(0,2)] public float deathMultiplier = .5f;
    public LevelStatReward[] statRewards = {
        new() { level = 7, stat = PlayerStatId.MaxHealth, amount = 10 },
        new() { level = 10, stat = PlayerStatId.MaxHealth, amount = 10 }
    };
    public int RequiredForNext(int level) => experiencePerLevel == null || experiencePerLevel.Length == 0
        ? 1000 : Mathf.Max(1, experiencePerLevel[Mathf.Clamp(level - 1, 0, experiencePerLevel.Length - 1)]);
    public int Level(long total)
    {
        int level = 1;
        while (level < Mathf.Clamp(maximumLevel,1,1000) && total >= RequiredForNext(level))
        { total -= RequiredForNext(level); level++; }
        return level;
    }
    public long IntoLevel(long total)
    {
        int level = 1;
        while (level < Mathf.Clamp(maximumLevel,1,1000) && total >= RequiredForNext(level))
        { total -= RequiredForNext(level); level++; }
        return Math.Max(0,total);
    }
    // Reuses the permanent-upgrade pattern: authored per-level effects, derived rather than repeatedly granted.
    public void ApplyRewards(long total, Action<PlayerStatModifier> apply)
    {
        int level = Level(total);
        foreach (var reward in statRewards ?? Array.Empty<LevelStatReward>())
            if (reward != null && reward.level <= level)
                apply(new PlayerStatModifier { stat = reward.stat, type = StatModifierType.FlatAdd, value = reward.amount });
    }
    public int Discovery(PlayerShopItemRarity rarity) => discoveryByRarity != null && (int)rarity >= 0 && (int)rarity < discoveryByRarity.Length
        ? Mathf.Max(0,discoveryByRarity[(int)rarity]) : 0;
}
[Serializable] public sealed class LevelStatReward { [Min(1)] public int level = 1; public PlayerStatId stat = PlayerStatId.MaxHealth; public float amount; }
