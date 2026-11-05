using UnityEngine;

/// <summary>WIP: 이전 플레이어 주먹/빈총 휘두르기 프로필입니다. 현행 플레이어 전투에서는 참조하지 않습니다.</summary>
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
    public float PenetrationClass => 0;
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
