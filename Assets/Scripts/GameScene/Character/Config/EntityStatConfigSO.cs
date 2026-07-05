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

    [Header("Paint Tick")]
    [SerializeField, Min(0.02f)] private float _paintMarkTickInterval = 0.1f;

    [Header("Paint Gain")]
    [SerializeField, Min(0f)] private float _vaccinePaintMarkAmountPerTick = 5f;
    [SerializeField, Min(0f)] private float _virusPaintMarkAmountPerTick = 5f;

    [Header("Entity Damaged Player")]
    [SerializeField, Min(0f)] private float _virusMarkAmountOnDamagingPlayer = 20f;

    [Header("Decay")]
    [SerializeField, Min(0f)] private float _inactiveSlowDecayAmountPerTick = 0f;
    [SerializeField, Min(0f)] private float _activeSlowDecayAmountPerTick = 0.5f;
    [SerializeField, Min(0f)] private float _oppositePaintFastDecayAmountPerTick = 1f;

    [Header("Damage Taken Additive")]
    [SerializeField] private float _vaccineDamageTakenAddPerStack = 0.15f;
    [SerializeField] private float _virusDamageTakenAddPerStack = -0.15f;

    [Header("Armor Class Modifier")]
    [SerializeField] private int _vaccineArmorClassDeltaPerStack = -1;
    [SerializeField] private int _virusArmorClassDeltaPerStack = 1;

    public bool UsePaintMark => _usePaintMark;
    public float MarkAmountPerStack => Mathf.Max(1f, _markAmountPerStack);
    public int MaxVaccineMarkStacks => Mathf.Max(1, _maxVaccineMarkStacks);
    public int MaxVirusMarkStacks => Mathf.Max(1, _maxVirusMarkStacks);
    public float MaxVaccineMarkAmount => MarkAmountPerStack * MaxVaccineMarkStacks;
    public float MaxVirusMarkAmount => MarkAmountPerStack * MaxVirusMarkStacks;
    public bool StartWithVirusMark => _startWithVirusMark;
    public float StartingVirusMarkAmount => Mathf.Max(0f, _startingVirusMarkAmount);
    public float PaintMarkTickInterval => Mathf.Max(0.02f, _paintMarkTickInterval);
    public float VaccinePaintMarkAmountPerTick => Mathf.Max(0f, _vaccinePaintMarkAmountPerTick);
    public float VirusPaintMarkAmountPerTick => Mathf.Max(0f, _virusPaintMarkAmountPerTick);
    public float VirusMarkAmountOnDamagingPlayer => Mathf.Max(0f, _virusMarkAmountOnDamagingPlayer);
    public float InactiveSlowDecayAmountPerTick => Mathf.Max(0f, _inactiveSlowDecayAmountPerTick);
    public float ActiveSlowDecayAmountPerTick => Mathf.Max(0f, _activeSlowDecayAmountPerTick);
    public float OppositePaintFastDecayAmountPerTick => Mathf.Max(0f, _oppositePaintFastDecayAmountPerTick);
    public float VaccineDamageTakenAddPerStack => _vaccineDamageTakenAddPerStack;
    public float VirusDamageTakenAddPerStack => _virusDamageTakenAddPerStack;
    public int VaccineArmorClassDeltaPerStack => _vaccineArmorClassDeltaPerStack;
    public int VirusArmorClassDeltaPerStack => _virusArmorClassDeltaPerStack;
}

[Serializable]
public class EntityVisionSettings
{
    [Header("Range")]
    [SerializeField, Min(0.1f)] private float _visionRange = 12f;

    [Header("Line Of Sight")]
    [SerializeField] private bool _useLineOfSight = true;
    [SerializeField] private LayerMask _lineOfSightBlockerMask = 0;
    [SerializeField, Min(0f)] private float _eyeHeight = 1f;
    [SerializeField, Min(0f)] private float _targetHeight = 0.8f;

    [Header("Forward Arc")]
    [Tooltip("If true, long vision is limited to a forward cone. Close range still uses weapon max range as a circle.")]
    [SerializeField] private bool _useForwardArc = true;

    [SerializeField, Range(1f, 360f)] private float _baseArcAngle = 90f;

    [Tooltip("Extra cone angle added by VisionRange. Final angle = Base Arc Angle + VisionRange * this value.")]
    [SerializeField, Min(0f)] private float _arcAnglePerVisionRange = 0f;

    [Tooltip("Forward cone multiplier at full aim. 0.5 means the cone becomes half as wide while fully aimed.")]
    [SerializeField, Range(0.05f, 1f)] private float _aimArcAngleMultiplier = 0.65f;

    public float VisionRange => Mathf.Max(0.1f, _visionRange);
    public bool UseLineOfSight => _useLineOfSight;
    public LayerMask LineOfSightBlockerMask => _lineOfSightBlockerMask;
    public float EyeHeight => Mathf.Max(0f, _eyeHeight);
    public float TargetHeight => Mathf.Max(0f, _targetHeight);
    public bool UseForwardArc => _useForwardArc;
    public float BaseArcAngle => Mathf.Clamp(_baseArcAngle, 1f, 360f);
    public float ArcAnglePerVisionRange => Mathf.Max(0f, _arcAnglePerVisionRange);
    public float AimArcAngleMultiplier => Mathf.Clamp(_aimArcAngleMultiplier, 0.05f, 1f);

    public float ResolveArcAngle(float visionRange)
    {
        return Mathf.Clamp(BaseArcAngle + Mathf.Max(0f, visionRange) * ArcAnglePerVisionRange, 1f, 360f);
    }
}

[Serializable]
public class EntityInfectionSettings
{
    [Header("Tick")]
    [SerializeField, Min(0.01f)] private float _zoneTickInterval = 0.2f;
    [SerializeField, Min(0.01f)] private float _naturalDecayInterval = 1f;

    [Header("Zone")]
    [SerializeField, Min(0f)] private float _virusZoneInfectionGainPerTick = 1f;
    [SerializeField, Min(0f)] private float _vaccineZoneRecoverPerTick = 1f;

    [Header("Natural Decay")]
    [SerializeField, Min(0f)] private float _naturalDecayAmountPerTick = 0f;

    public float ZoneTickInterval => Mathf.Max(0.01f, _zoneTickInterval);
    public float NaturalDecayInterval => Mathf.Max(0.01f, _naturalDecayInterval);
    public float VirusZoneInfectionGainPerTick => Mathf.Max(0f, _virusZoneInfectionGainPerTick);
    public float VaccineZoneRecoverPerTick => Mathf.Max(0f, _vaccineZoneRecoverPerTick);
    public float NaturalDecayAmountPerTick => Mathf.Max(0f, _naturalDecayAmountPerTick);
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
    public EntityVisionSettings Vision => _vision;
    public EntityInfectionSettings Infection => _infection;
    public EntityArmorSettings Armor => _armor;

    public virtual float ResolveInitialHealth()
    {
        return Mathf.Max(1f, InitialHealth);
    }

    public virtual float ResolveReferenceMoveSpeed()
    {
        return Mathf.Max(0.01f, ReferenceMoveSpeed);
    }
}
