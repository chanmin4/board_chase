using System.Collections.Generic;
using System.Linq;

/// <summary>Catalog selection, independent of IMGUI and the Project window selection.</summary>
internal sealed class ItemMakerSelection
{
    private readonly List<ItemSO> _items = new();
    private ItemSO _anchor;
    public IReadOnlyList<ItemSO> Items => _items;
    public ItemSO Active { get; private set; }
    public bool Contains(ItemSO item) => _items.Contains(item);

    public void Set(IEnumerable<ItemSO> items, ItemSO active = null)
    {
        ItemSO[] next = items.Where(item => item != null).Distinct().ToArray();
        _items.Clear();
        _items.AddRange(next);
        Active = active != null && _items.Contains(active) ? active : _items.LastOrDefault();
        _anchor = Active;
    }

    public void Click(ItemSO item, IReadOnlyList<ItemSO> visible, bool toggle, bool range,
        bool preserveSelectedForDrag)
    {
        if (item == null) return;
        int anchorIndex = IndexOf(visible, _anchor);
        int itemIndex = IndexOf(visible, item);
        if (range && anchorIndex >= 0 && itemIndex >= 0)
        {
            if (!toggle) _items.Clear();
            for (int i = System.Math.Min(anchorIndex, itemIndex); i <= System.Math.Max(anchorIndex, itemIndex); i++)
                if (!_items.Contains(visible[i])) _items.Add(visible[i]);
            Active = item;
            return;
        }

        _anchor = item;
        if (toggle)
        {
            if (!_items.Remove(item)) { _items.Add(item); Active = item; }
            else if (Active == item) Active = _items.LastOrDefault();
        }
        else
        {
            if (!preserveSelectedForDrag || !_items.Contains(item))
            {
                _items.Clear();
                _items.Add(item);
            }
            Active = item;
        }
    }

    public bool Retain(IReadOnlyList<ItemSO> visible)
    {
        int removed = _items.RemoveAll(item => item == null || IndexOf(visible, item) < 0);
        if (removed == 0) return false;
        if (!_items.Contains(Active)) Active = _items.LastOrDefault();
        if (!_items.Contains(_anchor)) _anchor = Active;
        return true;
    }

    private static int IndexOf(IReadOnlyList<ItemSO> items, ItemSO item)
    {
        if (item == null) return -1;
        for (int i = 0; i < items.Count; i++) if (items[i] == item) return i;
        return -1;
    }
}
