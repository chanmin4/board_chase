using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectile : MonoBehaviour
{
    private enum HitKind
    {
        None,
        Impact,
        Damage
    }

    private Vector3 _direction;
    private GameObject _source;

    private float _speed;
    private float _healthDamage;
    private float _infectionDamage;
    private float _castRadius;
    private float _maxLifetime;
    private float _lifeTime;

    private LayerMask _damageTargetMask;
    private LayerMask _impactMask;
    private QueryTriggerInteraction _triggerInteraction;

    private MaskRenderManager _maskRenderManager;
    private PaintChannel _paintChannel;
    private float _paintRadiusWorld;
    private int _paintPriority;

    private bool _initialized;

    public void Init(
        Vector3 direction,
        float speed,
        float healthDamage,
        float infectionDamage,
        float castRadius,
        float maxLifetime,
        LayerMask damageTargetMask,
        LayerMask impactMask,
        QueryTriggerInteraction triggerInteraction,
        MaskRenderManager maskRenderManager,
        PaintChannel paintChannel,
        float paintRadiusWorld,
        int paintPriority,
        GameObject source)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        _direction = direction.normalized;
        _speed = Mathf.Max(0.01f, speed);
        _healthDamage = Mathf.Max(0f, healthDamage);
        _infectionDamage = Mathf.Max(0f, infectionDamage);
        _castRadius = Mathf.Max(0.001f, castRadius);
        _maxLifetime = Mathf.Max(0.01f, maxLifetime);

        _damageTargetMask = damageTargetMask;
        _impactMask = impactMask;
        _triggerInteraction = triggerInteraction;

        _maskRenderManager = maskRenderManager;
        _paintChannel = paintChannel;
        _paintRadiusWorld = Mathf.Max(0.001f, paintRadiusWorld);
        _paintPriority = paintPriority;

        _source = source;
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

        float travelDistance = _speed * Time.deltaTime;
        if (travelDistance <= 0f)
            return;

        if (TryGetClosestHit(travelDistance, out RaycastHit hit, out HitKind hitKind))
        {
            transform.position = hit.point;

            if (hitKind == HitKind.Damage)
                ApplyDamage(hit);

            StampVirus(hit.point);

            Debug.Log(
                $"[EnemyProjectile] Hit {hit.collider.name}, " +
                $"layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}, " +
                $"hitKind={hitKind}, distance={hit.distance}");

            Destroy(gameObject);
            return;
        }

        transform.position += _direction * travelDistance;
    }

    private bool TryGetClosestHit(
        float travelDistance,
        out RaycastHit closestHit,
        out HitKind hitKind)
    {
        closestHit = default;
        hitKind = HitKind.None;

        bool hasHit = false;
        float closestDistance = float.PositiveInfinity;

        TryScanMask(
            _impactMask,
            HitKind.Impact,
            travelDistance,
            ref hasHit,
            ref closestDistance,
            ref closestHit,
            ref hitKind);

        TryScanMask(
            _damageTargetMask,
            HitKind.Damage,
            travelDistance,
            ref hasHit,
            ref closestDistance,
            ref closestHit,
            ref hitKind);

        return hasHit;
    }

    private void TryScanMask(
        LayerMask mask,
        HitKind candidateKind,
        float travelDistance,
        ref bool hasHit,
        ref float closestDistance,
        ref RaycastHit closestHit,
        ref HitKind hitKind)
    {
        if (mask.value == 0)
        {
            Debug.Log($"[EnemyProjectile][Scan:{candidateKind}] mask is 0");
            return;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            _castRadius,
            _direction,
            travelDistance,
            mask,
            _triggerInteraction);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider collider = hit.collider;

            if (collider == null)
                continue;
                
            Debug.Log(
                $"[EnemyProjectile][Candidate:{candidateKind}] " +
                $"name={collider.name}, " +
                $"root={collider.transform.root.name}, " +
                $"layer={LayerMask.LayerToName(collider.gameObject.layer)}, " +
                $"distance={hit.distance:F3}, " +
                $"point={hit.point}, " +
                $"isTrigger={collider.isTrigger}");
                if (_source != null && collider.transform.IsChildOf(_source.transform))
            {  
                Debug.Log($"[EnemyProjectile][Skip] source child: {collider.name}");
                continue;
            }   
            bool isSameDistance = hit.distance <= closestDistance + 0.001f;
            bool damagePriority = candidateKind == HitKind.Damage && isSameDistance;

            bool shouldReplace =
                !hasHit ||
                hit.distance < closestDistance ||
                damagePriority;

            if (!shouldReplace)
                continue;

            hasHit = true;
            closestDistance = hit.distance;
            closestHit = hit;
            hitKind = candidateKind;
        }
    }

    private void ApplyDamage(RaycastHit hit)
    {
        if (hit.collider == null)
            return;

        ResolveDamageTargets(
            hit.collider,
            out Damageable damageable,
            out PlayerInfection playerInfection);

        if (damageable != null && damageable.CanReceiveDamage && _healthDamage > 0f)
            damageable.ReceiveAnAttack(_healthDamage, _source);

        if (playerInfection != null && _infectionDamage > 0f)
            playerInfection.AddInfection(_infectionDamage);
    }

    private void ResolveDamageTargets(
        Collider collider,
        out Damageable damageable,
        out PlayerInfection playerInfection)
    {
        damageable = null;
        playerInfection = null;

        EnemyProjectileHurtbox hurtbox =
            collider.GetComponent<EnemyProjectileHurtbox>() ??
            collider.GetComponentInParent<EnemyProjectileHurtbox>();

        if (hurtbox != null && hurtbox.AcceptsCollider(collider))
            hurtbox.TryGetDamageable(out damageable);

        VSplatter_Character player =
            collider.GetComponent<VSplatter_Character>() ??
            collider.GetComponentInParent<VSplatter_Character>();

        if (player != null)
        {
            if (damageable == null)
                damageable = player.GetComponent<Damageable>() ?? player.GetComponentInParent<Damageable>();

            playerInfection = player.GetComponent<PlayerInfection>() ?? player.GetComponentInParent<PlayerInfection>();
            return;
        }

        if (damageable == null)
            damageable = collider.GetComponent<Damageable>() ?? collider.GetComponentInParent<Damageable>();

        playerInfection =
            collider.GetComponent<PlayerInfection>() ??
            collider.GetComponentInParent<PlayerInfection>();
    }

    private void StampVirus(Vector3 worldPoint)
    {
        if (_paintRadiusWorld <= 0f)
            return;

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_maskRenderManager == null)
            return;

        _maskRenderManager.RequestCircle(
            _paintChannel,
            worldPoint,
            _paintRadiusWorld,
            _paintPriority,
            _source != null ? _source : this);
    }
}
