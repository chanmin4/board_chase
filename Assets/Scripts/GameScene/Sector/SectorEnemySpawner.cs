using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class SectorEnemySpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorRuntime _sectorRuntime; 
    // 이 스포너가 관리할 sector runtime.
    // spawn point 목록, opened 상태, sector 좌표 등의 기준이 된다.

    [SerializeField] private StageEnemySpawnTableSO _spawnTable; 
    // 현재 stage 기준으로
    // 어떤 일반몹이 나오는지, sector min/max alive와 spawn interval이 얼마인지 정의한 테이블.

    [SerializeField] private Transform _spawnedEnemiesRoot; 
    // 실제 생성된 적들을 정리해서 붙여둘 부모.
    // 비워두면 기본적으로 해당 sector runtime 밑에 생성.


    [Header("Listening To")]
    [SerializeField] private SectorRuntimeEventChannelSO _sectorOpenedEvent; 
    // sector가 열렸을 때 받는 이벤트 채널.
    // spawnOnlyWhenOpened가 켜져 있으면 이 이벤트 이후부터 스폰 가능해진다.

    [SerializeField] private StageProgressSnapshotEventChannelSO _stageProgressChangedChannel; 
    // 현재 stage index 변경을 받는 채널.
    // 이를 기준으로 resolved min/max alive, spawn interval을 갱신한다.


    [Header("Options")]
    [SerializeField] private bool _spawnOnlyWhenOpened = true; 
    // true면 sector가 opened 상태일 때만 적을 스폰한다.

    [SerializeField] private bool _fillMinimumImmediately = false;
    // true면 현재 살아있는 적 수가 resolved min보다 적을 때
    // min 수치까지 가능한 한 즉시 채우려 한다.
    // false면 일반 spawn interval 주기만 따른다.

    [SerializeField] private int _maxImmediateSpawnPerTick = 3; 
    // fillMinimumImmediately가 켜져 있을 때
    // 한 프레임에 최대로 즉시 생성할 적 수.


    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private int _currentStageIndex=-1; 
    // 현재 적용 중인 stage index.

    [ReadOnly] [SerializeField] private int _resolvedMinAlive; 
    // 현재 stage rule에서 계산된 sector 최소 유지 적 수.

    [ReadOnly] [SerializeField] private int _resolvedMaxAlive; 
    // 현재 stage rule에서 계산된 sector 최대 유지 적 수.

    [ReadOnly] [SerializeField] private float _resolvedSpawnInterval; 
    // 현재 stage rule에서 계산된 스폰 간격(초).

    [ReadOnly] [SerializeField] private bool _isSectorActive; 
    // 현재 이 sector가 스폰 활성 상태인지.


    private readonly List<Enemy> _aliveEnemies = new();
    private readonly Dictionary<Enemy, Damageable> _trackedDamageables = new();
    private readonly Dictionary<Enemy, UnityAction> _deathHandlers = new();
    private readonly List<Transform> _spawnPointBuffer = new();

    private float _nextSpawnTime;

    private void Reset()
    {
        if (_sectorRuntime == null)
            _sectorRuntime = GetComponent<SectorRuntime>();
    }

    private void Awake()
    {
        if (_sectorRuntime == null)
            _sectorRuntime = GetComponent<SectorRuntime>();
    }

    private void OnEnable()
    {
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.OnEventRaised += OnSectorOpened;

        if (_stageProgressChangedChannel != null)
            _stageProgressChangedChannel.OnEventRaised += OnStageProgressChanged;

        _isSectorActive = _sectorRuntime != null && (!_spawnOnlyWhenOpened || _sectorRuntime.IsOpened);
        RefreshResolvedSettings();
    }

    private void OnDisable()
    {
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.OnEventRaised -= OnSectorOpened;

        if (_stageProgressChangedChannel != null)
            _stageProgressChangedChannel.OnEventRaised -= OnStageProgressChanged;

        UnbindAllTrackedEnemies();
    }

    private void Update()
    {
        CleanupTrackedEnemies();

        if (!CanSpawn())
        {
        
            return;
        }

        if (_aliveEnemies.Count >= _resolvedMaxAlive)
        {   
            return;
        }

        if (Time.time < _nextSpawnTime)
        {
            // 아직 다음 스폰 시각 전.
            // 타이머 때문에 안 나오는 건 정상이라 너무 시끄러우면 로그 안 찍는 게 낫다.
            return;
        }

        if (_aliveEnemies.Count < _resolvedMinAlive)
        {

            int immediateSpawnCount = 0;
            int maxSpawnCount = Mathf.Max(1, _maxImmediateSpawnPerTick);

            while (_aliveEnemies.Count < _resolvedMinAlive && immediateSpawnCount < maxSpawnCount)
            {
                if (!TrySpawnOne())
                    break;

                immediateSpawnCount++;
            }

            _nextSpawnTime = Time.time + _resolvedSpawnInterval;
            return;
        }

        if (TrySpawnOne())
        {

            _nextSpawnTime = Time.time + _resolvedSpawnInterval;
        }
    }

    private void OnSectorOpened(SectorRuntime sector)
    {
        if (_sectorRuntime == null || sector != _sectorRuntime)
            return;

        _isSectorActive = true;
        _nextSpawnTime = Time.time + _resolvedSpawnInterval;

        Debug.Log(
            $"[SectorEnemySpawner] SectorOpened " +
            $"sector={sector.name}, nextSpawnTime={_nextSpawnTime}, " +
            $"resolvedSpawnInterval={_resolvedSpawnInterval}");
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
        _resolvedSpawnInterval = 0f;

        if (_spawnTable == null)
        {
           // Debug.Log("[SectorEnemySpawner] RefreshResolvedSettings failed: spawnTable is null");
            return;
        }

        if (!_spawnTable.TryGetRule(_currentStageIndex, out StageEnemySpawnTableSO.StageSpawnRule rule))
        {
            //Debug.Log($"[SectorEnemySpawner] RefreshResolvedSettings failed: no rule for stageIndex={_currentStageIndex}");
            return;
        }

        _resolvedMinAlive = Mathf.Max(0, rule.sectorMinAlive);
        _resolvedMaxAlive = Mathf.Max(_resolvedMinAlive, rule.sectorMaxAlive);
        _resolvedSpawnInterval = Mathf.Max(0.01f, rule.spawnIntervalSeconds);

        _nextSpawnTime = Time.time + _resolvedSpawnInterval;

    }

    private bool CanSpawn()
    {
        if (_sectorRuntime == null || _spawnTable == null)
            return false;

        if (_spawnOnlyWhenOpened && !_isSectorActive)
            return false;

        if (_resolvedMaxAlive <= 0)
            return false;

        return true;
    }

    private bool TrySpawnOne()
    {
        if (_sectorRuntime == null || _spawnTable == null)
        {
            Debug.Log("[SectorEnemySpawner] TrySpawnOne failed: missing sectorRuntime or spawnTable");
            return false;
        }

        if (!_spawnTable.TryPickArchetype(_currentStageIndex, out EnemyArchetypeSO archetype))
        {
            Debug.Log($"[SectorEnemySpawner] TrySpawnOne failed: no archetype for stageIndex={_currentStageIndex}");
            return false;
        }

        if (archetype == null || archetype.EnemyPrefab == null)
        {
            Debug.Log("[SectorEnemySpawner] TrySpawnOne failed: invalid archetype or EnemyPrefab is null");
            return false;
        }

        if (!TryGetSpawnPoint(out Transform spawnPoint))
        {
            Debug.Log("[SectorEnemySpawner] TrySpawnOne failed: no valid spawn point found");
            return false;
        }

        Transform parent = _spawnedEnemiesRoot != null ? _spawnedEnemiesRoot : _sectorRuntime.transform;

        Debug.Log(
            $"[SectorEnemySpawner] TrySpawnOne instantiate " +
            $"archetype={archetype.name}, prefab={archetype.EnemyPrefab.name}, " +
            $"spawnPoint={spawnPoint.name}, position={spawnPoint.position}, parent={parent.name}");

        Enemy enemyInstance = Instantiate(
            archetype.EnemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            parent);

        if (enemyInstance == null)
        {
            Debug.Log("[SectorEnemySpawner] TrySpawnOne failed: Instantiate returned null");
            return false;
        }

        Debug.Log($"[SectorEnemySpawner] TrySpawnOne success: instantiated {enemyInstance.name}");

        enemyInstance.SetCurrentSector(_sectorRuntime);
        TrackEnemy(enemyInstance);
        return true;
    }

    private bool TryGetSpawnPoint(out Transform spawnPoint)
    {
        spawnPoint = null;
        _spawnPointBuffer.Clear();

        if (_sectorRuntime == null)
        {
            Debug.Log("[SectorEnemySpawner] TryGetSpawnPoint failed: sectorRuntime is null");
            return false;
        }

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

        if (_spawnPointBuffer.Count == 0 && _sectorRuntime.SpawnPointMetadata != null)
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

        spawnPoint = _spawnPointBuffer[UnityEngine.Random.Range(0, _spawnPointBuffer.Count)];

        Debug.Log(
            $"[SectorEnemySpawner] TryGetSpawnPoint success " +
            $"candidateCount={_spawnPointBuffer.Count}, picked={spawnPoint.name}, position={spawnPoint.position}");

        return spawnPoint != null;
    }

    private void TrackEnemy(Enemy enemy)
    {
        if (enemy == null || _aliveEnemies.Contains(enemy))
            return;

        _aliveEnemies.Add(enemy);

        Debug.Log($"[SectorEnemySpawner] TrackEnemy {enemy.name}, aliveCount={_aliveEnemies.Count}");

        if (enemy.TryGetComponent(out Damageable damageable) && damageable != null)
        {
            UnityAction handler = () => OnTrackedEnemyDied(enemy);
            damageable.OnDie += handler;
            _trackedDamageables[enemy] = damageable;
            _deathHandlers[enemy] = handler;
        }
    }

    private void OnTrackedEnemyDied(Enemy enemy)
    {
        Debug.Log($"[SectorEnemySpawner] OnTrackedEnemyDied {enemy.name}");
        UntrackEnemy(enemy);
    }

    private void CleanupTrackedEnemies()
    {
        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _aliveEnemies[i];
            if (enemy == null)
            {
                if (ReferenceEquals(enemy, null))
                    _aliveEnemies.RemoveAt(i);
                else
                    UntrackEnemy(enemy);
                continue;
            }

            if (_trackedDamageables.TryGetValue(enemy, out Damageable damageable) && damageable != null && damageable.IsDead)
            {
                UntrackEnemy(enemy);
            }
        }
    }

    private void UntrackEnemy(Enemy enemy)
    {
        if (ReferenceEquals(enemy, null))
            return;

        if (_trackedDamageables.TryGetValue(enemy, out Damageable damageable) && damageable != null)
        {
            if (_deathHandlers.TryGetValue(enemy, out UnityAction handler))
                damageable.OnDie -= handler;

            _trackedDamageables.Remove(enemy);
        }

        _deathHandlers.Remove(enemy);
        _aliveEnemies.Remove(enemy);
        _nextSpawnTime = Time.time + _resolvedSpawnInterval;

        Debug.Log(
            $"[SectorEnemySpawner] UntrackEnemy {enemy.name}, " +
            $"aliveCount={_aliveEnemies.Count}, nextSpawnTime={_nextSpawnTime}");
    }

    private void UnbindAllTrackedEnemies()
    {
        foreach (var pair in _trackedDamageables)
        {
            if (pair.Value != null && _deathHandlers.TryGetValue(pair.Key, out UnityAction handler))
                pair.Value.OnDie -= handler;
        }

        _deathHandlers.Clear();
        _trackedDamageables.Clear();
        _aliveEnemies.Clear();
    }
}
