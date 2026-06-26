using UnityEngine;

public class RollerVirusEffectController : EnemyEffectController
{
    [Header("Roller Virus Particles")]
    [SerializeField] private ParticleSystem _RollingParticles = default;
    protected override void Start()
    {
        base.Start();

        StopIfAssigned(_RollingParticles);
    }

    public void PlayRollingParticles()
    {
        PlayIfAssigned(_RollingParticles);
    }

    public void StopRollingParticles()
    {
        StopIfAssigned(_RollingParticles);
    }
}
