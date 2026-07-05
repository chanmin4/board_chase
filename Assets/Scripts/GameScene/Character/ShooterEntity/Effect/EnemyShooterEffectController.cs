using UnityEngine;

public class EnemyShooterEffectController : ShooterEffectController
{
    [Header("Enemy Shooter Spawn")]
    [SerializeField] private ParticleSystem _spawnParticles;

    protected override void Awake()
    {
        base.Awake();
        StopIfAssigned(_spawnParticles);
    }

    public virtual void PlaySpawnParticles() => PlayIfAssigned(_spawnParticles);
}
