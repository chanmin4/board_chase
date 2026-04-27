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
    private LayerMask _damageTargetMask;
    private LayerMask _impactMask;
    private QueryTriggerInteraction _triggerInteraction;
    private bool _useFlatDamageHit;
    private float _flatHitHalfHeight;
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
        LayerMask damageTargetMask,
        LayerMask impactMask,
        QueryTriggerInteraction triggerInteraction,
        bool useFlatDamageHit,
        float flatHitHalfHeight,
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
        _damageTargetMask = damageTargetMask;
        _impactMask = impactMask;
        _triggerInteraction = triggerInteraction;
        _useFlatDamageHit = useFlatDamageHit;
        _flatHitHalfHeight = Mathf.Max(_castRadius, flatHitHalfHeight);
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

        if (TryGetClosestHit(travelDistance, out RaycastHit hit, out bool isDamageHit))
        {
            transform.position = hit.point;

            if (isDamageHit)
                TryApplyDamage(hit);

            Destroy(gameObject);
            return;
        }

        transform.position += _direction * travelDistance;
        _travelledDistance += travelDistance;
    }

    private bool TryGetClosestHit(float travelDistance, out RaycastHit closestHit, out bool isDamageHit)
    {
        closestHit = default;
        isDamageHit = false;

        bool hasClosest = false;
        float closestDistance = float.PositiveInfinity;

        if (TryGetClosestImpactHit(travelDistance, out RaycastHit impactHit))
        {
            hasClosest = true;
            closestDistance = impactHit.distance;
            closestHit = impactHit;
            isDamageHit = false;
        }

        if (TryGetClosestDamageHit(travelDistance, out RaycastHit damageHit))
        {
            if (!hasClosest || damageHit.distance <= closestDistance)
            {
                hasClosest = true;
                closestDistance = damageHit.distance;
                closestHit = damageHit;
                isDamageHit = true;
            }
        }

        return hasClosest;
    }

    private bool TryGetClosestImpactHit(float travelDistance, out RaycastHit closestHit)
    {
        closestHit = default;

        if (_impactMask.value == 0)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            _castRadius,
            _direction,
            travelDistance,
            _impactMask,
            _triggerInteraction);

        return TrySelectClosestNonSelfHit(hits, out closestHit);
    }

    private bool TryGetClosestDamageHit(float travelDistance, out RaycastHit closestHit)
    {
        closestHit = default;

        if (_damageTargetMask.value == 0)
            return false;

        RaycastHit[] hits;

        if (_useFlatDamageHit)
        {
            GetFlatCapsulePoints(out Vector3 point1, out Vector3 point2);

            hits = Physics.CapsuleCastAll(
                point1,
                point2,
                _castRadius,
                _direction,
                travelDistance,
                _damageTargetMask,
                _triggerInteraction);
        }
        else
        {
            hits = Physics.SphereCastAll(
                transform.position,
                _castRadius,
                _direction,
                travelDistance,
                _damageTargetMask,
                _triggerInteraction);
        }

        return TrySelectClosestNonSelfHit(hits, out closestHit);
    }

    private bool TrySelectClosestNonSelfHit(RaycastHit[] hits, out RaycastHit closestHit)
    {
        closestHit = default;

        if (hits == null || hits.Length == 0)
            return false;

        bool found = false;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
                continue;

            if (_source != null && hit.collider.transform.IsChildOf(_source.transform))
                continue;

            if (!found || hit.distance < closestDistance)
            {
                found = true;
                closestDistance = hit.distance;
                closestHit = hit;
            }
        }

        return found;
    }

    private void GetFlatCapsulePoints(out Vector3 point1, out Vector3 point2)
    {
        float halfHeight = Mathf.Max(_castRadius, _flatHitHalfHeight);
        float pointOffset = Mathf.Max(0f, halfHeight - _castRadius);

        Vector3 up = Vector3.up * pointOffset;
        point1 = transform.position + up;
        point2 = transform.position - up;
    }

    private void TryApplyDamage(RaycastHit hit)
    {
        if (_damage <= 0f)
            return;

        Damageable damageable = hit.collider.GetComponentInParent<Damageable>();
        if (damageable == null)
            return;

        damageable.ReceiveAnAttack(_damage);
    }
}
