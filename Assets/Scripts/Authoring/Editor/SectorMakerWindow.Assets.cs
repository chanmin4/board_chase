using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class SectorMakerWindow
{
    [SerializeField] private int _libraryTab;
    [SerializeField] private int _roomWorkbenchTab;
    [SerializeField] private SectorPresetSO _existingPreset;
    [SerializeField] private string _presetNameDraft;
    [SerializeField] private List<SectorPresetSO> _selectedPresets = new();
    [SerializeField] private SectorPresetSO _presetSelectionAnchor;
    [NonSerialized] private bool _preservePresetSelection;
    [NonSerialized] private MakerSelectionUiState _presetNameUi;

    private void DrawRoomLibrary()
    {
        _libraryTab = GUILayout.Toolbar(_libraryTab, new[] { "프리팹", "Room SO", "Base" }); Observe("LibraryTabs");
        if (GUILayout.Button("+ Base에서 새 방 만들기")) { _libraryTab = 2; _catalogScroll = Vector2.zero; }
        Observe("ChooseBase");
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("기존 SO", GUILayout.Width(48));
            _existingPreset = (SectorPresetSO)EditorGUILayout.ObjectField(_existingPreset, typeof(SectorPresetSO), false);
            using (new EditorGUI.DisabledScope(_existingPreset == null))
                if (GUILayout.Button("선택", GUILayout.Width(44))) SelectPreset(_existingPreset, false);
        }
        if (_libraryTab == 1) { DrawPresetLibrary(); return; }
        if (_libraryTab == 2)
        {
            EditorGUILayout.LabelField("원본 / 1/2 / 1/4와 종류별 Base", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("누락된 Base SO 등록"))
                    AssetAction(() => { SectorMakerAssets.EnsureBasePresets(); RefreshCatalog(); });
            Observe("RegisterBases");
        }
        DrawPrefabLibrary();
    }
    private SectorMakerCategory PresetCategory(SectorPresetSO preset)
    {
        if (SectorMakerCatalog.TryPrefab(preset, out var prefab)) return _rooms.Find(r => r.prefab == prefab)?.category ?? SectorMakerCatalog.Infer(AssetDatabase.GetAssetPath(prefab));
        return SectorMakerCatalog.Infer(AssetDatabase.GetAssetPath(preset));
    }
    private void DrawPresetLibrary()
    {
        PrunePresetSelection();
        EditorGUILayout.LabelField("Room SO · Project의 기존 SO 자동 표시", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("클릭: 하나 / Ctrl: 추가·해제 / Shift: 범위", EditorStyles.wordWrappedMiniLabel);
        _roomSearch = EditorGUILayout.TextField(_roomSearch, EditorStyles.toolbarSearchField);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("전체 펼치기")) Array.Fill(_roomGroups, true);
            if (GUILayout.Button("전체 접기")) Array.Fill(_roomGroups, false);
        }
        _catalogScroll = EditorGUILayout.BeginScrollView(_catalogScroll);
        for (int c = 0; c < SectorMakerCatalog.Labels.Length; c++)
        {
            var presets = SectorMakerCatalog.Presets.Where(p => p != null && (int)PresetCategory(p) == c &&
                (string.IsNullOrWhiteSpace(_roomSearch) || (p.name + " " + (p.SectorPrefab != null ? p.SectorPrefab.name : "") + " " + SectorMakerCatalog.Labels[c]).IndexOf(_roomSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(p => p.name, Comparer<string>.Create(EditorUtility.NaturalCompare)).ToArray();
            if (presets.Length == 0 && !string.IsNullOrWhiteSpace(_roomSearch)) continue;
            _roomGroups[c] = EditorGUILayout.Foldout(_roomGroups[c], SectorMakerCatalog.Labels[c] + " · SO " + presets.Length + "개", true);
            if (!_roomGroups[c] && string.IsNullOrWhiteSpace(_roomSearch)) continue;
            foreach (var preset in presets)
            {
                string title = preset.name + (SectorMakerAssets.IsTemplate(preset) ? " [Base SO]" : "") +
                    "\n" + (preset.SectorPrefab != null ? "→ " + preset.SectorPrefab.name : "⚠ 프리팹 연결 없음");
                var old = GUI.backgroundColor;
                if (_selectedPresets.Contains(preset)) GUI.backgroundColor = new Color(.35f, .85f, 1);
                if (GUILayout.Button(new GUIContent(title, AssetDatabase.GetAssetPath(preset)), new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.MiddleLeft }, GUILayout.Height(44)))
                    ClickPreset(preset, Event.current.control || Event.current.command, Event.current.shift, VisiblePresets());
                Observe("Preset/" + preset.name); GUI.backgroundColor = old;
            }
        }
        EditorGUILayout.EndScrollView();
        int hidden = _selectedPresets.Except(VisiblePresets()).Count();
        EditorGUILayout.LabelField("선택한 SO " + _selectedPresets.Count + "개" + (hidden > 0 ? " · 숨겨진 선택 " + hidden + "개" : ""), EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("보이는 항목 선택")) SelectPresetRange(VisiblePresets()); Observe("SelectVisiblePresets");
            if (GUILayout.Button("선택 해제")) SelectPresetRange(Array.Empty<SectorPresetSO>()); Observe("ClearPresetSelection");
        }
        using (new EditorGUI.DisabledScope(_selectedPresets.Count == 0 || EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("선택한 SO " + _selectedPresets.Count + "개 삭제…", GUILayout.Height(28))) DeleteSelectedPresets();
        Observe("DeleteSelectedPresets");
    }
    private void PrunePresetSelection()
    {
        _selectedPresets ??= new List<SectorPresetSO>();
        _selectedPresets.RemoveAll(p => p == null);
    }
    private void ResetPresetSelection(SectorPresetSO preset)
    {
        PrunePresetSelection(); _selectedPresets.Clear();
        if (preset != null) _selectedPresets.Add(preset);
        _presetSelectionAnchor = preset;
    }
    private SectorPresetSO[] VisiblePresets()
    {
        bool searching = !string.IsNullOrWhiteSpace(_roomSearch);
        return SectorMakerCatalog.Presets.Where(p => p != null).Select(p => (preset: p, category: (int)PresetCategory(p)))
            .Where(p => (searching || _roomGroups[p.category]) && (!searching ||
                (p.preset.name + " " + (p.preset.SectorPrefab != null ? p.preset.SectorPrefab.name : "") + " " + SectorMakerCatalog.Labels[p.category])
                    .IndexOf(_roomSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0))
            .OrderBy(p => p.category).ThenBy(p => p.preset.name, Comparer<string>.Create(EditorUtility.NaturalCompare))
            .Select(p => p.preset).ToArray();
    }
    private void ClickPreset(SectorPresetSO preset, bool additive, bool range, SectorPresetSO[] visible)
    {
        if (preset == null) return;
        PrunePresetSelection();
        int anchor = Array.IndexOf(visible, _presetSelectionAnchor), clicked = Array.IndexOf(visible, preset);
        if (range && anchor >= 0 && clicked >= 0)
        {
            if (!additive) _selectedPresets.Clear();
            for (int i = Math.Min(anchor, clicked); i <= Math.Max(anchor, clicked); i++)
                if (!_selectedPresets.Contains(visible[i])) _selectedPresets.Add(visible[i]);
        }
        else
        {
            if (!additive) _selectedPresets.Clear();
            if (additive && _selectedPresets.Contains(preset)) _selectedPresets.Remove(preset);
            else _selectedPresets.Add(preset);
            _presetSelectionAnchor = preset;
        }
        FocusPreset(_selectedPresets.Contains(preset) ? preset : _selectedPresets.LastOrDefault(), false);
    }
    private void SelectPresetRange(SectorPresetSO[] presets)
    {
        PrunePresetSelection(); _selectedPresets.Clear(); _selectedPresets.AddRange(presets.Where(p => p != null).Distinct());
        _presetSelectionAnchor = _selectedPresets.FirstOrDefault();
        FocusPreset(_selectedPresets.LastOrDefault(), false);
    }
    private void SelectPreset(SectorPresetSO preset, bool open)
    {
        if (preset == null) return;
        ResetPresetSelection(preset); FocusPreset(preset, open);
    }
    private void FocusPreset(SectorPresetSO preset, bool open)
    {
        _preservePresetSelection = true;
        try
        {
            if (preset != null && SectorMakerCatalog.TryPrefab(preset, out var prefab)) SelectRoom(prefab, open, preset);
            else
            {
                _roomPrefab = null; SetActiveSectorRuntime(null); DisposeSelectionEditor(); SetActiveSectorPreset(preset);
            }
            _roomWorkbenchTab = 1;
        }
        finally { _preservePresetSelection = false; }
        Repaint();
    }
    private void DrawMultiPresetWorkbench()
    {
        EditorGUILayout.LabelField("선택한 Room SO · " + _selectedPresets.Count + "개", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("여러 SO가 선택됐습니다. 개별 이름·데이터를 편집하려면 하나를 일반 클릭하세요.\n일괄 삭제 시 사용 중이거나 저장하지 않은 SO는 제외합니다.", MessageType.Info);
        foreach (var preset in _selectedPresets)
            EditorGUILayout.LabelField(preset.name + "\n" + AssetDatabase.GetAssetPath(preset), EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("선택한 SO " + _selectedPresets.Count + "개 삭제…", GUILayout.Height(30))) DeleteSelectedPresets();
        Observe("DeleteSelectedPresetsWorkbench");
    }
    private void DrawPrefabSource()
    {
        var parent = PrefabUtility.GetCorrespondingObjectFromSource(_roomPrefab);
        EditorGUILayout.LabelField(CurrentRoom?.isBase == true ? "공통 Base · 새 방의 기준" : "방 프리팹", EditorStyles.miniBoldLabel);
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("상속 원본 프리팹", parent, typeof(GameObject), false);
        if (CurrentRoom?.isBase == true)
            EditorGUILayout.HelpBox("아래 ‘새 방 만들기’로 Variant와 SO를 함께 생성하세요. Base 직접 편집은 모든 자식 방에 영향을 줄 수 있습니다.", MessageType.Info);
    }
    private void DrawNewRoomPlan(SectorMakerCatalog.Room room)
    {
        DrawPrefabSource();
        EditorGUILayout.HelpBox("① 왼쪽 Base 탭에서 기준을 고릅니다.\n② 기준 SO를 선택합니다.\n③ 프리팹과 SO를 저장할 경로를 각각 고릅니다.\n프리팹은 Variant로 상속, SO와 콘텐츠는 독립 복사됩니다. 기존 방·스테이지는 바꾸지 않습니다.", MessageType.Info);
        if (room != null)
        {
            var names = new[] { "기준 SO 선택…" }.Concat(room.presets.Select(p => p.name)).ToArray();
            int index = room.presets.IndexOf(_activeSectorPreset) + 1;
            int next = EditorGUILayout.Popup("복사할 기준 SO", index, names);
            if (next != index) SetActiveSectorPreset(next == 0 ? null : room.presets[next - 1]);
        }
        if (!PresetMatchesRoom) _newPresetType = (StageRoomType)EditorGUILayout.EnumPopup("새 SO 게임 방 타입", _newPresetType);
        else EditorGUILayout.LabelField("SO 방 타입", _activeSectorPreset.RoomType.ToString());
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("새 방 생성 · Variant + SO…", GUILayout.Height(34))) CreateRoomVariant();
        Observe("CreateRoomPair");
        if (room?.isBase == true && !room.presets.Any(SectorMakerAssets.IsTemplate))
        {
            EditorGUILayout.HelpBox("이 Base의 기준 SO가 아직 없습니다. 왼쪽 ‘누락된 Base SO 등록’을 누르세요.", MessageType.Warning);
        }
    }
    private void CreatePresetForRoom()
    {
        string path = EditorUtility.SaveFilePanelInProject("새 Room SO", "SectorPreset_" + _roomPrefab.name, "asset", "이 프리팹을 사용하는 새 SO를 추가합니다. 스테이지는 바꾸지 않습니다.");
        if (string.IsNullOrEmpty(path)) return;
        AssetAction(() => { var preset = SectorMakerCatalog.CreatePreset(_roomPrefab, path, _newPresetType); RefreshCatalog(); SetActiveSectorPreset(preset); });
    }
    private void DrawPresetActions()
    {
        if (_activeSectorPreset == null) return;
        using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("선택된 Room SO", _activeSectorPreset, typeof(SectorPresetSO), false);
        EditorGUILayout.LabelField("게임 방 타입", _activeSectorPreset.RoomType.ToString());
        EditorGUILayout.LabelField("프리팹 = Variant 상속 / SO = 독립 데이터 (자동 상속 없음)", EditorStyles.wordWrappedMiniLabel);
        _presetNameUi ??= new MakerSelectionUiState();
        _presetNameDraft = _presetNameUi.NameFor(_activeSectorPreset, _presetNameDraft);
        _presetNameDraft = EditorGUILayout.TextField("SO 파일 이름", _presetNameDraft);
        if (GUILayout.Button("SO 이름 변경")) RenamePreset(); Observe("RenamePreset");
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("SO 복사 추가…")) CopySelectedPreset(); Observe("CopyPreset");
            if (GUILayout.Button("SO 삭제…")) DeleteSelectedPreset(); Observe("DeletePreset");
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("SO 저장")) { AssetDatabase.SaveAssetIfDirty(_activeSectorPreset); ShowNotification(new GUIContent("선택한 SO만 저장했습니다.")); }
            if (GUILayout.Button("SO Inspector")) Selection.activeObject = _activeSectorPreset;
        }
        EditorGUILayout.LabelField("기존 SO는 왼쪽 Room SO 목록이나 ‘기존 SO’ 입력칸에서 선택합니다. 삭제는 SO만 휴지통으로 보내며, 사용 중이면 차단합니다.", EditorStyles.wordWrappedMiniLabel);
    }
    private void CopySelectedPreset()
    {
        if (!PresetMatchesRoom) { ShowNotification(new GUIContent("SO의 프리팹 연결을 먼저 복구하세요.")); return; }
        string path = EditorUtility.SaveFilePanelInProject("Room SO 독립 복사", _activeSectorPreset.name + "_Copy", "asset", "콘텐츠 설정도 독립 복사합니다. 프리팹과 스테이지는 그대로입니다.");
        if (string.IsNullOrEmpty(path)) return;
        AssetAction(() => { var copy = SectorMakerAssets.CopyPreset(_activeSectorPreset, _roomPrefab, path); RefreshCatalog(); SetActiveSectorPreset(copy); });
    }
    private void RenamePreset()
    {
        AssetAction(() =>
        {
            string name = (_presetNameDraft ?? "").Trim();
            if (name.Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..") throw new ArgumentException("유효한 SO 이름을 입력하세요.");
            string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(_activeSectorPreset), name);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            RefreshCatalog();
        });
    }
    private void DeleteSelectedPreset()
    {
        ResetPresetSelection(_activeSectorPreset); DeleteSelectedPresets();
    }
    private void DeleteSelectedPresets()
    {
        PrunePresetSelection();
        if (_selectedPresets.Count == 0) return;
        AssetAction(() =>
        {
            var plan = SectorMakerAssets.PlanTrashPresets(_selectedPresets);
            var deletable = plan.Where(e => e.CanDelete).ToArray();
            var blocked = plan.Where(e => !e.CanDelete).ToArray();
            string Preview(SectorMakerAssets.TrashEntry[] items, bool reasons) => string.Join("\n", items.Take(8).Select(e =>
                "• " + e.name + (reasons ? " · " + e.reason + (e.users.Length > 0 ? "\n  " + e.users[0] : "") : ""))) +
                (items.Length > 8 ? "\n… 외 " + (items.Length - 8) + "개 (전체 대상은 Console에 기록)" : "");
            string details = "[Sector Maker SO 삭제 검사]\n" + string.Join("\n", plan.Select(e => e.path + " · " +
                (e.CanDelete ? "삭제 가능" : e.reason + "\n" + string.Join("\n", e.users))));
            if (plan.Length > 8) Debug.Log(details);
            if (deletable.Length == 0)
            { EditorUtility.DisplayDialog("삭제 가능한 SO 없음", Preview(blocked, true), "확인"); return; }
            string message = "휴지통으로 이동: " + deletable.Length + "개\n" + Preview(deletable, false) +
                (blocked.Length > 0 ? "\n\n유지 / 삭제 제외: " + blocked.Length + "개\n" + Preview(blocked, true) : "") +
                "\n\nSO만 삭제합니다. 방 프리팹·콘텐츠 SO·Stage 연결은 변경하지 않습니다.";
            if (!EditorUtility.DisplayDialog("Room SO 일괄 삭제", message, "미사용 " + deletable.Length + "개 휴지통", "취소")) return;
            var result = SectorMakerAssets.TrashPresets(plan);
            PrunePresetSelection(); RefreshCatalog(); FocusPreset(_selectedPresets.LastOrDefault(), false);
            ShowNotification(new GUIContent("SO " + result.deleted.Count + "개 휴지통 · 유지 " + result.skipped.Count + "개 · 실패 " + result.failed.Count + "개"));
            if (result.failed.Count > 0) EditorUtility.DisplayDialog("일부 SO 삭제 실패", string.Join("\n", result.failed), "확인");
            GUIUtility.ExitGUI();
        });
    }
    private void DrawOrphanPreset()
    {
        EditorGUILayout.LabelField(_activeSectorPreset.name, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("이 SO의 섹터 프리팹 연결이 없습니다. Inspector에서 연결을 복구하거나 사용하지 않는 SO를 삭제할 수 있습니다.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode)) DrawPresetActions();
    }
    private void AssetAction(Action action)
    {
        try { action(); }
        catch (ExitGUIException) { throw; }
        catch (Exception ex) { EditorUtility.DisplayDialog("Sector Maker · 작업할 수 없음", ex.Message, "확인"); }
    }
}
