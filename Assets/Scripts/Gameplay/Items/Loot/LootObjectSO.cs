/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Defines one reusable world-loot object's identity, presentation, capacity, and loot-object-type binding.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;
using UnityEngine.Localization;

// Keep the original serialized values for Custom / Small / Medium / Large.
public enum LootContainerSize { Custom = 0, VerySmall = 4, Small = 1, Medium = 2, Large = 3, VeryLarge = 5 }

/// <summary>Relative weights per box, normalized over non-empty, enabled tiers only.</summary>
[Serializable]
public sealed class LootContainerRarityWeights
{
    [Min(0)] public float normal = 63f;
    [Min(0)] public float rare = 30f;
    [Min(0)] public float unique = 6f;
    [Min(0)] public float legendary = 1f;

    public float Get(PlayerShopItemRarity rarity) => Clean(rarity switch
    {
        PlayerShopItemRarity.Normal => normal,
        PlayerShopItemRarity.Rare => rare,
        PlayerShopItemRarity.Unique => unique,
        PlayerShopItemRarity.Legendary => legendary,
        _ => 0f
    });

    private static float Clean(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    public void Normalize()
    {
        normal = Clean(normal); rare = Clean(rare); unique = Clean(unique); legendary = Clean(legendary);
    }
}

/// <summary>Variant-only yield settings. They never edit the family's shared item pool.</summary>
[Serializable]
public sealed class LootContainerRollSettings
{
    public bool overrideTableRollCount;
    [Range(0, 128)] public int minRollCount = 1;
    [Range(0, 128)] public int maxRollCount = 1;
    [Tooltip("Success chance for EACH roll, not the chance of spawning the world prefab. Failed rolls produce nothing.")]
    [Range(0f, 100f)] public float rollSuccessChancePercent = 100f;

    public float SuccessChance => float.IsNaN(rollSuccessChancePercent) ? 100f : Mathf.Clamp(rollSuccessChancePercent, 0f, 100f);
    public int MinRolls => Mathf.Clamp(minRollCount, 0, 128);
    public int MaxRolls => Mathf.Clamp(maxRollCount, MinRolls, 128);
    public void Normalize()
    {
        minRollCount = MinRolls;
        maxRollCount = MaxRolls;
        rollSuccessChancePercent = SuccessChance;
    }
}

/// <summary>
/// Authoring record for one searchable world object such as a safe, backpack,
/// or military case. World prefabs reference this asset directly; no string
/// identifier or global-table lookup is required at runtime. Each authored box
/// owns its table; the old family reference remains only to read/migrate legacy assets.
/// </summary>
[CreateAssetMenu(fileName = "LootObject_", menuName = "Game/Loot/Loot Object")]
public sealed class LootObjectSO : ScriptableObject
{
    [Header("World Object")]
    [Tooltip("The reusable world prefab represented by this record. This is authoring context only; spawning still uses the sector content prefab candidate.")]
    [SerializeField] private GameObject _worldPrefab;

    [Header("Loot Hub Presentation")]
    [Tooltip("Fallback title used until a localized title is authored below.")]
    [SerializeField] private string _displayName = "Loot Object";
    [Tooltip("Optional localized Loot Hub title. It takes precedence over the fallback title when assigned.")]
    [SerializeField] private LocalizedString _localizedDisplayName = default;
    [Tooltip("Number of item slots shown when this object is searched.")]
    [SerializeField, Min(1)] private int _lootSlotCount = 6;

    [Header("Variant Size and Yield")]
    [Tooltip("Authoring label only. Use a size preset in Loot Maker to set capacity. Does not change rarity weights or scale the world model.")]
    [SerializeField] private LootContainerSize _sizeClass;
    // Retained for legacy asset migration/API compatibility. New world generation never uses its success chance.
    [HideInInspector, SerializeField] private LootContainerRollSettings _rollSettings = new();

    [Header("Generated Occupied Slots (not item units)")]
    [HideInInspector, SerializeField] private int _slotGenerationVersion;
    [SerializeField, Min(0)] private int _minGeneratedSlots = 1;
    [SerializeField, Min(0)] private int _maxGeneratedSlots = 1;
    [SerializeField] private LootContainerRarityWeights _rarityWeights = new();

    [Header("Loot Contents")]
    // Read-only legacy compatibility. Loot Maker converts this to an owned table before editing.
    [HideInInspector, SerializeField] private LootObjectTypeSO _lootObjectType;

    // Existing records inherit the family's category until explicitly moved in Loot Maker.
    // Classification is independent of table sharing and never changes generated loot.
    [HideInInspector, SerializeField] private bool _overrideCategory;
    [HideInInspector, SerializeField] private LootContainerCategory _category;

    [HideInInspector]
    [Tooltip("This box's independent table. Assign through Loot Maker so copies never share a live table.")]
    [SerializeField] private LootTableSO _lootTable;

    public GameObject WorldPrefab => _worldPrefab;
    public LootObjectTypeSO LootObjectType => _lootObjectType;
    public bool HasCategoryAssignment => _overrideCategory || _lootObjectType != null;
    public LootContainerCategory Category => _overrideCategory ? _category :
        _lootObjectType != null ? _lootObjectType.Category : LootContainerCategory.Uncategorized;

    public void AssignCategory(LootContainerCategory category)
    {
        if (!Enum.IsDefined(typeof(LootContainerCategory), category))
            throw new ArgumentOutOfRangeException(nameof(category));
        _category = category;
        _overrideCategory = true;
    }
    public LootTableSO LootTable => _lootObjectType != null
        ? _lootObjectType.LootTable
        : _lootTable;
    public int LootSlotCount => Mathf.Max(1, _lootSlotCount);
    public LootContainerSize SizeClass => _sizeClass;
    public LootContainerRollSettings RollSettings => _rollSettings ??= new LootContainerRollSettings();
    public bool HasSlotGenerationSettings => _slotGenerationVersion > 0;
    public LootContainerRarityWeights RarityWeights => _rarityWeights ??= new LootContainerRarityWeights();
    public int MinGeneratedSlots => Mathf.Clamp(HasSlotGenerationSettings ? _minGeneratedSlots :
        RollSettings.overrideTableRollCount ? RollSettings.MinRolls : LootTable?.Pool.minRollCount ?? 1, 0, LootSlotCount);
    public int MaxGeneratedSlots => Mathf.Clamp(HasSlotGenerationSettings ? _maxGeneratedSlots :
        RollSettings.overrideTableRollCount ? RollSettings.MaxRolls : LootTable?.Pool.maxRollCount ?? 1, MinGeneratedSlots, LootSlotCount);

    // Explicit edit/save migration: reading cards never dirties assets. Old success chance is intentionally retired.
    public void InitializeSlotGenerationSettings()
    {
        if (!HasSlotGenerationSettings) SetGeneratedSlotRange(MinGeneratedSlots, MaxGeneratedSlots);
    }

    public void SetGeneratedSlotRange(int min, int max)
    {
        _minGeneratedSlots = Mathf.Clamp(min, 0, LootSlotCount);
        _maxGeneratedSlots = Mathf.Clamp(max, _minGeneratedSlots, LootSlotCount);
        _slotGenerationVersion = 1;
    }

    /// <summary>Changes only size label and capacity. Never applied automatically on import or category changes.</summary>
    public void ApplySizePreset(LootContainerSize size)
    {
        InitializeSlotGenerationSettings();
        _sizeClass = size;
        if (size == LootContainerSize.Custom) return;
        _lootSlotCount = size switch
        {
            LootContainerSize.VerySmall => 3,
            LootContainerSize.Small => 4,
            LootContainerSize.Medium => 6,
            LootContainerSize.Large => 9,
            LootContainerSize.VeryLarge => 12,
            _ => _lootSlotCount
        };
    }

    /// <summary>
    /// Resolves the title for the Loot Hub without duplicating presentation
    /// strings on either the prefab or its runtime inventory component.
    /// </summary>
    public string ResolveDisplayName()
    {
        if (_localizedDisplayName != null && !_localizedDisplayName.IsEmpty)
        {
            string localized = _localizedDisplayName.GetLocalizedString();
            if (!string.IsNullOrWhiteSpace(localized))
                return localized;
        }

        return string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    }

    /// <summary>
    /// Legacy import/test compatibility only. Loot Maker never authors shared links.
    /// New authoring must create a separate table and call AssignOwnLootTable.
    /// </summary>
    public void AssignLootObjectType(LootObjectTypeSO lootObjectType)
    {
        // Changing tables must not also change this box's generation count.
        if (LootTable != null) InitializeSlotGenerationSettings();
        _lootObjectType = lootObjectType;
    }

    /// <summary>Installs an editor-created owned table while preserving effective category and filled slots.</summary>
    public void AssignOwnLootTable(LootTableSO table)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        LootContainerCategory category = Category;
        InitializeSlotGenerationSettings();
        _lootTable = table;
        _lootObjectType = null;
        AssignCategory(category);
    }

    // Purpose: Keeps authoring values valid while the asset is edited.
    private void OnValidate()
    {
        _lootSlotCount = Mathf.Max(1, _lootSlotCount);
        RollSettings.Normalize();
        RarityWeights.Normalize();
        if (HasSlotGenerationSettings) SetGeneratedSlotRange(_minGeneratedSlots, _maxGeneratedSlots);
    }
}
