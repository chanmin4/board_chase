using UnityEngine;

public class ShooterEffectController : EntityEffectController
{
    [Header("Shooter Particles")]
    [SerializeField] private ParticleSystem _footTrailParticles;
    [SerializeField] private ParticleSystem _getHitParticles;
    [SerializeField] private ParticleSystem _deathParticles;
    [SerializeField] private ParticleSystem _dashParticles;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;

    [Header("Listening To")]
    [SerializeField] private HitReceivedEventChannelSO _hitReceivedEvent;

    private bool _footTrailPlaying;

    protected virtual void Reset()
    {
        ResolveRefs();
    }

    protected virtual void Awake()
    {
        ResolveRefs();
        StopAllShooterParticles();
    }

    protected virtual void OnEnable()
    {
        ResolveRefs();

        if (_damageable != null)
            _damageable.OnDie += PlayDeathParticles;

        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised += HandleHitReceived;
    }

    protected virtual void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnDie -= PlayDeathParticles;

        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised -= HandleHitReceived;

        StopFootTrailParticles();
        StopDashParticles();
    }

    public virtual void PlayDashParticles()
    {
        PlayIfAssigned(_dashParticles);
    }

    public virtual void StopDashParticles()
    {
        StopIfAssigned(_dashParticles);
    }

    public virtual void PlayFootTrailParticles()
    {
        _footTrailPlaying = true;
        PlayIfAssigned(_footTrailParticles);
    }

    public virtual void StopFootTrailParticles()
    {
        _footTrailPlaying = false;
        StopIfAssigned(_footTrailParticles);
    }

    public void SetFootTrailActive(bool active)
    {
        if (active)
        {
            if (!_footTrailPlaying)
                PlayFootTrailParticles();

            return;
        }

        if (_footTrailPlaying)
            StopFootTrailParticles();
    }

    public virtual void PlayGetHitParticles()
    {
        PlayIfAssigned(_getHitParticles);
    }

    public virtual void PlayDeathParticles()
    {
        PlayIfAssigned(_deathParticles);
    }

    protected virtual void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ??
                          GetComponentInParent<Damageable>() ??
                          GetComponentInChildren<Damageable>(true);
    }

    protected virtual void StopAllShooterParticles()
    {
        StopIfAssigned(_footTrailParticles);
        StopIfAssigned(_getHitParticles);
        StopIfAssigned(_deathParticles);
        StopIfAssigned(_dashParticles);
        _footTrailPlaying = false;
    }

    private void HandleHitReceived(GameObject hitTarget)
    {
        if (hitTarget == null || _damageable == null)
            return;

        Damageable hitDamageable =
            hitTarget.GetComponent<Damageable>() ??
            hitTarget.GetComponentInParent<Damageable>() ??
            hitTarget.GetComponentInChildren<Damageable>(true);

        if (hitDamageable != _damageable)
            return;

        PlayGetHitParticles();
    }
}
