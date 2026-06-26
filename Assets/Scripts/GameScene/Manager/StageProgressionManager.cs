using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class StageProgressionManager : MonoBehaviour
{
    private enum SectorJudgePhase
    {
        None,
        Judging,
        SuccessCountdown,
        FailureCountdown,
        Resolved
    }

    [Header("Rules")]
    [SerializeField] private StageProgressionRulesSO _rules;
    [SerializeField] private SectorRulesSO _sectorRules;
    [SerializeField] private StageBattleSettingsSO _stageBattleSettings;
    [SerializeField] private int _startingStageIndex = 0;

    [Header("Stage Transition")]
    [SerializeField] private bool _cleanupBeforeNextStage = true;
    [SerializeField] private bool _clearPaintOnStageTransition = true;

    [Header("Refs")]
    [SerializeField] private SectorStateManager _sectorStateManager;
    [SerializeField] private SectorCleanupApplier _sectorCleanupApplier;
    [SerializeField] private StageSectorInstantiator _stageSectorInstantiator;

    [Header("Listening To")]
    [SerializeField] private IntEventChannelSO _stageAppliedChannel;

    [Header("Broadcasting On")]
    [SerializeField] private VoidEventChannelSO _requestProgressNextStageChannel;

    [Header("Manager Ready")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [SerializeField] private NamedSectorControllerReadyEventChannelSO _namedSectorControllerReadyChannel;
    [SerializeField] private InfectionControlManagerReadyEventChannelSO _infectionControlManagerReadyChannel;

    [SerializeField] private VoidEventChannelSO _finalStageCompletedChannel;
    [SerializeField] private StageProgressSnapshotEventChannelSO _snapshotChangedChannel;
    [SerializeField] private FloatEventChannelSO _stageInfectionControlRecoverChannel;

    [Header("No Hit Tracking")]
    [SerializeField] private HitReceivedEventChannelSO _playerHitReceivedChannel;

    private NamedSectorController _namedSectorController;
    private InfectionControlManager _infectionControlManager;

    public event Action FinalStageCompleted;

    private SectorJudgePhase _sectorJudgePhase;
    private StageProgressionRulesSO.StageProgressRule _currentRule;
    private SectorRuntime _activeSector;
    private SectorOccupancy _activeOccupancy;
    private SectorEnemySpawner _activeEnemySpawner;

    private int _currentStageIndex;
    private float _remainingSeconds;
    private float _restRemainingSeconds;
    private float _restDurationSeconds;

    private bool _hasRule;
    private bool _isCompleted;
    private bool _isResting;
    private bool _stageTransitionPending;
    private bool _finalStageCompleted;
    private bool _currentStageTookPlayerHit;
    private bool _currentStageResultRecorded;
    private int _consecutiveNoHitStageCount;

    private bool UseTimedNormalBattleJudge =>
        _sectorRules != null && _sectorRules.UseTimedNormalBattleJudge;

    private bool CompleteNormalBattleOnEnemyClear =>
        _sectorRules == null || _sectorRules.CompleteNormalBattleOnEnemyClear;

    private bool ApplyNormalBattleResolveCoatingEffects =>
        _sectorRules != null && _sectorRules.ApplyNormalBattleResolveCoatingEffects;

    private float SuccessCountdownSeconds =>
        _sectorRules != null ? _sectorRules.SuccessCountdownSeconds : 5f;

    private float FailureCountdownSeconds =>
        _sectorRules != null ? _sectorRules.FailureCountdownSeconds : 5f;

    private SectorOwner NeutralJudgeResult =>
        _sectorRules != null ? _sectorRules.NeutralJudgeResult : SectorOwner.Neutral;

    private bool FillCompletedSectorWithVaccine =>
        _sectorRules != null &&
        _sectorRules.ApplyNormalBattleResolveCoatingEffects &&
        _sectorRules.FillCompletedSectorWithVaccine;

    private bool FillFailedSectorWithVirus =>
        _sectorRules != null &&
        _sectorRules.ApplyNormalBattleResolveCoatingEffects &&
        _sectorRules.FillFailedSectorWithVirus;

    private bool ClearPaintMasksOnBattleResolve =>
        _sectorRules == null || _sectorRules.ClearPaintMasksOnBattleResolve;

    private float CurrentNormalBattleTimerSeconds
    {
        get
        {
            return _stageBattleSettings != null &&
                   _stageBattleSettings.TryGetNormalBattleTimerSettings(
                       _currentStageIndex,
                       out float timerSeconds,
                       out _)
                ? timerSeconds
                : 30f;
        }
    }

    private bool CurrentResetTimerWhenRequirementLost
    {
        get
        {
            return _stageBattleSettings != null &&
                   _stageBattleSettings.TryGetNormalBattleTimerSettings(
                       _currentStageIndex,
                       out _,
                       out bool resetTimerWhenRequirementLost) &&
                   resetTimerWhenRequirementLost;
        }
    }

    private void Awake()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorCleanupApplier == null)
            _sectorCleanupApplier = FindAnyObjectByType<SectorCleanupApplier>();

        if (_stageSectorInstantiator == null)
            _stageSectorInstantiator = FindAnyObjectByType<StageSectorInstantiator>();

        if (_infectionControlManager == null)
            _infectionControlManager = FindAnyObjectByType<InfectionControlManager>();
    }

    private void OnEnable()
    {
        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

        if (_namedSectorControllerReadyChannel != null)
        {
            _namedSectorControllerReadyChannel.OnEventRaised += HandleNamedSectorControllerReady;

            if (_namedSectorControllerReadyChannel.HasCurrent)
                HandleNamedSectorControllerReady(_namedSectorControllerReadyChannel.Current);
        }
        else
        {
            BindNamedSectorController(_namedSectorController);
        }

        if (_infectionControlManagerReadyChannel != null)
        {
            _infectionControlManagerReadyChannel.OnEventRaised += HandleInfectionControlManagerReady;

            if (_infectionControlManagerReadyChannel.HasCurrent)
                HandleInfectionControlManagerReady(_infectionControlManagerReadyChannel.Current);
        }

        if (_stageAppliedChannel != null)
            _stageAppliedChannel.OnEventRaised += OnStageApplied;

        if (_playerHitReceivedChannel != null)
            _playerHitReceivedChannel.OnEventRaised += OnPlayerHitReceived;

        BeginStage(_startingStageIndex);
    }

    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        if (_namedSectorControllerReadyChannel != null)
            _namedSectorControllerReadyChannel.OnEventRaised -= HandleNamedSectorControllerReady;

        if (_infectionControlManagerReadyChannel != null)
            _infectionControlManagerReadyChannel.OnEventRaised -= HandleInfectionControlManagerReady;

        if (_stageAppliedChannel != null)
            _stageAppliedChannel.OnEventRaised -= OnStageApplied;

        if (_playerHitReceivedChannel != null)
            _playerHitReceivedChannel.OnEventRaised -= OnPlayerHitReceived;

        UnbindNamedSectorController();
    }

    private void Update()
    {
        SyncActiveSector();

        if (!_hasRule || _activeSector == null)
            return;

        if (_isResting)
        {
            TickRest();
            return;
        }

        if (TryCompleteNormalBattleByEnemyClear())
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

    private void CompleteCurrentSector()
    {
        if (_activeSector == null)
            return;

        _remainingSeconds = 0f;
        _isCompleted = true;
        _sectorJudgePhase = SectorJudgePhase.Resolved;

        bool completedStartSector =
            _sectorStateManager != null &&
            _sectorStateManager.IsStartSector(_activeSector);

        if (_sectorStateManager != null)
            _sectorStateManager.CompleteSector(_activeSector);
        else
            _activeSector.SetCleared(true);

        if (_sectorCleanupApplier != null)
        {
            if (ApplyNormalBattleResolveCoatingEffects && FillCompletedSectorWithVaccine)
            {
                _sectorCleanupApplier.CleanupThenApplyPlayerCompletedState(
                    _activeSector,
                    ClearPaintMasksOnBattleResolve);
            }
            else if (ApplyNormalBattleResolveCoatingEffects)
            {
                _sectorCleanupApplier.CleanupSector(
                    _activeSector,
                    ClearPaintMasksOnBattleResolve);
            }
            else
            {
                _sectorCleanupApplier.CleanupCombatObjects(_activeSector);
            }
        }

        StageRoomType completedRoomType = StageRoomType.Empty;
        bool hasCompletedRoomType =
            _sectorStateManager != null &&
            _sectorStateManager.TryGetStageRoomType(_activeSector, out completedRoomType);

        if (!completedStartSector &&
            hasCompletedRoomType &&
            IsTimedBattleRoom(completedRoomType))
        {
            RecoverInfectionControlOnBattleRoomClear();
        }

        if (completedStartSector &&
            _currentRule.advanceStageOnStartSectorComplete &&
            _rules != null &&
            _rules.HasNextRule(_currentStageIndex))
        {
            RaiseStageCompleteRewards();
            BeginStageTransition();
            return;
        }

        if (!completedStartSector &&
            hasCompletedRoomType &&
            ShouldAdvanceStageOnCompletedGoalRoom(completedRoomType))
        {
            CompleteStageAfterGoalRoomClear();
            return;
        }

        PublishSnapshot();
    }

    private void BeginStageTransition()
    {
        if (_stageTransitionPending || _finalStageCompleted)
            return;

        RecordCurrentStageResultForTreasureRoll();

        _stageTransitionPending = true;

        float restSeconds = Mathf.Max(0f, _currentRule.restSecondsBeforeNextStage);

        if (restSeconds <= 0f)
        {
            RequestNextStage();
            return;
        }

        _isResting = true;
        _restDurationSeconds = restSeconds;
        _restRemainingSeconds = restSeconds;
        PublishSnapshot();
    }

    private void RecoverInfectionControlOnBattleRoomClear()
    {
        if (_infectionControlManager == null)
            _infectionControlManager = FindAnyObjectByType<InfectionControlManager>();

        _infectionControlManager?.RecoverSectorExpanded();
    }

    private bool ShouldAdvanceStageOnCompletedGoalRoom(StageRoomType roomType)
    {
        if (!_hasRule ||
            !_currentRule.advanceStageOnBossBattleComplete ||
            roomType != StageRoomType.BigMonsterWave ||
            _stageTransitionPending ||
            _finalStageCompleted ||
            _sectorStateManager == null)
        {
            return false;
        }

        if (!_sectorStateManager.TryGetStageGoalSector(out SectorRuntime goalSector))
            return false;

        return goalSector == _activeSector;
    }

    private void CompleteStageAfterGoalRoomClear()
    {
        RaiseStageCompleteRewards();

        if (_currentRule.isFinalStage ||
            _rules == null ||
            !_rules.HasNextRule(_currentStageIndex))
        {
            CompleteFinalStage();
            return;
        }

        BeginStageTransition();
    }

    private void TickRest()
    {
        _restRemainingSeconds = Mathf.Max(0f, _restRemainingSeconds - Time.deltaTime);

        if (_restRemainingSeconds <= 0f)
        {
            _isResting = false;
            RequestNextStage();
            return;
        }

        PublishSnapshot();
    }

    private void RequestNextStage()
    {
        if (_cleanupBeforeNextStage)
            CleanupSectorsForStageTransition();

        if (_requestProgressNextStageChannel != null)
        {
            _requestProgressNextStageChannel.RaiseEvent();
            return;
        }

        if (_sectorStateManager != null)
            _sectorStateManager.ProgressNextStage();
    }

    private void RaiseStageCompleteRewards()
    {
        if (!_hasRule || _stageInfectionControlRecoverChannel == null)
            return;

        float recoverAmount = Mathf.Max(0f, _currentRule.infectionControlRecoverOnComplete);

        if (recoverAmount > 0f)
            _stageInfectionControlRecoverChannel.RaiseEvent(recoverAmount);
    }

    private void OnStageApplied(int stageIndex)
    {
        _stageTransitionPending = false;
        BeginStage(stageIndex);
    }

    private void OnNamedBattleCompleted(SectorRuntime _)
    {
        NotifyStageBossBattleCompleted();
    }

    public void NotifyStageBossBattleCompleted()
    {
        if (!_hasRule ||
            !_currentRule.advanceStageOnBossBattleComplete ||
            _stageTransitionPending ||
            _finalStageCompleted)
        {
            return;
        }

        RaiseStageCompleteRewards();

        if (_currentRule.isFinalStage ||
            _rules == null ||
            !_rules.HasNextRule(_currentStageIndex))
        {
            CompleteFinalStage();
            return;
        }

        BeginStageTransition();
    }

    private void CompleteFinalStage()
    {
        RecordCurrentStageResultForTreasureRoll();

        _finalStageCompleted = true;
        _isCompleted = true;
        _isResting = false;
        _remainingSeconds = 0f;
        _sectorJudgePhase = SectorJudgePhase.Resolved;

        FinalStageCompleted?.Invoke();
        _finalStageCompletedChannel?.RaiseEvent();

        Debug.Log(
            $"[StageProgressionManager] Final stage completed. stage={_currentStageIndex}",
            this);

        PublishSnapshot();
    }

    private void CleanupSectorsForStageTransition()
    {
        if (_sectorStateManager == null || _sectorCleanupApplier == null)
            return;

        IReadOnlyList<SectorRuntime> sectors = _sectorStateManager.Sectors;

        for (int i = 0; i < sectors.Count; i++)
        {
            SectorRuntime sector = sectors[i];

            if (sector == null || _sectorStateManager.IsStartSector(sector))
                continue;

            _sectorCleanupApplier.CleanupSector(
                sector,
                _clearPaintOnStageTransition);
        }
    }

    private void BeginStage(int stageIndex)
    {
        _currentStageIndex = stageIndex;
        _hasRule = _rules != null && _rules.TryGetRule(stageIndex, out _currentRule);
        _finalStageCompleted = false;
        _sectorJudgePhase = SectorJudgePhase.None;
        _currentStageTookPlayerHit = false;
        _currentStageResultRecorded = false;

        if (_hasRule && _sectorStateManager != null)
        {
            bool generatedStageBuilt =
                _stageSectorInstantiator != null &&
                _stageSectorInstantiator.BuildStage(
                    _currentRule,
                    _sectorStateManager,
                    _consecutiveNoHitStageCount);

            if (!generatedStageBuilt)
            {
                _sectorStateManager.ConfigureStageMap(
                    stageIndex,
                    _currentRule.roomGridSize,
                    _currentRule.useStartSectorOnly);
            }
        }

        int displayStage = _currentStageIndex + 1;
        string displayName = _hasRule ? _currentRule.displayName : $"{displayStage} Stage";

        RunResult.SetStage(displayStage, displayName);
        BeginSector(_sectorStateManager != null ? _sectorStateManager.CurrentSector : null);
    }

    private void SyncActiveSector()
    {
        SectorRuntime current =
            _sectorStateManager != null ? _sectorStateManager.CurrentSector : null;

        if (current != _activeSector)
            BeginSector(current);
    }

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

        _isResting = false;
        _restRemainingSeconds = 0f;
        _restDurationSeconds = 0f;
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

    private bool IsRequirementMet()
    {
        if (!_hasRule ||
            _activeSector == null ||
            _activeSector.IsCleared ||
            (_activeOccupancy == null && !ShouldCompleteNormalBattleOnEnemyClear(_activeSector)))
        {
            return false;
        }

        if (ShouldCompleteNormalBattleOnEnemyClear(_activeSector))
            return _activeEnemySpawner != null &&
                   _activeEnemySpawner.HasCompletedNormalBattleEncounter;

        if (_sectorJudgePhase == SectorJudgePhase.SuccessCountdown)
            return true;

        if (_sectorJudgePhase == SectorJudgePhase.FailureCountdown)
            return false;

        if (_sectorJudgePhase == SectorJudgePhase.Judging)
            return _activeOccupancy.CurrentSnapshot.dominantOwner == SectorOwner.Player;

        return _activeOccupancy.CurrentSnapshot.owner == SectorOwner.Player;
    }

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
        bool usesEnemyClearNormalBattle =
            _activeSector != null &&
            ShouldCompleteNormalBattleOnEnemyClear(_activeSector);
        bool showPlayerTimer =
            _isResting ||
            _sectorJudgePhase == SectorJudgePhase.SuccessCountdown ||
            _sectorJudgePhase == SectorJudgePhase.FailureCountdown ||
            ShouldShowPlayerTimerForSector(_activeSector);
        bool playerOwned =
            _activeSector != null &&
            (_activeSector.IsCleared ||
             _sectorJudgePhase == SectorJudgePhase.SuccessCountdown ||
             (!usesEnemyClearNormalBattle && dominantOwner == SectorOwner.Player));

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
            displayName = _hasRule ? _currentRule.displayName : string.Empty,

            remainingSeconds = _remainingSeconds,
            durationSeconds = durationSeconds,
            requiredPlayerOwnedCount = _activeSector != null && !isStartSector ? 1 : 0,
            currentPlayerOwnedCount = playerOwned ? 1 : 0,
            showPlayerTimer = showPlayerTimer,
            requirementMet = requirementMet,
            isCompleted = _isCompleted,
            hasNextStage =
                _hasRule &&
                !_currentRule.isFinalStage &&
                _rules != null &&
                _rules.HasNextRule(_currentStageIndex),

            isResting = _isResting,
            restRemainingSeconds = _restRemainingSeconds,
            restDurationSeconds = _restDurationSeconds,

            dominantOwner = dominantOwner,
        };

        snapshot.progress01 = snapshot.durationSeconds > 0f
            ? 1f - Mathf.Clamp01(snapshot.remainingSeconds / snapshot.durationSeconds)
            : 0f;

        snapshot.restProgress01 = snapshot.restDurationSeconds > 0f
            ? 1f - Mathf.Clamp01(snapshot.restRemainingSeconds / snapshot.restDurationSeconds)
            : 0f;

        _snapshotChangedChannel.RaiseEvent(snapshot);
    }

    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();
    }

    private void HandleNamedSectorControllerReady(NamedSectorController controller)
    {
        BindNamedSectorController(controller);
    }

    private void HandleInfectionControlManagerReady(InfectionControlManager manager)
    {
        _infectionControlManager = manager;
    }

    private void BindNamedSectorController(NamedSectorController controller)
    {
        if (_namedSectorController == controller)
            return;

        UnbindNamedSectorController();

        _namedSectorController = controller;

        if (_namedSectorController != null)
            _namedSectorController.NamedBattleCompleted += OnNamedBattleCompleted;
    }

    private void UnbindNamedSectorController()
    {
        if (_namedSectorController != null)
            _namedSectorController.NamedBattleCompleted -= OnNamedBattleCompleted;
    }

    private void OnPlayerHitReceived(GameObject _)
    {
        if (!_hasRule || _currentStageIndex <= 0 || _isResting || _finalStageCompleted)
            return;

        _currentStageTookPlayerHit = true;
    }

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

        return IsTimedBattleRoom(roomType);
    }

    private bool ShouldCompleteNormalBattleOnEnemyClear(SectorRuntime sector)
    {
        if (!CompleteNormalBattleOnEnemyClear ||
            sector == null ||
            _sectorStateManager == null ||
            _sectorStateManager.IsStartSector(sector) ||
            _sectorStateManager.IsSectorFailed(sector))
        {
            return false;
        }

        if (!_sectorStateManager.TryGetStageRoomType(sector, out StageRoomType roomType))
            return false;

        return IsTimedBattleRoom(roomType);
    }

    private bool TryCompleteNormalBattleByEnemyClear()
    {
        if (_activeSector == null ||
            _isCompleted ||
            !ShouldCompleteNormalBattleOnEnemyClear(_activeSector) ||
            _activeEnemySpawner == null ||
            !_activeEnemySpawner.HasCompletedNormalBattleEncounter)
        {
            return false;
        }

        CompleteCurrentSector();
        return true;
    }

    private void TickNormalBattleJudge()
    {
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

    private void ApplyNormalBattleTimerExpiredPenalty()
    {
        if (!_hasRule)
            return;

        if (_infectionControlManager == null)
            _infectionControlManager = FindAnyObjectByType<InfectionControlManager>();

        if (_infectionControlManager != null)
            _infectionControlManager.ApplyNormalBattleTimerExpiredPenalty();
    }

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
            if (_sectorCleanupApplier != null)
                _sectorCleanupApplier.CleanupCombatObjects(_activeSector);

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
            if (_sectorCleanupApplier != null)
                _sectorCleanupApplier.CleanupCombatObjects(_activeSector);

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

    private void TickSuccessCountdown()
    {
        _remainingSeconds = Mathf.Max(0f, _remainingSeconds - Time.deltaTime);

        if (_remainingSeconds <= 0f)
        {
            ApplyPlayerCoating();
            return;
        }

        PublishSnapshot();
    }

    private void BeginFailureCountdown()
    {
        _sectorJudgePhase = SectorJudgePhase.FailureCountdown;
        _remainingSeconds = Mathf.Max(0f, FailureCountdownSeconds);

        if (_remainingSeconds <= 0f)
        {
            ApplyVirusCoating();
            return;
        }

        PublishSnapshot();
    }

    private void TickFailureCountdown()
    {
        _remainingSeconds = Mathf.Max(0f, _remainingSeconds - Time.deltaTime);

        if (_remainingSeconds <= 0f)
        {
            ApplyVirusCoating();
            return;
        }

        PublishSnapshot();
    }

    private void ApplyPlayerCoating()
    {
        if (_activeSector == null)
            return;

        _remainingSeconds = 0f;
        _sectorJudgePhase = SectorJudgePhase.Resolved;

        if (_sectorCleanupApplier != null)
        {
            if (ApplyNormalBattleResolveCoatingEffects && FillCompletedSectorWithVaccine)
            {
                _sectorCleanupApplier.ApplyPlayerCompletedState(_activeSector);
            }
            else if (ApplyNormalBattleResolveCoatingEffects)
            {
                _sectorCleanupApplier.CleanupSector(
                    _activeSector,
                    ClearPaintMasksOnBattleResolve);
            }
            else
            {
                _sectorCleanupApplier.CleanupCombatObjects(_activeSector);
            }
        }

        PublishSnapshot();
    }

    private void ApplyVirusCoating()
    {
        if (_activeSector == null)
            return;

        _sectorJudgePhase = SectorJudgePhase.Resolved;
        _remainingSeconds = 0f;
        _isCompleted = false;

        if (_sectorCleanupApplier != null)
        {
            if (ApplyNormalBattleResolveCoatingEffects && FillFailedSectorWithVirus)
            {
                _sectorCleanupApplier.ApplyVirusFailedState(_activeSector);
            }
            else if (ApplyNormalBattleResolveCoatingEffects)
            {
                _sectorCleanupApplier.CleanupSector(
                    _activeSector,
                    ClearPaintMasksOnBattleResolve);
            }
            else
            {
                _sectorCleanupApplier.CleanupCombatObjects(_activeSector);
            }
        }

        PublishSnapshot();
    }

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

    private bool ShouldShowPlayerTimerForSector(SectorRuntime sector)
    {
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

        return IsTimedBattleRoom(roomType);
    }

    private static bool IsTimedBattleRoom(StageRoomType roomType)
    {
        return roomType == StageRoomType.NormalBattle ||
               roomType == StageRoomType.BigMonsterWave;
    }
}
