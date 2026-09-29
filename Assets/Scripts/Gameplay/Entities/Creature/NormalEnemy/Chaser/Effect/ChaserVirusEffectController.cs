/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Chaser > Effect
 * Primary responsibility: Coordinates the ChaserVirusEffectController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class ChaserVirusEffectController : EnemyEffectController
{
    [Header("Chaser Virus Particles")]
    [SerializeField] private ParticleSystem _selfDestructExplosionParticles = default;
    [SerializeField] private ParticleSystem _selfDestructChargeParticles = default;

    // Purpose: Completes initialization after scene dependencies are ready.
    protected override void Start()
    {
        base.Start();

        StopIfAssigned(_selfDestructExplosionParticles);
        StopIfAssigned(_selfDestructChargeParticles);
    }

    // Purpose: Plays the requested self destruct charge particles action or effect.
    public void PlaySelfDestructChargeParticles()
    {
        PlayIfAssigned(_selfDestructChargeParticles);
    }

    // Purpose: Stops the active self destruct charge particles behavior.
    public void StopSelfDestructChargeParticles()
    {
        StopIfAssigned(_selfDestructChargeParticles);
    }

    // Purpose: Plays the requested self destruct explosion particles action or effect.
    public void PlaySelfDestructExplosionParticles()
    {
        PlayIfAssigned(_selfDestructExplosionParticles);
    }
}
