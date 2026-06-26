using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerPassiveInventoryRuntime : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("Debug override. If true, every passive can be duplicated regardless of the PassiveItemSO pickup rule.")]
    [FormerlySerializedAs("_allowDuplicates")]
    [SerializeField] private bool _forceAllowDuplicates = false;

    [Header("Runtime")]
    [SerializeField] private List<PassiveItemSO> _items = new List<PassiveItemSO>();

    public event Action OnChanged;

    public IReadOnlyList<PassiveItemSO> Items => _items;

    public bool TryAdd(PassiveItemSO item)
    {
        if (item == null)
            return false;

        if (!_forceAllowDuplicates &&
            !item.AllowDuplicatePickup &&
            _items.Contains(item))
        {
            return false;
        }

        _items.Add(item);
        OnChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        if (_items.Count <= 0)
            return;

        _items.Clear();
        OnChanged?.Invoke();
    }
}
