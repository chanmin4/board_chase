/*
 * Role hierarchy: Gameplay > Items > World
 * Primary responsibility: Preserves an equipped backpack and its cargo while it exists as a world pickup.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BackpackWorldPickupPayload : MonoBehaviour
{
    [SerializeField, ReadOnly] private BackpackItemSO _backpack;
    [SerializeField, ReadOnly] private List<PlayerInventoryItemStack> _cargo = new();

    public BackpackItemSO Backpack => _backpack;
    public IReadOnlyList<PlayerInventoryItemStack> Cargo => _cargo;

    // Purpose: Copies the detached backpack contents into a world-owned pickup payload.
    public void Initialize(BackpackItemSO backpack, IReadOnlyList<PlayerInventoryItemStack> cargo)
    {
        _backpack = backpack;
        _cargo.Clear();

        if (cargo == null)
            return;

        for (int index = 0; index < cargo.Count; index++)
        {
            PlayerInventoryItemStack source = cargo[index];

            if (source == null || source.IsEmpty)
                continue;

            var copy = new PlayerInventoryItemStack();
            copy.Set(
                source.Item,
                source.Amount,
                source.ArmorDurability,
                source.WeaponCurrentDurability,
                source.WeaponMaxDurability,
                source.GasMaskFilterDurability,
                source.ArmorMaxDurability,
                source.GasMaskMaxFilterDurability);
            _cargo.Add(copy);
        }
    }

    // Purpose: Atomically equips the stored backpack and returns all preserved cargo to the player.
    public bool TryEquipInto(PlayerInventoryRuntime inventory, out string message)
    {
        if (inventory == null)
        {
            message = "Player inventory runtime is missing.";
            return false;
        }

        return inventory.TryEquipBackpackFromWorld(_backpack, _cargo, out message);
    }
}
