using UnityEngine;

public class EnemyVirusTrail : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;
    [SerializeField] private EnemyPollutionTrailSettingsSO _settings;

    [Header("Runtime")]
    [SerializeField] private bool _trailEnabled = true;

    private MaskRenderManager _maskRenderManager;
    private Vector3 _lastPosition;

    public void SetTrailEnabled(bool enabled)
    {
        _trailEnabled = enabled;

        if (enabled)
            ResetTrail();
    }

    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManager = _maskRenderManagerReadyChannel.Current;

        ResetTrail();
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;
    }

    private void LateUpdate()
    {
        if (!_trailEnabled || _settings == null)
            return;

        MaskRenderManager manager = ResolveMaskRenderManager();
        if (manager == null)
            return;

        Vector3 current = transform.position;

        Vector3 from = _lastPosition;
        Vector3 to = current;

        from.y = 0f;
        to.y = 0f;

        Vector3 delta = to - from;
        float moved = delta.magnitude;

        if (moved <= _settings.minSegmentDistance)
            return;

        if (moved > _settings.teleportResetDistance)
        {
            ResetTrail();
            return;
        }

        float speed = moved / Mathf.Max(Time.deltaTime, 0.0001f);
        if (speed < _settings.minMoveSpeed)
        {
            _lastPosition = current;
            return;
        }

        manager.RequestCapsuleTrail(
            MaskRenderManager.PaintChannel.Virus,
            _lastPosition,
            current,
            _settings.trailRadius,
            _settings.paintPriority,
            gameObject);

        _lastPosition = current;
    }

    private void ResetTrail()
    {
        _lastPosition = transform.position;
    }

    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    private MaskRenderManager ResolveMaskRenderManager()
    {
        if (_maskRenderManager != null)
            return _maskRenderManager;

        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManager = _maskRenderManagerReadyChannel.Current;

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        return _maskRenderManager;
    }
}
