/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines categorized physical barter/quest loot with no active use.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Physical barter, quest and sale loot with an author-owned material category. It still
/// uses the shared ItemSO display, rarity, stacking, and world-pickup data so
/// it can appear in any LootTableSO alongside weapons, ammunition, or keys.
/// </summary>
[CreateAssetMenu(
    fileName = "ValuableItem",
    menuName = "Game/Items/Valuable Item")]
public class ValuableItemSO : ItemSO
{
    [Tooltip("Item Maker에서 관리하는 물품 분류. 이름 변경에도 유지되는 고정 ID입니다.")]
    [SerializeField] private string _valuableCategoryId;
    [SerializeField, HideInInspector] private bool _wasPassive;
    public bool WasPassive => _wasPassive;
    public virtual string ValuableCategoryId => _valuableCategoryId ?? string.Empty;
    public string ValuableCategoryLabel => ValuableCategoryCatalogSO.Active?.Label(ValuableCategoryId) ?? "미분류";
    public override ItemKind Kind => ItemKind.Valuable;
}
