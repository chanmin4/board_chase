/*
 * Save Maker: 편집기 전용 저장 관리 작업 공간.
 * 기존 클래스/.meta는 창 레이아웃 호환을 위해 유지하고, 메뉴는 Authoring/Save Maker만 노출한다.
 * 조회는 SaveMakerStorage, 디스크 변경은 확인 단계를 통과한 명시적 버튼에서만 실행한다.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class SaveInspectorWindow : EditorWindow
{
    [SerializeField] private SaveSystem _saveSystemAsset;
    [SerializeField] private int _section;
    [SerializeField] private float _leftWidth = 215;
    private Vector2 _scroll;
    private string[] _names = { "settings.es3", "profile_default.es3", "run_default.es3" };
    private string[] _configNames;
    private MakerSelectionUiState _selectionUi;
    private MakerSelectionUiState SelectionUi => _selectionUi ??= new MakerSelectionUiState();
    private float _debounce;
    private SettingsSO _settingsLink;
    private VoidEventChannelSO _eventLink;
    private bool _configDirty, _showGuide = true, _settingsDirty;
    private DeviceSettingsSaveData _settingsDraft;
    private readonly Dictionary<SaveMakerPartition, SaveMakerStorage.Snapshot[]> _snapshots = new();
    private string _message, _lastArchive, _confirmation = "";
    private DateTime _refreshed;
    private int _recoveryPartition;
    private string _jsonView = "";
    private bool _dragging;
    private Vector2 _guiOrigin;
    internal Action<string, Rect> ObserveControl;
    // 테스트는 별도의 임시 저장 폴더만 지정한다. 사용자 UI에는 임의 루트 변경 기능을 제공하지 않는다.
    internal string TestRoot;
    private string Root => TestRoot ?? Application.persistentDataPath;
    private static readonly string[] Sections = { "전체 현황", "저장 설정 (프로젝트)", "기기 설정", "영구 진행", "현재 런", "백업 / 복구 / 초기화" };
    private string Pref(string key) => "BoardChase.SaveMaker." + Application.dataPath + "." + key;

    [MenuItem("Tools/Authoring/Save Maker")]
    public static void Open()
    {
        var window = GetWindow<SaveInspectorWindow>("Save Maker");
        window.Show();
    }
    private void OnEnable()
    {
        titleContent = new GUIContent("Save Maker"); minSize = new Vector2(960, 600);
        _leftWidth = EditorPrefs.GetFloat(Pref("LeftWidth"), 215);
        if (_saveSystemAsset == null) _saveSystemAsset = AssetDatabase.LoadAssetAtPath<SaveSystem>("Assets/ScriptableObject/SaveSystem/SaveSystem.asset");
        if (_saveSystemAsset == null)
            _saveSystemAsset = AssetDatabase.FindAssets("t:SaveSystem").Select(g => AssetDatabase.LoadAssetAtPath<SaveSystem>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();
        ReadConfigDraft(); RefreshSnapshots();
        Undo.undoRedoPerformed += OnUndo;
    }
    private void OnDisable() => Undo.undoRedoPerformed -= OnUndo;
    private void OnUndo()
    {
        // 다른 Maker의 Undo가 발생해도 이 창에서 아직 적용하지 않은 초안을 버리지 않는다.
        if (_configDirty || _settingsDirty) { _message = "Undo/Redo가 발생했습니다. 미적용 초안은 유지했습니다. 필요하면 Refresh로 다시 읽으세요."; Repaint(); return; }
        ReadConfigDraft(); RefreshSnapshots();
    }
    private void Observe(string id, Rect rect)
    {
        if (ObserveControl == null || Event.current.type != EventType.Repaint) return;
        rect.position = GUIUtility.GUIToScreenPoint(rect.position) - _guiOrigin; ObserveControl(id, rect);
    }
    private void OnGUI()
    {
        _guiOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
        EditorGUILayout.LabelField("SAVE MAKER", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            var next = (SaveSystem)EditorGUILayout.ObjectField(_saveSystemAsset, typeof(SaveSystem), false, GUILayout.MinWidth(200));
            if (next != _saveSystemAsset && CanDiscardDrafts())
            { _saveSystemAsset = next; ReadConfigDraft(); RefreshSnapshots(); }
            if (GUILayout.Button("저장 폴더", EditorStyles.toolbarButton, GUILayout.Width(80))) EditorUtility.RevealInFinder(Root);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(65)) && CanDiscardDrafts()) { ReadConfigDraft(); RefreshSnapshots(); }
            if (GUILayout.Button("Guide", EditorStyles.toolbarButton, GUILayout.Width(55))) _showGuide = !_showGuide;
        }
        if (_showGuide)
            EditorGUILayout.HelpBox("① 전체 현황에서 파일 상태 확인  →  ② 프로젝트의 저장 설정 또는 이 PC의 세이브 선택  →  ③ 적용 대상을 확인하고 실행\n" +
                "프로젝트 설정 저장과 실제 세이브 변경은 별개입니다. 조회는 읽기 전용 · 복구/초기화는 원본 백업 후 실행 · Play Mode에서는 디스크 변경 금지", MessageType.Info);
        string blocked = SaveMakerStorage.WriteBlockedReason(_saveSystemAsset);
        if (blocked != null)
        {
            EditorGUILayout.HelpBox(blocked, MessageType.Warning);
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
                GUILayout.Button("스크립트 재로드 (작업 저장 후 실행)"))
            {
                if (CanDiscardDrafts() && EditorUtility.DisplayDialog("스크립트 재로드", "미저장 작업을 저장했는지 확인하세요. 메모리 세이브 캐시를 비우기 위해 스크립트를 재로드합니다. 디스크 세이브는 변경하지 않습니다.", "재로드", "취소"))
                    EditorUtility.RequestScriptReload();
            }
        }
        if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);
        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(_leftWidth), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("SAVE WORKSPACE", EditorStyles.boldLabel);
                for (int i = 0; i < Sections.Length; i++)
                {
                    if (i == 2) EditorGUILayout.LabelField("이 PC에 저장된 데이터", EditorStyles.miniBoldLabel);
                    Color old = GUI.backgroundColor;
                    if (_section == i) GUI.backgroundColor = new Color(.3f, .65f, .8f);
                    if (GUILayout.Button(Sections[i], GUILayout.Height(34))) { _section = i; _scroll = Vector2.zero; }
                    Observe("section:" + i, GUILayoutUtility.GetLastRect()); GUI.backgroundColor = old;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("마지막 조회: " + _refreshed.ToString("HH:mm:ss"), EditorStyles.miniLabel);
                EditorGUILayout.LabelField("세이브는 Assets 폴더가 아니라\npersistentDataPath에 저장됩니다.", EditorStyles.wordWrappedMiniLabel);
            }
            DrawSplitter();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandHeight(true)))
            {
                SelectionUi.Synchronize(_saveSystemAsset, _section.ToString());
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                if (_section == 0) DrawOverview();
                else if (_section == 1) DrawConfiguration();
                else if (_section >= 2 && _section <= 4) DrawPartition((SaveMakerPartition)(_section - 2));
                else DrawRecovery();
                EditorGUILayout.EndScrollView();
            }
        }
    }
    private void DrawSplitter()
    {
        Rect rect = GUILayoutUtility.GetRect(6, 1, GUILayout.Width(6), GUILayout.ExpandHeight(true));
        Observe("split", rect); EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        int id = GUIUtility.GetControlID(0x5A4E, FocusType.Passive, rect); var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition)) { _dragging = true; GUIUtility.hotControl = id; e.Use(); }
        if (_dragging && e.type == EventType.MouseDrag) { _leftWidth = Mathf.Clamp(_leftWidth + e.delta.x, 175, Mathf.Min(360, position.width - 640)); e.Use(); Repaint(); }
        if (_dragging && e.type == EventType.MouseUp) { _dragging = false; GUIUtility.hotControl = 0; EditorPrefs.SetFloat(Pref("LeftWidth"), _leftWidth); e.Use(); }
    }
    private bool CanDiscardDrafts() => !(_configDirty || _settingsDirty) ||
        EditorUtility.DisplayDialog("적용하지 않은 초안", "적용하지 않은 설정 초안을 버리고 다시 읽을까요? 실제 파일은 변경하지 않습니다.", "초안 버리기", "취소");

    private void RefreshSnapshots()
    {
        _names = _saveSystemAsset == null ? new[] { "settings.es3", "profile_default.es3", "run_default.es3" } :
            new[] { _saveSystemAsset.SettingsFilename, _saveSystemAsset.ProfileFilename, _saveSystemAsset.RunFilename };
        _snapshots.Clear();
        for (int i = 0; i < 3; i++)
        {
            var partition = (SaveMakerPartition)i;
            var files = new SaveMakerStorage.Snapshot[4];
            files[0] = SaveMakerStorage.Read(Root, partition, _names[i]);
            for (int b = 1; b <= 3; b++) files[b] = SaveMakerStorage.Read(Root, partition, SaveMakerStorage.BackupName(_names[i], b));
            _snapshots[partition] = files;
        }
        var settings = _snapshots[SaveMakerPartition.Settings][0];
        _settingsDraft = settings.Valid ? JsonUtility.FromJson<DeviceSettingsSaveData>(settings.Json) : null;
        _settingsDirty = false; _refreshed = DateTime.Now; Repaint();
    }
    private void ReadConfigDraft()
    {
        SelectionUi.SelectionClicked();
        if (_saveSystemAsset == null) { _configNames = null; _configDirty = false; return; }
        var so = new SerializedObject(_saveSystemAsset);
        _configNames = new[] { _saveSystemAsset.SettingsFilename, _saveSystemAsset.ProfileFilename, _saveSystemAsset.RunFilename };
        _debounce = so.FindProperty("_settingsWriteDebounceSeconds").floatValue;
        _settingsLink = so.FindProperty("_currentSettings").objectReferenceValue as SettingsSO;
        _eventLink = so.FindProperty("_saveSettingsEvent").objectReferenceValue as VoidEventChannelSO;
        _configDirty = false;
    }
    private void DrawOverview()
    {
        EditorGUILayout.LabelField("내 세이브는 어디에 저장되나요?", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(Root, EditorStyles.textField, GUILayout.Height(22));
        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            for (int i = 0; i < 3; i++)
            {
                var p = (SaveMakerPartition)i; var snapshot = _snapshots[p][0];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(SaveMakerStorage.Label(p) + "\n" + snapshot.Status, GUILayout.Height( 60 ))) _section = i + 2;
                    EditorGUILayout.LabelField(_names[i], EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.LabelField(i == 0 ? "오디오·화면·언어·키 바인딩" : i == 1 ? "퀘스트·상점 해금·창고·영구 강화" : "인벤토리·장비·맵·재고·타이머", EditorStyles.wordWrappedLabel);
                }
            }
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox("게임이 저장할 때마다 파티션별 자동 백업 3개가 순환합니다. Save Maker에서 복구/초기화하기 전에는 별도 원본 아카이브도 만듭니다.", MessageType.Info);
        EditorGUILayout.HelpBox("주의: 런은 영구 진행과 별개입니다. 런만 초기화하면 퀘스트·창고는 유지됩니다. 새 게임 초기화는 프로필과 런을 함께 초기화하고 기기 설정은 보존합니다.", MessageType.None);
        if (GUILayout.Button("백업 확인 / 복구 / 초기화 →", GUILayout.Height(30))) _section = 5;
    }
    private void DrawConfiguration()
    {
        EditorGUILayout.LabelField("프로젝트 저장 설정", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("이 페이지는 SaveSystem 에셋을 편집합니다. 기존 세이브 파일은 이동·삭제·변환하지 않습니다. 파일명을 바꾸면 다음 실행에서 다른 저장 파일을 사용하므로 주의하세요.", MessageType.Info);
        if (_configNames == null) { EditorGUILayout.HelpBox("상단에서 SaveSystem 에셋을 선택하세요.", MessageType.Warning); return; }
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < 3; i++) _configNames[i] = EditorGUILayout.DelayedTextField(SaveMakerStorage.Label((SaveMakerPartition)i) + " 파일", _configNames[i]);
            _debounce = EditorGUILayout.Slider(new GUIContent("설정 저장 지연 (초)", "슬라이더/키 설정의 연속 변경을 모아 저장하는 시간. 게임 진행 자동 저장 주기가 아닙니다."), _debounce, .1f, 3f);
            _settingsLink = (SettingsSO)EditorGUILayout.ObjectField("런타임 SettingsSO", _settingsLink, typeof(SettingsSO), false);
            _eventLink = (VoidEventChannelSO)EditorGUILayout.ObjectField("설정 저장 이벤트", _eventLink, typeof(VoidEventChannelSO), false);
            if (EditorGUI.EndChangeCheck()) _configDirty = true;
            EditorGUILayout.LabelField("자동 백업 세대 수", "3 (현재 런타임 고정)");
            EditorGUILayout.LabelField("SettingsSO는 런타임 설정 연결입니다. 이 PC의 저장 값은 '기기 설정'에서 별도로 편집하세요.", EditorStyles.wordWrappedMiniLabel);
            string error = SaveMakerStorage.ValidateFilenames(_configNames);
            var so = new SerializedObject(_saveSystemAsset);
            foreach (string legacy in new[] { "_legacyEasySaveFilename", "_legacyEasySaveBackupFilename" })
                if (_configNames.Any(n => string.Equals(n, so.FindProperty(legacy).stringValue, StringComparison.OrdinalIgnoreCase)))
                    error = "기존 마이그레이션 원본 파일과 같은 이름을 사용할 수 없습니다.";
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
            using (new EditorGUI.DisabledScope(!_configDirty || error != null))
                if (GUILayout.Button("프로젝트 설정 적용 + 에셋 저장", GUILayout.Height(30)))
                {
                    if (_settingsDirty && !EditorUtility.DisplayDialog("기기 설정 초안", "프로젝트 설정을 적용하면 파일을 다시 조회합니다. 아직 적용하지 않은 기기 설정 초안을 버릴까요?", "초안 버리고 계속", "취소")) return;
                    bool pathsChanged = !_configNames.SequenceEqual(_names);
                    if (!pathsChanged || EditorUtility.DisplayDialog("저장 파일 경로 변경", "기존 파일은 그대로 둡니다. 다음 실행에서 새로운 파일명으로 읽고 씁니다. 계속할까요?", "설정 적용", "취소"))
                    {
                        Undo.RecordObject(_saveSystemAsset, "Edit Save Maker configuration");
                        so.FindProperty("_settingsFilename").stringValue = _configNames[0];
                        so.FindProperty("_profileFilename").stringValue = _configNames[1];
                        so.FindProperty("_runFilename").stringValue = _configNames[2];
                        so.FindProperty("_settingsWriteDebounceSeconds").floatValue = _debounce;
                        so.FindProperty("_currentSettings").objectReferenceValue = _settingsLink;
                        so.FindProperty("_saveSettingsEvent").objectReferenceValue = _eventLink;
                        so.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(_saveSystemAsset);
                        _message = "SaveSystem 에셋 설정을 저장했습니다. 기존 세이브 파일은 변경하지 않았습니다.";
                        ReadConfigDraft(); RefreshSnapshots();
                    }
                }
            if (GUILayout.Button("초안 되돌리기")) ReadConfigDraft();
        }
    }
}
