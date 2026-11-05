/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Reads legacy shared loot-family assets for compatibility only.
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
/// Legacy shared-family data. Current authored boxes store their category and
/// independent LootTableSO directly; this type is retained for old serialized data.
/// </summary>
// Legacy serialized asset only. New containers use an owned LootTableSO, not a shared family.
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
    /// Initializes legacy import/test data. New Loot Maker authoring does not create families.
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
