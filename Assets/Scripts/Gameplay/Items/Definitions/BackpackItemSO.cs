/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines a portable inventory container and its movement penalty.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "BackpackItem",
    menuName = "Game/Character Item/Equipment/Backpack")]
public class BackpackItemSO : ItemSO
{
    [Header("Identity")]
    [SerializeField] private string _backpackId = "backpack";
    [SerializeField] private string _displayName = "Backpack";

    [Header("Storage")]
    [Tooltip("추가 인벤토리 칸수. 캐릭터 Config의 기본 인벤토리 칸수에 더합니다. 포켓·장착·패시브 슬롯은 별도입니다.")]
    [InspectorName("추가 인벤토리 칸수")]
    [SerializeField, Min(0)] private int _inventorySlotCapacity = 8;

    [Header("Mobility")]
    [SerializeField, Min(0)] private float _carryCapacityBonusKg;
    public float CarryCapacityBonusKg => Mathf.Max(0,_carryCapacityBonusKg);
    [Tooltip("Final character movement multiplier while this bag is worn. Shared by player and future enemy backpack equipment. 1 means no penalty; 0.8 means 20% slower.")]
    [SerializeField, Range(0.05f, 1f)] private float _moveSpeedMultiplier = 0.9f;

    [Header("Backpack Equipment Visual")]
    [Tooltip("착용할 때 등 기준점에 표시할 모델. 월드 드롭/인벤토리와 별도이며 Shooter Customize에서 배치를 저장합니다.")]
    [SerializeField] private GameObject _equippedVisualPrefab;
    [SerializeField] private ShooterVisualLocalTransform _equippedVisualTransform = new ShooterVisualLocalTransform(Vector3.zero, Vector3.zero, Vector3.one);
    [SerializeField] private ShooterVisualOccupancy _visualCoverage = ShooterVisualOccupancy.Back;

    public string BackpackId => string.IsNullOrWhiteSpace(_backpackId) ? name : _backpackId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? BackpackId : _displayName;
    public int InventorySlotCapacity => Mathf.Max(0, _inventorySlotCapacity);
    public float MoveSpeedMultiplier => Mathf.Clamp(_moveSpeedMultiplier, 0.05f, 1f);
    public override ItemKind Kind => ItemKind.Backpack;
    public override int MaxStack => 1;
    public GameObject EquippedVisualPrefab => _equippedVisualPrefab;
    public ShooterVisualLocalTransform EquippedVisualTransform => _equippedVisualTransform;
    public ShooterVisualOccupancy VisualCoverage => _equippedVisualPrefab == null ? ShooterVisualOccupancy.None : _visualCoverage;

    /// <summary>시각 배치만 변경한다. 수납 용량·이동 페널티·아이템 소유 상태는 건드리지 않는다.</summary>
    public void SetEquippedVisual(GameObject prefab, ShooterVisualLocalTransform pose)
    { _equippedVisualPrefab = prefab; _equippedVisualTransform = pose; }

    public void ApplyEquippedVisualTransform(Transform target) => _equippedVisualTransform.ApplyTo(target);
}
