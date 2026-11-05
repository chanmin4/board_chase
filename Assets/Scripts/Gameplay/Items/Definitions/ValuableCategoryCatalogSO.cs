using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Author-owned labels; stable IDs, not display names, are saved on items and loot weights.</summary>
[CreateAssetMenu(menuName = "Game/Items/Valuable Categories", fileName = "ValuableCategories")]
public sealed class ValuableCategoryCatalogSO : ScriptableObject
{
    [Serializable]
    public sealed class Category
    {
        public string id;
        public string label;
    }
    public List<Category> categories = new();
    private static ValuableCategoryCatalogSO _active;
    public static ValuableCategoryCatalogSO Active => _active != null ? _active : _active = Resources.Load<ValuableCategoryCatalogSO>("ValuableCategories");
    public Category Find(string id) => string.IsNullOrEmpty(id) ? null : categories?.Find(c => c != null && c.id == id);
    public string Label(string id) => Find(id)?.label ?? "미분류";
    public int Order(string id) => Find(id) is Category category ? categories.IndexOf(category) : int.MaxValue;
}
