using UnityEngine;

[DisallowMultipleComponent]
public class AttackBullet : MonoBehaviour
{
    [SerializeField] private bool _debugHit;

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
    private float _vaccineMarkAmountOnHit;
    private float _virusMarkAmountOnHit;

    private LayerMask _damageTargetMask;
    private LayerMask _impactMask;
    private QueryTriggerInteraction _triggerInteraction;
    private ShootHitConfirmedEventChannelSO _shootHitConfirmedEvent;

    private float _travelledDistance;
    private float _lifeTime;
    private bool _initialized;
    private bool _completed;

    public void Init(
        Vector3 gameplayStartPosition,
        Vector3 direction,
        Vector3 visualDirection,
        float maxDistance,
        float speed,
        float castRadius,
        float maxLifetime,
        float damage,
        float vaccineMarkAmountOnHit,
        float virusMarkAmountOnHit,
        LayerMask damageTargetMask,
        LayerMask impactMask,
        QueryTriggerInteraction triggerInteraction,
        GameObject source,
        ShootHitConfirmedEventChannelSO shootHitConfirmedEvent)
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
        _vaccineMarkAmountOnHit = Mathf.Max(0f, vaccineMarkAmountOnHit);
        _virusMarkAmountOnHit = Mathf.Max(0f, virusMarkAmountOnHit);

        _damageTargetMask = damageTargetMask;
        _impactMask = impactMask;
        _triggerInteraction = triggerInteraction;
        _source = source;
        _shootHitConfirmedEvent = shootHitConfirmedEvent;

        _travelledDistance = 0f;
        _lifeTime = 0f;
        _initialized = true;
        _completed = false;
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
            Complete(_gameplayPosition);
            return;
        }

        float remainingDistance = _maxDistance - _travelledDistance;

        if (remainingDistance <= 0f)
        {
            Complete(_gameplayPosition);
            return;
        }

        float travelDistance = Mathf.Min(
            _speed * Time.deltaTime,
            remainingDistance);

        if (travelDistance <= 0f)
            return;

        if (TryGetClosestHit(
                travelDistance,
                out RaycastHit hit,
                out bool isDamageHit))
        {
            float travelledToHit = _travelledDistance + hit.distance;

            _gameplayPosition += _direction * hit.distance;
            transform.position =
                _visualStartPosition + _visualDirection * travelledToHit;

            _travelledDistance = travelledToHit;

            if (isDamageHit)
                TryApplyDamage(hit);

            Complete(hit.point);
            return;
        }

        _gameplayPosition += _direction * travelDistance;
        _travelledDistance += travelDistance;

        transform.position =
            _visualStartPosition +
            _visualDirection * _travelledDistance;

        if (_travelledDistance >= _maxDistance - 0.001f)
            Complete(_gameplayPosition);
    }

    private void Complete(Vector3 worldPoint)
    {
        if (_completed)
            return;

        _completed = true;

        OnCompleted(worldPoint);

        Destroy(gameObject);
    }

    protected virtual void OnCompleted(Vector3 worldPoint)
    {
    }

    private bool TryGetClosestHit(
        float travelDistance,
        out RaycastHit closestHit,
        out bool isDamageHit)
    {
        closestHit = default;
        isDamageHit = false;

        bool found = false;
        float closestDistance = float.PositiveInfinity;

        if (TryGetClosestImpactHit(travelDistance, out RaycastHit impactHit))
        {
            found = true;
            closestDistance = impactHit.distance;
            closestHit = impactHit;
        }

        if (TryGetClosestDamageHit(travelDistance, out RaycastHit damageHit))
        {
            if (!found || damageHit.distance <= closestDistance)
            {
                found = true;
                closestHit = damageHit;
                isDamageHit = true;
            }
        }

        return found;
    }

    private bool TryGetClosestImpactHit(
        float travelDistance,
        out RaycastHit closestHit)
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

        return TrySelectClosestHit(hits, false, out closestHit);
    }

    private bool TryGetClosestDamageHit(
        float travelDistance,
        out RaycastHit closestHit)
    {
        closestHit = default;

        if (_damageTargetMask.value == 0)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            _gameplayPosition,
            _castRadius,
            _direction,
            travelDistance,
            _damageTargetMask,
            _triggerInteraction);

        return TrySelectClosestHit(hits, true, out closestHit);
    }

    private bool TrySelectClosestHit(
        RaycastHit[] hits,
        bool requireDamageableTarget,
        out RaycastHit closestHit)
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

            if (_source != null &&
                collider.transform.IsChildOf(_source.transform))
            {
                continue;
            }

            if (requireDamageableTarget &&
                !TryResolveDamageableTarget(collider, out _))
            {
                continue;
            }

            if (!found || hit.distance < closestDistance)
            {
                found = true;
                closestDistance = hit.distance;
                closestHit = hit;
            }
        }

        return found;
    }

    private void TryApplyDamage(RaycastHit hit)
    {
        if (_damage <= 0f || hit.collider == null)
            return;

        if (!TryResolveDamageableTarget(hit.collider, out Damageable damageable))
            return;

        if (!damageable.CanReceiveDamage)
            return;

        if (_debugHit)
        {
            Debug.Log(
                $"[AttackBullet] target={damageable.name}, damage={_damage}",
                damageable);
        }

        damageable.ReceiveAnAttack(_damage, _source);
        TryApplyPaintMark(damageable);
        _shootHitConfirmedEvent?.RaiseEvent();
    }

    private void TryApplyPaintMark(Damageable damageable)
    {
        if (damageable == null ||
            (_vaccineMarkAmountOnHit <= 0f && _virusMarkAmountOnHit <= 0f))
        {
            return;
        }

        EntityPaintMarkController mark =
            damageable.GetComponent<EntityPaintMarkController>();

        if (mark == null)
            mark = damageable.GetComponentInParent<EntityPaintMarkController>();

        if (mark == null)
            mark = damageable.GetComponentInChildren<EntityPaintMarkController>(true);

        if (mark == null)
            return;

        if (_vaccineMarkAmountOnHit > 0f)
            mark.AddMark(PaintMarkFaction.Vaccine, _vaccineMarkAmountOnHit);

        if (_virusMarkAmountOnHit > 0f)
            mark.AddMark(PaintMarkFaction.Virus, _virusMarkAmountOnHit);
    }

    private bool TryGetProjectileHurtbox(
        Collider collider,
        out EnemyProjectileHurtbox hurtbox)
    {
        hurtbox = null;

        if (collider == null)
            return false;

        EnemyProjectileHurtbox direct =
            collider.GetComponent<EnemyProjectileHurtbox>();

        if (IsAcceptedHurtbox(direct, collider, out hurtbox))
            return true;

        EnemyProjectileHurtbox[] parentHurtboxes =
            collider.GetComponentsInParent<EnemyProjectileHurtbox>(true);

        for (int i = 0; i < parentHurtboxes.Length; i++)
        {
            if (IsAcceptedHurtbox(
                    parentHurtboxes[i],
                    collider,
                    out hurtbox))
            {
                return true;
            }
        }

        Damageable damageable = collider.GetComponentInParent<Damageable>();

        if (damageable == null)
            return false;

        EnemyProjectileHurtbox[] childHurtboxes =
            damageable.GetComponentsInChildren<EnemyProjectileHurtbox>(true);

        for (int i = 0; i < childHurtboxes.Length; i++)
        {
            if (IsAcceptedHurtbox(
                    childHurtboxes[i],
                    collider,
                    out hurtbox))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolveDamageableTarget(
        Collider collider,
        out Damageable damageable)
    {
        damageable = null;

        if (collider == null)
            return false;

        if (TryGetProjectileHurtbox(collider, out EnemyProjectileHurtbox hurtbox) &&
            hurtbox.TryGetDamageable(out damageable))
        {
            return damageable != null;
        }

        damageable = collider.GetComponent<Damageable>();

        if (damageable != null)
            return !HasConfiguredProjectileHurtbox(damageable);

        damageable = collider.GetComponentInParent<Damageable>();
        return damageable != null &&
               !HasConfiguredProjectileHurtbox(damageable);
    }

    private static bool HasConfiguredProjectileHurtbox(Damageable damageable)
    {
        return damageable != null &&
               damageable.GetComponentInChildren<EnemyProjectileHurtbox>(true) != null;
    }

    private static bool IsAcceptedHurtbox(
        EnemyProjectileHurtbox candidate,
        Collider collider,
        out EnemyProjectileHurtbox hurtbox)
    {
        hurtbox = null;

        if (candidate == null || !candidate.AcceptsCollider(collider))
            return false;

        hurtbox = candidate;
        return true;
    }
}
