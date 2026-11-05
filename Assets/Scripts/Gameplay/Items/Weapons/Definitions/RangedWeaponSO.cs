/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the RangedWeaponSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public sealed class RangedWeaponCombatSettings
{
    [InspectorName("최대 사거리"), Min(0.1f)] public float maxRange = 100f;
    [InspectorName("초당 발사 수"), Min(0.01f)] public float shotsPerSecond = 7.8f;
    [InspectorName("재장전 시간 (초)"), Min(0.01f)] public float reloadDurationSeconds = 2f;
    [InspectorName("조준 속도 · 높을수록 빠름"), Min(0.01f)] public float aimSpeed = 3f;
    [InspectorName("비조준 탄퍼짐 · 전체 각도"), Min(0f)] public float hipFireSpreadAngleDeg = 36f;
    [InspectorName("정조준 탄퍼짐 · 전체 각도"), Min(0f)] public float aimSpreadAngleDeg = 4f;
    [InspectorName("조준 중 이동 배율"), Range(0f, 1f)] public float aimingMoveSpeedMultiplier = 0.6f;
    [InspectorName("관통력 보정")] public float penetrationClassBonus;
    [InspectorName("총성 반경"), Min(0f)] public float gunshotSoundRadius = 150f;
    [InspectorName("발사 반동 거리"), Min(0f)] public float shotKickDistance = 0.35f;
    [InspectorName("발사 반동 누적 상한"), Min(0f)] public float maximumShotKickDistance = 2f;
    [InspectorName("발사 반동 초당 복구"), Min(0f)] public float shotKickRecoveryPerSecond = 2.5f;
}

/// <summary>
/// Firearm-specific weapon data. The script GUID intentionally matches the
/// former WeaponSO script so existing firearm assets migrate without losing
/// references or serialized firearm fields.
/// </summary>
[CreateAssetMenu(
    fileName = "RangedWeapon",
    menuName = "Game/Character Item/Weapon/Ranged Weapon")]
public class RangedWeaponSO : WeaponSO
{
    [Header("무기 기본 성능 · 플레이어 / 적 공통")]
    [SerializeField] private RangedWeaponCombatSettings _combat = new();
    public RangedWeaponCombatSettings Combat => _combat ??= new RangedWeaponCombatSettings();
    public float MaxRange => Mathf.Max(0.1f, Combat.maxRange);
    public float ShotsPerSecond => Mathf.Max(0.01f, Combat.shotsPerSecond);
    public float ReloadDurationSeconds => Mathf.Max(0.01f, Combat.reloadDurationSeconds);
    public float AimSpeed => Mathf.Max(0.01f, Combat.aimSpeed);
    public float HipFireSpreadAngleDeg => Mathf.Max(0f, Combat.hipFireSpreadAngleDeg);
    public float AimSpreadAngleDeg => Mathf.Max(0f, Combat.aimSpreadAngleDeg);
    public float AimingMoveSpeedMultiplier => Mathf.Clamp01(Combat.aimingMoveSpeedMultiplier);
    public float PenetrationClassBonus => Combat.penetrationClassBonus;
    public float GunshotSoundRadius => Mathf.Max(0f, Combat.gunshotSoundRadius);
    public float ShotKickDistance => Mathf.Max(0f, Combat.shotKickDistance);
    public float MaximumShotKickDistance => Mathf.Max(0f, Combat.maximumShotKickDistance);
    public float ShotKickRecoveryPerSecond => Mathf.Max(0f, Combat.shotKickRecoveryPerSecond);
    [Header("Firearm Type")]
    [Tooltip("Determines which bullet family this firearm accepts. Pistol/SMG=Light, Revolver/LMG=Heavy, AR=Medium, Sniper=Sniper, Shotgun=ShotgunShell, RocketLauncher=Rocket.")]
    [SerializeField] private RangedWeaponType _weaponType = RangedWeaponType.Pistol;

    [Header("Archetype Combat Coefficients")]
    [Tooltip("Weapon-family damage multiplier applied after Shooter, meta, passive, and item modifiers. For pellet weapons this is the total damage multiplier of one trigger pull and is divided across the spawned pellets.")]
    [SerializeField, Min(0f)] private float _damageMultiplier = 1f;
    [Header("정조준 헤드샷")]
    [SerializeField, Min(1f)] private float _headshotDamageMultiplier = 1.5f;
    public float HeadshotDamageMultiplier => Mathf.Max(1f, _headshotDamageMultiplier);
    // Legacy multipliers were folded into the weapon's absolute Combat values during migration.
    [SerializeField, HideInInspector, Min(0f)] private float _shotKickDistanceMultiplier = 1f;
    [SerializeField, HideInInspector, Min(0f)] private float _maximumKickDistanceMultiplier = 1f;
    [SerializeField, HideInInspector, Min(0f)] private float _aimKickRecoveryMultiplier = 1f;
    public float ShotKickDistanceMultiplier => Mathf.Max(0f, _shotKickDistanceMultiplier);
    public float MaximumKickDistanceMultiplier => Mathf.Max(0f, _maximumKickDistanceMultiplier);
    public float AimKickRecoveryMultiplier => Mathf.Max(0f, _aimKickRecoveryMultiplier);

    [Tooltip("Weapon-family fire-rate multiplier applied after the Shooter's base Shots Per Second and stat modifiers.")]
    [SerializeField, HideInInspector, Min(0.01f)] private float _shotsPerSecondMultiplier = 1f;

    [Header("Magazine")]
    [Tooltip("Rounds loaded by this firearm. Character, passive, armor and ammunition modifiers never change magazine capacity. Future parts must resolve capacity through the weapon.")]
    [SerializeField, Min(1)] private int _magazineSize = 6;

    [Header("Legacy Fire Audio Fallback")]
    [Tooltip("Used only when Audio Profile has no Fire Cue.")]
    [SerializeField] private AudioCueSO _fireAudioCue;

    [Tooltip("Used only when Audio Profile has no Empty Fire Cue.")]
    [SerializeField] private AudioCueSO _emptyShootAudioCue;

    [Tooltip("Used only when Audio Profile has no Hit Confirm Cue.")]
    [SerializeField] private AudioCueSO _hitConfirmAudioCue;

    [FormerlySerializedAs("reloadAudioCue")]
    [Tooltip("Used only when Audio Profile has no Player Reload Cue.")]
    [SerializeField] private AudioCueSO _playerReloadAudioCue;

    [Tooltip("Used only when Audio Profile has no Enemy Reload Cue.")]
    [SerializeField] private AudioCueSO _enemyReloadAudioCue;

    [Header("Muzzle")]
    [Tooltip("Optional muzzle particle spawned at the equipped WeaponView fire origin on each shot.")]
    [SerializeField] private ParticleSystem _muzzleParticlePrefab;

    [Tooltip("Multiplier applied to the shooter's base gunshot-hearing radius for this firearm.")]
    [SerializeField, HideInInspector, Min(0f)] private float _gunshotSoundRadiusMultiplier = 1f;

    [Tooltip("Flat world-distance added after the gunshot-hearing radius multiplier.")]
    [SerializeField, HideInInspector] private float _gunshotSoundRadiusAdd;

    [Header("Enemy Aim Laser")]
    [Tooltip("When enabled, enemy shooters holding this firearm draw a thin red line from the actual Weapon View Fire Origin along their current aim direction. This is a combat telegraph only and does not change accuracy or projectile collision.")]
    [SerializeField] private bool _showEnemyAimLaser;

    [Header("Durability")]
    [Tooltip("Factory maximum durability. Runtime items carry their own current and maximum values after spawning, looting, and repairing.")]
    [SerializeField, Min(1f)] private float _maxDurability = 100f;

    [Tooltip("플레이어의 성공한 발사 1회당 감소량. 산탄 펠릿 수와 무관하며 적은 소모하지 않습니다.")]
    [SerializeField, Min(0f)] private float _durabilityLossPerFire = 0.05f;

    [Tooltip("Fraction of the missing durability permanently removed from maximum durability when fully repaired. 0.4 turns 80/100 into 92/92.")]
    [SerializeField, Range(0f, 1f)] private float _repairPermanentLossRatio = 0.4f;

    public AudioCueSO FireAudioCue =>
        AudioProfile != null && AudioProfile.FireCue != null
            ? AudioProfile.FireCue
            : _fireAudioCue;

    public AudioCueSO EmptyShootAudioCue =>
        AudioProfile != null && AudioProfile.EmptyFireCue != null
            ? AudioProfile.EmptyFireCue
            : _emptyShootAudioCue;

    public AudioCueSO HitConfirmAudioCue =>
        AudioProfile != null && AudioProfile.HitConfirmCue != null
            ? AudioProfile.HitConfirmCue
            : _hitConfirmAudioCue;

    public AudioCueSO PlayerReloadAudioCue =>
        AudioProfile != null && AudioProfile.PlayerReloadCue != null
            ? AudioProfile.PlayerReloadCue
            : _playerReloadAudioCue;

    public AudioCueSO EnemyReloadAudioCue =>
        AudioProfile != null && AudioProfile.EnemyReloadCue != null
            ? AudioProfile.EnemyReloadCue
            : _enemyReloadAudioCue;
    public ParticleSystem MuzzleParticlePrefab => _muzzleParticlePrefab;
    public float GunshotSoundRadiusMultiplier => Mathf.Max(0f, _gunshotSoundRadiusMultiplier);
    public float GunshotSoundRadiusAdd => _gunshotSoundRadiusAdd;
    public bool ShowEnemyAimLaser => _showEnemyAimLaser;
    public float MaxDurability => Mathf.Max(1f, _maxDurability);
    [Header("총기 고장 · 내구도 1% 구간마다 확률 증가")]
    [Tooltip("이 비율 이상이면 고장 없음. 기본 90%; 바로 아래부터 첫 1% 구간을 적용합니다.")]
    [SerializeField, Range(0f, 1f)] private float _malfunctionThreshold = 0.90f;
    [Tooltip("누적 확률을 보정할 기준 내구도. 기본 30%.")]
    [SerializeField, Range(0f, 1f)] private float _malfunctionReferenceCondition = 0.30f;
    [SerializeField, Min(1)] private int _malfunctionReferenceShots = 30;
    [Tooltip("기준 내구도를 고정하고 기준 발수 안에 한 번 이상 고장 날 확률. 기본 25%.")]
    [SerializeField, Range(0f, 1f)] private float _malfunctionReferenceCumulativeChance = 0.25f;
    public float MalfunctionChance(float condition01)
    {
        float threshold = Mathf.Clamp01(_malfunctionThreshold);
        if (threshold <= 0f || float.IsNaN(condition01) || float.IsInfinity(condition01) || condition01 >= threshold) return 0f;
        // Round away float representation noise at exact whole-percent boundaries.
        // Ceil gives [89,90) bucket 1, [88,89) bucket 2, etc. No rounding of the safe threshold.
        int buckets = Mathf.Max(1, (int)System.Math.Ceiling(System.Math.Round(
            ((double)threshold - Mathf.Clamp01(condition01)) * 100d, 5)));
        int referenceBuckets = Mathf.Max(1, (int)System.Math.Ceiling(System.Math.Round(
            ((double)threshold - Mathf.Clamp01(_malfunctionReferenceCondition)) * 100d, 5)));
        // P(at least one in N) = 1 - (1-p)^N. Convert the chosen cumulative target to p first.
        double referencePerShot = 1d - System.Math.Pow(1d - Mathf.Clamp01(_malfunctionReferenceCumulativeChance),
            1d / Mathf.Max(1, _malfunctionReferenceShots));
        return Mathf.Clamp01((float)(referencePerShot * buckets / referenceBuckets));
    }
    public float DurabilityLossPerFire => Mathf.Max(0f, _durabilityLossPerFire);
    public float RepairPermanentLossRatio => Mathf.Clamp01(_repairPermanentLossRatio);
    public float DamageMultiplier => Mathf.Max(0f, _damageMultiplier);
    public float ShotsPerSecondMultiplier => Mathf.Max(0.01f, _shotsPerSecondMultiplier);
    public int MagazineSize => Mathf.Max(1, _magazineSize);
    public virtual RangedWeaponType WeaponType => _weaponType;
    public virtual RangedFirePattern FirePattern => RangedFirePattern.SingleProjectile;
    public virtual float PelletSpreadAngleDegrees => 0f;

    public BulletFamily RequiredBulletFamily => WeaponType switch
    {
        RangedWeaponType.Pistol => BulletFamily.Light,
        RangedWeaponType.SubmachineGun => BulletFamily.Light,
        RangedWeaponType.Revolver => BulletFamily.Heavy,
        RangedWeaponType.LightMachineGun => BulletFamily.Heavy,
        RangedWeaponType.AssaultRifle => BulletFamily.Medium,
        RangedWeaponType.SniperRifle => BulletFamily.Sniper,
        RangedWeaponType.Shotgun => BulletFamily.ShotgunShell,
        RangedWeaponType.RocketLauncher => BulletFamily.Rocket,
        _ => BulletFamily.Light
    };

    // Purpose: Checks whether compatible with is currently true.
    public bool IsCompatibleWith(BulletSO bullet)
    {
        return bullet != null &&
               (bullet.InfectionExposureOnHit <= 0f || WeaponType == RangedWeaponType.AssaultRifle) &&
               (bullet.Family == BulletFamily.Universal ||
                bullet.Family == RequiredBulletFamily);
    }

    // Purpose: Resolves the projectile count needed by this component.
    public virtual int ResolveProjectileCount(BulletSO bullet)
    {
        return 1;
    }

    // Purpose: Resolves the damage multiplier per projectile needed by this component.
    public float ResolveDamageMultiplierPerProjectile(BulletSO bullet)
    {
        int projectileCount = Mathf.Max(1, ResolveProjectileCount(bullet));
        return DamageMultiplier / projectileCount;
    }

    // Purpose: Resolves the gunshot sound radius needed by this component.
    public float ResolveGunshotSoundRadius(float baseRadius)
    {
        return GunshotSoundRadius; // Compatibility signature; no character base or legacy multiplier.
    }
}
