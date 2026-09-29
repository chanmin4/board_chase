/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Effects
 * Primary responsibility: Coordinates the EnemyShooterEffectController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class EnemyShooterEffectController : ShooterEffectController
{
    [Header("Enemy Shooter Spawn")]
    [SerializeField] private ParticleSystem _spawnParticles;

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();
        StopIfAssigned(_spawnParticles);
    }

    // Purpose: Plays the requested spawn particles action or effect.
    public virtual void PlaySpawnParticles() => PlayIfAssigned(_spawnParticles);
}
