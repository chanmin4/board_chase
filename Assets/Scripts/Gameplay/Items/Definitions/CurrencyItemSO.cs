/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines the CurrencyItemSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "CurrencyItem",
    menuName = "Game/Items/Valuable/Currency")]
/// <summary>
/// A valuable with explicit payment semantics. Currency stays physically
/// lootable and sellable, while shop and repair transactions can still
/// identify it as the dedicated payment item.
/// </summary>
public class CurrencyItemSO : ValuableItemSO
{
    public const string CategoryId = "currency";
    public const int CurrencyStackLimit = 500_000;
    public override string ValuableCategoryId => CategoryId;
    public override int MaxStack => CurrencyStackLimit;
}
