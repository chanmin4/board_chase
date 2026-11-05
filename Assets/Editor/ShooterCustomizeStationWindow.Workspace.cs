using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>대상 → 프리셋 → 미리보기/편집 → 저장. 원본 외형 편집기는 별도 partial에 유지한다.</summary>
public sealed partial class ShooterCustomizeStationWindow
{
    [Serializable] private sealed class PresetRow
    {
        public ShooterCustomizeSO look;
        public float weight = 1;
    }
    private static readonly IComparer<string> LookNameComparer = Comparer<string>.Create(EditorUtility.NaturalCompare);
    [SerializeField] private string _targetPath;
    [SerializeField] private ShooterSpawnLookMode _spawnMode;
    [SerializeField] private List<PresetRow> _presets = new();
    [SerializeField] private bool _bindingDirty;
    [SerializeField] private float _catalogWidth = 240, _workbenchWidth = 360;
    [SerializeField] private List<string> _collapsedGroups = new();
    [SerializeField] private bool _showGuide = true;
    [SerializeField] private bool _showTargetDetails;
    [SerializeField] private bool _catalogLayoutInitialized;
    private string _targetSearch = "", _renameLook = "";
    private MakerSelectionUiState _lookNameUi;
    private MakerSelectionUiState LookNameUi => _lookNameUi ??= new MakerSelectionUiState();
    private Vector2 _targetScroll, _presetScroll;
    private List<ShooterCustomizeCatalog.Target> _targets = new();
    private Action _pendingUi;
    private ShooterCustomizeSO _importLook;
    private readonly Dictionary<ShooterCustomizeSO, Texture2D> _thumbnails = new();
    private ShooterCustomizePreview _thumbnailPreview;
    private double _lastThumbnailTime;
    private Vector2 _workspaceScreenOrigin;
    // Internal hooks for the separate verification window; no project mutation is required to click UI.
    public Action<string, Rect> ObserveControl;
    public Action<Texture> ObservePreview;
    private ShooterCustomizeCatalog.Target CurrentTarget => _targets.FirstOrDefault(t => t.Path == _targetPath);

    private void Queue(Action action) { if (_pendingUi == null) _pendingUi = action; }
    private void Observe(string key)
    {
        if (Event.current.type != EventType.Repaint) return;
        Rect rect = GUILayoutUtility.GetLastRect();
        rect.position = GUIUtility.GUIToScreenPoint(rect.position) - _workspaceScreenOrigin;
        ObserveControl?.Invoke(key, rect);
    }
    private void RefreshTargets()
    {
        _targets = ShooterCustomizeCatalog.Discover();
        if (!_catalogLayoutInitialized)
        {
            _collapsedGroups = _targets.Select(t => t.Group).Distinct().Where(g => g != "Player").ToList();
            _catalogLayoutInitialized = true;
        }
        Repaint();
    }

    private void DrawWorkspace()
    {
        _workspaceScreenOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("SHOOTER CUSTOMIZE", EditorStyles.boldLabel);
                GUILayout.Label("1 몹 선택  →  2 외형 추가·등장 확률  →  3 꾸미고 적용", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("사용 가이드", GUILayout.Width(85))) _showGuide = !_showGuide;
                using (new EditorGUI.DisabledScope(_pendingUi != null))
                    if (GUILayout.Button("목록 새로고침", GUILayout.Width(100))) Queue(RefreshTargets);
                using (new EditorGUI.DisabledScope(CurrentTarget == null || _pendingUi != null))
                {
                    if (GUILayout.Button("다시 불러오기", GUILayout.Width(100))) Queue(() => { if (ConfirmLeave()) LoadTarget(CurrentTarget); });
                    if (GUILayout.Button("저장·스폰에 적용" + (Dirty ? " *" : ""), GUILayout.Width(145), GUILayout.Height(27)))
                        Queue(() => { if (SaveAll()) ShowNotification(new GUIContent("외형과 스폰 연결을 저장했습니다.")); });
                }
            }
            if (_showGuide)
                GUILayout.Label("몹 하나 = 전투 Config 하나. 외형만 여러 개 추가하세요. 예: Scraper → 모자 50 / 방독면 30 / 맨얼굴 20 → 스폰마다 하나 선택. " +
                    "외형을 추가해도 능력치·AI·장비는 달라지지 않습니다. Player·NPC·Boss는 고정 외형 하나를 사용합니다.",
                    EditorStyles.wordWrappedMiniLabel);
        }
        float top = _showGuide ? 82 : 44;
        float height = Mathf.Max(550, position.height - top - 25);
        _catalogWidth = Mathf.Clamp(_catalogWidth, 190, Mathf.Min(380, position.width - 750));
        _workbenchWidth = Mathf.Clamp(_workbenchWidth, 330, Mathf.Min(520, position.width - _catalogWidth - 430));
        float centerX = _catalogWidth + 9;
        float centerWidth = position.width - centerX - _workbenchWidth - 13;
        GUILayout.BeginArea(new Rect(3, top, _catalogWidth, height));
        DrawTargetCatalog(height);
        GUILayout.EndArea();
        GUILayout.BeginArea(new Rect(centerX, top, centerWidth, height));
        float presetHeight = _showTargetDetails ? 344 : 288;
        DrawPresetStrip(centerWidth, presetHeight);
        DrawViewport(centerWidth, height - presetHeight - 2);
        GUILayout.EndArea();
        GUILayout.BeginArea(new Rect(position.width - _workbenchWidth - 3, top, _workbenchWidth, height));
        DrawPartSelector();
        using (new EditorGUI.DisabledScope(CurrentTarget == null)) DrawEditor(_workbenchWidth, height - 114);
        GUILayout.EndArea();
        DrawWorkspaceSplitter(new Rect(_catalogWidth + 3, top, 6, height), true);
        DrawWorkspaceSplitter(new Rect(position.width - _workbenchWidth - 9, top, 6, height), false);
        GUI.Label(new Rect(7, position.height - 22, position.width - 14, 20),
            Dirty ? "● 미저장 변경 있음 — [저장·스폰에 적용]을 누르기 전에는 스폰 연결이 바뀌지 않습니다." :
                "외형 전용 도구  |  카드 클릭: 편집  |  미리보기 드래그: 회전  |  휠: 확대  |  패널 경계 드래그: 너비 조절",
            EditorStyles.miniLabel);
    }

    private void DrawTargetCatalog(float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(height)))
        {
            GUILayout.Label("1. 꾸밀 캐릭터 / 몹", EditorStyles.boldLabel);
            _targetSearch = EditorGUILayout.TextField(_targetSearch, EditorStyles.toolbarSearchField);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("모두 펼치기")) _collapsedGroups.Clear();
                if (GUILayout.Button("모두 접기")) _collapsedGroups = _targets.Select(t => t.Group).Distinct().ToList();
            }
            _targetScroll = EditorGUILayout.BeginScrollView(_targetScroll);
            foreach (var group in _targets.Where(t => string.IsNullOrWhiteSpace(_targetSearch) ||
                (t.DisplayName + " " + t.Name + " " + t.Group).IndexOf(_targetSearch, StringComparison.OrdinalIgnoreCase) >= 0).GroupBy(t => t.Group))
            {
                // Search reveals matching targets without forcing users to open every family first.
                bool open = !string.IsNullOrWhiteSpace(_targetSearch) || !_collapsedGroups.Contains(group.Key);
                bool next = EditorGUILayout.Foldout(open, group.Key + "  (" + group.Count() + ")", true, EditorStyles.foldoutHeader);
                Observe("group:" + group.Key);
                if (next != open) { if (next) _collapsedGroups.Remove(group.Key); else _collapsedGroups.Add(group.Key); }
                if (!next) continue;
                foreach (var target in group)
                {
                    Color color = GUI.backgroundColor;
                    if (target.Path == _targetPath) GUI.backgroundColor = new Color(.25f, .72f, .86f);
                    string badge = !target.HasVisualRig ? "외형 연결 확인 필요" : SavedLookSummary(target);
                    if (GUILayout.Button(new GUIContent(target.DisplayName + "\n" + badge, target.Path),
                        new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, wordWrap = true }, GUILayout.Height(44)))
                        Queue(() => { if (ConfirmLeave()) LoadTarget(target); });
                    Observe("target:" + target.Name);
                    GUI.backgroundColor = color;
                }
                GUILayout.Space(6);
            }
            EditorGUILayout.EndScrollView();
            GUILayout.Label("외형을 늘릴 때는 Config를 만들지 말고\n가운데 [외형 추가]를 누르세요.", EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void DrawPresetStrip(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(height)))
        {
            var target = CurrentTarget;
            GUILayout.Label("2. " + (target != null ? target.DisplayName + " — 스폰 가능한 외형" : "왼쪽에서 캐릭터를 선택하세요"), EditorStyles.boldLabel);
            if (target == null)
            {
                EditorGUILayout.HelpBox("먼저 캐릭터를 선택하면 프리셋을 만들고 실제 스폰에 연결할 수 있습니다. 기존 외형은 자동 변경되지 않습니다.", MessageType.Info);
                GUILayout.FlexibleSpace(); return;
            }
            if (!target.HasVisualRig)
            {
                EditorGUILayout.HelpBox("실제 캐릭터 프리팹에 외형 연결(ShooterVisualRig)이 없습니다. 아래 미리보기만으로는 게임에 적용되지 않습니다. 공통 Visual_Shooter 연결을 먼저 복구해야 합니다.", MessageType.Warning);
                if (GUILayout.Button("Config 선택")) Selection.activeObject = target.Config;
                GUILayout.FlexibleSpace(); return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                string[] labels = target.AllowsRandom ? new[] { "기본 모습", "한 외형 고정", "여러 외형 랜덤" } :
                    new[] { "기본 모습", "한 외형 고정" };
                int mode = GUILayout.Toolbar(Mathf.Min((int)_spawnMode, labels.Length - 1), labels, GUILayout.Height(26));
                if (mode != (int)_spawnMode) Queue(() => ChangeMode((ShooterSpawnLookMode)mode));
                Observe("appearance-mode");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("외형 " + _presets.Count + "개 · 능력치 / AI는 공통", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(_presets.Count == 0))
                    if (GUILayout.Button("한 마리 뽑아보기", GUILayout.Width(112))) Queue(RandomPreview);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!target.AllowsRandom && _presets.Count > 0))
                    if (GUILayout.Button("+ 외형 추가")) Queue(() => CreatePreset(false));
                using (new EditorGUI.DisabledScope(_draft.target == null || !target.AllowsRandom && _presets.Count > 0))
                    if (GUILayout.Button("선택 외형 복제")) Queue(() => CreatePreset(true));
                if (GUILayout.Button("저장된 외형에서 추가…")) ShowSavedLooksMenu();
            }
            string error = BindingError();
            GUILayout.Label(error ?? (_spawnMode == ShooterSpawnLookMode.RandomPool ?
                "이 몹이 스폰될 때마다 아래 외형 중 하나를 선택합니다. 비중 0 = 등장 안 함." :
                _spawnMode == ShooterSpawnLookMode.Fixed ? "항상 아래 외형 사용 · 외형을 바꾸려면 편집하거나 다른 프리셋을 복사하세요" :
                "현재는 기존 프리팹 외형을 유지합니다. 프리셋을 추가하면 적용 모드로 바뀝니다."), EditorStyles.wordWrappedMiniLabel);
            _presetScroll = EditorGUILayout.BeginScrollView(_presetScroll, GUILayout.Height(130));
            using (new EditorGUILayout.HorizontalScope())
            {
                double total = _presets.Sum(p => EffectiveWeight(p));
                foreach (var row in SortedPresetRows(_presets)) DrawPresetCard(row, total);
                if (_presets.Count == 0) GUILayout.Label("① [외형 추가] → 오른쪽에서 꾸미기\n② [선택 외형 복제]로 다른 모습 추가\n③ 등장 비중을 정하고 [저장·스폰에 적용]", EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
            _showTargetDetails = EditorGUILayout.Foldout(_showTargetDetails, "고급: 전투 Config / 프리팹 확인", true);
            if (_showTargetDetails)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("전투 설정", target.Config != null ? (UnityEngine.Object)target.Config : target.Prefab, typeof(UnityEngine.Object), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _importLook = (ShooterCustomizeSO)EditorGUILayout.ObjectField(_importLook, typeof(ShooterCustomizeSO), false);
                    using (new EditorGUI.DisabledScope(_importLook == null))
                        if (GUILayout.Button("복사해서 추가", GUILayout.Width(100))) Queue(ImportPreset);
                }
            }
        }
    }

    private void ShowSavedLooksMenu()
    {
        var menu = new GenericMenu();
        var looks = AssetDatabase.FindAssets("t:ShooterCustomizeSO").Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !p.Contains("/WIP/") && !p.Contains("/__"))
            .Select(AssetDatabase.LoadAssetAtPath<ShooterCustomizeSO>).Where(l => l != null).OrderBy(l => l.name, LookNameComparer).ToList();
        foreach (var look in looks)
        {
            string prefix = ShooterCustomizeCatalog.IsOwned(CurrentTarget, look) ? "이 몹의 저장된 외형/" : "다른 외형 복사/";
            menu.AddItem(new GUIContent(prefix + look.name), false, () => Queue(() => { _importLook = look; ImportPreset(); }));
        }
        if (looks.Count == 0) menu.AddDisabledItem(new GUIContent("저장된 외형 없음 — 외형 추가로 시작하세요"));
        menu.ShowAsContext();
    }

    private static string SavedLookSummary(ShooterCustomizeCatalog.Target target)
    {
        if (target.Config == null) return target.CharacterLook != null ? "고정 외형 1개" : "기본 모습";
        return target.Config.SpawnLookMode switch
        {
            ShooterSpawnLookMode.RandomPool => "랜덤 외형 " + (target.Config.SpawnLookPool?.Customizes.Count ?? 0) + "개",
            ShooterSpawnLookMode.Fixed => "고정 외형 1개",
            _ => "기본 모습 · 외형 추가 가능"
        };
    }

    private static double EffectiveWeight(PresetRow row) => row.look == null || float.IsNaN(row.weight) || float.IsInfinity(row.weight) ? 0 : Math.Max(0, row.weight);

    // Sort the display only. Keep each original row/weight and the saved spawn pool order intact.
    private static PresetRow[] SortedPresetRows(IEnumerable<PresetRow> rows) => rows
        .OrderBy(row => row.look == null)
        .ThenBy(row => row.look != null ? row.look.name : "", LookNameComparer).ToArray();

    private void DrawPresetCard(PresetRow row, double total)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(126)))
        {
            Color color = GUI.backgroundColor;
            if (row.look == _draft.target) GUI.backgroundColor = new Color(.25f, .72f, .86f);
            Texture thumbnail = GetThumbnail(row.look);
            if (GUILayout.Button(new GUIContent(row.look != null ? row.look.name : "Missing", thumbnail,
                row.look != null ? AssetDatabase.GetAssetPath(row.look) : "누락된 프리셋 — 목록에서 제거하세요"),
                new GUIStyle(GUI.skin.button) { imagePosition = ImagePosition.ImageAbove, wordWrap = true, fontSize = 10 },
                GUILayout.Width(126), GUILayout.Height(80))) Queue(() => SelectPreset(row.look));
            Observe("preset:" + (row.look != null ? row.look.name : "Missing"));
            GUI.backgroundColor = color;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_spawnMode == ShooterSpawnLookMode.RandomPool)
                {
                    GUILayout.Label(new GUIContent("비중", "예: 50 / 30 / 20이면 각각 50%, 30%, 20%로 등장합니다. 0은 제외됩니다."), GUILayout.Width(25));
                    float weight = EditorGUILayout.FloatField(row.weight, GUILayout.Width(34));
                    if (weight != row.weight) { row.weight = float.IsFinite(weight) ? Mathf.Max(0, weight) : 0; _bindingDirty = true; SyncDirty(); }
                    double percent = total > 0 ? EffectiveWeight(row) / total * 100 : 0;
                    GUILayout.Label(percent > 0 && percent < .01 ? "<0.01%" : percent.ToString("0.##") + "%", EditorStyles.miniLabel);
                }
                else GUILayout.Label(_spawnMode == ShooterSpawnLookMode.Fixed ? "고정 · 100%" : "기존 외형", EditorStyles.centeredGreyMiniLabel);
                if (GUILayout.Button(new GUIContent("×", "이 대상의 목록에서만 제거. 프리셋 파일은 유지합니다."), GUILayout.Width(20)))
                    Queue(() => RemovePresetRow(row));
            }
        }
    }

    private void DrawPartSelector()
    {
        GUILayout.Label("3. 선택한 외형 꾸미기", EditorStyles.boldLabel);
        string[] labels = Parts.Select(p => PartLabel(p) + (_draft.attachments.Count(a => a.socket == p) > 0 ?
            " (" + _draft.attachments.Count(a => a.socket == p) + ")" : "") + (HasEquipmentAtPart(p) ? " ·장비" : "")).Concat(new[] { "장비 테스트" }).ToArray();
        int index = GUILayout.SelectionGrid(_equipmentMode ? Parts.Length : _part, labels, 3,
            new GUIStyle(GUI.skin.button) { fontSize = 10 }, GUILayout.Height(72));
        if (index != (_equipmentMode ? Parts.Length : _part))
        { _equipmentMode = index == Parts.Length; if (!_equipmentMode) _part = index; _selectedFittingGear = ShooterVisualEquipmentKind.None; _attachment = -1; _editScroll = Vector2.zero; }
        Observe("parts");
        using (new EditorGUILayout.HorizontalScope())
        {
            _renameLook = LookNameUi.NameFor(_draft.target, _renameLook);
            GUI.SetNextControlName("ShooterCustomize.LookName");
            _renameLook = EditorGUILayout.TextField(_renameLook);
            Observe("look-name");
            using (new EditorGUI.DisabledScope(_draft.target == null))
            {
                if (GUILayout.Button("이름 변경", GUILayout.Width(70))) Queue(RenamePreset);
                if (GUILayout.Button("목록에서 제거", GUILayout.Width(96))) Queue(RemovePreset);
            }
        }
    }

    private void LoadTarget(ShooterCustomizeCatalog.Target target)
    {
        if (target == null) return;
        _targetPath = target.Path; _presets.Clear(); _bindingDirty = false; _renameLook = ""; _error = null;
        _spawnMode = target.Config != null ? target.Config.SpawnLookMode :
            target.CharacterLook != null ? ShooterSpawnLookMode.Fixed : ShooterSpawnLookMode.ExistingPrefab;
        if (_spawnMode == ShooterSpawnLookMode.RandomPool && target.Config?.SpawnLookPool != null)
        {
            var pool = target.Config.SpawnLookPool;
            for (int i = 0; i < pool.Customizes.Count; i++) _presets.Add(new PresetRow { look = pool.Customizes[i], weight = pool.WeightAt(i) });
        }
        else
        {
            var look = _spawnMode == ShooterSpawnLookMode.Fixed ?
                target.Config != null ? target.Config.FixedSpawnLook : target.CharacterLook : target.PrefabLook;
            if (look != null) _presets.Add(new PresetRow { look = look });
        }
        ClearThumbnails();
        if (target.Visual != null) LoadSource(target.Visual, _presets.FirstOrDefault()?.look);
        else { _preview?.Dispose(); _preview = null; _draft.target = null; _draft.lookDirty = false; }
        SyncDirty();
    }

    private bool ConfirmLookSwitch()
    {
        if (!_draft.lookDirty && !_draft.HasEquipmentEdits) return true;
        int choice = EditorUtility.DisplayDialogComplex("외형 / 장비에 미저장 변경이 있어요", "현재 외형과 장비 Item SO 배치를 저장할까요? 장비 배치는 같은 아이템을 쓰는 모든 캐릭터에 적용됩니다. 스폰 연결은 상단 저장 버튼으로 적용합니다.", "저장", "취소", "변경 버리기");
        if (choice == 1) return false;
        if (choice == 0)
        {
            if (_draft.lookDirty && !SaveLook(false)) return false;
            _draft.SaveEquipmentEdits();
        }
        else { _draft.lookDirty = false; _draft.ReloadEquipmentEdits(); }
        return true;
    }

    private void SelectPreset(ShooterCustomizeSO look)
    {
        if (look == null || CurrentTarget == null || !ConfirmLookSwitch()) return;
        LoadSource(CurrentTarget.Visual, look); _renameLook = look.name;
    }

    private void CreatePreset(bool copy)
    {
        if (CurrentTarget?.Visual == null || !ConfirmLookSwitch()) return;
        var seed = CreateInstance<ShooterCustomizeDraft>();
        ShooterCustomizePreview baseline = null;
        try
        {
            // A new preset starts from authored geometry, not the currently edited look's proportions.
            if (!copy) { baseline = new ShooterCustomizePreview(); baseline.Build(CurrentTarget.Visual); }
            seed.LoadLook(CurrentTarget.Visual, copy ? _draft.target : CurrentTarget.PrefabLook, baseline?.Rig ?? _preview.Rig);
            var look = ShooterCustomizeCatalog.CreateLook(CurrentTarget, seed, copy ? _draft.target.name + "_Copy" : CurrentTarget.DisplayName + "_Look");
            AddWorkingLook(look);
        }
        finally { baseline?.Dispose(); DestroyImmediate(seed); }
    }

    private void ImportPreset()
    {
        if (_importLook == null || CurrentTarget?.Visual == null || !ConfirmLookSwitch()) return;
        if (!CurrentTarget.AllowsRandom && _presets.Count > 0 && !EditorUtility.DisplayDialog("고정 외형 교체", "현재 외형 대신 선택한 프리셋의 독립 복사본을 사용합니다. 기존 외형 에셋은 삭제하지 않습니다.", "복사·교체", "취소")) return;
        var seed = CreateInstance<ShooterCustomizeDraft>();
        try
        {
            seed.LoadLook(CurrentTarget.Visual, _importLook, _preview.Rig);
            AddWorkingLook(ShooterCustomizeCatalog.CreateLook(CurrentTarget, seed, _importLook.name + "_Copy"));
            _importLook = null;
        }
        finally { DestroyImmediate(seed); }
    }

    private void AddWorkingLook(ShooterCustomizeSO look)
    {
        if (!CurrentTarget.AllowsRandom) _presets.Clear();
        _presets.Add(new PresetRow { look = look });
        _spawnMode = _presets.Count > 1 ? ShooterSpawnLookMode.RandomPool : ShooterSpawnLookMode.Fixed;
        _bindingDirty = true;
        LoadSource(CurrentTarget.Visual, look); _renameLook = look.name;
    }

    private void ReplaceWorkingLook(ShooterCustomizeSO oldLook, ShooterCustomizeSO newLook)
    {
        var row = _presets.FirstOrDefault(p => p.look == oldLook);
        if (row != null) row.look = newLook; else _presets.Add(new PresetRow { look = newLook });
        if (_spawnMode == ShooterSpawnLookMode.ExistingPrefab) _spawnMode = _presets.Count > 1 ? ShooterSpawnLookMode.RandomPool : ShooterSpawnLookMode.Fixed;
        _bindingDirty = true; _renameLook = newLook.name;
    }

    private void ChangeMode(ShooterSpawnLookMode mode)
    {
        if (!ConfirmLookSwitch()) return;
        if (mode == ShooterSpawnLookMode.Fixed && _presets.Count > 1)
        {
            if (!EditorUtility.DisplayDialog("고정 외형으로 변경", "현재 선택한 외형 하나만 스폰에 사용합니다. 나머지 외형 에셋은 삭제하지 않습니다.", "선택 외형만 사용", "취소")) return;
            var selected = _presets.FirstOrDefault(p => p.look == _draft.target) ?? _presets[0];
            _presets.Clear(); _presets.Add(selected);
        }
        _spawnMode = mode; _bindingDirty = true; SyncDirty();
    }

    private string BindingError() => ShooterCustomizeCatalog.Validate(CurrentTarget, _spawnMode,
        _presets.Select(p => p.look).ToList(), _presets.Select(p => p.weight).ToList());

    private bool SaveTargetBinding()
    {
        try
        {
            ShooterCustomizeCatalog.SaveBinding(CurrentTarget, _spawnMode,
                _presets.Select(p => p.look).ToList(), _presets.Select(p => p.weight).ToList());
            _bindingDirty = false; return true;
        }
        catch (Exception e) { _error = e.Message; return false; }
    }

    private void RemovePreset()
    {
        var row = _presets.FirstOrDefault(p => p.look == _draft.target);
        if (row != null) RemovePresetRow(row);
    }

    private void RemovePresetRow(PresetRow row)
    {
        if (!ConfirmLookSwitch()) return;
        if (!EditorUtility.DisplayDialog("스폰 목록에서 제거", "이 대상의 스폰 목록에서만 제거합니다. 프리셋 파일과 다른 대상의 연결은 삭제하지 않습니다. 상단 [저장·스폰에 적용]으로 확정하세요.", "목록에서 제거", "취소")) return;
        _presets.Remove(row);
        if (_presets.Count == 0) _spawnMode = ShooterSpawnLookMode.ExistingPrefab;
        _bindingDirty = true; LoadSource(CurrentTarget.Visual, _presets.FirstOrDefault()?.look); _renameLook = "";
    }

    private void RenamePreset()
    {
        if (_draft.target == null || string.IsNullOrWhiteSpace(_renameLook)) return;
        string requested = _renameLook.Trim();
        if (ShooterCustomizeCatalog.NeedsIndependentCopy(CurrentTarget, _draft.target) && !SaveLook(false)) return;
        string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(_draft.target), ShooterCustomizeCatalog.SafeName(requested));
        _renameLook = _draft.target.name;
        LookNameUi.SelectionClicked();
        if (!string.IsNullOrEmpty(error)) _error = error;
    }

    private void RandomPreview()
    {
        if (!ConfirmLookSwitch()) return;
        if (_spawnMode != ShooterSpawnLookMode.RandomPool) { SelectPreset(_presets.FirstOrDefault()?.look); return; }
        var pool = CreateInstance<ShooterCustomizePoolSO>();
        try
        {
            pool.SetEntries(_presets.Select(p => p.look).ToList(), _presets.Select(p => p.weight).ToList());
            var look = pool.GetRandom();
            if (look != null) { SelectPreset(look); ShowNotification(new GUIContent("미리보기 추첨: " + look.name)); }
        }
        finally { DestroyImmediate(pool); }
    }

    private void SelectDroppedTarget(GameObject prefab)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        var matches = _targets.Where(t => AssetDatabase.GetAssetPath(t.Prefab) == path).ToList();
        if (matches.Count == 1) { if (ConfirmLeave()) LoadTarget(matches[0]); }
        else if (matches.Count > 1)
        {
            _targetSearch = matches[0].Name.Split(new[] { "Shooter", "Config" }, StringSplitOptions.None)[0];
            _collapsedGroups.Remove(matches[0].Group);
            ShowNotification(new GUIContent("이 모델을 공유하는 서로 다른 몹이 있습니다. 왼쪽에서 꾸밀 몹을 선택해 주세요."));
        }
        else ShowNotification(new GUIContent("캐릭터 Config/Player/NPC에 연결된 프리팹을 선택해 주세요. Visual_Shooter는 공통 외형 원본입니다."));
    }

    private void DrawWorkspaceSplitter(Rect rect, bool left)
    {
        EditorGUI.DrawRect(rect, new Color(.12f, .12f, .12f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        if (Event.current.type == EventType.Repaint) ObserveControl?.Invoke(left ? "split:left" : "split:right", rect);
        int control = GUIUtility.GetControlID(left ? 8031 : 8032, FocusType.Passive, rect);
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition)) { GUIUtility.hotControl = control; e.Use(); }
        if (e.type == EventType.MouseDrag && GUIUtility.hotControl == control)
        { if (left) _catalogWidth += e.delta.x; else _workbenchWidth -= e.delta.x; e.Use(); Repaint(); }
        if (e.type == EventType.MouseUp && GUIUtility.hotControl == control) { GUIUtility.hotControl = 0; e.Use(); }
    }

    private Texture GetThumbnail(ShooterCustomizeSO look)
    {
        if (look == null) return null;
        if (_thumbnails.TryGetValue(look, out var cached)) return cached;
        if (Event.current.type != EventType.Repaint || EditorApplication.timeSinceStartup - _lastThumbnailTime < .12 || CurrentTarget?.Visual == null) return null;
        _lastThumbnailTime = EditorApplication.timeSinceStartup;
        var draft = CreateInstance<ShooterCustomizeDraft>();
        try
        {
            _thumbnailPreview ??= new ShooterCustomizePreview();
            _thumbnailPreview.Build(CurrentTarget.Visual);
            draft.LoadLook(CurrentTarget.Visual, look, _thumbnailPreview.Rig);
            _thumbnailPreview.Apply(draft, false); _thumbnailPreview.Frame();
            var render = _thumbnailPreview.Render(new Rect(0, 0, 100, 70), new Vector2(160, 8), 1, _thumbnailPreview.FramingBounds.center) as RenderTexture;
            if (render == null) return null;
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = render;
                var texture = new Texture2D(render.width, render.height, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
                texture.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); texture.Apply();
                _thumbnails[look] = texture; return texture;
            }
            finally { RenderTexture.active = previous; }
        }
        catch (Exception e) { _error = "썸네일: " + e.Message; return null; }
        finally { DestroyImmediate(draft); }
    }

    private void ClearThumbnails()
    {
        foreach (var texture in _thumbnails.Values) if (texture != null) DestroyImmediate(texture);
        _thumbnails.Clear(); _thumbnailPreview?.Dispose(); _thumbnailPreview = null;
    }
}
