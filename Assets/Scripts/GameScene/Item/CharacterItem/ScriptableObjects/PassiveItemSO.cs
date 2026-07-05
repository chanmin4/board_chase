using UnityEngine;

[CreateAssetMenu(
    fileName = "PassiveItem",
    menuName = "Game/Passive Item")]
public class PassiveItemSO : ItemSO
{
    [Header("Pickup Rules")]
    [Tooltip("If true, this passive item can be picked up multiple times.")]
    [SerializeField] private bool _allowDuplicatePickup = false;

    [Header("Passive Stat Modifiers")]
    [Tooltip("Player stat modifiers applied while this passive is owned.")]
    [SerializeField] private PlayerStatModifier[] _statModifiers;

    [Tooltip("Player feature flags applied while this passive is owned.")]
    [SerializeField] private PlayerFeatureFlagModifier[] _featureFlags;

    public bool AllowDuplicatePickup => _allowDuplicatePickup;
    public PlayerStatModifier[] StatModifiers => _statModifiers;
    public PlayerFeatureFlagModifier[] FeatureFlags => _featureFlags;
}
