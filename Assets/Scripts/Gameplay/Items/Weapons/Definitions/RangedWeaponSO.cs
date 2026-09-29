/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the RangedWeaponSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

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
    [Header("Firearm Type")]
    [Tooltip("Determines which bullet family this firearm accepts. Pistol/SMG=Light, Revolver/LMG=Heavy, AR=Medium, Sniper=Sniper, Shotgun=ShotgunShell, RocketLauncher=Rocket.")]
    [SerializeField] private RangedWeaponType _weaponType = RangedWeaponType.Pistol;

    [Header("Archetype Combat Coefficients")]
    [Tooltip("Weapon-family damage multiplier applied after Shooter, meta, passive, and item modifiers. For pellet weapons this is the total damage multiplier of one trigger pull and is divided across the spawned pellets.")]
    [SerializeField, Min(0f)] private float _damageMultiplier = 1f;

    [Tooltip("Weapon-family fire-rate multiplier applied after the Shooter's base Shots Per Second and stat modifiers.")]
    [SerializeField, Min(0.01f)] private float _shotsPerSecondMultiplier = 1f;

    [Header("Magazine")]
    [Tooltip("Base rounds loaded by this specific firearm. Passive/meta Magazine Size modifiers are applied after this value.")]
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

    [Header("Gunshot Sound")]
    [Tooltip("Multiplier applied to the shooter's base gunshot-hearing radius for this firearm.")]
    [SerializeField, Min(0f)] private float _gunshotSoundRadiusMultiplier = 1f;

    [Tooltip("Flat world-distance added after the gunshot-hearing radius multiplier.")]
    [SerializeField] private float _gunshotSoundRadiusAdd;

    [Header("Enemy Aim Laser")]
    [Tooltip("When enabled, enemy shooters holding this firearm draw a thin red line from the actual Weapon View Fire Origin along their current aim direction. This is a combat telegraph only and does not change accuracy or projectile collision.")]
    [SerializeField] private bool _showEnemyAimLaser;

    [Header("Durability")]
    [Tooltip("Factory maximum durability. Runtime items carry their own current and maximum values after spawning, looting, and repairing.")]
    [SerializeField, Min(1f)] private float _maxDurability = 100f;

    [Tooltip("Durability removed after one successful trigger fire. A shotgun loses this once per shell, not once per pellet.")]
    [SerializeField, Min(0f)] private float _durabilityLossPerFire = 0.5f;

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
        return Mathf.Max(
            0f,
            Mathf.Max(0f, baseRadius) * GunshotSoundRadiusMultiplier + GunshotSoundRadiusAdd);
    }
}
