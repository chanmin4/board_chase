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
    [Tooltip("Inventory slots supplied while this bag is worn. Pocket slots are separate and always remain available.")]
    [SerializeField, Min(0)] private int _inventorySlotCapacity = 8;

    [Header("Mobility")]
    [Tooltip("Final player movement multiplier while this bag is worn. 1 means no penalty; 0.8 means 20% slower.")]
    [SerializeField, Range(0.05f, 1f)] private float _moveSpeedMultiplier = 0.9f;

    public string BackpackId => string.IsNullOrWhiteSpace(_backpackId) ? name : _backpackId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? BackpackId : _displayName;
    public int InventorySlotCapacity => Mathf.Max(0, _inventorySlotCapacity);
    public float MoveSpeedMultiplier => Mathf.Clamp(_moveSpeedMultiplier, 0.05f, 1f);
    public override ItemKind Kind => ItemKind.Backpack;
    public override int MaxStack => 1;
}
