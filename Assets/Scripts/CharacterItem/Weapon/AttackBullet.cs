using UnityEngine;

[DisallowMultipleComponent]
public class AttackBullet : MonoBehaviour
{
    private Vector3 _direction;
    private GameObject _source;
    private float _speed;
    private float _castRadius;
    private float _maxDistance;
    private float _maxLifetime;
    private float _damage;
    private LayerMask _damageHitMask;
    private LayerMask _blockHitMask;
    private QueryTriggerInteraction _triggerInteraction;
    private float _travelledDistance;
    private float _lifeTime;
    private bool _initialized;

    public void Init(
        Vector3 direction,
        float maxDistance,
        float speed,
        float castRadius,
        float maxLifetime,
        float damage,
        LayerMask damageHitMask,
        LayerMask blockHitMask,
        QueryTriggerInteraction triggerInteraction,
        GameObject source)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        _direction = direction.normalized;
        _maxDistance = Mathf.Max(0.01f, maxDistance);
        _speed = Mathf.Max(0.01f, speed);
        _castRadius = Mathf.Max(0.001f, castRadius);
        _maxLifetime = Mathf.Max(0.01f, maxLifetime);
        _damage = Mathf.Max(0f, damage);
        _damageHitMask = damageHitMask;
        _blockHitMask = blockHitMask;
        _triggerInteraction = triggerInteraction;
        _source = source;
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
        if (remainingDistance <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float travelDistance = Mathf.Min(_speed * Time.deltaTime, remainingDistance);
        if (travelDistance <= 0f)
            return;

        int hitMask = _damageHitMask.value | _blockHitMask.value;
        if (hitMask != 0 && Physics.SphereCast(
            transform.position,
            _castRadius,
            _direction,
            out RaycastHit hit,
            travelDistance,
            hitMask,
            _triggerInteraction))
        {
            transform.position = hit.point;
            TryApplyDamage(hit);
            Destroy(gameObject);
            return;
        }

        transform.position += _direction * travelDistance;
        _travelledDistance += travelDistance;
    }

    private void TryApplyDamage(RaycastHit hit)
    {
        if (_damage <= 0f || !IsLayerInMask(hit.collider.gameObject.layer, _damageHitMask))
            return;

        IInkDamageable inkDamageable = hit.collider.GetComponentInParent<IInkDamageable>();
        if (inkDamageable != null)
        {
            inkDamageable.ApplyInkDamage(_damage, hit.point, _source != null ? _source : gameObject);
            return;
        }

        Damageable damageable = hit.collider.GetComponentInParent<Damageable>();
        if (damageable != null)
            damageable.ReceiveAnAttack(_damage);
    }

    private static bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
