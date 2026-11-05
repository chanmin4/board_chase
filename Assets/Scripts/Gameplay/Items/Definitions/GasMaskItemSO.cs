/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines a head-equipment gas mask that mitigates
 * biomechanical room-gas Infection while it still has filter durability.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public enum GasMaskTier
{
    Normal = 0,
    Rare = 1,
    Unique = 2,
    Legendary = 3
}

[CreateAssetMenu(
    fileName = "GasMaskItem",
    menuName = "Game/Character Item/Equipment/Gas Mask")]
public sealed class GasMaskItemSO : ItemSO
{
    [Header("Identity")]
    [SerializeField] private string _gasMaskId = "gas_mask";
    [SerializeField] private string _displayName = "Gas Mask";
    [SerializeField] private GasMaskTier _tier = GasMaskTier.Normal;

    [Header("Biochemical Gas")]
    [SerializeField, Min(0)] private int _gasDefenseLevel = 1;
    public int GasDefenseLevel => Mathf.Max(0, _gasDefenseLevel);
    [SerializeField, Min(0f)] private float _infectionCapacityBonusPerDefenseLevel = 25f;
    public float InfectionCapacityBonus => GasDefenseLevel * Mathf.Max(0f, _infectionCapacityBonusPerDefenseLevel);
    [Tooltip("Final multiplier applied to room-gas Infection while the filter has durability. Lower is better.")]
    [SerializeField, Range(0.01f, 1f)] private float _gasInfectionMultiplier = 0.65f;

    [Tooltip("Filter durability available to absorb gas. At zero this mask remains equipped but offers no mitigation.")]
    [SerializeField, Min(1f)] private float _maxFilterDurability = 100f;

    [Tooltip("Filter durability consumed for each point of raw room-gas Infection exposure.")]
    [SerializeField, Min(0.001f)] private float _filterDurabilityLossPerRawInfection = 1f;

    [Header("Repair")]
    [Tooltip("Fraction of missing filter durability permanently removed from this mask instance's maximum when repaired. 0 keeps the maximum unchanged.")]
    [SerializeField, Range(0f, 1f)] private float _repairPermanentLossRatio = 0.1f;

    [Header("Mask Equipment Visual")]
    [Tooltip("Optional visual instantiated under the owner's FaceSocket or MouthSocket while this gas mask is equipped. This is gameplay equipment, not a permanent Shooter Customize cosmetic.")]
    [SerializeField] private GameObject _equippedVisualPrefab;

    [Tooltip("Face anchors keep full-face masks separate from mouth-only respirators and eye-only accessories.")]
    [SerializeField] private ShooterVisualSocketId _equippedVisualSocket = ShooterVisualSocketId.Face;

    [Tooltip("Visible regions claimed while this item is equipped. Face-slot masks always claim Eyes + Mouth; respirators use Mouth only.")]
    [SerializeField] private ShooterVisualOccupancy _visualCoverage =
        ShooterVisualOccupancy.None;

    [Tooltip("Local placement below the selected FaceSocket or MouthSocket. Author this in Shooter Customize Station with the Equipment Preview controls.")]
    [SerializeField] private ShooterVisualLocalTransform _equippedVisualTransform = default;

    public string GasMaskId => string.IsNullOrWhiteSpace(_gasMaskId) ? name : _gasMaskId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? GasMaskId : _displayName;
    public GasMaskTier Tier => _tier;
    public float GasInfectionMultiplier => Mathf.Clamp(_gasInfectionMultiplier, 0.01f, 1f);
    public float MaxFilterDurability => Mathf.Max(1f, _maxFilterDurability);
    public float FilterDurabilityLossPerRawInfection => Mathf.Max(0.001f, _filterDurabilityLossPerRawInfection);
    public float RepairPermanentLossRatio => Mathf.Clamp01(_repairPermanentLossRatio);
    public GameObject EquippedVisualPrefab => _equippedVisualPrefab;
    public ShooterVisualSocketId EquippedVisualSocket =>
        _equippedVisualSocket == ShooterVisualSocketId.Armor
            ? ShooterVisualSocketId.Mask
            : _equippedVisualSocket;
    public ShooterVisualOccupancy VisualCoverage
    {
        get
        {
            if (_equippedVisualPrefab == null)
                return ShooterVisualOccupancy.None;

            if (EquippedVisualSocket == ShooterVisualSocketId.Face)
                return _visualCoverage | ShooterVisualOccupancy.Eyes | ShooterVisualOccupancy.Mouth;

            if (_visualCoverage != ShooterVisualOccupancy.None)
                return _visualCoverage;

            return EquippedVisualSocket switch
            {
                ShooterVisualSocketId.Mouth => ShooterVisualOccupancy.Mouth,
                ShooterVisualSocketId.Eyes => ShooterVisualOccupancy.Eyes,
                _ => ShooterVisualOccupancy.None
            };
        }
    }
    public ShooterVisualLocalTransform EquippedVisualTransform => _equippedVisualTransform;
    public override ItemKind Kind => ItemKind.GasMask;
    public override int MaxStack => 1;

    // Purpose: Applies the authored mask-equipment visual placement to an instantiated gas-mask view.
    public void ApplyEquippedVisualTransform(Transform target)
    {
        _equippedVisualTransform.ApplyTo(target);
    }

    // Purpose: Updates the optional runtime mask-equipment visual prefab without changing gas mitigation data.
    public void SetEquippedVisualPrefab(GameObject prefab)
    {
        _equippedVisualPrefab = prefab;
    }

    // Purpose: Defines which face anchor and visible regions this equipment occupies.
    public void SetVisualOccupancy(
        ShooterVisualSocketId socket,
        ShooterVisualOccupancy coverage)
    {
        _equippedVisualSocket = socket;
        _visualCoverage = coverage;
    }

    // Purpose: Updates the local FaceSocket or MouthSocket placement captured by Shooter Customize Station.
    public void SetEquippedVisualTransform(ShooterVisualLocalTransform transform)
    {
        _equippedVisualTransform = transform;
    }
}
