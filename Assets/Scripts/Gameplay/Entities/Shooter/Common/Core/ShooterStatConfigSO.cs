/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Implements the focused ShooterStatSettings responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public sealed class ShooterInventorySettings
{
    [SerializeField, Min(0)] private int _pocketCapacity = 2;
    [SerializeField, Min(0)] private int _baseInventoryCapacity = 4;

    public int PocketCapacity => Mathf.Max(0, _pocketCapacity);
    public int BaseInventoryCapacity => Mathf.Max(0, _baseInventoryCapacity);
    public int CapacityWithoutBackpack => PocketCapacity + BaseInventoryCapacity;
    public int ResolveCapacity(BackpackItemSO backpack) =>
        CapacityWithoutBackpack + (backpack != null ? backpack.InventorySlotCapacity : 0);
}

[System.Serializable]
public sealed class ShooterDashSettings
{
    public bool useMoveSpeedBasedDash = true;
    [Min(0f)] public float speedMultiplier = 1.8f;
    [Min(0f)] public float fixedSpeed = 20f;
    [Min(0.01f)] public float durationSeconds = 0.3f;
    [Min(0f)] public float cooldownSeconds = 10f;
    [Min(0f)] public float distanceMultiplier = 1.3f;
    public bool useForwardWhenNoMovementInput = true;
    public bool rotateTowardDashDirection = true;
    public bool useVisualLean = true;
    public Vector3 visualLeanEuler = new(18f, 0f, 0f);
    [Min(0f)] public float visualLeanInSpeed = 18f;
    [Min(0f)] public float visualLeanOutSpeed = 12f;
}

[System.Serializable]
public sealed class ShooterStatSettings
{
    [Header("Combat Coefficients")]
    [Tooltip("Multiplier applied to the ammunition Health Damage. Keep 1 for the normal Player/Shooter baseline.")]
    [SerializeField, HideInInspector, Min(0f)] private float _healthDamageCoefficient = 1f;

    [Tooltip("Multiplier applied to the ammunition Infection Damage. Keep 1 for the normal Player/Shooter baseline.")]
    [SerializeField, HideInInspector, Min(0f)] private float _infectionDamageCoefficient = 1f;

    [Tooltip("Multiplier applied to the ammunition Mark Amount. Mark itself remains penetration-independent.")]
    [SerializeField, HideInInspector, Min(0f)] private float _markAmountCoefficient = 1f;

    [Header("Weapon Handling")]
    [FormerlySerializedAs("maxRange")]
    [FormerlySerializedAs("_maxRange")]
    [Tooltip("Maximum projectile travel distance. It does not limit the cursor, attack input, close vision, or forward vision.")]
    [SerializeField, HideInInspector, Min(0.1f)] private float _maxRange = 12f;

    [FormerlySerializedAs("shotsPerSecond")]
    [FormerlySerializedAs("attackShotsPerSecond")]
    [FormerlySerializedAs("_shotsPerSecond")]
    [Tooltip("Shooter baseline automatic-fire cadence before the equipped firearm's archetype multiplier. The first shot is immediate.")]
    [SerializeField, HideInInspector, Min(0.01f)] private float _shotsPerSecond = 2.6f;

    [FormerlySerializedAs("reloadDurationSeconds")]
    [FormerlySerializedAs("_reloadDurationSeconds")]
    [SerializeField, HideInInspector, Min(0.01f)] private float _reloadDurationSeconds = 1.2f;

    [FormerlySerializedAs("magazineSize")]
    [FormerlySerializedAs("_magazineSize")]
    [SerializeField, HideInInspector, Min(1)] private int _magazineSize = 6;

    [Header("Penetration Base")]
    [SerializeField, HideInInspector, Min(0)] private float _penetrationClassBonus = 0;

    [Header("Aim")]
    [Tooltip("How quickly Aim01 reaches full aim while RMB is held.")]
    [SerializeField, HideInInspector, Min(0.01f)] private float _aimSpeed = 8f;

    [Tooltip("Full hip-fire cone angle in degrees. A value of 36 fires within -18 to +18 degrees.")]
    [SerializeField, HideInInspector, Min(0f)] private float _hipFireSpreadAngleDeg = 6f;

    [Tooltip("Full cone angle used only while RMB is still held and Aim01 has reached 100%.")]
    [SerializeField, HideInInspector, Min(0f)] private float _aimSpreadAngleDeg = 0.5f;

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
    [SerializeField, HideInInspector, Min(0f)] private float _gunshotSoundRadius = 16f;

    [FormerlySerializedAs("soundInvestigateDelaySeconds")]
    [FormerlySerializedAs("_soundInvestigateDelaySeconds")]
    [SerializeField, Min(0f)] private float _soundInvestigateDelaySeconds = 0.25f;

    [FormerlySerializedAs("footstepSoundRadius")]
    [FormerlySerializedAs("_footstepSoundRadius")]
    [SerializeField, Min(0f)] private float _footstepSoundRadius = 5f;

    [FormerlySerializedAs("footstepSoundInterval")]
    [FormerlySerializedAs("_footstepSoundInterval")]
    [SerializeField, Min(0.05f)] private float _footstepSoundInterval = 0.35f;

    // Serialized compatibility only. Capacity now belongs to ShooterInventorySettings.
    [SerializeField, HideInInspector, Min(0)] private int _inventoryCapacity = 12;

    // Compatibility API only: the equipped RangedWeaponSO owns all shooting baselines.
    public float MaxRange => 0f;
    public float ShotsPerSecond => 0f;
    public float ReloadDurationSeconds => 0f;
    public int MagazineSize => 1; // Compatibility fallback; firearms own their magazine.
    public float PenetrationClassBonus => 0f;
    public float AimSpeed => 0f;
    public float AimRangeMultiplier => 1f;
    public float AimMoveSpeedMultiplier => 1f;
    public float HipFireSpreadAngleDeg => 0f;
    public float AimSpreadAngleDeg => 0f;
    public float RecoilAngleDeg => 0f;
    public float RecoilRecoverySpeedDegPerSecond => 0f;
    public float RecoilForwardDistancePerShot => 0f;
    public float RecoilSideDistancePerShot => 0f;
    public float MaxRecoilDistance => 0f;
    public float RecoilDistanceRecoveryPerSecond => 0f;
    public float HipFireSpreadRadius => 0f;
    public float AimSpreadRadius => 0f;
    public float GunshotSoundRadius => 0f;
    public float SoundInvestigateDelaySeconds => Mathf.Max(0f, _soundInvestigateDelaySeconds);
    public float FootstepSoundRadius => Mathf.Max(0f, _footstepSoundRadius);
    public float FootstepSoundInterval => Mathf.Max(0.05f, _footstepSoundInterval);
    public int InventoryCapacity => 0; // Compatibility only; never an active capacity source.
    public float HealthDamageCoefficient => 1f;
    public float InfectionDamageCoefficient => 0f; // Legacy basic-bullet infection only.
    public float MarkAmountCoefficient => 0f; // WIP: no mark damage in current combat.
}

public abstract class ShooterStatConfigSO : EntityStatConfigSO
{
    [Header("공통 규칙 · 기본 공유 / 필요한 항목만 Override")]
    [SerializeField] private ShooterCommonRulesSO _commonRules;
    private static ShooterCommonRulesSO _defaultCommonRules;
    public ShooterCommonRulesSO CommonRules
    {
        get
        {
            if (_commonRules != null) return _commonRules;
            if (_defaultCommonRules == null) _defaultCommonRules = Resources.Load<ShooterCommonRulesSO>("ShooterCommonRules");
            return _defaultCommonRules;
        }
    }
    [SerializeField, Min(1f)] private float _maximumStamina;
    public float MaximumStamina => _maximumStamina >= 1f ? _maximumStamina
        : (this is EnemyShooterConfigSO ? 400f : 200f);
    protected virtual void OnValidate()
    {
        if (_maximumStamina < 1f) _maximumStamina = this is EnemyShooterConfigSO ? 400f : 200f;
    }

    [Header("Shooter")]
    [SerializeField] private ShooterStatSettings _shooter = new();

    [Header("걷기 · 달리기 · 스태미나 (공통)")]
    [SerializeField] private bool _overrideCommonLocomotion;
    [SerializeField, HideInInspector] private bool _overrideCommonLocomotionInitialized;
    [SerializeField, HideInInspector] private ShooterLocomotionSettings _locomotion = new();
    public ShooterLocomotionSettings Locomotion => !_overrideCommonLocomotion && CommonRules != null
        ? CommonRules.Locomotion : (_locomotion ??= new ShooterLocomotionSettings());
    // AI-only sprint decisions remain local, not part of shared movement/resource rules.
    public bool EnemyCanSprint => _locomotion.enemyCanSprint;
    public float EnemySprintStartDistance => Mathf.Max(0f, _locomotion.enemySprintStartDistance);
    public float EnemySprintStopDistance => Mathf.Clamp(_locomotion.enemySprintStopDistance, 0f, EnemySprintStartDistance);
    [Header("피격 흔들림 (공통) · 발사 반동은 무기 SO")]
    [SerializeField] private bool _overrideCommonAimFeedback;
    [SerializeField, HideInInspector] private bool _overrideCommonAimFeedbackInitialized;
    [SerializeField, HideInInspector] private ShooterAimFeedbackSettings _aimFeedback = new();
    public ShooterAimFeedbackSettings AimFeedback => !_overrideCommonAimFeedback && CommonRules != null
        ? CommonRules.AimFeedback : (_aimFeedback ??= new ShooterAimFeedbackSettings());

    [Header("인벤토리 · 포켓 / 기본칸 / 가방 추가칸")]
    [SerializeField] private ShooterInventorySettings _inventory = new();
    public ShooterInventorySettings Inventory => _inventory ??= new ShooterInventorySettings();

    [Header("대시 · 플레이어 / 적 공통")]
    [SerializeField] private ShooterDashSettings _dash = new();
    public ShooterDashSettings Dash => _dash ??= new ShooterDashSettings();
    public virtual float DashCooldownSeconds => Mathf.Max(0f, Dash.cooldownSeconds);

    [Header("Loadout")]
    [SerializeField] private ShooterLoadoutTableSO _loadoutTable;

    public ShooterLoadoutTableSO LoadoutTable => _loadoutTable;

    public float MaxRange => _shooter.MaxRange;
    public float ShotsPerSecond => _shooter.ShotsPerSecond;
    public float ReloadDurationSeconds => _shooter.ReloadDurationSeconds;
    public int MagazineSize => _shooter.MagazineSize;
    public float PenetrationClassBonus => _shooter.PenetrationClassBonus;
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
    public int InventoryCapacity => Inventory.CapacityWithoutBackpack;
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
