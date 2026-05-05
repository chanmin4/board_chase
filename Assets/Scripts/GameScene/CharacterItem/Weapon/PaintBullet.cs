using UnityEngine;

[DisallowMultipleComponent]
public class PaintBullet : MonoBehaviour
{
    private MaskRenderManager _maskRenderManager;
    private Vector3 _direction;
    private Vector3 _gameplayPosition;
    private Vector3 _visualStartPosition;
    private Vector3 _paintTarget;
    private Vector3 _visualTarget;
    private object _sender;
    private float _speed;
    private float _castRadius;
    private float _maxLifetime;
    private float _paintRadiusWorld;
    private int _paintPriority;
    private LayerMask _impactMask;
    private QueryTriggerInteraction _triggerInteraction;
    private MaskRenderManager.PaintChannel _paintChannel;
    private float _maxDistance;
    private float _travelledDistance;
    private float _lifeTime;
    private bool _initialized;

    public void Init(
        Vector3 gameplayStartPosition,
        Vector3 gameplayDirection,
        Vector3 visualStartPosition,
        Vector3 targetWorld,
        float speed,
        float castRadius,
        float maxLifetime,
        LayerMask impactMask,
        QueryTriggerInteraction triggerInteraction,
        MaskRenderManager maskRenderManager,
        MaskRenderManager.PaintChannel paintChannel,
        float paintRadiusWorld,
        int paintPriority,
        object sender)
    {
        _paintTarget = targetWorld;
        _visualTarget = targetWorld;
        _gameplayPosition = gameplayStartPosition;
        _visualStartPosition = visualStartPosition;
        _visualTarget.y = _visualStartPosition.y;
        gameplayDirection.y = 0f;
        if (gameplayDirection.sqrMagnitude < 0.0001f)
            gameplayDirection = transform.forward;

        gameplayDirection.y = 0f;
        if (gameplayDirection.sqrMagnitude < 0.0001f)
            gameplayDirection = Vector3.forward;

        _direction = gameplayDirection.normalized;
        _maxDistance = Mathf.Max(0.01f, Vector3.Distance(
            new Vector3(_gameplayPosition.x, 0f, _gameplayPosition.z),
            new Vector3(_paintTarget.x, 0f, _paintTarget.z)));

        _speed = Mathf.Max(0.01f, speed);
        _castRadius = Mathf.Max(0.001f, castRadius);
        _maxLifetime = Mathf.Max(0.01f, maxLifetime);
        _impactMask = impactMask;
        _triggerInteraction = triggerInteraction;
        _maskRenderManager = maskRenderManager != null ? maskRenderManager : FindAnyObjectByType<MaskRenderManager>();
        _paintChannel = paintChannel;
        _paintRadiusWorld = Mathf.Max(0.001f, paintRadiusWorld);
        _paintPriority = paintPriority;
        _sender = sender;
        _travelledDistance = 0f;
        _lifeTime = 0f;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized)
        {
            Destroy(gameObject);
            return;
        }

        _lifeTime += Time.deltaTime;
        if (_lifeTime >= _maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        float remainingDistance = _maxDistance - _travelledDistance;
        if (remainingDistance <= 0.001f)
        {
            transform.position = _visualTarget;
            Stamp(_paintTarget);
            Destroy(gameObject);
            return;
        }

        float travelDistance = Mathf.Min(_speed * Time.deltaTime, remainingDistance);
        if (travelDistance <= 0f)
            return;

        if (_impactMask.value != 0 && Physics.SphereCast(
            _gameplayPosition,
            _castRadius,
            _direction,
            out RaycastHit hit,
            travelDistance,
            _impactMask,
            _triggerInteraction))
        {
            float progress = _maxDistance > 0.001f
                ? Mathf.Clamp01((_travelledDistance + hit.distance) / _maxDistance)
                : 1f;

            transform.position = Vector3.Lerp(_visualStartPosition, _visualTarget, progress);
            Stamp(hit.point);
            Destroy(gameObject);
            return;
        }

        _gameplayPosition += _direction * travelDistance;
        _travelledDistance += travelDistance;
        float visualProgress = _maxDistance > 0.001f
            ? Mathf.Clamp01(_travelledDistance / _maxDistance)
            : 1f;
        transform.position = Vector3.Lerp(_visualStartPosition, _visualTarget, visualProgress);
    }

    private void Stamp(Vector3 worldPoint)
    {
        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_maskRenderManager == null)
            return;

        _maskRenderManager.RequestCircle(
            _paintChannel,
            worldPoint,
            _paintRadiusWorld,
            _paintPriority,
            _sender ?? this);
    }
}
