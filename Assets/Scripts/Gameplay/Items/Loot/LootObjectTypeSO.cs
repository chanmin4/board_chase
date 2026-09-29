/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Shares one fixed LootTableSO across a family of world loot-object prefabs.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

// Serialized values are stable; categories organize authoring, not item eligibility.
public enum LootContainerCategory
{
    Uncategorized = 0, Ammo = 1, Medical = 2, MilitaryGear = 3,
    TreasureBox = 4, Safe = 5, Backpack = 6, MetalBox = 7
}

/// <summary>
/// Authoring-level container family such as Abandoned Bag, Ammo Container, or
/// Safe. Visual variants reference individual LootObjectSO records, while the
/// records can share this type's single fixed LootTableSO.
/// </summary>
[CreateAssetMenu(fileName = "LootObjectType_", menuName = "Game/Loot/Loot Object Type")]
public sealed class LootObjectTypeSO : ScriptableObject
{
    [Header("Authoring Identity")]
    [Tooltip("Preview-only prefab shown in Loot Maker. It does not control runtime spawning.")]
    [SerializeField] private GameObject _previewPrefab;
    [Tooltip("Author-facing family name. Individual LootObjectSO assets still own their localized in-game display title.")]
    [SerializeField] private string _displayName = "Loot Object Type";

    [SerializeField] private LootContainerCategory _category;

    [Tooltip("Every visual variant of this type shares this one fixed loot table.")]
    [SerializeField] private LootTableSO _lootTable;

    public GameObject PreviewPrefab => _previewPrefab;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    public LootTableSO LootTable => _lootTable;
    public LootContainerCategory Category => _category;

    public void AssignCategory(LootContainerCategory category) => _category = category;

    /// <summary>
    /// Initializes a newly created type and its shared table from Loot Maker.
    /// Individual visual objects then bind here.
    /// </summary>
    public void Configure(string displayName, GameObject previewPrefab, LootTableSO lootTable)
    {
        _displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
        _previewPrefab = previewPrefab;
        _lootTable = lootTable;
    }

    // Purpose: Prevents a missing table from silently creating an unlootable type asset.
    private void OnValidate()
    {
        _lootTable?.EnsurePool();
    }
}
