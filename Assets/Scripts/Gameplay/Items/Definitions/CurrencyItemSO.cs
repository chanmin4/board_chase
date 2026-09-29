/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines the CurrencyItemSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "CurrencyItem",
    menuName = "Game/Currency Item")]
/// <summary>
/// A valuable with explicit payment semantics. Currency stays physically
/// lootable and sellable, while shop and repair transactions can still
/// identify it as the dedicated payment item.
/// </summary>
public class CurrencyItemSO : ValuableItemSO
{
    [Header("Physical Currency")]
    [Tooltip("Currency is a normal physical stack. It has no numerical wallet type or hidden runtime balance.")]
    [SerializeField, TextArea] private string _physicalCurrencyNote = "Use this ItemSO in inventory, loot pools, shop payments, repairs, and rerolls.";

    public override ItemKind Kind => ItemKind.Currency;
}
