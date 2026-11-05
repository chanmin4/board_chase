using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class MakerRegistrationUi
{
    internal static float BadgeHeight(string text, float width) => Mathf.Max(18, EditorStyles.wordWrappedMiniLabel.CalcHeight(new GUIContent(text), width));
    internal static void Badge(Rect rect, ItemSO item, string context = null)
    {
        if (item == null) return;
        var record = MakerRegistrationIndex.Current.For(item);
        var style = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
        style.normal.textColor = record.NeedsCheck ? new Color(1, .75f, .3f) : record.Registered ? new Color(.5f, .9f, .7f) : new Color(.85f, .85f, .85f);
        GUI.Label(rect, new GUIContent(context ?? record.LootLabel, record.Tooltip), style);
    }
    internal static void ItemSummary(ItemSO item, string context = null)
    {
        if (item == null) return;
        var record = MakerRegistrationIndex.Current.For(item);
        if (item is ValuableItemSO valuable) EditorGUILayout.LabelField("물품 분류 · " + valuable.ValuableCategoryLabel, EditorStyles.miniLabel);
        EditorGUILayout.LabelField(new GUIContent(context ?? record.LootLabel, record.Tooltip), EditorStyles.wordWrappedMiniLabel);
        if (context != null)
            EditorGUILayout.LabelField(new GUIContent(record.LootLabel, record.Tooltip), EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField($"상점 상품 {record.ShopCount}곳 · 퀘스트 사용 {record.QuestCount}개", EditorStyles.wordWrappedMiniLabel);
        if (record.NeedsCheck)
            EditorGUILayout.LabelField("확인 필요 · " + string.Join(" / ", record.Uses.Where(u => !string.IsNullOrEmpty(u.Warning)).Select(u => u.Warning).Distinct()), EditorStyles.wordWrappedMiniLabel);
    }
    internal static void UsageList(ItemSO item)
    {
        if (item == null) return;
        ItemSummary(item);
        EditorGUILayout.LabelField("등록된 사용처 — 클릭해서 확인", EditorStyles.boldLabel);
        var record = MakerRegistrationIndex.Current.For(item);
        if (record.Uses.Count == 0) EditorGUILayout.LabelField("상자·적 드롭·상점 상품·퀘스트 사용처 없음", EditorStyles.wordWrappedMiniLabel);
        foreach (var use in record.Uses.GroupBy(u => (u.Owner, u.Source, u.Stage, u.Kind)).Select(g => g.First()))
            if (GUILayout.Button(new GUIContent(use.Label, use.Warning), new GUIStyle(EditorStyles.miniButton) { wordWrap = true }))
            {
                if (use.Kind == MakerRegistrationIndex.Kind.Box) LootProgressionEditorWindow.OpenAndFocusWorldTable(use.Table, item);
                else if (use.Kind == MakerRegistrationIndex.Kind.Enemy) LootProgressionEditorWindow.OpenAndSelectSource((LootProgressionConfigSO)use.Owner, use.Source, use.Stage);
                else { Selection.activeObject = use.Owner; EditorGUIUtility.PingObject(use.Owner); }
            }
        EditorGUILayout.LabelField("등록은 참조 존재 여부입니다. 확률 0·배치·해금 조건 등에 따라 실제 등장하지 않을 수 있습니다.", EditorStyles.wordWrappedMiniLabel);
    }
    internal static void PrefabSummary(GameObject prefab)
    {
        if (prefab == null) return;
        var items = MakerRegistrationIndex.Current.ForPrefab(prefab);
        EditorGUILayout.LabelField(items.Count == 0 ? "Item SO 연결 없음 (월드·착용·손 모델 기준)" : $"Item SO 연결됨 · {items.Count}개", EditorStyles.wordWrappedMiniLabel);
        foreach (var item in items)
            if (GUILayout.Button(item.name, EditorStyles.miniButton)) { Selection.activeObject = item; EditorGUIUtility.PingObject(item); }
        foreach (var record in MakerRegistrationIndex.Current.RecordsForPrefab(prefab))
            if (GUILayout.Button((record is LootObjectSO ? "루팅 상자 등록: " : "Room Preset 등록: ") + record.name, EditorStyles.miniButton))
            { Selection.activeObject = record; EditorGUIUtility.PingObject(record); }
    }
}
