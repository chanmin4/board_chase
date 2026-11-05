using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Live visual-only authoring: parts / isolated character viewport / adjustments.</summary>
public sealed partial class ShooterCustomizeStationWindow : EditorWindow
{
    private const string DefaultVisualPath = "Assets/Prefabs/GameScene/Shooter/Visual_Shooter.prefab";
    private static readonly ShooterVisualSocketId[] Parts = { ShooterVisualSocketId.Body, ShooterVisualSocketId.Hair,
        ShooterVisualSocketId.Eyes, ShooterVisualSocketId.Mouth, ShooterVisualSocketId.Face, ShooterVisualSocketId.Armor,
        ShooterVisualSocketId.Feet, ShooterVisualSocketId.Back, ShooterVisualSocketId.RightHand, ShooterVisualSocketId.LeftHand };
    [SerializeField] private string _recoveryJson;
    [SerializeField] private Vector2 _orbit = new(180, 8);
    [SerializeField] private float _zoom = 1;
    [SerializeField] private int _part;
    [SerializeField] private bool _equipmentMode;
    private ShooterCustomizeDraft _draft;
    private ShooterCustomizePreview _preview;
    private GameObject _builtSource;
    private Vector2 _partScroll, _editScroll;
    private readonly Vector2[] _scaleSliderRanges = { new(.01f, 3), new(.01f, 3), new(.01f, 3) };
    private Vector3 _pan;
    private int _attachment = -1;
    private bool _advanced, _showAnchors = true, _showEquipment = true;
    private string _error;
    private bool _applyNeeded;
    private double _nextPreviewTick;
    private bool Dirty => _draft != null && (_draft.lookDirty || _draft.HasEquipmentEdits || _bindingDirty);
    private ShooterVisualSocketId Part => Parts[Mathf.Clamp(_part, 0, Parts.Length - 1)];
    private static string PartLabel(ShooterVisualSocketId part) => part == ShooterVisualSocketId.Back ? "등 / 백팩" : part.ToString();

    [MenuItem("Tools/Authoring/Shooter Customize Station")]
    public static void Open()
    {
        var window = GetWindow<ShooterCustomizeStationWindow>("Shooter Customize");
        window.minSize = new Vector2(1180, 760);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Shooter Customize");
        minSize = new Vector2(1180, 760);
        _draft = CreateInstance<ShooterCustomizeDraft>();
        _draft.hideFlags = HideFlags.HideAndDontSave;
        if (!string.IsNullOrEmpty(_recoveryJson)) EditorJsonUtility.FromJsonOverwrite(_recoveryJson, _draft);
        Undo.undoRedoPerformed += OnUndo;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (_draft.visualPrefab != null) RebuildPreview(false);
        else
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultVisualPath);
            if (source != null) LoadSource(source, null);
        }
        SyncDirty();
        RefreshTargets();
    }

    private void OnDisable()
    {
        StopPreviewNavigation();
        Undo.undoRedoPerformed -= OnUndo;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        if (_draft != null) _recoveryJson = EditorJsonUtility.ToJson(_draft);
        _preview?.Dispose();
        _preview = null;
        ClearThumbnails();
        if (_draft != null) DestroyImmediate(_draft);
        _draft = null;
    }

    private void OnPlayMode(PlayModeStateChange state)
    {
        // Never instantiate gameplay objects during a play-mode transition.
        if (state == PlayModeStateChange.ExitingEditMode) { StopPreviewNavigation(); _preview?.Dispose(); _preview = null; }
        if (state == PlayModeStateChange.EnteredEditMode) RebuildPreview(false);
        Repaint();
    }

    private void Tick()
    {
        if (_draft == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        TickPreviewNavigation();
        if (_pendingUi != null)
        {
            var action = _pendingUi; _pendingUi = null;
            try { action(); } catch (Exception e) { _error = e.Message; Debug.LogException(e); }
            SyncDirty(); Repaint();
        }
        if (_applyNeeded && _preview?.Rig != null)
        {
            _applyNeeded = false;
            try { _preview.Apply(_draft, _showEquipment); _error = null; }
            catch (Exception e) { _error = e.Message; Debug.LogException(e); }
        }
        if (EditorApplication.timeSinceStartup < _nextPreviewTick) return;
        _nextPreviewTick = EditorApplication.timeSinceStartup + .05;
        Repaint();
    }

    private void OnUndo()
    {
        if (_draft == null) return;
        if (_builtSource != _draft.visualPrefab) RebuildPreview(false);
        _applyNeeded = true;
        SyncDirty();
        Repaint();
    }

    private void SyncDirty()
    {
        hasUnsavedChanges = Dirty;
        saveChangesMessage = "외형·스폰 연결과 장비 Item SO의 배치 변경을 저장한 뒤 닫을까요? 장비 배치는 같은 아이템을 쓰는 모든 캐릭터에 적용됩니다.";
    }

    private void Edit(string label, Action change, bool equipment = false)
    {
        Undo.RecordObject(_draft, label);
        change();
        if (equipment) _draft.equipmentDirty = true; else _draft.lookDirty = true;
        _focusFittingAnchor = equipment;
        _applyNeeded = true;
        SyncDirty();
        Repaint();
    }

    private void OnGUI()
    {
        if (_draft == null) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox("Stop Play Mode to edit visuals. Your unsaved draft is kept.", MessageType.Info);
            return;
        }
        DrawWorkspace();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(28)))
        {
            GUILayout.Label("Shooter", GUILayout.Width(46));
            var source = (GameObject)EditorGUILayout.ObjectField(_draft.visualPrefab, typeof(GameObject), false, GUILayout.MinWidth(150));
            if (source != _draft.visualPrefab && ConfirmLeave()) TryLoadSource(source);
            if (GUILayout.Button("Use Selected", EditorStyles.toolbarButton, GUILayout.Width(86)) && ConfirmLeave())
                TryLoadSource(Selection.activeGameObject);
            GUILayout.Space(10);
            GUILayout.Label("Look", GUILayout.Width(32));
            var look = (ShooterCustomizeSO)EditorGUILayout.ObjectField(_draft.target, typeof(ShooterCustomizeSO), false, GUILayout.MinWidth(150));
            if (look != _draft.target && ConfirmLeave()) LoadSource(look != null && look.VisualPrefab != null ? look.VisualPrefab : _draft.visualPrefab, look);
            using (new EditorGUI.DisabledScope(_preview?.Rig == null))
            {
                if (GUILayout.Button("Save Look" + (_draft.lookDirty ? " *" : ""), EditorStyles.toolbarButton, GUILayout.Width(90))) SaveLook(false);
                if (GUILayout.Button("Save As...", EditorStyles.toolbarButton, GUILayout.Width(72))) SaveLook(true);
            }
            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(55)) && ConfirmLeave())
                LoadSource(_draft.visualPrefab, _draft.target);
        }
    }

    private void DrawParts(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height)))
        {
            GUILayout.Label("BODY PARTS", EditorStyles.boldLabel);
            _partScroll = EditorGUILayout.BeginScrollView(_partScroll);
            for (int i = 0; i < Parts.Length; i++)
            {
                int count = _draft.attachments.FindAll(a => a.socket == Parts[i]).Count;
                string label = PartLabel(Parts[i]) + (count > 0 ? $"  ({count})" : "");
                var previous = GUI.backgroundColor;
                if (!_equipmentMode && _part == i) GUI.backgroundColor = new Color(.25f, .72f, .86f);
                if (GUILayout.Button(label, GUILayout.Height(35)))
                { _part = i; _equipmentMode = false; _attachment = -1; _editScroll = Vector2.zero; _advanced = false; }
                GUI.backgroundColor = previous;
            }
            GUILayout.Space(14);
            GUILayout.Label("TRY ON EQUIPMENT", EditorStyles.miniBoldLabel);
            var color = GUI.backgroundColor;
            if (_equipmentMode) GUI.backgroundColor = new Color(.85f, .65f, .3f);
            if (GUILayout.Button("Gas Mask / Goggles", GUILayout.Height(36)))
            { _equipmentMode = true; _editScroll = Vector2.zero; _advanced = false; }
            GUI.backgroundColor = color;
            GUILayout.Space(10);
            GUILayout.Label("Select a part. Add a prefab, then adjust it while watching the character.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawViewport(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height)))
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Front", EditorStyles.toolbarButton)) SetPreviewView(new Vector2(180, 0));
                if (GUILayout.Button("Side", EditorStyles.toolbarButton)) SetPreviewView(new Vector2(90, 0));
                if (GUILayout.Button("Back", EditorStyles.toolbarButton)) SetPreviewView(new Vector2(0, 0));
                if (GUILayout.Button("Fit", EditorStyles.toolbarButton)) FramePreview();
                _showAnchors = GUILayout.Toggle(_showAnchors, "Anchor", EditorStyles.toolbarButton);
            }
            Rect view = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            Observe("viewport");
            EditorGUI.DrawRect(view, new Color(.105f, .135f, .17f));
            HandlePreviewInput(view);
            HandleSourceDrop(view);
            if (_preview?.Root != null && Event.current.type == EventType.Repaint)
            {
                try
                {
                    var texture = _preview.Render(view, _orbit, _zoom, _preview.FramingBounds.center + _pan);
                    if (texture != null)
                    {
                        GUI.DrawTexture(view, texture, ScaleMode.StretchToFill, false);
                        if (Event.current.type == EventType.Repaint) ObservePreview?.Invoke(texture);
                    }
                    if (_showAnchors) DrawAnchor(view);
                }
                catch (Exception e) { _error = e.Message; }
            }
            if (_preview?.Root == null)
                GUI.Label(view, "Drop a Shooter / Visual prefab here\nor choose one above.", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true });
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Zoom", GUILayout.Width(35));
                _zoom = GUILayout.HorizontalSlider(_zoom, .45f, 3.5f);
                if (_draft.HasPreviewEquipment)
                {
                    bool visible = GUILayout.Toggle(_showEquipment, "테스트 장비 보기", GUILayout.Width(112));
                    if (visible != _showEquipment) { _showEquipment = visible; _applyNeeded = true; }
                }
            }
            GUILayout.Label("우클릭: 둘러보기 · 우클릭 + WASD: 이동 · Q/E: 아래/위 · Shift: 빠르게", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("Alt + 좌클릭: 캐릭터 중심 회전 · 휠: 확대 · 가운데 버튼: 이동 · F: 전체 보기", EditorStyles.centeredGreyMiniLabel);
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Warning);
        }
    }

    private void HandleSourceDrop(Rect rect)
    {
        var e = Event.current;
        if (!rect.Contains(e.mousePosition) || (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)) return;
        if (DragAndDrop.objectReferences.Length != 1 || DragAndDrop.objectReferences[0] is not GameObject source ||
            ShooterCustomizePreview.ResolveVisualSource(source) == null) return;
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            Queue(() => SelectDroppedTarget(source));
        }
        e.Use();
    }

    private void DrawAnchor(Rect view)
    {
        bool backpack = _equipmentMode && _draft.previewBackpack != null && _draft.showPreviewBackpack;
        var id = backpack ? ShooterVisualSocketId.Back : _equipmentMode ? _draft.equipmentSocket : Part;
        Transform anchor = backpack ? _preview.BackpackMount : _preview.Rig.ResolveSocket(id);
        string label = PartLabel(id) + " Anchor";
        if (!_equipmentMode && _focusFittingAnchor && _attachment < 0 && HasEquipmentAtPart(Part) && FittingItem(_selectedFittingGear) != null &&
            EquipmentAppearsInPart(_selectedFittingGear, Part, _draft.equipmentSocket))
        {
            anchor = _preview.EquipmentMount(_selectedFittingGear) ?? anchor;
            label = FittingLabel(_selectedFittingGear) + " 실제 장착점";
        }
        if (anchor == null) return;
        Vector3 screen = _preview.Camera.WorldToViewportPoint(anchor.position);
        if (screen.z <= 0) return;
        Vector2 point = new(view.x + screen.x * view.width, view.yMax - screen.y * view.height);
        if (!view.Contains(point)) return;
        EditorGUI.DrawRect(new Rect(point.x - 5, point.y - 1, 10, 2), Color.cyan);
        EditorGUI.DrawRect(new Rect(point.x - 1, point.y - 5, 2, 10), Color.cyan);
        GUI.Label(new Rect(Mathf.Clamp(point.x + 8, view.x, view.xMax - 135), Mathf.Clamp(point.y - 10, view.y, view.yMax - 20), 132, 20),
            label, EditorStyles.whiteMiniLabel);
        if (id == ShooterVisualSocketId.Back && Event.current.type == EventType.Repaint)
            ObserveControl?.Invoke("anchor:backpack", new Rect(point - Vector2.one * 5, Vector2.one * 10));
    }

    private void DrawEditor(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height)))
        {
            GUILayout.Label(_equipmentMode ? "무기 · 방탄복 · 가스마스크 · 백팩 테스트" : PartLabel(Part) + " — 기준점 / 액세서리", EditorStyles.boldLabel);
            _editScroll = EditorGUILayout.BeginScrollView(_editScroll);
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 108;
            try
            {
                if (_preview?.Rig == null) EditorGUILayout.HelpBox("Choose a compatible Shooter visual first.", MessageType.Info);
                else if (_equipmentMode) DrawEquipment();
                else DrawPartEditor();
                GUILayout.Space(18);
                _advanced = EditorGUILayout.Foldout(_advanced, _equipmentMode ? "고급: 가스마스크 Item SO 공통 배치 편집" : "고급: 표시 영역 / 내보내기", true);
                if (_advanced && _preview?.Rig != null) DrawAdvanced();
            }
            finally { EditorGUIUtility.labelWidth = labelWidth; }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawPartEditor()
    {
        DrawPartEquipment();
        DrawAnchorEditor();
        if (Part == ShooterVisualSocketId.Body)
        {
            EditorGUI.BeginChangeCheck();
            Color tint = EditorGUILayout.ColorField("Body color", _draft.tint);
            if (EditorGUI.EndChangeCheck()) Edit("Change Body Color", () => _draft.tint = tint);
            if (_preview.BodyHasCollider)
                EditorGUILayout.HelpBox("This source has a collider inside its body mesh. Body resizing is locked. Separate visual and collision roots first.", MessageType.Warning);
            _showBodyShape = EditorGUILayout.Foldout(_showBodyShape, "몸통 비율 조정 (장착 기준점과 별도)", true);
            if (_showBodyShape)
                using (new EditorGUI.DisabledScope(_preview.BodyHasCollider || _preview.Rig.BodyVisualRoot == null))
                    DrawPose("Body proportions", _draft.body, pose => Edit("Change Body Proportions", () => _draft.body = pose), true);
            GUILayout.Space(12);
        }
        if ((Part == ShooterVisualSocketId.Eyes || Part == ShooterVisualSocketId.Mouth) &&
            _preview.Rig.ResolveSocket(Part) != null && !_preview.Rig.ResolveSocket(Part).gameObject.activeSelf)
            EditorGUILayout.HelpBox("Face 장식 / 장비 때문에 현재 숨겨진 부위입니다. 아래 목록에서 제거하거나 숨김 조건을 바꿀 수 있어요.", MessageType.Info);
        GUILayout.Label($"장착된 액세서리 — 전체 {_draft.attachments.Count}개", EditorStyles.boldLabel);
        _showAllAttachments = EditorGUILayout.ToggleLeft("모든 부위 보기 (숨겨진 장식 포함)", _showAllAttachments);
        Observe("attachments:all");
        if (_draft.attachments.Count == 0) GUILayout.Label("장착된 장식이 없습니다. 아래에 프리팹을 넣어 주세요.", EditorStyles.wordWrappedMiniLabel);
        for (int i = 0; i < _draft.attachments.Count; i++)
        {
            if (!_showAllAttachments && _draft.attachments[i].socket != Part) continue;
            int index = i;
            var entry = _draft.attachments[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                Texture icon = entry.visualPrefab != null ? AssetPreview.GetAssetPreview(entry.visualPrefab) ?? AssetPreview.GetMiniThumbnail(entry.visualPrefab) : null;
                var color = GUI.backgroundColor;
                if (_attachment == i) GUI.backgroundColor = new Color(.25f, .72f, .86f);
                string label = "[" + entry.socket + "] " + (entry.visualPrefab != null ? entry.visualPrefab.name : "Missing prefab") +
                    (_preview.IsAttachmentHidden(entry) ? " (숨김)" : "");
                if (GUILayout.Button(new GUIContent(label, icon), GUILayout.Height(36)))
                { _attachment = i; int partIndex = Array.IndexOf(Parts, entry.socket); if (partIndex >= 0) _part = partIndex; }
                Observe("accessory:" + i);
                GUI.backgroundColor = color;
                if (GUILayout.Button(new GUIContent("빼기", "선택한 외형에서만 제거합니다. 원본 프리팹 파일은 삭제하지 않습니다. Ctrl+Z로 복구 가능."), GUILayout.Width(48), GUILayout.Height(36)))
                    Queue(() => { Edit("Remove Accessory", () => _draft.RemoveAttachment(index)); _attachment = -1; });
                Observe("accessory:remove:" + i);
            }
        }
        DrawAccessoryRegistration();
        GUILayout.Label("SO의 기본 부위에 추가 — 등록 후 변경 가능", EditorStyles.miniBoldLabel);
        DrawAccessoryDrop();
        if (_attachment >= 0 && _attachment < _draft.attachments.Count && _draft.attachments[_attachment].socket == Part)
        {
            int index = _attachment;
            var entry = _draft.attachments[index];
            GUILayout.Space(8);
            DrawAccessoryDefinition(index);
            DrawEquipmentHideRules(index);
            DrawPose("위치·크기·회전 조정", entry.localTransform, pose => Edit("Fit Accessory", () =>
            { var value = _draft.attachments[index]; value.localTransform = pose; _draft.attachments[index] = value; }), false);
            DrawAccessoryAlignment(index);
            if (GUILayout.Button(new GUIContent("프리팹 원래 배치로 되돌리기", "SO 저장값이 아니라 원본 프리팹의 배치로 되돌립니다. SO나 프리팹 파일은 바꾸지 않습니다.")))
                Edit("Reset Accessory", () => { var value = _draft.attachments[index];
                    value.localTransform = ShooterVisualLocalTransform.FromTransform(value.visualPrefab != null ? value.visualPrefab.transform : null);
                    _draft.attachments[index] = value; });
        }
        else GUILayout.Label("목록에서 장식을 누르면 개별 위치·크기·장비 착용 시 숨김을 설정할 수 있어요.", EditorStyles.wordWrappedMiniLabel);
    }

    private void DrawAccessoryDrop()
    {
        Rect rect = GUILayoutUtility.GetRect(1, 42, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "+ 액세서리 SO / 순수 장식 프리팹을 드래그\nSO의 기본 부위·보정값으로 추가", EditorStyles.helpBox);
        Event e = Event.current;
        if (!rect.Contains(e.mousePosition) || (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)) return;
        var prefabs = new List<UnityEngine.Object>();
        foreach (var obj in DragAndDrop.objectReferences)
            if (CanAddAccessoryAsset(obj)) prefabs.Add(obj);
        if (prefabs.Count == 0) return;
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (e.type == EventType.DragPerform) { DragAndDrop.AcceptDrag(); Queue(() => { foreach (var prefab in prefabs) AddAccessoryAsset(prefab); }); }
        e.Use();
    }

    private void DrawPose(string title, ShooterVisualLocalTransform pose, Action<ShooterVisualLocalTransform> changed, bool body)
    {
        GUILayout.Label(title, EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        Vector3 scale = pose.localScale;
        // Display legacy default zero scale as runtime does, without dirtying it simply by viewing.
        if (scale == Vector3.zero) scale = Vector3.one;
        scale.y = DrawUnboundedScale(body ? "키 (Y)" : "세로 크기 (Y)", scale.y, 1);
        scale.x = DrawUnboundedScale("가로 크기 (X)", scale.x, 0);
        scale.z = DrawUnboundedScale("두께 (Z)", scale.z, 2);
        Vector3 pos = pose.localPosition;
        pos.y = EditorGUILayout.Slider("상하 위치 (Y)", pos.y, Mathf.Min(-2, pos.y), Mathf.Max(2, pos.y));
        pos.x = EditorGUILayout.Slider("좌우 위치 (X)", pos.x, Mathf.Min(-2, pos.x), Mathf.Max(2, pos.x));
        pos.z = EditorGUILayout.Slider("앞뒤 위치 (Z)", pos.z, Mathf.Min(-2, pos.z), Mathf.Max(2, pos.z));
        Vector3 angles = EditorGUILayout.Vector3Field("회전 (도)", pose.localEulerAngles);
        if (EditorGUI.EndChangeCheck()) changed(new ShooterVisualLocalTransform(pos, angles, scale));
    }

    private float DrawUnboundedScale(string label, float value, int axis)
    {
        // Grow the convenience slider around the current value, but freeze its range
        // during a drag so a stationary mouse cannot continually increase the scale.
        if (GUIUtility.hotControl == 0)
            _scaleSliderRanges[axis] = new Vector2(Mathf.Min(.01f, value), Mathf.Max(3, Mathf.Abs(value) * 2));
        var range = _scaleSliderRanges[axis];
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel(new GUIContent(label, "크기 상한 없음. 오른쪽 숫자칸에 원하는 값을 직접 입력하세요."));
            EditorGUI.BeginChangeCheck();
            float sliderValue = GUILayout.HorizontalSlider(value, range.x, range.y, GUILayout.MinWidth(24));
            // A repaint must not clamp a typed value to the convenience slider range.
            if (EditorGUI.EndChangeCheck()) value = sliderValue;
            return EditorGUILayout.FloatField(value, GUILayout.Width(60));
        }
    }

    private void DrawMaskItemAuthoring()
    {
        var item = (GasMaskItemSO)EditorGUILayout.ObjectField("가스마스크 Item SO", _draft.equipment, typeof(GasMaskItemSO), false);
        if (item != _draft.equipment && ConfirmEquipmentSwitch())
        { _draft.LoadEquipment(item); _showEquipment = true; _applyNeeded = true; SyncDirty(); }
        if (_draft.equipment == null)
        { EditorGUILayout.HelpBox("Choose a GasMask ItemSO to try it on. Equipment fitting is saved separately from the permanent look.", MessageType.Info); return; }
        GUILayout.Label("영구 외형에 추가하지 않고, 착용 배치만 Item SO에 저장합니다.", EditorStyles.wordWrappedMiniLabel);
        EditorGUI.BeginChangeCheck();
        var prefab = (GameObject)EditorGUILayout.ObjectField("착용 모델", _draft.equipmentPrefab, typeof(GameObject), false);
        var socket = (ShooterVisualSocketId)EditorGUILayout.EnumPopup("Item SO 장착 부위", _draft.equipmentSocket);
        if (EditorGUI.EndChangeCheck()) Edit("Change Equipment Visual", () =>
        { _draft.equipmentPrefab = prefab; _draft.equipmentSocket = socket; }, true);
        if (_preview.Rig.ResolveSocket(_draft.equipmentSocket) == null)
            EditorGUILayout.HelpBox("The selected socket is missing on this character.", MessageType.Warning);
        DrawPose("가스마스크 배치", _draft.equipmentPose,
            pose => Edit("Fit Equipment", () => _draft.equipmentPose = pose, true), false);
        Observe("fitting:pose:GasMask");
        if (GUILayout.Button("가스마스크 Item SO에 저장" + (_draft.equipmentDirty ? " *" : ""), GUILayout.Height(30)))
        { _draft.SaveEquipment(); SyncDirty(); ShowNotification(new GUIContent("가스마스크 배치를 Item SO에 저장했습니다.")); }
        Observe("fitting:save:GasMask");
        if (GUILayout.Button("가스마스크 SO 저장값 불러오기") && ConfirmEquipmentSwitch())
        { _draft.LoadEquipment(_draft.equipment); _applyNeeded = true; SyncDirty(); }
        Observe("fitting:reload:GasMask");
    }

    private void DrawAdvanced()
    {
        if (!_equipmentMode)
        {
            if (_attachment >= 0 && _attachment < _draft.attachments.Count && _draft.attachments[_attachment].socket == Part)
            {
                int index = _attachment;
                EditorGUI.BeginChangeCheck();
                var coverage = (ShooterVisualOccupancy)EditorGUILayout.EnumFlagsField("Occupied regions", _draft.attachments[index].occupiedRegions);
                if (EditorGUI.EndChangeCheck()) Edit("Change Cosmetic Coverage", () =>
                { var entry = _draft.attachments[index]; entry.occupiedRegions = coverage; _draft.attachments[index] = entry; });
            }
        }
        else
        {
            GUILayout.Label("가스마스크 위치·모델은 위쪽 장비 카드에서 편집합니다.", EditorStyles.wordWrappedMiniLabel);
            if (_draft.equipment == null) return;
            EditorGUI.BeginChangeCheck();
            var coverage = (ShooterVisualOccupancy)EditorGUILayout.EnumFlagsField("Occupied regions", _draft.equipmentCoverage);
            if (EditorGUI.EndChangeCheck()) Edit("Change Equipment Coverage", () => _draft.equipmentCoverage = coverage, true);
        }
        GUILayout.Space(10);
        using (new EditorGUI.DisabledScope(_draft.target == null))
        {
            if (GUILayout.Button("Select saved Look asset")) Selection.activeObject = _draft.target;
            if (GUILayout.Button("Export Visual Prefab...")) ExportPrefab();
        }
        if (GUILayout.Button("Rebuild preview")) RebuildPreview(false);
    }

    private void TryLoadSource(GameObject input)
    {
        var source = ShooterCustomizePreview.ResolveVisualSource(input);
        if (source == null)
        { ShowNotification(new GUIContent("Choose a prefab with a ShooterVisualRig or Visual_Shooter hierarchy.")); return; }
        var rig = source.GetComponent<ShooterVisualRig>();
        LoadSource(source, rig != null ? rig.DefaultCustomize : null);
    }

    private void LoadSource(GameObject source, ShooterCustomizeSO look)
    {
        if (source == null) return;
        LookNameUi.SelectionClicked();
        _renameLook = look != null ? look.name : string.Empty;
        _draft.visualPrefab = source;
        _draft.target = look;
        RebuildPreview(true);
        _attachment = -1;
        _pan = Vector3.zero; _zoom = 1;
        SyncDirty();
    }

    private void RebuildPreview(bool loadLook)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || _draft.visualPrefab == null) return;
        try
        {
            _preview ??= new ShooterCustomizePreview();
            _preview.Build(_draft.visualPrefab, CurrentTarget?.Prefab);
            _builtSource = _draft.visualPrefab;
            if (loadLook) _draft.LoadLook(_draft.visualPrefab, _draft.target, _preview.Rig);
            _preview.Apply(_draft, _showEquipment);
            _preview.Frame();
            _applyNeeded = false;
            _error = null;
        }
        catch (Exception e) { _error = e.Message; _preview?.Dispose(); _preview = null; Debug.LogException(e); }
        Repaint();
    }

    private bool ConfirmEquipmentSwitch()
    {
        return ConfirmFittingSwitch(ShooterVisualEquipmentKind.GasMask);
    }

    private bool ConfirmLeave()
    {
        if (!Dirty) return true;
        int choice = EditorUtility.DisplayDialogComplex("캐릭터 / 장비에 미저장 변경이 있어요", "외형·스폰 연결과 장비 Item SO 배치를 저장할까요? 장비 배치는 같은 아이템을 쓰는 모든 캐릭터에 적용됩니다.", "저장", "취소", "변경 버리기");
        if (choice == 1) return false;
        if (choice == 0 && !SaveAll()) return false;
        if (choice == 2) _draft.ReloadEquipmentEdits();
        return true;
    }

    public override void SaveChanges() { if (SaveAll()) base.SaveChanges(); }
    public override void DiscardChanges() { if (_draft != null) { _draft.lookDirty = false; _draft.ReloadEquipmentEdits(); } _bindingDirty = false; base.DiscardChanges(); }

    private bool SaveAll()
    {
        if (_draft == null) return true;
        if (_bindingDirty)
        {
            string error = BindingError();
            if (error != null) { _error = error; return false; }
        }
        if (_draft.lookDirty && !SaveLook(false)) return false;
        _draft.SaveEquipmentEdits();
        if (_bindingDirty && !SaveTargetBinding()) return false;
        SyncDirty();
        return !Dirty;
    }

    private bool SaveLook(bool saveAs)
    {
        if (_draft.visualPrefab == null || _preview?.Rig == null) return false;
        ShooterCustomizeSO destination = _draft.target;
        if (CurrentTarget != null)
        {
            ShooterCustomizeSO original = destination;
            if (saveAs || destination == null || ShooterCustomizeCatalog.NeedsIndependentCopy(CurrentTarget, destination))
                destination = ShooterCustomizeCatalog.CreateLook(CurrentTarget, _draft,
                    destination != null ? destination.name + "_Copy" : CurrentTarget.Name + "_Look");
            Undo.RecordObject(destination, "Save Shooter Look");
            _draft.CaptureTo(destination);
            EditorUtility.SetDirty(destination);
            AssetDatabase.SaveAssetIfDirty(destination);
            ReplaceWorkingLook(original, destination);
            _draft.target = destination; _draft.lookDirty = false;
            ClearThumbnails(); SyncDirty();
            return true;
        }
        if (saveAs || destination == null)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Shooter Look", destination != null ? destination.name + "_Copy" : "ShooterLook",
                "asset", "Save the visual look. The source prefab and equipment items stay separate.");
            if (string.IsNullOrEmpty(path)) return false;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            { EditorUtility.DisplayDialog("Choose a new asset", "Use a new name for Save As. Select an existing Look above to update it.", "OK"); return false; }
            destination = CreateInstance<ShooterCustomizeSO>();
            var serialized = new SerializedObject(destination);
            serialized.FindProperty("_customizeId").stringValue = Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Replace(' ', '_');
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(destination, path);
        }
        Undo.RecordObject(destination, "Save Shooter Look");
        _draft.CaptureTo(destination);
        EditorUtility.SetDirty(destination);
        AssetDatabase.SaveAssetIfDirty(destination);
        _draft.target = destination;
        _draft.lookDirty = false;
        SyncDirty();
        ShowNotification(new GUIContent("Shooter look saved. Source prefab unchanged."));
        return true;
    }

    private void ExportPrefab()
    {
        if (_draft.target == null || (_draft.lookDirty && !SaveLook(false))) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(_draft.visualPrefab)) != _draft.visualPrefab)
        { EditorUtility.DisplayDialog("Visual sub-object", "Export requires a standalone visual prefab. Save the Look normally, or choose the Visual_Shooter prefab itself.", "OK"); return; }
        string output = EditorUtility.SaveFilePanelInProject("Export Shooter Visual", "Visual_" + _draft.target.name, "prefab", "Exports a visual bound to this Look asset.");
        if (string.IsNullOrEmpty(output)) return;
        // Do not overwrite the source or an existing gameplay prefab.
        if (AssetDatabase.LoadMainAssetAtPath(output) != null)
        { EditorUtility.DisplayDialog("Choose a new prefab", "Export to a new file to preserve existing prefabs.", "OK"); return; }
        var contents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(_draft.visualPrefab));
        try
        {
            var rig = contents.GetComponent<ShooterVisualRig>() ?? contents.AddComponent<ShooterVisualRig>();
            rig.AutoResolveReferences();
            rig.SetDefaultCustomize(_draft.target);
            var saved = PrefabUtility.SaveAsPrefabAsset(contents, output);
            EditorGUIUtility.PingObject(saved);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }
}
