/*
 * Role hierarchy: Gameplay > World > Sectors
 * Primary responsibility: Manages SectorEnemySpawner stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class SectorEnemySpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorRuntime _sectorRuntime;

    [Tooltip("Enemies spawned by this spawner will put their projectiles under this root.")]
    [SerializeField] private Transform _projectileRoot;

    [Tooltip("Runtime parent for instantiated enemies. This is separate from SectorRuntime's authored Enemy Spawn Point Root.")]
    [SerializeField] private Transform _spawnedEnemiesRoot;

    [Header("Exclusion")]
    [SerializeField] private SectorExclusionRulesSO _sectorExclusionRules;

    [Header("Listening To")]
    [SerializeField] private SectorRuntimeEventChannelSO _sectorOpenedEvent;
    [SerializeField] private StageProgressSnapshotEventChannelSO _stageProgressChangedChannel;

    [Header("Manager Ready")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;

    [Header("Options")]
    [SerializeField] private bool _spawnOnlyWhenOpened = true;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private bool _isBattleResolveCountdownLocked;
    [ReadOnly] [SerializeField] private int _currentStageIndex = -1;
    [ReadOnly] [SerializeField] private int _resolvedMaxAlive;
    [ReadOnly] [SerializeField] private int _resolvedEncounterTotalSpawnCount;
    [ReadOnly] [SerializeField] private int _spawnedEncounterEnemyCount;
    [ReadOnly] [SerializeField] private int _pendingEnemySpawnCount;
    [ReadOnly] [SerializeField] private bool _encounterSpawnFinished;
    [ReadOnly] [SerializeField] private bool _encounterCleared;
    [ReadOnly] [SerializeField] private bool _isSectorActive;
    [ReadOnly] [SerializeField] private bool _hasEncounterConfiguration;
    [ReadOnly] [SerializeField] private bool _hasEnemyEncounterConfiguration;
    [ReadOnly] [SerializeField] private bool _encounterConfigurationFailed;
    [ReadOnly] [SerializeField] private string _selectedEncounterPresetName;
    [ReadOnly] [SerializeField] private int _aliveEncounterTargetCount;

    private SectorStateManager _sectorStateManager;
    private StageBattleEncounterPresetSO _selectedEncounterPreset;
    private StageBattleEncounterPresetSO _configuredEncounterPreset;

    private readonly List<Enemy> _aliveEnemies = new();
    private readonly Dictionary<Enemy, Damageable> _trackedDamageables = new();
    private readonly Dictionary<Enemy, UnityAction> _deathHandlers = new();
    private readonly List<Damageable> _aliveEncounterTargets = new();
    private readonly Dictionary<Damageable, UnityAction> _encounterTargetDeathHandlers = new();
    private readonly List<Transform> _spawnPointBuffer = new();
    private readonly List<StageEnemyWavePresetSO> _resolvedEnemyWavePresets = new();
    private readonly Queue<EnemyShooterConfigSO> _pendingEnemySpawnQueue = new();
    private float _encounterStartTime;
    private bool _encounterStarted;
    private bool _loggedMissingEnemySpawnPoints;
    private int _nextEnemyWaveIndex;
    private System.Random _spawnRng;
    private readonly HashSet<string> _loggedResolveSummaryKeys = new();
    private readonly HashSet<string> _loggedPendingResolveKeys = new();

    public bool HasCompletedEncounter =>
        _hasEnemyEncounterConfiguration && _encounterCleared;
    public bool HasStartedEncounter => _encounterStarted;
    public int AliveEnemyCount => _aliveEnemies.Count;
    public int AliveEncounterTargetCount => _aliveEncounterTargets.Count;
    public int SpawnedEncounterEnemyCount => _spawnedEncounterEnemyCount;
    public int EncounterTotalSpawnCount => _resolvedEncounterTotalSpawnCount;
    public int EncounterMaxAlive => _resolvedMaxAlive;
    public int PendingEncounterSpawnCount => _pendingEnemySpawnCount;
    public bool EncounterSpawnFinished => _encounterSpawnFinished;
    public bool EncounterConfigurationFailed => _encounterConfigurationFailed;
    public string ConfiguredEncounterPresetName =>
        _configuredEncounterPreset != null
            ? _configuredEncounterPreset.DisplayName
            : "<none>";
    public string SelectedEncounterPresetName =>
        string.IsNullOrWhiteSpace(_selectedEncounterPresetName)
            ? "<none>"
            : _selectedEncounterPresetName;

    /// <summary>
    /// Editor-facing summary of the deterministic wave plan. This exposes the
    /// actual runtime result, not merely the candidates authored in the asset.
    /// </summary>
    public string DebugResolvedWavePlan
    {
        get
        {
            if (_resolvedEnemyWavePresets.Count <= 0)
                return "<not resolved>";

            System.Text.StringBuilder builder = new System.Text.StringBuilder(96);

            for (int i = 0; i < _resolvedEnemyWavePresets.Count; i++)
            {
                if (i > 0)
                    builder.Append(" | ");

                StageEnemyWavePresetSO wave = _resolvedEnemyWavePresets[i];
                builder.Append($"W{i + 1}: ");
                builder.Append(wave != null ? wave.DisplayName : "<invalid>");
                builder.Append(wave != null ? $" ({wave.TotalSpawnCount})" : string.Empty);
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// Removes the live encounter without rewarding kills. Called only by an
    /// explicit sector-bypass station after the player chooses to leave combat.
    /// </summary>
    public void CancelEncounterForSectorSkip()
    {
        _pendingEnemySpawnQueue.Clear();
        _pendingEnemySpawnCount = 0;
        _encounterSpawnFinished = true;
        _encounterCleared = true;
        _isBattleResolveCountdownLocked = true;

        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _aliveEnemies[i];
            if (enemy == null)
                continue;

            if (_trackedDamageables.TryGetValue(enemy, out Damageable damageable) &&
                damageable != null &&
                _deathHandlers.TryGetValue(enemy, out UnityAction handler))
            {
                damageable.OnDie -= handler;
            }

            Destroy(enemy.gameObject);
        }

        UnbindAllTrackedEnemies();
    }

    /// <summary>
    /// Binds the exact encounter selected by the current StageDefinitionSO.
    /// The StageSectorInstantiator also supplies the stage index because spawned
    /// sector prefabs can enable before the generated map is registered.
    /// </summary>
    public bool ConfigureEncounter(
        StageBattleEncounterPresetSO encounterPreset,
        int stageIndex)
    {
        if (encounterPreset == null || !encounterPreset.IsValid)
        {
            Debug.LogError(
                "[SectorEnemySpawner] Cannot configure encounter because the preset is missing or invalid.",
                this);
            _configuredEncounterPreset = null;
            return false;
        }

        _configuredEncounterPreset = encounterPreset;
        _currentStageIndex = Mathf.Max(0, stageIndex);
        return true;
    }

    /// <summary>
    /// Resolves the authored encounter only after its generated sector has been
    /// registered in SectorStateManager. This prevents prefab OnEnable timing
    /// from leaving a room with an old or incomplete encounter plan.
    /// </summary>
    public void ResolveConfiguredEncounterAfterStageMapRegistered()
    {
        if (_configuredEncounterPreset == null)
            return;

        TryRefreshResolvedSettings("GeneratedStageMapRegistered");
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        if (_sectorRuntime == null)
            _sectorRuntime = GetComponent<SectorRuntime>();

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_sectorRuntime == null)
            _sectorRuntime = GetComponent<SectorRuntime>();

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorStateManager != null)
            _sectorStateManager.EnsureInitialized();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.OnEventRaised += OnSectorOpened;

        if (_stageProgressChangedChannel != null)
        {
            _stageProgressChangedChannel.OnEventRaised += OnStageProgressChanged;

            if (_stageProgressChangedChannel.HasCurrent)
                ApplyStageSnapshot(_stageProgressChangedChannel.Current);
        }

        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

        TryRefreshResolvedSettings("OnEnable");
        RefreshSectorActivity();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.OnEventRaised -= OnSectorOpened;

        if (_stageProgressChangedChannel != null)
            _stageProgressChangedChannel.OnEventRaised -= OnStageProgressChanged;

        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        UnbindAllTrackedEnemies();
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        CleanupTrackedEnemies();
        CleanupEncounterTargets();
        RefreshSectorActivity();

        if (!CanRunEncounter())
            return;

        EnsureEncounterStarted();
        TickEnemyEncounterWaves();
        TickPendingEnemySpawnQueue();
        RefreshEncounterSpawnFinished();
        RefreshEncounterCompletion();
    }

    // Purpose: Responds to the sector opened callback.
    private void OnSectorOpened(SectorRuntime sector)
    {
        if (_sectorRuntime == null || sector != _sectorRuntime)
            return;

        // This also covers play sessions that preserve event-channel state across reloads.
        if (!_hasEncounterConfiguration && !_encounterConfigurationFailed)
            TryRefreshResolvedSettings("SectorOpened");

        RefreshSectorActivity();
    }

    // Purpose: Responds to the stage progress changed callback.
    private void OnStageProgressChanged(StageProgressSnapshot snapshot)
    {
        int previousStageIndex = _currentStageIndex;

        ApplyStageSnapshot(snapshot);

        bool stageChanged = previousStageIndex != snapshot.stageIndex;

        if (stageChanged)
            TryRefreshResolvedSettings("StageProgressChanged");

        RefreshSectorActivity();
    }
    // Purpose: Applies the requested stage snapshot change.
    private void ApplyStageSnapshot(StageProgressSnapshot snapshot)
    {
        bool isThisCurrentSector =
            _sectorRuntime != null &&
            _sectorStateManager != null &&
            _sectorStateManager.CurrentSector == _sectorRuntime;

        _isBattleResolveCountdownLocked =
            snapshot.isResolveCountdown &&
            isThisCurrentSector;

        _currentStageIndex = snapshot.stageIndex;
    }
// Purpose: Attempts to refresh resolved settings and reports whether the request succeeded.
private void TryRefreshResolvedSettings(string reason)
    {
        if (_sectorRuntime == null)
            return;

        if (_currentStageIndex < 0)
        {
            //LogPendingResolveOnce(reason, "stage index is not ready");
            return;
        }

        if (_sectorStateManager == null || !_sectorStateManager.HasCurrentStageMap)
        {
            //LogPendingResolveOnce(reason, "sector state manager or stage map is not ready");
            return;
        }

        if (!_sectorStateManager.TryGetStageRoomType(_sectorRuntime, out StageRoomType roomType))
        {
            //LogPendingResolveOnce(reason, "sector is not registered in current stage map");
            return;
        }

        RefreshResolvedSettings(roomType, reason);
    }

    // Purpose: Refreshes the displayed or cached resolved settings.
    private void RefreshResolvedSettings(StageRoomType roomType, string reason)
    {
        _resolvedMaxAlive = 0;
        _resolvedEncounterTotalSpawnCount = 0;
        _spawnedEncounterEnemyCount = 0;
        _pendingEnemySpawnCount = 0;
        _encounterSpawnFinished = false;
        _encounterCleared = false;
        _selectedEncounterPreset = null;
        _selectedEncounterPresetName = string.Empty;
        _resolvedEnemyWavePresets.Clear();
        _pendingEnemySpawnQueue.Clear();
        _nextEnemyWaveIndex = 0;
        _encounterStarted = false;
        _encounterStartTime = 0f;
        _loggedMissingEnemySpawnPoints = false;
        _hasEncounterConfiguration = false;
        _hasEnemyEncounterConfiguration = false;
        _encounterConfigurationFailed = false;

        SelectDeterministicPresets(roomType, reason);
        ResetEncounterState();
    }
    // Purpose: Selects deterministic presets for the active workflow.
    private void SelectDeterministicPresets(
        StageRoomType roomType,
        string reason)
    {
        if (_sectorRuntime == null)
            return;

        int stageSeed = ResolveStageSeed();
        Vector2Int coord = _sectorRuntime.Coord;
        bool isBattle = UsesEnemyEncounter(roomType);

        bool pickedEncounterPreset = TryPickEncounterPreset(
            _currentStageIndex,
            stageSeed,
            coord,
            roomType,
            out StageBattleEncounterPresetSO encounterPreset);

        if (isBattle && pickedEncounterPreset)
        {
            _selectedEncounterPreset = encounterPreset;
            _selectedEncounterPresetName = encounterPreset.DisplayName;

            _resolvedMaxAlive = encounterPreset.SectorMaxAlive;
            ResolveEnemyWavePresetPlan(stageSeed, coord, roomType);
        }

        _hasEnemyEncounterConfiguration =
            _selectedEncounterPreset != null &&
            _resolvedEncounterTotalSpawnCount > 0;

        _hasEncounterConfiguration = _hasEnemyEncounterConfiguration;

        _spawnRng = new System.Random(StageDefinitionSO.BuildSectorSeed(
            stageSeed,
            coord,
            997));

        LogResolveSummaryOnce(
            reason,
            coord,
            roomType,
            isBattle,
            pickedEncounterPreset,
            encounterPreset);
    }

    // Purpose: Attempts to pick encounter preset and reports whether the request succeeded.
    private bool TryPickEncounterPreset(
        int stageIndex,
        int stageSeed,
        Vector2Int coord,
        StageRoomType roomType,
        out StageBattleEncounterPresetSO encounterPreset)
    {
        encounterPreset = null;

        if (UsesEnemyEncounter(roomType))
        {
            encounterPreset = _configuredEncounterPreset;

            if (encounterPreset != null && encounterPreset.IsValid)
                return true;

            Debug.LogError(
                $"[SectorEnemySpawner] Battle room has no configured encounter preset. " +
                $"stage={stageIndex}, roomType={roomType}, coord={coord}, sector={_sectorRuntime?.name}",
                this);
            return false;
        }

        return false;
    }

    // Purpose: Resolves the enemy wave preset plan needed by this component.
    private void ResolveEnemyWavePresetPlan(
        int stageSeed,
        Vector2Int coord,
        StageRoomType roomType)
    {
        _resolvedEnemyWavePresets.Clear();
        _resolvedEncounterTotalSpawnCount = 0;

        if (_selectedEncounterPreset == null ||
            _selectedEncounterPreset.Waves == null)
        {
            return;
        }

        for (int i = 0; i < _selectedEncounterPreset.WaveCount; i++)
        {
            int seed = StageDefinitionSO.BuildSectorSeed(
                stageSeed,
                coord,
                1100 + i);

            if (_selectedEncounterPreset.TryPickEnemyWavePreset(
                    i,
                    seed,
                    out StageEnemyWavePresetSO enemyWavePreset))
            {
                _resolvedEnemyWavePresets.Add(enemyWavePreset);
                _resolvedEncounterTotalSpawnCount += enemyWavePreset.TotalSpawnCount;
            }
            else
            {
                _resolvedEnemyWavePresets.Add(null);
                Debug.LogWarning(
                    $"[SectorEnemySpawner] Failed to resolve enemy wave preset. stage={_currentStageIndex}, sector={coord}, waveIndex={i}",
                    this);
            }
        }

        ValidateResolvedEnemyRoles(roomType, coord);
    }

    // Purpose: Validates resolved enemy roles and reports invalid authored data.
    private void ValidateResolvedEnemyRoles(StageRoomType roomType, Vector2Int coord)
    {
        int normalCount = 0;
        int namedCount = 0;
        int bossCount = 0;
        int bossGuardCount = 0;

        for (int i = 0; i < _resolvedEnemyWavePresets.Count; i++)
        {
            StageEnemyWavePresetSO preset = _resolvedEnemyWavePresets[i];
            if (preset == null || preset.Enemies == null)
                continue;

            for (int j = 0; j < preset.Enemies.Count; j++)
            {
                StageEnemyWavePresetSO.EnemySpawn spawn = preset.Enemies[j];
                if (spawn == null || spawn.archetype == null || spawn.count <= 0)
                    continue;

                int count = Mathf.Max(1, spawn.count);

                switch (spawn.archetype.EnemyRole)
                {
                    case EnemyShooterRole.StandardEnemy:
                        normalCount += count;
                        break;
                    case EnemyShooterRole.NamedEnemy:
                        namedCount += count;
                        break;
                    case EnemyShooterRole.Boss:
                        bossCount += count;
                        break;
                    case EnemyShooterRole.BossGuard:
                        bossGuardCount += count;
                        break;
                }
            }
        }

        string error = null;

        if (roomType == StageRoomType.Normal &&
            (bossCount > 0 || bossGuardCount > 0))
        {
            error =
                "Normal Sector may contain only NormalEnemy or NamedEnemy roles. " +
                $"Boss={bossCount}, BossGuard={bossGuardCount}.";
        }
        else if (roomType == StageRoomType.Boss)
        {
            if (bossCount != 1)
            {
                error = $"Boss encounter requires exactly one Boss role, but resolved {bossCount}.";
            }
            else if (normalCount > 0 || namedCount > 0)
            {
                error =
                    "Boss encounter may contain only one Boss and zero or more BossGuard roles. " +
                    $"NormalEnemy={normalCount}, NamedEnemy={namedCount}.";
            }
        }

        if (string.IsNullOrEmpty(error))
            return;

        _encounterConfigurationFailed = true;
        Debug.LogError(
            $"[SectorEnemySpawner] Invalid enemy role composition. {error} " +
            $"stage={_currentStageIndex}, coord={coord}, sector={_sectorRuntime?.name}, " +
            $"encounter={_selectedEncounterPresetName}",
            this);
    }

    // Purpose: Resolves the stage seed needed by this component.
    private int ResolveStageSeed()
    {
        if (_sectorStateManager != null &&
            _sectorStateManager.CurrentStageMapLayout != null)
        {
            return _sectorStateManager.CurrentStageMapLayout.stageSeed;
        }

        return StageMapGenerator.BuildStageSeed(0, _currentStageIndex);
    }

    // Purpose: Returns the current encounter wave count.
    private static int GetEncounterWaveCount(
        StageBattleEncounterPresetSO preset)
    {
        return preset != null
            ? preset.WaveCount
            : 0;
    }

    // Purpose: Performs the reset encounter state workflow for this component.
    private void ResetEncounterState()
    {
        _spawnedEncounterEnemyCount = 0;
        _pendingEnemySpawnCount = 0;
        _pendingEnemySpawnQueue.Clear();
        RefreshEncounterSpawnFinished();
        RefreshEncounterCompletion();
    }

    // Purpose: Ensures encounter started is ready before the workflow continues.
    private void EnsureEncounterStarted()
    {
        if (_encounterStarted)
            return;

        _encounterStarted = true;
        _encounterStartTime = Time.time;

        RefreshEncounterSpawnFinished();
        RefreshEncounterCompletion();
    }

    // Purpose: Processes one update step for enemy encounter waves.
    private void TickEnemyEncounterWaves()
    {
        if (_selectedEncounterPreset == null ||
            _selectedEncounterPreset.Waves == null ||
            _nextEnemyWaveIndex >= _selectedEncounterPreset.WaveCount)
        {
            return;
        }

        while (_nextEnemyWaveIndex < _selectedEncounterPreset.WaveCount)
        {
            StageBattleEncounterPresetSO.EncounterWave wave =
                _selectedEncounterPreset.Waves[_nextEnemyWaveIndex];

            float delay = wave != null ? Mathf.Max(0f, wave.delaySeconds) : 0f;

            if (Time.time < _encounterStartTime + delay)
                return;

            StageEnemyWavePresetSO enemyWavePreset =
                _nextEnemyWaveIndex < _resolvedEnemyWavePresets.Count
                    ? _resolvedEnemyWavePresets[_nextEnemyWaveIndex]
                    : null;

            EnqueueEnemyWavePreset(enemyWavePreset);
            _nextEnemyWaveIndex++;
        }
    }

    // Purpose: Performs the enqueue enemy wave preset workflow for this component.
    private void EnqueueEnemyWavePreset(StageEnemyWavePresetSO preset)
    {
        if (preset == null || preset.Enemies == null)
            return;

        for (int i = 0; i < preset.Enemies.Count; i++)
        {
            StageEnemyWavePresetSO.EnemySpawn spawn = preset.Enemies[i];

            if (spawn == null || spawn.archetype == null || !spawn.archetype.IsValid)
                continue;

            int count = Mathf.Max(1, spawn.count);

            for (int j = 0; j < count; j++)
                _pendingEnemySpawnQueue.Enqueue(spawn.archetype);
        }

        _pendingEnemySpawnCount = _pendingEnemySpawnQueue.Count;
    }

    // Purpose: Processes one update step for pending enemy spawn queue.
    private void TickPendingEnemySpawnQueue()
    {
        while (_pendingEnemySpawnQueue.Count > 0)
        {
            if (_resolvedMaxAlive > 0 && _aliveEnemies.Count >= _resolvedMaxAlive)
                break;

            EnemyShooterConfigSO enemyConfig = _pendingEnemySpawnQueue.Peek();

            if (!TrySpawnOne(enemyConfig))
                break;

            _pendingEnemySpawnQueue.Dequeue();
            _spawnedEncounterEnemyCount++;
            _pendingEnemySpawnCount = _pendingEnemySpawnQueue.Count;
        }
    }

    // Purpose: Refreshes the displayed or cached encounter spawn finished.
    private void RefreshEncounterSpawnFinished()
    {
        if (!_hasEncounterConfiguration)
        {
            _encounterSpawnFinished = false;
            return;
        }

        bool enemyWavesFinished =
            _selectedEncounterPreset == null ||
            _nextEnemyWaveIndex >= GetEncounterWaveCount(_selectedEncounterPreset);

        bool pendingEnemiesFinished = _pendingEnemySpawnQueue.Count <= 0;

        _encounterSpawnFinished =
            enemyWavesFinished &&
            pendingEnemiesFinished;

        _pendingEnemySpawnCount = _pendingEnemySpawnQueue.Count;
    }

    // Purpose: Refreshes the displayed or cached encounter completion.
    private void RefreshEncounterCompletion()
    {
        if (!_hasEnemyEncounterConfiguration)
        {
            _encounterCleared = false;
            return;
        }
        _encounterCleared =
            _encounterSpawnFinished &&
            _aliveEncounterTargets.Count == 0;
    }
    // Purpose: Checks whether run encounter is currently permitted.
    private bool CanRunEncounter()
    {

        if (_sectorRuntime == null)
            return false;

        if (!_hasEncounterConfiguration)
            return false;

        if (_encounterConfigurationFailed)
            return false;

        if (_sectorExclusionRules != null &&
            _sectorExclusionRules.ExcludeFromEnemySpawn(_sectorRuntime.Coord))
        {
            return false;
        }

        if (_isBattleResolveCountdownLocked)
            return false;

        if (_sectorStateManager != null &&
            _sectorStateManager.IsSectorFailed(_sectorRuntime))
        {
            return false;
        }

        if (!_isSectorActive)
            return false;

        if (_sectorRuntime.IsCleared)
            return false;

        if (_sectorRuntime.IsStartSector)
            return false;

        if (_sectorStateManager != null &&
            _sectorStateManager.HasCurrentStageMap)
        {
            if (!_sectorStateManager.TryGetStageRoomType(
                    _sectorRuntime,
                    out StageRoomType roomType) ||
                !UsesEnemyEncounter(roomType))
            {
                return false;
            }
        }

        return true;
    }

    // Purpose: Performs the uses enemy encounter workflow for this component.
    private bool UsesEnemyEncounter(StageRoomType roomType)
    {
        if (roomType == StageRoomType.Boss)
            return true;

        return roomType == StageRoomType.Normal &&
               (_sectorRuntime == null || _sectorRuntime.UsesNormalSectorEnemyEncounter);
    }

    // Purpose: Refreshes the displayed or cached sector activity.
    private void RefreshSectorActivity()
    {
        bool isFailed =
            _sectorRuntime != null &&
            _sectorStateManager != null &&
            _sectorStateManager.IsSectorFailed(_sectorRuntime);

        _isSectorActive =
            _sectorRuntime != null &&
            _sectorStateManager != null &&
            _sectorStateManager.CurrentSector == _sectorRuntime &&
            (!_spawnOnlyWhenOpened || _sectorRuntime.IsOpened) &&
            !_sectorRuntime.IsCleared &&
            !isFailed &&
            !_isBattleResolveCountdownLocked;
    }

    // Purpose: Attempts to spawn one and reports whether the request succeeded.
    private bool TrySpawnOne(EnemyShooterConfigSO enemyConfig)
    {
        if (enemyConfig == null || enemyConfig.EnemyPrefab == null)
            return false;

        if (!TryGetEnemySpawnPoint(out Transform spawnPoint))
        {
            return false;
        }

        Transform parent = _spawnedEnemiesRoot != null
            ? _spawnedEnemiesRoot
            : _sectorRuntime.transform;

        Enemy enemyInstance = Instantiate(
            enemyConfig.EnemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            parent);

        if (enemyInstance == null)
            return false;

        enemyInstance.SetSpawnReady(false);
        enemyInstance.SetCurrentSector(_sectorRuntime);
        BindShooterEnemyConfig(enemyInstance, enemyConfig);
        _sectorRuntime.GetComponent<SectorRoomTraitRuntime>()?.ApplyToSpawnedEnemy(enemyInstance);
        BindProjectileRoot(enemyInstance);
        if (!TrackEnemy(enemyInstance))
        {
            _encounterConfigurationFailed = true;
            _pendingEnemySpawnQueue.Clear();
            _pendingEnemySpawnCount = 0;
            Destroy(enemyInstance.gameObject);
            return false;
        }

        return true;
    }

    // Purpose: Binds shooter enemy config to the active runtime or UI target.
    private static void BindShooterEnemyConfig(
        Enemy enemy,
        EnemyShooterConfigSO enemyConfig)
    {
        if (enemy == null || enemyConfig == null)
            return;

        EnemyKillRewardSource[] killRewardSources =
            enemy.GetComponentsInChildren<EnemyKillRewardSource>(true);

        for (int i = 0; i < killRewardSources.Length; i++)
            killRewardSources[i].SetEnemyShooterConfig(enemyConfig);

        EnemyScreenSpaceHPUIAnchor[] uiAnchors =
            enemy.GetComponentsInChildren<EnemyScreenSpaceHPUIAnchor>(true);

        for (int i = 0; i < uiAnchors.Length; i++)
            uiAnchors[i].SetEnemyShooterConfig(enemyConfig);

        if (enemy is EnemyShooter shooter)
            shooter.SetEnemyShooterConfig(enemyConfig);
    }

    // Purpose: Binds projectile root to the active runtime or UI target.
    private void BindProjectileRoot(Enemy enemy)
    {
        if (enemy == null || _projectileRoot == null)
            return;

        EnemyAttackRig[] rigs = enemy.GetComponentsInChildren<EnemyAttackRig>(true);

        for (int i = 0; i < rigs.Length; i++)
        {
            if (rigs[i] != null)
                rigs[i].SetProjectileRoot(_projectileRoot);
        }
    }
    // Purpose: Attempts to get enemy spawn point and reports whether the request succeeded.
    private bool TryGetEnemySpawnPoint(out Transform spawnPoint)
    {
        spawnPoint = null;
        _spawnPointBuffer.Clear();

        if (_sectorRuntime == null)
            return false;

        _sectorRuntime.CollectEnemySpawnPoints(_spawnPointBuffer);

        if (_spawnPointBuffer.Count <= 0)
        {
            if (!_loggedMissingEnemySpawnPoints)
            {
                _loggedMissingEnemySpawnPoints = true;
                Debug.LogError(
                    $"[SectorEnemySpawner] No enabled EnemySpawnPoint exists below the SectorRuntime Enemy Spawn Point Root. sector={_sectorRuntime.name}",
                    _sectorRuntime);
            }

            return false;
        }

        int index = _spawnRng != null
            ? _spawnRng.Next(0, _spawnPointBuffer.Count)
            : Random.Range(0, _spawnPointBuffer.Count);

        spawnPoint = _spawnPointBuffer[index];
        return spawnPoint != null;
    }

    // Purpose: Performs the track enemy workflow for this component.
    private bool TrackEnemy(Enemy enemy)
    {
        if (enemy == null)
            return false;

        if (_aliveEnemies.Contains(enemy))
            return true;

        Damageable damageable = EntityComponentResolver.Resolve<Damageable>(enemy);

        if (damageable == null)
        {
            Debug.LogError(
                $"[SectorEnemySpawner] Spawned encounter enemy has no Damageable in its entity hierarchy. " +
                $"enemy={enemy.name}, sector={_sectorRuntime?.name}",
                enemy);
            return false;
        }

        _aliveEnemies.Add(enemy);

        UnityAction handler = () => OnTrackedEnemyDied(enemy);
        damageable.OnDie += handler;

        _trackedDamageables[enemy] = damageable;
        _deathHandlers[enemy] = handler;

        TrackEncounterTarget(damageable);
        return true;
    }

    // Purpose: Responds to the tracked enemy died callback.
    private void OnTrackedEnemyDied(Enemy enemy)
    {
        UntrackEnemy(enemy);
        RefreshEncounterCompletion();
    }

    // Purpose: Performs the cleanup tracked enemies workflow for this component.
    private void CleanupTrackedEnemies()
    {
        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _aliveEnemies[i];

            if (enemy == null)
            {
                RemoveDestroyedEnemyAt(i);
                continue;
            }

            if (_trackedDamageables.TryGetValue(
                    enemy,
                    out Damageable damageable) &&
                damageable != null &&
                damageable.IsDead)
            {
                UntrackEnemy(enemy);
                RefreshEncounterCompletion();
            }
        }
    }

    // Purpose: Performs the track encounter target workflow for this component.
    private void TrackEncounterTarget(Damageable damageable)
    {
        if (damageable == null ||
            _aliveEncounterTargets.Contains(damageable) ||
            damageable.IsDead)
        {
            RefreshAliveEncounterTargetDebugCount();
            return;
        }

        _aliveEncounterTargets.Add(damageable);

        UnityAction handler = () => OnTrackedEncounterTargetDied(damageable);
        damageable.OnDie += handler;
        _encounterTargetDeathHandlers[damageable] = handler;

        RefreshAliveEncounterTargetDebugCount();
    }

    // Purpose: Responds to the tracked encounter target died callback.
    private void OnTrackedEncounterTargetDied(Damageable damageable)
    {
        UntrackEncounterTarget(damageable);
        RefreshEncounterCompletion();
    }

    // Purpose: Performs the cleanup encounter targets workflow for this component.
    private void CleanupEncounterTargets()
    {
        for (int i = _aliveEncounterTargets.Count - 1; i >= 0; i--)
        {
            Damageable damageable = _aliveEncounterTargets[i];

            if (damageable == null || damageable.IsDead)
            {
                UntrackEncounterTarget(damageable);
                RefreshEncounterCompletion();
            }
        }
    }

    // Purpose: Performs the untrack encounter target workflow for this component.
    private void UntrackEncounterTarget(Damageable damageable)
    {
        if (damageable != null &&
            _encounterTargetDeathHandlers.TryGetValue(
                damageable,
                out UnityAction handler))
        {
            damageable.OnDie -= handler;
            _encounterTargetDeathHandlers.Remove(damageable);
        }

        _aliveEncounterTargets.Remove(damageable);
        RefreshAliveEncounterTargetDebugCount();
    }

    // Purpose: Refreshes the displayed or cached alive encounter target debug count.
    private void RefreshAliveEncounterTargetDebugCount()
    {
        _aliveEncounterTargetCount = _aliveEncounterTargets.Count;
    }

    // Purpose: Removes the requested destroyed enemy at from this component.
    private void RemoveDestroyedEnemyAt(int index)
    {
        if (index < 0 || index >= _aliveEnemies.Count)
            return;

        Enemy enemy = _aliveEnemies[index];

        if (enemy != null)
        {
            if (_trackedDamageables.TryGetValue(enemy, out Damageable damageable))
                UntrackEncounterTarget(damageable);

            _trackedDamageables.Remove(enemy);
            _deathHandlers.Remove(enemy);
        }

        _aliveEnemies.RemoveAt(index);
        RefreshEncounterCompletion();
    }

    // Purpose: Performs the untrack enemy workflow for this component.
    private void UntrackEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            _aliveEnemies.Remove(enemy);
            RefreshEncounterCompletion();
            return;
        }

        if (_trackedDamageables.TryGetValue(
                enemy,
                out Damageable damageable) &&
            damageable != null)
        {
            if (_deathHandlers.TryGetValue(enemy, out UnityAction handler))
                damageable.OnDie -= handler;

            _trackedDamageables.Remove(enemy);
            UntrackEncounterTarget(damageable);
        }

        _deathHandlers.Remove(enemy);
        _aliveEnemies.Remove(enemy);
        RefreshEncounterCompletion();
    }

    // Purpose: Removes the all tracked enemies binding.
    private void UnbindAllTrackedEnemies()
    {
        foreach (var pair in _trackedDamageables)
        {
            if (pair.Value != null &&
                _deathHandlers.TryGetValue(pair.Key, out UnityAction handler))
            {
                pair.Value.OnDie -= handler;
            }
        }

        _deathHandlers.Clear();
        _trackedDamageables.Clear();
        _aliveEnemies.Clear();

        foreach (var pair in _encounterTargetDeathHandlers)
        {
            if (pair.Key != null && pair.Value != null)
                pair.Key.OnDie -= pair.Value;
        }

        _encounterTargetDeathHandlers.Clear();
        _aliveEncounterTargets.Clear();
        RefreshAliveEncounterTargetDebugCount();
    }

    // Purpose: Handles the incoming sector state manager ready request or event.
    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();

        if (_stageProgressChangedChannel != null &&
            _stageProgressChangedChannel.HasCurrent)
        {
            ApplyStageSnapshot(_stageProgressChangedChannel.Current);
        }

        TryRefreshResolvedSettings("SectorStateManagerReady");
        RefreshSectorActivity();
    }

    // Purpose: Performs the log pending resolve once workflow for this component.
    private void LogPendingResolveOnce(string reason, string cause)
    {
        if (_sectorRuntime == null)
            return;

        string key = $"{_currentStageIndex}:{_sectorRuntime.GetInstanceID()}:{reason}:{cause}";

        if (!_loggedPendingResolveKeys.Add(key))
            return;
/*
        Debug.Log(
            $"[SectorEnemySpawner] Resolve pending. reason={reason}, cause={cause}, " +
            $"name={name}, coord={_sectorRuntime.Coord}, isStart={_sectorRuntime.IsStartSector}, " +
            $"stageIndex={_currentStageIndex}, managerNull={_sectorStateManager == null}, " +
            $"hasStageMap={(_sectorStateManager != null && _sectorStateManager.HasCurrentStageMap)}",
            this);
            */
    }

    // Purpose: Performs the log resolve summary once workflow for this component.
    private void LogResolveSummaryOnce(
        string reason,
        Vector2Int coord,
        StageRoomType roomType,
        bool isBattle,
        bool pickedEncounterPreset,
        StageBattleEncounterPresetSO encounterPreset)
    {
        string key = $"{_currentStageIndex}:{coord.x}:{coord.y}:{roomType}";

        if (!_loggedResolveSummaryKeys.Add(key))
            return;
        /*
        Debug.Log(
            $"[SectorEnemySpawner] Resolve summary. reason={reason}, name={name}, " +
            $"coord={coord}, roomType={roomType}, isStart={_sectorRuntime.IsStartSector}, " +
            $"isBattle={isBattle}, pickedEncounter={pickedEncounterPreset}, " +
            $"encounter={(encounterPreset != null ? encounterPreset.DisplayName : "null")}, " +
            $"totalEnemy={_resolvedEncounterTotalSpawnCount}, maxAlive={_resolvedMaxAlive}, " +
            $"hasEnemyConfig={_hasEnemyEncounterConfiguration}, " +
            $"hasEncounterConfig={_hasEncounterConfiguration}, opened={_sectorRuntime.IsOpened}, cleared={_sectorRuntime.IsCleared}",
            this);
            */
    }
}
