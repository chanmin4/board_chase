/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Roller > Effect
 * Primary responsibility: Coordinates the RollerVirusEffectController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class RollerVirusEffectController : EnemyEffectController
{
    [Header("Roller Virus Particles")]
    [SerializeField] private ParticleSystem _RollingParticles = default;
    // Purpose: Completes initialization after scene dependencies are ready.
    protected override void Start()
    {
        base.Start();

        StopIfAssigned(_RollingParticles);
    }

    // Purpose: Plays the requested rolling particles action or effect.
    public void PlayRollingParticles()
    {
        PlayIfAssigned(_RollingParticles);
    }

    // Purpose: Stops the active rolling particles behavior.
    public void StopRollingParticles()
    {
        StopIfAssigned(_RollingParticles);
    }
}
