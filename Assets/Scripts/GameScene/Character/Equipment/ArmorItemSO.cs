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
    [SerializeField] private float _damageTakenMultiplierAdditive = -0.1f;

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
    public float DamageTakenMultiplierAdditive => _damageTakenMultiplierAdditive;
    public PlayerStatModifier[] StatModifiers => _statModifiers;

    public GameObject EquippedVisualPrefab => _equippedVisualPrefab;

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