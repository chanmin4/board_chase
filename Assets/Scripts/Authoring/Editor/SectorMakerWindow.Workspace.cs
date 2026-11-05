using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed partial class SectorMakerWindow
{
    [SerializeField] private GameObject _roomPrefab;
    [SerializeField] private string _roomSearch = "";
    [SerializeField] private string _prefabNameDraft;
    [NonSerialized] private MakerSelectionUiState _prefabNameUi;
    [SerializeField] private bool _showGuide = true, _showBases, _showLegacy, _advancedPlacement, _showLinks;
    [SerializeField] private float _catalogWidth = 260f;
    [SerializeField] private Vector2 _catalogScroll, _roomScroll;
    [SerializeField] private int _workbenchTab;
    [SerializeField] private StageRoomType _newPresetType = StageRoomType.Normal;
    [SerializeField] private bool[] _roomGroups = Enumerable.Repeat(true, 11).ToArray();
    private List<SectorMakerCatalog.Room> _rooms = new();
    [NonSerialized] private Editor _selectionEditor;
    private MakerSelectionUiState _roomSelectionUi, _sceneSelectionUi;
    private MakerSelectionUiState RoomSelectionUi => _roomSelectionUi ??= new MakerSelectionUiState();
    private MakerSelectionUiState SceneSelectionUi => _sceneSelectionUi ??= new MakerSelectionUiState();
    [NonSerialized] private UnityEngine.Object _selectionEditorTarget;
    private bool _refreshPending;
    internal Action<string, Rect> ObserveControl; // Optional isolated UI verification; no runtime dependency.
    private Vector2 _workspaceOrigin;
    private void Observe(string id)
    {
        if (ObserveControl == null || Event.current.type != EventType.Repaint) return;
        var rect = GUILayoutUtility.GetLastRect();
        rect.position = GUIUtility.GUIToScreenPoint(rect.position) - _workspaceOrigin;
        ObserveControl(id, rect);
    }
    private string RoomPath => _roomPrefab != null ? AssetDatabase.GetAssetPath(_roomPrefab) : "";
    private SectorMakerCatalog.Room CurrentRoom => _rooms?.Find(r => r.prefab == _roomPrefab);
    private bool PresetMatchesRoom => _activeSectorPreset != null && _activeSectorPreset.SectorPrefab != null &&
        AssetDatabase.GetAssetPath(_activeSectorPreset.SectorPrefab) == RoomPath;
    private bool IsEditingRoom
    {
        get
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            return !EditorApplication.isPlayingOrWillChangePlaymode && _activeSectorRuntime != null && stage != null &&
                stage.assetPath == RoomPath && stage.IsPartOfPrefabContents(_activeSectorRuntime.gameObject);
        }
    }

    private void RefreshCatalog()
    {
        _rooms = SectorMakerCatalog.Scan();
        _refreshPending = false;
        if (_roomGroups == null || _roomGroups.Length != SectorMakerCatalog.Labels.Length)
            _roomGroups = Enumerable.Repeat(true, SectorMakerCatalog.Labels.Length).ToArray();
        Repaint();
    }
    private void OnProjectChange() { _refreshPending = true; Repaint(); }
    private void OnInspectorUpdate()
    {
        if (_refreshPending && !EditorApplication.isCompiling && !EditorApplication.isUpdating) RefreshCatalog();
        SyncRoomStage();
        Repaint();
    }
    private void OnRoomStageOpened(PrefabStage stage)
    {
        SyncRoomStage();
        if (IsEditingRoom) RequestCompactMode(true);
    }
    private void OnRoomStageClosing(PrefabStage stage)
    {
        if (_activeSectorRuntime != null && stage.IsPartOfPrefabContents(_activeSectorRuntime.gameObject))
        { _paintingEnabled = false; SetActiveSectorRuntime(null); DisposeSelectionEditor(); RequestCompactMode(false); }
    }
    private void SyncRoomStage()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        var root = stage != null && stage.assetPath == RoomPath ? stage.prefabContentsRoot?.GetComponent<SectorRuntime>() : null;
        if (root != _activeSectorRuntime) SetActiveSectorRuntime(root);
        if (!IsEditingRoom) _paintingEnabled = false;
    }
    private void SelectRoom(GameObject prefab, bool open, SectorPresetSO preferred = null)
    {
        if (!SectorMakerCatalog.TryPrefab(prefab, out prefab))
        { ShowNotification(new GUIContent("SectorRuntime이 루트에 있는 섹터 프리팹을 넣어주세요.")); return; }
        _paintingEnabled = false;
        _workbenchTab = 0;
        _roomPrefab = prefab;
        RoomSelectionUi.SelectionClicked();
        _prefabNameUi?.SelectionClicked();
        DisposeSelectionEditor();
        var room = CurrentRoom;
        if (room == null)
        {
            SectorMakerCatalogSettings.instance.Set(AssetDatabase.AssetPathToGUID(RoomPath), SectorMakerCatalog.Infer(RoomPath));
            RefreshCatalog(); room = CurrentRoom;
        }
        SetActiveSectorPreset(preferred != null && preferred.SectorPrefab != null && AssetDatabase.GetAssetPath(preferred.SectorPrefab) == RoomPath
            ? preferred : room?.presets.Count == 1 ? room.presets[0] : null);
        _newPresetType = room?.category == SectorMakerCategory.Boss ? StageRoomType.Boss :
            room?.category == SectorMakerCategory.Start || room?.category == SectorMakerCategory.Home ? StageRoomType.Start : StageRoomType.Normal;
        _roomWorkbenchTab = room?.isBase == true ? 2 : 0;
        SyncRoomStage();
        if (open) OpenRoomForEditing();
        Repaint();
    }
    private void OpenRoomForEditing()
    {
        if (!SectorMakerCatalog.TryPrefab(_roomPrefab, out _) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (CurrentRoom?.isBase == true && !EditorUtility.DisplayDialog("공통 Base 편집", "이 Base의 변경은 자식 Variant에도 반영됩니다. 개별 방을 만들려면 취소 후 '새 Variant 만들기'를 사용하세요.", "Base 편집", "취소")) return;
        _paintingEnabled = false;
        // Unity's native stage switch owns the unsaved-prefab prompt. Never close/save a working scene here.
        var stage = PrefabStageUtility.OpenPrefab(RoomPath);
        if (stage == null || stage.assetPath != RoomPath) return;
        SyncRoomStage();
        FrameRoom(false);
        RequestCompactMode(true);
    }
    private void FrameRoom(bool top)
    {
        if (!IsEditingRoom) return;
        var scene = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView : GetWindow<SceneView>();
        Selection.activeGameObject = _activeSectorRuntime.gameObject;
        if (top) scene.rotation = Quaternion.Euler(90, 0, 0);
        if (TryCalculateRendererBounds(_activeSectorRuntime.gameObject, out var bounds)) scene.Frame(bounds, false);
        else scene.FrameSelected();
        scene.Focus(); scene.Repaint();
    }
    private void SaveEditingRoom()
    {
        SyncRoomStage();
        if (!IsEditingRoom) { ShowNotification(new GUIContent("선택한 방을 Prefab Mode에서 먼저 여세요.")); return; }
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath, out bool success);
        if (!success) { ShowNotification(new GUIContent("프리팹 저장 실패. Console을 확인하세요.")); return; }
        stage.ClearDirtiness();
        // Save only this working set, never all modified project assets or the user's scene.
        if (PresetMatchesRoom) AssetDatabase.SaveAssetIfDirty(_activeSectorPreset);
        if (_contentProfile != null && PresetMatchesRoom && _activeSectorPreset.ContentProfile == _contentProfile && CanEditContentPlan())
        {
            AssetDatabase.SaveAssetIfDirty(_contentProfile);
            SaveLootSpawnTables();
            SectorSpawnCatalogSynchronizer.Queue();
        }
        ShowNotification(new GUIContent("선택한 방과 연결된 콘텐츠 설정을 저장했어요."));
    }

    private void DrawGuidedWorkspace()
    {
        _workspaceOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
        SyncRoomStage();
        if (_compactMode) { DrawCompactWorkspace(); return; }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Sector Maker", EditorStyles.boldLabel, GUILayout.Width(110));
            GUILayout.Label("1 방 선택  →  2 Scene에서 배치  →  3 저장", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("축소 모드", EditorStyles.toolbarButton)) RequestCompactMode(true);
            Observe("CompactMode");
            _showGuide = GUILayout.Toggle(_showGuide, "? 사용 가이드", EditorStyles.toolbarButton); Observe("Guide");
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton)) RefreshCatalog();
        }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("편집할 섹터 프리팹", GUILayout.Width(120));
            UnityEngine.Object dropped = EditorGUILayout.ObjectField(_roomPrefab, typeof(UnityEngine.Object), false); Observe("RoomDrop");
            if (dropped != _roomPrefab && dropped != null)
            {
                if (dropped is SectorPresetSO preset) SelectPreset(preset, false);
                else if (SectorMakerCatalog.TryPrefab(dropped, out var prefab)) SelectRoom(prefab, false);
            }
            GUILayout.Label("← 프리팹 / Room SO 드래그로 선택", EditorStyles.miniLabel, GUILayout.Width(240));
        }
        if (_showGuide)
            EditorGUILayout.HelpBox("① 방 선택 → 'Scene에서 편집 시작'.\n② 구조물 / 루팅 오브젝트 / 아이템 스폰 / 적 위치 선택 → 기본값 설정 → 배치 시작 → Scene 바닥 클릭.\n③ Esc로 배치 종료 → 자리 선택 후 확률/테이블 수정 → 방 저장.\n등장 여부는 자리의 고정 확률, 종류는 테이블 가중치로 결정합니다. 방 분류와 스테이지 출현 등록은 별개입니다.", MessageType.Info);
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorGUILayout.HelpBox("Play Mode를 종료하면 방을 편집할 수 있어요.", MessageType.Warning);
        using (new EditorGUILayout.HorizontalScope())
        {
            _catalogWidth = Mathf.Clamp(_catalogWidth, 190, Mathf.Max(190, position.width - 710));
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(_catalogWidth), GUILayout.ExpandHeight(true))) DrawRoomLibrary();
            DrawCatalogSplitter();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(Mathf.Clamp(position.width * .30f, 300, 430)), GUILayout.ExpandHeight(true)))
            {
                _roomScroll = EditorGUILayout.BeginScrollView(_roomScroll);
                DrawRoomWorkbench();
                EditorGUILayout.EndScrollView();
            }
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                _settingsScrollPosition = EditorGUILayout.BeginScrollView(_settingsScrollPosition);
                DrawPlacementWorkbench();
                EditorGUILayout.EndScrollView();
            }
        }
    }
    private void DrawCatalogSplitter()
    {
        Rect rect = GUILayoutUtility.GetRect(6, 6, GUILayout.ExpandHeight(true));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        int id = GUIUtility.GetControlID("SectorMaker.Split".GetHashCode(), FocusType.Passive);
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition)) { GUIUtility.hotControl = id; e.Use(); }
        if (GUIUtility.hotControl != id) return;
        if (e.type == EventType.MouseDrag) { _catalogWidth += e.delta.x; e.Use(); Repaint(); }
        if (e.type == EventType.MouseUp) { GUIUtility.hotControl = 0; e.Use(); }
    }
    private void DrawPrefabLibrary()
    {
        EditorGUILayout.LabelField("1. 방 고르기", EditorStyles.boldLabel);
        _roomSearch = EditorGUILayout.TextField(_roomSearch, EditorStyles.toolbarSearchField);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("전체 펼치기", EditorStyles.miniButtonLeft)) Array.Fill(_roomGroups, true);
            if (GUILayout.Button("전체 접기", EditorStyles.miniButtonRight)) Array.Fill(_roomGroups, false);
        }
        if (_libraryTab != 2) _showLegacy = EditorGUILayout.ToggleLeft("WIP / 이전 방도 보기", _showLegacy);
        _catalogScroll = EditorGUILayout.BeginScrollView(_catalogScroll);
        for (int c = 0; c < SectorMakerCatalog.Labels.Length; c++)
        {
            int categoryIndex = c;
            var rooms = _rooms.Where(r => (int)r.category == categoryIndex && (_libraryTab == 2 ? r.isBase : !r.isBase) && (_showLegacy || !r.isLegacy) &&
                (string.IsNullOrWhiteSpace(_roomSearch) || r.prefab.name.IndexOf(_roomSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 || SectorMakerCatalog.Labels[categoryIndex].Contains(_roomSearch.Trim()))).ToList();
            _roomGroups[c] = EditorGUILayout.Foldout(_roomGroups[c], $"{SectorMakerCatalog.Labels[c]}  ({rooms.Count})", true); Observe("Category/" + c);
            if (!_roomGroups[c] && string.IsNullOrWhiteSpace(_roomSearch)) continue;
            foreach (var room in rooms)
            {
                var old = GUI.backgroundColor;
                if (_roomPrefab == room.prefab) GUI.backgroundColor = new Color(.35f, .85f, 1);
                Texture preview = AssetPreview.GetAssetPreview(room.prefab) ?? AssetPreview.GetMiniThumbnail(room.prefab);
                var sizeLabel = room.prefab.name.Contains("HalfSize") ? " · 1/2 크기" : room.prefab.name.Contains("QuarterSize") ? " · 1/4 크기" : "";
                var label = room.prefab.name + sizeLabel + (room.isBase ? "  [Base]" : "") + (room.isLegacy ? "  [WIP]" : "") + "\nSO " + room.presets.Count + "개";
                if (GUILayout.Button(new GUIContent(label, preview, room.path + "\n클릭: 선택 / 더블클릭: Scene 편집"), new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, wordWrap = true }, GUILayout.Height(42)))
                    SelectRoom(room.prefab, Event.current.clickCount == 2, room.isBase ? room.presets.Find(SectorMakerAssets.IsTemplate) : null);
                Observe("Room/" + room.prefab.name);
                GUI.backgroundColor = old;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.LabelField("목록 경계를 드래그하면 너비가 바뀝니다.", EditorStyles.wordWrappedMiniLabel);
    }
    private void DrawRoomWorkbench()
    {
        PrunePresetSelection();
        if (_selectedPresets.Count > 1) { DrawMultiPresetWorkbench(); return; }
        RoomSelectionUi.Synchronize(_roomPrefab, _activeSectorPreset != null ? _activeSectorPreset.GetInstanceID().ToString() : "");
        EditorGUILayout.LabelField("2. Scene에서 방 편집", EditorStyles.boldLabel);
        if (_roomPrefab == null)
        {
            if (_activeSectorPreset != null) { DrawOrphanPreset(); return; }
            EditorGUILayout.HelpBox("왼쪽에서 방을 선택하세요.\n이미 만든 방은 프리팹을 위 입력칸에 드래그해도 됩니다.\n\n새 방은 Base 템플릿을 선택한 뒤 '새 Variant 만들기'로 시작하세요.", MessageType.Info);
            return;
        }
        EditorGUILayout.LabelField(_roomPrefab.name, EditorStyles.boldLabel);
        DrawRoomPrefabName();
        var room = CurrentRoom;
        _roomWorkbenchTab = GUILayout.Toolbar(_roomWorkbenchTab, new[] { "방 편집", "SO 관리", "새 방" }); Observe("RoomWorkbenchTabs");
        if (_roomWorkbenchTab == 1) { DrawPresetPicker(room); DrawRoomStageLinks(room); return; }
        if (_roomWorkbenchTab == 2) { DrawNewRoomPlan(room); return; }
        DrawPrefabSource();
        MakerRegistrationUi.PrefabSummary(_roomPrefab);
        Rect previewRect = GUILayoutUtility.GetRect(150, 125, GUILayout.ExpandWidth(true));
        var preview = AssetPreview.GetAssetPreview(_roomPrefab);
        if (preview != null) GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
        else GUI.Label(previewRect, "Scene에서 실제 방을 보며 편집할 수 있어요.", EditorStyles.centeredGreyMiniLabel);
        if (room != null)
        {
            int category = EditorGUILayout.Popup("목록 분류", (int)room.category, SectorMakerCatalog.Labels);
            if (category != (int)room.category)
            { room.category = (SectorMakerCategory)category; SectorMakerCatalogSettings.instance.Set(room.guid, room.category); }
            if (room.category == SectorMakerCategory.Start)
                EditorGUILayout.LabelField("임시방 = 기존 Start 분류. 레이드 입장·탈출 해치는 별도 ‘해치’ 분류입니다.", EditorStyles.wordWrappedMiniLabel);
        }
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button(IsEditingRoom ? "Scene으로 이동 / 방 전체 보기" : "▶ Scene에서 편집 시작", GUILayout.Height(36)))
            { if (IsEditingRoom) { FrameRoom(false); RequestCompactMode(true); } else OpenRoomForEditing(); }
            Observe("OpenRoom");
        }
        if (IsEditingRoom)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("위에서 보기")) FrameRoom(true);
                if (GUILayout.Button("프리팹 선택")) Selection.activeGameObject = _activeSectorRuntime.gameObject;
            }
            EditorGUILayout.HelpBox("● 편집 중: " + _activeSectorRuntime.name + "\nUnity Scene 탭에서 배치·이동·회전하세요. Maker는 옆에 도킹해 두면 편해요.\nPrefab Mode의 Auto Save가 켜져 있으면 변경이 즉시 저장됩니다.", MessageType.None);
        }
        else EditorGUILayout.HelpBox("아직 편집을 시작하지 않았어요. 위 버튼을 누르면 방 루트와 배치 위치가 자동 연결됩니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(!IsEditingRoom))
        {
            if (GUILayout.Button("방 저장", GUILayout.Height(30))) SaveEditingRoom(); Observe("SaveRoom");
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("새 방 만들기 (Variant + SO)…")) CreateRoomVariant();
            Observe("CreateRoomPair");
            if (GUILayout.Button("방 점검")) RunRoomReadinessCheck();
        }
        foreach (var issue in _roomReadinessIssues) EditorGUILayout.HelpBox(issue.Message, issue.MessageType);
        if (GUILayout.Button("Room SO 관리 · 추가 / 복사 / 삭제")) _roomWorkbenchTab = 1;
        _showLinks = EditorGUILayout.Foldout(_showLinks, "게임 출현 / 스테이지 등록 확인", true);
        if (_showLinks) DrawRoomStageLinks(room);
        _showAdvancedTopology = EditorGUILayout.Foldout(_showAdvancedTopology, "고급: 스테이지 동선 설계 (방 배치와 별개)", true);
        if (_showAdvancedTopology) DrawTopologySettings();
    }
    private void DrawRoomPrefabName()
    {
        _prefabNameUi ??= new MakerSelectionUiState();
        _prefabNameDraft = _prefabNameUi.NameFor(_roomPrefab, _prefabNameDraft);
        _prefabNameDraft = EditorGUILayout.TextField("프리팹 이름", _prefabNameDraft); Observe("PrefabName");
        bool canRename = !EditorApplication.isPlayingOrWillChangePlaymode && PrefabStageUtility.GetCurrentPrefabStage() == null;
        using (new EditorGUI.DisabledScope(!canRename))
            if (GUILayout.Button("프리팹 이름 변경", GUILayout.Height(24))) RenameRoomPrefab();
        Observe("RenameRoomPrefab");
        EditorGUILayout.LabelField(canRename
            ? "이름만 변경 · SO / Stage 연결과 Variant 상속은 유지됩니다."
            : "Play / Prefab Mode를 저장·종료하면 이름을 변경할 수 있어요.", EditorStyles.wordWrappedMiniLabel);
    }
    private void RenameRoomPrefab()
    {
        AssetAction(() =>
        {
            string previousPath = RoomPath;
            var category = CurrentRoom?.category ?? SectorMakerCatalog.Infer(previousPath);
            string path = SectorMakerAssets.RenameRoomPrefab(_roomPrefab, _prefabNameDraft);
            // Names are not category/role migrations. Retain a filename-inferred
            // category if the new name would otherwise move the room to another group.
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (SectorMakerCatalogSettings.instance.Category(guid, SectorMakerCatalog.Infer(path)) != category)
                SectorMakerCatalogSettings.instance.Set(guid, category);
            _roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            _prefabNameDraft = _roomPrefab.name;
            _prefabNameUi?.SelectionClicked();
            DisposeSelectionEditor(); RefreshCatalog(); SyncRoomStage();
            ShowNotification(new GUIContent("프리팹 이름 변경 완료 · 기존 연결 유지"));
        });
    }
    private void DrawPresetPicker(SectorMakerCatalog.Room room)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("이 방의 게임 설정 (Room Preset)", EditorStyles.boldLabel);
        if (room == null) return;
        var names = new[] { "설정 선택…" }.Concat(room.presets.Select(p => p.name)).ToArray();
        int index = room.presets.IndexOf(_activeSectorPreset) + 1;
        int next = EditorGUILayout.Popup("연결된 설정", index, names); Observe("PresetPicker");
        if (next != index) SetActiveSectorPreset(next == 0 ? null : room.presets[next - 1]);
        if (room.presets.Count > 1 && _activeSectorPreset == null)
            EditorGUILayout.HelpBox("이 프리팹에 연결된 설정이 여러 개예요. 랜덤 루팅을 편집하려면 위에서 하나를 고르세요. 물체 배치는 설정 선택 없이 가능해요.", MessageType.Warning);
        if (room.presets.Count == 0) EditorGUILayout.HelpBox("이 프리팹에 연결된 SO가 없습니다. 아래 ‘새 SO 추가’로 만들 수 있습니다.", MessageType.Warning);
        _newPresetType = (StageRoomType)EditorGUILayout.EnumPopup("새 SO의 게임 방 타입", _newPresetType);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("+ 새 SO 추가 · 이 프리팹에 연결…")) CreatePresetForRoom();
            Observe("CreatePreset");
            DrawPresetActions();
        }
    }
    private void DrawRoomStageLinks(SectorMakerCatalog.Room room)
    {
        EditorGUILayout.HelpBox("목록 분류는 정리용입니다. 실제 출현 종류·빈도·적은 StageDefinition에서 정해집니다.\n일반방: Normal Battle / 특수방: Raid Runtime → Rooms → 해당 Role → Sector Variants / 보스: Boss Battles", MessageType.Info);
        if (room == null || room.stages.Count == 0) { EditorGUILayout.HelpBox("이 방 설정을 직접 참조하는 스테이지가 없습니다. 아직 게임에 출현하지 않을 수 있어요.", MessageType.Warning); return; }
        foreach (var stage in room.stages)
        {
            if (GUILayout.Button(stage.name + " 설정 열기")) Selection.activeObject = stage;
            foreach (var binding in stage.RaidRuntime?.rooms ?? new List<RaidRoomPresetBinding>())
                if (binding != null && room.presets.Any(p => binding.ContainsPreset(p)))
                {
                    EditorGUILayout.LabelField("  실제 역할: " + binding.role, EditorStyles.miniLabel);
                    if (binding.content != null) EditorGUILayout.HelpBox("이 스테이지는 별도 콘텐츠 설정 '" + binding.content.name + "'을 사용합니다. Room Preset의 콘텐츠와 다를 수 있어요.", MessageType.Warning);
                }
        }
    }
    private void CreateRoomVariant()
    {
        if (_roomPrefab == null) return;
        if (IsEditingRoom && PrefabStageUtility.GetCurrentPrefabStage().scene.isDirty)
        { EditorUtility.DisplayDialog("먼저 저장해주세요", "새 Variant는 저장된 원본을 상속합니다. 현재 방을 저장한 뒤 다시 눌러주세요.", "확인"); return; }
        if (CurrentRoom?.presets.Count > 1 && !PresetMatchesRoom)
        { EditorUtility.DisplayDialog("기준 SO 선택", "복사할 기준 SO를 먼저 선택하세요. 프리팹 상속과 SO 복사를 함께 생성합니다.", "확인"); return; }
        string path = EditorUtility.SaveFilePanelInProject("① 새 방 Variant 프리팹", _roomPrefab.name + "_Room", "prefab", "기준 프리팹의 자식 Variant를 만듭니다. 스테이지에는 자동 등록하지 않습니다.", SectorMakerCatalog.Root + "Rooms");
        if (string.IsNullOrEmpty(path)) return;
        string presetPath = EditorUtility.SaveFilePanelInProject("② 새 방 Room SO", "SectorPreset_" + System.IO.Path.GetFileNameWithoutExtension(path), "asset", "기준 SO의 독립 복사본을 만듭니다. 콘텐츠 설정도 있으면 별도로 복사합니다.", "Assets/ScriptableObject/GameScene/Stage/Sector/Preset/RaidRooms");
        if (string.IsNullOrEmpty(presetPath)) return;
        try
        {
            var category = CurrentRoom?.category ?? SectorMakerCategory.Other;
            var preset = SectorMakerAssets.CreateRoom(_roomPrefab, PresetMatchesRoom ? _activeSectorPreset : null, path, presetPath, _newPresetType);
            var prefab = preset.SectorPrefab.gameObject;
            SectorMakerCatalogSettings.instance.Set(AssetDatabase.AssetPathToGUID(path), category);
            _libraryTab = 0; RefreshCatalog(); SelectRoom(prefab, false, preset);
            ShowNotification(new GUIContent("Variant + 독립 SO 생성 완료. ‘Scene에서 편집 시작’으로 여세요."));
        }
        catch (Exception e) { EditorUtility.DisplayDialog("Variant를 만들 수 없음", e.Message, "확인"); }
    }

    private void DrawPlacementWorkbench()
    {
        EditorGUILayout.LabelField("3. 배치 / 선택한 물체 수정", EditorStyles.boldLabel);
        if (!IsEditingRoom) { EditorGUILayout.HelpBox("방을 선택한 뒤 'Scene에서 편집 시작'을 누르면 도구가 열립니다.", MessageType.Info); return; }
        int nextTab = GUILayout.Toolbar(_workbenchTab, new[] { "새로 배치", "선택한 물체", "바닥 / 벽 외형" });
        if (nextTab != _workbenchTab) { _workbenchTab = nextTab; _paintingEnabled = false; }
        if (_workbenchTab == 1) { DrawSelectionWorkbench(); return; }
        if (_workbenchTab == 2) { DrawAppearanceWorkbench(); return; }
        DrawLootPlacementToolbar();
        EditorGUILayout.Space(5);
        switch (_mode)
        {
            case PlacementMode.FixedSectorStructure:
                EditorGUILayout.HelpBox("항상 있는 벽·장애물·장식 구조물을 배치합니다. 파밍 상자/가방은 '루팅 오브젝트'에서 자리를 배치하세요.\n기존 고정 상자는 그대로 보존합니다.", MessageType.None);
                _structurePrefab = (GameObject)EditorGUILayout.ObjectField("구조물 프리팹", _structurePrefab, typeof(GameObject), false);
                DrawStructureVisionPlacement();
                _structureBottomOffset = EditorGUILayout.FloatField("바닥 위 높이", _structureBottomOffset);
                break;
            case PlacementMode.EnemySpawnPoint:
                EditorGUILayout.HelpBox("적이 등장할 위치와 방향을 찍습니다. 실제 적 종류·웨이브는 스테이지 Encounter에서 정합니다.", MessageType.None);
                _showExistingEnemySpawnPoints = EditorGUILayout.ToggleLeft("기존 적 위치 표시", _showExistingEnemySpawnPoints);
                _enemySpawnPointYOffset = EditorGUILayout.FloatField("바닥 위 높이", _enemySpawnPointYOffset);
                break;
            case PlacementMode.ContentSlotAnchor:
                EditorGUILayout.HelpBox(_contentSlotKind == SectorContentSlotKind.WorldItem ? "게임 시작부터 바닥/선반에 놓이는 아이템 자리입니다." : "상호작용해서 여는 루팅 오브젝트의 등장 자리입니다. 상자 내부 내용은 Loot Maker에서 정합니다.", MessageType.None);
                _contentSlotId = EditorGUILayout.TextField("위치 이름", _contentSlotId);
                _contentSlotKind = (SectorContentSlotKind)EditorGUILayout.EnumPopup("위치 용도", _contentSlotKind);
                _showExistingContentSlots = EditorGUILayout.ToggleLeft("기존 루팅 위치 표시", _showExistingContentSlots);
                DrawContentPlanSetup();
                using (new EditorGUI.DisabledScope(!CanEditContentPlan())) DrawLootPlacementDraft();
                DrawLootSpawnLimits();
                break;
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            _rotationY = EditorGUILayout.FloatField("방향 Y", _rotationY);
            if (GUILayout.Button("+90°", GUILayout.Width(56))) _rotationY += 90;
        }
        bool canStart = _mode != PlacementMode.FixedSectorStructure || _structurePrefab != null && PrefabUtility.IsPartOfPrefabAsset(_structurePrefab);
        using (new EditorGUI.DisabledScope(!canStart && !_paintingEnabled))
        {
            Color old = GUI.backgroundColor; GUI.backgroundColor = _paintingEnabled ? new Color(1, .65f, .4f) : new Color(.5f, .9f, .7f);
            if (GUILayout.Button(_paintingEnabled ? "■ 배치 종료 (Esc)" : "▶ 배치 시작 — Scene 바닥 클릭", GUILayout.Height(36)))
            {
                if (_paintingEnabled) _paintingEnabled = false;
                else BeginGuidedPlacement();
            }
            Observe("StartPlacement"); GUI.backgroundColor = old;
        }
        EditorGUILayout.HelpBox(_paintingEnabled ? "Scene 바닥 클릭: 배치   Q/E: 회전   Esc: 종료\nCtrl+Z: 되돌리기 / Alt+드래그: 카메라 이동" : "배치가 꺼져 있습니다. Scene에서 물체를 선택하면 이동·회전할 수 있어요.", MessageType.None);
        if (_paintingEnabled && !TryValidateConfiguration(out string reason)) EditorGUILayout.HelpBox(reason, MessageType.Warning);
        _advancedPlacement = EditorGUILayout.Foldout(_advancedPlacement, "고급: 바닥 레이어 / 연결 / 배치 설정", true);
        if (_advancedPlacement)
        {
            _floorSurfaceMask = LayerMaskField(new GUIContent("클릭할 바닥 레이어"), _floorSurfaceMask);
            DrawRotationSettings();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("자동 연결: 방 루트", _activeSectorRuntime, typeof(SectorRuntime), true);
                EditorGUILayout.ObjectField("자동 연결: 물체 부모", _sectorObjectParent, typeof(Transform), true);
                EditorGUILayout.ObjectField("자동 연결: 적 위치 부모", _enemySpawnPointParent, typeof(Transform), true);
                EditorGUILayout.ObjectField("자동 연결: 루팅 위치 부모", _contentSlotParent, typeof(Transform), true);
            }
            _contentSlotYOffset = EditorGUILayout.FloatField("루팅 위치 바닥 위 높이", _contentSlotYOffset);
        }
    }
    private void BeginGuidedPlacement()
    {
        SyncRoomStage(); if (!IsEditingRoom) return;
        if (_mode == PlacementMode.FixedSectorStructure && _structurePrefab == null) return;
        if (_mode == PlacementMode.ContentSlotAnchor && !CanEditContentPlan())
        { ShowNotification(new GUIContent("이 방의 독립 콘텐츠 설정을 먼저 만들어주세요.")); return; }
        int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Sector Maker 배치 준비");
        if (_mode == PlacementMode.FixedSectorStructure && _sectorObjectParent == null)
        {
            var parent = new GameObject("SectorObjectContainer");
            Undo.RegisterCreatedObjectUndo(parent, "물체 배치 부모 만들기");
            parent.transform.SetParent(_activeSectorRuntime.transform, false); _sectorObjectParent = parent.transform;
        }
        if (_mode == PlacementMode.EnemySpawnPoint && _enemySpawnPointParent == null) CreateEnemySpawnPointParent();
        if (_mode == PlacementMode.ContentSlotAnchor && _contentSlotParent == null) CreateContentSlotParent();
        if (_mode == PlacementMode.ContentSlotAnchor) _contentSlotId = NextSlotId(_contentSlotParent, _contentSlotId);
        Undo.CollapseUndoOperations(group);
        _paintingEnabled = TryValidateConfiguration(out string reason);
        if (!_paintingEnabled) ShowNotification(new GUIContent(reason));
        SceneView.RepaintAll();
    }
    internal static string NextSlotId(Transform parent, string candidate)
    {
        candidate = string.IsNullOrWhiteSpace(candidate) ? "loot_01" : candidate.Trim();
        if (!HasContentSlotAnchorWithId(parent, candidate)) return candidate;
        string stem = candidate;
        int underscore = stem.LastIndexOf('_');
        if (underscore >= 0 && int.TryParse(stem.Substring(underscore + 1), out _)) stem = stem.Substring(0, underscore);
        for (int n = 1; ; n++) { string id = stem + "_" + n.ToString("00"); if (!HasContentSlotAnchorWithId(parent, id)) return id; }
    }
    private int ProfileUsers => _contentProfile == null ? 0 : SectorMakerCatalog.Presets.Count(p => p != null && p.ContentProfile == _contentProfile);
    private bool HasStageProfileOverride => _contentProfile != null && SectorMakerCatalog.Stages.Any(s => s != null && s.RaidRuntime?.rooms != null && s.RaidRuntime.rooms.Any(r => r != null && r.content == _contentProfile));
    private bool CanEditContentPlan() => PresetMatchesRoom && _contentProfile != null && _activeSectorPreset.ContentProfile == _contentProfile && ProfileUsers <= 1 && !HasStageProfileOverride;
    private void DrawContentPlanSetup()
    {
        using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("이 방 콘텐츠 설정", _contentProfile, typeof(SectorContentProfileSO), false);
        if (_activeSectorPreset == null) { EditorGUILayout.HelpBox("가운데 '이 방의 게임 설정'에서 Room Preset을 선택하거나 만들어주세요.", MessageType.Warning); return; }
        if (!CanEditContentPlan())
        {
            EditorGUILayout.HelpBox(_contentProfile == null ? "랜덤 위치에 무엇이 나올지 저장할 콘텐츠 설정이 필요합니다." : "다른 방과 콘텐츠를 공유하고 있습니다. 독립 복사본을 만들어 편집하세요.", MessageType.Warning);
            if (GUILayout.Button(_contentProfile == null ? "이 방 전용 콘텐츠 만들기…" : "이 방 전용으로 복사…"))
            {
                string path = EditorUtility.SaveFilePanelInProject("방 전용 콘텐츠", "Content_" + _roomPrefab.name, "asset", "새 콘텐츠 설정을 만듭니다. 이전 설정은 수정하지 않습니다.");
                if (string.IsNullOrEmpty(path)) return;
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) { ShowNotification(new GUIContent("새 파일 경로를 선택해주세요.")); return; }
                var profile = _contentProfile != null ? Instantiate(_contentProfile) : CreateInstance<SectorContentProfileSO>();
                AssetDatabase.CreateAsset(profile, path);
                _contentProfile = profile; ApplyContentProfileToPreset();
            }
        }
    }
    private void DrawSelectionWorkbench()
    {
        var selected = Selection.activeGameObject;
        SceneSelectionUi.Synchronize(selected);
        if (selected == null || _activeSectorRuntime == null || !selected.transform.IsChildOf(_activeSectorRuntime.transform))
        { EditorGUILayout.HelpBox("Scene 또는 Hierarchy에서 이 방 안의 물체를 선택하세요.\n이동 W / 회전 E / 크기 R — 배치 모드가 꺼진 상태에서 사용하세요.", MessageType.Info); return; }
        EditorGUILayout.LabelField(selected.name, EditorStyles.boldLabel);
        MakerRegistrationUi.PrefabSummary(selected);
        if (_selectionEditorTarget != selected.transform)
        { DisposeSelectionEditor(); _selectionEditorTarget = selected.transform; _selectionEditor = Editor.CreateEditor(selected.transform); }
        _selectionEditor?.OnInspectorGUI();
        DrawPlacedStructureVision();
        if (GUILayout.Button("Scene에서 선택 물체 보기")) { SceneView.lastActiveSceneView?.FrameSelected(); SceneView.lastActiveSceneView?.Focus(); }
        var anchor = selected.GetComponent<SectorContentSlotAnchor>();
        if (anchor != null)
        {
            DrawContentPlanSetup();
            using (new EditorGUI.DisabledScope(!CanEditContentPlan())) DrawAnchorDefinition(anchor);
        }
        _showSelectedLinks = EditorGUILayout.Foldout(_showSelectedLinks, "고급: 선택한 물체의 데이터 연결", true);
        if (_showSelectedLinks) DrawSelectedDataLinkContext();
    }
    [SerializeField] private bool _showSelectedLinks;
    private void DrawAnchorDefinition(SectorContentSlotAnchor anchor)
    {
        if (_contentProfile == null) return;
        if (!_contentProfile.TryGetSlotDefinition(anchor.SlotId, out var definition))
        {
            if (GUILayout.Button("이 위치의 출현 설정 추가"))
            { Undo.RecordObject(_contentProfile, "콘텐츠 위치 설정 추가"); _contentProfile.GetOrCreateSlotDefinition(anchor.SlotId, anchor.ContentKind); EditorUtility.SetDirty(_contentProfile); }
            return;
        }
        EditorGUILayout.LabelField("위치: " + anchor.SlotId, EditorStyles.boldLabel);
        var data = new SerializedObject(_contentProfile);
        var rows = data.FindProperty("_slotDefinitions");
        int index = _contentProfile.SlotDefinitions.ToList().IndexOf(definition);
        if (index < 0) return;
        var row = rows.GetArrayElementAtIndex(index);
        EditorGUILayout.PropertyField(row.FindPropertyRelative("enabled"), new GUIContent("이 위치 사용"));
        EditorGUILayout.PropertyField(row.FindPropertyRelative("alwaysSpawn"), new GUIContent("항상 생성 · 랜덤 상한 제외"));
        using (new EditorGUI.DisabledScope(row.FindPropertyRelative("alwaysSpawn").boolValue))
            EditorGUILayout.PropertyField(row.FindPropertyRelative("spawnChancePercent"), new GUIContent("자리 등장 확률 (%)"));
        bool worldItem = anchor.ContentKind == SectorContentSlotKind.WorldItem;
        var tableProperty = row.FindPropertyRelative(worldItem ? "worldItemTable" : "lootObjectTable");
        EditorGUILayout.PropertyField(tableProperty, new GUIContent(worldItem ? "아이템 테이블" : "루팅 오브젝트 테이블"));
        if (!worldItem && tableProperty.objectReferenceValue == null)
            EditorGUILayout.PropertyField(row.FindPropertyRelative("prefabCandidates"), new GUIContent("기존 프리팹 후보 / 가중치 (호환)"), true);
        if (data.ApplyModifiedProperties())
        {
            _contentProfile.TryGetSlotDefinition(anchor.SlotId, out definition);
            if (definition.lootObjectTable != null || definition.worldItemTable != null)
            {
                var profileData = new SerializedObject(_contentProfile);
                profileData.FindProperty("_useAuthoredLootPlacement").boolValue = true;
                profileData.ApplyModifiedProperties();
            }
            SectorSpawnCatalogSynchronizer.Queue();
        }
        _contentProfile.TryGetSlotDefinition(anchor.SlotId, out definition);
        float chance = definition.alwaysSpawn ? 100 : Mathf.Clamp(definition.spawnChancePercent, 0, 100);
        DrawSpawnTableTools(worldItem ? (ScriptableObject)definition.worldItemTable : definition.lootObjectTable,
            worldItem, table =>
            {
                Undo.RecordObject(_contentProfile, "자리 스폰 테이블 지정");
                if (worldItem) definition.worldItemTable = (SectorWorldItemSpawnTableSO)table;
                else definition.lootObjectTable = (SectorLootObjectSpawnTableSO)table;
                var profileData = new SerializedObject(_contentProfile);
                profileData.FindProperty("_useAuthoredLootPlacement").boolValue = true;
                profileData.ApplyModifiedProperties();
                EditorUtility.SetDirty(_contentProfile); SectorSpawnCatalogSynchronizer.Queue();
            }, chance);
        EditorGUILayout.HelpBox(worldItem ? "이 자리에서 한 종류/한 스택을 선택합니다. 생성 수는 방 상한 설정에서 제한합니다." :
            "여기 확률은 상자 자체의 등장 확률입니다. 상자 안 내용은 해당 LootObject의 기존 Loot Maker 설정을 유지합니다.", MessageType.None);
    }
    private void DisposeSelectionEditor()
    {
        if (_selectionEditor != null) DestroyImmediate(_selectionEditor);
        _selectionEditor = null; _selectionEditorTarget = null;
    }
}
