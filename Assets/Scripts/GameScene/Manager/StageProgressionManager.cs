using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class StageProgressionManager : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("stage별 방 크기, 목표 방 타입, PlayerTimer, stage 전환 조건을 담은 규칙 SO입니다.")]
    [SerializeField] private StageProgressionRulesSO _rules;

    [Tooltip("게임 시작 시 적용할 stage index입니다. 보통 0으로 두고 StartSector 전용 시작 stage를 먼저 실행합니다.")]
    [SerializeField] private int _startingStageIndex = 0;

    [Header("Room Completion")]
    [Tooltip("방 클리어 시 해당 sector를 백신 100% 상태로 덮을지 여부입니다.")]
    [SerializeField] private bool _fillCompletedSectorWithVaccine = true;

    [Header("Stage Transition")]
    [Tooltip("다음 stage로 넘어가기 전에 현재 stage sector들의 런타임 상태를 정리할지 여부입니다.")]
    [SerializeField] private bool _cleanupBeforeNextStage = true;

    [Tooltip("stage 전환 정리 시 바닥 페인트까지 지울지 여부입니다. 끄면 기존 페인트 시각/점유 상태가 남을 수 있습니다.")]
    [SerializeField] private bool _clearPaintOnStageTransition = true;

    [Header("Refs")]
    [Tooltip("현재 sector, sector opened/cleared 상태, generated stage map 등록을 관리하는 매니저입니다.")]
    [SerializeField] private SectorStateManager _sectorStateManager;

    [Tooltip("stage 전환 또는 방 완료 시 sector paint/runtime 상태를 정리하는 컴포넌트입니다.")]
    [SerializeField] private SectorCleanupApplier _sectorCleanupApplier;
    

    [Tooltip("stage 시작 시 StageProgressionRulesSO의 roomGridSize를 기준으로 sector prefab을 생성하는 컴포넌트입니다.")]
    [SerializeField] private StageSectorInstantiator _stageSectorInstantiator;

    [Header("Listening To")]
    [Tooltip("SectorStateManager가 stage index를 적용했을 때 받는 이벤트입니다. 수신하면 해당 stage 규칙으로 진행 상태를 갱신합니다.")]
    [SerializeField] private IntEventChannelSO _stageAppliedChannel;

    [Header("Broadcasting On")]
    [Tooltip("휴식 시간이 끝난 뒤 다음 stage 적용을 요청하는 이벤트입니다.")]
    [SerializeField] private VoidEventChannelSO _requestProgressNextStageChannel;
    [Header("Manager Ready")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [SerializeField] private NamedSectorControllerReadyEventChannelSO _namedSectorControllerReadyChannel;
    [Tooltip("final stage 완료 시 발생시키는 이벤트입니다. 승리 UI나 결과 처리에서 구독합니다.")]
    [SerializeField] private VoidEventChannelSO _finalStageCompletedChannel;

    [Tooltip("PlayerTimer HUD가 읽는 stage 진행 snapshot 이벤트입니다.")]
    [SerializeField] private StageProgressSnapshotEventChannelSO _snapshotChangedChannel;

    [Tooltip("stage 완료 보상으로 Infection Control을 회복시킬 때 발생시키는 이벤트입니다.")]
    [SerializeField] private FloatEventChannelSO _stageInfectionControlRecoverChannel;

    private NamedSectorController _namedSectorController;
    public event Action FinalStageCompleted;

    private StageProgressionRulesSO.StageProgressRule _currentRule;
    private SectorRuntime _activeSector;
    private SectorOccupancy _activeOccupancy;

    private int _currentStageIndex;
    private float _remainingSeconds;
    private float _restRemainingSeconds;
    private float _restDurationSeconds;

    private bool _hasRule;
    private bool _isCompleted;
    private bool _isResting;
    private bool _stageTransitionPending;
    private bool _finalStageCompleted;

    private void Awake()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();


        if (_stageSectorInstantiator == null)
            _stageSectorInstantiator = FindAnyObjectByType<StageSectorInstantiator>();
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

        if (_stageAppliedChannel != null)
            _stageAppliedChannel.OnEventRaised += OnStageApplied;

        BeginStage(_startingStageIndex);
    }

    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        if (_namedSectorControllerReadyChannel != null)
            _namedSectorControllerReadyChannel.OnEventRaised -= HandleNamedSectorControllerReady;

        if (_stageAppliedChannel != null)
            _stageAppliedChannel.OnEventRaised -= OnStageApplied;

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

        if (_isCompleted)
            return;

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
        else if (_currentRule.resetTimerWhenRequirementLost)
        {
            _remainingSeconds = _currentRule.timerSeconds;
        }

        PublishSnapshot();
    }

    private void CompleteCurrentSector()
    {
        if (_activeSector == null)
            return;

        _remainingSeconds = 0f;
        _isCompleted = true;

        bool completedStartSector =
            _sectorStateManager != null &&
            _sectorStateManager.IsStartSector(_activeSector);

        if (_sectorStateManager != null)
            _sectorStateManager.CompleteSector(_activeSector);
        else
            _activeSector.SetCleared(true);

        if (_sectorCleanupApplier != null)
        {
            if (_fillCompletedSectorWithVaccine)
                _sectorCleanupApplier.CleanupThenApplyPlayerCompletedState(_activeSector, clearPaintMasks: true);
            else
                _sectorCleanupApplier.CleanupSector(_activeSector, clearPaintMasks: true);
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

        PublishSnapshot();
    }

    private void BeginStageTransition()
    {
        if (_stageTransitionPending || _finalStageCompleted)
            return;

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
        _finalStageCompleted = true;
        _isCompleted = true;
        _isResting = false;
        _remainingSeconds = 0f;

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

        if (_hasRule && _sectorStateManager != null)
        {
            bool generatedStageBuilt =
                _stageSectorInstantiator != null &&
                _stageSectorInstantiator.BuildStage(_currentRule, _sectorStateManager);

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

        _isResting = false;
        _restRemainingSeconds = 0f;
        _restDurationSeconds = 0f;
        _isCompleted = sector != null && sector.IsCleared;
        _remainingSeconds = _isCompleted || !_hasRule
            ? 0f
            : _currentRule.timerSeconds;

        PublishSnapshot();
    }

    private bool IsRequirementMet()
    {
        if (!_hasRule ||
            _activeSector == null ||
            _activeSector.IsCleared ||
            _activeOccupancy == null)
        {
            return false;
        }

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
        bool requirementMet = IsRequirementMet();
        bool playerOwned =
            _activeSector != null &&
            (_activeSector.IsCleared ||
             (_activeOccupancy != null &&
              _activeOccupancy.CurrentSnapshot.owner == SectorOwner.Player));

        StageProgressSnapshot snapshot = new StageProgressSnapshot
        {
            isStartSector = isStartSector,
            stageIndex = _currentStageIndex,
            displayName = _hasRule ? _currentRule.displayName : string.Empty,

            remainingSeconds = _remainingSeconds,
            durationSeconds = _hasRule ? _currentRule.timerSeconds : 0f,
            requiredPlayerOwnedCount = _activeSector != null ? 1 : 0,
            currentPlayerOwnedCount = playerOwned ? 1 : 0,

            requirementMet = requirementMet,
            isCompleted = _isCompleted,
            hasNextStage =
                _hasRule &&
                !_currentRule.isFinalStage &&
                _rules != null &&
                _rules.HasNextRule(_currentStageIndex),

            isResting = _isResting,
            restRemainingSeconds = _restRemainingSeconds,
            restDurationSeconds = _restDurationSeconds
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
}
