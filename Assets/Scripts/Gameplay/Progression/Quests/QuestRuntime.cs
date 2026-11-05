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
    public bool IsReadyToTurnIn
    {
        get
        {
            if (!_accepted || _completed || _definition == null || _definition.Objectives.Length == 0) return false;
            for (int i = 0; i < _definition.Objectives.Length; i++)
                if (_definition.Objectives[i] == null || i >= _objectiveProgress.Length ||
                    _objectiveProgress[i] < _definition.Objectives[i].RequiredCount) return false;
            return true;
        }
    }
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
    private bool _submittingItems;

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

        if(definition.AcceptanceItems.Length>0)
        {
            ResolvePlayerInventory();
            if(_playerInventory==null||!_playerInventory.CanReceiveQuestAcceptance(definition.AcceptanceItems,out _))return false;
        }
        return true;
    }

    // Purpose: Attempts to accept and reports whether the request succeeded.
    public bool TryAccept(QuestDefinitionSO definition)
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        if (!CanAccept(definition))
            return false;

        QuestRuntimeState state = new();
        state.Accept(definition);
        _states.Add(state);
        if(definition.AcceptanceItems.Length>0&&(_playerInventory==null||!_playerInventory.TryReceiveQuestAcceptance(definition.AcceptanceItems,out _)))
        {_states.Remove(state);return false;}
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
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
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
        GameProgressionRuntime.Kill(attacker, target);
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
    public static void ReportHeadshot(GameObject attacker, Damageable target, bool killed, WeaponSO shotWeapon)
    {
        _instance?.ReportCombatEvent(QuestObjectiveType.HeadshotHit, attacker, target, shotWeapon);
        if (killed) _instance?.ReportCombatEvent(QuestObjectiveType.HeadshotKill, attacker, target, shotWeapon);
    }

    private void ReportCombatEvent(QuestObjectiveType eventType, GameObject attacker, Damageable target, WeaponSO shotWeapon = null)
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return;
#endif
        if (!IsPlayerSource(attacker) || target == null)
            return;

        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
        WeaponSO usedWeapon = shotWeapon != null ? shotWeapon : ResolvePlayerWeapon(attacker);
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

            // Goal progress is not completion. Only the Home NPC turn-in button grants rewards.
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
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return;
#endif
        if (_submittingItems) return;
        RefreshCollectObjectives();
    }

    /// <summary>
    /// Explicit submission, unlike CollectItem ownership observation. All remaining rows
    /// are aggregated and consumed atomically through the existing physical-trade path.
    /// Content/quantities are authored later; no quest data is generated here.
    /// </summary>
    public bool TrySubmitItems(QuestDefinitionSO definition, out string message)
    {
        message = "제출할 수 있는 진행 중 퀘스트가 없습니다.";
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) { message = "전투실험 중에는 임무 진행을 변경하지 않습니다."; return false; }
#endif
        if (_submittingItems || definition == null || !CanTurnInAt(_selectedQuestGiver, definition)) return false;
        QuestRuntimeState state = FindState(definition);
        if (state == null || !state.IsAccepted || state.IsCompleted) return false;
        ResolvePlayerInventory();
        if (_playerInventory == null || _playerInventory.GetComponent<Damageable>()?.IsDead == true) return false;
        var requirements = new List<ShopTradeRequirement>();
        var rows = new List<int>();
        for (int i = 0; i < definition.Objectives.Length; i++)
        {
            var goal = definition.Objectives[i];
            if (goal == null || goal.ObjectiveType != QuestObjectiveType.SubmitItem) continue;
            int current = i < state.ObjectiveProgress.Count ? state.ObjectiveProgress[i] : 0;
            int remaining = goal.RequiredCount - current;
            if (remaining <= 0) continue;
            if (goal.TargetItem == null) { message = "제출 대상 Item SO가 지정되지 않았습니다."; return false; }
            requirements.Add(new ShopTradeRequirement(goal.TargetItem, remaining)); rows.Add(i);
        }
        if (rows.Count == 0) return false;
        _submittingItems = true;
        try
        {
            if (!_playerInventory.TryConsumeInventoryItemRequirements(requirements, out message)) return false;
            foreach (int row in rows) state.TrySetProgress(row, definition.Objectives[row].RequiredCount);
            message = "아이템 제출 완료. 같은 항목은 다시 소비하지 않습니다.";
            NotifyStateChanged(); return true;
        }
        finally { _submittingItems = false; }
    }

    private bool CanTurnInAt(HomeQuestNpc giver, QuestDefinitionSO definition)
    {
        ResolvePlayerInventory();
        return giver != null && giver.CanInteract && giver.IsConversationOpen && giver.CanOffer(definition) &&
               _playerInventory != null && _playerInventory.GetComponent<Damageable>()?.IsDead != true &&
               (giver.transform.position - _playerInventory.transform.position).sqrMagnitude <= 100f;
    }

    public bool TryTurnIn(QuestDefinitionSO definition, HomeQuestNpc giver, out string message)
    {
        message = "HOME 임무 NPC와 대화하여 목표를 제출하고 완료 버튼을 누르세요.";
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        var state = FindState(definition);
        if (state == null || !CanTurnInAt(giver, definition) || !state.IsReadyToTurnIn) return false;
        var warehouse=FindFirstObjectByType<InheritanceWarehouseRuntime>(FindObjectsInactive.Include);
        bool needsWarehouse=false;
        foreach(var reward in definition.Rewards)if(reward?.RewardType==QuestRewardType.Item)needsWarehouse=true;
        if(needsWarehouse&&(warehouse==null||!warehouse.CanReceiveQuestItems(state,out message)))return false;
        if(!state.TryComplete())return false;
        HandleQuestCompleted(state);
        NotifyStateChanged();
        message = "임무 완료. 보상이 지급되었습니다.";
        return true;
    }

    // Purpose: Refreshes the displayed or cached collect objectives.
    private bool RefreshCollectObjectives()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
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

            // Keep the ready state until the player explicitly turns this quest in at Home.
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

        GameProgressionRuntime.GrantQuest(state.Definition.QuestId, state.Definition.ExperienceReward);

        PublishSystemMessage($"Quest complete: {state.Definition.DisplayName}");
        UIAudio.PlayQuestNotification(state.Definition.CompletionAudioCue, true);
        ProcessPendingRewardGrants();
    }

    // Purpose: Processes every outstanding individual quest reward exactly once.
    private bool ProcessPendingRewardGrants()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
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
            QuestRewardType.PassiveSlots => TryGrantPassiveSlotReward(definition, rewardIndex, reward),
            QuestRewardType.PocketSlots => TryGrantPocketSlotReward(definition, rewardIndex, reward),
            QuestRewardType.RecyclingSlots => TryGrantRecyclingSlots(definition,rewardIndex,reward),
            _ => false
        };
    }

    // Item rewards are warehouse-only. No inventory/floor fallback may bypass the completion preflight.
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

        var warehouse=FindFirstObjectByType<InheritanceWarehouseRuntime>(FindObjectsInactive.Include);
        if(warehouse==null||!warehouse.TryReceiveQuestItem(item,reward.ItemAmount,definition.QuestId+":"+rewardIndex,out string message))return false;
        PublishSystemMessage($"창고 보상: {item.name} ×{reward.ItemAmount}");return true;
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
    private bool TryGrantPassiveSlotReward(QuestDefinitionSO definition, int rewardIndex, QuestRewardDefinition reward)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.QuestId) || rewardIndex < 0) return false;
        var saves = SaveSystem.Active;
        if (saves == null) return false;
        var records = saves.Profile.QuestPassiveSlotRewards;
        if (!records.Exists(x => x != null && x.questId == definition.QuestId && x.rewardIndex == rewardIndex))
            records.Add(new QuestPassiveSlotRewardSaveData { questId = definition.QuestId, rewardIndex = rewardIndex, slotAmount = reward.PassiveSlotAmount });
        var passive = FindFirstObjectByType<PlayerPassiveInventoryRuntime>();
        if (passive != null) passive.SetUnlockedSlotCount(saves.Profile.UnlockedPassiveSlotCount);
        // Record and capacity are persisted in the same profile, before marking the quest reward granted.
        saves.SaveDataToDisk();
        return true;
    }

    private bool TryGrantPocketSlotReward(QuestDefinitionSO definition, int rewardIndex, QuestRewardDefinition reward)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.QuestId) || rewardIndex < 0) return false;
        var saves = SaveSystem.Active;
        if (saves == null) return false;
        var records = saves.Profile.questPocketSlotRewards ??= new();
        if (!records.Exists(x => x != null && x.questId == definition.QuestId && x.rewardIndex == rewardIndex))
            records.Add(new QuestPassiveSlotRewardSaveData { questId = definition.QuestId, rewardIndex = rewardIndex, slotAmount = reward.PocketSlotAmount });
        _playerInventory?.PublishMedicalItemChange();
        saves.SaveDataToDisk();
        return true;
    }

    private static void ApplyWarehouseCapacityToLiveRuntime(int capacity)
    {
        InheritanceWarehouseRuntime warehouse =
            FindFirstObjectByType<InheritanceWarehouseRuntime>(FindObjectsInactive.Include);
        warehouse?.TryApplyPersistentCapacity(capacity);
    }
    private bool TryGrantRecyclingSlots(QuestDefinitionSO quest,int index,QuestRewardDefinition reward)
    {
        var profile=SaveSystem.Active?.Profile;if(profile==null)return false;
        var receipts=profile.recyclingSlotReceipts??=new();string key=quest.QuestId+":"+index;
        if(receipts.Contains(key))return true;
        receipts.Add(key);profile.recyclingSlotBonus=Mathf.Min(RecyclingRulesSO.Active?.recycling.maximumSlots??10,profile.recyclingSlotBonus+reward.RecyclingSlotAmount);
        return true; // Quest state and bonus are persisted together by the caller.
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
