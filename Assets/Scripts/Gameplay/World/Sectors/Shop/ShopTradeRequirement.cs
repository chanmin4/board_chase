/*
 * Role hierarchy: Gameplay > World > Sectors > Shop
 * Primary responsibility: Describes one physical ItemSO payment needed by a shop offer.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

/// <summary>
/// One required inventory item for a shop trade. The same representation is
/// used by authored shop products, runtime offers, and run-save snapshots.
/// </summary>
[Serializable]
public sealed class ShopTradeRequirement
{
    [Tooltip("Any ItemSO can be used here, including a physical currency item.")]
    [SerializeField] private ItemSO _item;

    [Min(1)]
    [SerializeField] private int _amount = 1;

    public ItemSO Item => _item;
    public int Amount => Mathf.Max(1, _amount);
    public bool IsValid => _item != null && _amount > 0;

    public ShopTradeRequirement(ItemSO item, int amount)
    {
        _item = item;
        _amount = Mathf.Max(1, amount);
    }
}
