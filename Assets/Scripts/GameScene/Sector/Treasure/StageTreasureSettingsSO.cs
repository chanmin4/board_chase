using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageTreasureSettings",
    menuName = "Game/Stage/Stage Treasure Settings")]
public class StageTreasureSettingsSO : ScriptableObject
{
    [System.Serializable]
    public sealed class StageTreasureRule
    {
        public int stageIndex;
        public string displayName;

        [Header("Room Generation")]
        public bool enableTreasureRooms = true;
        [Min(0)] public int treasureRoomMinCount = 1;
        [Min(0)] public int treasureRoomMaxCount = 2;
        [Range(0f, 1f)] public float extraTreasureChancePerNoHitStage = 0.1f;
        [Range(0f, 1f)] public float maxExtraTreasureChance = 1f;
        public bool excludeFirstRoomFromTreasure = true;

        [Header("Reward")]
        public TreasureRoomDropTableSO dropTable;
    }

    [SerializeField] private StageTreasureRule[] _stageRules;

    public StageTreasureRoomGenerationSettings CreateTreasureRoomGenerationSettings(
        int stageIndex,
        int consecutiveNoHitStageCount)
    {
        StageTreasureRule rule = FindRule(stageIndex);

        if (rule == null)
            return StageTreasureRoomGenerationSettings.Disabled;

        return new StageTreasureRoomGenerationSettings(
            rule.enableTreasureRooms,
            rule.treasureRoomMinCount,
            rule.treasureRoomMaxCount,
            consecutiveNoHitStageCount,
            rule.extraTreasureChancePerNoHitStage,
            rule.maxExtraTreasureChance,
            rule.excludeFirstRoomFromTreasure);
    }

    public bool TryRollReward(
        int stageIndex,
        int seed,
        IReadOnlyList<PassiveItemSO> ownedPassiveItems,
        out TreasureRoomReward reward)
    {
        reward = default;

        StageTreasureRule rule = FindRule(stageIndex);

        if (rule == null || rule.dropTable == null)
            return false;

        return rule.dropTable.TryRollReward(seed, ownedPassiveItems, out reward);
    }

    public bool TryGetRule(int stageIndex, out StageTreasureRule rule)
    {
        rule = FindRule(stageIndex);
        return rule != null;
    }

    private StageTreasureRule FindRule(int stageIndex)
    {
        if (_stageRules == null)
            return null;

        for (int i = 0; i < _stageRules.Length; i++)
        {
            StageTreasureRule rule = _stageRules[i];

            if (rule != null && rule.stageIndex == stageIndex)
                return rule;
        }

        return null;
    }
}