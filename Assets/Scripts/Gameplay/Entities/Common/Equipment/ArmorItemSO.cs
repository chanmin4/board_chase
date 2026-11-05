/*
 * Role hierarchy: Gameplay > Entities > Common > Equipment
 * Primary responsibility: Defines the ArmorItemSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "ArmorItem",
    menuName = "Game/Character Item/Equipment/Armor")]
public class ArmorItemSO : ItemSO
{
    [Header("Identity")]
    [SerializeField] private string _armorId = "armor";
    [SerializeField] private string _displayName = "Armor";

    [Header("Defense")]
    [SerializeField, Min(0)] private int _armorClass = 1;

    [Header("Durability")]
    [Tooltip("방탄복이 흡수할 수 있는 피해량입니다. 체력 피해 1을 흡수하면 내구도 1이 소모됩니다. 신품 기본값은 1000입니다.")]
    [SerializeField, Min(0f)] private float _maxDurability = 1000f;

    [Tooltip("Fraction of missing durability permanently removed from this armor's instance maximum when it is repaired. 0 keeps the maximum unchanged.")]
    [SerializeField, Range(0f, 1f)] private float _repairPermanentLossRatio = 0.1f;

    [Tooltip("Health damage multiplier when bullet penetration class is equal to or higher than armor class.")]
    [SerializeField, Min(0f)] private float _sameOrHigherPenetrationHealthDamageMultiplier = 1f;

    [Tooltip("Legacy data retained for existing assets. Direct health damage now uses ArmorPenetrationDamageCurve: a 10% floor with a random roll from the curve minimum to full damage.")]
    [SerializeField, HideInInspector, Range(0f, 1f)] private float _healthDamageMultiplierPerClassGap = 0.5f;

    [Tooltip("Paint mark/infection multiplier when bullet penetration class is equal to or higher than armor class.")]
    [SerializeField, Min(0f)] private float _sameOrHigherPenetrationMarkDamageMultiplier = 1f;

    [Tooltip("Paint mark/infection multiplier applied for each armor class above bullet penetration. 0.75 gives 42.1875% at 3 class gap.")]
    [SerializeField, Range(0f, 1f)] private float _markDamageMultiplierPerClassGap = 0.75f;

    [Header("Visual Placement")]
     [Tooltip("equipped view prefab need to ref with world item prefab")]
    [SerializeField] private GameObject _equippedVisualPrefab;
    [SerializeField] private Vector3 _equippedVisualLocalPosition;
    [SerializeField] private Vector3 _equippedVisualLocalEulerAngles;
    [SerializeField] private Vector3 _equippedVisualLocalScale = Vector3.one;

    [Tooltip("Visible regions occupied by this armor. Armor visuals default to the torso so face accessories remain independent.")]
    [SerializeField] private ShooterVisualOccupancy _visualCoverage = ShooterVisualOccupancy.Torso;

    [Header("Stat Modifiers")]
    [SerializeField] private PlayerStatModifier[] _statModifiers;

    public string ArmorId => _armorId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _armorId : _displayName;

    public int ArmorClass => Mathf.Max(0, _armorClass);
    public float MaxDurability => Mathf.Max(0f, _maxDurability);
    public float RepairPermanentLossRatio => Mathf.Clamp01(_repairPermanentLossRatio);
    public PlayerStatModifier[] StatModifiers => _statModifiers;
    public override ItemKind Kind => ItemKind.Armor;

    public GameObject EquippedVisualPrefab => _equippedVisualPrefab;
    public ShooterVisualOccupancy VisualCoverage => _equippedVisualPrefab == null
        ? ShooterVisualOccupancy.None
        : _visualCoverage == ShooterVisualOccupancy.None
            ? ShooterVisualOccupancy.Torso
            : _visualCoverage;

    // Purpose: Resolves the health damage multiplier needed by this component.
    public float ResolveHealthDamageMultiplier(float penetrationClass, float baseArmorClass, float armorClassDelta)
    {
        float effectiveArmorClass = Mathf.Max(0f, baseArmorClass + armorClassDelta);
        return Mathf.Max(
            0f,
            _sameOrHigherPenetrationHealthDamageMultiplier *
            ArmorPenetrationDamageCurve.RollMultiplier(ArmorClass, penetrationClass) *
            ArmorPenetrationDamageCurve.RollMultiplier(
                effectiveArmorClass,
                penetrationClass));
    }

    // Purpose: Resolves the mark damage multiplier needed by this component.
    public float ResolveMarkDamageMultiplier(float penetrationClass, float baseArmorClass, float armorClassDelta)
    {
        float effectiveArmorClass = Mathf.Max(0f, baseArmorClass + ArmorClass + armorClassDelta);
        float classGap = Mathf.Max(0f, effectiveArmorClass - Mathf.Max(0, penetrationClass));

        return Mathf.Max(
            0f,
            _sameOrHigherPenetrationMarkDamageMultiplier *
            Mathf.Pow(_markDamageMultiplierPerClassGap, classGap));
    }

    // Purpose: Applies the requested equipped visual transform change.
    public void ApplyEquippedVisualTransform(Transform visualTransform)
    {
        if (visualTransform == null)
            return;

        visualTransform.localPosition = _equippedVisualLocalPosition;
        visualTransform.localRotation = Quaternion.Euler(_equippedVisualLocalEulerAngles);
        visualTransform.localScale = SanitizeScale(_equippedVisualLocalScale);
    }

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        // Retain and sanitize legacy serialized data so upgrading existing
        // armor assets never changes their YAML shape unexpectedly.
        _healthDamageMultiplierPerClassGap =
            Mathf.Clamp01(_healthDamageMultiplierPerClassGap);
    }

    // Purpose: Normalizes scale to a safe authored value.
    private static Vector3 SanitizeScale(Vector3 scale)
    {
        const float min = 0.0001f;

        return new Vector3(
            Mathf.Abs(scale.x) < min ? 1f : scale.x,
            Mathf.Abs(scale.y) < min ? 1f : scale.y,
            Mathf.Abs(scale.z) < min ? 1f : scale.z);
    }
}
