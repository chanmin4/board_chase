using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class NamedSectorRuntimeUnityEvent : UnityEvent<SectorRuntime>
{
}

public class NamedSectorController : MonoBehaviour
{
    [Header("Auto Refs")]
    [SerializeField] private SectorStateManager _sectorStateManager;

    [Header("Refs")]
    [SerializeField] private NamedSectorTransitionController _transitionController;
    [SerializeField] private SectorNamedStateApplier _namedStateApplier;
    [Header("Named Runtime Roots")]
    [Tooltip("Projectiles spawned by the named enemy are parented here so battle reset can clear them before reward popup.")]
    [SerializeField] private Transform _namedProjectileRoot;
        
    [Header("Battle Sector Reset")]
    [SerializeField] private NamedBattleSectorResetter _battleSectorResetter;
    
    [Header("Rules")]
    [SerializeField] private SectorExclusionRulesSO _sectorExclusionRules;
    [SerializeField] private NamedSectorTimingSO _timing;

    [Header("Listening")]
    [SerializeField] private NamedEnemyKilledEventChannelSO _namedEnemyKilledEvent;
    [SerializeField] private SectorRuntimeEventChannelSO _currentsectorchangedEvent;
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [SerializeField] private IntEventChannelSO _stageAppliedEvent;

    [Header("Broadcasting")]
    [SerializeField] private BossRewardRequestEventChannelSO _bossRewardRequestEvent;
    [SerializeField] private NamedSectorPhaseEventChannelSO _phaseChangedEvent;
    [SerializeField] private NamedSectorTimerSnapshotEventChannelSO _timerSnapshotEvent;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleStartedEvent;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleEndedEvent;

    [Header("Named Spawn")]
    [SerializeField] private StageEnemySpawnTableSO _stageEnemySpawnTable;
    [SerializeField] private int _stageOverride = -1;
    [SerializeField] private Transform _namedSpawnPoint;
    [SerializeField] private Transform _namedRoot;
    [Header("Named Spawn InfoBroadcasting")]
    [SerializeField] private NamedEnemySpawnInfoEventChannelSO _namedEnemySpawnInfoChannel;

    [Header("Sector Hooks")]
    [SerializeField] private NamedSectorRuntimeUnityEvent _onSectorReserved;
    [SerializeField] private NamedSectorRuntimeUnityEvent _onSectorPresented;
    [SerializeField] private NamedSectorRuntimeUnityEvent _onBeforeBattleSetup;
    [SerializeField] private NamedSectorRuntimeUnityEvent _onApplyNamedReward;

    [Header("Debug")]
    [SerializeField] private bool _logNamedTimer;

    private NamedSectorPhase _phase = NamedSectorPhase.None;
    private SectorRuntime _selectedSector;
    private SectorRuntime _currentPlayerSector;
    private GameObject _namedInstance;

    private float _timer;
    private float _timerDuration;
    private float _timerPublishCooldown;
    private float _debugLogCooldown;

    private bool _battleRoutineRunning;
    private bool _firstCycleStarted;

    private void Reset()
    {
        if (_namedStateApplier == null)
            _namedStateApplier = FindAnyObjectByType<SectorNamedStateApplier>();
    }

    private void Awake()
    {
        if (_namedStateApplier == null)
            _namedStateApplier = FindAnyObjectByType<SectorNamedStateApplier>();
    }

    private void OnEnable()
    {
        if (_currentsectorchangedEvent != null)
            _currentsectorchangedEvent.OnEventRaised += HandlePlayerEnteredSector;
        if (_namedEnemyKilledEvent != null)
            _namedEnemyKilledEvent.OnEventRaised += HandleNamedEnemyKilled;
        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }
        else if (_sectorStateManager != null)
        {
            HandleSectorStateManagerReady(_sectorStateManager);
        }

        if (_stageAppliedEvent != null)
            _stageAppliedEvent.OnEventRaised += HandleStageApplied;
    }

    private void OnDisable()
    {
        if (_currentsectorchangedEvent != null)
            _currentsectorchangedEvent.OnEventRaised -= HandlePlayerEnteredSector;
        if (_namedEnemyKilledEvent != null)
            _namedEnemyKilledEvent.OnEventRaised -= HandleNamedEnemyKilled;
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        if (_stageAppliedEvent != null)
            _stageAppliedEvent.OnEventRaised -= HandleStageApplied;
    }

    private void Update()
    {
        if (!IsTimerPhase(_phase))
            return;

        _timer = Mathf.Max(0f, _timer - Time.deltaTime);

        TickTimerSnapshot();
        TickDebugLog();

        if (_timer > 0f)
            return;

        switch (_phase)
        {
            case NamedSectorPhase.WaitingForReservation:
            case NamedSectorPhase.DefeatedCooldown:
                ReserveRandomSector();
                break;

            case NamedSectorPhase.Reserved:
                PresentNamedSector();
                break;
        }
    }

    public void StartReservationTimer(float delaySeconds)
    {
        if (_sectorStateManager == null)
        {
            Debug.LogWarning("[NamedSectorController] Cannot start timer. SectorStateManager is missing.", this);
            return;
        }

        _selectedSector = null;
        _timer = Mathf.Max(0f, delaySeconds);
        _timerDuration = _timer;
        _timerPublishCooldown = 0f;

        SetPhase(NamedSectorPhase.WaitingForReservation, null);
        PublishTimerSnapshot();
    }

    public void ForceReserveNow()
    {
        ReserveRandomSector();
    }

    public void ForcePresentNow()
    {
        if (_selectedSector == null)
            ReserveRandomSector();

        PresentNamedSector();
    }

    public void RequestStartBattle()
    {
        if (_selectedSector != null)
            StartBattle(_selectedSector);
    }

    public bool TryStartBattleFromPortal(SectorRuntime targetSector)
    {
        Debug.Log(
        $"[NamedSectorController] TryStartBattleFromPortal. " +
        $"phase={_phase}, selected={_selectedSector?.name}, target={targetSector?.name}",
        this);
        if (_phase != NamedSectorPhase.Present)
            return false;

        if (_selectedSector == null || targetSector != _selectedSector)
            return false;

        StartBattle(_selectedSector);
        return true;
    }

    public void NotifyNamedKilled()
    {
        if (_phase != NamedSectorPhase.Battle || _battleRoutineRunning)
            return;

        NamedEnemy namedEnemy = _namedInstance != null
            ? _namedInstance.GetComponent<NamedEnemy>()
            : null;

        SetPhase(NamedSectorPhase.RewardPending, _selectedSector);
        if (_battleSectorResetter != null)
            _battleSectorResetter.ResetBattleSector();
        Debug.Log(
            $"[NamedSectorController] Named killed. Reward pending. " +
            $"sector={_selectedSector?.name}, named={namedEnemy?.name}",
            this);

        _bossRewardRequestEvent?.RaiseEvent(new BossRewardRequest(
            _selectedSector,
            namedEnemy
        ));
    }

    public void ConfirmNamedRewardAndEndBattle()
    {
        if (_phase != NamedSectorPhase.RewardPending || _battleRoutineRunning)
            return;

        StartCoroutine(FinishBattleRoutine());
    }
    private void HandleNamedEnemyKilled(NamedEnemy namedEnemy)
    {
        if (namedEnemy == null)
            return;

        if (_namedInstance != null && namedEnemy.gameObject != _namedInstance)
            return;

        NotifyNamedKilled();
    }
    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();

        TryStartFirstCycle();
    }

    private void HandleStageApplied(int stage)
    {
        TryStartFirstCycle();
    }

    private void TryStartFirstCycle()
    {
        if (_firstCycleStarted)
            return;

        if (_timing == null || !_timing.StartOnReady)
            return;

        if (_sectorStateManager == null)
            return;

        if (_stageEnemySpawnTable == null)
            return;

        int stage = ResolveCurrentStage();

        if (!_stageEnemySpawnTable.CanStartNamedCycle(stage))
            return;

        _firstCycleStarted = true;

        if (_timing.ReserveFirstSectorImmediately)
            ReserveRandomSector();
        else
            StartReservationTimer(DifficultyRuntime.ApplyNamedFirstReservationDelay(_timing.FirstReservationDelay));
    }

    private void HandlePlayerEnteredSector(SectorRuntime sector)
    {
        _currentPlayerSector = sector;

        if (_phase == NamedSectorPhase.Present && sector == _selectedSector)
            StartBattle(_selectedSector);
    }

    private void ReserveRandomSector()
    {
        if (!TryPickRandomOpenedSector(out SectorRuntime sector))
        {
            Debug.LogWarning("[NamedSectorController] No valid opened sector candidate.", this);
            float retryDelay = _timing != null ? _timing.RetryDelayWhenNoCandidate : 5f;
            StartReservationTimer(DifficultyRuntime.ApplyNamedRetryDelay(retryDelay));
            return;
        }

        _selectedSector = sector;
        float reservationDuration = _timing != null ? _timing.ReservationDuration : 30f;
        _timer = DifficultyRuntime.ApplyNamedReservationDuration(reservationDuration);
        _timerDuration = _timer;
        _timerPublishCooldown = 0f;

        if (_namedStateApplier != null)
            _namedStateApplier.SetReserved(_selectedSector);
        else
            Debug.LogWarning("[NamedSectorController] SectorNamedStateApplier is missing.", this);

        _onSectorReserved?.Invoke(_selectedSector);

        SetPhase(NamedSectorPhase.Reserved, _selectedSector);
        PublishTimerSnapshot();

        Debug.Log($"[NamedSectorController] Reserved sector={_selectedSector.name}, duration={_timer:0.00}", this);
    }

    private void PresentNamedSector()
    {
        if (_selectedSector == null)
            return;

        if (_namedStateApplier != null)
            _namedStateApplier.SetPresented(_selectedSector);
        else
            Debug.LogWarning("[NamedSectorController] SectorNamedStateApplier is missing.", this);

        _onSectorPresented?.Invoke(_selectedSector);

        SetPhase(NamedSectorPhase.Present, _selectedSector);
        PublishTimerSnapshot();

         Debug.Log(
        $"[NamedSectorController] Present check. " +
        $"selected={_selectedSector?.name}, " +
        $"current={_currentPlayerSector?.name}, " +
        $"same={_currentPlayerSector == _selectedSector}",
        this);
        Debug.Log(_currentPlayerSector);
        if (_currentPlayerSector == _selectedSector)
            StartBattle(_selectedSector);
    }

    private void StartBattle(SectorRuntime sourceSector)
    {
        Debug.Log(
        $"[NamedSectorController] StartBattle requested. " +
        $"phase={_phase}, source={sourceSector?.name}, routineRunning={_battleRoutineRunning}",
        this);
        if (_phase == NamedSectorPhase.Battle ||
            _phase == NamedSectorPhase.EnteringBattle ||
            _battleRoutineRunning)
            return;

        StartCoroutine(StartBattleRoutine(sourceSector));
    }

    private IEnumerator StartBattleRoutine(SectorRuntime sourceSector)
    {
         Debug.Log($"[NamedSectorController] StartBattleRoutine begin. source={sourceSector?.name}", this);
        if (_transitionController == null)
        {
            Debug.LogWarning("[NamedSectorController] TransitionController is missing.", this);
            yield break;
        }

        _battleRoutineRunning = true;
        SetPhase(NamedSectorPhase.EnteringBattle, sourceSector);

        yield return _transitionController.PlayEnterTransition(
            sourceSector,
            () => PrepareBattleWhileCovered(sourceSector),
            () => CompleteBattleStart(sourceSector)
        );

        _battleRoutineRunning = false;
    }

    private void PrepareBattleWhileCovered(SectorRuntime sourceSector)
    {
        if (_battleSectorResetter != null)
            _battleSectorResetter.ResetBattleSector();

        _onBeforeBattleSetup?.Invoke(sourceSector);
        SpawnNamed();
    }

    private void CompleteBattleStart(SectorRuntime sourceSector)
    {
        SetPhase(NamedSectorPhase.Battle, sourceSector);
        _battleStartedEvent?.RaiseEvent(sourceSector);
    }

    private IEnumerator FinishBattleRoutine()
    {
        if (_transitionController == null)
        {
            Debug.LogWarning("[NamedSectorController] TransitionController is missing.", this);
            yield break;
        }

        _battleRoutineRunning = true;

        SectorRuntime sourceSector = _selectedSector;
        SetPhase(NamedSectorPhase.EndingBattle, sourceSector);

        yield return _transitionController.PlayExitTransition(
            sourceSector,
            () => ApplyBattleEndWhileCovered(sourceSector),
            () => CompleteBattleEnd(sourceSector)
        );

        _battleRoutineRunning = false;
    }

    private void ApplyBattleEndWhileCovered(SectorRuntime sourceSector)
    {
        if (_namedInstance != null)
        {
            Destroy(_namedInstance);
            _namedInstance = null;
        }

        if (_battleSectorResetter != null)
            _battleSectorResetter.ResetBattleSector();

        _onApplyNamedReward?.Invoke(sourceSector);

        if (_namedStateApplier != null)
            _namedStateApplier.ClearNamedState(sourceSector);
        else if (sourceSector != null)
            sourceSector.GetComponent<SectorOccupancy>()?.RemoveSpecialState(SectorSpecialState.NamedActive);
    }

    private void CompleteBattleEnd(SectorRuntime sourceSector)
    {
        _battleEndedEvent?.RaiseEvent(sourceSector);

        _selectedSector = null;
        float respawnCooldown = _timing != null ? _timing.RespawnCooldownAfterKill : 120f;
        StartReservationTimer(DifficultyRuntime.ApplyNamedRespawnCooldown(respawnCooldown));
    }

    private void SpawnNamed()
    {
        if (_stageEnemySpawnTable == null || _namedSpawnPoint == null)
        {
            Debug.LogWarning("[NamedSectorController] Named spawn refs are missing.", this);
            return;
        }

        int stage = ResolveCurrentStage();

        if (!_stageEnemySpawnTable.TryPickNamedArchetype(stage, out EnemyArchetypeSO archetype))
        {
            Debug.LogWarning($"[NamedSectorController] No named enemy entry for stage {stage}.", this);
            return;
        }

        if (archetype == null || archetype.EnemyPrefab == null)
        {
            Debug.LogWarning($"[NamedSectorController] Named archetype has no EnemyPrefab. Stage={stage}.", this);
            return;
        }

        Enemy instance = Instantiate(
            archetype.EnemyPrefab,
            _namedSpawnPoint.position,
            _namedSpawnPoint.rotation,
            _namedRoot != null ? _namedRoot : null
        );

        _namedInstance = instance.gameObject;
        EnemyAttackRig attackRig = instance.GetComponentInChildren<EnemyAttackRig>(true);
        if (attackRig != null && _namedProjectileRoot != null)
            attackRig.SetProjectileRoot(_namedProjectileRoot);
        
        if (instance.TryGetComponent(out NamedEnemy namedEnemy))
        {
            _namedEnemySpawnInfoChannel?.RaiseEvent(new NamedEnemySpawnInfo(
                namedEnemy,
                _selectedSector,
                _namedSpawnPoint
            ));
        }
        else
        {
            Debug.LogWarning("[NamedSectorController] Spawned named prefab has no NamedEnemy component.", this);
        }
    }

    private int ResolveCurrentStage()
    {
        if (_stageOverride >= 0)
            return _stageOverride;

        return _sectorStateManager != null ? _sectorStateManager.CurrentStage : 0;
    }

    private bool TryPickRandomOpenedSector(out SectorRuntime result)
    {
        result = null;

        if (_sectorStateManager == null)
            return false;

        IReadOnlyList<SectorRuntime> sectors = _sectorStateManager.Sectors;
        if (sectors == null || sectors.Count == 0)
            return false;

        List<SectorRuntime> candidates = new();

        for (int i = 0; i < sectors.Count; i++)
        {
            SectorRuntime sector = sectors[i];

            if (sector == null || !sector.IsOpened)
                continue;

            if (_sectorStateManager.IsStartSector(sector))
                continue;

            Vector2Int coord = _sectorStateManager.GetSectorCoord(sector);
            if (_sectorExclusionRules != null &&
                _sectorExclusionRules.ExcludeFromNamedReservation(coord))
                continue;

            candidates.Add(sector);
        }

        if (candidates.Count == 0)
            return false;

        result = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
    }

    private void SetPhase(NamedSectorPhase phase, SectorRuntime sector)
    {
        _phase = phase;
        _phaseChangedEvent?.RaiseEvent(phase, sector);
    }

    private void PublishTimerSnapshot()
    {
        _timerSnapshotEvent?.RaiseEvent(new NamedSectorTimerSnapshot(
            _phase,
            _selectedSector,
            _timer,
            _timerDuration
        ));
    }

    private bool IsTimerPhase(NamedSectorPhase phase)
    {
        return phase == NamedSectorPhase.WaitingForReservation ||
            phase == NamedSectorPhase.Reserved ||
            phase == NamedSectorPhase.DefeatedCooldown;
    }

    private void TickTimerSnapshot()
    {
        _timerPublishCooldown -= Time.deltaTime;
        if (_timerPublishCooldown > 0f)
            return;

        _timerPublishCooldown = Mathf.Max(0.01f, _timing != null ? _timing.TimerPublishInterval : 0.1f);
        PublishTimerSnapshot();
    }

    private void TickDebugLog()
    {
        if (!_logNamedTimer)
            return;

        _debugLogCooldown -= Time.deltaTime;
        if (_debugLogCooldown > 0f)
            return;

        _debugLogCooldown = Mathf.Max(0.1f, _timing != null ? _timing.DebugLogInterval : 1f);

        Debug.Log(
            $"[NamedSectorController] phase={_phase}, timer={_timer:0.00}/{_timerDuration:0.00}, " +
            $"stage={(_sectorStateManager != null ? _sectorStateManager.CurrentStage : -1)}, " +
            $"selected={(_selectedSector != null ? _selectedSector.name : "null")}",
            this);
    }
    
}
