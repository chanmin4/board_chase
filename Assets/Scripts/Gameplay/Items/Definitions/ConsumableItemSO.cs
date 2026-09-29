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
    [Header("Use")]
    [Tooltip("Health restored after the full use duration completes. The item is consumed only when the heal succeeds.")]
    [SerializeField, Min(0f)] private float _healAmount = 30f;

    [Tooltip("Mark removed immediately after the full use duration completes. This can cross a 100-point stack boundary.")]
    [SerializeField, Min(0f)] private float _markRecoveryAmount;

    [Tooltip("Seconds required to use this item. Walking is allowed. Attacking, dashing, interacting, changing action, taking damage, or manually cancelling interrupts it.")]
    [SerializeField, Min(0.1f)] private float _useDurationSeconds = 4f;

    [Header("Status Effect")]
    [Tooltip("Optional status applied to the user after this consumable finishes. Painkillers use this as their temporary injury-prevention window.")]
    [SerializeField] private StatusEffectDefinitionSO _appliedStatusEffect;

    [Tooltip("When enabled, this consumable removes all active status effects marked as bullet injuries after its use completes.")]
    [SerializeField] private bool _curesInjuries;

    public float HealAmount => Mathf.Max(0f, _healAmount);
    public float MarkRecoveryAmount => Mathf.Max(0f, _markRecoveryAmount);
    public float UseDurationSeconds => Mathf.Max(0.1f, _useDurationSeconds);
    public StatusEffectDefinitionSO AppliedStatusEffect => _appliedStatusEffect;
    public bool CuresInjuries => _curesInjuries;

    public virtual bool HasUseEffect =>
        HealAmount > 0f ||
        MarkRecoveryAmount > 0f ||
        _appliedStatusEffect != null ||
        _curesInjuries;
}
