/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Implements the focused ShooterStatSettings responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public sealed class ShooterStatSettings
{
    [Header("Combat Coefficients")]
    [Tooltip("Multiplier applied to the ammunition Health Damage. Keep 1 for the normal Player/Shooter baseline.")]
    [SerializeField, Min(0f)] private float _healthDamageCoefficient = 1f;

    [Tooltip("Multiplier applied to the ammunition Infection Damage. Keep 1 for the normal Player/Shooter baseline.")]
    [SerializeField, Min(0f)] private float _infectionDamageCoefficient = 1f;

    [Tooltip("Multiplier applied to the ammunition Mark Amount. Mark itself remains penetration-independent.")]
    [SerializeField, Min(0f)] private float _markAmountCoefficient = 1f;

    [Header("Weapon Handling")]
    [FormerlySerializedAs("maxRange")]
    [FormerlySerializedAs("_maxRange")]
    [Tooltip("Maximum projectile travel distance. It does not limit the cursor, attack input, close vision, or forward vision.")]
    [SerializeField, Min(0.1f)] private float _maxRange = 12f;

    [FormerlySerializedAs("shotsPerSecond")]
    [FormerlySerializedAs("attackShotsPerSecond")]
    [FormerlySerializedAs("_shotsPerSecond")]
    [Tooltip("Shooter baseline automatic-fire cadence before the equipped firearm's archetype multiplier. The first shot is immediate.")]
    [SerializeField, Min(0.01f)] private float _shotsPerSecond = 2.6f;

    [FormerlySerializedAs("reloadDurationSeconds")]
    [FormerlySerializedAs("_reloadDurationSeconds")]
    [SerializeField, Min(0.01f)] private float _reloadDurationSeconds = 1.2f;

    [FormerlySerializedAs("magazineSize")]
    [FormerlySerializedAs("_magazineSize")]
    [SerializeField, Min(1)] private int _magazineSize = 6;

    [Header("Penetration Base")]
    [SerializeField, Min(0)] private int _penetrationClassBonus = 0;

    [Header("Aim")]
    [Tooltip("How quickly Aim01 reaches full aim while RMB is held.")]
    [SerializeField, Min(0.01f)] private float _aimSpeed = 8f;

    [Tooltip("Full hip-fire cone angle in degrees. A value of 36 fires within -18 to +18 degrees.")]
    [SerializeField, Min(0f)] private float _hipFireSpreadAngleDeg = 6f;

    [Tooltip("Full cone angle used only while RMB is still held and Aim01 has reached 100%.")]
    [SerializeField, Min(0f)] private float _aimSpreadAngleDeg = 0.5f;

    // Kept hidden only so existing assets deserialize without losing their old data.
    // Gameplay no longer consumes range/move/bloom tuning from these legacy fields.
    [HideInInspector, SerializeField] private float _aimRangeMultiplier = 1f;
    [HideInInspector, SerializeField] private float _aimMoveSpeedMultiplier = 0.6f;
    [HideInInspector, SerializeField] private float _recoilAngleDeg;
    [HideInInspector, SerializeField] private float _recoilRecoverySpeedDegPerSecond;
    [HideInInspector, SerializeField] private float _recoilForwardDistancePerShot;
    [HideInInspector, SerializeField] private float _recoilSideDistancePerShot;
    [HideInInspector, SerializeField] private float _maxRecoilDistance;
    [HideInInspector, SerializeField] private float _recoilDistanceRecoveryPerSecond;
    [HideInInspector, SerializeField] private float _hipFireSpreadRadius;
    [HideInInspector, SerializeField] private float _aimSpreadRadius;

    [Header("Sound")]
    [FormerlySerializedAs("gunshotSoundRadius")]
    [FormerlySerializedAs("_gunshotSoundRadius")]
    [SerializeField, Min(0f)] private float _gunshotSoundRadius = 16f;

    [FormerlySerializedAs("soundInvestigateDelaySeconds")]
    [FormerlySerializedAs("_soundInvestigateDelaySeconds")]
    [SerializeField, Min(0f)] private float _soundInvestigateDelaySeconds = 0.25f;

    [FormerlySerializedAs("footstepSoundRadius")]
    [FormerlySerializedAs("_footstepSoundRadius")]
    [SerializeField, Min(0f)] private float _footstepSoundRadius = 5f;

    [FormerlySerializedAs("footstepSoundInterval")]
    [FormerlySerializedAs("_footstepSoundInterval")]
    [SerializeField, Min(0.05f)] private float _footstepSoundInterval = 0.35f;

    [Header("Inventory")]
    [SerializeField, Min(0)] private int _inventoryCapacity = 12;

    public float MaxRange => Mathf.Max(0.1f, _maxRange);
    public float ShotsPerSecond => Mathf.Max(0.01f, _shotsPerSecond);
    public float ReloadDurationSeconds => Mathf.Max(0.01f, _reloadDurationSeconds);
    public int MagazineSize => Mathf.Max(1, _magazineSize);
    public int PenetrationClassBonus => Mathf.Max(0, _penetrationClassBonus);
    public float AimSpeed => Mathf.Max(0.01f, _aimSpeed);
    public float AimRangeMultiplier => Mathf.Max(0.1f, _aimRangeMultiplier);
    public float AimMoveSpeedMultiplier => Mathf.Clamp(_aimMoveSpeedMultiplier, 0.05f, 1f);
    public float HipFireSpreadAngleDeg => Mathf.Max(0f, _hipFireSpreadAngleDeg);
    public float AimSpreadAngleDeg => Mathf.Max(0f, _aimSpreadAngleDeg);
    public float RecoilAngleDeg => Mathf.Max(0f, _recoilAngleDeg);
    public float RecoilRecoverySpeedDegPerSecond => Mathf.Max(0f, _recoilRecoverySpeedDegPerSecond);
    public float RecoilForwardDistancePerShot => Mathf.Max(0f, _recoilForwardDistancePerShot);
    public float RecoilSideDistancePerShot => Mathf.Max(0f, _recoilSideDistancePerShot);
    public float MaxRecoilDistance => Mathf.Max(0f, _maxRecoilDistance);
    public float RecoilDistanceRecoveryPerSecond => Mathf.Max(0f, _recoilDistanceRecoveryPerSecond);
    public float HipFireSpreadRadius => Mathf.Max(0f, _hipFireSpreadRadius);
    public float AimSpreadRadius => Mathf.Max(0f, _aimSpreadRadius);
    public float GunshotSoundRadius => Mathf.Max(0f, _gunshotSoundRadius);
    public float SoundInvestigateDelaySeconds => Mathf.Max(0f, _soundInvestigateDelaySeconds);
    public float FootstepSoundRadius => Mathf.Max(0f, _footstepSoundRadius);
    public float FootstepSoundInterval => Mathf.Max(0.05f, _footstepSoundInterval);
    public int InventoryCapacity => Mathf.Max(0, _inventoryCapacity);
    public float HealthDamageCoefficient => Mathf.Max(0f, _healthDamageCoefficient);
    public float InfectionDamageCoefficient => Mathf.Max(0f, _infectionDamageCoefficient);
    public float MarkAmountCoefficient => Mathf.Max(0f, _markAmountCoefficient);
}

public abstract class ShooterStatConfigSO : EntityStatConfigSO
{
    [Header("Shooter")]
    [SerializeField] private ShooterStatSettings _shooter = new();

    [Header("Loadout")]
    [SerializeField] private ShooterLoadoutTableSO _loadoutTable;

    public ShooterLoadoutTableSO LoadoutTable => _loadoutTable;

    public float MaxRange => _shooter.MaxRange;
    public float ShotsPerSecond => _shooter.ShotsPerSecond;
    public float ReloadDurationSeconds => _shooter.ReloadDurationSeconds;
    public int MagazineSize => _shooter.MagazineSize;
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
    public int InventoryCapacity => _shooter.InventoryCapacity;
    public float HealthDamageCoefficient => _shooter.HealthDamageCoefficient;
    public float InfectionDamageCoefficient => _shooter.InfectionDamageCoefficient;
    public float MarkAmountCoefficient => _shooter.MarkAmountCoefficient;

    // Purpose: Attempts to roll loadout and reports whether the request succeeded.
    public bool TryRollLoadout(out ShooterLoadoutSnapshot snapshot)
    {
        if (_loadoutTable == null)
        {
            snapshot = default;
            return false;
        }

        return _loadoutTable.TryRoll(out snapshot);
    }
}
