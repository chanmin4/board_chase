using UnityEngine;

public class ChaserVirusEffectController : EnemyEffectController
{
    [Header("Chaser Virus Particles")]
    [SerializeField] private ParticleSystem _selfDestructExplosionParticles = default;
    [SerializeField] private ParticleSystem _selfDestructChargeParticles = default;

    protected override void Start()
    {
        base.Start();

        StopIfAssigned(_selfDestructExplosionParticles);
        StopIfAssigned(_selfDestructChargeParticles);
    }

    public void PlaySelfDestructChargeParticles()
    {
        PlayIfAssigned(_selfDestructChargeParticles);
    }

    public void StopSelfDestructChargeParticles()
    {
        StopIfAssigned(_selfDestructChargeParticles);
    }

    public void PlaySelfDestructExplosionParticles()
    {
        PlayIfAssigned(_selfDestructExplosionParticles);
    }
}
