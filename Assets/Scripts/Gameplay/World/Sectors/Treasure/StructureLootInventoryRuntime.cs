/*
 * Role hierarchy: Gameplay > World > Sectors > Treasure
 * Primary responsibility: Manages StructureLootInventoryRuntime item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Reusable world-structure loot inventory. It shares the LootHub transfer
/// behavior with corpses, but intentionally exposes inventory slots only.
/// </summary>
[DisallowMultipleComponent]
public sealed class StructureLootInventoryRuntime : EnemyLootInventoryRuntime
{
    // LootableObject or SectorTreasure supplies this at runtime from its owning
    // authoring asset. Keeping it non-serialized prevents duplicate prefab data.
    private string _runtimeDisplayName;
    private int _runtimeSlotCount = 1;

    public override string LootDisplayName =>
        string.IsNullOrWhiteSpace(_runtimeDisplayName) ? name : _runtimeDisplayName;
    public override bool ShowsEquipment => false;
    public override bool CanInteract => isActiveAndEnabled;
    public int RuntimeSlotCount => Mathf.Max(1, _runtimeSlotCount);

    // Purpose: Configures this component''s configure.
    public void Configure(
        string displayName,
        int slotCount,
        EnemyLootOpenRequestEventChannelSO openRequestChannel,
        UIOverlayRequestEventChannelSO overlayRequestChannel)
    {
        _runtimeDisplayName = displayName;
        _runtimeSlotCount = Mathf.Max(1, slotCount);
        ConfigureInventoryCapacity(_runtimeSlotCount);
        ConfigureInteractionChannels(openRequestChannel, overlayRequestChannel);
    }
}
