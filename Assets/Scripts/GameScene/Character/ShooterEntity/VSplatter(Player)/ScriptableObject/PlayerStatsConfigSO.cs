using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStatsConfig",
    menuName = "VSplatter/Player/Stats Config")]
public class PlayerStatsConfigSO : EntityStatConfigSO
{
    [Header("Shooter")]
    [SerializeField] private ShooterStatSettings _shooter = new();

    [Header("Currency Base")]
    [Min(0)] [SerializeField] private int startingRunCurrency = 0;
    [Min(0)] [SerializeField] private int startingRoguelikeCurrencyForNewSave = 0;
    [Header("Dash")]
    [Min(0f)] [SerializeField] private float dashCooldownSeconds = 3f;

    [Header("Player Survival")]
    [Range(0f, 1f)] [SerializeField] private float dodgeChance = 0f;

    public int StartingRunCurrency => startingRunCurrency;
    public int StartingRoguelikeCurrencyForNewSave => startingRoguelikeCurrencyForNewSave;
    public float DashCooldownSeconds => Mathf.Max(0f, dashCooldownSeconds);
    public float DodgeChance => Mathf.Clamp01(dodgeChance);

    public float AttackDamage => _shooter.AttackDamage;
    public float Damage => _shooter.Damage;
    public float MaxRange => _shooter.MaxRange;
    public float ShotsPerSecond => _shooter.ShotsPerSecond;
    public float ReloadDurationSeconds => _shooter.ReloadDurationSeconds;
    public int MagazineSize => _shooter.MagazineSize;
    public float PaintRadius => _shooter.PaintRadius;
    public int PaintPriority => _shooter.PaintPriority;
    public float PaintMarkDamage => _shooter.PaintMarkDamage;
    public float InfectionDamage => _shooter.InfectionDamage;
    public int PenetrationClassBonus => _shooter.PenetrationClassBonus;
    public float AimSpeed => _shooter.AimSpeed;
    public float AimRangeMultiplier => _shooter.AimRangeMultiplier;
    public float AimMoveSpeedMultiplier => _shooter.AimMoveSpeedMultiplier;
    public float HipFireSpreadAngleDeg => _shooter.HipFireSpreadAngleDeg;
    public float AimSpreadAngleDeg => _shooter.AimSpreadAngleDeg;
    public float RecoilAngleDeg => _shooter.RecoilAngleDeg;
    public float RecoilRecoverySpeedDegPerSecond => _shooter.RecoilRecoverySpeedDegPerSecond;
    public float RecoilForwardDistancePerShot => _shooter.RecoilForwardDistancePerShot;
    public float RecoilSideDistancePerShot => _shooter.RecoilSideDistancePerShot;
    public float MaxRecoilDistance => _shooter.MaxRecoilDistance;
    public float RecoilDistanceRecoveryPerSecond => _shooter.RecoilDistanceRecoveryPerSecond;
    public float HipFireSpreadRadius => _shooter.HipFireSpreadRadius;
    public float AimSpreadRadius => _shooter.AimSpreadRadius;
    public float GunshotSoundRadius => _shooter.GunshotSoundRadius;
    public float SoundInvestigateDelaySeconds => _shooter.SoundInvestigateDelaySeconds;
    public float FootstepSoundRadius => _shooter.FootstepSoundRadius;
    public float FootstepSoundInterval => _shooter.FootstepSoundInterval;
}
