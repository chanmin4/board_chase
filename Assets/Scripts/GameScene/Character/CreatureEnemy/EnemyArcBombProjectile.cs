using System.Collections.Generic;
using UnityEngine;

public class EnemyArcBombProjectile : MonoBehaviour
{
    [Header("Impact Visual")]
    [SerializeField] private ParticleSystem _impactParticlePrefab;
    [SerializeField] private Vector3 _impactParticleOffset = Vector3.zero;

    private Vector3 _start;
    private Vector3 _target;
    private float _travelTime;
    private float _arcHeight;

    private float _damageRadius;
    private float _impactHealthDamage;
    private float _impactInfectionDamage;
    private LayerMask _damageTargetMask;
    private QueryTriggerInteraction _triggerInteraction;

    private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;
    private PaintChannel _paintChannel;
    private float _paintRadiusWorld;
    private int _paintPriority;
    private PoisonPuddleDamageConfigSO _poisonPuddleDamageConfig;

    private GameObject _source;
    private float _elapsed;
    private bool _initialized;
    private bool _exploded;

    private bool _disableCollidersDuringFlight;
    private bool _showImpactTelegraph;
    private float _impactTelegraphRadius;
    private AreaAttackTelegraphStyle _impactTelegraphStyle;
    private AreaAttackTelegraph _impactTelegraph;

    private Collider[] _flightColliders;

    private readonly Collider[] _hits = new Collider[32];
    private readonly HashSet<Damageable> _damaged = new();
    private readonly HashSet<PlayerInfection> _infected = new();

    private void Awake()
    {
        _flightColliders = GetComponentsInChildren<Collider>(true);
    }

    public void Init(
        Vector3 start,
        Vector3 target,
        float travelTime,
        float arcHeight,
        float damageRadius,
        float impactHealthDamage,
        float impactInfectionDamage,
        LayerMask damageTargetMask,
        QueryTriggerInteraction triggerInteraction,
        MaskRenderManagerEventChannelSO maskRenderManagerReadyChannel,
        PaintChannel paintChannel,
        float paintRadiusWorld,
        int paintPriority,
        PoisonPuddleDamageConfigSO poisonPuddleDamageConfig,
        GameObject source,
        bool disableCollidersDuringFlight,
        bool showImpactTelegraph,
        float impactTelegraphRadius,
        AreaAttackTelegraphStyle impactTelegraphStyle)
    {
        _start = start;
        _target = target;
        _travelTime = Mathf.Max(0.01f, travelTime);
        _arcHeight = Mathf.Max(0f, arcHeight);

        _damageRadius = Mathf.Max(0f, damageRadius);
        _impactHealthDamage = Mathf.Max(0f, impactHealthDamage);
        _impactInfectionDamage = Mathf.Max(0f, impactInfectionDamage);
        _damageTargetMask = damageTargetMask;
        _triggerInteraction = triggerInteraction;

        _maskRenderManagerReadyChannel = maskRenderManagerReadyChannel;
        _paintChannel = paintChannel;
        _paintRadiusWorld = Mathf.Max(0f, paintRadiusWorld);
        _paintPriority = paintPriority;
        _poisonPuddleDamageConfig = poisonPuddleDamageConfig;

        _source = source;
        _disableCollidersDuringFlight = disableCollidersDuringFlight;
        _showImpactTelegraph = showImpactTelegraph;
        _impactTelegraphRadius = Mathf.Max(0f, impactTelegraphRadius);
        _impactTelegraphStyle = impactTelegraphStyle.segments > 0
            ? impactTelegraphStyle
            : AreaAttackTelegraphStyle.Default;

        _elapsed = 0f;
        _initialized = true;
        _exploded = false;

        transform.position = _start;

        DisableFlightCollision();
        SpawnImpactTelegraph();
    }

    private void Update()
    {
        if (!_initialized)
        {
            Destroy(gameObject);
            return;
        }

        _elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(_elapsed / _travelTime);
        Vector3 position = Vector3.Lerp(_start, _target, t);
        position.y += Mathf.Sin(t * Mathf.PI) * _arcHeight;

        transform.position = position;

        if (t < 1f)
            return;

        Explode();
        Destroy(gameObject);
    }

    private void DisableFlightCollision()
    {
        if (!_disableCollidersDuringFlight)
            return;

        if (_flightColliders == null)
            _flightColliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < _flightColliders.Length; i++)
        {
            if (_flightColliders[i] != null)
                _flightColliders[i].enabled = false;
        }
    }

    private void SpawnImpactTelegraph()
    {
        if (!_showImpactTelegraph || _impactTelegraphRadius <= 0f)
            return;

        _impactTelegraph = AreaAttackTelegraph.SpawnCircle(
            _target,
            _impactTelegraphRadius,
            _travelTime,
            _impactTelegraphStyle,
            transform.parent);
    }

    private void Explode()
    {
        if (_exploded)
            return;

        _exploded = true;

        if (_impactTelegraph != null)
        {
            _impactTelegraph.CompleteAndDestroy();
            _impactTelegraph = null;
        }

        ApplyImpactDamage();
        StampPaint();
        SpawnImpactParticle();
    }

    private void ApplyImpactDamage()
    {
        if (_damageRadius <= 0f || _damageTargetMask.value == 0)
            return;

        _damaged.Clear();
        _infected.Clear();

        int count = Physics.OverlapSphereNonAlloc(
            _target,
            _damageRadius,
            _hits,
            _damageTargetMask,
            _triggerInteraction);

        Transform sourceRoot = _source != null ? _source.transform.root : null;

        for (int i = 0; i < count; i++)
        {
            Collider hit = _hits[i];

            if (hit == null)
                continue;

            if (sourceRoot != null && hit.transform.IsChildOf(sourceRoot))
                continue;

            Damageable damageable = hit.GetComponentInParent<Damageable>();

            if (damageable != null &&
                _damaged.Add(damageable) &&
                _impactHealthDamage > 0f &&
                damageable.CanReceiveDamage)
            {
                damageable.ReceiveAnAttack(_impactHealthDamage, _source);
            }

            PlayerInfection infection =
                hit.GetComponent<PlayerInfection>() ??
                hit.GetComponentInParent<PlayerInfection>();

            if (infection != null &&
                _infected.Add(infection) &&
                _impactInfectionDamage > 0f)
            {
                infection.AddInfection(_impactInfectionDamage);
            }
        }
    }

    private void StampPaint()
    {
        if (_paintRadiusWorld <= 0f)
            return;

        MaskRenderManager manager = _maskRenderManagerReadyChannel != null
            ? _maskRenderManagerReadyChannel.Current
            : null;

        if (manager == null)
            manager = FindAnyObjectByType<MaskRenderManager>();

        if (manager == null)
            return;

        if (_paintChannel == PaintChannel.PoisonPuddle)
        {
            manager.RequestCircle(
                _paintChannel,
                _target,
                _paintRadiusWorld,
                _paintPriority,
                _source != null ? _source : this,
                _poisonPuddleDamageConfig);
        }
        else
        {
            manager.RequestCircle(
                _paintChannel,
                _target,
                _paintRadiusWorld,
                _paintPriority,
                _source != null ? _source : this);
        }
    }

    private void SpawnImpactParticle()
    {
        if (_impactParticlePrefab == null)
            return;

        ParticleSystem particle = Instantiate(
            _impactParticlePrefab,
            _target + _impactParticleOffset,
            Quaternion.identity,
            transform.parent);

        particle.Play(true);
    }

    private void OnDestroy()
    {
        if (!_exploded && _impactTelegraph != null)
            _impactTelegraph.CompleteAndDestroy();
    }
}