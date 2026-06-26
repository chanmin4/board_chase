using UnityEngine;
using UnityEngine.Serialization;

public class EnemyVirusTrail : MonoBehaviour
{
    private const float ManagerLookupInterval = 1f;
    private const float MinTrailSegmentSqr = 0.0001f;

    [Header("Refs")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Enemy Stat Config")]
    [FormerlySerializedAs("_settings")]
    [SerializeField] private CreatureEnemyStatConfigSO _enemyStatConfig;

    [Header("Runtime")]
    [Tooltip("Usually leave true. Base enable/disable lives in EnemyStatConfigSO.")]
    [FormerlySerializedAs("_trailEnabled")]
    [SerializeField] private bool _runtimeTrailEnabled = true;

    private Transform _cachedTransform;
    private MaskRenderManager _maskRenderManager;
    private EnemyMovementStatsProvider _movementStatsProvider;
    private CreatureEnemyStatConfigSO _resolvedConfig;

    private Vector3 _lastBodyPosition;
    private Vector3 _lastTrailPoint;
    private float _lastPaintTime;
    private float _nextStampTime;
    private float _nextManagerLookupTime;
    private bool _hasBodyPosition;
    private bool _hasTrailPoint;

    public void SetEnemyStatConfig(CreatureEnemyStatConfigSO enemyStatConfig)
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

        CreatureEnemyStatConfigSO config = ResolveStatConfig();
        if (config == null || !config.VirusTrailEnabled)
            return;

        Transform currentTransform = _cachedTransform != null ? _cachedTransform : transform;
        Vector3 currentBodyPosition = currentTransform.position;

        if (!_hasBodyPosition)
        {
            SetLastBodyPosition(currentBodyPosition, Time.time);
            return;
        }

        Vector3 fromBody = _lastBodyPosition;
        Vector3 toBody = currentBodyPosition;
        fromBody.y = 0f;
        toBody.y = 0f;

        Vector3 bodyDelta = toBody - fromBody;
        float movedSqr = bodyDelta.sqrMagnitude;

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
            ResetTrail(currentBodyPosition, now);
            return;
        }

        float elapsed = Mathf.Max(0.0001f, now - _lastPaintTime);
        float speed = moved / elapsed;

        if (speed < config.VirusTrailMinMoveSpeed)
        {
            SetLastBodyPosition(currentBodyPosition, now);
            return;
        }

        MaskRenderManager manager = ResolveMaskRenderManager(now);
        if (manager == null)
        {
            SetLastBodyPosition(currentBodyPosition, now);
            return;
        }

        Vector3 moveDirection = bodyDelta / moved;
        float rearOffset = config.VirusTrailRadius + config.VirusTrailSelfExclusionDistance;

        Vector3 currentTrailPoint = currentBodyPosition - moveDirection * rearOffset;
        currentTrailPoint.y = currentBodyPosition.y;

        Vector3 previousTrailPoint = _hasTrailPoint
            ? _lastTrailPoint
            : _lastBodyPosition - moveDirection * rearOffset;

        previousTrailPoint.y = currentBodyPosition.y;

        float maxPaintSegmentDistance = config.VirusTrailMaxPaintSegmentDistance;
        if (maxPaintSegmentDistance > 0f)
        {
            Vector3 trailDelta = currentTrailPoint - previousTrailPoint;
            trailDelta.y = 0f;

            float trailDistance = trailDelta.magnitude;
            if (trailDistance > maxPaintSegmentDistance)
                previousTrailPoint = currentTrailPoint - moveDirection * maxPaintSegmentDistance;
        }

        Vector3 finalTrailDelta = currentTrailPoint - previousTrailPoint;
        finalTrailDelta.y = 0f;

        if (finalTrailDelta.sqrMagnitude <= MinTrailSegmentSqr)
        {
            SetLastTrailState(currentBodyPosition, currentTrailPoint, now);
            _nextStampTime = now + config.VirusTrailStampInterval;
            return;
        }

        manager.RequestVirusTrailSegment(
            previousTrailPoint,
            currentTrailPoint,
            config.VirusTrailRadius,
            config.VirusTrailPaintPriority,
            gameObject,
            config.VirusTrailSpacing,
            config.VirusTrailMaxSteps);

        SetLastTrailState(currentBodyPosition, currentTrailPoint, now);
        _nextStampTime = now + config.VirusTrailStampInterval;
    }

    private CreatureEnemyStatConfigSO ResolveStatConfig()
    {
        if (_resolvedConfig != null)
            return _resolvedConfig;

        return CacheStatConfig();
    }

    private CreatureEnemyStatConfigSO CacheStatConfig()
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
        ResetTrail(
            _cachedTransform != null ? _cachedTransform.position : transform.position,
            Time.time);
    }

    private void ResetTrail(Vector3 position, float now)
    {
        SetLastBodyPosition(position, now);
        _hasTrailPoint = false;

        float stampInterval = ResolveStatConfig()?.VirusTrailStampInterval ?? 0f;

        _nextStampTime = stampInterval > 0f
            ? now + Random.Range(0f, stampInterval)
            : now;
    }

    private void SetLastBodyPosition(Vector3 position, float time)
    {
        _lastBodyPosition = position;
        _lastPaintTime = time;
        _hasBodyPosition = true;
    }

    private void SetLastTrailState(Vector3 bodyPosition, Vector3 trailPoint, float time)
    {
        _lastBodyPosition = bodyPosition;
        _lastTrailPoint = trailPoint;
        _lastPaintTime = time;
        _hasBodyPosition = true;
        _hasTrailPoint = true;
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
