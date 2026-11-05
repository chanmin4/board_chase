/*
 * Role hierarchy: Gameplay > Progression > Stage
 * Primary responsibility: Manages StageProgressionManager stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public partial class StageProgressionManager : MonoBehaviour
{
    private const string LegacyHighestUnlockedStagePrefsKey =
        "StageProgression.HighestUnlockedStage";

    private enum SectorJudgePhase
    {
        None,
        Judging,
        SuccessCountdown,
        FailureCountdown,
        Resolved
    }

    [Header("Rules")]
    [FormerlySerializedAs("_rules")]
    [Tooltip("Ordered run flow. Each entry owns all map, encounter, Shop and Treasure settings for that stage.")]
    [SerializeField] private StageRunFlowSO _flow;
    [SerializeField] private SectorRulesSO _sectorRules;
    [SerializeField] private int _startingStageIndex = 1;

    [Header("Refs")]
    [SerializeField] private SectorStateManager _sectorStateManager;
    [SerializeField] private StageSectorInstantiator _stageSectorInstantiator;
    [Tooltip("Authoritative ES3 profile. PlayerPrefs is read only once as a legacy Stage unlock migration source.")]
    [SerializeField] private SaveSystem _saveSystem;

    [Header("Direct RUNScene Fallback")]
    [Tooltip("Used only when RUNScene is entered directly without the persistent SceneLoader. The authored HomeScene remains the sole source of HomeSector.")]
    [SerializeField] private GameSceneSO _homeScene;

    [Header("Manager Ready")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;

    [SerializeField] private VoidEventChannelSO _finalStageCompletedChannel;
    [SerializeField] private StageProgressSnapshotEventChannelSO _snapshotChangedChannel;

    [Header("No Hit Tracking")]
    [SerializeField] private HitReceivedEventChannelSO _playerHitReceivedChannel;

    private Coroutine _initialHomeEntryRoutine;
    private AsyncOperationHandle<SceneInstance> _directHomeSceneLoadHandle;
    private bool _directHomeSceneLoadRequested;

    public event Action FinalStageCompleted;

    private SectorJudgePhase _sectorJudgePhase;
    private StageDefinitionSO _currentStage;
    private SectorRuntime _activeSector;
    private SectorOccupancy _activeOccupancy;
    private SectorEnemySpawner _activeEnemySpawner;

    private int _currentStageIndex;
    private float _remainingSeconds;

    private bool _hasRule;
    private bool _isCompleted;
    private bool _stageTransitionPending;
    private bool _finalStageCompleted;
    private bool _currentStageCompletionClaimed;
    private bool _currentStageTookPlayerHit;
    private bool _currentStageResultRecorded;
    private int _consecutiveNoHitStageCount;

    public int CurrentStageIndex => _currentStageIndex;
    public int HighestUnlockedStageIndex => _highestUnlockedStageIndex;
    public bool CurrentStageCompletionClaimed => _currentStageCompletionClaimed;
    public string LastExtractionFailure { get; private set; } = string.Empty;

    // Initial Home entry deliberately has no generated raid map. SceneLoader
    // must wait for this explicit outcome, not infer success from a Home marker
    // that can also be present while a saved raid is failing to resume.
    public bool IsInitialRuntimePrepared { get; private set; }
    public bool InitialEntryRequiresGeneratedMap { get; private set; } = true;
    public string InitialRuntimePreparationFailure { get; private set; } = string.Empty;
    public SectorRuntime InitialHideoutSector => _stageSectorInstantiator != null
        ? _stageSectorInstantiator.HideoutSector : null;

    private int _highestUnlockedStageIndex;

    private bool UseTimedNormalBattleJudge =>
        _sectorRules != null && _sectorRules.UseTimedNormalBattleJudge;

    private bool CompleteEncounterOnEnemyClear =>
        _sectorRules == null || _sectorRules.CompleteNormalBattleOnEnemyClear;

    private float SuccessCountdownSeconds =>
        _sectorRules != null ? _sectorRules.SuccessCountdownSeconds : 5f;

    private float FailureCountdownSeconds =>
        _sectorRules != null ? _sectorRules.FailureCountdownSeconds : 5f;

    private SectorOwner NeutralJudgeResult =>
        _sectorRules != null ? _sectorRules.NeutralJudgeResult : SectorOwner.Neutral;

    private float CurrentNormalBattleTimerSeconds
    {
        get
        {
            return 0f; // All room deadlines are retired; personal suppression owns the HUD clock.
        }
    }

    private bool CurrentResetTimerWhenRequirementLost
    {
        get
        {
            return _currentStage != null &&
                   _currentStage.ResetNormalBattleTimerWhenRequirementLost;
        }
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_saveSystem != null)
            _saveSystem.LoadSaveDataFromDisk();

        int savedBoundary = _saveSystem != null && _saveSystem.saveData != null
            ? _saveSystem.saveData.HighestUnlockedStageIndex
            : -1;

        _highestUnlockedStageIndex = savedBoundary >= 0
            ? Mathf.Max(_startingStageIndex, savedBoundary)
            : Mathf.Max(
                _startingStageIndex,
                PlayerPrefs.GetInt(LegacyHighestUnlockedStagePrefsKey, _startingStageIndex));

        // A profile created before the ES3 conversion has no stage boundary in
        // its JSON payload. Copy the former PlayerPrefs value exactly once.
        if (_saveSystem != null && savedBoundary != _highestUnlockedStageIndex)
            _saveSystem.SetHighestUnlockedStageIndex(_highestUnlockedStageIndex);

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_stageSectorInstantiator == null)
            _stageSectorInstantiator = FindAnyObjectByType<StageSectorInstantiator>();

    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

        if (_playerHitReceivedChannel != null)
            _playerHitReceivedChannel.OnEventRaised += OnPlayerHitReceived;

        // HomeScene is mounted additively before RUNScene in normal flow. In a
        // direct editor launch it can arrive a few frames later, so defer the
        // first generated map until the authored HomeSector is available.
        _initialHomeEntryRoutine = StartCoroutine(InitializeInitialHomeEntry());
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        if (_playerHitReceivedChannel != null)
            _playerHitReceivedChannel.OnEventRaised -= OnPlayerHitReceived;

        if (_initialHomeEntryRoutine != null)
        {
            StopCoroutine(_initialHomeEntryRoutine);
            _initialHomeEntryRoutine = null;
        }

        IsInitialRuntimePrepared = false;

    }

    // Purpose: Initializes initial home entry for use by this component.
    private IEnumerator InitializeInitialHomeEntry()
    {
        IsInitialRuntimePrepared = false;
        InitialEntryRequiresGeneratedMap = true;
        InitialRuntimePreparationFailure = string.Empty;
        const float timeoutSeconds = 10f;
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;

        RequestHomeSceneForInitialEntry();

        while (_stageSectorInstantiator == null ||
               !_stageSectorInstantiator.HasAuthoredHomeSector)
        {
            if (_stageSectorInstantiator == null)
                _stageSectorInstantiator = FindAnyObjectByType<StageSectorInstantiator>();

            RequestHomeSceneForInitialEntry();

            if (Time.realtimeSinceStartup >= deadline)
            {
                InitialRuntimePreparationFailure = "Authored HomeSector did not become available.";
                Debug.LogError(
                    "[StageProgressionManager] Initial Home entry timed out. " +
                    "Make sure HomeScene is loaded additively and contains one HomeSector prefab.",
                    this);
                _initialHomeEntryRoutine = null;
                yield break;
            }

            yield return null;
        }

        RaidRunSaveData pendingRaid = (_saveSystem != null ? _saveSystem : SaveSystem.Active)?.RunData.raid;
        if (pendingRaid != null && pendingRaid.active && pendingRaid.layout != null && !pendingRaid.extractionCommitted)
        {
            IsInitialRuntimePrepared = BeginStage(pendingRaid.layout.stageIndex, pendingRaid);
            if (!IsInitialRuntimePrepared)
            {
                InitialRuntimePreparationFailure = "Saved raid could not be resumed; its snapshot was preserved.";
                Debug.LogError("[Raid] Saved raid could not be resumed. Preserved its snapshot; no replacement raid was generated.", this);
            }
        }
        else if (_flow != null && _flow.TryGetStage(_startingStageIndex, out var firstStage) &&
                 firstStage.MapPolicy != StageMapPolicy.Legacy)
        {
            InitialEntryRequiresGeneratedMap = false;
            IsInitialRuntimePrepared = EnterHideout(); // Generate the raid only on deployment.
            if (!IsInitialRuntimePrepared)
                InitialRuntimePreparationFailure = "Initial Hideout entry failed.";
        }
        else
        {
            IsInitialRuntimePrepared = BeginStage(_startingStageIndex) && EnterHideout();
            if (!IsInitialRuntimePrepared)
                InitialRuntimePreparationFailure = "Initial legacy stage or Hideout entry failed.";
        }

        _initialHomeEntryRoutine = null;
    }

    // Purpose: Requests home scene for initial entry from the owning service.
    private void RequestHomeSceneForInitialEntry()
    {
        if (_stageSectorInstantiator != null && _stageSectorInstantiator.HasAuthoredHomeSector)
            return;

        SceneLoader sceneLoader = FindAnyObjectByType<SceneLoader>();
        if (sceneLoader != null && sceneLoader.EnsureHomeEnvironmentForRun())
            return;

        if (_directHomeSceneLoadRequested)
            return;

        if (_homeScene == null)
        {
            Debug.LogError(
                "[StageProgressionManager] HomeScene fallback is not assigned. " +
                "Assign the authored HomeScene GameSceneSO on StageProgressionManager.",
                this);
            _directHomeSceneLoadRequested = true;
            return;
        }

        _directHomeSceneLoadRequested = true;
        _directHomeSceneLoadHandle =
            _homeScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
        _directHomeSceneLoadHandle.Completed += OnDirectHomeSceneLoaded;
    }

    // Purpose: Responds to the direct home scene loaded callback.
    private void OnDirectHomeSceneLoaded(AsyncOperationHandle<SceneInstance> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
            return;

        Debug.LogError(
            "[StageProgressionManager] Direct RUNScene fallback failed to load HomeScene. " +
            "Verify that HomeScene is an Addressable scene and that its GameSceneSO points to it.",
            this);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return;
#endif
        SyncActiveSector();

        if (!_hasRule || _activeSector == null)
            return;

        if (_stageTransitionPending)
            return;

        if (TryCompleteEncounterByEnemyClear())
            return;

        if (_sectorJudgePhase == SectorJudgePhase.Judging)
        {
            TickNormalBattleJudge();
            return;
        }

        if (_sectorJudgePhase == SectorJudgePhase.SuccessCountdown)
        {
            TickSuccessCountdown();
            return;
        }

        if (_sectorJudgePhase == SectorJudgePhase.FailureCountdown)
        {
            TickFailureCountdown();
            return;
        }

        if (_isCompleted)
            return;

        TickLegacyStartSectorTimer();
    }

    // Purpose: Performs the complete current sector workflow for this component.
    private void CompleteCurrentSector()
    {
        if (_activeSector == null)
            return;

        _remainingSeconds = 0f;
        _isCompleted = true;
        _sectorJudgePhase = SectorJudgePhase.Resolved;

        if (_sectorStateManager != null)
            _sectorStateManager.CompleteSector(_activeSector);
        else
            _activeSector.SetCleared(true);

        // WIP retired Infection Control previously recovered here. Timed-room
        // pressure is now managed by the room's gas-leak stacks instead.

        PublishSnapshot();
    }

    /// <summary>
    /// Explicit player-driven sector bypass used by authored SectorSkipStation
    /// terminals. This reuses the normal clear route so connected sectors,
    /// snapshots, infection recovery, and boss exits remain consistent.
    /// </summary>
    public bool TrySkipActiveSector(SectorRuntime requestedSector)
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        if (!_hasRule ||
            requestedSector == null ||
            requestedSector != _activeSector ||
            requestedSector.IsCleared ||
            _stageTransitionPending ||
            _finalStageCompleted)
        {
            return false;
        }

        // Skipped combat is neither a kill nor a loot source. Remove pending
        // and live encounter enemies before normal sector completion opens the
        // next connections.
        _activeEnemySpawner?.CancelEncounterForSectorSkip();
        CompleteCurrentSector();
        return requestedSector.IsCleared;
    }

    // WIP retired Infection Control compatibility hook. New gameplay must not call this.
    private void RecoverInfectionControlOnBattleRoomClear()
    {
    }

    // Purpose: Performs the complete stage after goal room clear workflow for this component.
    private void CompleteStageAfterGoalRoomClear()
    {
        if (_currentStageCompletionClaimed)
            return;

        _currentStageCompletionClaimed = true;
        RaiseStageCompleteRewards();
        RecordCurrentStageResultForTreasureRoll();

        if (_flow == null ||
            _flow.IsFinalStage(_currentStage) ||
            !_flow.HasNextStage(_currentStage))
        {
            CompleteFinalStage();
            return;
        }

        _isCompleted = true;
        _remainingSeconds = 0f;
        _sectorJudgePhase = SectorJudgePhase.Resolved;

        bool returnedHome = EnterHideout();

        Debug.Log(
            $"[StageProgressionManager] Stage cleared. The player returned to Home; next-stage access remains " +
            $"controlled by completed quest rewards. stage={_currentStageIndex}, " +
            $"highestUnlocked={_highestUnlockedStageIndex}, returnedHome={returnedHome}",
            this);

        if (!returnedHome)
            PublishSnapshot();
    }

    // WIP retired Infection Control compatibility hook. New stage rewards do not recover a global resource.
    private void RaiseStageCompleteRewards()
    {
    }

    // Purpose: Attempts to complete stage from boss exit and reports whether the request succeeded.
    public bool TryCompleteStageFromBossExit(SectorRuntime bossSector)
    {
        if (_sectorStateManager?.CurrentStageMapLayout?.usesRaidLayout == true) return false;
        if (!_hasRule ||
            bossSector == null ||
            !bossSector.IsCleared ||
            _currentStageCompletionClaimed ||
            _stageTransitionPending ||
            _finalStageCompleted ||
            _sectorStateManager == null)
        {
            return false;
        }

        if (!_sectorStateManager.TryGetStageGoalSector(out SectorRuntime goalSector) ||
            goalSector != bossSector ||
            _sectorStateManager.CurrentSector != bossSector ||
            !_sectorStateManager.TryGetStageRoomType(bossSector, out StageRoomType roomType) ||
            roomType != StageRoomType.Boss)
        {
            Debug.LogError(
                $"[StageProgressionManager] Boss exit rejected because its owner is not the current Boss goal sector. " +
                $"sector={bossSector.name}, stage={_currentStageIndex}",
                bossSector);
            return false;
        }

        CompleteStageAfterGoalRoomClear();
        return true;
    }

    /// <summary>
    /// Returns through the one generated normal-room extraction portal. This
    /// ends the current visit without claiming boss completion, rewards, or a
    /// stage unlock; carried runtime inventory remains untouched.
    /// </summary>
    public bool TryExtractToHomeFromNormalExit(SectorRuntime extractionSector, Action<bool> onCompleted = null)
    {
        LastExtractionFailure = string.Empty;
        if (!_hasRule) return RejectHomeExtraction("현재 스테이지 설정이 없습니다.");
        if (extractionSector == null) return RejectHomeExtraction("탈출 요청 섹터가 없습니다.");
        // _isCompleted is the current ROOM clear state, not a completed raid.
        // Clearing an extraction room unlocks its exit; it must never forbid Home entry.
        if (_currentStageCompletionClaimed) return RejectHomeExtraction("스테이지 완료 처리가 이미 진행되었습니다.");
        if (_stageTransitionPending) return RejectHomeExtraction("다른 씬/스테이지 이동이 진행 중입니다.");
        if (_sectorStateManager == null) return RejectHomeExtraction("SectorStateManager가 없습니다.");
        if (_sectorStateManager.CurrentSector != extractionSector)
            return RejectHomeExtraction("탈출 요청 섹터와 플레이어의 현재 섹터가 다릅니다.");

        StageMapLayout raidLayout = _sectorStateManager.CurrentStageMapLayout;
        if (raidLayout != null && raidLayout.usesRaidLayout &&
            RaidRuntime.Active?.State?.extractionCommitted != true)
            return RejectHomeExtraction("RaidRuntime의 탈출 트랜잭션을 거치지 않은 요청입니다.");
        if ((raidLayout == null || !raidLayout.usesRaidLayout) && !extractionSector.IsCleared)
            return RejectHomeExtraction("일반 탈출은 탈출방을 먼저 클리어해야 합니다.");
        PlayerInventoryRuntime extractingPlayer = FindFirstObjectByType<PlayerInventoryRuntime>();
        if (extractingPlayer == null) return RejectHomeExtraction("탈출할 플레이어 인벤토리가 없습니다.");
        if (extractingPlayer.GetComponent<Damageable>()?.IsDead == true)
            return RejectHomeExtraction("사망한 플레이어는 탈출할 수 없습니다.");
        bool accepted = EnterHideout(success =>
        {
            if (success) Debug.Log(
                $"[StageProgressionManager] Early extraction returned the player to Home without stage completion. " +
                $"stage={_currentStageIndex}", this);
            else RejectHomeExtraction("HOME 씬 로딩 또는 플레이어 이동이 완료되지 않았습니다. SceneLoader 오류 로그를 확인하세요.");
            onCompleted?.Invoke(success);
        });
        if (!accepted)
            return RejectHomeExtraction($"HOME 복귀 요청이 거절되었습니다. instantiator={_stageSectorInstantiator != null}, " +
                $"sectors={_sectorStateManager != null}, loader={SceneLoader.Instance != null}, transitionPending={_stageTransitionPending}");
        return true;
    }

    private bool RejectHomeExtraction(string reason)
    {
        LastExtractionFailure = reason;
        Debug.LogWarning("[StageProgressionManager][Extraction] " + reason, this);
        return false;
    }

    // Purpose: Performs the complete final stage workflow for this component.
    private void CompleteFinalStage()
    {
        RecordCurrentStageResultForTreasureRoll();

        _finalStageCompleted = true;
        _isCompleted = true;
        _remainingSeconds = 0f;
        _sectorJudgePhase = SectorJudgePhase.Resolved;

        RunLifecycleCoordinator.NotifyRunCompleted(_currentStageIndex);

        FinalStageCompleted?.Invoke();
        _finalStageCompletedChannel?.RaiseEvent();

        Debug.Log(
            $"[StageProgressionManager] Final stage completed. stage={_currentStageIndex}",
            this);

        if (!EnterHideout())
            PublishSnapshot();
    }

    // Purpose: Checks whether stage unlocked is currently true.
    public bool IsStageUnlocked(int stageIndex)
    {
        return _flow != null &&
               _flow.TryGetStage(stageIndex, out StageDefinitionSO stage) &&
               stage != null &&
               stageIndex <= _highestUnlockedStageIndex;
    }

    /// <summary>
    /// Quest reward entry point. It raises the persistent contiguous unlock
    /// boundary only after confirming that this authored stage belongs to the
    /// active StageRunFlowSO; it never starts a stage or bypasses the loading gate.
    /// </summary>
    public bool TryUnlockStageFromQuest(StageDefinitionSO stage)
    {
        if (stage == null ||
            _flow == null ||
            !_flow.TryGetStage(stage.StageIndex, out StageDefinitionSO registeredStage) ||
            registeredStage != stage)
        {
            Debug.LogError(
                $"[StageProgressionManager] Quest stage unlock rejected because the stage is not in the active flow. " +
                $"stage={(stage != null ? stage.name : "None")}",
                this);
            return false;
        }

        if (stage.StageIndex <= _highestUnlockedStageIndex)
            return true;

        int expectedNextStageIndex = _highestUnlockedStageIndex + 1;
        if (stage.StageIndex != expectedNextStageIndex)
        {
            Debug.LogError(
                $"[StageProgressionManager] Quest stage unlock rejected because it would skip the contiguous progression boundary. " +
                $"requested={stage.StageIndex}, expected={expectedNextStageIndex}, highestUnlocked={_highestUnlockedStageIndex}",
                this);
            return false;
        }

        _highestUnlockedStageIndex = stage.StageIndex;
        PersistHighestUnlockedStage();
        PublishSnapshot();
        return true;
    }

    // Purpose: Attempts to get next unlocked stage and reports whether the request succeeded.
    public bool TryGetNextUnlockedStage(out StageDefinitionSO nextStage)
    {
        nextStage = null;

        return _flow != null &&
               _currentStage != null &&
               _flow.TryGetNextStage(_currentStage, out nextStage) &&
               nextStage != null &&
               IsStageUnlocked(nextStage.StageIndex);
    }

    /// <summary>
    /// Returns the player to the persistent Hideout without registering it as a
    /// stage room. The generated Start sector remains an internal entry node.
    /// </summary>
    public bool EnterHideout(Action<bool> onCompleted = null)
    {
        if (_stageSectorInstantiator == null || _sectorStateManager == null) return false;
        SceneLoader loader = SceneLoader.Instance;
        if (_stageSectorInstantiator.HasAuthoredHomeSector &&
            (loader == null || !loader.IsHomeEnvironmentUnloading))
        {
            bool entered = EnterPreparedHideout();
            if (entered) onCompleted?.Invoke(true);
            return entered;
        }
        if (!Application.isPlaying || loader == null || _stageTransitionPending) return false;
        _stageTransitionPending = true;
        bool accepted = loader.RequestHomeTransition(EnterPreparedHideout, success =>
        {
            _stageTransitionPending = false;
            PublishSnapshot();
            onCompleted?.Invoke(success);
        });
        if (!accepted) _stageTransitionPending = false;
        return accepted; // accepted request, not a completed scene load
    }

    private bool EnterPreparedHideout()
    {
        if (_stageSectorInstantiator == null ||
            _sectorStateManager == null ||
            !_stageSectorInstantiator.EnsureHideout())
        {
            Debug.LogError(
                "[StageProgressionManager] Hideout entry failed because a required runtime reference is missing.",
                this);
            return false;
        }

        SectorRuntime hideout = _stageSectorInstantiator.HideoutSector;
        var home=hideout!=null?hideout.GetComponent<HomeSector>():null;
        if(home!=null&&!home.TryPrepareSafeSpawn(out string spawnError))
        {Debug.LogError("[Home Spawn] "+spawnError,this);return false;}

        if (!_sectorStateManager.SetExternalCurrentSector(
                hideout,
                notifySpawnSystem: true))
        {
            return false;
        }

        _activeSector = null;
        if (RaidRuntime.Active?.State?.extractionCommitted == true)
            GameProgressionRuntime.Settle(true);
        RaidRuntime.Active?.EndRaid();
        var passiveInventory = FindFirstObjectByType<PlayerInventoryRuntime>();
        passiveInventory?.EndPassiveRaid();
        SetActiveSceneForSector(hideout);
        SnapCameraToPlayer();
        PublishSnapshot();
        RunLifecycleCoordinator.NotifyHomeEntered();
        return true;
    }

    /// <summary>
    /// Entry point for Start-hub stage selectors. Stage selection always passes
    /// through the existing loading gate and never grants an unlock implicitly.
    /// </summary>
    public bool TryEnterUnlockedStage(int stageIndex)
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        if (_stageTransitionPending || !IsStageUnlocked(stageIndex))
        {
            Debug.LogWarning(
                $"[StageProgressionManager] Stage entry rejected. " +
                $"requested={stageIndex}, highestUnlocked={_highestUnlockedStageIndex}, " +
                $"transitionPending={_stageTransitionPending}",
                this);
            return false;
        }

        if (!_flow.TryGetStage(stageIndex, out StageDefinitionSO stage) || stage == null)
            return false;

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null)
        {
            Debug.LogError(
                "[StageProgressionManager] SceneLoader is required for Start-hub stage entry.",
                this);
            return false;
        }

        _stageTransitionPending = true;

        bool requestAccepted = sceneLoader.RequestStageTransition(
            stage.DisplayName,
            () =>
            {
                bool built = BeginStage(stage.StageIndex);
                _stageTransitionPending = true;
                return built;
            },
            () =>
            {
                _stageTransitionPending = false;
                PublishSnapshot();
            });

        if (requestAccepted)
            return true;

        _stageTransitionPending = false;
        Debug.LogError(
            $"[StageProgressionManager] Start-hub stage loading gate rejected entry. " +
            $"stage={stage.StageIndex}",
            this);
        return false;
    }

    // Purpose: Begins the stage workflow.
    private bool BeginStage(int stageIndex, RaidRunSaveData restoredRaid = null)
    {
        _currentStageIndex = stageIndex;
        _hasRule = _flow != null && _flow.TryGetStage(stageIndex, out _currentStage);
        _finalStageCompleted = false;
        _currentStageCompletionClaimed = false;
        _sectorJudgePhase = SectorJudgePhase.None;
        _currentStageTookPlayerHit = false;
        _currentStageResultRecorded = false;
        _stageTransitionPending = false;

        if (!_hasRule)
        {
            Debug.LogError(
                $"[StageProgressionManager] StageDefinitionSO not found in StageRunFlowSO. stageIndex={stageIndex}",
                this);
            _activeSector = null;
            PublishSnapshot();
            return false;
        }

        if (_sectorStateManager == null)
        {
            Debug.LogError(
                $"[StageProgressionManager] SectorStateManager is missing. stageIndex={stageIndex}",
                this);
            _activeSector = null;
            PublishSnapshot();
            return false;
        }

        if (_stageSectorInstantiator == null)
        {
            Debug.LogError(
                $"[StageProgressionManager] StageSectorInstantiator is missing. stageIndex={stageIndex}",
                this);
            _activeSector = null;
            PublishSnapshot();
            return false;
        }

        bool generatedStageBuilt = _stageSectorInstantiator.BuildStage(
            _currentStage,
            _sectorStateManager,
            _consecutiveNoHitStageCount,
            restoredRaid);

        if (!generatedStageBuilt)
        {
            Debug.LogError(
                $"[StageProgressionManager] Failed to build generated stage map. stageIndex={stageIndex}",
                this);
            _activeSector = null;
            PublishSnapshot();
            return false;
        }

        // The Hideout is outside every generated stage. Entering a selected stage
        // must therefore begin at that stage's authored StartSector before the
        // player moves into its first battle room.
        StageMapLayout layout = _sectorStateManager.CurrentStageMapLayout;

        if (layout == null ||
            !_sectorStateManager.TryGetSector(restoredRaid != null ? restoredRaid.playerSectorCoord : layout.startSectorCoord, out SectorRuntime entrySector) ||
            entrySector == null ||
            !_sectorStateManager.SetPlayerCurrentSector(
                entrySector,
                notifySpawnSystem: true))
        {
            Debug.LogError(
                $"[StageProgressionManager] Failed to place player in the generated StartSector. " +
                $"stage={stageIndex}",
                this);
            _activeSector = null;
            PublishSnapshot();
            return false;
        }

        int displayStage = _currentStageIndex + 1;
        string displayName = _hasRule ? _currentStage.DisplayName : $"{displayStage} Stage";

        RunResult.SetStage(displayStage, displayName);
        BeginSector(_sectorStateManager != null ? _sectorStateManager.CurrentSector : null);
        SetActiveSceneForSector(entrySector);
        SnapCameraToPlayer();
        RunLifecycleCoordinator.NotifyRunStarted(stageIndex);
        if (restoredRaid == null && !_continuingThroughHatch) (_saveSystem != null ? _saveSystem : SaveSystem.Active)?.TrySaveRunDataToDisk();
        return true;
    }

    // Purpose: Finishes a sector teleport by synchronizing the camera before a loading gate is released.
    private static void SnapCameraToPlayer()
    {
        CameraManager cameraManager = FindAnyObjectByType<CameraManager>();

        if (cameraManager != null)
            cameraManager.SnapToTarget();
    }

    // Purpose: Updates the active scene for sector state.
    private static void SetActiveSceneForSector(SectorRuntime sector)
    {
        if (sector == null)
            return;

        Scene scene = sector.gameObject.scene;
        if (scene.IsValid() && scene.isLoaded && SceneManager.GetActiveScene() != scene)
            SceneManager.SetActiveScene(scene);
    }

    // Purpose: Performs the persist highest unlocked stage workflow for this component.
    private void PersistHighestUnlockedStage()
    {
        if (_saveSystem == null)
        {
            Debug.LogError(
                "[StageProgressionManager] SaveSystem is missing. Stage unlock changes cannot persist.",
                this);
            return;
        }

        _saveSystem.SetHighestUnlockedStageIndex(_highestUnlockedStageIndex);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Ordered stage definitions exposed to editor diagnostics without allowing
    /// the DebugMonitor to mutate the StageRunFlowSO asset.
    /// </summary>
    public IReadOnlyList<StageDefinitionSO> DebugStages =>
        _flow != null
            ? _flow.Stages
            : Array.Empty<StageDefinitionSO>();

    public bool DebugHasNextStage =>
        Application.isPlaying &&
        _hasRule &&
        _flow != null &&
        _currentStage != null &&
        _flow.HasNextStage(_currentStage);

    public string DebugCurrentStageLabel =>
        _currentStage != null
            ? $"Stage {_currentStage.StageIndex}: {_currentStage.DisplayName}"
            : "Not initialized";

    /// <summary>
    /// Sets the persisted contiguous unlock boundary exactly to an existing
    /// stage. Lower values intentionally re-lock every later stage.
    /// </summary>
    public bool DebugTrySetHighestUnlockedStage(int stageIndex)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[StageProgressionManager] Debug stage unlock editing is available only in PlayMode.",
                this);
            return false;
        }

        if (_flow == null ||
            !_flow.TryGetStage(stageIndex, out StageDefinitionSO stage) ||
            stage == null)
        {
            Debug.LogError(
                $"[StageProgressionManager] Debug unlock boundary rejected because the stage does not exist. " +
                $"requested={stageIndex}",
                this);
            return false;
        }

        _highestUnlockedStageIndex = stage.StageIndex;
        PersistHighestUnlockedStage();
        PublishSnapshot();

        Debug.Log(
            $"[StageProgressionManager] DebugMonitor set the highest unlocked stage exactly. " +
            $"highestUnlocked={_highestUnlockedStageIndex}",
            this);
        return true;
    }

    /// <summary>
    /// Immediately rebuilds any configured stage for editor testing. This
    /// intentionally bypasses both the unlock check and the loading gate.
    /// </summary>
    public bool DebugTryBeginStage(int stageIndex)
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[StageProgressionManager] Debug stage movement is available only in PlayMode.",
                this);
            return false;
        }

        if (_stageTransitionPending)
        {
            Debug.LogWarning(
                $"[StageProgressionManager] Debug stage movement was rejected while a loading transition is pending. " +
                $"requested={stageIndex}",
                this);
            return false;
        }

        if (_flow == null ||
            !_flow.TryGetStage(stageIndex, out StageDefinitionSO stage) ||
            stage == null)
        {
            Debug.LogError(
                $"[StageProgressionManager] Debug stage movement rejected because the stage does not exist. " +
                $"requested={stageIndex}",
                this);
            return false;
        }

        _isCompleted = false;
        _stageTransitionPending = false;

        bool built = BeginStage(stage.StageIndex);

        if (built) SceneLoader.Instance?.ReleaseHomeEnvironmentForRun();

        if (built)
        {
            Debug.Log(
                $"[StageProgressionManager] DebugMonitor moved directly to stage={stage.StageIndex}, " +
                $"name={stage.DisplayName}.",
                this);
        }

        return built;
    }

    /// <summary>
    /// Immediately rebuilds the next stage for editor testing. The normal loading
    /// gate is intentionally skipped; BuildStage still publishes StartSectorReady,
    /// so the existing spawn flow relocates the player to the new Start sector.
    /// </summary>
    public bool DebugTryBeginNextStage()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[StageProgressionManager] Debug stage movement is available only in PlayMode.",
                this);
            return false;
        }

        if (_flow == null || !_hasRule || _currentStage == null)
        {
            Debug.LogError(
                "[StageProgressionManager] The current stage is not initialized, so DebugMonitor cannot select the next stage.",
                this);
            return false;
        }

        if (!_flow.TryGetNextStage(_currentStage, out StageDefinitionSO nextStage) ||
            nextStage == null)
        {
            Debug.LogWarning(
                $"[StageProgressionManager] No next stage exists after {_currentStage.DisplayName}.",
                this);
            return false;
        }

        _isCompleted = false;
        _stageTransitionPending = false;

        bool built = BeginStage(nextStage.StageIndex);

        if (built) SceneLoader.Instance?.ReleaseHomeEnvironmentForRun();

        if (built)
        {
            Debug.Log(
                $"[StageProgressionManager] DebugMonitor moved directly to stage={nextStage.StageIndex}, name={nextStage.DisplayName}.",
                this);
        }

        return built;
    }
#endif

    // Purpose: Synchronizes active sector with the current source state.
    private void SyncActiveSector()
    {
        SectorRuntime current =
            _sectorStateManager != null ? _sectorStateManager.CurrentSector : null;

        if (current != _activeSector)
            BeginSector(current);
    }

    // Purpose: Begins the sector workflow.
    private void BeginSector(SectorRuntime sector)
    {
        if (_sectorStateManager != null &&
            sector != null &&
            !_sectorStateManager.IsManagedSector(sector))
        {
            sector = null;
        }

        _activeSector = sector;
        _activeOccupancy = sector != null
            ? sector.GetComponentInChildren<SectorOccupancy>(true)
            : null;
        _activeEnemySpawner = sector != null
            ? sector.GetComponentInChildren<SectorEnemySpawner>(true)
            : null;

        _sectorJudgePhase = SectorJudgePhase.None;

        _isCompleted = sector != null && sector.IsCleared;
        _remainingSeconds = 0f;

        if (!_hasRule || sector == null || _isCompleted)
        {
            PublishSnapshot();
            return;
        }

        if (!ShouldShowPlayerTimerForSector(sector))
        {
            PublishSnapshot();
            return;
        }

        if (ShouldUseTimedNormalBattleJudge(sector))
        {
            _sectorJudgePhase = SectorJudgePhase.Judging;
            _remainingSeconds = Mathf.Max(0f, CurrentNormalBattleTimerSeconds);
            PublishSnapshot();
            return;
        }

        _remainingSeconds = CurrentNormalBattleTimerSeconds;
        PublishSnapshot();
    }

    // Purpose: Checks whether requirement met is currently true.
    private bool IsRequirementMet()
    {
        if (!_hasRule ||
            _activeSector == null ||
            _activeSector.IsCleared ||
            (_activeOccupancy == null && !ShouldCompleteEncounterOnEnemyClear(_activeSector)))
        {
            return false;
        }

        if (ShouldCompleteEncounterOnEnemyClear(_activeSector))
            return _activeEnemySpawner != null &&
                   _activeEnemySpawner.HasCompletedEncounter;

        if (_sectorJudgePhase == SectorJudgePhase.SuccessCountdown)
            return true;

        if (_sectorJudgePhase == SectorJudgePhase.FailureCountdown)
            return false;

        if (_sectorJudgePhase == SectorJudgePhase.Judging)
            return _activeOccupancy.CurrentSnapshot.dominantOwner == SectorOwner.Player;

        return _activeOccupancy.CurrentSnapshot.owner == SectorOwner.Player;
    }

    // Purpose: Publishes the snapshot notification.
    private void PublishSnapshot()
    {
        if (_snapshotChangedChannel == null)
            return;

        bool isStartSector =
            _sectorStateManager != null &&
            _activeSector != null &&
            _sectorStateManager.IsStartSector(_activeSector);

        SectorOwner dominantOwner = _activeOccupancy != null
            ? _activeOccupancy.CurrentSnapshot.dominantOwner
            : SectorOwner.Neutral;

        bool requirementMet = IsRequirementMet();
        bool usesEnemyClearEncounter =
            _activeSector != null &&
            ShouldCompleteEncounterOnEnemyClear(_activeSector);
        bool showPlayerTimer =
            _sectorJudgePhase == SectorJudgePhase.SuccessCountdown ||
            _sectorJudgePhase == SectorJudgePhase.FailureCountdown ||
            ShouldShowPlayerTimerForSector(_activeSector);
        bool playerOwned =
            _activeSector != null &&
            (_activeSector.IsCleared ||
             _sectorJudgePhase == SectorJudgePhase.SuccessCountdown ||
             (!usesEnemyClearEncounter && dominantOwner == SectorOwner.Player));

        float durationSeconds = _sectorJudgePhase == SectorJudgePhase.SuccessCountdown
            ? SuccessCountdownSeconds
            : _sectorJudgePhase == SectorJudgePhase.FailureCountdown
                ? FailureCountdownSeconds
                : _hasRule ? CurrentNormalBattleTimerSeconds : 0f;

        bool isResolveCountdown =
            _sectorJudgePhase == SectorJudgePhase.SuccessCountdown ||
            _sectorJudgePhase == SectorJudgePhase.FailureCountdown;

        SectorOwner resolveCountdownOwner =
            _sectorJudgePhase == SectorJudgePhase.SuccessCountdown ? SectorOwner.Player :
            _sectorJudgePhase == SectorJudgePhase.FailureCountdown ? SectorOwner.Virus :
            SectorOwner.Neutral;

        StageProgressSnapshot snapshot = new StageProgressSnapshot
        {
            isResolveCountdown = isResolveCountdown,
            resolveCountdownOwner = resolveCountdownOwner,
            isFailureCountdown = _sectorJudgePhase == SectorJudgePhase.FailureCountdown,
            isStartSector = isStartSector,
            stageIndex = _currentStageIndex,
            displayName = _hasRule ? _currentStage.DisplayName : string.Empty,

            remainingSeconds = _remainingSeconds,
            durationSeconds = durationSeconds,
            requiredPlayerOwnedCount = _activeSector != null && !isStartSector ? 1 : 0,
            currentPlayerOwnedCount = playerOwned ? 1 : 0,
            showPlayerTimer = showPlayerTimer,
            requirementMet = requirementMet,
            isCompleted = _isCompleted,
            hasNextStage =
                _hasRule &&
                _flow != null &&
                !_flow.IsFinalStage(_currentStage) &&
                _flow.HasNextStage(_currentStage),

            dominantOwner = dominantOwner,
        };

        snapshot.progress01 = snapshot.durationSeconds > 0f
            ? 1f - Mathf.Clamp01(snapshot.remainingSeconds / snapshot.durationSeconds)
            : 0f;

        _snapshotChangedChannel.RaiseEvent(snapshot);
    }

    // Purpose: Handles the incoming sector state manager ready request or event.
    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();
    }

    // Purpose: Responds to the player hit received callback.
    private void OnPlayerHitReceived(GameObject _)
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return;
#endif
        if (!_hasRule || _currentStageIndex <= 0 || _finalStageCompleted)
            return;

        _currentStageTookPlayerHit = true;
    }

    // Purpose: Performs the record current stage result for treasure roll workflow for this component.
    private void RecordCurrentStageResultForTreasureRoll()
    {
        if (_currentStageResultRecorded)
            return;

        _currentStageResultRecorded = true;

        if (!_hasRule || _currentStageIndex <= 0)
            return;

        if (_playerHitReceivedChannel == null)
        {
            _consecutiveNoHitStageCount = 0;
            return;
        }

        _consecutiveNoHitStageCount = _currentStageTookPlayerHit
            ? 0
            : _consecutiveNoHitStageCount + 1;
    }

    // Purpose: Performs the should use timed normal battle judge workflow for this component.
    private bool ShouldUseTimedNormalBattleJudge(SectorRuntime sector)
    {
        if (!UseTimedNormalBattleJudge ||
            sector == null ||
            _activeOccupancy == null ||
            _sectorStateManager == null ||
            _sectorStateManager.IsStartSector(sector) ||
            _sectorStateManager.IsSectorFailed(sector))
        {
            return false;
        }

        if (!_sectorStateManager.TryGetStageRoomType(sector, out StageRoomType roomType))
            return false;

        return IsTimedNormalSector(sector, roomType);
    }

    // Purpose: Performs the should complete encounter on enemy clear workflow for this component.
    private bool ShouldCompleteEncounterOnEnemyClear(SectorRuntime sector)
    {
        if (!CompleteEncounterOnEnemyClear ||
            sector == null ||
            _sectorStateManager == null ||
            _sectorStateManager.IsStartSector(sector) ||
            _sectorStateManager.IsSectorFailed(sector))
        {
            return false;
        }

        if (!_sectorStateManager.TryGetStageRoomType(sector, out StageRoomType roomType))
            return false;

        return IsEnemyClearEncounterRoom(sector, roomType);
    }

    // Purpose: Attempts to complete encounter by enemy clear and reports whether the request succeeded.
    private bool TryCompleteEncounterByEnemyClear()
    {
        if (_activeSector == null ||
            _isCompleted ||
            !ShouldCompleteEncounterOnEnemyClear(_activeSector) ||
            _activeEnemySpawner == null ||
            !_activeEnemySpawner.HasCompletedEncounter)
        {
            return false;
        }

        CompleteCurrentSector();
        return true;
    }

    // Purpose: Processes one update step for normal battle judge.
    private void TickNormalBattleJudge()
    {
        if (_currentStage != null && _currentStage.MapPolicy != StageMapPolicy.Legacy)
        { _remainingSeconds = 0f; return; }
        float timerDuration = _hasRule ? Mathf.Max(0f, CurrentNormalBattleTimerSeconds) : 0f;

        if (timerDuration <= 0f)
        {
            _remainingSeconds = 0f;
            PublishSnapshot();
            return;
        }

        _remainingSeconds = Mathf.Max(0f, _remainingSeconds - Time.deltaTime);

        if (_remainingSeconds <= 0f)
        {
            ApplyNormalBattleTimerExpiredPenalty();
            _remainingSeconds = timerDuration;
        }

        PublishSnapshot();
    }

    // Purpose: Applies the requested normal battle timer expired penalty change.
    private void ApplyNormalBattleTimerExpiredPenalty()
    {
        if (_currentStage != null && _currentStage.MapPolicy != StageMapPolicy.Legacy) return;
        if (!_hasRule)
            return;

        // Infection Control is retired/WIP. A failed room timer now starts a
        // persistent leak in this room, which adds personal Infection until
        // the player clears or leaves it.
        SectorRoomTraitRuntime roomTrait = _activeSector != null
            ? _activeSector.GetComponent<SectorRoomTraitRuntime>()
            : null;

        if (roomTrait != null && roomTrait.TryAddTimerFailureGasLeak(out int stacks))
        {
            Debug.Log(
                $"[StageProgressionManager] Biochemical gas leak intensified. " +
                $"sector={_activeSector.name}, stacks={stacks}",
                _activeSector);
        }
    }

    // Purpose: Resolves the normal battle judge needed by this component.
    private void ResolveNormalBattleJudge()
    {
        if (_activeSector == null)
            return;

        if (_activeOccupancy != null)
            _activeOccupancy.RefreshNow();

        SectorOwner result = _activeOccupancy != null
            ? _activeOccupancy.CurrentSnapshot.owner
            : SectorOwner.Neutral;

        if (result == SectorOwner.Neutral)
            result = NeutralJudgeResult;

        if (result == SectorOwner.Player)
        {
            if (_sectorStateManager != null)
                _sectorStateManager.CompleteSector(_activeSector);
            else
                _activeSector.SetCleared(true);

            _isCompleted = true;
            BeginSuccessCountdown();
            return;
        }

        if (result == SectorOwner.Virus)
        {
            if (_sectorStateManager != null)
                _sectorStateManager.FailSector(_activeSector);

            _isCompleted = false;
            BeginFailureCountdown();
            return;
        }

        _sectorJudgePhase = SectorJudgePhase.Resolved;
        _remainingSeconds = 0f;
        PublishSnapshot();
    }

    // Purpose: Begins the success countdown workflow.
    private void BeginSuccessCountdown()
    {
        _sectorJudgePhase = SectorJudgePhase.SuccessCountdown;
        _remainingSeconds = Mathf.Max(0f, SuccessCountdownSeconds);

        if (_remainingSeconds <= 0f)
        {
            CompleteCurrentSector();
            return;
        }

        PublishSnapshot();
    }

    // Purpose: Processes one update step for success countdown.
    private void TickSuccessCountdown()
    {
        _remainingSeconds = Mathf.Max(0f, _remainingSeconds - Time.deltaTime);

        if (_remainingSeconds <= 0f)
        {
            ResolveSuccessCountdown();
            return;
        }

        PublishSnapshot();
    }

    // Purpose: Begins the failure countdown workflow.
    private void BeginFailureCountdown()
    {
        _sectorJudgePhase = SectorJudgePhase.FailureCountdown;
        _remainingSeconds = Mathf.Max(0f, FailureCountdownSeconds);

        if (_remainingSeconds <= 0f)
        {
            ResolveFailureCountdown();
            return;
        }

        PublishSnapshot();
    }

    // Purpose: Processes one update step for failure countdown.
    private void TickFailureCountdown()
    {
        _remainingSeconds = Mathf.Max(0f, _remainingSeconds - Time.deltaTime);

        if (_remainingSeconds <= 0f)
        {
            ResolveFailureCountdown();
            return;
        }

        PublishSnapshot();
    }

    // Purpose: Resolves the success countdown needed by this component.
    private void ResolveSuccessCountdown()
    {
        if (_activeSector == null)
            return;

        _remainingSeconds = 0f;
        _sectorJudgePhase = SectorJudgePhase.Resolved;

        PublishSnapshot();
    }

    // Purpose: Resolves the failure countdown needed by this component.
    private void ResolveFailureCountdown()
    {
        if (_activeSector == null)
            return;

        _sectorJudgePhase = SectorJudgePhase.Resolved;
        _remainingSeconds = 0f;
        _isCompleted = false;

        PublishSnapshot();
    }

    // Purpose: Processes one update step for legacy start sector timer.
    private void TickLegacyStartSectorTimer()
    {
        bool requirementMet = IsRequirementMet();

        if (requirementMet)
        {
            _remainingSeconds -= Time.deltaTime;

            if (_remainingSeconds <= 0f)
            {
                CompleteCurrentSector();
                return;
            }
        }
        else if (CurrentResetTimerWhenRequirementLost)
        {
            _remainingSeconds = CurrentNormalBattleTimerSeconds;
        }

        PublishSnapshot();
    }

    // Purpose: Performs the should show player timer for sector workflow for this component.
    private bool ShouldShowPlayerTimerForSector(SectorRuntime sector)
    {
        if (_currentStage != null && _currentStage.MapPolicy != StageMapPolicy.Legacy)
            return false;
        if (!_hasRule || sector == null)
            return false;

        if (_sectorStateManager != null &&
            _sectorStateManager.IsStartSector(sector))
        {
            return false;
        }

        if (_sectorStateManager == null ||
            !_sectorStateManager.HasCurrentStageMap)
        {
            return true;
        }

        if (!_sectorStateManager.TryGetStageRoomType(
                sector,
                out StageRoomType roomType))
        {
            return false;
        }

        return IsTimedNormalSector(sector, roomType);
    }

    // Purpose: Checks whether timed normal sector is currently true.
    private static bool IsTimedNormalSector(SectorRuntime sector, StageRoomType roomType)
    {
        return roomType == StageRoomType.Normal &&
               (sector == null || sector.UsesNormalSectorStageDefaultTimer);
    }

    // Purpose: Checks whether enemy clear encounter room is currently true.
    private static bool IsEnemyClearEncounterRoom(SectorRuntime sector, StageRoomType roomType)
    {
        if (roomType == StageRoomType.Boss)
            return true;

        return roomType == StageRoomType.Normal &&
               (sector == null || sector.RequiresNormalSectorEnemyClearForProgression);
    }
}
