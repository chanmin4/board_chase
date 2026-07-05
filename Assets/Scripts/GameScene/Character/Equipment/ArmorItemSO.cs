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
    [Tooltip("Runtime armor durability when equipped. Absorbed damage reduces this value.")]
    [SerializeField, Min(0f)] private float _maxDurability = 100f;

    [Tooltip("Health damage multiplier when bullet penetration class is equal to or higher than armor class.")]
    [SerializeField, Min(0f)] private float _sameOrHigherPenetrationHealthDamageMultiplier = 1f;

    [Tooltip("Health damage multiplier applied for each armor class above bullet penetration. 0.5 means one class gap halves damage.")]
    [SerializeField, Range(0f, 1f)] private float _healthDamageMultiplierPerClassGap = 0.5f;

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

    [Header("Stat Modifiers")]
    [SerializeField] private PlayerStatModifier[] _statModifiers;

    public string ArmorId => _armorId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _armorId : _displayName;

    public int ArmorClass => Mathf.Max(0, _armorClass);
    public float MaxDurability => Mathf.Max(0f, _maxDurability);
    public PlayerStatModifier[] StatModifiers => _statModifiers;

    public GameObject EquippedVisualPrefab => _equippedVisualPrefab;

    public float ResolveHealthDamageMultiplier(int penetrationClass, int baseArmorClass, int armorClassDelta)
    {
        int effectiveArmorClass = Mathf.Max(0, baseArmorClass + ArmorClass + armorClassDelta);
        int classGap = Mathf.Max(0, effectiveArmorClass - Mathf.Max(0, penetrationClass));

        return Mathf.Max(
            0f,
            _sameOrHigherPenetrationHealthDamageMultiplier *
            Mathf.Pow(_healthDamageMultiplierPerClassGap, classGap));
    }

    public float ResolveMarkDamageMultiplier(int penetrationClass, int baseArmorClass, int armorClassDelta)
    {
        int effectiveArmorClass = Mathf.Max(0, baseArmorClass + ArmorClass + armorClassDelta);
        int classGap = Mathf.Max(0, effectiveArmorClass - Mathf.Max(0, penetrationClass));

        return Mathf.Max(
            0f,
            _sameOrHigherPenetrationMarkDamageMultiplier *
            Mathf.Pow(_markDamageMultiplierPerClassGap, classGap));
    }

    public void ApplyEquippedVisualTransform(Transform visualTransform)
    {
        if (visualTransform == null)
            return;

        visualTransform.localPosition = _equippedVisualLocalPosition;
        visualTransform.localRotation = Quaternion.Euler(_equippedVisualLocalEulerAngles);
        visualTransform.localScale = SanitizeScale(_equippedVisualLocalScale);
    }

    private static Vector3 SanitizeScale(Vector3 scale)
    {
        const float min = 0.0001f;

        return new Vector3(
            Mathf.Abs(scale.x) < min ? 1f : scale.x,
            Mathf.Abs(scale.y) < min ? 1f : scale.y,
            Mathf.Abs(scale.z) < min ? 1f : scale.z);
    }
}
