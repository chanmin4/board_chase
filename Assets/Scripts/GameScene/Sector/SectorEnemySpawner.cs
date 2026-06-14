using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SectorEnemySpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorRuntime _sectorRuntime;

    [Tooltip("Enemies spawned by this spawner will put their projectiles under this root.")]
    [SerializeField] private Transform _projectileRoot;

    [FormerlySerializedAs("_spawnTable")]
    [FormerlySerializedAs("_stageenemysetting")]
    [FormerlySerializedAs("_stageEnemySetting")]
    [SerializeField] private StageBattleSettingsSO _stageBattleSettings;

    [SerializeField] private Transform _spawnedEnemiesRoot;

    [Tooltip("Sector objects spawned by battle presets are parented here. Uses Spawned Enemies Root, then the sector transform when empty.")]
    [SerializeField] private Transform _spawnedSectorObjectsRoot;

    [Header("Exclusion")]
    [SerializeField] private SectorExclusionRulesSO _sectorExclusionRules;

    [Header("Listening To")]
    [SerializeField] private SectorRuntimeEventChannelSO _sectorOpenedEvent;
    [SerializeField] private StageProgressSnapshotEventChannelSO _stageProgressChangedChannel;

    [Header("Named Battle Lock")]
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
    [ReadOnly] [SerializeField] private int _spawnedEncounterObjectCount;
    [ReadOnly] [SerializeField] private bool _encounterSpawnFinished;
    [ReadOnly] [SerializeField] private bool _encounterCleared;
    [ReadOnly] [SerializeField] private bool _isSectorActive;
    [ReadOnly] [SerializeField] private bool _hasEncounterConfiguration;
    [ReadOnly] [SerializeField] private string _selectedEncounterPresetName;
    [ReadOnly] [SerializeField] private string _selectedObjectRoomPresetName;
    [ReadOnly] [SerializeField] private int _aliveEncounterTargetCount;

    private SectorStateManager _sectorStateManager;
    private StageBattleSettingsSO.StageSpawnRule _currentRule;
    private StageBattleSettingsSO.NormalBattleEncounterPreset _selectedEncounterPreset;
    private StageBattleSettingsSO.SectorObjectRoomPreset _selectedObjectRoomPreset;

    private readonly List<Enemy> _aliveEnemies = new();
    private readonly Dictionary<Enemy, Damageable> _trackedDamageables = new();
    private readonly Dictionary<Enemy, UnityAction> _deathHandlers = new();
    private readonly List<Damageable> _aliveEncounterTargets = new();
    private readonly Dictionary<Damageable, UnityAction> _encounterTargetDeathHandlers = new();
    private readonly List<Transform> _spawnPointBuffer = new();
    private readonly List<StageBattleSettingsSO.EnemyWavePreset> _resolvedEnemyWavePresets = new();
    private readonly Queue<EnemyStatConfigSO> _pendingEnemySpawnQueue = new();
    private readonly HashSet<Transform> _objectOccupiedSpawnPoints = new();
    private readonly List<GameObject> _clearWithRoomObjects = new();
    private float _encounterStartTime;
    private bool _encounterStarted;
    private bool _sectorObjectsSpawned;
    private int _nextEnemyWaveIndex;
    private System.Random _spawnRng;

    public bool HasCompletedNormalBattleEncounter => _hasEncounterConfiguration && _encounterCleared;
    public bool HasStartedNormalBattleEncounter => _encounterStarted;
    public int AliveEnemyCount => _aliveEnemies.Count;
    public int AliveEncounterTargetCount => _aliveEncounterTargets.Count;
    public int SpawnedEncounterEnemyCount => _spawnedEncounterEnemyCount;
    public int EncounterTotalSpawnCount => _resolvedEncounterTotalSpawnCount;

    private void Reset()
    {
        if (_sectorRuntime == null)
            _sectorRuntime = GetComponent<SectorRuntime>();

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();
    }

    private void Awake()
    {
        if (_sectorRuntime == null)
            _sectorRuntime = GetComponent<SectorRuntime>();

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorStateManager != null)
            _sectorStateManager.EnsureInitialized();
    }

    private void OnEnable()
    {
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.OnEventRaised += OnSectorOpened;

        if (_stageProgressChangedChannel != null)
        {
            _stageProgressChangedChannel.OnEventRaised += OnStageProgressChanged;

            if (_stageProgressChangedChannel.HasCurrent)
                OnStageProgressChanged(_stageProgressChangedChannel.Current);
        }

        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

        RefreshResolvedSettings();
        RefreshSectorActivity();
    }

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

    private void OnSectorOpened(SectorRuntime sector)
    {
        if (_sectorRuntime == null || sector != _sectorRuntime)
            return;

        RefreshSectorActivity();
    }

    private void OnStageProgressChanged(StageProgressSnapshot snapshot)
    {
        bool isThisCurrentSector =
            _sectorRuntime != null &&
            _sectorStateManager != null &&
            _sectorStateManager.CurrentSector == _sectorRuntime;

        _isBattleResolveCountdownLocked =
            snapshot.isResolveCountdown &&
            isThisCurrentSector;

        if (_currentStageIndex != snapshot.stageIndex)
        {
            _currentStageIndex = snapshot.stageIndex;
            RefreshResolvedSettings();
        }

        RefreshSectorActivity();
    }

    private void RefreshResolvedSettings()
    {
        _objectOccupiedSpawnPoints.Clear();
        _clearWithRoomObjects.Clear();
        _currentRule = null;
        _resolvedMaxAlive = 0;
        _resolvedEncounterTotalSpawnCount = 0;
        _spawnedEncounterEnemyCount = 0;
        _pendingEnemySpawnCount = 0;
        _spawnedEncounterObjectCount = 0;
        _encounterSpawnFinished = false;
        _encounterCleared = false;
        _selectedEncounterPreset = null;
        _selectedObjectRoomPreset = null;
        _selectedEncounterPresetName = string.Empty;
        _selectedObjectRoomPresetName = string.Empty;
        _resolvedEnemyWavePresets.Clear();
        _pendingEnemySpawnQueue.Clear();
        _nextEnemyWaveIndex = 0;
        _encounterStarted = false;
        _encounterStartTime = 0f;
        _sectorObjectsSpawned = false;
        _hasEncounterConfiguration = false;

        if (_stageBattleSettings == null)
            return;

        if (!_stageBattleSettings.TryGetRule(
                _currentStageIndex,
                out StageBattleSettingsSO.StageSpawnRule rule))
        {
            return;
        }

        _currentRule = rule;
        SelectDeterministicPresets(rule);
        ResetEncounterState();
    }

    private void SelectDeterministicPresets(StageBattleSettingsSO.StageSpawnRule rule)
    {
        if (_stageBattleSettings == null || _sectorRuntime == null || rule == null)
            return;

        int stageSeed = ResolveStageSeed();
        Vector2Int coord = _sectorRuntime.Coord;

        if (_stageBattleSettings.TryPickNormalBattleEncounterPreset(
                _currentStageIndex,
                stageSeed,
                coord,
                out StageBattleSettingsSO.NormalBattleEncounterPreset encounterPreset))
        {
            _selectedEncounterPreset = encounterPreset;
            _selectedEncounterPresetName = string.IsNullOrWhiteSpace(encounterPreset.displayName)
                ? "EncounterPreset"
                : encounterPreset.displayName;

            _resolvedMaxAlive = Mathf.Max(0, encounterPreset.sectorMaxAlive);
            ResolveEnemyWavePresetPlan(rule, stageSeed, coord);
        }

        if (_stageBattleSettings.TryPickSectorObjectRoomPreset(
                _currentStageIndex,
                stageSeed,
                coord,
                out StageBattleSettingsSO.SectorObjectRoomPreset objectPreset))
        {
            _selectedObjectRoomPreset = objectPreset;
            _selectedObjectRoomPresetName = string.IsNullOrWhiteSpace(objectPreset.displayName)
                ? "ObjectRoomPreset"
                : objectPreset.displayName;
        }

        _hasEncounterConfiguration =
            _selectedEncounterPreset != null ||
            _selectedObjectRoomPreset != null;

        _spawnRng = new System.Random(StageBattleSettingsSO.BuildSectorSeed(
            stageSeed,
            coord,
            997));

        if (!_hasEncounterConfiguration)
        {
            Debug.LogWarning(
                $"[SectorEnemySpawner] NormalBattle room has no encounter preset and no object room preset. stage={_currentStageIndex}, sector={coord}",
                this);
        }
    }

    private void ResolveEnemyWavePresetPlan(
        StageBattleSettingsSO.StageSpawnRule rule,
        int stageSeed,
        Vector2Int coord)
    {
        _resolvedEnemyWavePresets.Clear();
        _resolvedEncounterTotalSpawnCount = 0;

        if (_selectedEncounterPreset == null ||
            _selectedEncounterPreset.waves == null)
        {
            return;
        }

        for (int i = 0; i < _selectedEncounterPreset.waves.Count; i++)
        {
            StageBattleSettingsSO.NormalBattleEnemyWave wave =
                _selectedEncounterPreset.waves[i];

            int seed = StageBattleSettingsSO.BuildSectorSeed(
                stageSeed,
                coord,
                1100 + i);

            if (_stageBattleSettings.TryPickEnemyWavePreset(
                    rule,
                    wave,
                    seed,
                    out StageBattleSettingsSO.EnemyWavePreset enemyWavePreset))
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
    }

    private int ResolveStageSeed()
    {
        if (_sectorStateManager != null &&
            _sectorStateManager.CurrentStageMapLayout != null)
        {
            return _sectorStateManager.CurrentStageMapLayout.stageSeed;
        }

        return StageMapGenerator.BuildStageSeed(0, _currentStageIndex);
    }

    private static int GetEncounterWaveCount(
        StageBattleSettingsSO.NormalBattleEncounterPreset preset)
    {
        return preset != null && preset.waves != null
            ? preset.waves.Count
            : 0;
    }

    private void ResetEncounterState()
    {
        _spawnedEncounterEnemyCount = 0;
        _pendingEnemySpawnCount = 0;
        _spawnedEncounterObjectCount = 0;
        _pendingEnemySpawnQueue.Clear();
        RefreshEncounterSpawnFinished();
        RefreshEncounterCompletion();
    }

    private void EnsureEncounterStarted()
    {
        if (_encounterStarted)
            return;

        _encounterStarted = true;
        _encounterStartTime = Time.time;

        SpawnSectorObjectRoomPreset();
        RefreshEncounterSpawnFinished();
        RefreshEncounterCompletion();
    }

    private void TickEnemyEncounterWaves()
    {
        if (_selectedEncounterPreset == null ||
            _selectedEncounterPreset.waves == null ||
            _nextEnemyWaveIndex >= _selectedEncounterPreset.waves.Count)
        {
            return;
        }

        while (_nextEnemyWaveIndex < _selectedEncounterPreset.waves.Count)
        {
            StageBattleSettingsSO.NormalBattleEnemyWave wave =
                _selectedEncounterPreset.waves[_nextEnemyWaveIndex];

            float delay = wave != null ? Mathf.Max(0f, wave.delaySeconds) : 0f;

            if (Time.time < _encounterStartTime + delay)
                return;

            StageBattleSettingsSO.EnemyWavePreset enemyWavePreset =
                _nextEnemyWaveIndex < _resolvedEnemyWavePresets.Count
                    ? _resolvedEnemyWavePresets[_nextEnemyWaveIndex]
                    : null;

            EnqueueEnemyWavePreset(enemyWavePreset);
            _nextEnemyWaveIndex++;
        }
    }

    private void EnqueueEnemyWavePreset(StageBattleSettingsSO.EnemyWavePreset preset)
    {
        if (preset == null || preset.enemies == null)
            return;

        for (int i = 0; i < preset.enemies.Count; i++)
        {
            StageBattleSettingsSO.EnemyPresetSpawn spawn = preset.enemies[i];

            if (spawn == null || spawn.archetype == null || !spawn.archetype.IsValid)
                continue;

            int count = Mathf.Max(1, spawn.count);

            for (int j = 0; j < count; j++)
                _pendingEnemySpawnQueue.Enqueue(spawn.archetype);
        }

        _pendingEnemySpawnCount = _pendingEnemySpawnQueue.Count;
    }

    private void TickPendingEnemySpawnQueue()
    {
        while (_pendingEnemySpawnQueue.Count > 0)
        {
            if (_resolvedMaxAlive > 0 && _aliveEnemies.Count >= _resolvedMaxAlive)
                break;

            EnemyStatConfigSO enemyConfig = _pendingEnemySpawnQueue.Peek();

            if (!TrySpawnOne(enemyConfig))
                break;

            _pendingEnemySpawnQueue.Dequeue();
            _spawnedEncounterEnemyCount++;
            _pendingEnemySpawnCount = _pendingEnemySpawnQueue.Count;
        }
    }

    private void SpawnSectorObjectRoomPreset()
    {
        if (_sectorObjectsSpawned)
            return;

        _sectorObjectsSpawned = true;

        if (_selectedObjectRoomPreset == null ||
            _selectedObjectRoomPreset.objects == null)
        {
            return;
        }

        for (int i = 0; i < _selectedObjectRoomPreset.objects.Count; i++)
        {
            StageBattleSettingsSO.SectorObjectPresetSpawn spawn =
                _selectedObjectRoomPreset.objects[i];

        if (spawn == null || spawn.objectConfig == null ||!spawn.objectConfig.IsValid)
        {
            continue;
        }

            int count = Mathf.Max(1, spawn.count);

            for (int j = 0; j < count; j++)
            {
                if (TrySpawnSectorObject(spawn))
                    _spawnedEncounterObjectCount++;
            }
        }
    }

    private bool TrySpawnSectorObject(StageBattleSettingsSO.SectorObjectPresetSpawn spawn)
    {
        if (spawn == null ||
            spawn.objectConfig == null ||
            !spawn.objectConfig.IsValid)
        {
            return false;
        }

        if (!TryGetSpawnPoint(
                SectorObjectSpawnPointUsage.SectorObject,
                true,
                out Transform spawnPoint))
        {
            return false;
        }

        Transform parent = _spawnedSectorObjectsRoot != null
            ? _spawnedSectorObjectsRoot
            : _spawnedEnemiesRoot != null
                ? _spawnedEnemiesRoot
                : _sectorRuntime.transform;

        Quaternion rotation = spawn.objectConfig.ResolveSpawnRotation(spawnPoint, _spawnRng);

        GameObject instance = Instantiate(
            spawn.objectConfig.Prefab,
            spawnPoint.position,
            rotation,
            parent);

        if (instance == null)
            return false;

        _objectOccupiedSpawnPoints.Add(spawnPoint);

        if (spawn.objectConfig.ClearWithRoom)
            _clearWithRoomObjects.Add(instance);

        return true;
    }

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

        bool objectsFinished =
            _selectedObjectRoomPreset == null ||
            _sectorObjectsSpawned;

        _encounterSpawnFinished =
            enemyWavesFinished &&
            pendingEnemiesFinished &&
            objectsFinished;

        _pendingEnemySpawnCount = _pendingEnemySpawnQueue.Count;
    }

    private void RefreshEncounterCompletion()
    {
        if (!_hasEncounterConfiguration)
        {
            _encounterCleared = false;
            return;
        }
        bool wasCleared = _encounterCleared;

        _encounterCleared =
            _encounterSpawnFinished &&
            _aliveEncounterTargets.Count == 0;

        if (_encounterCleared && !wasCleared)
            CleanupClearWithRoomObjects();
    }
    private void CleanupClearWithRoomObjects()
    {
        for (int i = _clearWithRoomObjects.Count - 1; i >= 0; i--)
        {
            GameObject instance = _clearWithRoomObjects[i];

            if (instance != null)
                Destroy(instance);
        }

        _clearWithRoomObjects.Clear();
    }
    private bool CanRunEncounter()
    {

        if (_sectorRuntime == null || _stageBattleSettings == null)
            return false;

        if (!_hasEncounterConfiguration)
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
                roomType != StageRoomType.NormalBattle)
            {
                return false;
            }
        }

        return true;
    }

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

    private bool TrySpawnOne(EnemyStatConfigSO enemyConfig)
    {
        if (enemyConfig == null || enemyConfig.EnemyPrefab == null)
            return false;

        if (!TryGetSpawnPoint(
                SectorObjectSpawnPointUsage.Enemy,
                true,
                out Transform spawnPoint))
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
        BindEnemyStatConfig(enemyInstance, enemyConfig);
        BindProjectileRoot(enemyInstance);
        TrackEnemy(enemyInstance);

        return true;
    }

    private static void BindEnemyStatConfig(
        Enemy enemy,
        EnemyStatConfigSO enemyConfig)
    {
        if (enemy == null || enemyConfig == null)
            return;

        EnemyMovementStatsProvider[] movementProviders =
            enemy.GetComponentsInChildren<EnemyMovementStatsProvider>(true);

        for (int i = 0; i < movementProviders.Length; i++)
            movementProviders[i].SetEnemyStatConfig(enemyConfig);

        EnemyContactDamage[] contactDamages =
            enemy.GetComponentsInChildren<EnemyContactDamage>(true);

        for (int i = 0; i < contactDamages.Length; i++)
            contactDamages[i].SetEnemyStatConfig(enemyConfig);

        EnemyVirusTrail[] virusTrails =
            enemy.GetComponentsInChildren<EnemyVirusTrail>(true);

        for (int i = 0; i < virusTrails.Length; i++)
            virusTrails[i].SetEnemyStatConfig(enemyConfig);

        EnemyKillRewardSource[] killRewardSources =
            enemy.GetComponentsInChildren<EnemyKillRewardSource>(true);

        for (int i = 0; i < killRewardSources.Length; i++)
            killRewardSources[i].SetEnemyStatConfig(enemyConfig);

        EnemyScreenSpaceHPUIAnchor[] uiAnchors =
            enemy.GetComponentsInChildren<EnemyScreenSpaceHPUIAnchor>(true);

        for (int i = 0; i < uiAnchors.Length; i++)
            uiAnchors[i].SetEnemyStatConfig(enemyConfig);
    }

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
    private bool TryGetSpawnPoint(
        SectorObjectSpawnPointUsage usage,
        bool excludeObjectOccupiedPoints,
        out Transform spawnPoint)
    {
        spawnPoint = null;
        _spawnPointBuffer.Clear();

        if (_sectorRuntime == null)
            return false;

        Transform[] runtimePoints = _sectorRuntime.ObjectSpawnPoints;

        if (runtimePoints != null)
        {
            for (int i = 0; i < runtimePoints.Length; i++)
            {
                Transform point = runtimePoints[i];

                if (point == null)
                    continue;

                if (excludeObjectOccupiedPoints &&
                    _objectOccupiedSpawnPoints.Contains(point))
                {
                    continue;
                }

                SectorObjectSpawnPoint metadata =
                    point.GetComponent<SectorObjectSpawnPoint>();

                if (metadata != null && !metadata.CanUseFor(usage))
                    continue;

                _spawnPointBuffer.Add(point);
            }
        }

        if (_spawnPointBuffer.Count == 0 &&
            _sectorRuntime.ObjectSpawnPointMetadata != null)
        {
            SectorObjectSpawnPoint[] metadataPoints =
                _sectorRuntime.ObjectSpawnPointMetadata;

            for (int i = 0; i < metadataPoints.Length; i++)
            {
                SectorObjectSpawnPoint metadata = metadataPoints[i];

                if (metadata == null || !metadata.CanUseFor(usage))
                    continue;

                Transform point = metadata.transform;

                if (excludeObjectOccupiedPoints &&
                    _objectOccupiedSpawnPoints.Contains(point))
                {
                    continue;
                }

                _spawnPointBuffer.Add(point);
            }
        }

        if (_spawnPointBuffer.Count <= 0)
            return false;

        int index = _spawnRng != null
            ? _spawnRng.Next(0, _spawnPointBuffer.Count)
            : Random.Range(0, _spawnPointBuffer.Count);

        spawnPoint = _spawnPointBuffer[index];
        return spawnPoint != null;
    }

    private void TrackEnemy(Enemy enemy)
    {
        if (enemy == null || _aliveEnemies.Contains(enemy))
            return;

        _aliveEnemies.Add(enemy);

        if (enemy.TryGetComponent(out Damageable damageable) &&
            damageable != null)
        {
            UnityAction handler = () => OnTrackedEnemyDied(enemy);
            damageable.OnDie += handler;

            _trackedDamageables[enemy] = damageable;
            _deathHandlers[enemy] = handler;

            TrackEncounterTarget(damageable);
        }
    }

    private void OnTrackedEnemyDied(Enemy enemy)
    {
        UntrackEnemy(enemy);
        RefreshEncounterCompletion();
    }

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

    private void OnTrackedEncounterTargetDied(Damageable damageable)
    {
        UntrackEncounterTarget(damageable);
        RefreshEncounterCompletion();
    }

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

    private void RefreshAliveEncounterTargetDebugCount()
    {
        _aliveEncounterTargetCount = _aliveEncounterTargets.Count;
    }

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

    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();

        RefreshResolvedSettings();
        RefreshSectorActivity();
    }
}