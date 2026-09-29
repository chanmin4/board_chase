/*
 * Role hierarchy: Core > Persistence
 * Primary responsibility: Resolves authored runtime content by stable identifiers for save restoration.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Build-safe catalog for content referenced by resumable player-run saves.
/// The asset lives under Resources so it can resolve an inventory before any
/// particular loot table, shop, or stage has loaded its ItemSO references.
/// </summary>
[CreateAssetMenu(fileName = "RuntimeContentCatalog", menuName = "Board Chase/Persistence/Runtime Content Catalog")]
public sealed class RuntimeContentCatalogSO : ScriptableObject
{
    [Tooltip("ItemSO and StatusEffectDefinitionSO assets that may appear in a resumable player run. Other object types are ignored deliberately.")]
    [SerializeField] private UnityEngine.Object[] _contentAssets = Array.Empty<UnityEngine.Object>();

    private Dictionary<string, ItemSO> _itemsByGuid;
    private Dictionary<string, StatusEffectDefinitionSO> _statusEffectsById;

    public int ItemCount
    {
        get
        {
            EnsureLookup();
            return _itemsByGuid.Count;
        }
    }

    public int StatusEffectCount
    {
        get
        {
            EnsureLookup();
            return _statusEffectsById.Count;
        }
    }

    public bool TryResolveItem(string guid, out ItemSO item)
    {
        EnsureLookup();
        item = null;
        return !string.IsNullOrWhiteSpace(guid) && _itemsByGuid.TryGetValue(guid, out item) && item != null;
    }

    public bool TryResolveStatusEffect(string id, out StatusEffectDefinitionSO definition)
    {
        EnsureLookup();
        definition = null;
        return !string.IsNullOrWhiteSpace(id) &&
               _statusEffectsById.TryGetValue(id, out definition) &&
               definition != null;
    }

    private void OnEnable()
    {
        _itemsByGuid = null;
        _statusEffectsById = null;
    }

    private void EnsureLookup()
    {
        if (_itemsByGuid != null && _statusEffectsById != null)
            return;

        _itemsByGuid = new Dictionary<string, ItemSO>(StringComparer.Ordinal);
        _statusEffectsById = new Dictionary<string, StatusEffectDefinitionSO>(StringComparer.Ordinal);

        if (_contentAssets == null)
            return;

        for (int index = 0; index < _contentAssets.Length; index++)
        {
            switch (_contentAssets[index])
            {
                case ItemSO item when !string.IsNullOrWhiteSpace(item.Guid):
                    _itemsByGuid.TryAdd(item.Guid, item);
                    break;

                case StatusEffectDefinitionSO definition when !string.IsNullOrWhiteSpace(definition.Id):
                    _statusEffectsById.TryAdd(definition.Id, definition);
                    break;
            }
        }
    }
}
