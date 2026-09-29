/*
 * Role hierarchy: Gameplay > Progression > Quests > Runtime
 * Primary responsibility: Owns accepted quest progress and consumes player hit/kill reports.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class QuestRuntimeState
{
    [SerializeField] private QuestDefinitionSO _definition;
    [SerializeField] private bool _accepted;
    [SerializeField] private bool _completed;
    [SerializeField] private int[] _objectiveProgress = Array.Empty<int>();
    [SerializeField] private bool[] _rewardGranted = Array.Empty<bool>();

    public QuestDefinitionSO Definition => _definition;
    public bool IsAccepted => _accepted;
    public bool IsCompleted => _completed;
    public IReadOnlyList<int> ObjectiveProgress => _objectiveProgress;
    public bool AreAllRewardsGranted
    {
        get
        {
            int rewardCount = _definition != null ? _definition.Rewards.Length : 0;
            if (rewardCount == 0)
                return true;

            if (_rewardGranted == null || _rewardGranted.Length != rewardCount)
                return false;

            for (int i = 0; i < _rewardGranted.Length; i++)
            {
                if (!_rewardGranted[i])
                    return false;
            }

            return true;
        }
    }

    // Purpose: Performs the accept workflow for this component.
    public void Accept(QuestDefinitionSO definition)
    {
        _definition = definition;
        _accepted = true;
        _completed = false;

        int objectiveCount = definition != null ? definition.Objectives.Length : 0;
        _objectiveProgress = new int[objectiveCount];
        _rewardGranted = new bool[definition != null ? definition.Rewards.Length : 0];
    }

    // Purpose: Attempts to add progress and reports whether the request succeeded.
    public bool TryAddProgress(int objectiveIndex)
    {
        if (!_accepted || _completed || _definition == null ||
            objectiveIndex < 0 || objectiveIndex >= _objectiveProgress.Length)
        {
            return false;
        }

        int required = _definition.Objectives[objectiveIndex].RequiredCount;
        if (_objectiveProgress[objectiveIndex] >= required)
            return false;

        _objectiveProgress[objectiveIndex]++;
        return true;
    }

    // Purpose: Attempts to set progress and reports whether the request succeeded.
    public bool TrySetProgress(int objectiveIndex, int progress)
    {
        if (!_accepted || _completed || _definition == null ||
            objectiveIndex < 0 || objectiveIndex >= _objectiveProgress.Length)
        {
            return false;
        }

        int required = _definition.Objectives[objectiveIndex].RequiredCount;
        int clampedProgress = Mathf.Clamp(progress, 0, required);
        if (_objectiveProgress[objectiveIndex] == clampedProgress)
            return false;

        _objectiveProgress[objectiveIndex] = clampedProgress;
        return true;
    }

    // Purpose: Attempts to complete and reports whether the request succeeded.
    public bool TryComplete()
    {
        if (!_accepted || _completed || _definition == null)
            return false;

        QuestObjectiveDefinition[] objectives = _definition.Objectives;
        if (objectives.Length == 0)
            return false;

        for (int i = 0; i < objectives.Length; i++)
        {
            if (i >= _objectiveProgress.Length || _objectiveProgress[i] < objectives[i].RequiredCount)
                return false;
        }

        _completed = true;
        return true;
    }

    // Purpose: Performs the debug complete workflow for this component.
    public void DebugComplete()
    {
        if (_definition == null)
            return;

        QuestObjectiveDefinition[] objectives = _definition.Objectives;
        if (_objectiveProgress == null || _objectiveProgress.Length != objectives.Length)
            _objectiveProgress = new int[objectives.Length];

        _accepted = true;
        for (int i = 0; i < objectives.Length; i++)
            _objectiveProgress[i] = objectives[i].RequiredCount;

        _completed = true;
    }

    // Purpose: Checks whether the requested authored reward has already been granted.
    public bool IsRewardGranted(int rewardIndex)
    {
        EnsureRewardStateLength();
        return rewardIndex >= 0 && rewardIndex < _rewardGranted.Length && _rewardGranted[rewardIndex];
    }

    // Purpose: Records a completed reward transaction and reports whether the state changed.
    public bool TryMarkRewardGranted(int rewardIndex)
    {
        EnsureRewardStateLength();
        if (rewardIndex < 0 || rewardIndex >= _rewardGranted.Length || _rewardGranted[rewardIndex])
            return false;

        _rewardGranted[rewardIndex] = true;
        return true;
    }

    // Purpose: Restores the saved restore state.
    public void Restore(
        QuestDefinitionSO definition,
        bool accepted,
        bool completed,
        IReadOnlyList<int> objectiveProgress,
        IReadOnlyList<bool> rewardGranted)
    {
        _definition = definition;
        _accepted = accepted && definition != null;
        _completed = _accepted && completed;

        int objectiveCount = definition != null ? definition.Objectives.Length : 0;
        _objectiveProgress = new int[objectiveCount];

        for (int i = 0; i < objectiveCount; i++)
        {
            int progress = objectiveProgress != null && i < objectiveProgress.Count
                ? objectiveProgress[i]
                : 0;
            _objectiveProgress[i] = Mathf.Clamp(
                progress,
                0,
                definition.Objectives[i].RequiredCount);
        }

        int rewardCount = definition != null ? definition.Rewards.Length : 0;
        _rewardGranted = new bool[rewardCount];
        for (int i = 0; i < rewardCount; i++)
        {
            _rewardGranted[i] = rewardGranted != null &&
                                i < rewardGranted.Count &&
                                rewardGranted[i];
        }
    }

    // Purpose: Performs the to save data workflow for this component.
    public QuestProgressSaveData ToSaveData()
    {
        return new QuestProgressSaveData
        {
            questId = _definition != null ? _definition.QuestId : string.Empty,
            accepted = _accepted,
            completed = _completed,
            objectiveProgress = _objectiveProgress != null
                ? (int[])_objectiveProgress.Clone()
                : Array.Empty<int>(),
            rewardGranted = _rewardGranted != null
                ? (bool[])_rewardGranted.Clone()
                : Array.Empty<bool>()
        };
    }

    // Purpose: Resizes the persisted reward transaction flags while preserving already granted entries.
    private void EnsureRewardStateLength()
    {
        int rewardCount = _definition != null ? _definition.Rewards.Length : 0;
        if (_rewardGranted != null && _rewardGranted.Length == rewardCount)
            return;

        bool[] resized = new bool[rewardCount];
        if (_rewardGranted != null)
            Array.Copy(_rewardGranted, resized, Mathf.Min(_rewardGranted.Length, resized.Length));

        _rewardGranted = resized;
    }
}

[DisallowMultipleComponent]
public sealed class QuestRuntime : MonoBehaviour
{
    private static QuestRuntime _instance;

    [SerializeField] private List<QuestRuntimeState> _states = new();
    [NonSerialized] private HomeQuestNpc _selectedQuestGiver;
    private SystemMessageEventChannelSO _systemMessageChannel;
    private PlayerInventoryRuntime _playerInventory;
    private float _nextInventoryResolveTime;
    private bool _hasRestoredPersistentState;
    private bool _isGrantingRewards;

    public static QuestRuntime Instance => _instance;
    public IReadOnlyList<QuestRuntimeState> States => _states;
    public HomeQuestNpc SelectedQuestGiver => _selectedQuestGiver;
    public event Action OnQuestStateChanged;

    // Purpose: Ensures runtime is ready before the workflow continues.
    public static QuestRuntime EnsureRuntime()
    {
        if (_instance != null)
            return _instance;

        QuestRuntime existing = FindFirstObjectByType<QuestRuntime>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject runtimeObject = new("QuestRuntime");
        return runtimeObject.AddComponent<QuestRuntime>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_playerInventory != null)
            _playerInventory.OnChanged -= HandlePlayerInventoryChanged;

        if (_instance == this)
            _instance = null;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (_playerInventory != null || Time.unscaledTime < _nextInventoryResolveTime)
            return;

        _nextInventoryResolveTime = Time.unscaledTime + 1f;
        ResolvePlayerInventory();
    }

    // Purpose: Selects quest giver for the active workflow.
    public void SelectQuestGiver(HomeQuestNpc questGiver)
    {
        RestorePersistentStates(BuildHomeQuestCatalog(questGiver));
        _selectedQuestGiver = questGiver;
        OnQuestStateChanged?.Invoke();
    }

    // Purpose: Builds the full Home NPC quest catalog once so persistent Main, Item, and Stats progress restore together.
    private static List<QuestDefinitionSO> BuildHomeQuestCatalog(HomeQuestNpc selectedGiver)
    {
        List<QuestDefinitionSO> catalog = new();
        HashSet<QuestDefinitionSO> uniqueDefinitions = new();
        HomeQuestNpc[] homeQuestNpcs = FindObjectsByType<HomeQuestNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int npcIndex = 0; npcIndex < homeQuestNpcs.Length; npcIndex++)
            AddOfferedQuests(homeQuestNpcs[npcIndex], catalog, uniqueDefinitions);

        // Supports an isolated quest giver used in a test scene that is not included in the active HomeSector hierarchy.
        AddOfferedQuests(selectedGiver, catalog, uniqueDefinitions);
        return catalog;
    }

    // Purpose: Adds unique authored quest offers from one resident to the persistent restore catalog.
    private static void AddOfferedQuests(
        HomeQuestNpc questGiver,
        List<QuestDefinitionSO> catalog,
        HashSet<QuestDefinitionSO> uniqueDefinitions)
    {
        if (questGiver == null)
            return;

        QuestDefinitionSO[] offeredQuests = questGiver.OfferedQuests;
        for (int questIndex = 0; questIndex < offeredQuests.Length; questIndex++)
        {
            QuestDefinitionSO definition = offeredQuests[questIndex];
            if (definition != null && uniqueDefinitions.Add(definition))
                catalog.Add(definition);
        }
    }

    /// <summary>
    /// Restores quest states only after an authored giver provides a concrete
    /// QuestDefinitionSO catalog. Save files store stable quest ids, never
    /// ScriptableObject references, so moving assets does not break progress.
    /// </summary>
    public void RestorePersistentStates(IReadOnlyList<QuestDefinitionSO> definitions)
    {
        if (_hasRestoredPersistentState || definitions == null)
            return;

        SaveSystem saveSystem = SaveSystem.Active;
        if (saveSystem == null || saveSystem.saveData == null)
            return;

        Dictionary<string, QuestDefinitionSO> definitionsById =
            new Dictionary<string, QuestDefinitionSO>(StringComparer.Ordinal);

        for (int i = 0; i < definitions.Count; i++)
        {
            QuestDefinitionSO definition = definitions[i];
            if (definition != null && !string.IsNullOrWhiteSpace(definition.QuestId))
                definitionsById[definition.QuestId] = definition;
        }

        _states.Clear();

        List<QuestProgressSaveData> savedStates = saveSystem.saveData.QuestProgress;
        for (int i = 0; i < savedStates.Count; i++)
        {
            QuestProgressSaveData savedState = savedStates[i];
            if (savedState == null ||
                !definitionsById.TryGetValue(savedState.questId, out QuestDefinitionSO definition))
            {
                continue;
            }

            QuestRuntimeState restored = new QuestRuntimeState();
            restored.Restore(
                definition,
                savedState.accepted,
                savedState.completed,
                savedState.objectiveProgress,
                savedState.rewardGranted);
            _states.Add(restored);
        }

        _hasRestoredPersistentState = true;
        bool rewardsChanged = ProcessPendingRewardGrants();
        if (rewardsChanged)
            PersistStates();
        OnQuestStateChanged?.Invoke();
    }

    // Purpose: Checks whether accept is currently permitted.
    public bool CanAccept(QuestDefinitionSO definition)
    {
        if (definition == null || FindState(definition) != null)
            return false;

        QuestDefinitionSO[] prerequisites = definition.Prerequisites;
        for (int i = 0; i < prerequisites.Length; i++)
        {
            QuestDefinitionSO prerequisite = prerequisites[i];
            if (prerequisite == null)
                continue;

            QuestRuntimeState state = FindState(prerequisite);
            if (state == null || !state.IsCompleted)
                return false;
        }

        return true;
    }

    // Purpose: Attempts to accept and reports whether the request succeeded.
    public bool TryAccept(QuestDefinitionSO definition)
    {
        if (!CanAccept(definition))
            return false;

        QuestRuntimeState state = new();
        state.Accept(definition);
        _states.Add(state);
        PublishSystemMessage($"Quest accepted: {definition.DisplayName}");
        UIAudio.PlayQuestNotification(definition.AcceptedAudioCue, false);

        ResolvePlayerInventory();
        if (!RefreshCollectObjectives())
            NotifyStateChanged();

        return true;
    }

    // Purpose: Performs the debug complete workflow for this component.
    public bool DebugComplete(QuestDefinitionSO definition)
    {
        if (definition == null)
            return false;

        QuestRuntimeState state = FindState(definition);
        if (state == null)
        {
            state = new QuestRuntimeState();
            state.Accept(definition);
            _states.Add(state);
        }

        if (state.IsCompleted)
            return false;
        state.DebugComplete();
        HandleQuestCompleted(state);
        NotifyStateChanged();
        return true;
    }

    // Purpose: Performs the report entity hit workflow for this component.
    public static void ReportEntityHit(GameObject attacker, Damageable target)
    {
        _instance?.ReportCombatEvent(QuestObjectiveType.Hit, attacker, target);
    }

    // Purpose: Performs the report entity killed workflow for this component.
    public static void ReportEntityKilled(GameObject attacker, Damageable target)
    {
        _instance?.ReportCombatEvent(QuestObjectiveType.Kill, attacker, target);
    }

    /// <summary>
    /// Shared progression consumers, such as the Home shop, can query whether
    /// this session has completed a quest. Persistent consumers must save their
    /// own granted unlock after observing this state.
    /// </summary>
    public static bool IsCompleted(QuestDefinitionSO definition)
    {
        if (_instance == null || definition == null)
            return false;

        QuestRuntimeState state = _instance.FindState(definition);
        return state != null && state.IsCompleted;
    }

    // Purpose: Performs the report combat event workflow for this component.
    private void ReportCombatEvent(QuestObjectiveType eventType, GameObject attacker, Damageable target)
    {
        if (!IsPlayerSource(attacker) || target == null)
            return;

        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
        WeaponSO usedWeapon = ResolvePlayerWeapon(attacker);
        bool stateChanged = false;

        for (int stateIndex = 0; stateIndex < _states.Count; stateIndex++)
        {
            QuestRuntimeState state = _states[stateIndex];
            if (state == null || !state.IsAccepted || state.IsCompleted || state.Definition == null)
                continue;

            QuestObjectiveDefinition[] objectives = state.Definition.Objectives;
            for (int objectiveIndex = 0; objectiveIndex < objectives.Length; objectiveIndex++)
            {
                QuestObjectiveDefinition objective = objectives[objectiveIndex];
                if (objective == null || objective.ObjectiveType != eventType || !objective.Matches(target, distance, usedWeapon))
                    continue;

                if (!state.TryAddProgress(objectiveIndex))
                    continue;

                int current = state.ObjectiveProgress[objectiveIndex];
                PublishSystemMessage($"{state.Definition.DisplayName}: {objective.BuildSummary()} {current}/{objective.RequiredCount}");
                stateChanged = true;
            }

            if (state.TryComplete())
            {
                HandleQuestCompleted(state);
                stateChanged = true;
            }
        }

        if (stateChanged)
            NotifyStateChanged();
    }

    // Purpose: Finds the requested state.
    private QuestRuntimeState FindState(QuestDefinitionSO definition)
    {
        for (int i = 0; i < _states.Count; i++)
        {
            QuestRuntimeState state = _states[i];
            if (state != null && state.Definition == definition)
                return state;
        }

        return null;
    }

    // Purpose: Checks whether player source is currently true.
    private static bool IsPlayerSource(GameObject source)
    {
        return source != null && source.GetComponentInParent<PlayerStatsRuntime>() != null;
    }

    // Purpose: Resolves the player weapon needed by this component.
    private static WeaponSO ResolvePlayerWeapon(GameObject source)
    {
        PlayerStatsRuntime playerStats = source != null
            ? source.GetComponentInParent<PlayerStatsRuntime>()
            : null;
        return playerStats != null ? playerStats.CurrentWeapon : null;
    }

    // Purpose: Resolves the player inventory needed by this component.
    private void ResolvePlayerInventory()
    {
        PlayerInventoryRuntime resolved = FindFirstObjectByType<PlayerInventoryRuntime>(FindObjectsInactive.Include);
        if (resolved == _playerInventory)
            return;

        if (_playerInventory != null)
            _playerInventory.OnChanged -= HandlePlayerInventoryChanged;

        _playerInventory = resolved;

        if (_playerInventory == null)
            return;

        _playerInventory.OnChanged += HandlePlayerInventoryChanged;
        RefreshCollectObjectives();

        if (ProcessPendingRewardGrants())
            NotifyStateChanged();
    }

    // Purpose: Handles the incoming player inventory changed request or event.
    private void HandlePlayerInventoryChanged()
    {
        RefreshCollectObjectives();
    }

    // Purpose: Refreshes the displayed or cached collect objectives.
    private bool RefreshCollectObjectives()
    {
        if (_playerInventory == null)
            return false;

        bool stateChanged = false;

        for (int stateIndex = 0; stateIndex < _states.Count; stateIndex++)
        {
            QuestRuntimeState state = _states[stateIndex];
            if (state == null || !state.IsAccepted || state.IsCompleted || state.Definition == null)
                continue;

            QuestObjectiveDefinition[] objectives = state.Definition.Objectives;
            for (int objectiveIndex = 0; objectiveIndex < objectives.Length; objectiveIndex++)
            {
                QuestObjectiveDefinition objective = objectives[objectiveIndex];
                if (objective == null || objective.ObjectiveType != QuestObjectiveType.CollectItem || objective.TargetItem == null)
                    continue;

                int current = objectiveIndex < state.ObjectiveProgress.Count
                    ? state.ObjectiveProgress[objectiveIndex]
                    : 0;
                int ownedAmount = _playerInventory.GetInventoryItemAmount(objective.TargetItem);
                if (ownedAmount <= current || !state.TrySetProgress(objectiveIndex, ownedAmount))
                    continue;

                int updated = state.ObjectiveProgress[objectiveIndex];
                PublishSystemMessage(
                    $"{state.Definition.DisplayName}: {objective.BuildSummary()} {updated}/{objective.RequiredCount}");
                stateChanged = true;
            }

            if (state.TryComplete())
            {
                HandleQuestCompleted(state);
                stateChanged = true;
            }
        }

        if (stateChanged)
            NotifyStateChanged();

        return stateChanged;
    }

    // Purpose: Publishes completion and starts one-time reward transactions for a completed quest.
    private void HandleQuestCompleted(QuestRuntimeState state)
    {
        if (state == null || state.Definition == null)
            return;

        PublishSystemMessage($"Quest complete: {state.Definition.DisplayName}");
        UIAudio.PlayQuestNotification(state.Definition.CompletionAudioCue, true);
        ProcessPendingRewardGrants();
    }

    // Purpose: Processes every outstanding individual quest reward exactly once.
    private bool ProcessPendingRewardGrants()
    {
        if (_isGrantingRewards)
            return false;

        _isGrantingRewards = true;
        bool stateChanged = false;

        try
        {
            for (int stateIndex = 0; stateIndex < _states.Count; stateIndex++)
            {
                QuestRuntimeState state = _states[stateIndex];
                if (state == null || !state.IsCompleted || state.Definition == null || state.AreAllRewardsGranted)
                    continue;

                QuestRewardDefinition[] rewards = state.Definition.Rewards;
                for (int rewardIndex = 0; rewardIndex < rewards.Length; rewardIndex++)
                {
                    if (state.IsRewardGranted(rewardIndex) ||
                        !TryGrantReward(state.Definition, rewardIndex, rewards[rewardIndex]))
                    {
                        continue;
                    }

                    stateChanged |= state.TryMarkRewardGranted(rewardIndex);
                }
            }
        }
        finally
        {
            _isGrantingRewards = false;
        }

        return stateChanged;
    }

    // Purpose: Attempts one authored reward transaction and preserves it until it succeeds.
    private bool TryGrantReward(
        QuestDefinitionSO definition,
        int rewardIndex,
        QuestRewardDefinition reward)
    {
        if (reward == null)
        {
            Debug.LogError(
                $"[QuestRuntime] Quest reward is missing. quest={definition.QuestId}, rewardIndex={rewardIndex}",
                this);
            return false;
        }

        return reward.RewardType switch
        {
            QuestRewardType.Item => TryGrantItemReward(definition, rewardIndex, reward),
            QuestRewardType.Stat => TryGrantPersistentStatReward(definition, rewardIndex, reward),
            QuestRewardType.ShopUnlock => TryGrantShopUnlockReward(definition, rewardIndex, reward),
            QuestRewardType.StageUnlock => TryGrantStageUnlockReward(definition, rewardIndex, reward),
            QuestRewardType.WarehouseSlots => TryGrantWarehouseSlotReward(definition, rewardIndex, reward),
            _ => false
        };
    }

    // Purpose: Gives an authored item reward to inventory, or preserves it as a floor drop if inventory cannot accept it.
    private bool TryGrantItemReward(
        QuestDefinitionSO definition,
        int rewardIndex,
        QuestRewardDefinition reward)
    {
        ItemSO item = reward.Item;
        if (item == null)
        {
            Debug.LogError(
                $"[QuestRuntime] Item reward is missing its ItemSO. quest={definition.QuestId}, rewardIndex={rewardIndex}",
                this);
            return false;
        }

        if (_playerInventory == null)
            ResolvePlayerInventory();

        if (_playerInventory == null)
            return false;

        int amount = reward.ItemAmount;
        if (_playerInventory.TryPickup(item, amount, out _))
        {
            PublishSystemMessage($"Quest reward received: {item.name} x{amount}");
            return true;
        }

        Vector3 dropPosition = _playerInventory.transform.position + Vector3.forward * 0.45f;
        if (!WorldItemPickupSpawner.TrySpawn(
                item,
                amount,
                amount,
                0f,
                -1f,
                dropPosition,
                Quaternion.identity,
                out _))
        {
            return false;
        }

        PublishSystemMessage($"Quest reward dropped at feet: {item.name} x{amount}");
        return true;
    }

    // Purpose: Adds one permanent direct-stat reward to the save profile and rebuilds the live player snapshot.
    private bool TryGrantPersistentStatReward(
        QuestDefinitionSO definition,
        int rewardIndex,
        QuestRewardDefinition reward)
    {
        SaveSystem saveSystem = SaveSystem.Active;
        if (saveSystem == null || saveSystem.saveData == null)
            return false;

        saveSystem.saveData.EnsureRuntimeDefaults();
        List<QuestStatRewardSaveData> savedRewards = saveSystem.saveData.QuestStatRewards;

        for (int i = 0; i < savedRewards.Count; i++)
        {
            QuestStatRewardSaveData existing = savedRewards[i];
            if (existing != null &&
                existing.questId == definition.QuestId &&
                existing.rewardIndex == rewardIndex)
            {
                RebuildLivePlayerStats();
                return true;
            }
        }

        savedRewards.Add(new QuestStatRewardSaveData
        {
            questId = definition.QuestId,
            rewardIndex = rewardIndex,
            stat = reward.Stat,
            modifierType = reward.StatModifierType,
            value = reward.StatValue
        });

        RebuildLivePlayerStats();
        PublishSystemMessage($"Quest reward applied: {reward.BuildSummary()}");
        return true;
    }

    // Purpose: Grants one explicit Home-shop node unlock and keeps the quest transaction pending on invalid authored data.
    private bool TryGrantShopUnlockReward(
        QuestDefinitionSO definition,
        int rewardIndex,
        QuestRewardDefinition reward)
    {
        if (reward.ShopConfig == null || string.IsNullOrWhiteSpace(reward.ShopUnlockNodeId))
        {
            Debug.LogError(
                $"[QuestRuntime] Shop unlock reward is incomplete. quest={definition.QuestId}, rewardIndex={rewardIndex}",
                this);
            return false;
        }

        SaveSystem saveSystem = SaveSystem.Active;
        if (saveSystem == null || saveSystem.saveData == null)
            return false;

        bool unlocked = ShopUnlockRuntime.EnsureRuntime().TryUnlockNode(
            reward.ShopConfig,
            reward.ShopUnlockNodeId,
            saveSystem);
        if (!unlocked)
            return false;

        PublishSystemMessage($"Quest reward unlocked: {reward.BuildSummary()}");
        return true;
    }

    // Purpose: Raises the persistent stage boundary through the existing StageProgressionManager validation path.
    private bool TryGrantStageUnlockReward(
        QuestDefinitionSO definition,
        int rewardIndex,
        QuestRewardDefinition reward)
    {
        if (reward.StageToUnlock == null)
        {
            Debug.LogError(
                $"[QuestRuntime] Stage unlock reward is missing its StageDefinitionSO. " +
                $"quest={definition.QuestId}, rewardIndex={rewardIndex}",
                this);
            return false;
        }

        StageProgressionManager progression =
            FindFirstObjectByType<StageProgressionManager>(FindObjectsInactive.Include);
        if (progression == null || !progression.TryUnlockStageFromQuest(reward.StageToUnlock))
            return false;

        PublishSystemMessage($"Quest reward unlocked: {reward.BuildSummary()}");
        return true;
    }

    /// <summary>
    /// Adds a one-time permanent capacity increase to the inheritance warehouse.
    /// The per-reward save record prevents a completed quest from expanding it twice
    /// if the game closes between the grant and quest-state persistence.
    /// </summary>
    private bool TryGrantWarehouseSlotReward(
        QuestDefinitionSO definition,
        int rewardIndex,
        QuestRewardDefinition reward)
    {
        SaveSystem saveSystem = SaveSystem.Active;
        if (saveSystem == null || saveSystem.saveData == null)
            return false;

        saveSystem.saveData.EnsureRuntimeDefaults();
        List<QuestWarehouseSlotRewardSaveData> savedRewards =
            saveSystem.saveData.QuestWarehouseSlotRewards;

        for (int i = 0; i < savedRewards.Count; i++)
        {
            QuestWarehouseSlotRewardSaveData existing = savedRewards[i];
            if (existing == null ||
                existing.questId != definition.QuestId ||
                existing.rewardIndex != rewardIndex)
            {
                continue;
            }

            ApplyWarehouseCapacityToLiveRuntime(saveSystem.saveData.InheritanceWarehouse.capacity);
            return true;
        }

        int slotAmount = reward.WarehouseSlotAmount;
        InheritanceWarehouseSaveData warehouse = saveSystem.saveData.InheritanceWarehouse;
        warehouse.capacity = Mathf.Max(1, warehouse.capacity + slotAmount);
        warehouse.EnsureDefaults();

        savedRewards.Add(new QuestWarehouseSlotRewardSaveData
        {
            questId = definition.QuestId,
            rewardIndex = rewardIndex,
            slotAmount = slotAmount
        });

        ApplyWarehouseCapacityToLiveRuntime(warehouse.capacity);
        saveSystem.SaveDataToDisk();
        PublishSystemMessage($"Quest reward applied: {reward.BuildSummary()}");
        return true;
    }

    // Purpose: Updates an already-open Home warehouse after a quest reward changes its saved capacity.
    private static void ApplyWarehouseCapacityToLiveRuntime(int capacity)
    {
        InheritanceWarehouseRuntime warehouse =
            FindFirstObjectByType<InheritanceWarehouseRuntime>(FindObjectsInactive.Include);
        warehouse?.TryApplyPersistentCapacity(capacity);
    }

    // Purpose: Rebuilds the active player stat snapshot after a persistent quest modifier changes.
    private static void RebuildLivePlayerStats()
    {
        PlayerStatsRuntime playerStats = FindFirstObjectByType<PlayerStatsRuntime>(FindObjectsInactive.Include);
        playerStats?.RebuildAndPublish();
    }

    // Purpose: Publishes the state changed notification.
    private void NotifyStateChanged()
    {
        PersistStates();
        OnQuestStateChanged?.Invoke();
    }

    // Purpose: Performs the persist states workflow for this component.
    private void PersistStates()
    {
        SaveSystem saveSystem = SaveSystem.Active;
        if (saveSystem == null || saveSystem.saveData == null || !_hasRestoredPersistentState)
            return;

        List<QuestProgressSaveData> savedStates = saveSystem.saveData.QuestProgress;
        savedStates.Clear();

        for (int i = 0; i < _states.Count; i++)
        {
            QuestRuntimeState state = _states[i];
            if (state == null || state.Definition == null || !state.IsAccepted)
                continue;

            QuestProgressSaveData savedState = state.ToSaveData();
            if (!string.IsNullOrWhiteSpace(savedState.questId))
                savedStates.Add(savedState);
        }

        saveSystem.SaveDataToDisk();
    }

    // Purpose: Publishes the system message notification.
    private void PublishSystemMessage(string message)
    {
        if (_systemMessageChannel == null)
        {
            SystemMessageEventChannelSO[] channels = Resources.FindObjectsOfTypeAll<SystemMessageEventChannelSO>();
            if (channels.Length > 0)
                _systemMessageChannel = channels[0];
        }

        if (_systemMessageChannel != null)
            _systemMessageChannel.RaiseEvent(message, 3f);
        else
            Debug.Log($"[QuestRuntime] {message}", this);
    }
}
