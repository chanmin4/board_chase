using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Edits real Stage SOs; previews are data-only and never instantiate sectors.</summary>
public sealed partial class StageMakerWindow : EditorWindow
{
    [SerializeField] private StageDefinitionSO _selected;
    [SerializeField] private string _search = "";
    [SerializeField] private bool _sortByName;
    [SerializeField] private float _listWidth = 205, _settingsShare = .58f;
    [SerializeField] private bool _showLibrary = true, _showPreview = true;
    [SerializeField] private bool _generationOpen = true, _mapsOpen = true, _gameplayOpen;
    [SerializeField] private bool _testingOpen, _rawOpen;
    private readonly List<StageDefinitionSO> _stages = new();
    private SerializedObject _serialized;
    private Vector2 _listScroll, _settingsScroll;
    private string _rename = "";
    private bool _advanced, _identityOpen, _needsRefresh;
    private readonly HashSet<string> _poolAdvanced = new();
    private static bool CanWrite => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

    [MenuItem("Tools/Authoring/Stage Maker")]
    public static void Open()
    {
        var window = GetWindow<StageMakerWindow>("Stage Maker");
        window.minSize = new Vector2(940, 600);
        window.Show();
    }

    public static void OpenAsset(StageDefinitionSO stage)
    {
        Open(); GetWindow<StageMakerWindow>().Select(stage);
    }

    private void OnEnable()
    {
        bool globalImages = _showGlobalMapImages;
        minSize = new Vector2(940, 600);
        EditorApplication.projectChanged += ProjectChanged;
        Undo.undoRedoPerformed += OnUndo;
        EditorApplication.update += TickBatch;
        RefreshLibrary();
        if (_selected != null) Select(_selected);
        _showGlobalMapImages = globalImages;
    }
    private void OnDisable()
    {
        EditorApplication.projectChanged -= ProjectChanged;
        Undo.undoRedoPerformed -= OnUndo;
        EditorApplication.update -= TickBatch;
        _batchRunning = false;
    }
    private void ProjectChanged() { _needsRefresh = true; _previewStale = true; Repaint(); }
    private void OnUndo()
    {
        _serialized = _selected != null ? new SerializedObject(_selected) : null;
        if (_showGlobalMapImages) Repaint(); else Changed();
    }
    private void Changed()
    {
        _previewStale = true;
        if (_batchRunning) _batchMessage += "\n설정/테스트 옵션이 변경되어 검증을 중단했습니다.";
        _batchRunning = false; Repaint();
    }
    private void Select(StageDefinitionSO stage)
    {
        _showGlobalMapImages = false;
        _selected = stage; _serialized = stage != null ? new SerializedObject(stage) : null;
        _rename = stage != null ? stage.name : "";
        _preview = null; _selectedCell = null; _generationError = null; _batchMessage = "";
        _batchErrors.Clear(); _batchCategories.Clear();
        _hasPreviewAttempt = false;
        _settingsScroll = Vector2.zero; Changed();
    }
    private void RefreshLibrary()
    {
        _needsRefresh = false;
        _stages.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:StageDefinitionSO"))
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (stage != null) _stages.Add(stage);
        }
        _stages.Sort((a, b) => _sortByName
            ? string.Compare(a.name, b.name, StringComparison.CurrentCulture)
            : a.StageIndex != b.StageIndex ? a.StageIndex.CompareTo(b.StageIndex) : string.CompareOrdinal(a.name, b.name));
        if (_selected == null && _stages.Count > 0 && !_showGlobalMapImages) Select(_stages[0]);
    }

    private void OnGUI()
    {
        if (_needsRefresh) RefreshLibrary();
        DrawToolbar();
        bool drawLibrary = _showLibrary, drawPreview = _showPreview;
        var layout = StageMakerWorkspaceLayout.Calculate(position.size, drawLibrary, drawPreview, _listWidth, _settingsShare);
        if (drawLibrary)
        {
            Splitter(new Rect(layout.library.xMax, 24, 5, layout.library.height), true);
            GUILayout.BeginArea(layout.library); DrawLibrary(); GUILayout.EndArea();
        }
        if (drawPreview) Splitter(new Rect(layout.settings.xMax, 24, 5, layout.settings.height), false);
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = Mathf.Clamp(layout.settings.width * .40f, 175, 290);
        GUILayout.BeginArea(layout.settings);
        if (_showGlobalMapImages) DrawGlobalMapImages(layout.settings.width); else DrawSettings();
        GUILayout.EndArea();
        if (drawPreview)
        {
            EditorGUIUtility.labelWidth = 130;
            GUILayout.BeginArea(layout.preview); DrawPreviewPanel(layout.preview.size); GUILayout.EndArea();
        }
        EditorGUIUtility.labelWidth = oldLabelWidth;
    }
    private void Splitter(Rect rect, bool left)
    {
        EditorGUI.DrawRect(rect, new Color(.12f, .15f, .18f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        int id = GUIUtility.GetControlID(FocusType.Passive);
        var evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
        { GUIUtility.hotControl = id; evt.Use(); }
        if (GUIUtility.hotControl != id) return;
        if (evt.type == EventType.MouseDrag)
        {
            if (left) _listWidth = Mathf.Clamp(_listWidth + evt.delta.x, 175, 280);
            else
            {
                float available = position.width - (_showLibrary ? Mathf.Clamp(_listWidth, 175, 280) + 5 : 0) - 5;
                _settingsShare = Mathf.Clamp(_settingsShare + evt.delta.x / Mathf.Max(1, available), .25f, .8f);
            }
            evt.Use(); Repaint();
        }
        if (evt.type == EventType.MouseUp) { GUIUtility.hotControl = 0; evt.Use(); }
    }
    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("STAGE MAKER", EditorStyles.boldLabel, GUILayout.Width(100));
            using (new EditorGUI.DisabledScope(!CanWrite))
            {
                if (GUILayout.Button("새 Stage", EditorStyles.toolbarButton, GUILayout.Width(75))) CreateStage();
                using (new EditorGUI.DisabledScope(_selected == null || _showGlobalMapImages))
                {
                    if (GUILayout.Button("복사", EditorStyles.toolbarButton, GUILayout.Width(45))) DuplicateStage();
                    if (GUILayout.Button("휴지통", EditorStyles.toolbarButton, GUILayout.Width(55))) TrashStage();
                }
                using (new EditorGUI.DisabledScope(_selected == null && !_showGlobalMapImages))
                    if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(45)))
                    { if (!_showGlobalMapImages && _selected != null) AssetDatabase.SaveAssetIfDirty(_selected); SaveMapImageAssets(); }
            }
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70))) RefreshLibrary();
            Object inspectorTarget = _showGlobalMapImages ? (Object)_sharedMapImages : _selected;
            if (GUILayout.Button("Inspector", EditorStyles.toolbarButton, GUILayout.Width(65)) && inspectorTarget != null)
            { Selection.activeObject = inspectorTarget; EditorGUIUtility.PingObject(inspectorTarget); }
            GUILayout.FlexibleSpace();
            _showLibrary = GUILayout.Toggle(_showLibrary, "목록", EditorStyles.toolbarButton, GUILayout.Width(42));
            if (GUILayout.Button("설정 넓게", EditorStyles.toolbarButton, GUILayout.Width(72)))
            { _showPreview = true; _settingsShare = .68f; _fitPending = true; }
            if (GUILayout.Button("지도 넓게", EditorStyles.toolbarButton, GUILayout.Width(72)))
            { _showPreview = true; _settingsShare = .38f; _fitPending = true; }
            _showPreview = GUILayout.Toggle(_showPreview, "지도 표시", EditorStyles.toolbarButton, GUILayout.Width(68));
            if (!CanWrite) GUILayout.Label("읽기 전용", EditorStyles.miniLabel, GUILayout.Width(65));
        }
    }
    private void DrawLibrary()
    {
        GUILayout.Label("공통 설정", EditorStyles.boldLabel);
        Color menuColor = GUI.backgroundColor;
        if (_showGlobalMapImages) GUI.backgroundColor = new Color(.35f, .8f, 1);
        if (GUILayout.Button("지도 Sprite · 전체 공통", GUILayout.Height(34)))
        { _showGlobalMapImages = true; EnsureMapImageSettings(); Repaint(); }
        GUI.backgroundColor = menuColor;
        GUILayout.Space(7);
        GUILayout.Label("스테이지 목록", EditorStyles.boldLabel);
        _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
        bool sort = EditorGUILayout.ToggleLeft("이름순 (OFF: 스테이지 번호순)", _sortByName);
        if (sort != _sortByName) { _sortByName = sort; RefreshLibrary(); }
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        foreach (var stage in _stages)
        {
            if (stage == null || (!string.IsNullOrEmpty(_search) &&
                (stage.name + stage.DisplayName + stage.StageIndex).IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)) continue;
            Color old = GUI.backgroundColor;
            if (stage == _selected && !_showGlobalMapImages) GUI.backgroundColor = new Color(.35f, .8f, 1);
            if (GUILayout.Button(new GUIContent($"{stage.StageIndex:00}  {stage.name}" + (stage.CanEnter ? "" : " [WIP]"),
                AssetDatabase.GetAssetPath(stage)), EditorStyles.miniButton, GUILayout.Height(29))) Select(stage);
            GUI.backgroundColor = old;
        }
        EditorGUILayout.EndScrollView();
        if (_showGlobalMapImages)
        { EditorGUILayout.HelpBox("지도 Sprite는 스테이지와 독립된 공통 설정입니다. 오른쪽 지도는 선택한 Stage의 테스트 미리보기입니다.", MessageType.Info); return; }
        if (_selected == null) return;
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(_selected), EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(!CanWrite))
        {
            _rename = EditorGUILayout.TextField("이름", _rename);
            if (GUILayout.Button("에셋 이름 변경") && !string.IsNullOrWhiteSpace(_rename))
            {
                string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(_selected), _rename.Trim());
                if (!string.IsNullOrEmpty(error)) ShowNotification(new GUIContent(error)); else RefreshLibrary();
            }
        }
        EditorGUILayout.HelpBox("새/복사 Stage는 WIP로 만듭니다. 입장 가능 여부와 실제 진행 매니저의 Stage 목록은 별도입니다.", MessageType.Info);
    }
    private int NextStageIndex() => _stages.Count == 0 ? 1 : _stages.Max(s => s.StageIndex) + 1;
    private void CreateStage()
    {
        string path = EditorUtility.SaveFilePanelInProject("새 Stage SO", "NewStageDefinition", "asset", "실제 Stage SO를 생성합니다.");
        if (string.IsNullOrEmpty(path)) return;
        var stage = CreateInstance<StageDefinitionSO>();
        var data = new SerializedObject(stage);
        data.FindProperty("_stageIndex").intValue = NextStageIndex();
        data.FindProperty("_developmentOnly").boolValue = true;
        data.FindProperty("_mapPolicy").intValue = (int)StageMapPolicy.RadialBraidedRaid;
        data.FindProperty("_roomGridSize").intValue = 10;
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(stage, path); Undo.RegisterCreatedObjectUndo(stage, "Create Stage");
        RefreshLibrary(); Select(stage);
    }
    private void DuplicateStage()
    {
        string path = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(_selected));
        if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(_selected), path)) return;
        var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(path);
        var data = new SerializedObject(stage);
        data.FindProperty("_stageIndex").intValue = NextStageIndex();
        data.FindProperty("_developmentOnly").boolValue = true;
        data.ApplyModifiedProperties(); EditorUtility.SetDirty(stage);
        RefreshLibrary(); Select(stage);
    }
    private void TrashStage()
    {
        string path = AssetDatabase.GetAssetPath(_selected);
        if (!EditorUtility.DisplayDialog("Stage를 휴지통으로 이동", path +
            "\n진행 매니저/다른 에셋 참조가 끊길 수 있습니다. OS 휴지통에서 복구할 수 있습니다.", "휴지통으로 이동", "취소")) return;
        if (AssetDatabase.MoveAssetToTrash(path)) { Select(null); RefreshLibrary(); }
    }

    private void DrawSettings()
    {
        if (_selected == null || _serialized == null) { EditorGUILayout.HelpBox("Stage SO를 선택하세요.", MessageType.Info); return; }
        _serialized.Update();
        GUILayout.Label($"{_selected.DisplayName} · 스테이지 설정", EditorStyles.boldLabel);
        GUILayout.Label("원본 SO 직접 편집 · 설정은 아래 한 화면에 모았습니다.", EditorStyles.wordWrappedMiniLabel);
        _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll);
        EditorGUI.BeginChangeCheck();
        DrawGenerationFeedback();
        if (BeginSection(ref _generationOpen, "1. 맵 크기 · 생성 규칙", "격자 크기, 방 밀도, 연결, 발전실·탈출구"))
        {
            using (new EditorGUI.DisabledScope(!CanWrite)) DrawGeneration();
        }
        EndSection();
        if (BeginSection(ref _mapsOpen, "2. 등장할 맵 · 특별방 · 상인", "일반 / 전용 / 특별맵 / 상인 확률을 한 곳에서 설정"))
        {
            using (new EditorGUI.DisabledScope(!CanWrite))
            {
                DrawComposition();
                Heading("특별맵 종류와 등장 수량"); DrawSpecial();
                Heading("상인 · 공용 전투맵에 추가 등장");
                P("_raidMap.merchantSpawnPercent", "스테이지 상인 등장 확률 (%)", "방마다 10%가 아닌 스테이지 전체 1회 추첨입니다. 성공 시 공용 전투방 한 곳에 1명 추가합니다. 0은 OFF, 100은 배치 대상이 있을 때 확정.");
                GUILayout.Label("일반/전용/특별 비율과 별도입니다. 전용맵·특별맵·보스·발전실·해치·탈출구에는 배치하지 않습니다. 방의 적/클리어 조건은 유지합니다.", EditorStyles.wordWrappedMiniLabel);
                P("_raidRuntime.merchantConfig", "상인 상점 설정");
                P("_raidRuntime.merchantInitialMoney", "상인 초기 자금");
                P("_raidRuntime.merchantOpenChannel", "상점 열기 채널");
                P("_raidRuntime.merchantOverlayChannel", "상점 UI 채널");
                if (Prop("_raidMap.merchantSpawnPercent").intValue > 0 && Prop("_raidRuntime.merchantConfig").objectReferenceValue == null)
                    EditorGUILayout.HelpBox("상인 확률이 있지만 상점 설정이 없습니다. 상인 상점 설정에 ShopConfig SO를 등록하세요.", MessageType.Warning);
            }
        }
        EndSection();
        if (BeginSection(ref _gameplayOpen, "3. 전투 · 진행 · 환경", "발전실 / 해치 / 보스 / 전투 / 보급 / 가스"))
        {
            using (new EditorGUI.DisabledScope(!CanWrite))
            { Heading("전투와 필수방"); DrawCombat(); Heading("진행과 환경"); DrawEnvironment(); }
        }
        EndSection();
        // Test controls do not edit the Stage SO and remain usable in read-only mode.
        if (BeginSection(ref _testingOpen, "테스트 옵션 · 시드 검증", "시드, 필수 특별맵 시뮬레이션, 반복 검사")) DrawPreviewOptions();
        EndSection();
        if (BeginSection(ref _rawOpen, "고급 · 전체 원본 필드", "Legacy / WIP를 포함한 원본 Inspector"))
        {
            using (new EditorGUI.DisabledScope(!CanWrite)) DrawRaw();
        }
        EndSection();
        EditorGUI.EndChangeCheck();
        bool changed = false;
        if (CanWrite && _serialized.ApplyModifiedProperties()) { EditorUtility.SetDirty(_selected); changed = true; }
        if (changed) Changed();
        if (_selected.MapPolicy != StageMapPolicy.RadialBraidedRaid)
            EditorGUILayout.HelpBox("밀도/링/후보 배치는 V3 전용입니다. 현재는 선택한 구버전 생성기의 규칙을 사용합니다.", MessageType.Info);
        if (!_selected.RoomComposition.TryValidate(out string ratioError, _selected.SpecialSectors.enabled && _selected.SpecialSectors.useRatio))
            EditorGUILayout.HelpBox(ratioError, MessageType.Error);
        EditorGUILayout.EndScrollView();
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
        {
            if (GUILayout.Button("현재 시드로 생성", EditorStyles.toolbarButton)) { _showPreview = true; GeneratePreview(); }
            if (GUILayout.Button("랜덤 생성", EditorStyles.toolbarButton)) { _showPreview = true; RandomPreview(); }
            if (GUILayout.Button(_batchRunning ? "검증 중단" : $"{_batchCount}회 검증", EditorStyles.toolbarButton))
            { _showPreview = true; if (_batchRunning) _batchRunning = false; else StartBatch(); }
        }
    }
    private static bool BeginSection(ref bool open, string title, string description)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        open = EditorGUILayout.Foldout(open, title, true, EditorStyles.foldoutHeader);
        if (open) GUILayout.Label(description, EditorStyles.wordWrappedMiniLabel);
        return open;
    }
    private static void EndSection() => EditorGUILayout.EndVertical();
    private SerializedProperty Prop(string path) => _serialized.FindProperty(path);
    private void P(string path, string label, string tooltip = "")
    {
        var property = Prop(path);
        if (property == null) return;
        // Primitive controls intentionally omit the runtime SO's English Header decorators.
        var content = new GUIContent(label, tooltip);
        GUI.SetNextControlName(path);
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer: property.intValue = EditorGUILayout.IntField(content, property.intValue); break;
            case SerializedPropertyType.Float: property.floatValue = EditorGUILayout.FloatField(content, property.floatValue); break;
            case SerializedPropertyType.Boolean: property.boolValue = EditorGUILayout.Toggle(content, property.boolValue); break;
            case SerializedPropertyType.String: property.stringValue = EditorGUILayout.TextField(content, property.stringValue); break;
            case SerializedPropertyType.ObjectReference: EditorGUILayout.ObjectField(property, content); break;
            default: EditorGUILayout.PropertyField(property, content, true); break;
        }
    }
    private static void Heading(string label) { EditorGUILayout.Space(7); GUILayout.Label(label, EditorStyles.boldLabel); }
    private void DrawGeneration()
    {
        P("_roomGridSize", "맵 가로·세로 (1~64칸)", "1~7은 소형 테스트, 8 이상은 기존 V3 생성입니다. 1이면 한 방과 HOME 탈출구를 만듭니다.");
        P("_raidMap.roomDensity", "방 밀도 (0.15~0.65)", "격자 중 방을 만드는 목표 비중입니다. 0.32 = 약 32%.");
        int grid = Prop("_roomGridSize").intValue;
        bool compact = grid >= 1 && grid < 8;
        if (Prop("_mapPolicy").intValue == (int)StageMapPolicy.RadialBraidedRaid && grid >= 1 && grid <= 64)
        {
            float density = Prop("_raidMap.roomDensity").floatValue;
            if (!float.IsNaN(density) && !float.IsInfinity(density))
                GUILayout.Label($"{grid}×{grid} 격자 · 밀도 기준 목표 약 {Mathf.Max(grid == 1 ? 1 : compact ? 4 : 1, Mathf.RoundToInt(grid * grid * density))}방 (필수 구조·고급 방 수 설정에 따라 달라짐)", EditorStyles.wordWrappedMiniLabel);
            if (compact) EditorGUILayout.HelpBox(grid == 1
                ? "1×1 소형 테스트: 일반방 1개 + HOME 탈출구. 발전실·보스·해치는 배치하지 않습니다."
                : "소형 테스트: 링 없이 연결된 방을 최소 4개 만듭니다. 발전실·탈출구는 가지 끝에 배치하고 탈출구 수만 공간에 맞춰 축소합니다. 별도 추가 금고 추첨은 생략하고 특별맵 풀은 그대로 적용합니다.", MessageType.Info);
        }
        P("_raidMap.braidPercent", "인접 방 추가 연결 (%)", "0~100. 높을수록 막다른 길 대신 순환 경로가 많아집니다.");
        Heading("탈출 · 발전실 · 보스");
        P("_raidMap.extractionCount", "외곽 탈출구 수 (1~6)", "발전실과 별도로 외곽 가지 끝에 배치합니다. 소형 테스트는 공간에 맞춰 축소합니다.");
        P("_raidMap.bossSpawnPercent", "보스 출현 확률 (%)");
        P("_raidMap.preventBossBlockingExtraction", "보스가 탈출 경로 막지 않기", "ON: 보스방을 제외해도 시작점에서 모든 외곽 탈출구에 도달 가능한 위치에만 배치합니다. 새 맵 생성부터 적용됩니다.");
        GUILayout.Label("발전실·탈출구만 외곽 우선의 막다른 방에 배치합니다. 나머지는 거리 제한 없이 랜덤, 보스는 스테이지 전용맵에서 추첨합니다.", EditorStyles.wordWrappedMiniLabel);
        _identityOpen = EditorGUILayout.Foldout(_identityOpen, "스테이지 정보 · 외형 · 생성 방식", true);
        if (_identityOpen)
        {
            P("_stageIndex", "스테이지 번호"); P("_displayName", "표시 이름"); P("_developmentOnly", "WIP · 입장 차단");
            EditorGUI.BeginChangeCheck();
            int policy = EditorGUILayout.Popup("생성 방식", Prop("_mapPolicy").intValue,
                new[] { "기존 전체 격자 / 작성 토폴로지", "구버전 정사각 레이드", "V3 · 중심 해치 / 순환형 랜덤" });
            if (EditorGUI.EndChangeCheck()) Prop("_mapPolicy").intValue = policy;
            P("_useStartSectorOnly", "시작 섹터만 사용"); P("_sectorTheme", "스테이지 외형 테마");
            P("_startSectorPresets", "Legacy 시작방 풀");
        }
        if (_stages.Any(s => s != _selected && s.StageIndex == _selected.StageIndex))
            EditorGUILayout.HelpBox("스테이지 번호가 다른 SO와 중복됩니다.", MessageType.Warning);
        _advanced = EditorGUILayout.Foldout(_advanced, "고급 · 링 / 목표 방 수", true);
        if (!_advanced) return;
        P("_raidMap.customizeTopology", "고급 생성 규칙 Override");
        using (new EditorGUI.DisabledScope(!Prop("_raidMap.customizeTopology").boolValue))
        {
            P("_raidMap.topology.targetRoomCount", "목표 방 수 (0: 밀도)");
            P("_raidMap.topology.minimumRoomCount", "최소 방 수"); P("_raidMap.topology.maximumRoomCount", "최대 방 수 (0: 제한 없음)");
            using (new EditorGUI.DisabledScope(compact))
            { P("_raidMap.topology.ringRadius", "중심 링 반경 (0: 비율)"); P("_raidMap.topology.ringRadiusRatio", "격자 대비 링 반경"); }
        }
        P("_branchTopologies", "Legacy · 직접 작성한 연결 구조");
    }
    private void DrawComposition() => DrawCompositionControls();
    private void DrawSpecial() => DrawSpecialControls();
    private static string SpecialPoolLabel(string id) => id switch
    {
        "office" => "사무실", "storage" or "warehouse" => "창고", "security" or "armory" => "보안실",
        "medical" => "응급의료실", "vault" => "잠금보관실", "server" => "서버실", _ => id
    };
    private void DrawCombat()
    {
        Heading("이 스테이지의 적 추가 · 교체 · 감염방");
        P("_encounterRules.infectedRoomCount", "감염방 수 (자리 부족 시 가능한 만큼)");
        P("_encounterRules.infectionSpreadEnabled", "감염방 확산 사용");
        P("_encounterRules.infectionSpreadIntervalSeconds", "전체 감염방 공용 확산 간격 (초)");
        P("_encounterRules.maximumNewInfectedRoomsPerTick", "회당 새 감염방 상한 (0: 감염방마다 1회 시도)");
        P("_encounterRules.infectedRoomGasPressure", "감염방 가스 강도 · 방호 장비와 비교");
        P("_encounterRules.infectedEnemyChance", "감염방 내 감염 적 확률 (0~1)");
        P("_encounterRules.extraEnemyChance", "일반방 추가 적 1명 확률 (0~1)");
        P("_encounterRules.replacementEnemy", "일반 적 대체 후보 (없음: 사용 안 함)");
        P("_encounterRules.replacementChance", "일반 적 대체 확률 (0~1)");
        P("_raidRuntime.rooms", "역할별 맵 변형 / 전투 / 루팅 연결");
        P("_bossBattles", "보스 전투 · V3는 Encounter/Weight만 사용");
        GUILayout.Label("V3 신규 보스방은 스테이지 전용맵 풀을 사용합니다. 위 목록의 Sector Preset은 구버전 맵 전용입니다.", EditorStyles.wordWrappedMiniLabel);
        P("_normalBattle.encounterPresets", "기본 적 Encounter 풀");
        P("_normalBattle.timerSeconds", "일반방 제한 시간"); P("_normalBattle.resetTimerWhenRequirementLost", "조건 상실 시 타이머 리셋");
        P("_raidRuntime.generatorBattery", "발전실 제출 아이템"); P("_raidRuntime.batteryAmount", "필요 수량");
        P("_raidRuntime.vaultEntryKeyTier", "금고 열쇠 등급"); P("_raidRuntime.vaultUsesInternalCombatLock", "금고 내부 잠금 사용");
        EditorGUILayout.HelpBox("섹터 내부의 적 위치/루팅 배치는 Sector Maker에서 작성합니다. 이 창은 참조하는 맵과 전투 풀을 설정합니다. 공용 참조 SO 자체를 수정하면 다른 스테이지에도 영향을 줄 수 있습니다.", MessageType.Info);
    }
    private void DrawEnvironment()
    {
        P("_raidRuntime.hatchAdvancesStage", "해치 → 다음 스테이지"); P("_raidRuntime.hatchRequiredQuest", "해치 선행 퀘스트");
        P("_raidRuntime.durationSeconds", "원정 제한 시간 (0: OFF)"); P("_raidRuntime.overtimeInfectionPerSecond", "초과 시간 감염/초");
        P("_biochemicalGas", "가스 환경");
        EditorGUILayout.HelpBox("감염 누적·최대 HP 잠식은 Game System Tool → 상태이상 → InfectionAggravation에서, 앰플·주사기는 Item Maker에서 설정합니다. 구형 억제 유지 타이머는 사용하지 않습니다.", MessageType.None);
        P("_raidAnomalyTable", "레이드 이상현상 표"); P("_anomalyGasBaseline", "이상현상 가스 강도");
        P("_roomTraitTable", "일반 섹터 특성 표"); P("_raidRuntime.normalSupplyChance", "일반방 추가 보급 확률");
        P("_raidRuntime.weaponSupplyShare", "보급 중 무기 비중"); P("_raidRuntime.normalWeaponSupply", "무기 보급 풀");
        P("_raidRuntime.normalMedicalSupply", "의료 보급 풀"); P("_advanceStageOnStartSectorComplete", "Legacy 시작방 완료 시 진행");
    }
    private void DrawRaw()
    {
        EditorGUILayout.HelpBox("전체 원본 필드입니다. Legacy/WIP 항목도 포함합니다. 보통은 위의 용도별 설정을 사용하세요.", MessageType.Warning);
        var iterator = _serialized.GetIterator(); bool enter = true;
        while (iterator.NextVisible(enter))
        { enter = false; if (iterator.name != "m_Script") EditorGUILayout.PropertyField(iterator, true); }
    }

    private void DrawCandidates(SerializedProperty list, string title, bool hasIds)
    {
        list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, title + " · " + list.arraySize + "개", true);
        if (!list.isExpanded) return;
        for (int i = 0; i < list.arraySize; i++)
        {
            var entry = list.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (hasIds) EditorGUILayout.PropertyField(entry.FindPropertyRelative("id"), new GUIContent("변형 ID"));
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("preset"), GUIContent.none);
                    var weight = entry.FindPropertyRelative("weight");
                    weight.intValue = Mathf.Max(0, EditorGUILayout.IntField(weight.intValue, GUILayout.Width(40)));
                    if (GUILayout.Button("↑", GUILayout.Width(22)) && i > 0) { list.MoveArrayElement(i, i - 1); break; }
                    if (GUILayout.Button("↓", GUILayout.Width(22)) && i + 1 < list.arraySize) { list.MoveArrayElement(i, i + 1); break; }
                    if (GUILayout.Button("×", GUILayout.Width(22))) { list.DeleteArrayElementAtIndex(i); break; }
                }
                if (entry.FindPropertyRelative("preset").objectReferenceValue is SectorPresetSO preset)
                {
                    EditorGUILayout.LabelField("프리팹: " + (preset.SectorPrefab != null ? preset.SectorPrefab.name : "누락"), EditorStyles.miniLabel);
                }
            }
        }
        if (GUILayout.Button("+ 맵 후보 추가")) AppendCandidate(list, null, hasIds);
        Rect drop = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        GUI.Box(drop, "SectorPreset SO를 여기에 드래그 · 숫자는 상대 가중치", EditorStyles.helpBox);
        var evt = Event.current;
        if (!CanWrite || !drop.Contains(evt.mousePosition) || (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)) return;
        var presets = DragAndDrop.objectReferences.OfType<SectorPresetSO>().ToArray();
        if (presets.Length == 0) return;
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (evt.type == EventType.DragPerform)
        { DragAndDrop.AcceptDrag(); foreach (var preset in presets) AppendCandidate(list, preset, hasIds); GUI.changed = true; }
        evt.Use();
    }
    private static void AppendCandidate(SerializedProperty list, SectorPresetSO preset, bool hasIds)
    {
        list.InsertArrayElementAtIndex(list.arraySize);
        var entry = list.GetArrayElementAtIndex(list.arraySize - 1);
        entry.FindPropertyRelative("preset").objectReferenceValue = preset;
        entry.FindPropertyRelative("weight").intValue = 1;
        if (hasIds) entry.FindPropertyRelative("id").stringValue = "variant_" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}
