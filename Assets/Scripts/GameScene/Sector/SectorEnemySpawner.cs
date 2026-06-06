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
    [SerializeField] private StageEnemySettingSO _stageEnemySetting;

    [SerializeField] private Transform _spawnedEnemiesRoot;

    [Header("Exclusion")]
    [SerializeField] private SectorExclusionRulesSO _sectorExclusionRules;

    [Header("Listening To")]
    [SerializeField] private SectorRuntimeEventChannelSO _sectorOpenedEvent;
    [SerializeField] private StageProgressSnapshotEventChannelSO _stageProgressChangedChannel;

    [Header("Named Battle Lock")]
    [SerializeField] private BoolEventChannelSO _namedBattleWorldLockedChannel;
    
    
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    [Header("Options")]
    [SerializeField] private bool _spawnOnlyWhenOpened = true;
    [SerializeField] private bool _fillMinimumImmediately = false;
    [SerializeField] private int _maxImmediateSpawnPerTick = 3;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private int _currentStageIndex = -1;
    [ReadOnly] [SerializeField] private int _resolvedMinAlive;
    [ReadOnly] [SerializeField] private int _resolvedMaxAlive;
    [ReadOnly] [SerializeField] private int _resolvedSimultaneousSpawnCount = 1;
    [ReadOnly] [SerializeField] private float _resolvedFirstSpawnInterval;
    [ReadOnly] [SerializeField] private float _resolvedSpawnInterval;
    [ReadOnly] [SerializeField] private bool _hasSpawnedFirstBatch;
    [ReadOnly] [SerializeField] private bool _isSectorActive;
    [ReadOnly] [SerializeField] private bool _isNamedBattleWorldLocked;
    private SectorStateManager _sectorStateManager;
    private readonly List<Enemy> _aliveEnemies = new();
    private readonly Dictionary<Enemy, Damageable> _trackedDamageables = new();
    private readonly Dictionary<Enemy, UnityAction> _deathHandlers = new();
    private readonly List<Transform> _spawnPointBuffer = new();

    private float _nextSpawnTime;

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
            _stageProgressChangedChannel.OnEventRaised += OnStageProgressChanged;

        if (_namedBattleWorldLockedChannel != null)
            _namedBattleWorldLockedChannel.OnEventRaised += OnNamedBattleWorldLocked;
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

        if (_namedBattleWorldLockedChannel != null)
            _namedBattleWorldLockedChannel.OnEventRaised -= OnNamedBattleWorldLocked;
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;
        UnbindAllTrackedEnemies();
    }

    private void Update()
    {
        CleanupTrackedEnemies();
        RefreshSectorActivity();

        if (!CanSpawn())
            return;

        if (_aliveEnemies.Count >= _resolvedMaxAlive)
            return;

        if (_aliveEnemies.Count < _resolvedMinAlive)
        {
            if (!_fillMinimumImmediately && Time.time < _nextSpawnTime)
                return;

            int spawnedThisTick = 0;
            int maxSpawnCount = Mathf.Max(1, _maxImmediateSpawnPerTick);

            while (_aliveEnemies.Count < _resolvedMinAlive &&
                   spawnedThisTick < maxSpawnCount &&
                   _aliveEnemies.Count < _resolvedMaxAlive)
            {
                int spawned = TrySpawnEnemyBatch(maxSpawnCount - spawnedThisTick);

                if (spawned <= 0)
                    break;

                spawnedThisTick += spawned;

                if (!_fillMinimumImmediately)
                    break;
            }

            if (spawnedThisTick > 0)
                HandleSuccessfulSpawnBatch();

            return;
        }

        if (Time.time < _nextSpawnTime)
            return;

        if (TrySpawnEnemyBatch(int.MaxValue) > 0)
            HandleSuccessfulSpawnBatch();
    }

    private void OnNamedBattleWorldLocked(bool locked)
    {
        _isNamedBattleWorldLocked = locked;
    }

    private void OnSectorOpened(SectorRuntime sector)
    {
        if (_sectorRuntime == null || sector != _sectorRuntime)
            return;

        RefreshSectorActivity();
    }

    private void OnStageProgressChanged(StageProgressSnapshot snapshot)
    {
        if (_currentStageIndex == snapshot.stageIndex)
            return;

        _currentStageIndex = snapshot.stageIndex;
        RefreshResolvedSettings();
    }

    private void RefreshResolvedSettings()
    {
        _resolvedMinAlive = 0;
        _resolvedMaxAlive = 0;
        _resolvedSimultaneousSpawnCount = 1;
        _resolvedFirstSpawnInterval = 0f;
        _resolvedSpawnInterval = 0f;
        _hasSpawnedFirstBatch = false;
        _nextSpawnTime = float.PositiveInfinity;

        if (_stageEnemySetting == null)
            return;

        if (!_stageEnemySetting.TryGetRule(
                _currentStageIndex,
                out StageEnemySettingSO.StageSpawnRule rule))
        {
            return;
        }

        _resolvedMinAlive = Mathf.Max(0, rule.sectorMinAlive);
        _resolvedMaxAlive = Mathf.Max(_resolvedMinAlive, rule.sectorMaxAlive);
        _resolvedSimultaneousSpawnCount = Mathf.Max(1, rule.SimultaneousSpawnCount);

        _resolvedFirstSpawnInterval = DifficultyRuntime.ApplyEnemySpawnInterval(
            rule.firstSpawnIntervalSeconds);

        _resolvedSpawnInterval = DifficultyRuntime.ApplyEnemySpawnInterval(
            rule.spawnIntervalSeconds);

        ResetFirstSpawnSchedule();
    }

    private void ResetFirstSpawnSchedule()
    {
        _hasSpawnedFirstBatch = false;

        _nextSpawnTime = _fillMinimumImmediately
            ? Time.time
            : Time.time + _resolvedFirstSpawnInterval;
    }

    private void HandleSuccessfulSpawnBatch()
    {
        _hasSpawnedFirstBatch = true;
        _nextSpawnTime = Time.time + _resolvedSpawnInterval;
    }

    private void ScheduleAfterEnemyRemoved()
    {
        if (!_hasSpawnedFirstBatch)
            return;

        _nextSpawnTime = Time.time + _resolvedSpawnInterval;
    }

    private bool CanSpawn()
    {
        if (_isNamedBattleWorldLocked)
            return false;

        if (_sectorRuntime == null || _stageEnemySetting == null)
            return false;

        if (_sectorExclusionRules != null &&
            _sectorExclusionRules.ExcludeFromEnemySpawn(_sectorRuntime.Coord))
        {
            return false;
        }

        if (!_isSectorActive)
            return false;

        if (_sectorRuntime.IsCleared)
            return false;

        if (_resolvedMaxAlive <= 0)
            return false;

        return true;
    }

    private void RefreshSectorActivity()
    {
        bool wasActive = _isSectorActive;

        _isSectorActive =
            _sectorRuntime != null &&
            _sectorStateManager != null &&
            _sectorStateManager.CurrentSector == _sectorRuntime &&
            (!_spawnOnlyWhenOpened || _sectorRuntime.IsOpened) &&
            !_sectorRuntime.IsCleared;

        if (_isSectorActive && !wasActive && float.IsPositiveInfinity(_nextSpawnTime))
            ResetFirstSpawnSchedule();
    }
    private int TrySpawnEnemyBatch(int maxBatchLimit)
    {
        if (_sectorRuntime == null || _stageEnemySetting == null)
            return 0;

        int remainingCapacity = Mathf.Max(0, _resolvedMaxAlive - _aliveEnemies.Count);
        int maxAllowedThisCall = Mathf.Max(1, maxBatchLimit);

        int spawnCount = Mathf.Min(
            _resolvedSimultaneousSpawnCount,
            remainingCapacity,
            maxAllowedThisCall);

        int spawned = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            if (!_stageEnemySetting.TryPickArchetype(
                    _currentStageIndex,
                    out EnemyStatConfigSO enemyConfig))
            {
                break;
            }

            if (!TrySpawnOne(enemyConfig))
                break;

            spawned++;
        }

        return spawned;
    }

    private bool TrySpawnOne(EnemyStatConfigSO enemyConfig)
    {
        if (enemyConfig == null || enemyConfig.EnemyPrefab == null)
            return false;

        if (!TryGetSpawnPoint(out Transform spawnPoint))
            return false;

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

    private bool TryGetSpawnPoint(out Transform spawnPoint)
    {
        spawnPoint = null;
        _spawnPointBuffer.Clear();

        if (_sectorRuntime == null)
            return false;

        Transform[] runtimePoints = _sectorRuntime.enemySpawnPoints;

        if (runtimePoints != null)
        {
            for (int i = 0; i < runtimePoints.Length; i++)
            {
                Transform point = runtimePoints[i];

                if (point == null)
                    continue;

                EnemySpawnPoint metadata = point.GetComponent<EnemySpawnPoint>();

                if (metadata != null && !metadata.EnabledForSpawning)
                    continue;

                _spawnPointBuffer.Add(point);
            }
        }

        if (_spawnPointBuffer.Count == 0 &&
            _sectorRuntime.SpawnPointMetadata != null)
        {
            EnemySpawnPoint[] metadataPoints = _sectorRuntime.SpawnPointMetadata;

            for (int i = 0; i < metadataPoints.Length; i++)
            {
                EnemySpawnPoint metadata = metadataPoints[i];

                if (metadata == null || !metadata.EnabledForSpawning)
                    continue;

                _spawnPointBuffer.Add(metadata.transform);
            }
        }

        if (_spawnPointBuffer.Count <= 0)
            return false;

        spawnPoint = _spawnPointBuffer[
            UnityEngine.Random.Range(0, _spawnPointBuffer.Count)];

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
        }
    }

    private void OnTrackedEnemyDied(Enemy enemy)
    {
        UntrackEnemy(enemy);
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
            }
        }
    }

    private void RemoveDestroyedEnemyAt(int index)
    {
        if (index < 0 || index >= _aliveEnemies.Count)
            return;

        Enemy enemy = _aliveEnemies[index];

        if (enemy != null)
        {
            _trackedDamageables.Remove(enemy);
            _deathHandlers.Remove(enemy);
        }

        _aliveEnemies.RemoveAt(index);
        ScheduleAfterEnemyRemoved();
    }

    private void UntrackEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            _aliveEnemies.Remove(enemy);
            ScheduleAfterEnemyRemoved();
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
        }

        _deathHandlers.Remove(enemy);
        _aliveEnemies.Remove(enemy);
        ScheduleAfterEnemyRemoved();
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
