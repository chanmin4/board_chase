/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Separates preventive gauge treatment from maximum-health erosion recovery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "InfectionCureItem",
    menuName = "Game/Character Item/Consumable/Infection Cure")]
public sealed class InfectionCureItemSO : MedicalItemSO
{
    public enum MedicineKind { ExposureAmpoule, MaximumHealthSyringe }
    [SerializeField] private MedicineKind _medicineKind;
    public bool IsSyringe => _medicineKind == MedicineKind.MaximumHealthSyringe;
    [Header("Infection treatment")]
    [Tooltip("Ampoule: total partial-gauge recovery over its duration (after the recovery delay). Syringe: maximum-health erosion restored immediately.")]
    [SerializeField, Min(1f)] private float _infectionRecoveryAmount = 45f;

    public float InfectionRecoveryAmount => Mathf.Max(1f, _infectionRecoveryAmount);
    [SerializeField, Min(0.1f)] private float _infectionRecoveryDurationSeconds = 15f;
    public float InfectionRecoveryDurationSeconds => Mathf.Max(0.1f, _infectionRecoveryDurationSeconds);
    public float InfectionRecoveryPerSecond => InfectionRecoveryAmount / InfectionRecoveryDurationSeconds;
    [SerializeField, Range(0f, 1f)] private float _exposureGainMultiplier = .5f;
    public float ExposureGainMultiplier => Mathf.Clamp01(_exposureGainMultiplier);
    public override bool HasUseEffect => base.HasUseEffect || InfectionRecoveryAmount > 0f;
}
