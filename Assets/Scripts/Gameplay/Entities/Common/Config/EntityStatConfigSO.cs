/*
 * Role hierarchy: Gameplay > Entities > Common > Config
 * Primary responsibility: Handles EntityPaintMarkSettings combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class EntityPaintMarkSettings
{
    [Header("Enable")]
    [SerializeField] private bool _usePaintMark = true;

    [Header("Stack Amount")]
    [SerializeField, Min(1f)] private float _markAmountPerStack = 100f;
    [SerializeField, Min(1)] private int _maxVaccineMarkStacks = 3;
    [SerializeField, Min(1)] private int _maxVirusMarkStacks = 3;

    [Header("Start")]
    [SerializeField] private bool _startWithVirusMark = false;
    [SerializeField, Min(0f)] private float _startingVirusMarkAmount = 0f;

    [Header("Gain Resistance")]
    [Tooltip("Multiplier applied to incoming Mark for every already-completed stack. 0.75 means stack 1 gains at 75%, stack 2 at 56.25%.")]
    [SerializeField, Range(0.01f, 1f)] private float _gainMultiplierPerCompletedStack = 0.75f;

    [Header("Decay")]
    [SerializeField, Min(0f)] private float _inactiveSlowDecayAmountPerTick = 0f;
    [SerializeField, Min(0f)] private float _activeSlowDecayAmountPerTick = 0.5f;
    [Header("Armor Class")]
    [Tooltip("Effective Armor Class removed by each completed Mark stack, regardless of paint faction.")]
    [SerializeField, Min(0)] private int _armorClassReductionPerStack = 1;

    public bool UsePaintMark => _usePaintMark;
    public float MarkAmountPerStack => Mathf.Max(1f, _markAmountPerStack);
    public int MaxVaccineMarkStacks => Mathf.Max(1, _maxVaccineMarkStacks);
    public int MaxVirusMarkStacks => Mathf.Max(1, _maxVirusMarkStacks);
    public float MaxVaccineMarkAmount => MarkAmountPerStack * MaxVaccineMarkStacks;
    public float MaxVirusMarkAmount => MarkAmountPerStack * MaxVirusMarkStacks;
    public bool StartWithVirusMark => _startWithVirusMark;
    public float StartingVirusMarkAmount => Mathf.Max(0f, _startingVirusMarkAmount);
    public float GainMultiplierPerCompletedStack => Mathf.Clamp(_gainMultiplierPerCompletedStack, 0.01f, 1f);
    public float InactiveSlowDecayAmountPerTick => Mathf.Max(0f, _inactiveSlowDecayAmountPerTick);
    public float ActiveSlowDecayAmountPerTick => Mathf.Max(0f, _activeSlowDecayAmountPerTick);
    public int ArmorClassReductionPerStack => Mathf.Max(0, _armorClassReductionPerStack);
}

[Serializable]
public class EntityVisionSettings
{
    [Header("Range")]
    [Tooltip("Length of the forward vision cone for gameplay detection, entity reveal, and player fog. This is independent from projectile MaxRange.")]
    [SerializeField, Min(0.1f)] private float _visionRange = 12f;

    [Tooltip("Radius of the omnidirectional close vision circle. Structures still clip this circle through the same line-of-sight mask.")]
    [SerializeField, Min(0f)] private float _closeVisionRadius = 7.5f;

    [Header("Line Of Sight")]
    [Tooltip("When enabled, both the close vision circle and forward cone are clipped by the blocker mask below.")]
    [SerializeField] private bool _useLineOfSight = true;
    [Tooltip("Layers whose colliders block every part of entity vision. SectorStructure can be used directly when structure colliders are authored as full-height sight walls.")]
    [SerializeField] private LayerMask _lineOfSightBlockerMask = 0;
    [Tooltip("Vertical offset from the EntityRange origin used by gameplay LOS and the player fog ray fan.")]
    [SerializeField, Min(0f)] private float _eyeHeight = 1f;
    [Tooltip("Vertical offset used when a target Transform has no explicit renderer or visibility point.")]
    [SerializeField, Min(0f)] private float _targetHeight = 0.8f;

    [Header("Forward Arc")]
    [Tooltip("If true, VisionRange is limited to a forward cone. CloseVisionRadius remains omnidirectional, but still requires line of sight.")]
    [SerializeField] private bool _useForwardArc = true;

    [Tooltip("Base forward cone angle in degrees.")]
    [SerializeField, Range(1f, 360f)] private float _baseArcAngle = 90f;

    [Tooltip("Forward cone multiplier at full aim. 0.5 means the cone becomes half as wide while fully aimed.")]
    [SerializeField, Range(0.05f, 1f)] private float _aimArcAngleMultiplier = 0.65f;

    // Kept for Unity serialization and the generic entity defaults.
    public EntityVisionSettings()
    {
    }

    /// <summary>
    /// Builds the single target-detection shape shared by every ordinary,
    /// guard, and boss Shooter enemy. Individual enemy configs still own
    /// scan cadence, target memory, and reaction timing.
    /// </summary>
    public static EntityVisionSettings CreateSharedEnemyShooterDetection()
    {
        return new EntityVisionSettings(
            visionRange: 70f,
            closeVisionRadius: 7.5f,
            useLineOfSight: true,
            lineOfSightBlockerMask: (1 << 7) | (1 << 11),
            eyeHeight: 1f,
            targetHeight: 1f,
            useForwardArc: true,
            baseArcAngle: 60f,
            aimArcAngleMultiplier: 1f);
    }

    private EntityVisionSettings(
        float visionRange,
        float closeVisionRadius,
        bool useLineOfSight,
        LayerMask lineOfSightBlockerMask,
        float eyeHeight,
        float targetHeight,
        bool useForwardArc,
        float baseArcAngle,
        float aimArcAngleMultiplier)
    {
        _visionRange = visionRange;
        _closeVisionRadius = closeVisionRadius;
        _useLineOfSight = useLineOfSight;
        _lineOfSightBlockerMask = lineOfSightBlockerMask;
        _eyeHeight = eyeHeight;
        _targetHeight = targetHeight;
        _useForwardArc = useForwardArc;
        _baseArcAngle = baseArcAngle;
        _aimArcAngleMultiplier = aimArcAngleMultiplier;
    }

    public float VisionRange => Mathf.Max(0.1f, _visionRange);
    public float CloseVisionRadius => Mathf.Max(0f, _closeVisionRadius);
    public bool UseLineOfSight => _useLineOfSight;
    public LayerMask LineOfSightBlockerMask => _lineOfSightBlockerMask;
    public float EyeHeight => Mathf.Max(0f, _eyeHeight);
    public float TargetHeight => Mathf.Max(0f, _targetHeight);
    public bool UseForwardArc => _useForwardArc;
    public float BaseArcAngle => Mathf.Clamp(_baseArcAngle, 1f, 360f);
    public float AimArcAngleMultiplier => Mathf.Clamp(_aimArcAngleMultiplier, 0.05f, 1f);
}

[Serializable]
public class EntityInfectionSettings
{
    [Header("Injection DOT Resistance")]
    [Tooltip("Multiplier applied to Infection Injection health damage over time. 1 means normal damage; 0.3 means 70% resistance. Use a lower value for high-difficulty infection enemies.")]
    [SerializeField, Range(0f, 1f)] private float _infectionDamageOverTimeMultiplier = 1f;

    [Header("Tick")]
    [SerializeField, Min(0.01f)] private float _zoneTickInterval = 0.2f;
    [SerializeField, Min(0.01f)] private float _naturalDecayInterval = 1f;

    [Header("Natural Decay")]
    [SerializeField, Min(0f)] private float _naturalDecayAmountPerTick = 0.5f;

    [Tooltip("Seconds without receiving HP or Infection damage before natural Infection recovery may resume.")]
    [SerializeField, Min(0f)] private float _infectionRecoveryTimeWait = 5f;

    public float ZoneTickInterval => Mathf.Max(0.01f, _zoneTickInterval);
    public float NaturalDecayInterval => Mathf.Max(0.01f, _naturalDecayInterval);
    public float NaturalDecayAmountPerTick => Mathf.Max(0f, _naturalDecayAmountPerTick);
    public float InfectionRecoveryTimeWait => Mathf.Max(0f, _infectionRecoveryTimeWait);
    public float InfectionDamageOverTimeMultiplier => Mathf.Clamp01(_infectionDamageOverTimeMultiplier);
}

[Serializable]
public class EntityArmorSettings
{
    [Header("Durability Loss")]
    [Tooltip("Multiplier applied to HP damage absorbed by armor. 1 means absorbed HP damage reduces armor durability 1:1.")]
    [SerializeField, Min(0f)] private float _healthDurabilityLossMultiplier = 1f;

    [Tooltip("Multiplier applied to infection damage absorbed by armor. 0.5 means absorbed infection damage reduces armor durability by half.")]
    [SerializeField, Min(0f)] private float _infectionDurabilityLossMultiplier = 0.5f;

    public float HealthDurabilityLossMultiplier => Mathf.Max(0f, _healthDurabilityLossMultiplier);
    public float InfectionDurabilityLossMultiplier => Mathf.Max(0f, _infectionDurabilityLossMultiplier);
}

public abstract class EntityStatConfigSO : ScriptableObject
{
    [Header("Entity Base")]
    [FormerlySerializedAs("maxHealth")]
    [FormerlySerializedAs("_maxHealth")]
    [SerializeField, Min(1f)] private float _initialHealth = 20f;

    [FormerlySerializedAs("moveSpeed")]
    [FormerlySerializedAs("_moveSpeed")]
    [FormerlySerializedAs("_normalMovementSpeed")]
    [SerializeField, Min(0f)] private float _moveSpeed = 2.2f;

    [Header("Armor Base")]
    [Tooltip("Base armor class owned by this entity before equipped armor and paint mark modifiers are applied.")]
    [SerializeField, Min(0)] private int _baseArmorClass = 0;

    [Header("Paint Mark")]
    [SerializeField] private EntityPaintMarkSettings _paintMark = new();

    [Header("Vision")]
    [SerializeField] private EntityVisionSettings _vision = new();

    [Header("Infection")]
    [SerializeField] private EntityInfectionSettings _infection = new();

    [Header("Armor")]
    [SerializeField] private EntityArmorSettings _armor = new();

    protected float RawInitialHealth => Mathf.Max(1f, _initialHealth);
    protected float RawMoveSpeed => Mathf.Max(0f, _moveSpeed);

    public virtual float InitialHealth => RawInitialHealth;
    public virtual float MaxHealth => InitialHealth;
    public virtual float MoveSpeed => RawMoveSpeed;
    public virtual float ReferenceMoveSpeed => MoveSpeed;
    public virtual int BaseArmorClass => Mathf.Max(0, _baseArmorClass);
    public float VisionRange => Vision != null ? Vision.VisionRange : 12f;

    public EntityPaintMarkSettings PaintMark => _paintMark;
    public virtual EntityVisionSettings Vision => _vision;
    public EntityInfectionSettings Infection => _infection;
    public EntityArmorSettings Armor => _armor;

    // Purpose: Resolves the initial health needed by this component.
    public virtual float ResolveInitialHealth()
    {
        return Mathf.Max(1f, InitialHealth);
    }

    // Purpose: Resolves the reference move speed needed by this component.
    public virtual float ResolveReferenceMoveSpeed()
    {
        return Mathf.Max(0.01f, ReferenceMoveSpeed);
    }
}
