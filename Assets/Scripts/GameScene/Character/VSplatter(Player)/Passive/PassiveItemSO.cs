using UnityEngine;

[CreateAssetMenu(
    fileName = "PassiveItem",
    menuName = "Game/Player/Passive Item")]
public class PassiveItemSO : ItemSO
{
    [Header("Passive Stat Modifiers")]
    [Tooltip("Player stat modifiers applied while this passive is owned.")]
    [SerializeField] private PlayerStatModifier[] _statModifiers;

    [Tooltip("Player feature flags applied while this passive is owned.")]
    [SerializeField] private PlayerFeatureFlagModifier[] _featureFlags;

    public PlayerStatModifier[] StatModifiers => _statModifiers;
    public PlayerFeatureFlagModifier[] FeatureFlags => _featureFlags;
}
