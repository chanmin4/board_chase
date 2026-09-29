using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Live visual-only authoring: parts / isolated character viewport / adjustments.</summary>
public sealed class ShooterCustomizeStationWindow : EditorWindow
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
    private Vector3 _pan;
    private GameObject _addPrefab;
    private int _attachment = -1;
    private bool _advanced, _showAnchors = true, _showEquipment = true;
    private string _error;
    private bool _applyNeeded;
    private double _nextPreviewTick;
    private bool Dirty => _draft != null && (_draft.lookDirty || _draft.equipmentDirty);
    private ShooterVisualSocketId Part => Parts[Mathf.Clamp(_part, 0, Parts.Length - 1)];

    [MenuItem("Tools/Authoring/Shooter Customize Station")]
    public static void Open()
    {
        var window = GetWindow<ShooterCustomizeStationWindow>("Shooter Customize");
        window.minSize = new Vector2(1060, 620);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Shooter Customize");
        minSize = new Vector2(1060, 620);
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
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndo;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        if (_draft != null) _recoveryJson = EditorJsonUtility.ToJson(_draft);
        _preview?.Dispose();
        _preview = null;
        if (_draft != null) DestroyImmediate(_draft);
        _draft = null;
    }

    private void OnPlayMode(PlayModeStateChange state)
    {
        // Never instantiate gameplay objects during a play-mode transition.
        if (state == PlayModeStateChange.ExitingEditMode) { _preview?.Dispose(); _preview = null; }
        if (state == PlayModeStateChange.EnteredEditMode) RebuildPreview(false);
        Repaint();
    }

    private void Tick()
    {
        if (_draft == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
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
        saveChangesMessage = "Save the edited shooter look and equipment fitting before closing?";
    }

    private void Edit(string label, Action change, bool equipment = false)
    {
        Undo.RecordObject(_draft, label);
        change();
        if (equipment) _draft.equipmentDirty = true; else _draft.lookDirty = true;
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
        DrawToolbar();
        float height = Mathf.Max(500, position.height - 63);
        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(height)))
        {
            DrawParts(164, height);
            DrawViewport(Mathf.Max(380, position.width - 538), height);
            DrawEditor(358, height);
        }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label(Dirty ? "Unsaved draft - source assets are unchanged" : "Preview only - edit a part, then Save Look",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Face covers Eyes + Mouth. Hair stays separate.", EditorStyles.miniLabel);
        }
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
                string label = Parts[i] + (count > 0 ? $"  ({count})" : "");
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
                if (GUILayout.Button("Front", EditorStyles.toolbarButton)) _orbit = new Vector2(180, 0);
                if (GUILayout.Button("Side", EditorStyles.toolbarButton)) _orbit = new Vector2(90, 0);
                if (GUILayout.Button("Back", EditorStyles.toolbarButton)) _orbit = new Vector2(0, 0);
                if (GUILayout.Button("Fit", EditorStyles.toolbarButton)) { _preview?.Frame(); _zoom = 1; _pan = Vector3.zero; }
                _showAnchors = GUILayout.Toggle(_showAnchors, "Anchor", EditorStyles.toolbarButton);
            }
            Rect view = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(view, new Color(.105f, .135f, .17f));
            HandlePreviewInput(view);
            HandleSourceDrop(view);
            if (_preview?.Root != null && Event.current.type == EventType.Repaint)
            {
                try
                {
                    var texture = _preview.Render(view, _orbit, _zoom, _preview.FramingBounds.center + _pan);
                    if (texture != null) GUI.DrawTexture(view, texture, ScaleMode.StretchToFill, false);
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
                if (_draft.equipment != null)
                {
                    bool visible = GUILayout.Toggle(_showEquipment, "Show gear", GUILayout.Width(84));
                    if (visible != _showEquipment) { _showEquipment = visible; _applyNeeded = true; }
                }
            }
            GUILayout.Label("Drag: rotate   |   Wheel: zoom   |   Right-drag: pan", EditorStyles.centeredGreyMiniLabel);
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Warning);
        }
    }

    private void HandlePreviewInput(Rect rect)
    {
        Event e = Event.current;
        int id = GUIUtility.GetControlID("ShooterOrbit".GetHashCode(), FocusType.Passive, rect);
        if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
        { _zoom = Mathf.Clamp(_zoom * Mathf.Exp(-e.delta.y * .06f), .45f, 3.5f); e.Use(); Repaint(); }
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && e.button <= 2)
        { GUIUtility.hotControl = id; e.Use(); }
        if (e.type == EventType.MouseDrag && GUIUtility.hotControl == id)
        {
            if (e.button == 0) { _orbit.x += e.delta.x * .6f; _orbit.y = Mathf.Clamp(_orbit.y + e.delta.y * .4f, -75, 75); }
            else if (_preview?.Camera != null)
            {
                float scale = _preview.Bounds.size.magnitude / Mathf.Max(100, rect.height) / _zoom;
                _pan += (-_preview.Camera.transform.right * e.delta.x + _preview.Camera.transform.up * e.delta.y) * scale;
            }
            e.Use(); Repaint();
        }
        if (e.type == EventType.MouseUp && GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; e.Use(); }
    }

    private void HandleSourceDrop(Rect rect)
    {
        var e = Event.current;
        if (!rect.Contains(e.mousePosition) || (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)) return;
        if (DragAndDrop.objectReferences.Length != 1 || DragAndDrop.objectReferences[0] is not GameObject source ||
            ShooterCustomizePreview.ResolveVisualSource(source) == null) return;
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (e.type == EventType.DragPerform) { DragAndDrop.AcceptDrag(); if (ConfirmLeave()) TryLoadSource(source); }
        e.Use();
    }

    private void DrawAnchor(Rect view)
    {
        Transform anchor = _preview.Rig.ResolveSocket(_equipmentMode ? _draft.equipmentSocket : Part);
        if (anchor == null) return;
        Vector3 screen = _preview.Camera.WorldToViewportPoint(anchor.position);
        if (screen.z <= 0) return;
        Vector2 point = new(view.x + screen.x * view.width, view.yMax - screen.y * view.height);
        if (!view.Contains(point)) return;
        EditorGUI.DrawRect(new Rect(point.x - 5, point.y - 1, 10, 2), Color.cyan);
        EditorGUI.DrawRect(new Rect(point.x - 1, point.y - 5, 2, 10), Color.cyan);
        GUI.Label(new Rect(Mathf.Clamp(point.x + 8, view.x, view.xMax - 135), Mathf.Clamp(point.y - 10, view.y, view.yMax - 20), 132, 20),
            (_equipmentMode ? _draft.equipmentSocket : Part) + " anchor", EditorStyles.whiteMiniLabel);
    }

    private void DrawEditor(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height)))
        {
            GUILayout.Label(_equipmentMode ? "EQUIPMENT FITTING" : Part.ToString().ToUpperInvariant(), EditorStyles.boldLabel);
            _editScroll = EditorGUILayout.BeginScrollView(_editScroll);
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 108;
            try
            {
                if (_preview?.Rig == null) EditorGUILayout.HelpBox("Choose a compatible Shooter visual first.", MessageType.Info);
                else if (_equipmentMode) DrawEquipment();
                else DrawPartEditor();
                GUILayout.Space(18);
                _advanced = EditorGUILayout.Foldout(_advanced, "Advanced / Export", true);
                if (_advanced && _preview?.Rig != null) DrawAdvanced();
            }
            finally { EditorGUIUtility.labelWidth = labelWidth; }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawPartEditor()
    {
        if (Part == ShooterVisualSocketId.Body)
        {
            EditorGUI.BeginChangeCheck();
            Color tint = EditorGUILayout.ColorField("Body color", _draft.tint);
            if (EditorGUI.EndChangeCheck()) Edit("Change Body Color", () => _draft.tint = tint);
            if (_preview.BodyHasCollider)
                EditorGUILayout.HelpBox("This source has a collider inside its body mesh. Body resizing is locked. Separate visual and collision roots first.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(_preview.BodyHasCollider || _preview.Rig.BodyVisualRoot == null))
                DrawPose("Body proportions", _draft.body, pose => Edit("Change Body Proportions", () => _draft.body = pose), true);
            GUILayout.Space(12);
        }
        if (_preview.Rig.ResolveSocket(Part) == null)
        { EditorGUILayout.HelpBox("This visual has no " + Part + " socket.", MessageType.Warning); return; }
        if ((Part == ShooterVisualSocketId.Eyes || Part == ShooterVisualSocketId.Mouth) &&
            !_preview.Rig.ResolveSocket(Part).gameObject.activeSelf)
            EditorGUILayout.HelpBox("Hidden by the Face accessory/equipment. Removing Face reveals this part again. Hair is not hidden.", MessageType.Info);
        GUILayout.Label("ACCESSORIES", EditorStyles.miniBoldLabel);
        for (int i = 0; i < _draft.attachments.Count; i++)
        {
            if (_draft.attachments[i].socket != Part) continue;
            int index = i;
            var entry = _draft.attachments[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                Texture icon = entry.visualPrefab != null ? AssetPreview.GetAssetPreview(entry.visualPrefab) ?? AssetPreview.GetMiniThumbnail(entry.visualPrefab) : null;
                var color = GUI.backgroundColor;
                if (_attachment == i) GUI.backgroundColor = new Color(.25f, .72f, .86f);
                if (GUILayout.Button(new GUIContent(entry.visualPrefab != null ? entry.visualPrefab.name : "Missing prefab", icon), GUILayout.Height(40)))
                    _attachment = i;
                GUI.backgroundColor = color;
                if (GUILayout.Button("X", GUILayout.Width(24), GUILayout.Height(40)))
                { Edit("Remove Accessory", () => _draft.attachments.RemoveAt(index)); _attachment = -1; break; }
            }
        }
        DrawAccessoryDrop();
        using (new EditorGUILayout.HorizontalScope())
        {
            _addPrefab = (GameObject)EditorGUILayout.ObjectField(_addPrefab, typeof(GameObject), false);
            using (new EditorGUI.DisabledScope(_addPrefab == null))
                if (GUILayout.Button("+ Add", GUILayout.Width(56))) AddAccessory(_addPrefab);
        }
        if (_attachment >= 0 && _attachment < _draft.attachments.Count && _draft.attachments[_attachment].socket == Part)
        {
            int index = _attachment;
            var entry = _draft.attachments[index];
            GUILayout.Space(8);
            DrawPose("Selected accessory", entry.localTransform, pose => Edit("Fit Accessory", () =>
            { var value = _draft.attachments[index]; value.localTransform = pose; _draft.attachments[index] = value; }), false);
            if (GUILayout.Button("Reset to prefab placement"))
                Edit("Reset Accessory", () => { var value = _draft.attachments[index];
                    value.localTransform = ShooterVisualLocalTransform.FromTransform(value.visualPrefab != null ? value.visualPrefab.transform : null);
                    _draft.attachments[index] = value; });
        }
        else GUILayout.Label("Select an accessory above to adjust its placement.", EditorStyles.wordWrappedMiniLabel);
    }

    private void DrawAccessoryDrop()
    {
        Rect rect = GUILayoutUtility.GetRect(1, 42, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "+ Drop accessory prefabs here", EditorStyles.helpBox);
        Event e = Event.current;
        if (!rect.Contains(e.mousePosition) || (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)) return;
        var prefabs = new List<GameObject>();
        foreach (var obj in DragAndDrop.objectReferences)
            if (obj is GameObject prefab && EditorUtility.IsPersistent(prefab) && PrefabUtility.IsPartOfPrefabAsset(prefab)) prefabs.Add(prefab);
        if (prefabs.Count == 0) return;
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (e.type == EventType.DragPerform) { DragAndDrop.AcceptDrag(); foreach (var prefab in prefabs) AddAccessory(prefab); }
        e.Use();
    }

    private void AddAccessory(GameObject prefab)
    {
        if (prefab == null || !EditorUtility.IsPersistent(prefab) || !PrefabUtility.IsPartOfPrefabAsset(prefab)) return;
        Edit("Add Accessory", () => _draft.attachments.Add(new ShooterCustomizeAttachment {
            socket = Part, visualPrefab = prefab, localTransform = ShooterVisualLocalTransform.FromTransform(prefab.transform) }));
        _attachment = _draft.attachments.Count - 1;
        _addPrefab = null;
    }

    private void DrawPose(string title, ShooterVisualLocalTransform pose, Action<ShooterVisualLocalTransform> changed, bool body)
    {
        GUILayout.Label(title, EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        Vector3 scale = pose.localScale;
        // Display legacy default zero scale as runtime does, without dirtying it simply by viewing.
        if (scale == Vector3.zero) scale = Vector3.one;
        scale.y = EditorGUILayout.Slider(body ? "Height" : "Length (Y)", scale.y, Mathf.Min(.01f, scale.y), Mathf.Max(3, scale.y));
        scale.x = EditorGUILayout.Slider("Width (X)", scale.x, Mathf.Min(.01f, scale.x), Mathf.Max(3, scale.x));
        scale.z = EditorGUILayout.Slider("Depth (Z)", scale.z, Mathf.Min(.01f, scale.z), Mathf.Max(3, scale.z));
        Vector3 pos = pose.localPosition;
        pos.y = EditorGUILayout.Slider("Up / Down", pos.y, Mathf.Min(-2, pos.y), Mathf.Max(2, pos.y));
        pos.x = EditorGUILayout.Slider("Left / Right", pos.x, Mathf.Min(-2, pos.x), Mathf.Max(2, pos.x));
        pos.z = EditorGUILayout.Slider("Forward / Back", pos.z, Mathf.Min(-2, pos.z), Mathf.Max(2, pos.z));
        Vector3 angles = EditorGUILayout.Vector3Field("Rotation", pose.localEulerAngles);
        if (EditorGUI.EndChangeCheck()) changed(new ShooterVisualLocalTransform(pos, angles, scale));
    }

    private void DrawEquipment()
    {
        var item = (GasMaskItemSO)EditorGUILayout.ObjectField("Equipment item", _draft.equipment, typeof(GasMaskItemSO), false);
        if (item != _draft.equipment && ConfirmEquipmentSwitch())
        { _draft.LoadEquipment(item); _showEquipment = true; _applyNeeded = true; SyncDirty(); }
        if (_draft.equipment == null)
        { EditorGUILayout.HelpBox("Choose a GasMask ItemSO to try it on. Equipment fitting is saved separately from the permanent look.", MessageType.Info); return; }
        EditorGUILayout.HelpBox("Preview only until Save Equipment. This is not added to the permanent look.", MessageType.None);
        EditorGUI.BeginChangeCheck();
        var prefab = (GameObject)EditorGUILayout.ObjectField("Visual prefab", _draft.equipmentPrefab, typeof(GameObject), false);
        var socket = (ShooterVisualSocketId)EditorGUILayout.EnumPopup("Attach to", _draft.equipmentSocket);
        if (EditorGUI.EndChangeCheck()) Edit("Change Equipment Visual", () =>
        { _draft.equipmentPrefab = prefab; _draft.equipmentSocket = socket; }, true);
        if (_preview.Rig.ResolveSocket(_draft.equipmentSocket) == null)
            EditorGUILayout.HelpBox("The selected socket is missing on this character.", MessageType.Warning);
        DrawPose("Equipment placement", _draft.equipmentPose,
            pose => Edit("Fit Equipment", () => _draft.equipmentPose = pose, true), false);
        if (GUILayout.Button("Save Equipment" + (_draft.equipmentDirty ? " *" : ""), GUILayout.Height(30)))
        { _draft.SaveEquipment(); SyncDirty(); ShowNotification(new GUIContent("Equipment fitting saved.")); }
        if (GUILayout.Button("Reload Equipment") && ConfirmEquipmentSwitch())
        { _draft.LoadEquipment(_draft.equipment); _applyNeeded = true; SyncDirty(); }
    }

    private void DrawAdvanced()
    {
        if (!_equipmentMode)
        {
            Transform socket = _preview.Rig.ResolveSocket(Part);
            if (socket != null)
            {
                EditorGUILayout.HelpBox("Socket placement affects everything attached to this part, including gameplay equipment.", MessageType.None);
                using (new EditorGUI.DisabledScope(socket.GetComponentInChildren<Collider>(true) != null))
                    DrawPose("Socket transform", _draft.SocketPose(Part), pose => Edit("Move Visual Socket", () => _draft.SetSocketPose(Part, pose)), false);
            }
            if (_attachment >= 0 && _attachment < _draft.attachments.Count && _draft.attachments[_attachment].socket == Part)
            {
                int index = _attachment;
                EditorGUI.BeginChangeCheck();
                var coverage = (ShooterVisualOccupancy)EditorGUILayout.EnumFlagsField("Occupied regions", _draft.attachments[index].occupiedRegions);
                if (EditorGUI.EndChangeCheck()) Edit("Change Cosmetic Coverage", () =>
                { var entry = _draft.attachments[index]; entry.occupiedRegions = coverage; _draft.attachments[index] = entry; });
            }
        }
        else if (_draft.equipment != null)
        {
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
            _preview.Build(_draft.visualPrefab);
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
        if (!_draft.equipmentDirty) return true;
        int choice = EditorUtility.DisplayDialogComplex("Unsaved equipment fitting", "Save equipment changes first?", "Save", "Cancel", "Discard");
        if (choice == 1) return false;
        if (choice == 0) _draft.SaveEquipment();
        return true;
    }

    private bool ConfirmLeave()
    {
        if (!Dirty) return true;
        int choice = EditorUtility.DisplayDialogComplex("Unsaved shooter edits", "Save the current look and equipment fitting before switching?", "Save", "Cancel", "Discard");
        if (choice == 1) return false;
        if (choice == 0 && !SaveAll()) return false;
        if (choice == 2) _draft.LoadEquipment(_draft.equipment);
        return true;
    }

    public override void SaveChanges() { if (SaveAll()) base.SaveChanges(); }
    public override void DiscardChanges() { if (_draft != null) { _draft.lookDirty = false; _draft.equipmentDirty = false; } base.DiscardChanges(); }

    private bool SaveAll()
    {
        if (_draft == null) return true;
        if (_draft.lookDirty && !SaveLook(false)) return false;
        if (_draft.equipmentDirty) _draft.SaveEquipment();
        SyncDirty();
        return !Dirty;
    }

    private bool SaveLook(bool saveAs)
    {
        if (_draft.visualPrefab == null || _preview?.Rig == null) return false;
        ShooterCustomizeSO destination = _draft.target;
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
