using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class LootProgressionEditorWindow
{
    private bool _showRegistrationCandidates = true, _onlyUnregisteredCandidates = true;
    private string _candidateSearch = "";
    private Vector2 _candidateScroll;
    private LootTableSO _candidateTable;
    private readonly HashSet<ItemSO> _candidateSelection = new();
    private string _candidateCategoryKey = "", _candidateViewKey = "";
    private HashSet<string> _candidateExpanded = new(), _candidateFilterCollapsed = new();
    private HashSet<string> CandidateExpanded => _candidateExpanded ??= new();
    private HashSet<string> CandidateFilterCollapsed => _candidateFilterCollapsed ??= new();

    private bool CandidateCategoryExpanded(string key) => CandidateFilterFocused
        ? !CandidateFilterCollapsed.Contains(key) : CandidateExpanded.Contains(key);
    private bool CandidateFilterFocused => !string.IsNullOrEmpty(_candidateCategoryKey) || !string.IsNullOrWhiteSpace(_candidateSearch);
    private void SetCandidateCategoryExpanded(string key, bool expanded)
    {
        var keys = CandidateFilterFocused ? CandidateFilterCollapsed : CandidateExpanded;
        if (expanded != CandidateFilterFocused) keys.Add(key); else keys.Remove(key);
    }

    private void DrawRegistrationCandidates(LootTableSO table)
    {
        if (_candidateTable != table) { _candidateTable = table; _candidateSelection.Clear(); }
        _showRegistrationCandidates = EditorGUILayout.Foldout(_showRegistrationCandidates, "아이템 목록에서 추가 — 현재 상자 기준", true);
        if (!_showRegistrationCandidates) return;
        if (GUILayout.Button("물품 분류 편집 · Item Maker 공용")) ValuableCategoryEditorWindow.Open();
        _onlyUnregisteredCandidates = EditorGUILayout.ToggleLeft("이 상자에 미등록인 아이템만 보기", _onlyUnregisteredCandidates);
        var items = MakerRegistrationIndex.Current.Items;
        var registered = MakerRegistrationIndex.ItemsIn(table);
        _candidateSelection.RemoveWhere(i => i == null || registered.Contains(i));
        var categories = LootMakerItemView.CandidateGroups(items, null, false, "");
        var categoryKeys = new[] { "" }.Concat(categories.Select(group => group.Key)).ToArray();
        var categoryLabels = new[] { "전체 카테고리" }.Concat(categories.Select(group => group.Label)).ToArray();
        int categoryIndex = System.Array.IndexOf(categoryKeys, _candidateCategoryKey);
        _candidateCategoryKey = categoryKeys[EditorGUILayout.Popup("카테고리", Mathf.Max(0, categoryIndex), categoryLabels)];
        ObserveGuiControl("LootCandidates/CategoryFilter", GUILayoutUtility.GetLastRect());
        _candidateSearch = EditorGUILayout.TextField("이름 / 카테고리 검색", _candidateSearch);
        ObserveGuiControl("LootCandidates/Search", GUILayoutUtility.GetLastRect());
        var groups = LootMakerItemView.CandidateGroups(items, registered, _onlyUnregisteredCandidates, _candidateSearch, _candidateCategoryKey);
        string viewKey = _candidateCategoryKey + "\n" + _candidateSearch.Trim() + "\n" + _onlyUnregisteredCandidates;
        if (_candidateViewKey != viewKey)
        {
            _candidateViewKey = viewKey; CandidateFilterCollapsed.Clear(); _candidateScroll = Vector2.zero;
            if (Event.current.type != EventType.Layout) { Repaint(); GUIUtility.ExitGUI(); }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("분류 모두 펼치기"))
            { foreach (var group in groups) SetCandidateCategoryExpanded(group.Key, true); Repaint(); GUIUtility.ExitGUI(); }
            ObserveGuiControl("LootCandidates/ExpandAll", GUILayoutUtility.GetLastRect());
            if (GUILayout.Button("분류 모두 접기"))
            { foreach (var group in groups) SetCandidateCategoryExpanded(group.Key, false); Repaint(); GUIUtility.ExitGUI(); }
            ObserveGuiControl("LootCandidates/CollapseAll", GUILayoutUtility.GetLastRect());
        }
        using (var scroll = new EditorGUILayout.ScrollViewScope(_candidateScroll, GUILayout.Height(180)))
        {
            _candidateScroll = scroll.scrollPosition;
            foreach (var group in groups)
            {
                int selectedCount = group.Items.Count(_candidateSelection.Contains);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool currentExpanded = CandidateCategoryExpanded(group.Key);
                    bool expanded = EditorGUILayout.Foldout(currentExpanded,
                        new GUIContent($"{group.Label} ({group.Items.Length})" + (selectedCount > 0 ? $" · 선택 {selectedCount}" : ""), group.Label), true);
                    ObserveGuiControl("LootCandidates/Category/" + group.Key, GUILayoutUtility.GetLastRect());
                    if (expanded != currentExpanded)
                    { SetCandidateCategoryExpanded(group.Key, expanded); Repaint(); GUIUtility.ExitGUI(); }
                    using (new EditorGUI.DisabledScope(group.Items.All(registered.Contains)))
                        if (GUILayout.Button(new GUIContent("분류 선택", "이 분류의 미등록 아이템만 선택합니다."), GUILayout.Width(72)))
                            foreach (var item in group.Items.Where(i => !registered.Contains(i))) _candidateSelection.Add(item);
                }
                if (!CandidateCategoryExpanded(group.Key)) continue;
                foreach (var item in group.Items)
                {
                    bool exists = registered.Contains(item);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(12);
                        using (new EditorGUI.DisabledScope(exists))
                        {
                            bool selected = EditorGUILayout.Toggle(_candidateSelection.Contains(item), GUILayout.Width(18));
                            if (selected) _candidateSelection.Add(item); else _candidateSelection.Remove(item);
                        }
                        GUILayout.Label(new GUIContent($"[{item.Rarity}] {item.name}", MakerRegistrationIndex.Current.For(item).Tooltip), EditorStyles.wordWrappedMiniLabel, GUILayout.MinWidth(120));
                        ObserveGuiControl("LootCandidates/Item/" + item.GetInstanceID(), GUILayoutUtility.GetLastRect());
                        GUILayout.Label(exists ? "이 상자에 등록됨" : "이 상자에는 미등록", EditorStyles.miniLabel, GUILayout.Width(124));
                        if (GUILayout.Button("SO", GUILayout.Width(30))) { Selection.activeObject = item; EditorGUIUtility.PingObject(item); }
                    }
                }
            }
            if (groups.Count == 0) EditorGUILayout.LabelField("조건에 맞는 아이템 없음", EditorStyles.miniLabel);
        }
        var visible = LootMakerItemView.ExpandedCandidates(groups, CandidateCategoryExpanded).ToArray();
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!visible.Any(i => !registered.Contains(i))))
                if (GUILayout.Button("표시 중인 미등록 선택"))
                    foreach (var item in visible.Where(i => !registered.Contains(i))) _candidateSelection.Add(item);
            ObserveGuiControl("LootCandidates/SelectVisible", GUILayoutUtility.GetLastRect());
            if (GUILayout.Button("선택 해제")) _candidateSelection.Clear();
            using (new EditorGUI.DisabledScope(_candidateSelection.Count == 0))
                if (GUILayout.Button($"선택 {_candidateSelection.Count}개 추가"))
                {
                    RecordLootTable(table, "Add Selected Loot Candidates");
                    int added = AddItemsToWorldPool(table.Pool, _candidateSelection);
                    _candidateSelection.Clear(); MakerRegistrationIndex.Invalidate();
                    ShowNotification(new GUIContent($"{added}개 추가됨. 기존 항목·수량·비중은 유지합니다."));
                }
        }
        int hiddenSelected = _candidateSelection.Count(i => !visible.Contains(i));
        if (hiddenSelected > 0)
        {
            EditorGUILayout.LabelField($"접힌 분류 / 필터 밖의 선택 {hiddenSelected}개도 추가 대상에 포함됩니다. 필요하면 선택 해제를 누르세요.", EditorStyles.wordWrappedMiniLabel);
            ObserveGuiControl("LootCandidates/HiddenSelection", GUILayoutUtility.GetLastRect());
        }
        EditorGUILayout.LabelField("다른 상자에 등록된 아이템도 추가할 수 있습니다. 추가하면 같은 등급의 상대 확률은 재계산됩니다. 확률 설정은 직접 변경하지 않습니다.", EditorStyles.wordWrappedMiniLabel);
    }
}
