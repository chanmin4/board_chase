using UnityEngine;
using UnityEngine.Serialization;

public class EnemyVirusTrail : MonoBehaviour
{
    private const float ManagerLookupInterval = 1f;

    [Header("Refs")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Enemy Stat Config")]
    [FormerlySerializedAs("_settings")]
    [SerializeField] private EnemyStatConfigSO _enemyStatConfig;

    [Header("Runtime")]
    [Tooltip("Usually leave true. Base enable/disable lives in EnemyStatConfigSO.")]
    [FormerlySerializedAs("_trailEnabled")]
    [SerializeField] private bool _runtimeTrailEnabled = true;

    private Transform _cachedTransform;
    private MaskRenderManager _maskRenderManager;
    private EnemyMovementStatsProvider _movementStatsProvider;
    private EnemyStatConfigSO _resolvedConfig;

    private Vector3 _lastPaintPosition;
    private float _lastPaintTime;
    private float _nextStampTime;
    private float _nextManagerLookupTime;
    private bool _hasPaintPosition;

    public void SetEnemyStatConfig(EnemyStatConfigSO enemyStatConfig)
    {
        _enemyStatConfig = enemyStatConfig;
        _resolvedConfig = enemyStatConfig;
    }

    public void SetTrailEnabled(bool enabled)
    {
        _runtimeTrailEnabled = enabled;

        if (enabled)
            ResetTrail();
    }

    private void Awake()
    {
        _cachedTransform = transform;
        CacheMovementStatsProvider();
        CacheStatConfig();
    }

    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManager = _maskRenderManagerReadyChannel.Current;

        CacheStatConfig();
        ResetTrail();
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;
    }

    private void LateUpdate()
    {
        if (!_runtimeTrailEnabled)
            return;

        EnemyStatConfigSO config = ResolveStatConfig();
        if (config == null || !config.VirusTrailEnabled)
            return;

        Transform currentTransform = _cachedTransform != null ? _cachedTransform : transform;
        Vector3 current = currentTransform.position;

        if (!_hasPaintPosition)
        {
            SetLastPaintPosition(current, Time.time);
            return;
        }

        Vector3 from = _lastPaintPosition;
        Vector3 to = current;
        from.y = 0f;
        to.y = 0f;

        Vector3 delta = to - from;
        float movedSqr = delta.sqrMagnitude;

        float minSegmentDistance = config.VirusTrailMinSegmentDistance;
        if (movedSqr <= minSegmentDistance * minSegmentDistance)
            return;

        float now = Time.time;
        if (now < _nextStampTime)
            return;

        float moved = Mathf.Sqrt(movedSqr);

        float teleportResetDistance = config.VirusTrailTeleportResetDistance;
        if (teleportResetDistance > 0f && moved > teleportResetDistance)
        {
            ResetTrail(current, now);
            return;
        }

        float elapsed = Mathf.Max(0.0001f, now - _lastPaintTime);
        float speed = moved / elapsed;

        if (speed < config.VirusTrailMinMoveSpeed)
        {
            SetLastPaintPosition(current, now);
            return;
        }

        MaskRenderManager manager = ResolveMaskRenderManager(now);
        if (manager == null)
        {
            SetLastPaintPosition(current, now);
            return;
        }

        Vector3 paintFrom = _lastPaintPosition;

        float maxPaintSegmentDistance = config.VirusTrailMaxPaintSegmentDistance;
        if (maxPaintSegmentDistance > 0f && moved > maxPaintSegmentDistance)
        {
            Vector3 flatDirection = delta / moved;
            paintFrom = current - flatDirection * maxPaintSegmentDistance;
        }

        manager.RequestVirusTrailSegment(
            paintFrom,
            current,
            config.VirusTrailRadius,
            config.VirusTrailPaintPriority,
            gameObject,
            config.VirusTrailSpacing,
            config.VirusTrailMaxSteps);

        SetLastPaintPosition(current, now);
        _nextStampTime = now + config.VirusTrailStampInterval;
    }

    private EnemyStatConfigSO ResolveStatConfig()
    {
        if (_resolvedConfig != null)
            return _resolvedConfig;

        return CacheStatConfig();
    }

    private EnemyStatConfigSO CacheStatConfig()
    {
        if (_enemyStatConfig != null)
        {
            _resolvedConfig = _enemyStatConfig;
            return _resolvedConfig;
        }

        if (_movementStatsProvider == null)
            CacheMovementStatsProvider();

        _resolvedConfig = _movementStatsProvider != null
            ? _movementStatsProvider.EnemyStatConfig
            : null;

        return _resolvedConfig;
    }

    private void CacheMovementStatsProvider()
    {
        _movementStatsProvider =
            GetComponent<EnemyMovementStatsProvider>() ??
            GetComponentInParent<EnemyMovementStatsProvider>() ??
            GetComponentInChildren<EnemyMovementStatsProvider>(true);
    }

    private void ResetTrail()
    {
        ResetTrail(_cachedTransform != null ? _cachedTransform.position : transform.position, Time.time);
    }

    private void ResetTrail(Vector3 position, float now)
    {
        SetLastPaintPosition(position, now);

        float stampInterval = ResolveStatConfig()?.VirusTrailStampInterval ?? 0f;

        _nextStampTime = stampInterval > 0f
            ? now + Random.Range(0f, stampInterval)
            : now;
    }

    private void SetLastPaintPosition(Vector3 position, float time)
    {
        _lastPaintPosition = position;
        _lastPaintTime = time;
        _hasPaintPosition = true;
    }

    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    private MaskRenderManager ResolveMaskRenderManager(float now)
    {
        if (_maskRenderManager != null)
            return _maskRenderManager;

        if (_maskRenderManagerReadyChannel != null && _maskRenderManagerReadyChannel.Current != null)
        {
            _maskRenderManager = _maskRenderManagerReadyChannel.Current;
            return _maskRenderManager;
        }

        if (now < _nextManagerLookupTime)
            return null;

        _nextManagerLookupTime = now + ManagerLookupInterval;
        _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();
        return _maskRenderManager;
    }
}
