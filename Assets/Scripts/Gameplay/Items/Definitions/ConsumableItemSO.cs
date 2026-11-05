/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines the ConsumableItemSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "ConsumableItem",
    menuName = "Game/Character Item/Consumable")]
public class ConsumableItemSO : UsableItemSO
{
    [Header("Reusable item condition (zero destroys one item)")]
    [SerializeField, Min(0)] private float _maxDurability;
    [SerializeField, Min(.01f)] private float _durabilityCostPerUse = 1f;
    [SerializeField, Min(0)] private int _bleedingStacksRemoved;
    public bool UsesDurability => _maxDurability > 0f;
    public float MaxDurability => Mathf.Max(0f, _maxDurability);
    public float DurabilityCostPerUse => Mathf.Max(.01f, _durabilityCostPerUse);
    public int BleedingStacksRemoved => Mathf.Max(0, _bleedingStacksRemoved);
    [Header("Infection suppression (does not heal infection)")]
    [SerializeField, Min(0f)] private float _infectionSuppressionSeconds;
    public float InfectionSuppressionSeconds => Mathf.Max(0f, _infectionSuppressionSeconds);
    [Header("Use")]
    [Tooltip("Health restored after the full use duration completes. The item is consumed only when the heal succeeds.")]
    [SerializeField, Min(0f)] private float _healAmount = 30f;

    [Tooltip("Mark removed immediately after the full use duration completes. This can cross a 100-point stack boundary.")]
    [SerializeField, HideInInspector, Min(0f)] private float _markRecoveryAmount;

    [Tooltip("Seconds required to use this item. Walking is allowed. Attacking, dashing, interacting, changing action, taking damage, or manually cancelling interrupts it.")]
    [SerializeField, Min(0.1f)] private float _useDurationSeconds = 4f;

    [Header("Status Effect")]
    [Tooltip("Optional status applied to the user after this consumable finishes. Painkillers use this as their temporary injury-prevention window.")]
    [SerializeField] private StatusEffectDefinitionSO _appliedStatusEffect;

    [Tooltip("When enabled, this consumable removes all active status effects marked as bullet injuries after its use completes.")]
    [SerializeField] private bool _curesInjuries;

    public float HealAmount => Mathf.Max(0f, _healAmount);
    public float MarkRecoveryAmount => 0f; // WIP compatibility only.
    public float UseDurationSeconds => Mathf.Max(0.1f, _useDurationSeconds);
    public StatusEffectDefinitionSO AppliedStatusEffect => _appliedStatusEffect;
    public bool CuresInjuries => _curesInjuries;

    public virtual bool HasUseEffect =>
        HealAmount > 0f ||
        MarkRecoveryAmount > 0f ||
        _appliedStatusEffect != null ||
        _curesInjuries || BleedingStacksRemoved > 0 || InfectionSuppressionSeconds > 0f;
}
