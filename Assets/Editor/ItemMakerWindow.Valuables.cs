using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class ItemMakerWindow
{
    private string _valuableCategoryFilter;
    private void DrawValuableCategoryFilters()
    {
        if (_category != ItemCatalogCategory.Valuable) return;
        var catalog = ValuableCategoryCatalogSO.Active;
        if (!string.IsNullOrEmpty(_valuableCategoryFilter) && catalog?.Find(_valuableCategoryFilter) == null) _valuableCategoryFilter = null;
        if (GUILayout.Button("  전체 물품 분류", EditorStyles.miniButton)) _valuableCategoryFilter = null;
        if (GUILayout.Button("  미분류", EditorStyles.miniButton)) _valuableCategoryFilter = string.Empty;
        if (catalog == null) return;
        foreach (var category in catalog.categories.Where(c => c != null))
        {
            int count = _items.OfType<ValuableItemSO>().Count(i => i.ValuableCategoryId == category.id);
            if (GUILayout.Toggle(_valuableCategoryFilter == category.id, "  " + category.label + " (" + count + ")", EditorStyles.miniButton))
                _valuableCategoryFilter = category.id;
            var rect = GUILayoutUtility.GetLastRect();
            if (!rect.Contains(Event.current.mousePosition) || DragAndDrop.objectReferences.Length == 0) continue;
            var items = DragAndDrop.objectReferences.OfType<ValuableItemSO>().ToArray();
            if (items.Length != DragAndDrop.objectReferences.Length || EditorApplication.isPlayingOrWillChangePlaymode ||
                category.id == CurrencyItemSO.CategoryId || items.Any(item => item is CurrencyItemSO)) continue;
            if (Event.current.type == EventType.DragUpdated) { DragAndDrop.visualMode = DragAndDropVisualMode.Link; Event.current.Use(); }
            if (Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                ValuableCategoryEditorWindow.Assign(items, category.id);
                _valuableCategoryFilter = category.id;
                Event.current.Use();
            }
        }
        EditorGUILayout.LabelField("귀중품 카드를 분류로 드래그 · 이름/추가/삭제는 상단 물품 분류 관리", EditorStyles.wordWrappedMiniLabel);
    }
}

public sealed class ValuableCategoryEditorWindow : EditorWindow
{
    private string _newLabel = "";
    private Vector2 _scroll;
    public static void Open() => GetWindow<ValuableCategoryEditorWindow>("물품 분류 관리");

    internal static void DrawAssignment(SerializedProperty property)
    {
        var catalog = ValuableCategoryCatalogSO.Active;
        var targets = property.serializedObject.targetObjects;
        if (targets.Any(item => item is CurrencyItemSO))
        {
            string label = targets.All(item => item is CurrencyItemSO)
                ? catalog?.Find(CurrencyItemSO.CategoryId)?.label ?? "화폐" : "화폐와 일반 귀중품은 별도로 선택";
            EditorGUILayout.LabelField("물품 분류", label);
            return;
        }
        var categories = catalog?.categories.Where(c => c != null && c.id != CurrencyItemSO.CategoryId).ToArray() ??
            Array.Empty<ValuableCategoryCatalogSO.Category>();
        var ids = new[] { "" }.Concat(categories.Select(c => c.id)).ToArray();
        var labels = new[] { "미분류" }.Concat(categories.Select(c => c.label)).ToArray();
        int current = Array.IndexOf(ids, property.stringValue);
        bool previousMixed = EditorGUI.showMixedValue;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int selected = EditorGUILayout.Popup("물품 분류", Math.Max(0, current), labels);
        if (EditorGUI.EndChangeCheck()) property.stringValue = ids[selected];
        EditorGUI.showMixedValue = previousMixed;
        if (current < 0) EditorGUILayout.HelpBox("등록되지 않은 분류 ID입니다. 분류를 다시 선택하세요: " + property.stringValue, MessageType.Warning);
    }

    internal static void Assign(ValuableItemSO[] items, string id)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (items.Any(item => item == null || (AssetDatabase.Contains(item) && !AssetDatabase.IsOpenForEdit(item))))
            throw new InvalidOperationException("분류 변경 대상에 읽기 전용/유효하지 않은 아이템이 있습니다. 아무것도 변경하지 않았습니다.");
        if (items.Length > 0) Undo.RecordObjects(items, "물품 분류 변경");
        foreach (var item in items)
        {
            using var data = new SerializedObject(item);
            data.FindProperty("_valuableCategoryId").stringValue = id;
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(item);
        }
        MakerRegistrationIndex.Invalidate();
    }

    private void OnGUI()
    {
        var catalog = ValuableCategoryCatalogSO.Active;
        if (catalog == null) { EditorGUILayout.HelpBox("Resources/ValuableCategories 에셋이 필요합니다.", MessageType.Error); return; }
        EditorGUILayout.HelpBox("귀중품의 물품 분류입니다. 상자 종류와 별개이며 모든 Item/Loot Maker에서 공유합니다. 이름 변경은 ID·아이템·루팅 확률을 유지합니다. 삭제는 아이템을 미분류로 되돌립니다. Ctrl+Z 가능.", MessageType.Info);
        using var locked = new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < catalog.categories.Count; i++)
        {
            var category = catalog.categories[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                string label = EditorGUILayout.DelayedTextField(category.label);
                if (!string.IsNullOrWhiteSpace(label) && label.Trim() != category.label)
                { Undo.RecordObject(catalog, "분류 이름 변경"); category.label = label.Trim(); EditorUtility.SetDirty(catalog); }
                using (new EditorGUI.DisabledScope(i == 0))
                    if (GUILayout.Button("↑", GUILayout.Width(25)))
                    { Undo.RecordObject(catalog, "분류 순서 변경"); (catalog.categories[i - 1], catalog.categories[i]) = (catalog.categories[i], catalog.categories[i - 1]); EditorUtility.SetDirty(catalog); GUIUtility.ExitGUI(); }
                using var fixedCategory = new EditorGUI.DisabledScope(category.id == CurrencyItemSO.CategoryId);
                if (GUILayout.Button("삭제", GUILayout.Width(44)))
                {
                    var users = AssetDatabase.FindAssets("t:ValuableItemSO").Select(g => AssetDatabase.LoadAssetAtPath<ValuableItemSO>(AssetDatabase.GUIDToAssetPath(g)))
                        .Where(item => item != null && item.ValuableCategoryId == category.id).ToArray();
                    if (EditorUtility.DisplayDialog("분류 삭제", $"'{category.label}' 분류를 삭제하고 {users.Length}개 아이템을 미분류로 옮깁니다. 아이템 자체는 삭제하지 않습니다. 기존 루팅 가중치는 보존되지만 이 분류 키는 더 이상 사용되지 않습니다.", "삭제", "취소"))
                    {
                        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                        Undo.RecordObject(catalog, "물품 분류 삭제");
                        Assign(users, ""); catalog.categories.RemoveAt(i); EditorUtility.SetDirty(catalog);
                        Undo.CollapseUndoOperations(group); GUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUILayout.LabelField("고정 ID · " + category.id, EditorStyles.miniLabel);
        }
        EditorGUILayout.EndScrollView();
        using (new EditorGUILayout.HorizontalScope())
        {
            _newLabel = EditorGUILayout.TextField("새 분류", _newLabel);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newLabel)))
                if (GUILayout.Button("추가", GUILayout.Width(50)))
                {
                    Undo.RecordObject(catalog, "물품 분류 추가");
                    catalog.categories.Add(new ValuableCategoryCatalogSO.Category { id = Guid.NewGuid().ToString("N"), label = _newLabel.Trim() });
                    _newLabel = ""; EditorUtility.SetDirty(catalog);
                }
        }
        if (GUILayout.Button("분류 및 물품 저장"))
        {
            AssetDatabase.SaveAssetIfDirty(catalog);
            foreach (var guid in AssetDatabase.FindAssets("t:ValuableItemSO"))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<ValuableItemSO>(AssetDatabase.GUIDToAssetPath(guid)));
        }
    }
}

[CustomEditor(typeof(ValuableItemSO), true), CanEditMultipleObjects]
internal sealed class ValuableItemCategoryInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "_valuableCategoryId", "_maxStack");
        ValuableCategoryEditorWindow.DrawAssignment(serializedObject.FindProperty("_valuableCategoryId"));
        EditorGUILayout.LabelField("최대 스택", target is CurrencyItemSO ? CurrencyItemSO.CurrencyStackLimit.ToString("N0") : "1");
        serializedObject.ApplyModifiedProperties();
        if (GUILayout.Button("물품 분류 관리")) ValuableCategoryEditorWindow.Open();
    }
}

[CustomEditor(typeof(ValuableCategoryCatalogSO))]
internal sealed class ValuableCategoryCatalogInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("분류 ID는 아이템과 루팅 가중치가 공유하므로 직접 변경하지 않습니다. 관리 창에서 이름·순서·추가·삭제를 편집하세요.", MessageType.Info);
        foreach (var category in ((ValuableCategoryCatalogSO)target).categories)
            EditorGUILayout.LabelField(category.label, category.id);
        if (GUILayout.Button("Item Maker 물품 분류 관리")) ValuableCategoryEditorWindow.Open();
    }
}
