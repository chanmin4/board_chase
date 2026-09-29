/*
 * Role hierarchy: Gameplay > World > Sectors > Named_Boss
 * Primary responsibility: Manages NamedSectorRuntimeUnityEvent stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
    [Tooltip("WIP legacy choice-reward channel. Boss rewards now come only from the corpse loot and quest completion rewards, so this controller never raises it.")]
    [SerializeField, HideInInspector] private BossRewardRequestEventChannelSO _bossRewardRequestEvent;
    [SerializeField] private NamedSectorPhaseEventChannelSO _phaseChangedEvent;
    [SerializeField] private NamedSectorTimerSnapshotEventChannelSO _timerSnapshotEvent;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleStartedEvent;
    [SerializeField] private NamedBattleSignalEventChannelSO _battleEndedEvent;

    [Header("Named Spawn (Legacy)")]
    [SerializeField] private int _stageOverride = -1;
    [SerializeField] private Transform _namedSpawnPoint;
    [SerializeField] private Transform _namedRoot;

    [Header("Named Spawn Info Broadcasting")]
    [SerializeField] private NamedEnemySpawnInfoEventChannelSO _namedEnemySpawnInfoChannel;

    [Header("Sector Hooks")]
    [SerializeField] private NamedSectorRuntimeUnityEvent _onSectorReserved;
    [SerializeField] private NamedSectorRuntimeUnityEvent _onSectorPresented;
    [SerializeField] private NamedSectorRuntimeUnityEvent _onBeforeBattleSetup;
    [Tooltip("WIP legacy named-reward hook. It is retained for prefab compatibility and is intentionally not invoked by completed boss encounters.")]
    [SerializeField, HideInInspector] private NamedSectorRuntimeUnityEvent _onApplyNamedReward;

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

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        if (_namedStateApplier == null)
            _namedStateApplier = FindAnyObjectByType<SectorNamedStateApplier>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorStateManager != null)
            _sectorStateManager.EnsureInitialized();

        if (_namedStateApplier == null)
            _namedStateApplier = FindAnyObjectByType<SectorNamedStateApplier>();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
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

    // Purpose: Releases active subscriptions and transient state while disabled.
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

    // Purpose: Processes this component's per-frame runtime work.
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

    // Purpose: Begins the reservation timer workflow.
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

    // Purpose: Performs the force reserve now workflow for this component.
    public void ForceReserveNow()
    {
        ReserveRandomSector();
    }

    // Purpose: Performs the force present now workflow for this component.
    public void ForcePresentNow()
    {
        if (_selectedSector == null)
            ReserveRandomSector();

        PresentNamedSector();
    }

    // Purpose: Requests start battle from the owning service.
    public void RequestStartBattle()
    {
        if (_selectedSector != null)
            StartBattle(_selectedSector);
    }

    // Purpose: Attempts to start battle from portal and reports whether the request succeeded.
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

    // Purpose: Publishes the named killed notification.
    public void NotifyNamedKilled()
    {
        if (_phase != NamedSectorPhase.Battle || _battleRoutineRunning)
            return;

        NamedEnemy namedEnemy = _namedInstance != null
            ? _namedInstance.GetComponent<NamedEnemy>()
            : null;

        // Boss rewards are the spawned corpse loot plus normal quest rewards.
        // The retired choice-reward popup must never block the end-of-battle flow.
        SetPhase(NamedSectorPhase.RewardPending, _selectedSector);

        if (_battleSectorResetter != null)
            _battleSectorResetter.ResetBattleSector();

        Debug.Log(
            $"[NamedSectorController] Named killed. " +
            $"Ending battle directly; rewards come from corpse loot and quests. " +
            $"sector={_selectedSector?.name}, named={namedEnemy?.name}",
            this);

        ConfirmNamedRewardAndEndBattle();
    }

    // Purpose: Commits the current named reward and end battle selection or change.
    public void ConfirmNamedRewardAndEndBattle()
    {
        if (_phase != NamedSectorPhase.RewardPending || _battleRoutineRunning)
            return;

        StartCoroutine(FinishBattleRoutine());
    }

    // Purpose: Performs the reset for stage transition workflow for this component.
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

    // Purpose: Handles the incoming named enemy killed request or event.
    private void HandleNamedEnemyKilled(NamedEnemy namedEnemy)
    {
        if (namedEnemy == null)
            return;

        if (_namedInstance != null && namedEnemy.gameObject != _namedInstance)
            return;

        NotifyNamedKilled();
    }

    // Purpose: Handles the incoming sector state manager ready request or event.
    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();

        TryStartFirstCycle();
    }

    // Purpose: Handles the incoming stage applied request or event.
    private void HandleStageApplied(int stage)
    {
        ResetForStageTransition();
        TryStartFirstCycle();
    }

    // Purpose: Attempts to start first cycle and reports whether the request succeeded.
    private void TryStartFirstCycle()
    {
        // Named reservation cycles were removed from stage progression.
        _firstCycleStarted = false;
    }

    // Purpose: Handles the incoming player entered sector request or event.
    private void HandlePlayerEnteredSector(SectorRuntime sector)
    {
        _currentPlayerSector = sector;

        if (_phase == NamedSectorPhase.Present && sector == _selectedSector)
            StartBattle(_selectedSector);
    }

    // Purpose: Performs the reserve random sector workflow for this component.
    private void ReserveRandomSector()
    {
        SetPhase(NamedSectorPhase.None, null);
        PublishTimerSnapshot();
    }

    // Purpose: Performs the present named sector workflow for this component.
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

    // Purpose: Begins the battle workflow.
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

    // Purpose: Begins the battle routine workflow.
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

    // Purpose: Prepares battle while covered for the next workflow step.
    private void PrepareBattleWhileCovered(SectorRuntime sourceSector)
    {
        if (_battleSectorResetter != null)
            _battleSectorResetter.ResetBattleSector();

        _onBeforeBattleSetup?.Invoke(sourceSector);
        SpawnNamed();
    }

    // Purpose: Performs the complete battle start workflow for this component.
    private void CompleteBattleStart(SectorRuntime sourceSector)
    {
        SetPhase(NamedSectorPhase.Battle, sourceSector);
        _battleStartedEvent?.RaiseEvent(sourceSector);
    }

    // Purpose: Finishes the active battle routine workflow.
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

    // Purpose: Applies the requested battle end while covered change.
    private void ApplyBattleEndWhileCovered(SectorRuntime sourceSector)
    {
        if (_namedInstance != null)
        {
            Destroy(_namedInstance);
            _namedInstance = null;
        }

        if (_battleSectorResetter != null)
            _battleSectorResetter.ResetBattleSector();

        // WIP legacy choice-reward hooks are deliberately skipped. Do not
        // restore these effects unless boss rewards are redesigned explicitly.

        if (_namedStateApplier != null)
            _namedStateApplier.ClearNamedState(sourceSector);
        else if (sourceSector != null)
            sourceSector.GetComponent<SectorOccupancy>()?.RemoveSpecialState(SectorSpecialState.NamedActive);
    }

    // Purpose: Performs the complete battle end workflow for this component.
    private void CompleteBattleEnd(SectorRuntime sourceSector)
    {
        _battleEndedEvent?.RaiseEvent(sourceSector);

        _selectedSector = null;
        NamedBattleCompleted?.Invoke(sourceSector);
        SetPhase(NamedSectorPhase.None, null);
        PublishTimerSnapshot();
    }

    // Purpose: Spawns named for the current runtime context.
    private void SpawnNamed()
    {
        Debug.LogWarning(
            "[NamedSectorController] Named spawning is disabled. Boss encounters are configured by StageDefinitionSO.",
            this);
    }
    // Purpose: Binds enemy stat config to the active runtime or UI target.
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
        /*
        if (enemy is EnemyShooter shooter &&
            enemyConfig is EnemyShooterConfigSO shooterConfig)
        {
            shooter.SetEnemyShooterConfig(shooterConfig);
        }
        */
    }
    // Purpose: Resolves the current stage needed by this component.
    private int ResolveCurrentStage()
    {
        if (_stageOverride >= 0)
            return _stageOverride;

        return _sectorStateManager != null ? _sectorStateManager.CurrentStage : 0;
    }

    // Purpose: Attempts to pick random opened sector and reports whether the request succeeded.
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

    // Purpose: Attempts to pick stage goal sector and reports whether the request succeeded.
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

    // Purpose: Updates the phase state.
    private void SetPhase(NamedSectorPhase phase, SectorRuntime sector)
    {
        _phase = phase;
        _phaseChangedEvent?.RaiseEvent(phase, sector);
    }

    // Purpose: Publishes the timer snapshot notification.
    private void PublishTimerSnapshot()
    {
        _timerSnapshotEvent?.RaiseEvent(new NamedSectorTimerSnapshot(
            _phase,
            _selectedSector,
            _timer,
            _timerDuration));
    }

    // Purpose: Checks whether timer phase is currently true.
    private bool IsTimerPhase(NamedSectorPhase phase)
    {
        return phase == NamedSectorPhase.WaitingForReservation ||
               phase == NamedSectorPhase.Reserved ||
               phase == NamedSectorPhase.DefeatedCooldown;
    }

    // Purpose: Processes one update step for timer snapshot.
    private void TickTimerSnapshot()
    {
        _timerPublishCooldown -= Time.deltaTime;
        if (_timerPublishCooldown > 0f)
            return;

        _timerPublishCooldown = 0.1f;
        PublishTimerSnapshot();
    }

    // Purpose: Processes one update step for debug log.
    private void TickDebugLog()
    {
        if (!_logNamedTimer)
            return;

        _debugLogCooldown -= Time.deltaTime;
        if (_debugLogCooldown > 0f)
            return;

        _debugLogCooldown = 1f;

        Debug.Log(
            $"[NamedSectorController] phase={_phase}, timer={_timer:0.00}/{_timerDuration:0.00}, " +
            $"stage={(_sectorStateManager != null ? _sectorStateManager.CurrentStage : -1)}, " +
            $"selected={(_selectedSector != null ? _selectedSector.name : "null")}",
            this);
    }
}
