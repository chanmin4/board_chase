/*
 * Role hierarchy: Gameplay > Entities > Common > StatusEffects
 * Primary responsibility: Implements the focused StatusEffectPolarity responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public enum StatusEffectPolarity
{
    // Harmful stays zero so existing Slow/Infection Recovery Down assets keep
    // their intended classification after this serialized field is introduced.
    Harmful = 0,
    Beneficial = 1,
    Neutral = 2
}

public enum StatusEffectIconKind
{
    Custom = 0,
    Slow = 1,
    InfectionRecoveryDown = 2,
    PaintCoated = 3,
    InfectionAggravation = 4
}

public enum StatusEffectModifierType
{
    MoveSpeedMultiplier = 0,
    InfectionRecoveryMultiplier = 1,
    SpreadMultiplier = 2,
    DamageTakenMultiplier = 3,
    VisionArcMultiplier = 4
}

[System.Serializable]
public struct StatusEffectModifier
{
    [Tooltip("Runtime stat affected by this definition.")]
    public StatusEffectModifierType type;

    [Tooltip("Multiplicative value. 0.7 means 70% movement/recovery speed. Different definitions multiply; the same definition never stacks.")]
    [Min(0f)] public float multiplier;
}

[CreateAssetMenu(
    fileName = "StatusEffect",
    menuName = "Game/Character/Status Effect")]
public class StatusEffectDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable identifier used by debug tools and future status encyclopedias.")]
    [SerializeField] private string _id = "status_effect";

    [Tooltip("Optional authored icon. When empty, the HUD creates a clear procedural icon from Icon Kind.")]
    [SerializeField] private Sprite _icon;

    [SerializeField] private StatusEffectIconKind _iconKind = StatusEffectIconKind.Custom;
    [SerializeField] private Color _iconColor = Color.white;

    [Tooltip("Gameplay classification used by UI, cleansing, and AI evaluation. This does not infer behavior from the icon or modifier values.")]
    [SerializeField] private StatusEffectPolarity _polarity = StatusEffectPolarity.Harmful;

    [Tooltip("Player-facing explanation shown in the Stats status panel. Leave empty to use the generated modifier summary.")]
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    [Header("Lifetime")]
    [Tooltip("Duration after the last application. Reapplying refreshes this timer; it never adds a stack.")]
    [SerializeField, Min(0.05f)] private float _durationSeconds = 1.25f;

    [Tooltip("Persistent treatment-only effect. Reapplication adds stacks instead of refreshing a timer.")]
    [SerializeField] private bool _persistentStacks;
    [SerializeField, Min(1)] private int _maximumStacks = 10;
    [SerializeField, Min(0)] private float _damagePerStackPerSecond;
    [SerializeField, Min(.1f)] private float _damageTickInterval = 1f;
    [SerializeField] private bool _isBleeding;
    public virtual bool PersistentStacks => _persistentStacks;
    public virtual bool DealsInfectionDamage => false;
    public int MaximumStacks => Mathf.Max(1, _maximumStacks);
    public float DamagePerStackPerSecond => Mathf.Max(0f, _damagePerStackPerSecond);
    public float DamageTickInterval => Mathf.Max(.1f, _damageTickInterval);
    public bool IsBleeding => _isBleeding;

    [Tooltip("Friendly paint removes this effect immediately instead of waiting for its timer.")]
    [SerializeField] private bool _removeOnFriendlyPaint = true;

    [Header("Modifiers")]
    [SerializeField] private StatusEffectModifier[] _modifiers = System.Array.Empty<StatusEffectModifier>();

    [Header("Action Restrictions")]
    [Tooltip("When enabled, the affected entity cannot move, fire, aim, dash, interact, or use items while this effect is active.")]
    [SerializeField] private bool _blocksActions;

    [Tooltip("When enabled on the player, world sound-direction indicators are hidden until this effect ends.")]
    [SerializeField] private bool _suppressesSoundIndicators;

    [Tooltip("Marks this effect as a bullet injury. Painkillers use this flag to remove every active injury at once.")]
    [SerializeField] private bool _isInjury;

    [Tooltip("When enabled on the player, enemy bullet hits do not roll new injury effects while this status remains active.")]
    [SerializeField] private bool _suppressesInjuryApplications;

    [Tooltip("Flat Armor Class adjustment while active. Use -1 for a critical injury that weakens armor without modifying the equipped item.")]
    [SerializeField] private int _armorClassDelta;

    [Header("Optional World VFX")]
    [Tooltip("Optional looping VFX parented to the affected Shooter while this status is active.")]
    [SerializeField] private GameObject _worldVfxPrefab;

    public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
    public Sprite Icon => _icon;
    public StatusEffectIconKind IconKind => _iconKind;
    public Color IconColor => _iconColor;
    public StatusEffectPolarity Polarity => _polarity;
    public string Description => _description;
    public bool IsBuff => _polarity == StatusEffectPolarity.Beneficial;
    public bool IsDebuff => _polarity == StatusEffectPolarity.Harmful;
    public float DurationSeconds => Mathf.Max(0.05f, _durationSeconds);
    public bool RemoveOnFriendlyPaint => _removeOnFriendlyPaint;
    public StatusEffectModifier[] Modifiers => _modifiers;
    public bool BlocksActions => _blocksActions;
    public bool SuppressesSoundIndicators => _suppressesSoundIndicators;
    public bool IsInjury => _isInjury;
    public bool SuppressesInjuryApplications => _suppressesInjuryApplications;
    public int ArmorClassDelta => _armorClassDelta;
    public GameObject WorldVfxPrefab => _worldVfxPrefab;
}
