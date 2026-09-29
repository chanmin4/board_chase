/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Defines the read-only diagnostics boundary for PlayerInventoryRuntime.
 * Execution boundary: Runtime (Assembly-CSharp)
 *
 * This partial owns no item-transfer rules. It exists so Debug Monitor and
 * future inventory extractions can inspect the ownership model without adding
 * new dependencies on PlayerInventoryRuntime's internal serialized fields.
 */

public readonly struct PlayerInventoryRuntimeDiagnosticsSnapshot
{
    public PlayerInventoryRuntimeDiagnosticsSnapshot(
        int capacity,
        int usedSlots,
        int freeSlots,
        int stackEntryCount,
        bool hasEquipmentRuntime,
        bool hasQuickSlotRuntime,
        bool hasAmmoLoadoutCompatibility,
        bool startingLoadoutApplied)
    {
        Capacity = capacity;
        UsedSlots = usedSlots;
        FreeSlots = freeSlots;
        StackEntryCount = stackEntryCount;
        HasEquipmentRuntime = hasEquipmentRuntime;
        HasQuickSlotRuntime = hasQuickSlotRuntime;
        HasAmmoLoadoutCompatibility = hasAmmoLoadoutCompatibility;
        StartingLoadoutApplied = startingLoadoutApplied;
    }

    public int Capacity { get; }
    public int UsedSlots { get; }
    public int FreeSlots { get; }
    public int StackEntryCount { get; }
    public bool HasEquipmentRuntime { get; }
    public bool HasQuickSlotRuntime { get; }
    public bool HasAmmoLoadoutCompatibility { get; }
    public bool StartingLoadoutApplied { get; }
}

public partial class PlayerInventoryRuntime
{
    /// <summary>
    /// Read-only state for authoring/debug tooling. This intentionally does not
    /// call ResolveRefs, mutate inventory, or alter virtual-magazine state.
    /// </summary>
    public PlayerInventoryRuntimeDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        return new PlayerInventoryRuntimeDiagnosticsSnapshot(
            Capacity,
            UsedSlots,
            FreeSlots,
            _items != null ? _items.Count : 0,
            _equipmentRuntime != null,
            _quickSlotRuntime != null,
            _bulletLoadout != null,
            _startingLoadoutInventoryApplied);
    }
}
