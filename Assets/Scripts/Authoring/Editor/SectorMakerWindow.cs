/*
 * Role hierarchy: Authoring > Editor
 * Primary responsibility: Provides the SectorMakerWindow authoring tool for sector placement, content slots and branch-map topology.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class SectorMakerWindow : EditorWindow
{
    private enum PlacementMode
    {
        FixedSectorStructure,
        EnemySpawnPoint,
        ContentSlotAnchor,
        StageTopology
    }

    private readonly struct RoomReadinessIssue
    {
        public RoomReadinessIssue(MessageType messageType, string message)
        {
            MessageType = messageType;
            Message = message;
        }

        public MessageType MessageType { get; }
        public string Message { get; }
    }

    [SerializeField] private bool _paintingEnabled;
    [SerializeField] private PlacementMode _mode;
    [SerializeField] private SectorPresetSO _activeSectorPreset;
    [SerializeField] private SectorRuntime _activeSectorRuntime;
    [SerializeField] private bool _showAdvancedTopology;

    [SerializeField] private LayerMask _floorSurfaceMask;
    [SerializeField] private bool _surfaceMaskInitialized;

    [SerializeField] private GameObject _structurePrefab;
    [SerializeField] private Transform _sectorObjectParent;
    [SerializeField] private float _structureBottomOffset = 0.3f;

    [SerializeField] private Transform _enemySpawnPointParent;
    [SerializeField] private SectorRuntime _sectorRuntime;
    [SerializeField] private float _enemySpawnPointYOffset = 0.3f;
    [Tooltip("Shows every authored EnemySpawnPoint below the assigned parent as a numbered Scene-view overlay while the Enemy Spawn Point tab is selected.")]
    [SerializeField] private bool _showExistingEnemySpawnPoints = true;

    [SerializeField] private Transform _contentSlotParent;
    [SerializeField] private SectorRuntime _contentSectorRuntime;
    [SerializeField] private SectorPresetSO _contentSectorPreset;
    [SerializeField] private SectorContentProfileSO _contentProfile;
    [SerializeField] private string _contentSlotId = "loot_a";
    [SerializeField] private SectorContentSlotKind _contentSlotKind = SectorContentSlotKind.LootContainer;
    [SerializeField] private float _contentSlotYOffset = 0.05f;
    [SerializeField] private bool _showExistingContentSlots = true;

    [SerializeField] private StageTopologySO _topology;
    [SerializeField] private StageDefinitionSO _topologyStageDefinition;
    [SerializeField] private Vector2Int _topologyNodeCoord;
    [SerializeField] private bool _topologyNodeIsBoss;
    [SerializeField] [Min(0)] private int _topologyWeight = 1;

    [SerializeField] private float _rotationY;
    [SerializeField] private float _rotationStep = 15f;
    [SerializeField] private bool _selectCreatedObject;
    [SerializeField] private Vector2 _settingsScrollPosition;

    [System.NonSerialized] private GUIStyle _spawnPointLabelStyle;
    [System.NonSerialized] private GUIStyle _contentSlotLabelStyle;
    [System.NonSerialized] private System.Collections.Generic.List<RoomReadinessIssue> _roomReadinessIssues = new();

    [MenuItem("Tools/Authoring/Sector Maker")]
    // Purpose: Opens the open UI or workflow.
    private static void Open()
    {
        SectorMakerWindow window = GetWindow<SectorMakerWindow>("Sector Maker");
        window.minSize = new Vector2(1180f, 640f);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        InitializeFloorMask();
        minSize = new Vector2(1180f, 640f);
        SceneView.duringSceneGui += HandleSceneGUI;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        SceneView.duringSceneGui -= HandleSceneGUI;
    }

    // Purpose: Responds to the gui callback.
    private void OnGUI()
    {
        if (!_showAdvancedTopology && _mode == PlacementMode.StageTopology)
            _mode = PlacementMode.FixedSectorStructure;

        AuthoringWorkspaceUi.DrawHeader(
            "Sector Maker",
            "Owner: Level / Encounter",
            "Select a room preset, open its room prefab, then place objects or spawn positions directly on the floor.",
            "1 Choose Room",
            "2 Choose Placement",
            "3 Click Floor",
            "4 Save");

        DrawRoomSelection();

        float workspaceHeight = Mathf.Max(410f, position.height - 228f);
        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(workspaceHeight)))
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(324f), GUILayout.Height(workspaceHeight)))
                DrawSimplePlacementControls();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.Height(workspaceHeight)))
            {
                EditorGUILayout.LabelField(
                    _mode == PlacementMode.StageTopology ? "ADVANCED STAGE ROUTES" : "PLACEMENT SETTINGS",
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField(GetActiveLayerInstruction(), EditorStyles.wordWrappedMiniLabel);
                _settingsScrollPosition = EditorGUILayout.BeginScrollView(_settingsScrollPosition);
                DrawSelectedDataLinkContext();
                EditorGUILayout.Space(7f);
                DrawActiveLayerSettings();
                EditorGUILayout.EndScrollView();
            }
        }
    }

    // Purpose: Draws the room-preset and active-room selection workflow.
    private void DrawRoomSelection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("1. CHOOSE ROOM", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Choose the room preset first. Open its prefab, select the SectorRuntime root in the Hierarchy, then use it as the active room.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUI.BeginChangeCheck();
            SectorPresetSO selectedPreset = (SectorPresetSO)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Room Preset",
                    "The preset owns the room prefab and its runtime content profile. It is not copied or modified while you place objects."),
                _activeSectorPreset,
                typeof(SectorPresetSO),
                false);
            if (EditorGUI.EndChangeCheck())
                SetActiveSectorPreset(selectedPreset);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_activeSectorPreset == null || _activeSectorPreset.SectorPrefab == null))
                {
                    if (GUILayout.Button("Open Room Prefab", GUILayout.Height(24f)))
                        OpenActiveSectorPrefab();
                }

                if (GUILayout.Button("Use Selected Room Root", GUILayout.Height(24f)))
                    UseSelectedSectorRuntime();
            }

            EditorGUI.BeginChangeCheck();
            SectorRuntime selectedRuntime = (SectorRuntime)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Active Room Root",
                    "The SectorRuntime root inside the currently open room prefab or scene. Placement parents are resolved from this room when possible."),
                _activeSectorRuntime,
                typeof(SectorRuntime),
                true);
            if (EditorGUI.EndChangeCheck())
                SetActiveSectorRuntime(selectedRuntime);

            string roomStatus = _activeSectorRuntime != null
                ? $"Active room: {_activeSectorRuntime.name}"
                : "No active room selected. You can still assign placement parents manually below.";
            EditorGUILayout.LabelField(roomStatus, EditorStyles.miniBoldLabel);

            DrawRoomReadinessCheck();
        }
    }

    // Purpose: Draws a read-only structural check for the selected room prefab.
    private void DrawRoomReadinessCheck()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("ROOM SETUP CHECK", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Checks the selected room prefab without changing it. Use this before placing objects so missing floors, marker roots and content-plan links are found early.",
            EditorStyles.wordWrappedMiniLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(_activeSectorPreset == null || _activeSectorPreset.SectorPrefab == null))
            {
                if (GUILayout.Button("Check Selected Room", GUILayout.Height(22f)))
                    RunRoomReadinessCheck();
            }

            using (new EditorGUI.DisabledScope(_activeSectorRuntime == null))
            {
                if (GUILayout.Button("Select Active Room", GUILayout.Height(22f)))
                    Selection.activeObject = _activeSectorRuntime.gameObject;
            }
        }

        if (_roomReadinessIssues == null || _roomReadinessIssues.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Choose a Room Preset and press Check Selected Room. This does not create, move or delete anything.",
                MessageType.None);
            return;
        }

        int errorCount = 0;
        int warningCount = 0;
        for (int i = 0; i < _roomReadinessIssues.Count; i++)
        {
            RoomReadinessIssue issue = _roomReadinessIssues[i];
            if (issue.MessageType == MessageType.Error)
                errorCount++;
            else if (issue.MessageType == MessageType.Warning)
                warningCount++;
        }

        string summary = errorCount > 0
            ? $"Not ready: {errorCount} blocking issue(s), {warningCount} warning(s)."
            : warningCount > 0
                ? $"Placement can continue: {warningCount} optional setup warning(s)."
                : "Ready: floor and authored room links passed the structural check.";
        EditorGUILayout.LabelField(summary, EditorStyles.miniBoldLabel);

        for (int i = 0; i < _roomReadinessIssues.Count; i++)
        {
            RoomReadinessIssue issue = _roomReadinessIssues[i];
            EditorGUILayout.HelpBox(issue.Message, issue.MessageType);
        }
    }

    // Purpose: Reads the selected room prefab and reports only authoring issues; it never mutates room data.
    private void RunRoomReadinessCheck()
    {
        _roomReadinessIssues ??= new System.Collections.Generic.List<RoomReadinessIssue>();
        _roomReadinessIssues.Clear();

        if (_activeSectorPreset == null)
        {
            AddRoomReadinessIssue(MessageType.Error, "Choose a Room Preset before running the setup check.");
            return;
        }

        if (_activeSectorPreset.SectorPrefab == null)
        {
            AddRoomReadinessIssue(MessageType.Error, "The selected Room Preset has no Sector Prefab assigned.");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(_activeSectorPreset.SectorPrefab.gameObject);
        if (string.IsNullOrWhiteSpace(prefabPath))
        {
            AddRoomReadinessIssue(MessageType.Error, "The selected Room Preset does not reference a Project prefab asset.");
            return;
        }

        GameObject prefabContents = null;
        try
        {
            prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            SectorRuntime[] sectorRuntimes = prefabContents.GetComponentsInChildren<SectorRuntime>(true);
            if (sectorRuntimes.Length == 0)
            {
                AddRoomReadinessIssue(MessageType.Error, "The room prefab has no SectorRuntime. It cannot be used by the stage generator or Sector Maker.");
                return;
            }

            if (sectorRuntimes.Length > 1)
            {
                AddRoomReadinessIssue(MessageType.Warning, $"The room prefab contains {sectorRuntimes.Length} SectorRuntime components. Use one room root for predictable placement.");
            }

            SectorRuntime sectorRuntime = sectorRuntimes[0];
            CheckFloorPlacementReadiness(prefabContents, sectorRuntime);
            CheckStaticObjectParentReadiness(sectorRuntime);
            CheckEnemySpawnReadiness(sectorRuntime);
            CheckContentSlotReadiness(sectorRuntime, _activeSectorPreset.ContentProfile);
            CheckActiveRoomMatchesPreset(prefabPath);
        }
        catch (System.Exception exception)
        {
            AddRoomReadinessIssue(MessageType.Error, $"The room prefab could not be checked: {exception.Message}");
        }
        finally
        {
            if (prefabContents != null)
                PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    // Purpose: Adds one read-only result row to the room setup check.
    private void AddRoomReadinessIssue(MessageType messageType, string message)
    {
        _roomReadinessIssues ??= new System.Collections.Generic.List<RoomReadinessIssue>();
        _roomReadinessIssues.Add(new RoomReadinessIssue(messageType, message));
    }

    // Purpose: Checks whether the room contains a valid click target for floor placement.
    private void CheckFloorPlacementReadiness(GameObject prefabContents, SectorRuntime sectorRuntime)
    {
        Collider[] colliders = prefabContents.GetComponentsInChildren<Collider>(true);
        int validFloorColliderCount = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider != null &&
                !collider.isTrigger &&
                (_floorSurfaceMask.value & (1 << collider.gameObject.layer)) != 0)
            {
                validFloorColliderCount++;
            }
        }

        if (validFloorColliderCount == 0)
        {
            AddRoomReadinessIssue(
                MessageType.Error,
                $"No non-trigger Collider in '{sectorRuntime.name}' matches the current Floor Layer. Put the room floor on this layer before using click placement.");
            return;
        }

        AddRoomReadinessIssue(
            MessageType.Info,
            $"Floor placement ready: {validFloorColliderCount} valid floor Collider(s) match the current Floor Layer.");
    }

    // Purpose: Checks which parent will own fixed room objects created by the placement tool.
    private void CheckStaticObjectParentReadiness(SectorRuntime sectorRuntime)
    {
        Transform objectParent = FindChildByName(sectorRuntime.transform, "SectorObjectContainer");
        if (objectParent != null)
        {
            AddRoomReadinessIssue(MessageType.Info, "Static Object parent ready: SectorObjectContainer was found.");
            return;
        }

        AddRoomReadinessIssue(
            MessageType.Warning,
            "SectorObjectContainer was not found. Static Object placement will use the room root fallback. Create a dedicated SectorObjectContainer if you want fixed props kept separately.");
    }

    // Purpose: Checks authored enemy marker placement and registration below the room runtime root.
    private void CheckEnemySpawnReadiness(SectorRuntime sectorRuntime)
    {
        EnemySpawnPoint[] allPoints = sectorRuntime.GetComponentsInChildren<EnemySpawnPoint>(true);
        Transform markerRoot = sectorRuntime.EnemySpawnPointRoot;
        if (markerRoot == null)
        {
            AddRoomReadinessIssue(
                MessageType.Warning,
                $"No Enemy Spawn Point Parent is registered. Use 'Create Spawn Point Parent In Active Room' before placing exact enemy positions. Existing marker count: {allPoints.Length}.");
            return;
        }

        if (markerRoot != sectorRuntime.transform && !markerRoot.IsChildOf(sectorRuntime.transform))
        {
            AddRoomReadinessIssue(MessageType.Error, "Enemy Spawn Point Parent is outside this SectorRuntime. Move or recreate it below the room root.");
            return;
        }

        EnemySpawnPoint[] registeredPoints = markerRoot.GetComponentsInChildren<EnemySpawnPoint>(true);
        if (allPoints.Length != registeredPoints.Length)
        {
            AddRoomReadinessIssue(
                MessageType.Warning,
                $"Enemy Spawn Point Parent is assigned, but {allPoints.Length - registeredPoints.Length} marker(s) are outside it and will not be used by this room's authored spawn root.");
        }

        AddRoomReadinessIssue(
            MessageType.Info,
            $"Enemy spawn setup: {registeredPoints.Length} marker(s) below the registered Enemy Spawn Point Parent.");
    }

    // Purpose: Checks that content anchor IDs and kinds match the selected room content profile.
    private void CheckContentSlotReadiness(SectorRuntime sectorRuntime, SectorContentProfileSO contentProfile)
    {
        SectorContentSlotAnchor[] anchors = sectorRuntime.GetComponentsInChildren<SectorContentSlotAnchor>(true);
        if (anchors.Length == 0)
        {
            AddRoomReadinessIssue(MessageType.Info, "No Loot / Interactable Positions are authored yet. This is valid until the room needs runtime content.");
            return;
        }

        if (contentProfile == null)
        {
            AddRoomReadinessIssue(
                MessageType.Warning,
                $"The room has {anchors.Length} Loot / Interactable Position(s), but its Room Preset has no Content Profile. Positions will not receive configured runtime candidates yet.");
            return;
        }

        var usedIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < anchors.Length; i++)
        {
            SectorContentSlotAnchor anchor = anchors[i];
            string slotId = anchor != null ? anchor.SlotId : string.Empty;
            if (anchor == null || string.IsNullOrWhiteSpace(slotId))
            {
                AddRoomReadinessIssue(MessageType.Error, "A Loot / Interactable Position has no Position ID.");
                continue;
            }

            if (!usedIds.Add(slotId))
            {
                AddRoomReadinessIssue(MessageType.Error, $"Duplicate Loot / Interactable Position ID '{slotId}' was found. IDs must be unique inside one room.");
                continue;
            }

            if (!contentProfile.TryGetSlotDefinition(slotId, out SectorContentSlotDefinition definition))
            {
                AddRoomReadinessIssue(MessageType.Warning, $"Position '{slotId}' has no matching Content Profile row yet. Select its Content Profile and add candidates before runtime testing.");
                continue;
            }

            if (definition.contentKind != anchor.ContentKind)
            {
                AddRoomReadinessIssue(MessageType.Error, $"Position '{slotId}' type is {anchor.ContentKind}, but its Content Profile row is {definition.contentKind}. Make both types match.");
                continue;
            }

            if (definition.enabled && !definition.IsConfigured)
            {
                AddRoomReadinessIssue(MessageType.Warning, $"Position '{slotId}' is enabled but has no valid candidate prefab. Add a candidate or disable its Content Profile row.");
            }
        }

        AddRoomReadinessIssue(MessageType.Info, $"Content setup: {anchors.Length} position(s) checked against '{contentProfile.name}'.");
    }

    // Purpose: Warns when the open room root belongs to another prefab than the selected preset.
    private void CheckActiveRoomMatchesPreset(string expectedPrefabPath)
    {
        if (_activeSectorRuntime == null || string.IsNullOrWhiteSpace(expectedPrefabPath))
            return;

        string activePrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_activeSectorRuntime.gameObject);
        if (!string.IsNullOrWhiteSpace(activePrefabPath) &&
            !string.Equals(activePrefabPath, expectedPrefabPath, System.StringComparison.OrdinalIgnoreCase))
        {
            AddRoomReadinessIssue(
                MessageType.Warning,
                "The Active Room Root belongs to a different prefab than the selected Room Preset. Open the selected room prefab, then choose its SectorRuntime root before placing.");
        }
    }

    // Purpose: Draws the simplified floor-click placement controls.
    private void DrawSimplePlacementControls()
    {
        EditorGUILayout.LabelField("2. CHOOSE PLACEMENT", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Pick one tool, enter placement mode, then left-click a valid floor Collider in the Scene view.",
            EditorStyles.wordWrappedMiniLabel);

        DrawPlacementToolButton(
            "Static Object",
            "Obstacle, decorative prop, fixed loot object, or any prefab that should always exist in this room.",
            PlacementMode.FixedSectorStructure);
        DrawPlacementToolButton(
            "Enemy Spawn Point",
            "A position used by the selected encounter wave. The marker does not own enemy loot or wave data.",
            PlacementMode.EnemySpawnPoint);
        DrawPlacementToolButton(
            "Loot / Interactable Position",
            "A runtime position for a loot container, rare cache, mini shop, NPC, or other special interactable.",
            PlacementMode.ContentSlotAnchor);

        EditorGUILayout.Space(7f);
        _showAdvancedTopology = EditorGUILayout.ToggleLeft(
            new GUIContent(
                "Show Advanced Stage Routes",
                "Only needed to author the logical branch map between rooms. It is not needed to place room objects or spawn points."),
            _showAdvancedTopology);

        if (_showAdvancedTopology)
        {
            DrawPlacementToolButton(
                "Advanced: Stage Routes",
                "Logical Normal/Boss room graph. This does not place anything in the current room.",
                PlacementMode.StageTopology);
        }

        if (_mode == PlacementMode.StageTopology)
        {
            EditorGUILayout.HelpBox(
                "Stage Routes are separate stage-map data. Return to one of the three placement tools to place objects in the room.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space(5f);
        DrawPlacementModeToggle();
        _floorSurfaceMask = LayerMaskField(
            new GUIContent(
                "Floor Layer",
                "Only non-trigger Colliders on this layer accept Scene-view placement clicks. SectorFloor is selected by default."),
            _floorSurfaceMask);
        DrawRotationSettings();
        DrawPlacementGuidance();

        EditorGUILayout.Space(5f);
        if (GUILayout.Button("Save Current Room", GUILayout.Height(28f)))
            SaveCurrentRoom();

        if (_mode == PlacementMode.EnemySpawnPoint || _mode == PlacementMode.ContentSlotAnchor)
        {
            if (GUILayout.Button("Open Loot Maker", GUILayout.Height(24f)))
                LootProgressionEditorWindow.Open();

            EditorGUILayout.HelpBox(
                "Drop pools remain common data in Loot Maker. This room tool only owns placement positions, so a room cannot accidentally create a hidden stage-only loot table.",
                MessageType.None);
        }
    }

    // Purpose: Draws one simple placement-tool selector button.
    private void DrawPlacementToolButton(string title, string tooltip, PlacementMode mode)
    {
        bool selected = _mode == mode;
        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = selected
            ? new Color(0.22f, 0.72f, 0.80f)
            : Color.white;

        if (GUILayout.Button(new GUIContent(title, tooltip), GUILayout.Height(28f)))
            SetMode(mode);

        GUI.backgroundColor = previousColor;
    }

    // Purpose: Stores the selected room preset and mirrors its existing content profile.
    private void SetActiveSectorPreset(SectorPresetSO preset)
    {
        _activeSectorPreset = preset;
        _contentSectorPreset = preset;
        _contentProfile = preset != null ? preset.ContentProfile : null;
        ClearRoomReadinessCheck();
        Repaint();
    }

    // Purpose: Opens the currently selected room prefab for direct Scene-view authoring.
    private void OpenActiveSectorPrefab()
    {
        if (_activeSectorPreset == null || _activeSectorPreset.SectorPrefab == null)
            return;

        GameObject prefab = _activeSectorPreset.SectorPrefab.gameObject;
        Selection.activeObject = prefab;
        AssetDatabase.OpenAsset(prefab);
    }

    // Purpose: Uses the selected hierarchy object or one of its parents as the active room.
    private void UseSelectedSectorRuntime()
    {
        SectorRuntime selected = Selection.activeTransform != null
            ? Selection.activeTransform.GetComponentInParent<SectorRuntime>()
            : null;

        if (selected == null)
        {
            ShowNotification(new GUIContent("Select a SectorRuntime root or one of its children in the Hierarchy."));
            return;
        }

        SetActiveSectorRuntime(selected);
    }

    // Purpose: Sets the active room and resolves compatible existing placement parents.
    private void SetActiveSectorRuntime(SectorRuntime sectorRuntime)
    {
        _activeSectorRuntime = sectorRuntime;
        _sectorRuntime = sectorRuntime;
        _contentSectorRuntime = sectorRuntime;
        ClearRoomReadinessCheck();

        if (sectorRuntime == null)
            return;

        if (_sectorObjectParent == null || !_sectorObjectParent.IsChildOf(sectorRuntime.transform))
        {
            _sectorObjectParent = FindChildByName(sectorRuntime.transform, "SectorObjectContainer") ??
                                  sectorRuntime.PatternObjectRoot;
        }

        if (_enemySpawnPointParent == null || !_enemySpawnPointParent.IsChildOf(sectorRuntime.transform))
            _enemySpawnPointParent = sectorRuntime.EnemySpawnPointRoot;

        if (_contentSlotParent == null || !_contentSlotParent.IsChildOf(sectorRuntime.transform))
        {
            SectorContentSlotAnchor existingAnchor =
                sectorRuntime.GetComponentInChildren<SectorContentSlotAnchor>(true);
            _contentSlotParent = existingAnchor != null
                ? existingAnchor.transform.parent
                : FindChildByName(sectorRuntime.transform, "ContentSlots");
        }

        Repaint();
        SceneView.RepaintAll();
    }

    // Purpose: Clears stale validation feedback after the selected room context changes.
    private void ClearRoomReadinessCheck()
    {
        _roomReadinessIssues?.Clear();
    }

    // Purpose: Repaints the selected-data link panel whenever the Hierarchy or Project selection changes.
    private void OnSelectionChange()
    {
        Repaint();
    }

    // Purpose: Draws the read-only path from the selected authored object to its owner data.
    private void DrawSelectedDataLinkContext()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("SELECTED DATA LINK", EditorStyles.boldLabel);

            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a Loot / Interactable Position, LootableObject, or Enemy Spawn Point in the Hierarchy to inspect its runtime data path.",
                    MessageType.None);
                return;
            }

            SectorContentSlotAnchor contentAnchor = selectedObject.GetComponentInParent<SectorContentSlotAnchor>();
            if (contentAnchor != null)
            {
                DrawContentAnchorLinkContext(contentAnchor);
                return;
            }

            LootableObject lootableObject = selectedObject.GetComponentInParent<LootableObject>();
            if (lootableObject != null)
            {
                DrawLootableObjectLinkContext(lootableObject);
                return;
            }

            EnemySpawnPoint spawnPoint = selectedObject.GetComponentInParent<EnemySpawnPoint>();
            if (spawnPoint != null)
            {
                DrawEnemySpawnPointLinkContext(spawnPoint);
                return;
            }

            EditorGUILayout.HelpBox(
                "The selected object does not own a common loot or enemy-spawn data link. Select an authored position or runtime loot object instead.",
                MessageType.None);
        }
    }

    // Purpose: Shows the selected content anchor's profile row and its candidate-object loot links.
    private void DrawContentAnchorLinkContext(SectorContentSlotAnchor anchor)
    {
        EditorGUILayout.ObjectField("Selected Position", anchor, typeof(SectorContentSlotAnchor), true);
        EditorGUILayout.LabelField("Position ID", string.IsNullOrWhiteSpace(anchor.SlotId) ? "Missing" : anchor.SlotId);
        EditorGUILayout.LabelField("Content Type", anchor.ContentKind.ToString());

        if (!TryResolveSelectedRoomProfile(anchor.transform, out SectorPresetSO roomPreset, out SectorContentProfileSO profile))
            return;

        EditorGUILayout.ObjectField("Room Preset", roomPreset, typeof(SectorPresetSO), false);
        if (profile == null)
        {
            EditorGUILayout.HelpBox(
                "This Room Preset has no Content Profile. The position exists, but it has no runtime candidate list yet.",
                MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.ObjectField("Content Profile", profile, typeof(SectorContentProfileSO), false);
            if (GUILayout.Button("Select Profile", GUILayout.Width(96f)))
                Selection.activeObject = profile;
        }

        if (!profile.TryGetSlotDefinition(anchor.SlotId, out SectorContentSlotDefinition definition))
        {
            EditorGUILayout.HelpBox(
                $"No Content Profile row matches '{anchor.SlotId}'. Add the row in the Content Profile before this position can spawn content.",
                MessageType.Warning);
            return;
        }

        if (definition.contentKind != anchor.ContentKind)
        {
            EditorGUILayout.HelpBox(
                $"Type mismatch: position is {anchor.ContentKind}, but the Content Profile row is {definition.contentKind}.",
                MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField(
            "Runtime Candidate Roll",
            definition.enabled
                ? $"Enabled  •  {definition.spawnChancePercent:0.#}% chance"
                : "Disabled");

        if (definition.prefabCandidates == null || definition.prefabCandidates.Count == 0)
        {
            EditorGUILayout.HelpBox("This Content Profile row has no candidate prefab yet.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Candidate Links", EditorStyles.miniBoldLabel);
        for (int i = 0; i < definition.prefabCandidates.Count; i++)
        {
            WeightedSectorContentPrefab candidate = definition.prefabCandidates[i];
            if (candidate == null || candidate.prefab == null)
            {
                EditorGUILayout.HelpBox($"Candidate {i + 1} is missing a prefab.", MessageType.Warning);
                continue;
            }

            EditorGUILayout.ObjectField(
                new GUIContent($"{i + 1}. Candidate ({Mathf.Max(0, candidate.weight)} weight)"),
                candidate.prefab,
                typeof(GameObject),
                false);

            LootableObject candidateLootable = candidate.prefab.GetComponentInChildren<LootableObject>(true);
            if (candidateLootable == null)
            {
                EditorGUILayout.LabelField(
                    "  Link",
                    "No LootableObject: this can be an NPC, mini shop, or other non-loot content.",
                    EditorStyles.miniLabel);
                continue;
            }

            DrawLootTableSummary(candidateLootable, "  Loot Table");
        }
    }

    // Purpose: Shows the selected world loot object's resolved common pool without editing it.
    private void DrawLootableObjectLinkContext(LootableObject lootableObject)
    {
        EditorGUILayout.ObjectField("Selected Loot Object", lootableObject, typeof(LootableObject), true);
        DrawLootTableSummary(lootableObject, "Loot Table");
    }

    // Purpose: Shows the direct LootObjectSO and LootTableSO used by a world loot prefab.
    private static void DrawLootTableSummary(LootableObject lootableObject, string label)
    {
        if (lootableObject == null || lootableObject.LootObject == null)
        {
            EditorGUILayout.HelpBox(label + ": LootObjectSO is not assigned.", MessageType.Error);
            return;
        }

        LootObjectSO lootObject = lootableObject.LootObject;
        LootTableSO table = lootObject.LootTable;
        if (table == null)
        {
            EditorGUILayout.HelpBox(label + ": LootObjectSO '" + lootObject.name + "' has no direct LootTableSO.", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, lootObject.ResolveDisplayName() + " -> " + table.name, EditorStyles.miniLabel);
            if (GUILayout.Button("Open Table", GUILayout.Width(82f)))
                Selection.activeObject = table;
        }

        DrawLootTableFacts(table, label + ": the direct LootTableSO has no default pool.");
    }

    // Purpose: Draws a compact read-only summary of a LootTableSO default pool.
    private static void DrawLootTableFacts(LootTableSO table, string missingMessage)
    {
        LootObjectPool pool = table != null ? table.Pool : null;
        if (pool == null)
        {
            EditorGUILayout.HelpBox(missingMessage, MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField(
            "  Fixed Object Pool",
            Mathf.Max(1, pool.minRollCount) + "-" + Mathf.Max(pool.minRollCount, pool.maxRollCount) + " roll(s), placement decides stage",
            EditorStyles.miniLabel);

        if (pool.rarityTiers == null || pool.rarityTiers.Count == 0)
        {
            EditorGUILayout.HelpBox("The LootTableSO fixed pool has no rarity tiers.", MessageType.Warning);
            return;
        }

        for (int tierIndex = 0; tierIndex < pool.rarityTiers.Count; tierIndex++)
        {
            LootRarityTier tier = pool.rarityTiers[tierIndex];
            if (tier == null)
                continue;

            EditorGUILayout.LabelField(
                "  " + tier.rarity,
                tier.chancePercent.ToString("0.#") + "%  •  " + CountValidPoolItems(tier) + " item entries",
                EditorStyles.miniLabel);
        }
    }

    // Purpose: Shows the authored spawn point's stage encounter and enemy-to-loot path.
    private void DrawEnemySpawnPointLinkContext(EnemySpawnPoint spawnPoint)
    {
        EditorGUILayout.ObjectField("Selected Spawn Point", spawnPoint, typeof(EnemySpawnPoint), true);
        SectorRuntime room = spawnPoint.GetComponentInParent<SectorRuntime>();
        if (room == null)
        {
            EditorGUILayout.HelpBox("This Enemy Spawn Point is not below a SectorRuntime.", MessageType.Error);
            return;
        }

        EditorGUILayout.ObjectField("Room Runtime", room, typeof(SectorRuntime), true);
        if (room.EnemySpawnPointRoot == null)
        {
            EditorGUILayout.HelpBox(
                "This marker has no registered Enemy Spawn Point Parent. It will not be used by the authored room spawn root until registered.",
                MessageType.Warning);
        }
        else if (spawnPoint.transform != room.EnemySpawnPointRoot &&
                 !spawnPoint.transform.IsChildOf(room.EnemySpawnPointRoot))
        {
            EditorGUILayout.HelpBox(
                "This marker is outside the room's registered Enemy Spawn Point Parent and will not be selected by authored-point spawning.",
                MessageType.Warning);
        }

        if (_activeSectorPreset == null)
        {
            EditorGUILayout.HelpBox(
                "Choose this room's Room Preset above to trace its Stage -> Encounter -> Enemy -> Common Loot path.",
                MessageType.Info);
            return;
        }

        if (!DoesActivePresetMatchSelectedRoom(room.transform))
        {
            EditorGUILayout.HelpBox(
                "The selected spawn point belongs to another room prefab. Choose the matching Room Preset before reading its stage encounter path.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.ObjectField("Room Preset", _activeSectorPreset, typeof(SectorPresetSO), false);
        EditorGUILayout.HelpBox(
            "This marker owns only a position. A stage picks an Encounter, the Encounter picks Wave candidates, and those waves pick Enemy Shooter configs with common loot pools.",
            MessageType.None);

        int stageCount = DrawStagesUsingActivePreset();
        if (stageCount == 0)
        {
            EditorGUILayout.HelpBox(
                "No Stage Definition currently registers this Room Preset as a Normal room candidate.",
                MessageType.Warning);
        }
    }

    // Purpose: Resolves the selected content anchor only against the active room preset; it never guesses another preset.
    private bool TryResolveSelectedRoomProfile(
        Transform selectedTransform,
        out SectorPresetSO roomPreset,
        out SectorContentProfileSO contentProfile)
    {
        roomPreset = _activeSectorPreset;
        contentProfile = roomPreset != null ? roomPreset.ContentProfile : null;
        if (roomPreset == null)
        {
            EditorGUILayout.HelpBox(
                "Choose this room's Room Preset above to inspect the selected position's Content Profile.",
                MessageType.Info);
            return false;
        }

        SectorRuntime selectedRoom = selectedTransform != null
            ? selectedTransform.GetComponentInParent<SectorRuntime>()
            : null;
        if (selectedRoom != null && !DoesActivePresetMatchSelectedRoom(selectedRoom.transform))
        {
            EditorGUILayout.HelpBox(
                "The selected position belongs to another room prefab. Choose the matching Room Preset before inspecting its Content Profile.",
                MessageType.Warning);
            return false;
        }

        return true;
    }

    // Purpose: Checks whether the active preset refers to the same prefab as the selected room hierarchy.
    private bool DoesActivePresetMatchSelectedRoom(Transform selectedRoom)
    {
        if (_activeSectorPreset == null || _activeSectorPreset.SectorPrefab == null || selectedRoom == null)
            return true;

        string expectedPath = AssetDatabase.GetAssetPath(_activeSectorPreset.SectorPrefab.gameObject);
        string selectedPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedRoom.gameObject);
        return string.IsNullOrWhiteSpace(expectedPath) ||
               string.IsNullOrWhiteSpace(selectedPath) ||
               string.Equals(expectedPath, selectedPath, System.StringComparison.OrdinalIgnoreCase);
    }

    // Purpose: Draws every stage that can select the active Normal room preset and its encounter-to-loot path.
    private int DrawStagesUsingActivePreset()
    {
        string[] stageGuids = AssetDatabase.FindAssets("t:StageDefinitionSO");
        int matchingStageCount = 0;

        for (int i = 0; i < stageGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(stageGuids[i]);
            StageDefinitionSO stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(path);
            if (stage == null || !StageUsesActiveNormalPreset(stage))
                continue;

            matchingStageCount++;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(
                        $"Stage {stage.StageIndex}",
                        stage,
                        typeof(StageDefinitionSO),
                        false);
                    if (GUILayout.Button("Select", GUILayout.Width(58f)))
                        Selection.activeObject = stage;
                }

                var roleBinding = stage.RaidRuntime?.rooms?.Find(row => row != null && row.ContainsPreset(_activeSectorPreset));
                if (roleBinding != null)
                {
                    EditorGUILayout.LabelField($"Raid room role: {roleBinding.role}", EditorStyles.miniBoldLabel);
                    if (roleBinding.encounter != null)
                    {
                        DrawEncounterLootPath(roleBinding.encounter, 1, 1);
                        continue;
                    }
                }

                if (stage.NormalEncounterPresets == null || stage.NormalEncounterPresets.Count == 0)
                {
                    EditorGUILayout.HelpBox("This stage has no Normal-room Encounter candidates.", MessageType.Warning);
                    continue;
                }

                for (int encounterIndex = 0; encounterIndex < stage.NormalEncounterPresets.Count; encounterIndex++)
                {
                    StageDefinitionSO.WeightedEncounterPreset entry = stage.NormalEncounterPresets[encounterIndex];
                    if (entry == null || entry.weight <= 0 || entry.preset == null)
                        continue;

                    DrawEncounterLootPath(entry.preset, entry.weight, encounterIndex + 1);
                }
            }
        }

        return matchingStageCount;
    }

    // Purpose: Checks whether the active room preset is a usable Normal-room candidate for the stage.
    private bool StageUsesActiveNormalPreset(StageDefinitionSO stage)
    {
        if (stage == null || _activeSectorPreset == null || stage.NormalSectorPresets == null)
            return false;

        for (int i = 0; i < stage.NormalSectorPresets.Count; i++)
        {
            StageDefinitionSO.WeightedSectorPreset entry = stage.NormalSectorPresets[i];
            if (entry != null && entry.weight > 0 && entry.preset == _activeSectorPreset)
                return true;
        }

        return stage.RaidRuntime?.rooms != null &&
               stage.RaidRuntime.rooms.Exists(row => row != null && row.ContainsPreset(_activeSectorPreset));
    }

    // Purpose: Draws one encounter candidate and the wave candidates it can select at runtime.
    private void DrawEncounterLootPath(
        StageBattleEncounterPresetSO encounter,
        int weight,
        int encounterNumber)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.ObjectField(
                $"Encounter {encounterNumber} ({weight} weight)",
                encounter,
                typeof(StageBattleEncounterPresetSO),
                false);

            if (encounter.Waves == null || encounter.Waves.Count == 0)
            {
                EditorGUILayout.HelpBox("This Encounter has no waves.", MessageType.Warning);
                return;
            }

            for (int waveIndex = 0; waveIndex < encounter.Waves.Count; waveIndex++)
            {
                StageBattleEncounterPresetSO.EncounterWave wave = encounter.Waves[waveIndex];
                if (wave == null || wave.enemyWavePresetCandidates == null || wave.enemyWavePresetCandidates.Count == 0)
                {
                    EditorGUILayout.HelpBox($"Wave {waveIndex + 1} has no Wave Preset candidate.", MessageType.Warning);
                    continue;
                }

                EditorGUILayout.LabelField($"Wave {waveIndex + 1}", EditorStyles.miniBoldLabel);
                for (int candidateIndex = 0; candidateIndex < wave.enemyWavePresetCandidates.Count; candidateIndex++)
                {
                    StageBattleEncounterPresetSO.EnemyWavePresetCandidate candidate =
                        wave.enemyWavePresetCandidates[candidateIndex];
                    if (candidate == null || candidate.weight <= 0 || candidate.preset == null)
                        continue;

                    DrawEnemyWaveLootPath(candidate.preset, candidate.weight);
                }
            }
        }
    }

    // Purpose: Draws the enemy configs spawned by one wave and their resolved common loot pools.
    private void DrawEnemyWaveLootPath(StageEnemyWavePresetSO wavePreset, int weight)
    {
        EditorGUILayout.ObjectField(
            new GUIContent($"Wave Candidate ({weight} weight)"),
            wavePreset,
            typeof(StageEnemyWavePresetSO),
            false);

        if (wavePreset.Enemies == null || wavePreset.Enemies.Count == 0)
        {
            EditorGUILayout.HelpBox("This Wave Preset has no enemy entries.", MessageType.Warning);
            return;
        }

        for (int enemyIndex = 0; enemyIndex < wavePreset.Enemies.Count; enemyIndex++)
        {
            StageEnemyWavePresetSO.EnemySpawn enemy = wavePreset.Enemies[enemyIndex];
            if (enemy == null || enemy.count <= 0 || enemy.archetype == null)
            {
                EditorGUILayout.HelpBox($"Enemy entry {enemyIndex + 1} is missing its config or count.", MessageType.Warning);
                continue;
            }

            bool hasCommonLootSource = TryFindEnemyLootSource(
                enemy.archetype,
                out LootProgressionConfigSO config,
                out LootSourceDefinition source);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(
                    new GUIContent($"Enemy x{enemy.count}"),
                    enemy.archetype,
                    typeof(EnemyShooterConfigSO),
                    false);
                if (hasCommonLootSource && GUILayout.Button("Open Pool", GUILayout.Width(82f)))
                {
                    LootProgressionEditorWindow.OpenAndSelectSource(config, source);
                }
            }

            DrawCommonPoolFacts(source: hasCommonLootSource ? source : null,
                missingMessage: $"{enemy.archetype.DisplayName} has no registered Common Loot Source.");
        }
    }

    // Purpose: Shows the resolved common loot source for a LootableObject source ID.
    private void DrawLootSourceSummary(string sourceId, string label)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            EditorGUILayout.HelpBox($"{label}: Source ID is missing.", MessageType.Error);
            return;
        }

        if (!TryFindLootObjectSource(sourceId, out LootProgressionConfigSO config, out LootSourceDefinition source))
        {
            EditorGUILayout.HelpBox($"{label}: no registered Common Loot Source matches ID '{sourceId}'.", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, $"{source.displayName}  [ID: {source.sourceId}]", EditorStyles.miniLabel);
            if (GUILayout.Button("Open Pool", GUILayout.Width(82f)))
                LootProgressionEditorWindow.OpenAndSelectSource(config, source);
        }

        DrawCommonPoolFacts(source, $"{label}: the registered source has no Common Pool.");
    }

    // Purpose: Draws a compact read-only summary of the source's one shared pool without migrating legacy data.
    private static void DrawCommonPoolFacts(LootSourceDefinition source, string missingMessage)
    {
        if (source == null)
        {
            EditorGUILayout.HelpBox(missingMessage, MessageType.Warning);
            return;
        }

        LootStagePool pool = source.FindExactPool(0);
        if (pool == null)
        {
            EditorGUILayout.HelpBox(missingMessage, MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField(
            "  Shared Pool",
            $"{Mathf.Max(1, pool.minRollCount)}-{Mathf.Max(pool.minRollCount, pool.maxRollCount)} roll(s), every stage",
            EditorStyles.miniLabel);

        if (pool.rarityTiers == null || pool.rarityTiers.Count == 0)
        {
            EditorGUILayout.HelpBox("The Common Pool has no rarity tiers.", MessageType.Warning);
            return;
        }

        for (int tierIndex = 0; tierIndex < pool.rarityTiers.Count; tierIndex++)
        {
            LootRarityTier tier = pool.rarityTiers[tierIndex];
            if (tier == null)
                continue;

            EditorGUILayout.LabelField(
                $"  {tier.rarity}",
                $"{tier.chancePercent:0.#}%  •  {CountValidPoolItems(tier)} item entries",
                EditorStyles.miniLabel);
        }
    }

    // Purpose: Counts the valid ItemSO rows in one rarity tier for read-only editor feedback.
    private static int CountValidPoolItems(LootRarityTier tier)
    {
        if (tier?.items == null)
            return 0;

        int count = 0;
        for (int i = 0; i < tier.items.Count; i++)
        {
            if (tier.items[i] != null && tier.items[i].item != null)
                count++;
        }

        return count;
    }

    // Purpose: Resolves the single common enemy source registered for this enemy config across loot configs.
    private static bool TryFindEnemyLootSource(
        EnemyShooterConfigSO enemyConfig,
        out LootProgressionConfigSO foundConfig,
        out LootSourceDefinition foundSource)
    {
        foundConfig = null;
        foundSource = null;
        if (enemyConfig == null)
            return false;

        string[] configGuids = AssetDatabase.FindAssets("t:LootProgressionConfigSO");
        for (int i = 0; i < configGuids.Length; i++)
        {
            LootProgressionConfigSO config = AssetDatabase.LoadAssetAtPath<LootProgressionConfigSO>(
                AssetDatabase.GUIDToAssetPath(configGuids[i]));
            LootSourceDefinition source = config != null ? config.FindEnemySource(enemyConfig) : null;
            if (source == null)
                continue;

            foundConfig = config;
            foundSource = source;
            return true;
        }

        return false;
    }

    // Purpose: Resolves the common loot-object source registered for one stable LootableObject ID.
    private static bool TryFindLootObjectSource(
        string sourceId,
        out LootProgressionConfigSO foundConfig,
        out LootSourceDefinition foundSource)
    {
        foundConfig = null;
        foundSource = null;
        if (string.IsNullOrWhiteSpace(sourceId))
            return false;

        string[] configGuids = AssetDatabase.FindAssets("t:LootProgressionConfigSO");
        for (int i = 0; i < configGuids.Length; i++)
        {
            LootProgressionConfigSO config = AssetDatabase.LoadAssetAtPath<LootProgressionConfigSO>(
                AssetDatabase.GUIDToAssetPath(configGuids[i]));
            LootSourceDefinition source = config != null ? config.FindLootObjectSource(sourceId) : null;
            if (source == null)
                continue;

            foundConfig = config;
            foundSource = source;
            return true;
        }

        return false;
    }

    // Purpose: Finds the first child transform with the requested exact name.
    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && string.Equals(candidate.name, childName, System.StringComparison.Ordinal))
                return candidate;
        }

        return null;
    }

    // Purpose: Saves assets and the room's currently open scene or prefab stage.
    private void SaveCurrentRoom()
    {
        if (_activeSectorRuntime != null && _activeSectorRuntime.gameObject.scene.IsValid())
        {
            var activeRoomScene = _activeSectorRuntime.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(activeRoomScene);
            EditorSceneManager.SaveScene(activeRoomScene);
        }

        AssetDatabase.SaveAssets();
        ShowNotification(new GUIContent("Current room changes saved."));
    }

    // Purpose: Draws the sector assembly flow authoring UI.
    private void DrawSectorAssemblyFlow()
    {
        Rect canvas = GUILayoutUtility.GetRect(1f, 78f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(canvas, new Color(0.045f, 0.08f, 0.10f));
        GUI.Box(canvas, GUIContent.none, EditorStyles.helpBox);

        const float gap = 18f;
        float cardWidth = (canvas.width - 34f - gap * 3f) / 4f;
        float cardHeight = 56f;
        float cardY = canvas.y + 11f;
        for (int index = 0; index < 4; index++)
        {
            Rect card = new Rect(canvas.x + 10f + index * (cardWidth + gap), cardY, cardWidth, cardHeight);
            if (index < 3)
            {
                Rect next = new Rect(card.xMax + gap, cardY, cardWidth, cardHeight);
                DrawFlowConnector(card, next);
            }

            PlacementMode mode = (PlacementMode)index;
            DrawFlowCard(card, mode, BuildFlowValue(mode));
        }
    }

    // Purpose: Draws the flow card authoring UI.
    private void DrawFlowCard(Rect rect, PlacementMode mode, string value)
    {
        bool isSelected = _mode == mode;
        Color background = isSelected
            ? new Color(0.08f, 0.40f, 0.49f)
            : new Color(0.08f, 0.20f, 0.24f);
        EditorGUI.DrawRect(rect, background);
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
        GUI.Label(new Rect(rect.x + 7f, rect.y + 6f, rect.width - 14f, 15f), GetModeTitle(mode), EditorStyles.miniBoldLabel);
        GUI.Label(new Rect(rect.x + 7f, rect.y + 25f, rect.width - 14f, 22f), value, EditorStyles.wordWrappedMiniLabel);
        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            SetMode(mode);
    }

    // Purpose: Draws the flow connector authoring UI.
    private static void DrawFlowConnector(Rect from, Rect to)
    {
        Color color = new Color(0.27f, 0.80f, 0.86f, 0.85f);
        float y = from.center.y;
        EditorGUI.DrawRect(new Rect(from.xMax, y - 1f, to.xMin - from.xMax, 2f), color);
        EditorGUI.DrawRect(new Rect(to.xMin - 6f, y - 4f, 7f, 8f), color);
    }

    // Purpose: Builds the flow value from the current authored data.
    private string BuildFlowValue(PlacementMode mode)
    {
        return mode switch
        {
            PlacementMode.FixedSectorStructure => _structurePrefab != null ? _structurePrefab.name : "Choose structure prefab",
            PlacementMode.EnemySpawnPoint => _enemySpawnPointParent != null ? _enemySpawnPointParent.name : "Choose spawn parent",
            PlacementMode.ContentSlotAnchor => _contentProfile != null ? _contentProfile.name : "Choose content profile",
            PlacementMode.StageTopology => _topology != null ? _topology.name : "Choose stage topology",
            _ => string.Empty
        };
    }

    // Purpose: Returns the current mode title.
    private static string GetModeTitle(PlacementMode mode)
    {
        return mode switch
        {
            PlacementMode.FixedSectorStructure => "STATIC OBJECT",
            PlacementMode.EnemySpawnPoint => "ENEMY SPAWN POINT",
            PlacementMode.ContentSlotAnchor => "LOOT / INTERACTABLE POSITION",
            PlacementMode.StageTopology => "ADVANCED STAGE ROUTES",
            _ => "PLACEMENT"
        };
    }

    // Purpose: Updates the mode state.
    private void SetMode(PlacementMode mode)
    {
        _mode = mode;
        if (_mode == PlacementMode.StageTopology)
            _paintingEnabled = false;

        SceneView.RepaintAll();
        Repaint();
    }

    // Purpose: Draws the active layer controls authoring UI.
    private void DrawActiveLayerControls()
    {
        EditorGUILayout.LabelField("ACTIVE LAYER", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(GetModeTitle(_mode), EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(BuildFlowValue(_mode), EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(5f);

        if (_mode == PlacementMode.StageTopology)
        {
            EditorGUILayout.HelpBox(
                "Topology is data authoring. It does not use Scene-view placement mode.",
                MessageType.Info);
            return;
        }

        DrawPlacementModeToggle();
        EditorGUILayout.Space(4f);
        _floorSurfaceMask = LayerMaskField(
            new GUIContent(
                "Floor Surface Mask",
                "Only non-trigger Colliders on these layers can receive Scene-view placement clicks. SectorFloor is selected by default."),
            _floorSurfaceMask);
        EditorGUILayout.Space(6f);
        DrawRotationSettings();
        EditorGUILayout.Space(5f);
        DrawPlacementGuidance();
    }

    // Purpose: Draws the active layer settings authoring UI.
    private void DrawActiveLayerSettings()
    {
        switch (_mode)
        {
            case PlacementMode.FixedSectorStructure:
                DrawFixedStructureSettings();
                break;

            case PlacementMode.EnemySpawnPoint:
                DrawEnemySpawnPointSettings();
                break;

            case PlacementMode.ContentSlotAnchor:
                DrawContentSlotSettings();
                break;

            case PlacementMode.StageTopology:
                DrawTopologySettings();
                break;
        }
    }

    // Purpose: Returns the current active layer instruction.
    private string GetActiveLayerInstruction()
    {
        return _mode switch
        {
            PlacementMode.FixedSectorStructure => "Place permanent objects below the room's SectorObjectContainer.",
            PlacementMode.EnemySpawnPoint => "Place positions used by encounter waves at runtime.",
            PlacementMode.ContentSlotAnchor => "Place runtime positions for loot containers and special interactables, then assign candidates in the room content profile.",
            PlacementMode.StageTopology => "Author the logical stage route between rooms. This has no Scene-view object placement.",
            _ => string.Empty
        };
    }

    // Purpose: Draws the placement guidance authoring UI.
    private void DrawPlacementGuidance()
    {
        if (!_paintingEnabled)
        {
            EditorGUILayout.HelpBox(
                "Placement Mode is OFF. Scene-view navigation and selection work normally. Enter Placement Mode when ready.",
                MessageType.Info);
            return;
        }

        if (!TryValidateConfiguration(out string validationMessage))
        {
            EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
            return;
        }

        string action = _mode switch
        {
            PlacementMode.FixedSectorStructure => "place the selected static object prefab",
            PlacementMode.EnemySpawnPoint => "create an enemy spawn point",
            _ => "create a loot / interactable position"
        };
        EditorGUILayout.HelpBox(
            $"Ready: left-click a valid floor to {action}. Q/E rotates by Rotation Step, Shift+Q/E rotates 45 degrees, R randomizes Y rotation, Ctrl+Z undoes placement.",
            MessageType.Info);
    }

    // Purpose: Draws the placement mode toggle authoring UI.
    private void DrawPlacementModeToggle()
    {
        GUIContent buttonContent = _paintingEnabled
            ? new GUIContent(
                "Exit Placement Mode",
                "Stops placement immediately and returns full Scene-view navigation and selection control.")
            : new GUIContent(
                "Enter Placement Mode",
                "Enables placement preview and left-click placement on valid floor Colliders.");

        Color previousBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = _paintingEnabled
            ? new Color(1f, 0.55f, 0.35f)
            : new Color(0.45f, 0.9f, 0.55f);

        bool nextPaintingEnabled = GUILayout.Toggle(
            _paintingEnabled,
            buttonContent,
            "Button",
            GUILayout.Height(32f));

        GUI.backgroundColor = previousBackgroundColor;

        if (nextPaintingEnabled == _paintingEnabled)
            return;

        _paintingEnabled = nextPaintingEnabled;
        SceneView.RepaintAll();
        Repaint();
    }

    // Purpose: Draws the fixed structure settings authoring UI.
    private void DrawFixedStructureSettings()
    {
        EditorGUILayout.LabelField("Static Object", EditorStyles.boldLabel);

        _structurePrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent(
                "Object Prefab",
                "Prefab placed permanently in this room: obstacle, decoration, fixed loot object, or a static interactable. This is not a random runtime-content entry."),
            _structurePrefab,
            typeof(GameObject),
            false);

        using (new EditorGUI.DisabledScope(!TryGetSelectedPrefab(out _)))
        {
            if (GUILayout.Button("Use Selected Project Prefab") &&
                TryGetSelectedPrefab(out GameObject selectedPrefab))
            {
                _structurePrefab = selectedPrefab;
            }
        }

        _sectorObjectParent = (Transform)EditorGUILayout.ObjectField(
            new GUIContent(
                "Object Parent",
                "Permanent objects are created directly below this Transform. The active room resolves SectorObjectContainer automatically when present."),
            _sectorObjectParent,
            typeof(Transform),
            true);

        if (GUILayout.Button("Use Selected Hierarchy Object As Parent") &&
            Selection.activeTransform != null)
        {
            _sectorObjectParent = Selection.activeTransform;
        }

        _structureBottomOffset = EditorGUILayout.FloatField(
            new GUIContent(
                "Bottom Above Floor",
                "The lowest Renderer bound of the placed structure is aligned to the clicked floor height plus this value. Default 0.3 keeps the structure slightly above the floor."),
            _structureBottomOffset);

        EditorGUILayout.HelpBox(
            "The object is stored as a fixed room child. Its visual bottom, not its pivot, is aligned to Floor Hit Y + Bottom Above Floor.",
            MessageType.None);
    }

    // Purpose: Draws the enemy spawn point settings authoring UI.
    private void DrawEnemySpawnPointSettings()
    {
        EditorGUILayout.LabelField("Enemy Spawn Point", EditorStyles.boldLabel);

        _enemySpawnPointParent = (Transform)EditorGUILayout.ObjectField(
            new GUIContent(
                "Enemy Spawn Point Parent",
                "Dedicated authoring parent that will contain every EnemySpawnPoint marker for this sector. Do not use the runtime spawned-enemy container."),
            _enemySpawnPointParent,
            typeof(Transform),
            true);

        if (GUILayout.Button("Use Selected Hierarchy Object As Parent") &&
            Selection.activeTransform != null)
        {
            _enemySpawnPointParent = Selection.activeTransform;
            TryAutoResolveSectorRuntime();
        }

        TryAutoResolveSectorRuntime();

        using (new EditorGUI.DisabledScope(_activeSectorRuntime == null && _sectorRuntime == null))
        {
            if (GUILayout.Button("Create Spawn Point Parent In Active Room"))
                CreateEnemySpawnPointParent();
        }

        _sectorRuntime = (SectorRuntime)EditorGUILayout.ObjectField(
            new GUIContent(
                "Sector Runtime",
                "The parent above is automatically registered as this SectorRuntime's Enemy Spawn Point Root whenever a point is placed."),
            _sectorRuntime,
            typeof(SectorRuntime),
            true);

        _enemySpawnPointYOffset = EditorGUILayout.FloatField(
            new GUIContent(
                "Point Above Floor",
                "EnemySpawnPoint world Y is the clicked floor height plus this value. Default 0.3 makes the spawn position easy to see and fine-tune."),
            _enemySpawnPointYOffset);

        bool showExistingPoints = EditorGUILayout.Toggle(
            new GUIContent(
                "Show Existing Points",
                "Shows numbered pins and facing arrows for every EnemySpawnPoint below the assigned parent. This overlay remains visible while Placement Mode is OFF."),
            _showExistingEnemySpawnPoints);

        if (showExistingPoints != _showExistingEnemySpawnPoints)
        {
            _showExistingEnemySpawnPoints = showExistingPoints;
            SceneView.RepaintAll();
        }

        int totalPointCount = 0;
        int enabledPointCount = 0;

        if (_enemySpawnPointParent != null)
        {
            EnemySpawnPoint[] existingPoints =
                _enemySpawnPointParent.GetComponentsInChildren<EnemySpawnPoint>(true);

            totalPointCount = existingPoints.Length;

            for (int i = 0; i < existingPoints.Length; i++)
            {
                if (existingPoints[i] != null &&
                    existingPoints[i].EnabledForSpawning)
                {
                    enabledPointCount++;
                }
            }
        }

        EditorGUILayout.LabelField(
            "Existing Spawn Points",
            $"{enabledPointCount} enabled / {totalPointCount} total");

        if (_sectorRuntime != null && _enemySpawnPointParent != null)
        {
            bool alreadyRegistered =
                _sectorRuntime.EnemySpawnPointRoot == _enemySpawnPointParent;

            EditorGUILayout.HelpBox(
                alreadyRegistered
                    ? "Enemy Spawn Point Parent is registered on SectorRuntime."
                    : "The parent will be registered on SectorRuntime when the first point is placed.",
                MessageType.None);
        }
    }

    // Purpose: Draws the content slot settings authoring UI.
    private void DrawContentSlotSettings()
    {
        EditorGUILayout.LabelField("Loot / Interactable Positions", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Paint positions where the room may spawn a loot container, Rare Cache, mini shop, NPC, or special interactable. This creates the position now; candidates and chance belong to the room Content Profile.",
            MessageType.Info);

        _contentSlotParent = (Transform)EditorGUILayout.ObjectField(
            new GUIContent(
                "Position Parent",
                "Dedicated authored parent for loot/interactable position markers. It must be below the active SectorRuntime, not a runtime-spawned object container."),
            _contentSlotParent,
            typeof(Transform),
            true);

        if (GUILayout.Button("Use Selected Hierarchy Object As Parent") &&
            Selection.activeTransform != null)
        {
            _contentSlotParent = Selection.activeTransform;
            TryAutoResolveContentSectorRuntime();
        }

        TryAutoResolveContentSectorRuntime();

        using (new EditorGUI.DisabledScope(_activeSectorRuntime == null && _contentSectorRuntime == null))
        {
            if (GUILayout.Button("Create Loot Position Parent In Active Room"))
                CreateContentSlotParent();
        }

        _contentSectorRuntime = (SectorRuntime)EditorGUILayout.ObjectField(
            new GUIContent(
                "Sector Runtime",
                "Used only to verify that Content Slot Parent belongs to this sector prefab."),
            _contentSectorRuntime,
            typeof(SectorRuntime),
            true);

        EditorGUI.BeginChangeCheck();
        SectorPresetSO selectedPreset = (SectorPresetSO)EditorGUILayout.ObjectField(
            new GUIContent(
                "Room Preset",
                "Optional room preset that will reference this Content Profile at runtime."),
            _contentSectorPreset,
            typeof(SectorPresetSO),
            false);
        if (EditorGUI.EndChangeCheck())
        {
            _contentSectorPreset = selectedPreset;
            if (_contentSectorPreset != null)
                _contentProfile = _contentSectorPreset.ContentProfile;
        }

        _contentProfile = (SectorContentProfileSO)EditorGUILayout.ObjectField(
            new GUIContent(
                "Content Profile",
                "Contains the matching slot ID, chance and candidate prefab rows. The same profile can be reused by compatible Normal Sector presets."),
            _contentProfile,
            typeof(SectorContentProfileSO),
            false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Content Plan"))
                CreateContentProfileAsset();

            using (new EditorGUI.DisabledScope(_contentProfile == null))
            {
                if (GUILayout.Button("Select Profile"))
                    Selection.activeObject = _contentProfile;
            }

            using (new EditorGUI.DisabledScope(_contentSectorPreset == null || _contentProfile == null))
            {
                if (GUILayout.Button("Apply To Room Preset"))
                    ApplyContentProfileToPreset();
            }
        }

        _contentSlotId = EditorGUILayout.TextField(
            new GUIContent(
                "New Position ID",
                "Stable profile key. Keep it descriptive and unique: loot_a, rare_cache_a, special_npc_a."),
            _contentSlotId);
        _contentSlotKind = (SectorContentSlotKind)EditorGUILayout.EnumPopup(
            new GUIContent(
                "Content Type",
                "Semantic label matched against the Content Profile row."),
            _contentSlotKind);
        _contentSlotYOffset = EditorGUILayout.FloatField(
            new GUIContent(
                "Point Above Floor",
                "World-space height added above the clicked floor for this anchor marker."),
            _contentSlotYOffset);

        bool showExistingSlots = EditorGUILayout.Toggle(
            new GUIContent(
                "Show Existing Slots",
                "Shows colored labels for content anchors below Content Slot Parent, even while placement mode is off."),
            _showExistingContentSlots);
        if (showExistingSlots != _showExistingContentSlots)
        {
            _showExistingContentSlots = showExistingSlots;
            SceneView.RepaintAll();
        }

        int anchorCount = 0;
        if (_contentSlotParent != null)
            anchorCount = _contentSlotParent.GetComponentsInChildren<SectorContentSlotAnchor>(true).Length;

        int profileCount = _contentProfile != null
            ? _contentProfile.SlotDefinitions.Count
            : 0;
        EditorGUILayout.LabelField("Authored Content Slots", $"{anchorCount} anchors / {profileCount} profile rows");

        if (_contentProfile == null)
        {
            EditorGUILayout.HelpBox(
                "You may paint anchors without a profile, but they will be unconfigured until a matching Content Profile row is added.",
                MessageType.Warning);
        }
        else if (_contentSectorPreset != null &&
                 _contentSectorPreset.ContentProfile != _contentProfile)
        {
            EditorGUILayout.HelpBox(
                "Content Profile is not yet assigned to the selected Sector Preset. Press Apply To Preset before play-mode testing.",
                MessageType.Warning);
        }
    }

    // Purpose: Creates a dedicated spawn-point marker parent below the selected active room.
    private void CreateEnemySpawnPointParent()
    {
        SectorRuntime targetRuntime = _activeSectorRuntime != null
            ? _activeSectorRuntime
            : _sectorRuntime;
        if (targetRuntime == null)
        {
            ShowNotification(new GUIContent("Select an Active Room Root first."));
            return;
        }

        Transform existingParent = targetRuntime.EnemySpawnPointRoot;
        if (existingParent == null)
            existingParent = FindChildByName(targetRuntime.transform, "EnemySpawnPoints");

        if (existingParent == null)
        {
            GameObject createdParent = new("EnemySpawnPoints");
            Undo.RegisterCreatedObjectUndo(createdParent, "Create Enemy Spawn Point Parent");
            createdParent.transform.SetParent(targetRuntime.transform, worldPositionStays: false);
            existingParent = createdParent.transform;
            MarkSceneDirty(createdParent);
        }

        Undo.RecordObject(targetRuntime, "Assign Enemy Spawn Point Parent");
        targetRuntime.SetEnemySpawnPointRoot(existingParent);
        EditorUtility.SetDirty(targetRuntime);
        PrefabUtility.RecordPrefabInstancePropertyModifications(targetRuntime);

        _activeSectorRuntime = targetRuntime;
        _sectorRuntime = targetRuntime;
        _enemySpawnPointParent = existingParent;
        ShowNotification(new GUIContent("Enemy Spawn Points parent is ready."));
    }

    // Purpose: Creates a dedicated loot/interactable marker parent below the selected active room.
    private void CreateContentSlotParent()
    {
        SectorRuntime targetRuntime = _activeSectorRuntime != null
            ? _activeSectorRuntime
            : _contentSectorRuntime;
        if (targetRuntime == null)
        {
            ShowNotification(new GUIContent("Select an Active Room Root first."));
            return;
        }

        Transform existingParent = FindChildByName(targetRuntime.transform, "ContentSlots");
        if (existingParent == null)
        {
            Transform roomContainer = FindChildByName(targetRuntime.transform, "Container");
            GameObject createdParent = new("ContentSlots");
            Undo.RegisterCreatedObjectUndo(createdParent, "Create Loot Position Parent");
            createdParent.transform.SetParent(
                roomContainer != null ? roomContainer : targetRuntime.transform,
                worldPositionStays: false);
            existingParent = createdParent.transform;
            MarkSceneDirty(createdParent);
        }

        _activeSectorRuntime = targetRuntime;
        _contentSectorRuntime = targetRuntime;
        _contentSlotParent = existingParent;
        ShowNotification(new GUIContent("Loot / Interactable Positions parent is ready."));
    }

    // Purpose: Creates the content profile asset owned by this workflow.
    private void CreateContentProfileAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Normal Sector Content Profile",
            "NormalSectorContentProfile",
            "asset",
            "Choose the asset path for this reusable Normal Sector content profile.");
        if (string.IsNullOrWhiteSpace(path))
            return;

        SectorContentProfileSO created = CreateInstance<SectorContentProfileSO>();
        AssetDatabase.CreateAsset(created, path);
        AssetDatabase.SaveAssets();
        _contentProfile = created;
        Selection.activeObject = created;
        Repaint();
    }

    // Purpose: Applies the requested content profile to preset change.
    private void ApplyContentProfileToPreset()
    {
        if (_contentSectorPreset == null || _contentProfile == null)
            return;

        Undo.RecordObject(_contentSectorPreset, "Assign Sector Content Profile");
        _contentSectorPreset.SetContentProfile(_contentProfile);
        EditorUtility.SetDirty(_contentSectorPreset);
    }

    // Purpose: Draws the topology settings authoring UI.
    private void DrawTopologySettings()
    {
        EditorGUILayout.LabelField("Branch & Merge Stage Topology", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Create or select a topology asset. 2. Keep Entry as a Normal room adjacent to Start (default Start is -1, 0, so Entry 0, 0 works). 3. Add Normal rooms orthogonally. 4. Mark exactly one room as Boss. Adjacent cells connect automatically at runtime.",
            MessageType.None);

        _topology = (StageTopologySO)EditorGUILayout.ObjectField(
            new GUIContent(
                "Topology Asset",
                "Reusable logical room graph. It contains no prefab references; Stage Definition still chooses Normal and Boss sector pools."),
            _topology,
            typeof(StageTopologySO),
            false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Topology"))
                CreateTopologyAsset();

            using (new EditorGUI.DisabledScope(_topology == null))
            {
                if (GUILayout.Button("Select Asset"))
                    Selection.activeObject = _topology;
            }
        }

        if (_topology == null)
        {
            EditorGUILayout.HelpBox(
                "Create a Branch Topology asset first. Existing stages remain full-grid until this asset is explicitly added to a Stage Definition.",
                MessageType.Info);
            return;
        }

        _topologyNodeCoord = EditorGUILayout.Vector2IntField(
            new GUIContent(
                "Edit Room Coord",
                "Logical room coordinate. Click a cell in the preview to load it here."),
            _topologyNodeCoord);
        _topologyNodeIsBoss = EditorGUILayout.Toggle(
            new GUIContent(
                "Boss Goal Room",
                "Enable only for the one final Boss room. All other rooms are Normal Sector rooms."),
            _topologyNodeIsBoss);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add / Update Room"))
            {
                Undo.RecordObject(_topology, "Edit Branch Topology Room");
                _topology.UpsertRoom(
                    _topologyNodeCoord,
                    _topologyNodeIsBoss ? StageRoomType.Boss : StageRoomType.Normal);
                EditorUtility.SetDirty(_topology);
            }

            if (GUILayout.Button("Set Entry Here"))
            {
                Undo.RecordObject(_topology, "Set Branch Topology Entry");
                _topology.SetEntryRoomCoord(_topologyNodeCoord);
                EditorUtility.SetDirty(_topology);
            }

            if (GUILayout.Button("Remove Room"))
            {
                Undo.RecordObject(_topology, "Remove Branch Topology Room");
                if (_topology.RemoveRoom(_topologyNodeCoord))
                    EditorUtility.SetDirty(_topology);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Replace With Branch Sample"))
            {
                if (EditorUtility.DisplayDialog(
                        "Replace Branch Topology",
                        "Replace every room in this topology with the six-room branch-and-merge sample? The topology asset can be restored with Undo.",
                        "Replace",
                        "Cancel"))
                {
                    Undo.RecordObject(_topology, "Create Branch Topology Sample");
                    _topology.ReplaceWithBranchingExample();
                    EditorUtility.SetDirty(_topology);
                }
            }

            if (GUILayout.Button("Validate Topology"))
            {
                if (_topology.TryValidate(out string error))
                    ShowNotification(new GUIContent("Topology is valid."));
                else
                    ShowNotification(new GUIContent(error));
            }
        }

        DrawTopologyPreview();

        if (_topology.TryValidate(out string topologyError))
        {
            EditorGUILayout.HelpBox(
                $"Valid: {_topology.RoomCount} rooms, entry {_topology.EntryRoomCoord}. The actual Start Sector coordinate is set on StageSectorInstantiator and must be directly adjacent to this entry.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"Invalid topology: {topologyError}", MessageType.Error);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Assign To Stage", EditorStyles.boldLabel);
        _topologyStageDefinition = (StageDefinitionSO)EditorGUILayout.ObjectField(
            new GUIContent(
                "Stage Definition",
                "Optional target. Press Add / Update to put this topology into the stage's weighted Branch Topologies list."),
            _topologyStageDefinition,
            typeof(StageDefinitionSO),
            false);
        _topologyWeight = EditorGUILayout.IntField(
            new GUIContent(
                "Topology Weight",
                "Positive weight enables deterministic selection. Set zero to keep a WIP topology registered but disabled."),
            Mathf.Max(0, _topologyWeight));

        using (new EditorGUI.DisabledScope(_topologyStageDefinition == null))
        {
            if (GUILayout.Button("Add / Update In Stage Definition"))
            {
                Undo.RecordObject(_topologyStageDefinition, "Assign Branch Topology To Stage");
                _topologyStageDefinition.AddOrUpdateBranchTopology(_topology, _topologyWeight);
                EditorUtility.SetDirty(_topologyStageDefinition);
            }
        }
    }

    // Purpose: Creates the topology asset owned by this workflow.
    private void CreateTopologyAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Stage Branch Topology",
            "StageBranchTopology",
            "asset",
            "Choose an asset path for this reusable branch-and-merge room graph.");
        if (string.IsNullOrWhiteSpace(path))
            return;

        StageTopologySO created = CreateInstance<StageTopologySO>();
        created.ReplaceWithBranchingExample();
        AssetDatabase.CreateAsset(created, path);
        AssetDatabase.SaveAssets();
        _topology = created;
        Selection.activeObject = created;
        Repaint();
    }

    // Purpose: Draws the topology preview authoring UI.
    private void DrawTopologyPreview()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Topology Preview", EditorStyles.boldLabel);

        if (!_topology.TryGetBounds(out Vector2Int min, out Vector2Int max))
        {
            EditorGUILayout.HelpBox("No rooms have been added yet.", MessageType.None);
            return;
        }

        const int maxPreviewCellsPerAxis = 12;
        int width = max.x - min.x + 1;
        int height = max.y - min.y + 1;
        if (width > maxPreviewCellsPerAxis || height > maxPreviewCellsPerAxis)
        {
            EditorGUILayout.HelpBox(
                $"Preview bounds are {width} x {height}. Keep authoring coordinates compact; the interactive preview is limited to {maxPreviewCellsPerAxis} cells per axis.",
                MessageType.Warning);
            return;
        }

        Color previousColor = GUI.backgroundColor;
        for (int y = max.y; y >= min.y; y--)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(y.ToString(), GUILayout.Width(24f));
                for (int x = min.x; x <= max.x; x++)
                {
                    Vector2Int coord = new(x, y);
                    bool hasRoom = _topology.TryGetRoom(coord, out StageTopologyRoom room);
                    bool isEntry = coord == _topology.EntryRoomCoord;
                    GUI.backgroundColor = !hasRoom
                        ? new Color(0.24f, 0.24f, 0.24f)
                        : room.roomType == StageRoomType.Boss
                            ? new Color(0.82f, 0.32f, 0.32f)
                            : isEntry
                                ? new Color(0.32f, 0.76f, 0.45f)
                                : new Color(0.25f, 0.57f, 0.82f);

                    string label = !hasRoom
                        ? string.Empty
                        : room.roomType == StageRoomType.Boss
                            ? "B"
                            : isEntry ? "E" : "N";
                    if (GUILayout.Button(label, GUILayout.Width(34f), GUILayout.Height(28f)))
                    {
                        _topologyNodeCoord = coord;
                        _topologyNodeIsBoss = hasRoom && room.roomType == StageRoomType.Boss;
                        Repaint();
                    }
                }
            }
        }

        GUI.backgroundColor = previousColor;
        EditorGUILayout.LabelField($"x: {min.x} to {max.x}     E=Entry, N=Normal, B=Boss", EditorStyles.miniLabel);
    }

    // Purpose: Draws the rotation settings authoring UI.
    private void DrawRotationSettings()
    {
        EditorGUILayout.LabelField("Placement Rotation", EditorStyles.boldLabel);

        _rotationY = EditorGUILayout.FloatField(
            new GUIContent(
                "Y Rotation",
                "World-space Y rotation applied to the structure or used as the enemy's initial facing direction."),
            _rotationY);

        _rotationStep = EditorGUILayout.FloatField(
            new GUIContent(
                "Rotation Step",
                "Degrees added or removed by the Scene-view Q/E shortcuts."),
            _rotationStep);

        _selectCreatedObject = EditorGUILayout.Toggle(
            new GUIContent(
                "Select Created Object",
                "Select each created structure or point. Leave disabled for rapid repeated painting."),
            _selectCreatedObject);
    }

    // Purpose: Handles the incoming scene gui request or event.
    private void HandleSceneGUI(SceneView sceneView)
    {
        Event current = Event.current;

        if (current == null)
            return;

        if (current.type == EventType.Repaint)
        {
            DrawExistingEnemySpawnPoints();
            DrawExistingContentSlotAnchors();
        }

        if (!_paintingEnabled ||
            !TryValidateConfiguration(out _))
        {
            return;
        }

        if (HandleKeyboard(current))
            sceneView.Repaint();

        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        if (current.type == EventType.Layout)
            HandleUtility.AddDefaultControl(controlId);

        if (current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
            return;
        }

        bool isRepaint = current.type == EventType.Repaint;
        bool isPlacementClick =
            current.type == EventType.MouseDown &&
            current.button == 0 &&
            !current.alt;

        if (!isRepaint && !isPlacementClick)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);

        if (TryGetFloorHit(ray, out RaycastHit hit))
        {
            if (isRepaint)
                DrawPreview(hit.point);

            if (isPlacementClick)
            {
                PlaceAt(hit.point);
                current.Use();
            }
        }
        else
        {
            if (isRepaint)
                DrawNoFloorHit(current.mousePosition);

            if (isPlacementClick)
            {
                sceneView.ShowNotification(
                    new GUIContent(
                        "No valid floor hit. Check the Collider and Floor Surface Mask."),
                    1.5d);
                current.Use();
            }
        }
    }

    // Purpose: Attempts to get floor hit and reports whether the request succeeded.
    private bool TryGetFloorHit(Ray ray, out RaycastHit floorHit)
    {
        const float maxDistance = 10000f;
        floorHit = default;
        float nearestDistance = maxDistance;
        bool found = false;

        StageHandle currentStage = StageUtility.GetCurrentStageHandle();

        if (!currentStage.IsValid())
            return false;

        // Physics.RaycastAll only queries the default PhysicsScene. Prefab Mode
        // uses a separate Stage, so query its Colliders directly instead.
        Collider[] colliders = currentStage.FindComponentsOfType<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null ||
                !collider.enabled ||
                !collider.gameObject.activeInHierarchy ||
                collider.isTrigger)
            {
                continue;
            }

            int layerBit = 1 << collider.gameObject.layer;

            if ((_floorSurfaceMask.value & layerBit) == 0)
                continue;

            if (_sectorObjectParent != null &&
                collider.transform.IsChildOf(_sectorObjectParent))
            {
                continue;
            }

            if (!collider.Raycast(ray, out RaycastHit candidate, nearestDistance))
                continue;

            nearestDistance = candidate.distance;
            floorHit = candidate;
            found = true;
        }

        return found;
    }

    // Purpose: Draws the no floor hit authoring UI.
    private static void DrawNoFloorHit(Vector2 mousePosition)
    {
        Handles.BeginGUI();

        Rect messageRect = new(
            mousePosition.x + 16f,
            mousePosition.y + 16f,
            300f,
            42f);

        GUI.Label(
            messageRect,
            "No Floor Hit\nPoint at an enabled, non-trigger Collider on Floor Surface Mask.",
            EditorStyles.helpBox);

        Handles.EndGUI();
    }

    // Purpose: Draws the existing enemy spawn points authoring UI.
    private void DrawExistingEnemySpawnPoints()
    {
        if (_mode != PlacementMode.EnemySpawnPoint ||
            !_showExistingEnemySpawnPoints ||
            _enemySpawnPointParent == null ||
            !IsInCurrentStage(_enemySpawnPointParent))
        {
            return;
        }

        EnemySpawnPoint[] points =
            _enemySpawnPointParent.GetComponentsInChildren<EnemySpawnPoint>(true);

        if (points.Length == 0)
            return;

        GUIStyle labelStyle = GetSpawnPointLabelStyle();
        Color previousHandlesColor = Handles.color;
        UnityEngine.Rendering.CompareFunction previousZTest = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        for (int i = 0; i < points.Length; i++)
        {
            EnemySpawnPoint point = points[i];

            if (point == null)
                continue;

            bool enabledForSpawning = point.EnabledForSpawning;
            Color markerColor = enabledForSpawning
                ? new Color(1f, 0.12f, 0.08f, 0.95f)
                : new Color(0.55f, 0.55f, 0.55f, 0.8f);

            Vector3 position = point.transform.position;
            float handleSize = HandleUtility.GetHandleSize(position);
            float discRadius = handleSize * 0.1f;
            Vector3 markerTop = position + Vector3.up * (handleSize * 0.25f);

            Handles.color = new Color(
                markerColor.r,
                markerColor.g,
                markerColor.b,
                0.2f);
            Handles.DrawSolidDisc(position, Vector3.up, discRadius);

            Handles.color = markerColor;
            Handles.DrawWireDisc(position, Vector3.up, discRadius);
            Handles.DrawLine(position, markerTop, 3f);
            Handles.SphereHandleCap(
                0,
                markerTop,
                Quaternion.identity,
                handleSize * 0.08f,
                EventType.Repaint);
            Handles.ArrowHandleCap(
                0,
                position,
                point.transform.rotation,
                handleSize * 0.32f,
                EventType.Repaint);

            labelStyle.normal.textColor = markerColor;

            string label = enabledForSpawning
                ? $"SP {i + 1:00}"
                : $"SP {i + 1:00} OFF";

            Handles.Label(
                markerTop + Vector3.up * (handleSize * 0.05f),
                label,
                labelStyle);
        }

        Handles.color = previousHandlesColor;
        Handles.zTest = previousZTest;
    }

    // Purpose: Returns the current spawn point label style.
    private GUIStyle GetSpawnPointLabelStyle()
    {
        if (_spawnPointLabelStyle != null)
            return _spawnPointLabelStyle;

        _spawnPointLabelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11
        };

        return _spawnPointLabelStyle;
    }

    // Purpose: Draws the existing content slot anchors authoring UI.
    private void DrawExistingContentSlotAnchors()
    {
        if (_mode != PlacementMode.ContentSlotAnchor ||
            !_showExistingContentSlots ||
            _contentSlotParent == null ||
            !IsInCurrentStage(_contentSlotParent))
        {
            return;
        }

        SectorContentSlotAnchor[] anchors =
            _contentSlotParent.GetComponentsInChildren<SectorContentSlotAnchor>(true);
        if (anchors.Length == 0)
            return;

        GUIStyle labelStyle = GetContentSlotLabelStyle();
        Color previousHandlesColor = Handles.color;
        UnityEngine.Rendering.CompareFunction previousZTest = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        for (int i = 0; i < anchors.Length; i++)
        {
            SectorContentSlotAnchor anchor = anchors[i];
            if (anchor == null)
                continue;

            Color markerColor = GetContentSlotColor(anchor.ContentKind, anchor.EnabledForRuntime);
            Vector3 position = anchor.transform.position;
            float handleSize = HandleUtility.GetHandleSize(position);
            float radius = handleSize * 0.11f;
            Vector3 markerTop = position + Vector3.up * (handleSize * 0.22f);

            Handles.color = new Color(markerColor.r, markerColor.g, markerColor.b, 0.18f);
            Handles.DrawSolidDisc(position, Vector3.up, radius);
            Handles.color = markerColor;
            Handles.DrawWireDisc(position, Vector3.up, radius);
            Handles.DrawLine(position, markerTop, 2.5f);
            Handles.RectangleHandleCap(
                0,
                markerTop,
                Quaternion.Euler(90f, 0f, 0f),
                handleSize * 0.14f,
                EventType.Repaint);

            labelStyle.normal.textColor = markerColor;
            string state = anchor.EnabledForRuntime ? string.Empty : " OFF";
            Handles.Label(
                markerTop + Vector3.up * (handleSize * 0.05f),
                $"{GetContentSlotShortName(anchor.ContentKind)} {anchor.SlotId}{state}",
                labelStyle);
        }

        Handles.color = previousHandlesColor;
        Handles.zTest = previousZTest;
    }

    // Purpose: Returns the current content slot label style.
    private GUIStyle GetContentSlotLabelStyle()
    {
        if (_contentSlotLabelStyle != null)
            return _contentSlotLabelStyle;

        _contentSlotLabelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11
        };
        return _contentSlotLabelStyle;
    }

    // Purpose: Returns the current content slot color.
    private static Color GetContentSlotColor(
        SectorContentSlotKind kind,
        bool enabledForRuntime)
    {
        if (!enabledForRuntime)
            return new Color(0.55f, 0.55f, 0.55f, 0.8f);

        return kind switch
        {
            SectorContentSlotKind.RareCache => new Color(1f, 0.72f, 0.08f, 0.95f),
            SectorContentSlotKind.SpecialInteractable => new Color(0.82f, 0.28f, 1f, 0.95f),
            _ => new Color(0.18f, 0.86f, 0.72f, 0.95f)
        };
    }

    // Purpose: Returns the current content slot short name.
    private static string GetContentSlotShortName(SectorContentSlotKind kind)
    {
        return kind switch
        {
            SectorContentSlotKind.RareCache => "RARE",
            SectorContentSlotKind.SpecialInteractable => "SPECIAL",
            _ => "LOOT"
        };
    }

    // Purpose: Handles the incoming keyboard request or event.
    private bool HandleKeyboard(Event current)
    {
        if (current.type != EventType.KeyDown)
            return false;

        float step = current.shift
            ? 45f
            : Mathf.Max(0.01f, _rotationStep);

        if (current.keyCode == KeyCode.Q)
        {
            _rotationY -= step;
            current.Use();
            Repaint();
            return true;
        }

        if (current.keyCode == KeyCode.E)
        {
            _rotationY += step;
            current.Use();
            Repaint();
            return true;
        }

        if (current.keyCode == KeyCode.R)
        {
            _rotationY = Random.Range(0f, 360f);
            current.Use();
            Repaint();
            return true;
        }

        return false;
    }

    // Purpose: Draws the preview authoring UI.
    private void DrawPreview(Vector3 hitPoint)
    {
        float yOffset = _mode switch
        {
            PlacementMode.FixedSectorStructure => _structureBottomOffset,
            PlacementMode.EnemySpawnPoint => _enemySpawnPointYOffset,
            _ => _contentSlotYOffset
        };

        Vector3 position = hitPoint + Vector3.up * yOffset;

        Handles.color = _mode switch
        {
            PlacementMode.FixedSectorStructure => new Color(0.2f, 0.8f, 1f, 0.95f),
            PlacementMode.EnemySpawnPoint => new Color(1f, 0.1f, 0.1f, 0.95f),
            _ => GetContentSlotColor(_contentSlotKind, enabledForRuntime: true)
        };

        Handles.DrawWireDisc(position, Vector3.up, 0.4f);
        Handles.ArrowHandleCap(
            0,
            position,
            Quaternion.Euler(0f, _rotationY, 0f),
            0.8f,
            EventType.Repaint);

        string label = _mode switch
        {
            PlacementMode.FixedSectorStructure => _structurePrefab.name,
            PlacementMode.EnemySpawnPoint => "Enemy Spawn Point",
            _ => $"{GetContentSlotShortName(_contentSlotKind)} {_contentSlotId}"
        };

        Handles.Label(position + Vector3.up * 0.25f, label);
    }

    // Purpose: Performs the place at workflow for this component.
    private void PlaceAt(Vector3 hitPoint)
    {
        switch (_mode)
        {
            case PlacementMode.FixedSectorStructure:
                CreateFixedStructure(hitPoint);
                break;

            case PlacementMode.EnemySpawnPoint:
                CreateEnemySpawnPoint(hitPoint);
                break;

            case PlacementMode.ContentSlotAnchor:
                CreateContentSlotAnchor(hitPoint);
                break;
        }
    }

    // Purpose: Creates the fixed structure owned by this workflow.
    private void CreateFixedStructure(Vector3 hitPoint)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(
            _structurePrefab,
            _sectorObjectParent) as GameObject;

        if (instance == null)
        {
            Debug.LogError(
                $"[SectorMakerWindow] Failed to instantiate structure prefab. prefab={_structurePrefab.name}",
                _structurePrefab);
            return;
        }

        Undo.RegisterCreatedObjectUndo(instance, $"Place {_structurePrefab.name}");

        instance.transform.SetPositionAndRotation(
            hitPoint,
            Quaternion.Euler(0f, _rotationY, 0f));

        float targetBottomY = hitPoint.y + _structureBottomOffset;

        if (TryCalculateRendererBounds(instance, out Bounds bounds))
        {
            instance.transform.position +=
                Vector3.up * (targetBottomY - bounds.min.y);
        }
        else
        {
            instance.transform.position = new Vector3(
                hitPoint.x,
                targetBottomY,
                hitPoint.z);

            Debug.LogWarning(
                $"[SectorMakerWindow] Structure prefab has no Renderer bounds. Its pivot was placed at the requested floor offset instead. prefab={_structurePrefab.name}",
                instance);
        }

        FinalizeCreatedObject(instance);
    }

    // Purpose: Creates the enemy spawn point owned by this workflow.
    private void CreateEnemySpawnPoint(Vector3 hitPoint)
    {
        if (_sectorRuntime.EnemySpawnPointRoot != _enemySpawnPointParent)
        {
            Undo.RecordObject(_sectorRuntime, "Assign Enemy Spawn Point Root");

            if (!_sectorRuntime.SetEnemySpawnPointRoot(_enemySpawnPointParent))
                return;

            EditorUtility.SetDirty(_sectorRuntime);
            PrefabUtility.RecordPrefabInstancePropertyModifications(_sectorRuntime);
        }

        string objectName = GameObjectUtility.GetUniqueNameForSibling(
            _enemySpawnPointParent,
            "EnemySpawnPoint");

        GameObject marker = new(objectName);
        Undo.RegisterCreatedObjectUndo(marker, "Create Enemy Spawn Point");

        marker.transform.SetParent(_enemySpawnPointParent, worldPositionStays: true);
        marker.transform.SetPositionAndRotation(
            hitPoint + Vector3.up * _enemySpawnPointYOffset,
            Quaternion.Euler(0f, _rotationY, 0f));

        Undo.AddComponent<EnemySpawnPoint>(marker);
        FinalizeCreatedObject(marker);
    }

    // Purpose: Creates the content slot anchor owned by this workflow.
    private void CreateContentSlotAnchor(Vector3 hitPoint)
    {
        string slotId = _contentSlotId != null ? _contentSlotId.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(slotId))
        {
            ShowNotification(new GUIContent("Content Slot ID is required."));
            return;
        }

        if (HasContentSlotAnchorWithId(_contentSlotParent, slotId))
        {
            ShowNotification(new GUIContent("That Content Slot ID already exists below the selected parent."));
            return;
        }

        string objectName = GameObjectUtility.GetUniqueNameForSibling(
            _contentSlotParent,
            $"ContentSlot_{slotId}");

        GameObject marker = new(objectName);
        Undo.RegisterCreatedObjectUndo(marker, "Create Sector Content Slot");

        marker.transform.SetParent(_contentSlotParent, worldPositionStays: true);
        marker.transform.SetPositionAndRotation(
            hitPoint + Vector3.up * _contentSlotYOffset,
            Quaternion.Euler(0f, _rotationY, 0f));

        SectorContentSlotAnchor anchor = Undo.AddComponent<SectorContentSlotAnchor>(marker);
        anchor.ConfigureForAuthoring(slotId, _contentSlotKind);

        if (_contentProfile != null)
        {
            Undo.RecordObject(_contentProfile, "Add Sector Content Slot Definition");
            _contentProfile.GetOrCreateSlotDefinition(slotId, _contentSlotKind);
            EditorUtility.SetDirty(_contentProfile);
        }

        FinalizeCreatedObject(marker);
    }

    // Purpose: Checks whether this component has content slot anchor with id.
    private static bool HasContentSlotAnchorWithId(Transform parent, string slotId)
    {
        if (parent == null || string.IsNullOrWhiteSpace(slotId))
            return false;

        SectorContentSlotAnchor[] anchors =
            parent.GetComponentsInChildren<SectorContentSlotAnchor>(true);

        for (int i = 0; i < anchors.Length; i++)
        {
            SectorContentSlotAnchor anchor = anchors[i];
            if (anchor != null && string.Equals(anchor.SlotId, slotId, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    // Purpose: Attempts to validate configuration and reports whether the request succeeded.
    private bool TryValidateConfiguration(out string message)
    {
        if (_mode == PlacementMode.StageTopology)
        {
            message = "Branch Topology is edited in the Sector Maker window and does not use Scene-view placement.";
            return false;
        }

        if (_floorSurfaceMask.value == 0)
        {
            message = "Select at least one Floor Surface Mask layer. The clicked object also needs a non-trigger Collider.";
            return false;
        }

        if (_mode == PlacementMode.FixedSectorStructure)
        {
            if (_structurePrefab == null)
            {
                message = "Assign Structure Prefab or select a prefab in the Project window and press Use Selected Project Prefab.";
                return false;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(_structurePrefab))
            {
                message = "Structure Prefab must be a Project prefab asset, not a scene object.";
                return false;
            }

            if (_sectorObjectParent == null)
            {
                message = "Assign the sector's SectorObjectContainer as Sector Object Parent.";
                return false;
            }

            if (!IsInCurrentStage(_sectorObjectParent))
            {
                message = "Sector Object Parent must belong to the scene or prefab currently open in the Scene view.";
                return false;
            }
        }
        else if (_mode == PlacementMode.EnemySpawnPoint)
        {
            TryAutoResolveSectorRuntime();

            if (_enemySpawnPointParent == null)
            {
                message = "Assign a dedicated Enemy Spawn Point Parent.";
                return false;
            }

            if (_sectorRuntime == null)
            {
                message = "Assign Sector Runtime. It is normally found automatically from Enemy Spawn Point Parent.";
                return false;
            }

            Transform sectorTransform = _sectorRuntime.transform;

            if (_enemySpawnPointParent != sectorTransform &&
                !_enemySpawnPointParent.IsChildOf(sectorTransform))
            {
                message = "Enemy Spawn Point Parent must be a child of the assigned Sector Runtime.";
                return false;
            }

            if (!IsInCurrentStage(_enemySpawnPointParent))
            {
                message = "Enemy Spawn Point Parent must belong to the scene or prefab currently open in the Scene view.";
                return false;
            }
        }
        else
        {
            TryAutoResolveContentSectorRuntime();

            if (_contentSlotParent == null)
            {
                message = "Assign a dedicated Content Slot Parent below the sector prefab.";
                return false;
            }

            if (_contentSectorRuntime == null)
            {
                message = "Assign Sector Runtime. It is normally found automatically from Content Slot Parent.";
                return false;
            }

            Transform sectorTransform = _contentSectorRuntime.transform;
            if (_contentSlotParent != sectorTransform &&
                !_contentSlotParent.IsChildOf(sectorTransform))
            {
                message = "Content Slot Parent must be a child of the assigned Sector Runtime.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_contentSlotId))
            {
                message = "Enter a stable Content Slot ID before placing anchors.";
                return false;
            }

            if (!IsInCurrentStage(_contentSlotParent))
            {
                message = "Content Slot Parent must belong to the scene or prefab currently open in the Scene view.";
                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    // Purpose: Checks whether in current stage is currently true.
    private static bool IsInCurrentStage(Transform target)
    {
        if (target == null)
            return false;

        StageHandle currentStage = StageUtility.GetCurrentStageHandle();
        return currentStage.IsValid() && currentStage.Contains(target.gameObject);
    }

    // Purpose: Attempts to auto resolve sector runtime and reports whether the request succeeded.
    private void TryAutoResolveSectorRuntime()
    {
        if (_enemySpawnPointParent == null)
            return;

        SectorRuntime parentSector =
            _enemySpawnPointParent.GetComponentInParent<SectorRuntime>();

        if (parentSector != null && _sectorRuntime != parentSector)
            _sectorRuntime = parentSector;
    }

    // Purpose: Attempts to auto resolve content sector runtime and reports whether the request succeeded.
    private void TryAutoResolveContentSectorRuntime()
    {
        if (_contentSlotParent == null)
            return;

        SectorRuntime parentSector =
            _contentSlotParent.GetComponentInParent<SectorRuntime>();

        if (parentSector != null && _contentSectorRuntime != parentSector)
            _contentSectorRuntime = parentSector;
    }

    // Purpose: Performs the finalize created object workflow for this component.
    private void FinalizeCreatedObject(GameObject instance)
    {
        if (instance == null)
            return;

        if (_selectCreatedObject)
            Selection.activeGameObject = instance;

        EditorUtility.SetDirty(instance);
        MarkSceneDirty(instance);
    }

    // Purpose: Attempts to calculate renderer bounds and reports whether the request succeeded.
    private static bool TryCalculateRendererBounds(
        GameObject instance,
        out Bounds bounds)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    // Purpose: Attempts to get selected prefab and reports whether the request succeeded.
    private static bool TryGetSelectedPrefab(out GameObject prefab)
    {
        prefab = Selection.activeObject as GameObject;
        return prefab != null && PrefabUtility.IsPartOfPrefabAsset(prefab);
    }

    // Purpose: Initializes floor mask for use by this component.
    private void InitializeFloorMask()
    {
        if (_surfaceMaskInitialized)
            return;

        int sectorFloorLayer = LayerMask.NameToLayer("SectorFloor");
        _floorSurfaceMask = sectorFloorLayer >= 0
            ? 1 << sectorFloorLayer
            : ~0;
        _surfaceMaskInitialized = true;
    }

    // Purpose: Performs the mark scene dirty workflow for this component.
    private static void MarkSceneDirty(GameObject instance)
    {
        if (instance == null || !instance.scene.IsValid())
            return;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instance.scene);
    }

    // Purpose: Performs the layer mask field workflow for this component.
    private static LayerMask LayerMaskField(GUIContent label, LayerMask selected)
    {
        string[] layerNames = UnityEditorInternal.InternalEditorUtility.layers;
        int maskWithoutEmpty = 0;

        for (int i = 0; i < layerNames.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layerNames[i]);

            if ((selected.value & (1 << layer)) != 0)
                maskWithoutEmpty |= 1 << i;
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(
            label,
            maskWithoutEmpty,
            layerNames);

        int mask = 0;

        for (int i = 0; i < layerNames.Length; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) == 0)
                continue;

            int layer = LayerMask.NameToLayer(layerNames[i]);
            mask |= 1 << layer;
        }

        selected.value = mask;
        return selected;
    }
}
