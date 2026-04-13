using UnityEngine;

[DisallowMultipleComponent]
public class PaintBullet : MonoBehaviour
{
    private MaskRenderManager _maskRenderManager;
    private Vector3 _direction;
    private Vector3 _flightTarget;
    private Vector3 _paintTarget;
    private object _sender;
    private float _speed;
    private float _castRadius;
    private float _maxLifetime;
    private float _paintRadiusWorld;
    private int _paintPriority;
    private LayerMask _hitMask;
    private QueryTriggerInteraction _triggerInteraction;
    private MaskRenderManager.PaintChannel _paintChannel;
    private float _maxDistance;
    private float _travelledDistance;
    private float _lifeTime;
    private bool _initialized;

    public void Init(
        Vector3 targetWorld,
        float speed,
        float castRadius,
        float maxLifetime,
        LayerMask hitMask,
        QueryTriggerInteraction triggerInteraction,
        MaskRenderManager maskRenderManager,
        MaskRenderManager.PaintChannel paintChannel,
        float paintRadiusWorld,
        int paintPriority,
        object sender)
    {
        _paintTarget = targetWorld;
        _flightTarget = targetWorld;
        _flightTarget.y = transform.position.y;

        Vector3 direction = _flightTarget - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        _direction = direction.normalized;
        _maxDistance = Mathf.Max(0f, Vector3.Distance(Flatten(transform.position), Flatten(_flightTarget)));
        _speed = Mathf.Max(0.01f, speed);
        _castRadius = Mathf.Max(0.001f, castRadius);
        _maxLifetime = Mathf.Max(0.01f, maxLifetime);
        _hitMask = hitMask;
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
            Stamp(_paintTarget);
            Destroy(gameObject);
            return;
        }

        float travelDistance = Mathf.Min(_speed * Time.deltaTime, remainingDistance);
        if (travelDistance <= 0f)
            return;

        if (_hitMask.value != 0 && Physics.SphereCast(
            transform.position,
            _castRadius,
            _direction,
            out RaycastHit hit,
            travelDistance,
            _hitMask,
            _triggerInteraction))
        {
            transform.position = hit.point;
            Stamp(hit.point);
            Destroy(gameObject);
            return;
        }

        transform.position += _direction * travelDistance;
        _travelledDistance += travelDistance;

        if (_travelledDistance >= _maxDistance - 0.001f)
        {
            transform.position = _flightTarget;
            Stamp(_paintTarget);
            Destroy(gameObject);
        }
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

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
