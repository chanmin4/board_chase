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
    [Serializable]
    private sealed class SectorSpawnAsset
    {
        public string key;
        public GameObject prefab;
        public LootObjectSO lootObject;
    }
    [SerializeField] private List<SectorSpawnAsset> _sectorSpawnAssets = new();

    private Dictionary<string, ItemSO> _itemsByGuid;
    private Dictionary<string, StatusEffectDefinitionSO> _statusEffectsById;
    private Dictionary<string, SectorSpawnAsset> _sectorSpawnsByKey;

    public bool TryResolveSectorSpawn(string key, out GameObject prefab, out LootObjectSO lootObject)
    {
        EnsureLookup();
        prefab = null; lootObject = null;
        if (string.IsNullOrEmpty(key) || !_sectorSpawnsByKey.TryGetValue(key, out var entry)) return false;
        if (key.StartsWith("loot:", StringComparison.Ordinal) && entry.lootObject == null) return false;
        prefab = entry.prefab; lootObject = entry.lootObject;
        return prefab != null;
    }

#if UNITY_EDITOR
    // Append-only: removing a candidate from a table must not break an in-progress raid save.
    public string RegisterSectorSpawnForAuthoring(GameObject prefab, LootObjectSO lootObject, out bool changed)
    {
        changed = false;
        if (prefab == null) return string.Empty;
        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(
            lootObject != null ? (UnityEngine.Object)lootObject : prefab));
        if (string.IsNullOrEmpty(guid)) return string.Empty;
        string key = (lootObject != null ? "loot:" : "prefab:") + guid;
        _sectorSpawnAssets ??= new();
        var entry = _sectorSpawnAssets.Find(row => row != null && row.key == key);
        if (entry == null)
        {
            _sectorSpawnAssets.Add(new SectorSpawnAsset { key = key, prefab = prefab, lootObject = lootObject });
            changed = true;
        }
        else if (entry.prefab != prefab || entry.lootObject != lootObject)
        {
            entry.prefab = prefab; entry.lootObject = lootObject; changed = true;
        }
        if (changed) _sectorSpawnsByKey = null;
        return key;
    }

    public bool RegisterSectorItemForAuthoring(ItemSO item)
    {
        if (item == null) return false;
        if (_contentAssets != null && Array.IndexOf(_contentAssets, item) >= 0) return false;
        var assets = new List<UnityEngine.Object>(_contentAssets ?? Array.Empty<UnityEngine.Object>()) { item };
        _contentAssets = assets.ToArray();
        _itemsByGuid = null;
        return true;
    }
#endif

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
        if (PassiveInstanceRuntime.IsInstanceId(guid))
            return PassiveInstanceRuntime.TryResolve(guid, this, out item);
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
        _sectorSpawnsByKey = null;
    }

    private void EnsureLookup()
    {
        if (_itemsByGuid != null && _statusEffectsById != null && _sectorSpawnsByKey != null)
            return;

        _itemsByGuid = new Dictionary<string, ItemSO>(StringComparer.Ordinal);
        _statusEffectsById = new Dictionary<string, StatusEffectDefinitionSO>(StringComparer.Ordinal);
        _sectorSpawnsByKey = new Dictionary<string, SectorSpawnAsset>(StringComparer.Ordinal);
        if (_sectorSpawnAssets != null)
            foreach (var entry in _sectorSpawnAssets)
                if (entry != null && !string.IsNullOrEmpty(entry.key) && entry.prefab != null)
                    _sectorSpawnsByKey.TryAdd(entry.key, entry);

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
