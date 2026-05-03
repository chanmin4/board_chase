using UnityEngine;

[DisallowMultipleComponent]
public class AttackBullet : MonoBehaviour
{
    [SerializeField] private bool _debugHit = true;
    [SerializeField] private float _debugDrawDuration = 1f;
    private Vector3 _direction;
    private Vector3 _visualDirection;
    private Vector3 _gameplayPosition;
    private Vector3 _visualStartPosition;
    private GameObject _source;
    private float _speed;
    private float _castRadius;
    private float _maxDistance;
    private float _maxLifetime;
    private float _damage;
    private LayerMask _damageTargetMask;
    private LayerMask _impactMask;
    private QueryTriggerInteraction _triggerInteraction;
    private float _travelledDistance;
    private float _lifeTime;
    private bool _initialized;
    

    public void Init(
        Vector3 gameplayStartPosition,
        Vector3 direction,
        Vector3 visualDirection,
        float maxDistance,
        float speed,
        float castRadius,
        float maxLifetime,
        float damage,
        LayerMask damageTargetMask,
        LayerMask impactMask,
        QueryTriggerInteraction triggerInteraction,
        GameObject source)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = direction;

        _direction = direction.normalized;
        _visualDirection = visualDirection.normalized;
        _gameplayPosition = gameplayStartPosition;
        _visualStartPosition = transform.position;
        _maxDistance = Mathf.Max(0.01f, maxDistance);
        _speed = Mathf.Max(0.01f, speed);
        _castRadius = Mathf.Max(0.001f, castRadius);
        _maxLifetime = Mathf.Max(0.01f, maxLifetime);
        _damage = Mathf.Max(0f, damage);
        _damageTargetMask = damageTargetMask;
        _impactMask = impactMask;
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

        if (TryGetClosestHit(travelDistance, out RaycastHit hit, out bool isDamageHit))
        {
            float travelledToHit = _travelledDistance + hit.distance;
            _gameplayPosition += _direction * hit.distance;
            transform.position = _visualStartPosition + _visualDirection * travelledToHit;
            _travelledDistance = travelledToHit;

            if (isDamageHit)
                TryApplyDamage(hit);

            Destroy(gameObject);
            return;
        }

        _gameplayPosition += _direction * travelDistance;
        transform.position = _visualStartPosition + _visualDirection * (_travelledDistance + travelDistance);
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
            _gameplayPosition,
            _castRadius,
            _direction,
            travelDistance,
            _impactMask,
            _triggerInteraction);

        if (_debugHit)
        {
          //  Debug.Log($"[AttackBullet][Impact] rawHits={hits.Length}, mask={_impactMask.value}, from={transform.position}, dir={_direction}, dist={travelDistance}");
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (c == null)
                    continue;

        //        Debug.Log($"[AttackBullet][Impact] hit[{i}] name={c.name}, layer={c.gameObject.layer}, trigger={c.isTrigger}, dist={hits[i].distance}");
            }
        }
        return TrySelectClosestHit(hits, false, out closestHit);
    }

    private bool TryGetClosestDamageHit(float travelDistance, out RaycastHit closestHit)
    {
        closestHit = default;

        if (_damageTargetMask.value == 0)
            return false;

        RaycastHit[] hits;

            hits = Physics.SphereCastAll(
                _gameplayPosition,
                _castRadius,
                _direction,
                travelDistance,
                _damageTargetMask,
                _triggerInteraction);
        if (_debugHit)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (c == null)
                    continue;

                bool hasProjectileHurtbox = c.GetComponent<EnemyProjectileHurtbox>() != null;
                bool hasDamageableInParent = c.GetComponentInParent<Damageable>() != null;

                Debug.Log(
                    $"[AttackBullet][Damage] hit[{i}] name={c.name}, layer={c.gameObject.layer}, trigger={c.isTrigger}, dist={hits[i].distance}, " +
                    $"hasProjectileHurtbox={hasProjectileHurtbox}, hasDamageableInParent={hasDamageableInParent}");
            }
        }

        return TrySelectClosestHit(hits, true, out closestHit);
    }

    private bool TrySelectClosestHit(RaycastHit[] hits, bool requireProjectileHurtbox, out RaycastHit closestHit)
    {
        closestHit = default;

        if (hits == null || hits.Length == 0)
            return false;

        bool found = false;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider collider = hit.collider;

            if (collider == null)
                continue;

            if (_source != null && collider.transform.IsChildOf(_source.transform))
            {
                if (_debugHit)
                    Debug.Log($"[AttackBullet][Select] skip self: {collider.name}");
                continue;
            }

            if (requireProjectileHurtbox && collider.GetComponent<EnemyProjectileHurtbox>() == null)
            {
                if (_debugHit)
                    Debug.Log($"[AttackBullet][Select] skip no hurtbox: {collider.name}");
                continue;
            }

            if (_debugHit)
                Debug.Log($"[AttackBullet][Select] candidate ok: {collider.name}, dist={hit.distance}");

            if (!found || hit.distance < closestDistance)
            {
                found = true;
                closestDistance = hit.distance;
                closestHit = hit;
            }
        }

        if (_debugHit)
        {
            Debug.Log(found
                ? $"[AttackBullet][Select] selected={closestHit.collider.name}, dist={closestDistance}"
                : $"[AttackBullet][Select] no valid hit selected");
        }

        return found;
    }

    private void TryApplyDamage(RaycastHit hit)
    {
        if (_damage <= 0f || hit.collider == null)
        {
            if (_debugHit)
                Debug.Log($"[AttackBullet][Apply] blocked: invalid damage({_damage}) or collider null");
            return;
        }

        EnemyProjectileHurtbox hurtbox = hit.collider.GetComponent<EnemyProjectileHurtbox>();
        if (hurtbox == null)
        {
            if (_debugHit)
                Debug.Log($"[AttackBullet][Apply] blocked: no EnemyProjectileHurtbox on {hit.collider.name}");
            return;
        }

        if (!hurtbox.TryGetDamageable(out Damageable damageable))
        {
            if (_debugHit)
                Debug.Log($"[AttackBullet][Apply] blocked: hurtbox exists but no Damageable parent on {hit.collider.name}");
            return;
        }

        if (!damageable.CanReceiveDamage)
        {
            if (_debugHit)
                Debug.Log($"[AttackBullet][Apply] blocked: CanReceiveDamage=false on {damageable.name}");
            return;
        }

        if (_debugHit)
            Debug.Log($"[AttackBullet][Apply] success: target={damageable.name}, collider={hit.collider.name}, damage={_damage}");

        damageable.ReceiveAnAttack(_damage, _source);
    }
}
