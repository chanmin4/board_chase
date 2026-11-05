/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Defines the rarity-weight and bonus-roll rules for
 * one tier of Key Tool locked loot.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

/// <summary>
/// Reusable locked-container reward profile. It never owns an item list: the
/// existing Loot Progression Config remains the sole source of items. The
/// profile only changes how many rolls happen and how strongly each existing
/// rarity tier is weighted for this one opened container.
/// </summary>
[CreateAssetMenu(
    fileName = "LockedLootProfile",
    menuName = "Game/Loot/Locked Loot Profile")]
public sealed class LockedLootProfileSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("The Key Tool tier required by a lock using this profile.")]
    [SerializeField] private LootKeyTier _requiredKeyTier = LootKeyTier.Normal;

    [Tooltip("Editor-facing name for this reward profile. The container may still use its own display name.")]
    [SerializeField] private string _displayName = "Normal Locked Loot";

    [Header("Legacy Roll-Based Rewards (not used by slot-based boxes)")]
    [Tooltip("Legacy roll API only. Loot Maker's slot-based containers use their own Min/Max Filled Slots instead.")]
    [SerializeField, Min(0)] private int _minAdditionalRolls;
    [SerializeField, Min(0)] private int _maxAdditionalRolls;

    [Header("Legacy Rarity Multipliers (not used by slot-based boxes)")]
    [Tooltip("Legacy roll API only. Slot-based containers use the per-box weights in Loot Maker without these multipliers.")]
    [SerializeField, Min(0f)] private float _normalWeightMultiplier = 1f;
    [SerializeField, Min(0f)] private float _rareWeightMultiplier = 1f;
    [SerializeField, Min(0f)] private float _uniqueWeightMultiplier = 1f;
    [SerializeField, Min(0f)] private float _legendaryWeightMultiplier = 1f;

    public LootKeyTier RequiredKeyTier => _requiredKeyTier;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

    /// <summary>
    /// Returns an extra-roll count using the container's already deterministic
    /// loot random source. This preserves a stable outcome for one placed room
    /// without introducing a second random stream.
    /// </summary>
    public int GetAdditionalRollCount(System.Random random)
    {
        int min = Mathf.Max(0, _minAdditionalRolls);
        int max = Mathf.Max(min, _maxAdditionalRolls);
        return max <= min || random == null ? min : random.Next(min, max + 1);
    }

    // Purpose: Returns the authored multiplier for one existing loot rarity tier.
    public float GetRarityWeightMultiplier(PlayerShopItemRarity rarity)
    {
        return rarity switch
        {
            PlayerShopItemRarity.Normal => Mathf.Max(0f, _normalWeightMultiplier),
            PlayerShopItemRarity.Rare => Mathf.Max(0f, _rareWeightMultiplier),
            PlayerShopItemRarity.Unique => Mathf.Max(0f, _uniqueWeightMultiplier),
            PlayerShopItemRarity.Legendary => Mathf.Max(0f, _legendaryWeightMultiplier),
            _ => 1f
        };
    }

    // Purpose: Normalizes Inspector-authored profile values after they change.
    private void OnValidate()
    {
        _minAdditionalRolls = Mathf.Max(0, _minAdditionalRolls);
        _maxAdditionalRolls = Mathf.Max(_minAdditionalRolls, _maxAdditionalRolls);
        _normalWeightMultiplier = Mathf.Max(0f, _normalWeightMultiplier);
        _rareWeightMultiplier = Mathf.Max(0f, _rareWeightMultiplier);
        _uniqueWeightMultiplier = Mathf.Max(0f, _uniqueWeightMultiplier);
        _legendaryWeightMultiplier = Mathf.Max(0f, _legendaryWeightMultiplier);
    }
}
