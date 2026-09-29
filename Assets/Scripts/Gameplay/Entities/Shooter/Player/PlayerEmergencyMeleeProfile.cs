using UnityEngine;

/// <summary>Reuses the existing swing shape/audio/animation; snapshots player damage at attack acceptance.</summary>
public sealed class PlayerEmergencyMeleeProfile : IMeleeAttackProfile
{
    private readonly IMeleeAttackProfile _source;
    public PlayerEmergencyMeleeProfile(IMeleeAttackProfile source, float damage)
    {
        _source = source;
        Damage = Mathf.Max(0f, damage);
    }

    public static bool IsAvailable(WeaponSO weapon, bool noFireableAmmunition)
        => weapon == null || weapon is RangedWeaponSO && noFireableAmmunition;

    public string ProfileName => "Player Emergency Melee";
    public float Damage { get; }
    public int PenetrationClass => 0;
    public float ArmorHealthDurabilityLossMultiplier => 0f;
    public float Range => _source.Range;
    public float ArcAngle => _source.ArcAngle;
    public float HitRadius => _source.HitRadius;
    public float CooldownSeconds => _source.CooldownSeconds;
    public float HitDelaySeconds => _source.HitDelaySeconds;
    public float ActiveHitSeconds => _source.ActiveHitSeconds;
    public QueryTriggerInteraction TriggerInteraction => _source.TriggerInteraction;
    public AudioCueSO SwingAudioCue => _source.SwingAudioCue;
    public ParticleSystem SwingParticlePrefab => _source.SwingParticlePrefab;
    public ParticleSystem SwingTrailParticlePrefab => _source.SwingTrailParticlePrefab;
    public LayerMask ResolveDamageTargetMask(ShooterFaction faction) => _source.ResolveDamageTargetMask(faction);
}
