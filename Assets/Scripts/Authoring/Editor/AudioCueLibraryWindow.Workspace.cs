using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class AudioCueLibraryWindow
{
    private static bool CanAuthor => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
    private static readonly StringComparer CueNameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("ko-KR"), true);
    private static readonly string[] CategoryLabels = { "미분류", "UI · 메뉴", "음악", "플레이어", "무기", "적 슈터", "크리처", "상호작용", "월드", "환경음", "음성" };
    private static readonly string[][] SubcategoryDefaults =
    {
        new[] { "기타" },
        new[] { "버튼 · 선택", "패널 · 창", "인벤토리 · 장비", "거래 · 수리", "퀘스트", "알림", "기타" },
        new[] { "HOME", "레이드", "전투", "메뉴", "기타" },
        new[] { "이동 · 발소리", "전투", "피격", "사망", "대시 · 회피", "상태 · 약물", "기타" },
        new[] { "발사", "재장전", "장착 · 전환", "탄피 · 탄착", "근접", "기타" },
        new[] { "이동 · 발소리", "전투", "피격", "사망", "대시 · 회피", "상태", "기타" },
        new[] { "이동", "공격", "피격", "사망", "등장 · 특수 행동", "기타" },
        new[] { "줍기", "상자 · 금고", "문 · 포탈", "상점", "사용", "기타" },
        new[] { "오브젝트", "시스템 알림", "효과", "기타" },
        new[] { "HOME", "연구소", "가스", "실내", "실외", "기타" },
        new[] { "NPC", "플레이어", "적", "기타" }
    };
    private float _categoryWidth = 225f, _detailWidth = 455f;
    private string _subcategoryFilter = "", _newCueSubcategory = "", _cueRename = "";
    private int _sortMode;
    private bool _showCards, _queuedCatalogRefresh, _showPlayback;
    private SerializedObject _cueData, _playbackData;
    private string WorkspacePreference => "BoardChase.AudioCueLibrary.Workspace." + Application.dataPath;

    private void InitializeWorkspace()
    {
        minSize = new Vector2(1100f, 640f);
        _categoryWidth = EditorPrefs.GetFloat(WorkspacePreference + ".Category", 225f);
        _detailWidth = EditorPrefs.GetFloat(WorkspacePreference + ".Detail", 455f);
        EditorApplication.projectChanged -= QueueCatalogRefresh;
        EditorApplication.projectChanged += QueueCatalogRefresh;
        Undo.undoRedoPerformed -= OnWorkspaceUndo;
        Undo.undoRedoPerformed += OnWorkspaceUndo;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= QueueCatalogRefresh;
        EditorApplication.delayCall -= RefreshQueuedCatalog;
        Undo.undoRedoPerformed -= OnWorkspaceUndo;
        _queuedCatalogRefresh = false;
        EditorPrefs.SetFloat(WorkspacePreference + ".Category", _categoryWidth);
        EditorPrefs.SetFloat(WorkspacePreference + ".Detail", _detailWidth);
        StopPreview();
    }

    private void QueueCatalogRefresh()
    {
        if (_queuedCatalogRefresh) return;
        _queuedCatalogRefresh = true;
        EditorApplication.delayCall += RefreshQueuedCatalog;
    }
    private void RefreshQueuedCatalog() { _queuedCatalogRefresh = false; if (this != null) { _hasSearchedUsage = false; RefreshCatalog(); } }
    private void OnWorkspaceUndo() { _cueData = null; _playbackData = null; _hasSearchedUsage = false; RebuildFilteredCues(); ValidateCatalog(); Repaint(); }
    private static string CategoryLabel(AudioCueCategory category) => CategoryLabels[Mathf.Clamp((int)category, 0, CategoryLabels.Length - 1)];
    private static AudioCueCategory CategoryPopup(string label, AudioCueCategory value) =>
        (AudioCueCategory)EditorGUILayout.Popup(label, (int)value, CategoryLabels);
    private static bool Has(string name, params string[] keywords) => keywords.Any(word => name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

    private static string GetEffectiveSubcategory(AudioCueSO cue)
    {
        if (!string.IsNullOrWhiteSpace(cue.Subcategory)) return cue.Subcategory.Trim();
        string n = cue.name;
        switch (cue.Category)
        {
            case AudioCueCategory.Ui:
                if (Has(n, "quest")) return "퀘스트";
                if (Has(n, "transaction", "repair", "shop", "buy", "sell")) return "거래 · 수리";
                if (Has(n, "inventory", "equip", "item")) return "인벤토리 · 장비";
                if (Has(n, "panel", "window", "popup")) return "패널 · 창";
                if (Has(n, "button", "navigate", "confirm", "cancel", "click")) return "버튼 · 선택";
                if (Has(n, "notify", "alert", "warning")) return "알림";
                break;
            case AudioCueCategory.Weapon:
                if (Has(n, "reload", "magazine")) return "재장전";
                if (Has(n, "equip", "switch", "draw", "holster")) return "장착 · 전환";
                if (Has(n, "shell", "casing", "impact", "hit")) return "탄피 · 탄착";
                if (Has(n, "melee", "swing", "blade")) return "근접";
                if (Has(n, "fire", "shot", "shoot")) return "발사";
                break;
            case AudioCueCategory.Player:
            case AudioCueCategory.Enemy:
                if (Has(n, "death", "dead", "die")) return "사망";
                if (Has(n, "hit", "hurt", "damage")) return "피격";
                if (Has(n, "dash", "dodge", "roll")) return "대시 · 회피";
                if (Has(n, "foot", "step", "walk", "run", "move")) return "이동 · 발소리";
                if (Has(n, "attack", "fire", "shoot", "combat")) return "전투";
                if (Has(n, "infect", "status", "heal", "drug", "ampoule", "suppress")) return cue.Category == AudioCueCategory.Player ? "상태 · 약물" : "상태";
                break;
            case AudioCueCategory.Creature:
                if (Has(n, "death", "dead", "die")) return "사망";
                if (Has(n, "hit", "hurt", "damage")) return "피격";
                if (Has(n, "attack", "spit", "bite", "fire")) return "공격";
                if (Has(n, "walk", "move", "step")) return "이동";
                if (Has(n, "spawn", "roar", "special")) return "등장 · 특수 행동";
                break;
            case AudioCueCategory.Interactable:
                if (Has(n, "pickup", "pick_up", "loot")) return "줍기";
                if (Has(n, "safe", "chest", "box")) return "상자 · 금고";
                if (Has(n, "door", "portal", "hatch")) return "문 · 포탈";
                if (Has(n, "shop", "merchant")) return "상점";
                if (Has(n, "use", "consume")) return "사용";
                break;
            case AudioCueCategory.Music:
                if (Has(n, "home", "hub")) return "HOME";
                if (Has(n, "combat", "battle", "boss")) return "전투";
                if (Has(n, "menu", "title")) return "메뉴";
                if (Has(n, "raid", "stage", "run")) return "레이드";
                break;
            case AudioCueCategory.Ambience:
                if (Has(n, "home", "hub")) return "HOME";
                if (Has(n, "gas", "leak")) return "가스";
                if (Has(n, "lab", "raid", "stage")) return "연구소";
                if (Has(n, "indoor", "room")) return "실내";
                if (Has(n, "outdoor", "wind")) return "실외";
                break;
            case AudioCueCategory.Voice:
                if (Has(n, "npc", "merchant")) return "NPC";
                if (Has(n, "player")) return "플레이어";
                if (Has(n, "enemy", "raider", "scraper", "boss")) return "적";
                break;
            case AudioCueCategory.World:
                if (Has(n, "alert", "warning", "alarm")) return "시스템 알림";
                if (Has(n, "object", "machine", "generator")) return "오브젝트";
                if (Has(n, "effect", "explosion", "impact")) return "효과";
                break;
        }
        return "기타";
    }

    private string[] GetSubcategoryChoices(AudioCueCategory category) =>
        SubcategoryDefaults[Mathf.Clamp((int)category, 0, SubcategoryDefaults.Length - 1)].Concat(_allCues.Where(c => c != null && c.Category == category).Select(GetEffectiveSubcategory))
            .Distinct(StringComparer.Ordinal).OrderBy(value => value, CueNameComparer).ToArray();

    private void DrawSubcategorySuggestions(AudioCueCategory category, Action<string> assign)
    {
        if (!GUILayout.Button("추천 세부 분류 선택 ▾", EditorStyles.miniButton)) return;
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("자동 분류 · 비우기"), false, () => { assign(""); Repaint(); });
        foreach (string value in GetSubcategoryChoices(category))
            menu.AddItem(new GUIContent(value), false, () => { assign(value); Repaint(); });
        menu.ShowAsContext();
    }

    private void DrawWorkspaceToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            _library = EditorGUILayout.ObjectField(_library, typeof(AudioCueLibrarySO), false, GUILayout.Width(250f)) as AudioCueLibrarySO;
            if (EditorGUI.EndChangeCheck()) { _hasSearchedUsage = false; RebuildFilteredCues(); ValidateCatalog(); }
            using (new EditorGUI.DisabledScope(!CanAuthor))
            {
                if (_library == null) { if (GUILayout.Button("라이브러리 생성", EditorStyles.toolbarButton)) CreateLibrary(); }
                else if (GUILayout.Button("전체 Cue 등록", EditorStyles.toolbarButton)) SyncLibrary();
            }
            if (_library != null && GUILayout.Button("라이브러리 찾기", EditorStyles.toolbarButton)) EditorGUIUtility.PingObject(_library);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!CanAuthor))
                if (GUILayout.Button("+ 새 Cue", EditorStyles.toolbarButton)) BeginCreateCue();
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton)) RefreshCatalog();
            if (GUILayout.Button("데이터 점검", EditorStyles.toolbarButton))
            { ValidateCatalog(); ShowNotification(new GUIContent($"확인할 항목 {_validationIssues.Count}개")); }
        }
    }

    private void DrawDetailedCategoryBrowser(float height)
    {
        _categoryWidth = Mathf.Clamp(_categoryWidth, 180f, Mathf.Max(180f, position.width - _detailWidth - 240f));
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(_categoryWidth), GUILayout.Height(height)))
        {
            GUILayout.Label("1. 분류", EditorStyles.boldLabel);
            _browserScroll = EditorGUILayout.BeginScrollView(_browserScroll);
            if (GUILayout.Toggle(_categoryIndex == 0, $"전체 ({_allCues.Count})", EditorStyles.miniButton))
                SetBrowserCategory(0, "");
            for (int i = 0; i < CategoryLabels.Length; i++)
            {
                var category = (AudioCueCategory)i;
                if (GUILayout.Toggle(_categoryIndex == i + 1, $"{CategoryLabels[i]} ({CountCues(category)})", EditorStyles.miniButton))
                {
                    if (_categoryIndex != i + 1) SetBrowserCategory(i + 1, "");
                }
                if (_categoryIndex != i + 1) continue;
                foreach (string sub in GetSubcategoryChoices(category))
                {
                    int count = _allCues.Count(cue => cue != null && cue.Category == category && GetEffectiveSubcategory(cue) == sub);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(12f);
                        if (GUILayout.Toggle(_subcategoryFilter == sub, $"{sub} ({count})", EditorStyles.miniButton))
                            if (_subcategoryFilter != sub) SetBrowserCategory(i + 1, sub);
                    }
                }
                if (!string.IsNullOrEmpty(_subcategoryFilter) && GUILayout.Button("이 분류 전체 보기", EditorStyles.miniButton)) SetBrowserCategory(i + 1, "");
            }
            EditorGUILayout.Space(8f);
            GUILayout.Label("빠른 필터", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _missingClipOnly = EditorGUILayout.ToggleLeft($"클립 없음 ({CountCuesWithoutPlayableClips()})", _missingClipOnly);
            _notRegisteredOnly = EditorGUILayout.ToggleLeft($"미등록 ({CountUnregisteredCues()})", _notRegisteredOnly);
            if (EditorGUI.EndChangeCheck()) RebuildFilteredCues();
            if (GUILayout.Button("분류 · 검색 · 필터 초기화"))
            { _search = ""; _missingClipOnly = false; _notRegisteredOnly = false; SetBrowserCategory(0, ""); }
            EditorGUILayout.HelpBox("세부 분류는 직접 지정하거나 이름 기반 제안을 사용합니다. 분류만 변경해도 재생 동작·기존 참조는 그대로입니다.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }
    }

    private void SetBrowserCategory(int category, string sub)
    {
        if (_categoryIndex == category && _subcategoryFilter == sub) return;
        _categoryIndex = category; _subcategoryFilter = sub; _cueScroll = Vector2.zero;
        RebuildFilteredCues();
    }

    private void DrawWorkspaceSplitter(bool detail)
    {
        Rect rect = GUILayoutUtility.GetRect(5f, 5f, GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(rect, new Color(.2f, .2f, .2f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        int id = GUIUtility.GetControlID(detail ? 217664 : 217663, FocusType.Passive);
        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
        { GUIUtility.hotControl = id; evt.Use(); }
        else if (evt.type == EventType.MouseDrag && GUIUtility.hotControl == id)
        {
            if (detail) _detailWidth = Mathf.Clamp(_detailWidth - evt.delta.x, 350f, Mathf.Max(350f, position.width - _categoryWidth - 240f));
            else _categoryWidth = Mathf.Clamp(_categoryWidth + evt.delta.x, 180f, Mathf.Max(180f, position.width - _detailWidth - 240f));
            evt.Use(); Repaint();
        }
        else if (evt.type == EventType.MouseUp && GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; evt.Use(); }
    }

    private void SortFilteredCues()
    {
        _filteredCues.Sort((a, b) =>
        {
            int order = 0;
            if (_sortMode == 2) { order = a.Category.CompareTo(b.Category); if (order == 0) order = CueNameComparer.Compare(GetEffectiveSubcategory(a), GetEffectiveSubcategory(b)); }
            if (_sortMode == 3) order = b.ClipCount.CompareTo(a.ClipCount);
            if (order == 0) order = CueNameComparer.Compare(a.name, b.name);
            if (order == 0) order = StringComparer.Ordinal.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b));
            return _sortMode == 1 ? -order : order;
        });
    }

    private static bool CueMatchesExtendedSearch(AudioCueSO cue, string search)
    {
        if ((CategoryLabel(cue.Category) + " " + cue.Category + " " + GetEffectiveSubcategory(cue) + " " + AssetDatabase.GetAssetPath(cue)).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        var data = new SerializedObject(cue);
        var groups = data.FindProperty("_audioClipGroups");
        for (int g = 0; g < groups.arraySize; g++)
        {
            var clips = groups.GetArrayElementAtIndex(g).FindPropertyRelative("audioClips");
            for (int i = 0; i < clips.arraySize; i++)
                if (clips.GetArrayElementAtIndex(i).objectReferenceValue is AudioClip clip && clip.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    private void DrawIntuitiveCueList(float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.Height(height)))
        {
            GUILayout.Label("2. Cue 목록 · 우클릭으로 관리", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _search = EditorGUILayout.TextField("검색", _search);
            using (new EditorGUILayout.HorizontalScope())
            {
                _sortMode = EditorGUILayout.Popup(_sortMode, new[] { "가나다 · 오름차순", "가나다 · 내림차순", "분류별", "클립 많은 순" });
                _showCards = GUILayout.Toolbar(_showCards ? 1 : 0, new[] { "목록", "카드" }, GUILayout.Width(105f)) == 1;
            }
            if (EditorGUI.EndChangeCheck()) RebuildFilteredCues();
            GUILayout.Label($"{_filteredCues.Count}개 · {(_categoryIndex == 0 ? "전체" : CategoryLabel((AudioCueCategory)(_categoryIndex - 1)))} { _subcategoryFilter}", EditorStyles.miniLabel);
            _cueScroll = EditorGUILayout.BeginScrollView(_cueScroll);
            if (_showCards) DrawCueCardGrid();
            else
            {
                string previous = null;
                foreach (var cue in _filteredCues)
                {
                    string group = CategoryLabel(cue.Category) + " / " + GetEffectiveSubcategory(cue);
                    if (_sortMode == 2 && previous != group) { GUILayout.Label(group, EditorStyles.miniBoldLabel); previous = group; }
                    Rect row = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
                    if (cue == _selectedCue) EditorGUI.DrawRect(row, new Color(.08f, .38f, .48f));
                    GUI.Label(new Rect(row.x + 5f, row.y + 3f, row.width - 10f, 19f), new GUIContent((EditorUtility.IsDirty(cue) ? "● " : "") + cue.name, AssetDatabase.GetAssetPath(cue)), EditorStyles.boldLabel);
                    GUI.Label(new Rect(row.x + 5f, row.y + 23f, row.width - 10f, 18f), new GUIContent($"{group} · 클립 {cue.ClipCount} · {(cue.looping ? "반복" : "단발")} · {(_library == null ? "라이브러리 미선택" : _library.Contains(cue) ? "등록됨" : "미등록")}", group), EditorStyles.miniLabel);
                    HandleCueContextMenu(row, cue);
                    if (GUI.Button(row, GUIContent.none, GUIStyle.none)) SelectCue(cue);
                }
            }
            if (_filteredCues.Count == 0) EditorGUILayout.HelpBox("일치하는 Cue가 없습니다.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }
    }

    private void SelectCue(AudioCueSO cue)
    {
        CueSelectionUi.SelectionClicked();
        _selectedCue = cue; _cueRename = cue != null ? cue.name : "";
        _hasSearchedUsage = false; _showCreateCuePanel = false;
        _cueData = null; _playbackData = null; _detailScroll = Vector2.zero;
        Repaint();
    }

    private void HandleCueContextMenu(Rect rect, AudioCueSO cue)
    {
        Event evt = Event.current;
        if (evt.type != EventType.ContextClick || !rect.Contains(evt.mousePosition)) return;
        SelectCue(cue);
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Project에서 찾기"), false, () => EditorGUIUtility.PingObject(cue));
        if (CanAuthor)
        {
            menu.AddItem(new GUIContent("복사 · 새 Cue"), false, DuplicateSelectedCue);
            menu.AddItem(new GUIContent("선택 Cue 저장"), false, SaveSelectedAudio);
            menu.AddItem(new GUIContent("삭제 · 휴지통"), false, DeleteSelectedCue);
        }
        menu.ShowAsContext(); evt.Use();
    }

    private void DrawIntuitiveSelectedCue(float height)
    {
        CueSelectionUi.Synchronize(_selectedCue, _showCreateCuePanel ? "create" : "edit");
        _detailWidth = Mathf.Clamp(_detailWidth, 350f, Mathf.Max(350f, position.width - _categoryWidth - 240f));
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(_detailWidth), GUILayout.Height(height)))
        {
            GUILayout.Label(_showCreateCuePanel ? "3. 새 Cue" : "3. Cue 편집", EditorStyles.boldLabel);
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            try
            {
                if (_showCreateCuePanel) { DrawIntuitiveNewCue(); return; }
                var loaded = EditorGUILayout.ObjectField("Cue 불러오기", _selectedCue, typeof(AudioCueSO), false) as AudioCueSO;
                if (loaded != _selectedCue) { SelectCue(loaded); GUIUtility.ExitGUI(); }
                if (_selectedCue == null) { EditorGUILayout.HelpBox("Cue를 선택하거나 새 Cue를 만드세요.", MessageType.Info); return; }
                EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(_selectedCue), EditorStyles.miniLabel, GUILayout.Height(18f));
                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledScope(!CanAuthor))
                {
                    if (GUILayout.Button("저장 · Ctrl+S")) SaveSelectedAudio();
                    if (GUILayout.Button("복사")) DuplicateSelectedCue();
                    if (GUILayout.Button("삭제")) DeleteSelectedCue();
                }
                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledScope(!CanAuthor))
                {
                    _cueRename = EditorGUILayout.TextField("이름", CueSelectionUi.NameFor(_selectedCue, _cueRename));
                    if (GUILayout.Button("이름 변경", GUILayout.Width(80f))) RenameSelectedCue();
                }
                GUILayout.Label(EditorUtility.IsDirty(_selectedCue) ? "● 수정됨 · 선택 SO 저장으로 반영 / Ctrl+Z 실행 취소" : "저장된 Cue 데이터", EditorStyles.miniLabel);
                _cueData ??= new SerializedObject(_selectedCue);
                _cueData.UpdateIfRequiredOrScript();
                using (new EditorGUI.DisabledScope(!CanAuthor)) DrawIntuitiveCueFields();
                if (CanAuthor && _cueData.ApplyModifiedProperties()) { RebuildFilteredCues(); ValidateCatalog(); _hasSearchedUsage = false; }
                else if (!CanAuthor) _cueData.Update();
                DrawPlaybackEditor();
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!_selectedCue.HasPlayableClip))
                        if (GUILayout.Button("▶ 원본 클립 미리듣기")) PreviewCue(_selectedCue);
                    if (GUILayout.Button("■ 정지")) StopPreview();
                    if (GUILayout.Button("Project에서 찾기")) EditorGUIUtility.PingObject(_selectedCue);
                }
                EditorGUILayout.LabelField("미리듣기는 원본 클립 확인용입니다. Cue 배율·공간음·동시 레이어 설정은 적용하지 않습니다.", EditorStyles.wordWrappedMiniLabel);
                using (new EditorGUI.DisabledScope(!CanAuthor || _library == null))
                    if (GUILayout.Button(_library != null && _library.Contains(_selectedCue) ? "선택 Cue 등록 해제" : "선택 Cue 라이브러리 등록")) ToggleCueRegistration();
                GUILayout.Label("이 Cue를 사용하는 에셋", EditorStyles.boldLabel);
                if (GUILayout.Button("직접 참조 찾기 · 요청할 때만 검색")) FindUsagePaths();
                if (_hasSearchedUsage)
                {
                    GUILayout.Label($"직접 참조 {_usagePaths.Count}개 · 런타임 코드/문자열 참조는 제외", EditorStyles.wordWrappedMiniLabel);
                    foreach (string path in _usagePaths)
                        if (GUILayout.Button(path, EditorStyles.linkLabel)) EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(path));
                }
                DrawSelectedCueIssues();
            }
            finally { EditorGUILayout.EndScrollView(); }
        }
    }

    private void DrawIntuitiveNewCue()
    {
        using (new EditorGUI.DisabledScope(!CanAuthor))
        {
            EditorGUILayout.HelpBox("현재 분류에 새 Cue를 만듭니다. 원본 오디오 클립은 그대로 두고 새 SO만 생성합니다.", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("UI 단발음")) ApplyCueTemplate("UI_NewCue", AudioCueCategory.Ui, false);
                if (GUILayout.Button("반복 음악")) ApplyCueTemplate("BGM_NewTrack", AudioCueCategory.Music, true);
                if (GUILayout.Button("월드 효과음")) ApplyCueTemplate("SFX_World_NewCue", AudioCueCategory.World, false);
            }
            _newCueName = EditorGUILayout.TextField("에셋 이름", _newCueName);
            _newCueCategory = CategoryPopup("큰 분류", _newCueCategory);
            _newCueSubcategory = EditorGUILayout.TextField("세부 분류 (선택)", _newCueSubcategory);
            DrawSubcategorySuggestions(_newCueCategory, value => _newCueSubcategory = value);
            _newCuePlaybackRule = EditorGUILayout.ObjectField("재생 설정 (선택)", _newCuePlaybackRule, typeof(AudioConfigurationSO), false) as AudioConfigurationSO;
            _newCueLooping = EditorGUILayout.Toggle("반복 재생", _newCueLooping);
            if (GUILayout.Button("Cue 생성")) { CreateCue(); GUIUtility.ExitGUI(); }
        }
        if (GUILayout.Button("취소")) _showCreateCuePanel = false;
    }

    private void DrawIntuitiveCueFields()
    {
        GUILayout.Label("분류 · 설명", EditorStyles.boldLabel);
        var category = _cueData.FindProperty("_category");
        EditorGUI.BeginChangeCheck();
        var nextCategory = CategoryPopup("큰 분류", (AudioCueCategory)category.enumValueIndex);
        if (EditorGUI.EndChangeCheck()) category.enumValueIndex = (int)nextCategory;
        var sub = _cueData.FindProperty("_subcategory");
        EditorGUILayout.PropertyField(sub, new GUIContent("세부 분류 · 직접 입력"));
        DrawSubcategorySuggestions(nextCategory, value =>
        {
            if (!CanAuthor || _selectedCue == null) return;
            _cueData.Update(); _cueData.FindProperty("_subcategory").stringValue = value;
            _cueData.ApplyModifiedProperties(); RebuildFilteredCues(); Repaint();
        });
        if (string.IsNullOrWhiteSpace(sub.stringValue)) GUILayout.Label("이름 기반 제안: " + GetEffectiveSubcategory(_selectedCue), EditorStyles.miniLabel);
        EditorGUILayout.PropertyField(_cueData.FindProperty("description"), new GUIContent("설명"));
        EditorGUILayout.PropertyField(_cueData.FindProperty("_authoringNotes"), new GUIContent("작업 메모"));
        GUILayout.Label("재생 기본값", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_cueData.FindProperty("_defaultPlaybackConfiguration"), new GUIContent("재생 설정 SO"));
        EditorGUILayout.PropertyField(_cueData.FindProperty("looping"), new GUIContent("반복 재생"));
        EditorGUILayout.PropertyField(_cueData.FindProperty("_gainDb"), new GUIContent("추가 음량 (dB)"));
        DrawClipGroups();
        EditorGUILayout.SelectableLabel("Cue ID: " + _selectedCue.CueId, EditorStyles.miniLabel, GUILayout.Height(18f));
    }

    private void DrawClipGroups()
    {
        var groups = _cueData.FindProperty("_audioClipGroups");
        GUILayout.Label("클립 그룹 · 같은 그룹 안에서 변형음 선택", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("한 그룹 안에서 랜덤/순서대로 하나를 고릅니다. 단순 랜덤 변형음은 같은 그룹에 넣으세요.\n일반 효과음 경로는 여러 그룹을 함께 재생합니다. 음악·일부 직접 재생 경로는 첫 그룹만 사용합니다.", MessageType.None);
        for (int g = 0; g < groups.arraySize; g++)
        {
            var group = groups.GetArrayElementAtIndex(g);
            var clips = group.FindPropertyRelative("audioClips");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"그룹 {g + 1} · 클립 {clips.arraySize}개", EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(g == 0))
                        if (GUILayout.Button("↑", GUILayout.Width(25f))) { groups.MoveArrayElement(g, g - 1); CommitClipStructure(); }
                    using (new EditorGUI.DisabledScope(g == groups.arraySize - 1))
                        if (GUILayout.Button("↓", GUILayout.Width(25f))) { groups.MoveArrayElement(g, g + 1); CommitClipStructure(); }
                    if (GUILayout.Button("복사", GUILayout.Width(43f))) { CopyClipGroup(groups, g); CommitClipStructure(); }
                    if (GUILayout.Button("삭제", GUILayout.Width(43f))) { groups.DeleteArrayElementAtIndex(g); CommitClipStructure(); }
                }
                var mode = group.FindPropertyRelative("sequenceMode");
                EditorGUI.BeginChangeCheck();
                int chosen = EditorGUILayout.Popup("클립 선택 방식", mode.enumValueIndex, new[] { "랜덤", "랜덤 · 바로 이전 클립 제외", "순서대로" });
                if (EditorGUI.EndChangeCheck()) mode.enumValueIndex = chosen;
                for (int i = 0; i < clips.arraySize; i++)
                {
                    var clip = clips.GetArrayElementAtIndex(i);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label((i + 1).ToString(), GUILayout.Width(20f));
                        EditorGUILayout.PropertyField(clip, GUIContent.none);
                        using (new EditorGUI.DisabledScope(clip.objectReferenceValue == null))
                            if (GUILayout.Button("▶", GUILayout.Width(26f))) PreviewAudioClip(clip.objectReferenceValue as AudioClip);
                        if (GUILayout.Button("−", GUILayout.Width(26f))) { RemoveReferenceElement(clips, i); CommitClipStructure(); }
                    }
                }
                Rect drop = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true));
                GUI.Box(drop, "+ AudioClip 여러 개 드래그해서 추가");
                HandleClipDrop(drop, clips);
                if (GUILayout.Button("+ 빈 클립 칸")) { int index = clips.arraySize++; clips.GetArrayElementAtIndex(index).objectReferenceValue = null; CommitClipStructure(); }
            }
        }
        if (GUILayout.Button("+ 클립 그룹 추가"))
        {
            int index = groups.arraySize++;
            var group = groups.GetArrayElementAtIndex(index);
            group.FindPropertyRelative("sequenceMode").enumValueIndex = 1;
            group.FindPropertyRelative("audioClips").arraySize = 0;
            CommitClipStructure();
        }
    }

    private void CommitClipStructure()
    {
        if (!CanAuthor) return;
        _cueData.ApplyModifiedProperties(); RebuildFilteredCues(); ValidateCatalog();
        GUIUtility.ExitGUI();
    }
    private static void RemoveReferenceElement(SerializedProperty array, int index)
    { int size = array.arraySize; array.DeleteArrayElementAtIndex(index); if (array.arraySize == size) array.DeleteArrayElementAtIndex(index); }
    private static void CopyClipGroup(SerializedProperty groups, int source)
    {
        var original = groups.GetArrayElementAtIndex(source);
        int mode = original.FindPropertyRelative("sequenceMode").enumValueIndex;
        var clips = original.FindPropertyRelative("audioClips");
        var values = new UnityEngine.Object[clips.arraySize];
        for (int i = 0; i < values.Length; i++) values[i] = clips.GetArrayElementAtIndex(i).objectReferenceValue;
        int next = groups.arraySize++;
        var target = groups.GetArrayElementAtIndex(next);
        target.FindPropertyRelative("sequenceMode").enumValueIndex = mode;
        var copied = target.FindPropertyRelative("audioClips"); copied.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) copied.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
    private void HandleClipDrop(Rect rect, SerializedProperty clips)
    {
        Event evt = Event.current;
        if (!CanAuthor || !GUI.enabled || !rect.Contains(evt.mousePosition) || (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)) return;
        var added = DragAndDrop.objectReferences.OfType<AudioClip>().ToArray();
        DragAndDrop.visualMode = added.Length > 0 ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
        if (evt.type == EventType.DragPerform && added.Length > 0)
        {
            DragAndDrop.AcceptDrag();
            foreach (var clip in added) { int i = clips.arraySize++; clips.GetArrayElementAtIndex(i).objectReferenceValue = clip; }
            evt.Use(); CommitClipStructure();
        }
        evt.Use();
    }

    private static void PreviewAudioClip(AudioClip clip)
    {
        if (clip == null) return;
        Type util = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        var method = util?.GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        method?.Invoke(null, new object[] { clip, 0, false });
    }

    private static readonly Dictionary<string, string> PlaybackLabels = new()
    {
        ["description"] = "설명", ["outputMixerGroup"] = "출력 믹서 그룹", ["volume"] = "기본 음량", ["pitch"] = "음높이 배율",
        ["randomPitch"] = "음높이 랜덤", ["pitchMin"] = "랜덤 음높이 최솟값", ["pitchMax"] = "랜덤 음높이 최댓값",
        ["spatialBlend"] = "공간음 비율 (0: 2D / 1: 3D)", ["minDistance"] = "감쇠 시작 거리", ["maxDistance"] = "감쇠 끝 거리",
        ["spread"] = "3D 퍼짐 각도", ["useDistanceAttenuationFloor"] = "최소 음량 보장", ["minimumVolumeAtMaxDistance"] = "최대 거리 최소 음량",
        ["useScreenDirectionAssist"] = "탑다운 방향 보조", ["horizontalPanAssist"] = "좌우 방향 보조 강도", ["rearLowPassCutoffHz"] = "후방 저역 필터 (Hz)",
        ["stopFadeTime"] = "정지 페이드 시간 (초)"
    };
    private void DrawPlaybackEditor()
    {
        var rule = _selectedCue.DefaultPlaybackConfiguration;
        _showPlayback = EditorGUILayout.Foldout(_showPlayback, "재생 설정 SO 편집 · 공유 주의", true);
        if (!_showPlayback) return;
        if (rule == null) { EditorGUILayout.HelpBox("기본 재생 설정이 없습니다. 호출부가 별도 설정을 전달할 수 있습니다.", MessageType.None); return; }
        EditorGUILayout.HelpBox("실제 AudioConfigurationSO를 수정합니다. 이 설정을 공유하는 모든 Cue에 영향이 있습니다. 필요하면 복사해서 현재 Cue에만 연결하세요. 호출부의 별도 설정이 우선할 수 있습니다.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(!CanAuthor))
        {
            if (GUILayout.Button("재생 설정 복사 → 이 Cue에만 연결"))
            {
                string path = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(AssetDatabase.GetAssetPath(rule)).Replace('\\', '/') + "/" + rule.name + "_Copy.asset");
                var copy = Instantiate(rule); copy.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(copy, path); SetCreatedAssetGuid(copy); AssetDatabase.SaveAssetIfDirty(copy);
                _cueData.FindProperty("_defaultPlaybackConfiguration").objectReferenceValue = copy;
                _cueData.ApplyModifiedProperties(); _playbackData = null; GUIUtility.ExitGUI();
            }
            if (_playbackData == null || _playbackData.targetObject != rule) _playbackData = new SerializedObject(rule);
            _playbackData.UpdateIfRequiredOrScript();
            var property = _playbackData.GetIterator(); bool enter = true;
            while (property.NextVisible(enter))
            {
                enter = false;
                if (property.name == "m_Script") continue;
                string label = PlaybackLabels.TryGetValue(property.name, out string value) ? value : property.displayName;
                EditorGUILayout.PropertyField(property, new GUIContent(label, property.tooltip), true);
            }
            if (CanAuthor) _playbackData.ApplyModifiedProperties(); else _playbackData.Update();
            if (GUILayout.Button("이 재생 설정 SO 저장")) AssetDatabase.SaveAssetIfDirty(rule);
        }
    }

    private void HandleWorkspaceKeys()
    {
        Event evt = Event.current;
        if (!CanAuthor || evt.type != EventType.KeyDown || _selectedCue == null || _showCreateCuePanel) return;
        if ((evt.control || evt.command) && evt.keyCode == KeyCode.S) { SaveSelectedAudio(); evt.Use(); }
        if (EditorGUIUtility.editingTextField) return;
        if ((evt.control || evt.command) && evt.keyCode == KeyCode.D) { evt.Use(); DuplicateSelectedCue(); }
        else if (evt.keyCode == KeyCode.Delete) { evt.Use(); DeleteSelectedCue(); }
    }
    private void SaveSelectedAudio()
    {
        if (!CanAuthor || _selectedCue == null) return;
        _cueData?.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(_selectedCue);
        ShowNotification(new GUIContent("선택 Cue 저장 완료 · 재생 설정은 별도 저장"));
    }
    private void RenameSelectedCue()
    {
        if (!CanAuthor || _selectedCue == null || string.IsNullOrWhiteSpace(_cueRename)) return;
        string name = SanitizeAssetName(_cueRename, _selectedCue.name).Trim('.');
        if (string.IsNullOrWhiteSpace(name)) return;
        string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(_selectedCue), name);
        if (!string.IsNullOrEmpty(error)) EditorUtility.DisplayDialog("이름 변경 실패", error, "확인");
        _cueRename = _selectedCue.name; RefreshCatalog();
    }
    private void RegisterCue(AudioCueSO cue)
    {
        if (!CanAuthor || _library == null || cue == null || _library.Contains(cue)) return;
        var data = new SerializedObject(_library); var list = data.FindProperty("_cues");
        int i = list.arraySize++; list.GetArrayElementAtIndex(i).objectReferenceValue = cue;
        data.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(_library);
    }
    private static void SetCreatedAssetGuid(ScriptableObject asset)
    {
        // SerializableScriptableObject's inherited identity must never retain the source clone's GUID.
        var data = new SerializedObject(asset);
        var guid = data.FindProperty("_guid");
        if (guid == null || guid.propertyType != SerializedPropertyType.String) return;
        guid.stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private void ToggleCueRegistration()
    {
        if (!CanAuthor || _library == null || _selectedCue == null) return;
        if (!_library.Contains(_selectedCue)) RegisterCue(_selectedCue);
        else
        {
            var data = new SerializedObject(_library); var list = data.FindProperty("_cues");
            for (int i = list.arraySize - 1; i >= 0; i--)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == _selectedCue) RemoveReferenceElement(list, i);
            data.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(_library);
        }
        RebuildFilteredCues(); ValidateCatalog(); _hasSearchedUsage = false;
    }
    private void DeleteSelectedCue()
    {
        if (!CanAuthor || _selectedCue == null) return;
        string path = AssetDatabase.GetAssetPath(_selectedCue);
        if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) || AssetDatabase.IsSubAsset(_selectedCue)) return;
        string references = _hasSearchedUsage ? $"찾은 직접 참조 {_usagePaths.Count}개" : "직접 참조 검사를 아직 실행하지 않았습니다.";
        if (!EditorUtility.DisplayDialog("Cue 삭제 · 휴지통", path + "\n\n" + references + "\n모든 Audio Cue Library에서 등록을 제거합니다. 다른 SO/프리팹/씬 참조는 Missing이 될 수 있습니다.\n원본 오디오 클립과 재생 설정 SO는 삭제하지 않습니다. Cue와 .meta는 OS 휴지통에서 함께 복원할 수 있습니다.", "휴지통으로 이동", "취소")) return;
        int instanceId = _selectedCue.GetInstanceID();
        var libraries = AssetDatabase.FindAssets("t:AudioCueLibrarySO", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<AudioCueLibrarySO>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(library => library != null && library.Contains(_selectedCue)).ToArray();
        StopPreview();
        if (!AssetDatabase.MoveAssetToTrash(path)) { EditorUtility.DisplayDialog("삭제 실패", "휴지통으로 이동하지 못했습니다. 라이브러리는 변경하지 않았습니다.", "확인"); return; }
        foreach (var library in libraries)
        {
            var data = new SerializedObject(library); var list = data.FindProperty("_cues");
            for (int i = list.arraySize - 1; i >= 0; i--)
                if (list.GetArrayElementAtIndex(i).objectReferenceInstanceIDValue == instanceId || list.GetArrayElementAtIndex(i).objectReferenceValue == null) RemoveReferenceElement(list, i);
            data.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(library);
        }
        SelectCue(null); RefreshCatalog();
        ShowNotification(new GUIContent("Cue/.meta를 휴지통으로 이동했습니다. 클립·재생 설정은 유지됩니다."));
        GUIUtility.ExitGUI();
    }
}
