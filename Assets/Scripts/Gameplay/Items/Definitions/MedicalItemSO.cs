/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Classifies medicine and treatment consumables while
 * preserving the common ConsumableItemSO use pipeline.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// A consumable intended for medical treatment: healing, injury recovery,
/// infection treatment, or medical injections. Runtime systems continue to
/// consume it through <see cref="ConsumableItemSO"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "MedicalItem",
    menuName = "Game/Character Item/Consumable/Medical Item")]
public class MedicalItemSO : ConsumableItemSO
{
}
