using UnityEngine;

public class EntityEffectController : MonoBehaviour
{
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

    protected void StopAndClearIfAssigned(ParticleSystem particles)
    {
        if (!CanUseParticles(particles))
            return;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
