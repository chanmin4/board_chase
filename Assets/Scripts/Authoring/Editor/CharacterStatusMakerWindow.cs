using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Edits authored SOs directly. Never edits scene instances, applies effects, or rewires prefabs.</summary>
public sealed class CharacterStatusMakerWindow : EditorWindow
{
    private enum Domain { All, Player, Enemy, Creature, Status, Experience, Usage, Infection, Recycling, Passive, Travel, Map, Rarity, Shared }
    private static readonly string[] DomainNames = { "전체 SO", "플레이어 · 대시", "적 스탯 · AI · 순찰", "크리처 · 행동", "상태이상", "경험치 · 레벨", "아이템 사용 레벨", "감염", "시간제 분해기", "패시브 · 리롤 · 배정", "섹터 이동", "지도 · 발견 · 아이콘", "아이템 등급 색상", "공유 기본값 · 피격" };
    [SerializeField] private Domain _domain;
    private Vector2 _domainScroll;
    private enum Family { All, Shooter, Creature, Status, Common, OtherCharacter }
    private static readonly string[] Families = { "전체", "슈터", "크리처", "상태이상", "공통 설정", "기타 캐릭터" };
    private static readonly string[][] Details =
    {
        new[] { "전체" },
        new[] { "전체 슈터", "플레이어 스탯", "적 스탯", "NPC 스탯", "기타 슈터 스탯", "관련 설정" },
        new[] { "전체 크리처", "일반 적 스탯", "네임드 · 보스 스탯", "공격 설정", "이동 설정", "행동 · 패턴 설정" },
        new[] { "전체 상태이상", "상태이상 정의" },
        new[] { "전체 공통 설정", "무적", "감염 · 난이도", "피격 · 기타" },
        new[] { "전체 기타 캐릭터", "스탯" }
    };
    private static readonly HashSet<string> RelatedTypes = new(StringComparer.Ordinal)
    {
        "ExperienceRulesSO", "ItemUsageRulesSO", "RecyclingRulesSO",
        "SectorTravelRulesSO", "MapDiscoveryRulesSO", "ItemRarityStyleSO", "SectorMapIconSetSO", "MiniMapUISettingsSO", "FullMapUISettingsSO", "PassiveModuleRulesSO", "ShooterCommonRulesSO", "PlayerInteractConfigSO", "DifficultyRulesSO",
        "DamageFlash_GetHitConfigSO", "CreatureMovementConfigSO",
        "EnemyAttackConfigSO", "EnemyFireProjectileConfigSO", "EnemyArcBombAttackConfigSO",
        "NamedRepositionConfigSO", "NamedAttackConfigSO", "NamedPatternConfigSO",
        "MutarusPoisonPuddleAttackConfigSO", "MutarusProjectileAttackConfigSO",
        "NormalEnemyBehaviorConfigSO", "NamedNormalAttackConfigSO"
    };
    private sealed class Entry
    {
        public ScriptableObject asset;
        public string path;
        public Family family;
        public int detail;
    }
    [SerializeField] private ScriptableObject _selected;
    [SerializeField] private string _search = "";
    [SerializeField] private Family _family;
    [SerializeField] private int _detail;
    [SerializeField] private bool _descending;
    private readonly List<Entry> _entries = new();
    private readonly List<Entry> _visible = new();
    private readonly List<string> _references = new();
    private Type[] _createTypes = Array.Empty<Type>();
    private Type[] _availableCreateTypes = Array.Empty<Type>();
    private string[] _creationLabels = Array.Empty<string>();
    private int _createTypeIndex;
    private string _newName = "New Character Config";
    private string _rename = "";
    private Vector2 _listScroll, _editScroll, _referenceScroll;
    private float _listWidth = 285f;
    private int _splitControl;
    private bool _refreshQueued, _referencesChecked;
    private int _inspectorTab;
    private CharacterStatusMakerInspector _inspector;
    private string PreferenceKey => "BoardChase.CharacterStatusMaker.Width." + Application.dataPath;
    private static bool CanWrite => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

    [MenuItem("Tools/Authoring/Game System Tool")]
    public static void Open()
    {
        var window = GetWindow<CharacterStatusMakerWindow>("Game System Tool");
        window.minSize = new Vector2(880f, 540f);
        window.Show();
    }

    internal static void OpenAsset(ScriptableObject asset)
    {
        if (asset == null || !Supports(asset.GetType()) || !AssetDatabase.Contains(asset)) return;
        Open();
        GetWindow<CharacterStatusMakerWindow>().RevealAsset(asset);
    }

    private void OnEnable()
    {
        if (_domain == Domain.Infection) _domain = Domain.Status;
        minSize = new Vector2(880f, 540f);
        _listWidth = EditorPrefs.GetFloat(PreferenceKey, 285f);
        _createTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
            .Where(type => !type.IsAbstract && !type.IsGenericType && Supports(type))
            .OrderBy(type => type.Name, StringComparer.Ordinal).ToArray();
        EditorApplication.projectChanged += QueueRefresh;
        Undo.undoRedoPerformed += OnUndo;
        RefreshLibrary();
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= QueueRefresh;
        EditorApplication.delayCall -= DeferredRefresh;
        Undo.undoRedoPerformed -= OnUndo;
        _refreshQueued = false;
        EditorPrefs.SetFloat(PreferenceKey, _listWidth);
        _inspector = null;
    }

    private void OnUndo() { _inspector?.Rebuild(); Repaint(); }
    private void QueueRefresh()
    {
        if (_refreshQueued) return;
        _refreshQueued = true;
        EditorApplication.delayCall += DeferredRefresh;
    }
    private void DeferredRefresh()
    {
        _refreshQueued = false;
        if (this != null) RefreshLibrary();
    }

    internal static bool Supports(Type type)
    {
        if (typeof(EntityStatConfigSO).IsAssignableFrom(type) ||
            typeof(StatusEffectDefinitionSO).IsAssignableFrom(type) || typeof(InvulnerabilityConfigSO).IsAssignableFrom(type)) return true;
        for (Type current = type; current != null && current != typeof(ScriptableObject); current = current.BaseType)
            if (RelatedTypes.Contains(current.Name)) return true;
        return false;
    }

    private static bool InheritsNamed(Type type, params string[] names)
    {
        for (Type current = type; current != null && current != typeof(ScriptableObject); current = current.BaseType)
            foreach (string name in names)
                if (string.Equals(current.Name, name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // Asset browsing and creation use the same inheritance rules. Asset names/paths never determine roles.
    private static (Family family, int detail) Classify(Type type)
    {
        if (typeof(StatusEffectDefinitionSO).IsAssignableFrom(type)) return (Family.Status, 1);
        if (typeof(InvulnerabilityConfigSO).IsAssignableFrom(type)) return (Family.Common, 1);
        if (typeof(ShooterStatConfigSO).IsAssignableFrom(type))
        {
            // No dedicated NPC stat SO exists currently. Reserve this branch for explicit NPC role types.
            if (InheritsNamed(type, "NpcStatsConfigSO", "NpcStatConfigSO", "NpcShooterConfigSO", "NpcShooterStatConfigSO")) return (Family.Shooter, 3);
            if (typeof(PlayerStatsConfigSO).IsAssignableFrom(type)) return (Family.Shooter, 1);
            if (typeof(EnemyShooterConfigSO).IsAssignableFrom(type)) return (Family.Shooter, 2);
            return (Family.Shooter, 4);
        }
        if (typeof(EnemyStatConfigSO).IsAssignableFrom(type))
            return (Family.Creature, InheritsNamed(type, "NamedEnemyStatConfigSO") ? 2 : 1);
        if (typeof(EntityStatConfigSO).IsAssignableFrom(type)) return (Family.OtherCharacter, 1);
        if (InheritsNamed(type, "CreatureMovementConfigSO")) return (Family.Creature, 4);
        if (InheritsNamed(type, "NamedPatternConfigSO", "NormalEnemyBehaviorConfigSO", "NamedRepositionConfigSO")) return (Family.Creature, 5);
        if (InheritsNamed(type, "EnemyAttackConfigSO", "NamedNormalAttackConfigSO")) return (Family.Creature, 3);
        if (InheritsNamed(type, "DashConfigSO", "PlayerInteractConfigSO", "PlayerInfectionRulesSO")) return (Family.Shooter, 5);
        if (InheritsNamed(type, "DifficultyRulesSO", "InfectionControlRulesSO")) return (Family.Common, 2);
        return (Family.Common, 3);
    }

    private bool MatchesCategory(Family family, int detail) => _family == Family.All ||
        (_family == family && (_detail == 0 || _detail == detail));

    private static string CategoryPath(Family family, int detail) =>
        Families[(int)family] + " / " + Details[(int)family][detail];

    private void RevealAsset(ScriptableObject asset)
    {
        var category = Classify(asset.GetType());
        _domain = DomainFor(asset.GetType());
        _family = category.family;
        _detail = category.detail;
        _search = "";
        FilterLibrary();
        Select(asset);
    }

    private void RefreshLibrary()
    {
        _entries.Clear();
        // Cached library scan on opening / project changes only, never per repaint.
        var guids = new HashSet<string>();
        foreach (Type type in _createTypes)
            foreach (string guid in AssetDatabase.FindAssets("t:" + type.Name, new[] { "Assets" })) guids.Add(guid);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset != null && Supports(asset.GetType()))
            {
                var category = Classify(asset.GetType());
                _entries.Add(new Entry { asset = asset, path = path, family = category.family, detail = category.detail });
            }
        }
        FilterLibrary();
        if (_selected != null && !Supports(_selected.GetType())) Select(null);
        if (_selected != null && _inspector == null) Select(_selected);
        if (_selected == null) _inspector = null;
        // Asset paths may have changed since the last reference scan.
        _referencesChecked = false;
        Repaint();
    }

    private void FilterLibrary()
    {
        _family = (Family)Mathf.Clamp((int)_family, 0, Families.Length - 1);
        _detail = Mathf.Clamp(_detail, 0, Details[(int)_family].Length - 1);
        _visible.Clear();
        foreach (Entry entry in _entries)
        {
            if (entry.asset == null || !MatchesDomain(entry.asset.GetType()) || (_domain == Domain.All && !MatchesCategory(entry.family, entry.detail))) continue;
            if (!string.IsNullOrWhiteSpace(_search) &&
                (entry.asset.name + " " + entry.asset.GetType().Name + " " + entry.path)
                    .IndexOf(_search.Trim(), StringComparison.OrdinalIgnoreCase) < 0) continue;
            _visible.Add(entry);
        }
        var comparer = StringComparer.Create(CultureInfo.GetCultureInfo("ko-KR"), true);
        _visible.Sort((a, b) =>
        {
            int groupOrder = a.family.CompareTo(b.family);
            if (groupOrder == 0) groupOrder = a.detail.CompareTo(b.detail);
            if (groupOrder != 0) return groupOrder;
            int order = comparer.Compare(a.asset.name, b.asset.name);
            if (order == 0) order = StringComparer.Ordinal.Compare(a.path, b.path);
            return _descending ? -order : order;
        });
        Type previous = _createTypeIndex >= 0 && _createTypeIndex < _availableCreateTypes.Length ? _availableCreateTypes[_createTypeIndex] : null;
        _availableCreateTypes = _createTypes.Where(type =>
        {
            var category = Classify(type);
            return MatchesDomain(type) && (_domain != Domain.All || MatchesCategory(category.family, category.detail));
        }).OrderBy(type => (int)Classify(type).family).ThenBy(type => Classify(type).detail)
            .ThenBy(type => type.Name, StringComparer.Ordinal).ToArray();
        _creationLabels = _availableCreateTypes.Select(CreationLabel).ToArray();
        _createTypeIndex = Mathf.Max(0, Array.IndexOf(_availableCreateTypes, previous));
    }

    private void Select(ScriptableObject asset)
    {
        _selected = asset;
        _rename = asset != null ? asset.name : "";
        _inspector = asset != null ? new CharacterStatusMakerInspector(asset) : null;
        _editScroll = Vector2.zero;
        _references.Clear();
        _referencesChecked = false;
        Repaint();
    }

    private void OnGUI()
    {
        AuthoringWorkspaceUi.DrawHeader("Game System Tool", "캐릭터 · 상태이상 · 경험치 · 게임 공통 수치",
            "실제 에셋을 직접 수정합니다. 런타임 임시값이 아닙니다. 변경은 Undo 가능하며 선택 SO 저장으로 파일에 반영합니다.",
            "1 SO 선택", "2 수치 · 아이콘 편집", "3 선택 SO 저장");
        if (!CanWrite) EditorGUILayout.HelpBox("Play Mode / 컴파일 중에는 읽기 전용입니다. 원본 SO 보호를 위해 편집은 Play 종료 후 진행하세요.", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("기능별 메뉴는 왼쪽 · 각 메뉴는 별개의 원본 SO를 수정합니다.", EditorStyles.miniLabel);
            if (GUILayout.Button("Stage Maker")) StageMakerWindow.Open();
            if (GUILayout.Button("Item Maker")) ItemMakerWindow.Open();
        }
        HandleKeyboard();
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawDomains();
            DrawLibrary();
            Rect splitter = GUILayoutUtility.GetRect(6f, 6f, GUILayout.ExpandHeight(true));
            DrawSplitter(splitter);
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true))) DrawWorkbench();
        }
    }


    private static Domain DomainFor(Type type)
    {
        if (typeof(PlayerStatsConfigSO).IsAssignableFrom(type) || type.Name == "PlayerInteractConfigSO") return Domain.Player;
        if (typeof(EnemyShooterConfigSO).IsAssignableFrom(type)) return Domain.Enemy;
        if (typeof(StatusEffectDefinitionSO).IsAssignableFrom(type)) return Domain.Status;
        if (type == typeof(ExperienceRulesSO)) return Domain.Experience;
        if (type == typeof(ItemUsageRulesSO)) return Domain.Usage;
        if (type == typeof(RecyclingRulesSO)) return Domain.Recycling;
        if (type == typeof(PassiveModuleRulesSO)) return Domain.Passive;
        if (type == typeof(SectorTravelRulesSO)) return Domain.Travel;
        if (type == typeof(ItemRarityStyleSO)) return Domain.Rarity;
        if (type == typeof(MapDiscoveryRulesSO) || type == typeof(SectorMapIconSetSO) || type.Name is "MiniMapUISettingsSO" or "FullMapUISettingsSO") return Domain.Map;
        if (Classify(type).family == Family.Creature) return Domain.Creature;
        return Domain.Shared;
    }

    private bool MatchesDomain(Type type) => _domain == Domain.All || DomainFor(type) == _domain;

    private void DrawDomains()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(172f)))
        {
            GUILayout.Label("기능 메뉴", EditorStyles.boldLabel);
            _domainScroll = EditorGUILayout.BeginScrollView(_domainScroll);
            for (int i=0; i<DomainNames.Length; i++)
            {
                if (i == (int)Domain.Infection) continue; // Now owned by the Status menu; keep serialized indices stable.
                var domain=(Domain)i;
                if (GUILayout.Toggle(_domain==domain,DomainNames[i],EditorStyles.miniButton,GUILayout.Height(31f)) && _domain!=domain)
                {
                    _domain=domain;_family=Family.All;_detail=0;_search="";
                    _listScroll=Vector2.zero;FilterLibrary();
                    // Prefer the active Resources source; copies remain independently editable in the list.
                    var first=_visible.FirstOrDefault(e=>e.path.StartsWith("Assets/Resources/",StringComparison.Ordinal)) ?? _visible.FirstOrDefault();
                    Select(first?.asset);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("설정값은 담당 SO에만 저장합니다.\n적 AI → 적 스탯\n스테이지 추첨 → Stage Maker\n무기 성능 → Item Maker", MessageType.None);
        }
    }

    private void DrawLibrary()
    {
        _listWidth = Mathf.Clamp(_listWidth, 220f, Mathf.Max(220f, position.width - 650f));
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(_listWidth)))
        {
            EditorGUILayout.LabelField(DomainNames[(int)_domain], EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(_domain != Domain.All))
            {
            int family = EditorGUILayout.Popup("분류", (int)_family, Families);
            if (family != (int)_family) { _family = (Family)family; _detail = 0; _listScroll = Vector2.zero; }
            if (Details[family].Length > 2)
                _detail = EditorGUILayout.Popup("세부 분류", _detail, Details[family]);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _search = GUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.textField);
                if (GUILayout.Button("×", GUILayout.Width(24f))) { _search = ""; GUI.FocusControl(null); }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _descending = GUILayout.Toggle(_descending, _descending ? "ㅎ → ㄱ" : "ㄱ → ㅎ", EditorStyles.miniButton, GUILayout.Width(76f));
                if (GUILayout.Button("새로고침", EditorStyles.miniButton)) RefreshLibrary();
                GUILayout.Label($"{_visible.Count}개", EditorStyles.miniLabel, GUILayout.Width(48f));
            }
            if (EditorGUI.EndChangeCheck()) FilterLibrary();
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            Family previousFamily = Family.All;
            int previousDetail = -1;
            foreach (Entry entry in _visible)
            {
                if (entry.family != previousFamily || entry.detail != previousDetail)
                {
                    EditorGUILayout.Space(5f);
                    GUILayout.Label(CategoryPath(entry.family, entry.detail), EditorStyles.miniBoldLabel);
                    previousFamily = entry.family;
                    previousDetail = entry.detail;
                }
                string name = (EditorUtility.IsDirty(entry.asset) ? "● " : "") + entry.asset.name;
                var status = entry.asset as StatusEffectDefinitionSO;
                var content = new GUIContent(name, status != null && status.Icon != null ? status.Icon.texture :
                    AssetPreview.GetMiniThumbnail(entry.asset), CategoryPath(entry.family, entry.detail) + "\n" + entry.asset.GetType().Name + "\n" + entry.path);
                // Long asset names must clip inside the resizable pane, not widen its scroll content.
                Rect row = GUILayoutUtility.GetRect(0f, 32f, GUILayout.ExpandWidth(true));
                if (GUI.Toggle(row, entry.asset == _selected, content, EditorStyles.miniButton) && entry.asset != _selected) Select(entry.asset);
                Event evt = Event.current;
                if (row.Contains(evt.mousePosition) && evt.type == EventType.ContextClick)
                {
                    Select(entry.asset);
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Project에서 찾기"), false, () => EditorGUIUtility.PingObject(entry.asset));
                    if (CanWrite)
                    {
                        menu.AddItem(new GUIContent("복사"), false, Duplicate);
                        menu.AddItem(new GUIContent("삭제 · 휴지통"), false, Delete);
                    }
                    menu.ShowAsContext(); evt.Use();
                }
            }
            if (_visible.Count == 0) EditorGUILayout.HelpBox(_family == Family.Shooter && _detail == 3 ?
                "NPC 전용 스탯 SO 타입이 현재 없습니다. 공용 플레이어/적 SO는 원래 상속 분류에 남겨둡니다." :
                "일치하는 SO가 없습니다. 분류/검색을 바꾸거나 아래에서 새 SO를 만드세요.", MessageType.None);
            EditorGUILayout.EndScrollView();
            GUILayout.Label("새 SO 만들기", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!CanWrite || _availableCreateTypes.Length == 0))
            {
                _createTypeIndex = Mathf.Clamp(_createTypeIndex, 0, Mathf.Max(0, _availableCreateTypes.Length - 1));
                if (_availableCreateTypes.Length > 0) _createTypeIndex = EditorGUILayout.Popup(_createTypeIndex, _creationLabels, GUILayout.Width(_listWidth - 18f));
                else GUILayout.Label("이 분류에서 생성할 구체 SO 타입이 없습니다.", EditorStyles.wordWrappedMiniLabel);
                _newName = EditorGUILayout.TextField("에셋 이름", _newName);
                if (GUILayout.Button("새 SO 생성", GUILayout.Height(25f))) CreateAsset();
            }
        }
    }

    private static string CreationLabel(Type type)
    {
        var category = Classify(type);
        return CategoryPath(category.family, category.detail) + " — " + type.Name;
    }

    private void DrawWorkbench()
    {
        if (_selected == null)
        {
            EditorGUILayout.HelpBox("왼쪽에서 SO를 선택하세요. Project의 SO를 아래 칸에 드래그해서 바로 불러올 수도 있습니다.", MessageType.Info);
            DrawLoadField();
            return;
        }
        DrawLoadField();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(_selected.name, EditorStyles.boldLabel);
            if (GUILayout.Button("Project", GUILayout.Width(60f))) { Selection.activeObject = _selected; EditorGUIUtility.PingObject(_selected); }
            using (new EditorGUI.DisabledScope(!CanWrite))
            {
                if (GUILayout.Button("저장 · Ctrl+S", GUILayout.Width(112f))) SaveSelected();
                if (GUILayout.Button("복사", GUILayout.Width(52f))) Duplicate();
                if (GUILayout.Button("삭제", GUILayout.Width(52f))) Delete();
            }
        }
        EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(_selected), EditorStyles.miniLabel, GUILayout.Height(18f));
        var category = Classify(_selected.GetType());
        EditorGUILayout.LabelField(CategoryPath(category.family, category.detail) + " · " + _selected.GetType().Name, EditorStyles.miniBoldLabel);
        using (new EditorGUI.DisabledScope(!CanWrite))
        using (new EditorGUILayout.HorizontalScope())
        {
            _rename = EditorGUILayout.TextField("에셋 이름", _rename);
            if (GUILayout.Button("이름 변경", GUILayout.Width(80f))) Rename();
        }
        GUILayout.Label(EditorUtility.IsDirty(_selected) ? "● 수정됨 — 저장하면 디스크에 반영됩니다. Ctrl+Z로 실행 취소 가능." : "저장된 SO 데이터", EditorStyles.miniLabel);
        _inspectorTab = GUILayout.Toolbar(_inspectorTab, new[] { "간편 편집", "전체 SO 필드" });
        _editScroll = EditorGUILayout.BeginScrollView(_editScroll);
        using (new EditorGUI.DisabledScope(!CanWrite))
        {
            _inspector ??= new CharacterStatusMakerInspector(_selected);
            if(_selected is PassiveModuleRulesSO)
                EditorGUILayout.HelpBox("레이드 패시브 · 전투/생존 슬롯 · HOME 시작 추첨 · 통합 모듈의 실제 설정입니다. 고등급 추첨은 데모 기본 OFF이며 슬롯 레벨·비용·확률은 임시값입니다.",MessageType.Info);
            if(SystemDomainInspector.Supports(_selected))SystemDomainInspector.Draw(_selected);
            else _inspector.Draw(_inspectorTab == 0);
        }
        EditorGUILayout.Space(10f);
        DrawReferences();
        EditorGUILayout.EndScrollView();
    }

    private void DrawLoadField()
    {
        var loaded = EditorGUILayout.ObjectField("SO 불러오기", _selected, typeof(ScriptableObject), false) as ScriptableObject;
        if (loaded == _selected) return;
        if (loaded == null) Select(null);
        else if (Supports(loaded.GetType()) && AssetDatabase.Contains(loaded)) { RevealAsset(loaded); GUIUtility.ExitGUI(); }
        else ShowNotification(new GUIContent("캐릭터 스탯·상태이상·무적·지원 설정 SO 에셋을 넣어주세요."));
    }

    private void DrawSplitter(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        int id = GUIUtility.GetControlID("CharacterStatusMakerSplitter".GetHashCode(), FocusType.Passive);
        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
        { _splitControl = id; GUIUtility.hotControl = id; evt.Use(); }
        else if (evt.type == EventType.MouseDrag && GUIUtility.hotControl == id && _splitControl == id)
        { _listWidth = Mathf.Clamp(_listWidth + evt.delta.x, 220f, Mathf.Max(220f, position.width - 430f)); evt.Use(); Repaint(); }
        else if (evt.type == EventType.MouseUp && GUIUtility.hotControl == id)
        { GUIUtility.hotControl = 0; _splitControl = 0; EditorPrefs.SetFloat(PreferenceKey, _listWidth); evt.Use(); }
    }

    private void HandleKeyboard()
    {
        Event evt = Event.current;
        if (evt.type != EventType.KeyDown || _selected == null) return;
        if (CanWrite && (evt.control || evt.command) && evt.keyCode == KeyCode.S) { SaveSelected(); evt.Use(); }
        if (EditorGUIUtility.editingTextField) return;
        if (CanWrite && evt.keyCode == KeyCode.Delete) { evt.Use(); Delete(); }
        else if (CanWrite && (evt.control || evt.command) && evt.keyCode == KeyCode.D) { evt.Use(); Duplicate(); }
        else if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.DownArrow)
        {
            int index = _visible.FindIndex(entry => entry.asset == _selected);
            int next = Mathf.Clamp(index + (evt.keyCode == KeyCode.DownArrow ? 1 : -1), 0, _visible.Count - 1);
            if (_visible.Count > 0) Select(_visible[next].asset);
            evt.Use();
        }
    }

    private void SaveSelected()
    {
        if (!CanWrite || _selected == null) return;
        AssetDatabase.SaveAssetIfDirty(_selected);
        ShowNotification(new GUIContent("선택 SO 저장 완료"));
    }
    private static string CleanName(string value)
    {
        string name = (value ?? "").Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
        return name.Trim('.');
    }
    private void Rename()
    {
        string name = CleanName(_rename);
        if (string.IsNullOrWhiteSpace(name) || _selected == null || !CanWrite) return;
        string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(_selected), name);
        if (!string.IsNullOrEmpty(error)) EditorUtility.DisplayDialog("이름 변경 실패", error, "확인");
        _rename = _selected.name;
        RefreshLibrary();
    }

    private static void NewIdentity(ScriptableObject asset)
    {
        var data = new SerializedObject(asset);
        foreach (string path in new[] { "_id", "_enemy._id", "_sourceId" })
        {
            var property = data.FindProperty(path);
            if (property != null && property.propertyType == SerializedPropertyType.String)
                property.stringValue = CleanName(asset.name) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private void CreateAsset()
    {
        if (!CanWrite || _availableCreateTypes.Length == 0) return;
        Type type = _availableCreateTypes[_createTypeIndex];
        string name = CleanName(_newName);
        if (string.IsNullOrWhiteSpace(name)) return;
        string path = EditorUtility.SaveFilePanelInProject("새 SO 저장", name, "asset", "새 SO를 저장할 위치를 선택하세요.", "Assets/ScriptableObject/GameScene");
        if (string.IsNullOrEmpty(path)) return;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
        { EditorUtility.DisplayDialog("생성 취소", "기존 에셋을 덮어쓰지 않습니다. 다른 이름을 선택하세요.", "확인"); return; }
        var asset = CreateInstance(type);
        asset.name = Path.GetFileNameWithoutExtension(path);
        NewIdentity(asset);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssetIfDirty(asset);
        RefreshLibrary(); RevealAsset(asset);
        GUIUtility.ExitGUI();
    }
    private void Duplicate()
    {
        if (!CanWrite || _selected == null) return;
        string source = AssetDatabase.GetAssetPath(_selected);
        string path = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(source).Replace('\\', '/') + "/" + _selected.name + " Copy.asset");
        // Clone the current authored data, including unsaved edits, without saving unrelated assets.
        var asset = Instantiate(_selected);
        asset.name = Path.GetFileNameWithoutExtension(path);
        NewIdentity(asset);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssetIfDirty(asset);
        RefreshLibrary(); RevealAsset(asset);
        ShowNotification(new GUIContent("복사 완료 — 새 GUID/ID, 연결된 SO 참조는 유지"));
        GUIUtility.ExitGUI();
    }
    private void Delete()
    {
        if (!CanWrite || _selected == null) return;
        string path = AssetDatabase.GetAssetPath(_selected);
        if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) || AssetDatabase.IsSubAsset(_selected)) return;
        string references = _referencesChecked ? $"찾은 직접 참조: {_references.Count}개" : "참조 검사를 아직 실행하지 않았습니다.";
        if (!EditorUtility.DisplayDialog("SO 삭제 · 휴지통",
            path + "\n\n" + references + "\n삭제하면 이 SO를 사용하는 프리팹/씬/다른 SO의 연결이 Missing이 됩니다.\nOS 휴지통으로 이동하며, 복원 시 .meta 파일도 함께 복원해야 합니다.", "휴지통으로 이동", "취소")) return;
        if (!AssetDatabase.MoveAssetToTrash(path))
        { EditorUtility.DisplayDialog("삭제 실패", "에셋을 휴지통으로 이동하지 못했습니다.", "확인"); return; }
        Select(null); RefreshLibrary();
        ShowNotification(new GUIContent("SO와 .meta를 휴지통으로 이동했습니다."));
        GUIUtility.ExitGUI();
    }

    private void DrawReferences()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("이 SO를 사용하는 에셋", EditorStyles.boldLabel);
            if (GUILayout.Button("직접 참조 찾기 · 요청할 때만 검색")) FindReferences();
            if (!_referencesChecked) return;
            GUILayout.Label($"직접 에셋 참조 {_references.Count}개 · 문자열/Addressables 키 및 런타임 코드 연결은 제외", EditorStyles.wordWrappedMiniLabel);
            _referenceScroll = EditorGUILayout.BeginScrollView(_referenceScroll, GUILayout.MaxHeight(160f));
            foreach (string path in _references)
                if (GUILayout.Button(path, EditorStyles.linkLabel)) EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(path));
            EditorGUILayout.EndScrollView();
        }
    }
    private void FindReferences()
    {
        if (_selected == null) return;
        string target = AssetDatabase.GetAssetPath(_selected);
        string[] candidates = AssetDatabase.FindAssets("t:Prefab t:Scene t:ScriptableObject", new[] { "Assets" });
        _references.Clear();
        bool complete = true;
        try
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(candidates[i]);
                if (i % 25 == 0 && EditorUtility.DisplayCancelableProgressBar("직접 참조 검색", path, i / (float)Mathf.Max(1, candidates.Length)))
                { complete = false; break; }
                if (path != target && AssetDatabase.GetDependencies(path, false).Contains(target)) _references.Add(path);
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        _references.Sort(StringComparer.Ordinal);
        _referencesChecked = complete;
        if (!complete) ShowNotification(new GUIContent("참조 검색 취소 — 결과를 완전한 목록으로 취급하지 않습니다."));
    }
}
