/*
 * Role hierarchy: Gameplay > Entities > Creature > Effect
 * Primary responsibility: Coordinates the EnemyEffectController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class EnemyEffectController : EntityEffectController
{
    [Header("Common Enemy Particles")]
    [SerializeField] private ParticleSystem _spawnParticles = default;
    [SerializeField] private ParticleSystem _moveParticles = default;
    [SerializeField] private ParticleSystem _getHitParticles = default;
    [SerializeField] private ParticleSystem _deathParticles = default;

    // Purpose: Completes initialization after scene dependencies are ready.
    protected virtual void Start()
    {
        StopIfAssigned(_spawnParticles);
        StopIfAssigned(_moveParticles);
        StopIfAssigned(_getHitParticles);
        StopIfAssigned(_deathParticles);
    }

    // Purpose: Plays the requested spawn particles action or effect.
    public virtual void PlaySpawnParticles() => PlayIfAssigned(_spawnParticles);
    // Purpose: Plays the requested move particles action or effect.
    public virtual void PlayMoveParticles() => PlayIfAssigned(_moveParticles);
    // Purpose: Stops the active move particles behavior.
    public virtual void StopMoveParticles() => StopIfAssigned(_moveParticles);
    // Purpose: Plays the requested get hit particles action or effect.
    public virtual void PlayGetHitParticles() => PlayIfAssigned(_getHitParticles);
    // Purpose: Plays the requested death particles action or effect.
    public virtual void PlayDeathParticles() => PlayIfAssigned(_deathParticles);

}
