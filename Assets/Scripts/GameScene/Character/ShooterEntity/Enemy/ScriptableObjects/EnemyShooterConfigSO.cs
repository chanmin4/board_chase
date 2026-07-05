using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyShooterConfig",
    menuName = "Game/Enemy/Shooter Config")]
public class EnemyShooterConfigSO : EnemyStatConfigSO
{
    [Header("Prefab")]
    [SerializeField] private EnemyShooter _enemyShooterPrefab;

    [Header("Shooter")]
    [SerializeField] private ShooterStatSettings _shooter = new();

    [Header("Bullet")]
    [SerializeField] private BulletSO _bullet;
    [SerializeField] private PaintChannel _paintChannel = PaintChannel.Virus;

    [Header("Awareness")]
    [SerializeField] private bool _alertOnDamaged = true;
    [SerializeField, Min(0.05f)] private float _targetSearchInterval = 0.35f;
    [SerializeField, Min(0f)] private float _targetMemorySeconds = 4f;
    [SerializeField] private bool _chaseLastKnownPosition = true;

    [Header("Combat Distance")]
    [SerializeField] private bool _useMaxRangeAsAttackKeepRange = true;
    [SerializeField, Min(0f)] private float _attackKeepRange = 0f;
    [SerializeField, Min(0f)] private float _chaseStoppingDistance = 0f;

    [Header("Combat Timing")]
    [SerializeField, Min(0f)] private float _reactionTime = 0.35f;
    [SerializeField, Min(1)] private int _burstCount = 1;
    [SerializeField, Min(0f)] private float _burstInterval = 0.12f;

    [Header("Combat Movement")]
    [SerializeField] private bool _moveWhileAttacking = false;
    [SerializeField, Min(0.02f)] private float _combatMoveRefreshInterval = 0.1f;

    [Header("Patrol")]
    [SerializeField, Min(0f)] private float _patrolRadius = 8f;
    [SerializeField, Min(0.05f)] private float _patrolDestinationRefreshInterval = 1.25f;
    [SerializeField, Min(0f)] private float _patrolArriveDistance = 0.5f;
    [SerializeField, Min(0f)] private float _navMeshSampleDistance = 3f;

    [Header("AI Aim")]
    [SerializeField, Min(0f)] private float _aimTrackingSpeedDegPerSecond = 720f;
    [SerializeField, Min(0f)] private float _aimErrorAngleDeg = 0f;
    [SerializeField, Min(0f)] private float _targetLeadSeconds = 0f;

    [Header("Paint Avoidance")]
    [SerializeField] private bool _avoidVaccinePaint = true;
    [SerializeField] private bool _avoidVaccinePaintWhileAttacking = false;
    [SerializeField, Min(0.05f)] private float _paintAvoidanceCheckInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float _paintAvoidanceSearchRadius = 4f;
    [SerializeField, Min(1)] private int _paintAvoidanceSampleCount = 8;
    [SerializeField, Min(0.1f)] private float _paintAvoidanceNavMeshSampleDistance = 2f;

    [Header("Defensive Dash")]
    [SerializeField] private bool _useDash = false;
    [SerializeField] private bool _dashOnDamaged = true;
    [SerializeField, Range(0f, 1f)] private float _dashChanceOnDamaged = 1f;
    [SerializeField] private bool _dashWhenHealthBelowThreshold = true;
    [SerializeField, Range(0f, 1f)] private float _dashHealthNormalizedThreshold = 0.35f;
    [SerializeField, Min(0f)] private float _dashCooldownSeconds = 4f;
    [SerializeField, Min(0.01f)] private float _dashDurationSeconds = 0.18f;
    [SerializeField, Min(0f)] private float _dashSpeed = 14f;
    [SerializeField] private bool _dashAwayFromTarget = true;

    public EnemyShooter EnemyShooterPrefab => _enemyShooterPrefab;
    public override Enemy EnemyPrefab => _enemyShooterPrefab;

    public BulletSO Bullet => _bullet;
    public PaintChannel PaintChannel => _paintChannel;

    public bool AlertOnDamaged => _alertOnDamaged;
    public float TargetSearchInterval => Mathf.Max(0.05f, _targetSearchInterval);
    public float TargetMemorySeconds => Mathf.Max(0f, _targetMemorySeconds);
    public bool ChaseLastKnownPosition => _chaseLastKnownPosition;

    public float AttackStartRange => MaxRange;
    public float AttackKeepRange => _useMaxRangeAsAttackKeepRange ? MaxRange : Mathf.Max(0f, _attackKeepRange);
    public float ChaseStoppingDistance => _chaseStoppingDistance > 0f ? _chaseStoppingDistance : Mathf.Min(AttackStartRange, 8f);

    public float ReactionTime => Mathf.Max(0f, _reactionTime);
    public int BurstCount => Mathf.Max(1, _burstCount);
    public float BurstInterval => Mathf.Max(0f, _burstInterval);

    public bool MoveWhileAttacking => _moveWhileAttacking;
    public float CombatMoveRefreshInterval => Mathf.Max(0.02f, _combatMoveRefreshInterval);

    public float PatrolRadius => Mathf.Max(0f, _patrolRadius);
    public float PatrolDestinationRefreshInterval => Mathf.Max(0.05f, _patrolDestinationRefreshInterval);
    public float PatrolArriveDistance => Mathf.Max(0f, _patrolArriveDistance);
    public float NavMeshSampleDistance => Mathf.Max(0f, _navMeshSampleDistance);

    public float AimTrackingSpeedDegPerSecond => Mathf.Max(0f, _aimTrackingSpeedDegPerSecond);
    public float AimErrorAngleDeg => Mathf.Max(0f, _aimErrorAngleDeg);
    public float TargetLeadSeconds => Mathf.Max(0f, _targetLeadSeconds);

    public bool AvoidVaccinePaint => _avoidVaccinePaint;
    public bool AvoidVaccinePaintWhileAttacking => _avoidVaccinePaintWhileAttacking;
    public float PaintAvoidanceCheckInterval => Mathf.Max(0.05f, _paintAvoidanceCheckInterval);
    public float PaintAvoidanceSearchRadius => Mathf.Max(0.1f, _paintAvoidanceSearchRadius);
    public int PaintAvoidanceSampleCount => Mathf.Max(1, _paintAvoidanceSampleCount);
    public float PaintAvoidanceNavMeshSampleDistance => Mathf.Max(0.1f, _paintAvoidanceNavMeshSampleDistance);

    public bool UseDash => _useDash;
    public bool DashOnDamaged => _dashOnDamaged;
    public float DashChanceOnDamaged => Mathf.Clamp01(_dashChanceOnDamaged);
    public bool DashWhenHealthBelowThreshold => _dashWhenHealthBelowThreshold;
    public float DashHealthNormalizedThreshold => Mathf.Clamp01(_dashHealthNormalizedThreshold);
    public float DashCooldownSeconds => Mathf.Max(0f, _dashCooldownSeconds);
    public float DashDurationSeconds => Mathf.Max(0.01f, _dashDurationSeconds);
    public float DashSpeed => Mathf.Max(0f, _dashSpeed);
    public bool DashAwayFromTarget => _dashAwayFromTarget;

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
