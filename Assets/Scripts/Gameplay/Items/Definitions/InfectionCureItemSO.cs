/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines an emergency consumable that reduces the
 * player's personal Infection after its normal use/cast duration completes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "InfectionCureItem",
    menuName = "Game/Character Item/Consumable/Infection Cure")]
public sealed class InfectionCureItemSO : MedicalItemSO
{
    [Header("Emergency Infection Recovery")]
    [Tooltip("Personal Infection removed after the use cast completes. This item is not consumed when Infection is already zero.")]
    [SerializeField, Min(1f)] private float _infectionRecoveryAmount = 45f;

    [Tooltip("Emergency carry limit. One keeps the cure as a deliberate panic tool instead of a stackable heal.")]
    [SerializeField, Min(1)] private int _emergencyCarryLimit = 1;

    public float InfectionRecoveryAmount => Mathf.Max(1f, _infectionRecoveryAmount);
    public override int MaxOwnedAmount => Mathf.Max(1, _emergencyCarryLimit);
    public override int MaxStack => 1;
    public override bool HasUseEffect => base.HasUseEffect || InfectionRecoveryAmount > 0f;
}
