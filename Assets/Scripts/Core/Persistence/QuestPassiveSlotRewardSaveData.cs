using System;

/// <summary>Permanent grant keyed by quest/reward, independent of the run lost on death.</summary>
[Serializable]
public sealed class QuestPassiveSlotRewardSaveData
{
    public string questId;
    public int rewardIndex;
    public int slotAmount;
}
