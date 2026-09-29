using System;
using UnityEngine;

/// <summary>WIP authoring brief, not a second quest runtime. Bind existing QuestDefinitions after detailed design.</summary>
[CreateAssetMenu(fileName = "RaidProgressionDraft", menuName = "Game/Stage/Raid Progression Draft (WIP)")]
public sealed class RaidProgressionDraftSO : ScriptableObject
{
    [Tooltip("Draft only: never auto-grants stages or rewards. Connect reviewed QuestDefinitionSO assets later.")]
    public bool draftOnly = true;
    [Range(1, 8)] public int maximumShopUnlockLevel = 6;
    public int[] specialFarmingOnlyLevels = { 7, 8 };
    public RaidMainQuestDraft[] mainLine = Array.Empty<RaidMainQuestDraft>();
    public RaidSideQuestDraft[] sideLines = Array.Empty<RaidSideQuestDraft>();
    [TextArea] public string endingNote = "Stage 5 -> special map / ending conditions TBD.";
}

[Serializable]
public sealed class RaidMainQuestDraft
{
    public string stableId;
    public int currentStage;
    public PlayerShopItemRarity valuableRarity;
    [Tooltip("Zero is deliberately unset, not free completion.")]
    [Min(0)] public int valuableCount;
    public bool requiresCurrentStageBossKill = true;
    [Min(0)] public int moneyCost;
    public StageDefinitionSO stageToUnlock;
    public QuestDefinitionSO qualificationQuest;
    public QuestDefinitionSO stageUnlockQuest;
}

[Serializable]
public sealed class RaidSideQuestDraft
{
    public string stableId;
    public int minimumUnlockedStage = 1;
    public QuestBranch branch = QuestBranch.Item;
    public QuestRewardType rewardType = QuestRewardType.ShopUnlock;
    public int equipmentLevel;
    [TextArea] public string authoringNote;
    public QuestDefinitionSO quest;
}
