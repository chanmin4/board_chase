/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines a sell-only physical item with no active gameplay use.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Physical loot whose only gameplay value is its shop sell price. It still
/// uses the shared ItemSO display, rarity, stacking, and world-pickup data so
/// it can appear in any LootTableSO alongside weapons, ammunition, or keys.
/// </summary>
[CreateAssetMenu(
    fileName = "ValuableItem",
    menuName = "Game/Items/Valuable Item")]
public class ValuableItemSO : ItemSO
{
    public override ItemKind Kind => ItemKind.Valuable;
}
