/*
 * Role hierarchy: Gameplay > Progression > Quests > Data
 * Primary responsibility: Defines an English quest, its ordered prerequisites, objectives, and persistent reward description.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum QuestObjectiveType
{
    Kill = 0,
    Hit = 1,
    CollectItem = 2,
    SubmitItem = 3,
    HeadshotHit = 4,
    HeadshotKill = 5,
    Action = 6,
    Craft = 7,
    SectorClear = 8,
    StructureDestroyed = 9,
    LandmarkClear = 10
}

public enum QuestDistanceRule
{
    Any = 0,
    Within = 1,
    Beyond = 2
}

public enum QuestRewardType
{
    Item = 0,
    Stat = 1,
    ShopUnlock = 2,
    StageUnlock = 3,
    WarehouseSlots = 4,
    PassiveSlots = 5,
    PocketSlots = 6,
    RecyclingSlots = 7,
    CraftUnlock = 8
}

/// <summary>
/// Authored ownership branch for Home quest residents. This affects which NPC
/// may offer a quest; all branches still share one QuestRuntime and Mission UI.
/// </summary>
public enum QuestBranch
{
    Main = 0,
    Item = 1,
    Stats = 2
}

[Serializable]
public sealed class QuestRewardDefinition
{
    [SerializeField] private string _craftRecipeId;
    public string CraftRecipeId => _craftRecipeId;
    [SerializeField] private QuestRewardType _rewardType = QuestRewardType.Item;
    [SerializeField] private ItemSO _item;
    [SerializeField, Min(1)] private int _itemAmount = 1;
    [SerializeField] private PlayerStatId _stat = PlayerStatId.MaxHealth;
    [SerializeField] private StatModifierType _statModifierType = StatModifierType.FlatAdd;
    [SerializeField] private float _statValue = 1f;
    [Tooltip("Home shop catalog that owns the unlock node.")]
    [SerializeField] private ShopConfigSO _shopConfig;
    [Tooltip("Stable ShopConfigSO unlock-node ID. Select it through Quest Maker instead of typing a product ID.")]
    [SerializeField] private string _shopUnlockNodeId;
    [Tooltip("The authored stage definition to make reachable. Earlier stages remain unlocked.")]
    [SerializeField] private StageDefinitionSO _stageToUnlock;
    [Tooltip("Permanent Inheritance Warehouse capacity granted when this quest reward is claimed.")]
    [SerializeField, Min(1)] private int _warehouseSlotAmount = 1;
    [SerializeField, Min(1)] private int _passiveSlotAmount = 1;
    [SerializeField, Range(1, 4)] private int _pocketSlotAmount = 1;
    [SerializeField,Min(1)] private int _recyclingSlotAmount=1;
    public int RecyclingSlotAmount=>Mathf.Max(1,_recyclingSlotAmount);
    public int PocketSlotAmount => Mathf.Clamp(_pocketSlotAmount, 1, 4);

    public QuestRewardType RewardType => _rewardType;
    public ItemSO Item => _item;
    public int ItemAmount => Mathf.Max(1, _itemAmount);
    public PlayerStatId Stat => _stat;
    public StatModifierType StatModifierType => _statModifierType;
    public float StatValue => _statValue;
    public ShopConfigSO ShopConfig => _shopConfig;
    public string ShopUnlockNodeId => _shopUnlockNodeId;
    public StageDefinitionSO StageToUnlock => _stageToUnlock;
    public int WarehouseSlotAmount => Mathf.Max(1, _warehouseSlotAmount);
    public int PassiveSlotAmount => Mathf.Clamp(_passiveSlotAmount, 1, PlayerPassiveInventoryRuntime.MaximumSlotCount);

    // Purpose: Builds the summary from the current authored data.
    public string BuildSummary()
    {
        if (_rewardType == QuestRewardType.Item)
        {
            string itemName = _item != null ? _item.name : "Item";
            return $"{itemName} x{ItemAmount}";
        }

        if (_rewardType == QuestRewardType.ShopUnlock)
        {
            string shopName = _shopConfig != null ? _shopConfig.name : "Shop";
            string nodeId = string.IsNullOrWhiteSpace(_shopUnlockNodeId) ? "Unlock Node" : _shopUnlockNodeId;
            return $"Unlock {shopName}: {nodeId}";
        }

        if (_rewardType == QuestRewardType.StageUnlock)
        {
            return _stageToUnlock != null
                ? $"Unlock Stage {_stageToUnlock.StageIndex}: {_stageToUnlock.DisplayName}"
                : "Unlock Stage";
        }

        if (_rewardType == QuestRewardType.PassiveSlots)
            return $"패시브 장착칸 +{PassiveSlotAmount}";
        if (_rewardType == QuestRewardType.CraftUnlock)
            return $"제작법 해금: {_craftRecipeId}";
        if (_rewardType == QuestRewardType.PocketSlots) return $"포켓 +{PocketSlotAmount}칸 (최대 10칸)";
        if(_rewardType==QuestRewardType.RecyclingSlots)return $"분해기 +{RecyclingSlotAmount}칸";
        if (_rewardType == QuestRewardType.WarehouseSlots)
            return $"Inheritance Warehouse +{WarehouseSlotAmount} slots";

        string value = _statModifierType switch
        {
            StatModifierType.PercentAdd => $"{_statValue:+0.##;-0.##;0}%",
            StatModifierType.PercentMultiply => $"x{_statValue:0.##}",
            StatModifierType.Override => $"= {_statValue:0.##}",
            _ => $"{_statValue:+0.##;-0.##;0}"
        };
        return $"{_stat} {value}";
    }
}

[Serializable]
public sealed class QuestObjectiveDefinition
{
    [SerializeField] private string _actionId;
    public string ActionId => _actionId;
    [SerializeField] private QuestObjectiveType _objectiveType = QuestObjectiveType.Kill;
    [Tooltip("Optional exact EntityStatConfigSO target. Leave empty to match every enemy target.")]
    [SerializeField] private EntityStatConfigSO _targetConfig;
    [Tooltip("Required inventory item for Collect Item objectives.")]
    [SerializeField] private ItemSO _targetItem;
    [Tooltip("Optional exact player weapon required for a Kill or Hit objective.")]
    [SerializeField] private WeaponSO _requiredWeapon;
    [SerializeField, Min(1)] private int _requiredCount = 10;
    [SerializeField] private QuestDistanceRule _distanceRule = QuestDistanceRule.Any;
    [Tooltip("Distance from the player to the target at the hit or kill event.")]
    [SerializeField, Min(0f)] private float _distanceMeters = 10f;

    public QuestObjectiveType ObjectiveType => _objectiveType;
    public EntityStatConfigSO TargetConfig => _targetConfig;
    public ItemSO TargetItem => _targetItem;
    public WeaponSO RequiredWeapon => _requiredWeapon;
    public int RequiredCount => Mathf.Max(1, _requiredCount);
    public QuestDistanceRule DistanceRule => _distanceRule;
    public float DistanceMeters => Mathf.Max(0f, _distanceMeters);

    // Purpose: Checks whether the current data matches matches.
    public bool Matches(Damageable target, float distanceMeters)
    {
        return Matches(target, distanceMeters, null);
    }

    // Purpose: Checks whether the current data matches matches.
    public bool Matches(Damageable target, float distanceMeters, WeaponSO usedWeapon)
    {
        if (_objectiveType == QuestObjectiveType.CollectItem || _objectiveType == QuestObjectiveType.SubmitItem || target == null)
            return false;

        if (_targetConfig != null && target.StatConfig != _targetConfig)
            return false;

        if (_requiredWeapon != null && usedWeapon != _requiredWeapon)
            return false;

        return _distanceRule switch
        {
            QuestDistanceRule.Within => distanceMeters <= DistanceMeters,
            QuestDistanceRule.Beyond => distanceMeters >= DistanceMeters,
            _ => true
        };
    }

    // Purpose: Builds the summary from the current authored data.
    public string BuildSummary()
    {
        if(_objectiveType>=QuestObjectiveType.Action)
        {
            string label=_objectiveType switch
            {
                QuestObjectiveType.Craft=>"제작",QuestObjectiveType.SectorClear=>"섹터 클리어",
                QuestObjectiveType.LandmarkClear=>"랜드마크 클리어",QuestObjectiveType.StructureDestroyed=>"구조물 파괴",
                _=>"행동 완료"
            };
            return label+(string.IsNullOrEmpty(_actionId)?"":" · "+_actionId);
        }
        if (_objectiveType == QuestObjectiveType.SubmitItem)
            return $"Submit {(_targetItem != null ? _targetItem.name : "Item")}";
        if (_objectiveType == QuestObjectiveType.CollectItem)
        {
            string itemName = _targetItem != null ? _targetItem.name : "Item";
            return $"Collect {itemName}";
        }

        string action = _objectiveType switch
        {
            QuestObjectiveType.HeadshotHit => "Headshot Hit",
            QuestObjectiveType.HeadshotKill => "Headshot Kill",
            QuestObjectiveType.Kill => "Kill",
            _ => "Hit"
        };
        string targetName = _targetConfig != null ? _targetConfig.name : "Enemy";
        string weaponSuffix = _requiredWeapon != null ? $" using {_requiredWeapon.DisplayName}" : string.Empty;
        string distanceSuffix = _distanceRule switch
        {
            QuestDistanceRule.Within => $" within {DistanceMeters:0.#}m",
            QuestDistanceRule.Beyond => $" beyond {DistanceMeters:0.#}m",
            _ => string.Empty
        };

        return $"{action} {targetName}{weaponSuffix}{distanceSuffix}";
    }
}

[CreateAssetMenu(fileName = "Quest", menuName = "VSplatter/Progression/Quest Definition")]
public sealed class QuestDefinitionSO : ScriptableObject
{
    [Header("System gates (empty = existing behavior)")]
    [SerializeField] private int[] _requiredVisitedStages = Array.Empty<int>();
    [SerializeField] private string _sharedGrowthRewardGroup;
    [SerializeField] private string[] _craftUnlocksOnAccept = Array.Empty<string>();
    public int[] RequiredVisitedStages => _requiredVisitedStages ?? Array.Empty<int>();
    public string SharedGrowthRewardGroup => _sharedGrowthRewardGroup;
    public string[] CraftUnlocksOnAccept => _craftUnlocksOnAccept ?? Array.Empty<string>();
    [HideInInspector]
    [SerializeField, Min(-1)] private int _experienceReward = -1;
    public int ExperienceReward => _experienceReward;
    [Header("수락 시 인벤토리 지급 · 빈 공간 필수")]
    [SerializeField] private ShopTradeRequirement[] _acceptanceItems=Array.Empty<ShopTradeRequirement>();
    public ShopTradeRequirement[] AcceptanceItems=>_acceptanceItems??Array.Empty<ShopTradeRequirement>();
    [Header("Identity")]
    [Tooltip("Stable English identifier used by runtime save data. Do not change after shipping.")]
    [SerializeField] private string _questId = "quest_id";
    [SerializeField] private string _displayName = "Kill Raider 10";
    [TextArea(2, 4)] [SerializeField] private string _description;

    [Header("Home Quest Branch")]
    [Tooltip("Select which Home NPC may offer this quest. Main is the run-critical path; Item and Stats are optional side branches that can still use Main quests as prerequisites.")]
    [SerializeField] private QuestBranch _branch = QuestBranch.Main;

    [Header("Quest Flow")]
    [Tooltip("All prerequisite quests must be complete before this quest can be accepted.")]
    [SerializeField] private QuestDefinitionSO[] _prerequisites = Array.Empty<QuestDefinitionSO>();
    [SerializeField] private QuestObjectiveDefinition[] _objectives = Array.Empty<QuestObjectiveDefinition>();

    [Header("Rewards")]
    [Tooltip("Authored reward data for mission UI and one-time runtime reward transactions.")]
    [SerializeField] private QuestRewardDefinition[] _rewards = Array.Empty<QuestRewardDefinition>();

    [Header("Legacy Reward Note")]
    [Tooltip("Fallback text for old quests that have no linked Item or Stat reward data.")]
    [TextArea(2, 4)] [SerializeField] private string _persistentRewardSummary;
    [Header("Audio")]
    [Tooltip("Optional quest-specific cue; empty uses the shared UIAudio completion cue.")]
    [SerializeField] private AudioCueSO _completionAudioCue;
    [Tooltip("Optional quest-specific cue; empty uses the shared UIAudio accepted cue.")]
    [SerializeField] private AudioCueSO _acceptedAudioCue;

    public string QuestId => string.IsNullOrWhiteSpace(_questId) ? name : _questId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    public string Description => _description;
    public QuestBranch Branch => _branch;
    public QuestDefinitionSO[] Prerequisites => _prerequisites ?? Array.Empty<QuestDefinitionSO>();
    public QuestObjectiveDefinition[] Objectives => _objectives ?? Array.Empty<QuestObjectiveDefinition>();
    public QuestRewardDefinition[] Rewards => _rewards ?? Array.Empty<QuestRewardDefinition>();
    public string PersistentRewardSummary => _persistentRewardSummary;
    public AudioCueSO CompletionAudioCue => _completionAudioCue;
    public AudioCueSO AcceptedAudioCue => _acceptedAudioCue;
}
