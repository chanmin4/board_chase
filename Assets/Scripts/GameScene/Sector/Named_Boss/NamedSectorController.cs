using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[Serializable]
public class NamedSectorRuntimeUnityEvent : UnityEvent<SectorRuntime>
{
}

public class NamedSectorController : MonoBehaviour
{
    public event Action<SectorRuntime> NamedBattleCompleted;
    [Header("Manager Ready")]
    [SerializeField] private NamedSectorControllerReadyEventChannelSO _namedSectorControllerReadyChannel;
    

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
    [FormerlySerializedAs("_stageEnemySetting")]
    [FormerlySerializedAs("_stageBattleSettings")]
    [FormerlySerializedAs("_stageGoalSettings")]
    [SerializeField] private StageProgressionRulesSO _stageProgressionRules;
    [SerializeField] private int _stageOverride = -1;
    [SerializeField] private Transform _namedSpawnPoint;
    [SerializeField] private Transform _namedRoot;

    [Header("Named Spawn Info Broadcasting")]
    [SerializeField] private NamedEnemySpawnInfoEventChannelSO _namedEnemySpawnInfoChannel;

    [Header("Sector Hooks")]
    [SerializeField] private NamedSectorRuntimeUnityEvent _onSectorReserved;
    [SerializeField] private NamedSectorRuntimeUnityEvent _onSectorPresented;
    [SerializeField] private NamedSectorRuntimeUnityEvent _onBeforeBattleSetup;
    [SerializeField] private NamedSectorRuntimeUnityEvent _onApplyNamedReward;

    [Header("Debug")]
    [SerializeField] private bool _logNamedTimer;
    private SectorStateManager _sectorStateManager;
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
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorStateManager != null)
            _sectorStateManager.EnsureInitialized();

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
        _namedSectorControllerReadyChannel?.RaiseEvent(this);
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
        if (_namedSectorControllerReadyChannel != null)
            _namedSectorControllerReadyChannel.Clear(this);
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
            namedEnemy));
    }

    public void ConfirmNamedRewardAndEndBattle()
    {
        if (_phase != NamedSectorPhase.RewardPending || _battleRoutineRunning)
            return;

        StartCoroutine(FinishBattleRoutine());
    }

    public void ResetForStageTransition()
    {
        StopAllCoroutines();

        SectorRuntime previousSector = _selectedSector;
        bool wasBattlePhase =
            _phase == NamedSectorPhase.EnteringBattle ||
            _phase == NamedSectorPhase.Battle ||
            _phase == NamedSectorPhase.RewardPending ||
            _phase == NamedSectorPhase.EndingBattle;

        _battleRoutineRunning = false;
        _firstCycleStarted = false;
        _timer = 0f;
        _timerDuration = 0f;
        _timerPublishCooldown = 0f;
        _debugLogCooldown = 0f;

        if (_namedInstance != null)
        {
            Destroy(_namedInstance);
            _namedInstance = null;
        }

        if (_battleSectorResetter != null)
            _battleSectorResetter.ResetBattleSector();

        if (_namedStateApplier != null && previousSector != null)
            _namedStateApplier.ClearNamedState(previousSector);

        if (wasBattlePhase)
            _battleEndedEvent?.RaiseEvent(previousSector);

        _selectedSector = null;

        SetPhase(NamedSectorPhase.None, null);
        PublishTimerSnapshot();
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
        ResetForStageTransition();
        TryStartFirstCycle();
    }

    private void TryStartFirstCycle()
    {
        if (_firstCycleStarted)
            return;

        if (_sectorStateManager == null || _stageProgressionRules == null)
            return;

        if (!TryGetCurrentNamedOptions(out StageProgressionRulesSO.NamedOrBossGoalOptions options))
            return;

        if (!options.startCycleOnReady)
            return;

        _firstCycleStarted = true;

        if (options.reserveFirstSectorImmediately)
        {
            ReserveRandomSector();
            return;
        }

        StartReservationTimer(
            DifficultyRuntime.ApplyNamedFirstReservationDelay(options.firstReservationDelay));
    }

    private void HandlePlayerEnteredSector(SectorRuntime sector)
    {
        _currentPlayerSector = sector;

        if (_phase == NamedSectorPhase.Present && sector == _selectedSector)
            StartBattle(_selectedSector);
    }

    private void ReserveRandomSector()
    {
        if (!TryGetCurrentNamedOptions(out StageProgressionRulesSO.NamedOrBossGoalOptions options))
        {
            SetPhase(NamedSectorPhase.None, null);
            PublishTimerSnapshot();
            return;
        }

        if (!TryPickRandomOpenedSector(out SectorRuntime sector))
        {
            Debug.LogWarning("[NamedSectorController] No valid opened sector candidate.", this);

            StartReservationTimer(
                DifficultyRuntime.ApplyNamedRetryDelay(options.retryDelayWhenNoCandidate));

            return;
        }

        _selectedSector = sector;
        _timer = DifficultyRuntime.ApplyNamedReservationDuration(options.reservationDuration);
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
        {
            return;
        }

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
            () => CompleteBattleStart(sourceSector));

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
            () => CompleteBattleEnd(sourceSector));

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
        NamedBattleCompleted?.Invoke(sourceSector);

        if (!TryGetCurrentNamedOptions(out StageProgressionRulesSO.NamedOrBossGoalOptions options))
        {
            SetPhase(NamedSectorPhase.None, null);
            PublishTimerSnapshot();
            return;
        }

        StartReservationTimer(
            DifficultyRuntime.ApplyNamedRespawnCooldown(options.respawnCooldownAfterKill));
    }

    private void SpawnNamed()
    {
        if (_stageProgressionRules == null || _namedSpawnPoint == null)
        {
            Debug.LogWarning("[NamedSectorController] Named spawn refs are missing.", this);
            return;
        }

        int stage = ResolveCurrentStage();

        if (!_stageProgressionRules.TryPickNamedOrBossArchetype(stage, out EnemyStatConfigSO enemyConfig))
        {
            Debug.LogWarning($"[NamedSectorController] No named enemy entry for stage {stage}.", this);
            return;
        }

        if (enemyConfig == null || enemyConfig.EnemyPrefab == null)
        {
            Debug.LogWarning($"[NamedSectorController] Named enemy config has no EnemyPrefab. Stage={stage}.", this);
            return;
        }

        Enemy instance = Instantiate(
            enemyConfig.EnemyPrefab,
            _namedSpawnPoint.position,
            _namedSpawnPoint.rotation,
            _namedRoot != null ? _namedRoot : null);

        BindEnemyStatConfig(instance, enemyConfig);
        _namedInstance = instance.gameObject;

        EnemyAttackRig attackRig = instance.GetComponentInChildren<EnemyAttackRig>(true);
        if (attackRig != null && _namedProjectileRoot != null)
            attackRig.SetProjectileRoot(_namedProjectileRoot);

        if (instance.TryGetComponent(out NamedEnemy namedEnemy))
        {
            _namedEnemySpawnInfoChannel?.RaiseEvent(new NamedEnemySpawnInfo(
                namedEnemy,
                _selectedSector,
                _namedSpawnPoint));
        }
        else
        {
            Debug.LogWarning("[NamedSectorController] Spawned named prefab has no NamedEnemy component.", this);
        }
    }
    private static void BindEnemyStatConfig(Enemy enemy, EnemyStatConfigSO enemyConfig)
    {
        if (enemy == null || enemyConfig == null)
            return;

        if (enemyConfig is CreatureEnemyStatConfigSO creatureConfig)
        {
            EnemyMovementStatsProvider[] movementProviders =
                enemy.GetComponentsInChildren<EnemyMovementStatsProvider>(true);

            for (int i = 0; i < movementProviders.Length; i++)
                movementProviders[i].SetEnemyStatConfig(creatureConfig);

            EnemyContactDamage[] contactDamages =
                enemy.GetComponentsInChildren<EnemyContactDamage>(true);

            for (int i = 0; i < contactDamages.Length; i++)
                contactDamages[i].SetEnemyStatConfig(creatureConfig);

            EnemyVirusTrail[] virusTrails =
                enemy.GetComponentsInChildren<EnemyVirusTrail>(true);

            for (int i = 0; i < virusTrails.Length; i++)
                virusTrails[i].SetEnemyStatConfig(creatureConfig);
        }

        EnemyKillRewardSource[] killRewardSources =
            enemy.GetComponentsInChildren<EnemyKillRewardSource>(true);

        for (int i = 0; i < killRewardSources.Length; i++)
            killRewardSources[i].SetEnemyStatConfig(enemyConfig);

        EnemyScreenSpaceHPUIAnchor[] uiAnchors =
            enemy.GetComponentsInChildren<EnemyScreenSpaceHPUIAnchor>(true);

        for (int i = 0; i < uiAnchors.Length; i++)
            uiAnchors[i].SetEnemyStatConfig(enemyConfig);

        if (enemy is EnemyShooter shooter &&
            enemyConfig is EnemyShooterConfigSO shooterConfig)
        {
            shooter.SetEnemyShooterConfig(shooterConfig);
        }
    }
    private bool TryGetCurrentNamedRule(out StageProgressionRulesSO.StageProgressRule rule)
    {
        rule = null;

        if (_stageProgressionRules == null)
            return false;

        return _stageProgressionRules.TryGetNamedOrBossCycleRule(ResolveCurrentStage(), out rule);
    }

    private bool TryGetCurrentNamedOptions(out StageProgressionRulesSO.NamedOrBossGoalOptions options)
    {
        options = null;

        if (_stageProgressionRules == null)
            return false;

        if (!_stageProgressionRules.TryGetNamedOrBossOptions(ResolveCurrentStage(), out options))
            return false;

        return options.cycleEnabled &&
               _stageProgressionRules.HasValidNamedOrBossEntry(ResolveCurrentStage());
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

        if (TryPickStageGoalSector(out result))
            return true;

        if (_sectorStateManager.HasCurrentStageMap)
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
            {
                continue;
            }

            candidates.Add(sector);
        }

        if (candidates.Count == 0)
            return false;

        result = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
    }

    private bool TryPickStageGoalSector(out SectorRuntime result)
    {
        result = null;

        if (_sectorStateManager == null ||
            !_sectorStateManager.TryGetStageGoalSector(out SectorRuntime goalSector) ||
            goalSector == null ||
            !goalSector.IsOpened ||
            goalSector.IsCleared)
        {
            return false;
        }

        if (_sectorStateManager.TryGetStageRoomType(
                goalSector,
                out StageRoomType roomType) &&
            roomType != StageRoomType.Named &&
            roomType != StageRoomType.Boss)
        {
            return false;
        }

        Vector2Int coord = _sectorStateManager.GetSectorCoord(goalSector);

        if (_sectorExclusionRules != null &&
            _sectorExclusionRules.ExcludeFromNamedReservation(coord))
        {
            return false;
        }

        result = goalSector;
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
            _timerDuration));
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

        float interval = TryGetCurrentNamedOptions(out StageProgressionRulesSO.NamedOrBossGoalOptions options)
            ? options.timerPublishInterval
            : 0.1f;

        _timerPublishCooldown = Mathf.Max(0.01f, interval);
        PublishTimerSnapshot();
    }

    private void TickDebugLog()
    {
        if (!_logNamedTimer)
            return;

        _debugLogCooldown -= Time.deltaTime;
        if (_debugLogCooldown > 0f)
            return;

        float interval = TryGetCurrentNamedRule(out StageProgressionRulesSO.StageProgressRule rule)
            ? rule.goalDebugLogInterval
            : 1f;

        _debugLogCooldown = Mathf.Max(0.1f, interval);

        Debug.Log(
            $"[NamedSectorController] phase={_phase}, timer={_timer:0.00}/{_timerDuration:0.00}, " +
            $"stage={(_sectorStateManager != null ? _sectorStateManager.CurrentStage : -1)}, " +
            $"selected={(_selectedSector != null ? _selectedSector.name : "null")}",
            this);
    }
}
