using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerPassiveInventoryRuntime : MonoBehaviour
{
    private readonly List<PassiveItemSO> _inventoryBackedItems = new();

    [Header("Rules")]
    [Tooltip("Debug override. If true, every passive can be duplicated regardless of the PassiveItemSO pickup rule.")]
    [FormerlySerializedAs("_allowDuplicates")]
    [SerializeField] private bool _forceAllowDuplicates = false;

    [Header("Inventory Source")]
    [Tooltip("If assigned, passive effects are read from PlayerInventoryRuntime instead of this component's local list.")]
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;
    [SerializeField] private bool _useInventoryRuntime = true;

    [Header("Runtime")]
    [SerializeField] private List<PassiveItemSO> _items = new List<PassiveItemSO>();

    public event Action OnChanged;

    public IReadOnlyList<PassiveItemSO> Items =>
        _useInventoryRuntime && _inventoryRuntime != null
            ? _inventoryBackedItems
            : _items;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        RefreshInventoryBackedItems();
    }

    private void OnEnable()
    {
        ResolveRefs();

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged += HandleInventoryChanged;

        RefreshInventoryBackedItems();
    }

    private void OnDisable()
    {
        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged -= HandleInventoryChanged;
    }

    public bool TryAdd(PassiveItemSO item)
    {
        if (item == null)
            return false;

        if (_useInventoryRuntime && _inventoryRuntime != null)
            return _inventoryRuntime.TryPickupPassiveItem(item, out _);

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
        if (_useInventoryRuntime && _inventoryRuntime != null)
        {
            _inventoryRuntime.ClearPassiveItems();
            return;
        }

        if (_items.Count <= 0)
            return;

        _items.Clear();
        OnChanged?.Invoke();
    }

    private void ResolveRefs()
    {
        if (_inventoryRuntime == null)
            _inventoryRuntime = GetComponent<PlayerInventoryRuntime>();
    }

    private void HandleInventoryChanged()
    {
        RefreshInventoryBackedItems();
        OnChanged?.Invoke();
    }

    private void RefreshInventoryBackedItems()
    {
        _inventoryBackedItems.Clear();

        if (!_useInventoryRuntime || _inventoryRuntime == null)
            return;

        _inventoryRuntime.GetPassiveItems(_inventoryBackedItems);
    }
}
