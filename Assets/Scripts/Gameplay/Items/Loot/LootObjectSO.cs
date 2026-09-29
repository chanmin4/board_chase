/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Defines one reusable world-loot object's identity, presentation, capacity, and loot-object-type binding.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;
using UnityEngine.Localization;

public enum LootContainerSize { Custom = 0, Small = 1, Medium = 2, Large = 3 }

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
/// identifier or global-table lookup is required at runtime. A reusable type
/// may supply the shared table for multiple visual variants.
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
    [Tooltip("Authoring label only. Use Apply Size Preset in Loot Maker to set starter values. Does not scale the world model.")]
    [SerializeField] private LootContainerSize _sizeClass;
    [SerializeField] private LootContainerRollSettings _rollSettings = new();

    [Header("Loot Contents")]
    [Tooltip("Optional reusable container family. When assigned, this type's fixed table is used instead of the standalone table below.")]
    [SerializeField] private LootObjectTypeSO _lootObjectType;

    [HideInInspector]
    [Tooltip("Existing standalone fallback retained only for untyped objects. New content should use LootObjectTypeSO.")]
    [SerializeField] private LootTableSO _lootTable;

    public GameObject WorldPrefab => _worldPrefab;
    public LootObjectTypeSO LootObjectType => _lootObjectType;
    public LootTableSO LootTable => _lootObjectType != null
        ? _lootObjectType.LootTable
        : _lootTable;
    public int LootSlotCount => Mathf.Max(1, _lootSlotCount);
    public LootContainerSize SizeClass => _sizeClass;
    public LootContainerRollSettings RollSettings => _rollSettings ??= new LootContainerRollSettings();

    /// <summary>Explicit authoring action, never applied automatically on import or when changing category.</summary>
    public void ApplySizePreset(LootContainerSize size)
    {
        _sizeClass = size;
        if (size == LootContainerSize.Custom) return;
        _lootSlotCount = size == LootContainerSize.Small ? 4 : size == LootContainerSize.Medium ? 8 : 12;
        RollSettings.overrideTableRollCount = true;
        RollSettings.minRollCount = size == LootContainerSize.Small ? 1 : size == LootContainerSize.Medium ? 2 : 4;
        RollSettings.maxRollCount = size == LootContainerSize.Small ? 3 : size == LootContainerSize.Medium ? 5 : 8;
        RollSettings.rollSuccessChancePercent = size == LootContainerSize.Small ? 65f : size == LootContainerSize.Medium ? 80f : 90f;
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
    /// Editor-authoring helper used by Loot Maker to group a
    /// visual object under a reusable type without duplicating the table.
    /// </summary>
    public void AssignLootObjectType(LootObjectTypeSO lootObjectType)
    {
        _lootObjectType = lootObjectType;
    }

    // Purpose: Keeps authoring values valid while the asset is edited.
    private void OnValidate()
    {
        _lootSlotCount = Mathf.Max(1, _lootSlotCount);
        RollSettings.Normalize();
    }
}
