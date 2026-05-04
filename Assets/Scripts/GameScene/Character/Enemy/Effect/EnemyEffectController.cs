using UnityEngine;

public class EnemyEffectController : MonoBehaviour
{
    [Header("Common Enemy Particles")]
    [SerializeField] private ParticleSystem _spawnParticles = default;
    [SerializeField] private ParticleSystem _moveParticles = default;
    [SerializeField] private ParticleSystem _getHitParticles = default;
    [SerializeField] private ParticleSystem _deathParticles = default;

    protected virtual void Start()
    {
        StopIfAssigned(_spawnParticles);
        StopIfAssigned(_moveParticles);
        StopIfAssigned(_getHitParticles);
        StopIfAssigned(_deathParticles);
    }

    public virtual void PlaySpawnParticles() => PlayIfAssigned(_spawnParticles);
    public virtual void PlayMoveParticles() => PlayIfAssigned(_moveParticles);
    public virtual void StopMoveParticles() => StopIfAssigned(_moveParticles);
    public virtual void PlayGetHitParticles() => PlayIfAssigned(_getHitParticles);
    public virtual void PlayDeathParticles() => PlayIfAssigned(_deathParticles);

    protected void PlayIfAssigned(ParticleSystem particles)
    {
        if (!CanUseParticles(particles))
            return;

        particles.Play(true);
    }

    protected void StopIfAssigned(ParticleSystem particles)
    {
        if (!CanUseParticles(particles))
            return;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private bool CanUseParticles(ParticleSystem particles)
    {
        if (!isActiveAndEnabled)
            return false;

        if (particles == null)
            return false;

        if (!particles.gameObject.activeInHierarchy)
            return false;

        return true;
    }
}
