using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerPassiveInventoryRuntime : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("If false, the same PassiveItemSO cannot be added twice.")]
    [SerializeField] private bool _allowDuplicates = false;

    [Header("Runtime")]
    [SerializeField] private List<PassiveItemSO> _items = new List<PassiveItemSO>();

    public event Action OnChanged;

    public IReadOnlyList<PassiveItemSO> Items => _items;

    public bool TryAdd(PassiveItemSO item)
    {
        if (item == null)
            return false;

        if (!_allowDuplicates && _items.Contains(item))
            return false;

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
