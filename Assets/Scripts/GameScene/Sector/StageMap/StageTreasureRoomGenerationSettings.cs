using System;
using UnityEngine;

public readonly struct StageTreasureRoomGenerationSettings
{
    public static StageTreasureRoomGenerationSettings Disabled =>
        new StageTreasureRoomGenerationSettings(enabled: false, minCount: 0,maxCount: 0,
        consecutiveNoHitStageCount: 0,extraChancePerNoHitStage: 0f, maxExtraChance: 0f,excludeFirstRoom: true);

    public bool Enabled { get; }
    public int MinCount { get; }
    public int MaxCount { get; }
    public int ConsecutiveNoHitStageCount { get; }
    public float ExtraChancePerNoHitStage { get; }
    public float MaxExtraChance { get; }
    public bool ExcludeFirstRoom { get; }

    public StageTreasureRoomGenerationSettings(bool enabled,int minCount,int maxCount,int consecutiveNoHitStageCount,
        float extraChancePerNoHitStage,float maxExtraChance,bool excludeFirstRoom)
    {
        Enabled = enabled;
        MinCount = Mathf.Max(0, minCount);
        MaxCount = Mathf.Max(MinCount, maxCount);
        ConsecutiveNoHitStageCount = Mathf.Max(0, consecutiveNoHitStageCount);
        ExtraChancePerNoHitStage = Mathf.Clamp01(extraChancePerNoHitStage);
        MaxExtraChance = Mathf.Clamp01(maxExtraChance);
        ExcludeFirstRoom = excludeFirstRoom;
    }

    public int RollTreasureRoomCount(int stageIndex, System.Random rng)
    {
        if (!Enabled)
            return 0;

        int min = MinCount;
        int max = MaxCount;

        if (max <= min)
            return min;

        int count = min;
        int extraSlots = max - min;
        float extraChance = Mathf.Min(
            MaxExtraChance,
            ConsecutiveNoHitStageCount * ExtraChancePerNoHitStage);

        if (rng == null)
            rng = new System.Random();

        for (int i = 0; i < extraSlots; i++)
        {
            if (rng.NextDouble() <= extraChance)
                count++;
        }

        return Mathf.Clamp(count, min, max);
    }
}
