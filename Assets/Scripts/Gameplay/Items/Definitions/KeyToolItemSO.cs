/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines the physical container dimensions used by
 * one carried Key Tool inventory item.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "KeyToolItem",
    menuName = "Game/Character Item/Equipment/Key Tool")]
public sealed class KeyToolItemSO : ItemSO
{
    [Header("Identity")]
    [Tooltip("Stable content ID used by diagnostics and future ES3 run snapshots.")]
    [SerializeField] private string _keyToolId = "basic_key_tool";

    [Tooltip("Fallback display name used until localized Key Tool text is authored.")]
    [SerializeField] private string _displayName = "Basic Key Tool";

    [Header("Key Tool Storage")]
    [Tooltip("Total physical key slots inside this carried Key Tool. The Basic Key Tool starts as a 2 x 2 container.")]
    [SerializeField, Min(1)] private int _slotCapacity = 4;

    [Tooltip("Visible column count used by the Key Tool container panel.")]
    [SerializeField, Min(1)] private int _columnCount = 2;

    public string KeyToolId => string.IsNullOrWhiteSpace(_keyToolId) ? name : _keyToolId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? KeyToolId : _displayName;
    public int SlotCapacity => Mathf.Max(1, _slotCapacity);
    public int ColumnCount => Mathf.Clamp(_columnCount, 1, SlotCapacity);
    public override ItemKind Kind => ItemKind.KeyTool;
    public override int MaxStack => 1;
    public override int MaxOwnedAmount => 1;
}
