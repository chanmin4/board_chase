/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Effects
 * Primary responsibility: Coordinates the EntityEffectController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class EntityEffectController : MonoBehaviour
{
    // Purpose: Plays the requested if assigned action or effect.
    protected void PlayIfAssigned(ParticleSystem particles)
    {
        if (!CanUseParticles(particles))
            return;

        particles.Play(true);
    }

    // Purpose: Stops the active if assigned behavior.
    protected void StopIfAssigned(ParticleSystem particles)
    {
        if (!CanUseParticles(particles))
            return;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // Purpose: Stops the active and clear if assigned behavior.
    protected void StopAndClearIfAssigned(ParticleSystem particles)
    {
        if (!CanUseParticles(particles))
            return;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // Purpose: Checks whether use particles is currently permitted.
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
