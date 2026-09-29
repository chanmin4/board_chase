/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines one physical reusable key and its lock tier.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// The intentionally small first-generation key taxonomy. A lock requires one
/// tier rather than a unique named key so content can use locked loot before a
/// large bespoke-key library exists.
/// </summary>
public enum LootKeyTier
{
    Normal = 0,
    Rare = 1,
    Unique = 2,
    Legendary = 3
}

[CreateAssetMenu(
    fileName = "KeyItem",
    menuName = "Game/Character Item/Key")]
public sealed class KeyItemSO : ItemSO
{
    [Header("Identity")]
    [Tooltip("Stable content ID used by tools, diagnostics, and future saves. Keep this unique even though the first release uses only four tiers.")]
    [SerializeField] private string _keyId = "normal_key";

    [Tooltip("Fallback display name used until localized key text is authored.")]
    [SerializeField] private string _displayName = "Normal Key";

    [Tooltip("The Key Tool compartment and locked-container tier this key satisfies.")]
    [SerializeField] private LootKeyTier _tier = LootKeyTier.Normal;

    [Header("Durability")]
    [Tooltip("Successful matching lock opens available from this physical key. A depleted key is removed from the Key Tool.")]
    [SerializeField, Min(1)] private int _maxUses = 3;

    public string KeyId => string.IsNullOrWhiteSpace(_keyId) ? name : _keyId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? KeyId : _displayName;
    public LootKeyTier Tier => _tier;
    public int MaxUses => Mathf.Max(1, _maxUses);
    public override ItemKind Kind => ItemKind.Key;

    // Physical keys stay non-stackable. This keeps the durable in-Key-Tool
    // instance model deterministic and leaves room for named keys later.
    public override int MaxStack => 1;
}
