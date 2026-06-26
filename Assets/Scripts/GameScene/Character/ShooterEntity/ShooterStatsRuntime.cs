using UnityEngine;

public abstract class ShooterStatsRuntime : MonoBehaviour
{
    public abstract EntityStatConfigSO StatConfig { get; }

    public virtual WeaponSO CurrentWeapon => null;
    public virtual BulletSO CurrentBullet => null;
    public virtual PaintChannel PaintChannel => PaintChannel.Vaccine;

    public virtual float AttackDamage => 0f;
    public virtual float MaxRange => 0f;
    public virtual float ShotsPerSecond => 0f;
    public virtual float ReloadDurationSeconds => 0f;
    public virtual int MagazineSize => 0;

    public virtual float PaintRadius => 0f;
    public virtual int PaintPriority => 0;

    public virtual float MoveSpeed => 0f;
    public virtual float MaxHealth => 0f;
    public virtual float DodgeChance => 0f;
    public virtual float VisionRange => StatConfig != null ? StatConfig.Vision.VisionRange : 0f;
    public virtual float GunshotSoundRadius =>
        CurrentWeapon != null ? CurrentWeapon.ResolveGunshotSoundRadius(0f) : 0f;
    public virtual float SoundInvestigateDelaySeconds => 0f;
    public virtual float FootstepSoundRadius => 0f;
    public virtual float FootstepSoundInterval => 0.35f;

    public virtual float AimSpeed => 8f;
    public virtual float AimRangeMultiplier => 1f;
    public virtual float AimMoveSpeedMultiplier => 1f;
    public virtual float HipFireSpreadAngleDeg => 0f;
    public virtual float AimSpreadAngleDeg => 0f;
    public virtual float RecoilAngleDeg => 0f;
    public virtual float RecoilRecoverySpeedDegPerSecond => 20f;

    public virtual float ResolveAttackDamage(BulletSO bullet)
    {
        return Mathf.Max(0f, AttackDamage);
    }

    public virtual float ResolvePaintRadius(BulletSO bullet)
    {
        return Mathf.Max(0f, PaintRadius);
    }

    public virtual float ResolveShotsPerSecond(BulletSO bullet)
    {
        return Mathf.Max(0.01f, ShotsPerSecond);
    }

    public virtual int ResolveMagazineSize(BulletSO bullet)
    {
        return Mathf.Max(1, MagazineSize);
    }
}
