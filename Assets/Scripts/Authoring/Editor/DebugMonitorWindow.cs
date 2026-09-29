/*
 * Role hierarchy: Authoring > Editor
 * Primary responsibility: Provides the DebugMonitorWindow authoring tool and its editor workflow.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;

// Keep this monitor in Assembly-CSharp-Editor so it can inspect both gameplay types
// and the standalone state-machine assembly without creating a circular reference.
public sealed partial class DebugMonitorWindow : EditorWindow
{
    private enum MonitorScope
    {
        AllCharacters,
        CurrentSector
    }

    private enum CharacterFilter
    {
        All,
        Player,
        Enemy
    }

    private sealed class CharacterRow
    {
        public int id;
        public Transform root;
        public Damageable damageable;
        public PlayerCharacter player;
        public Enemy enemy;
        public EnemyShooter enemyShooter;
        public EnemyShooterCombatController combat;
        public EntityVisionController vision;
        public ShooterStatsRuntime stats;
        public ShooterEquipmentController equipment;
        public EntityEquipmentRuntime equipmentRuntime;
        public PlayerInventoryRuntime playerInventory;
        public EnemyLootInventoryRuntime lootInventory;
        public ShooterInfection infection;
        public EntityPaintMarkController paintMark;
        public EntityStatusEffectRuntime statusEffects;
        public NavMeshAgent agent;
        public SectorRuntime sector;
        public StateMachine[] stateMachines = Array.Empty<StateMachine>();
        public string displayName;
        public string typeName;
        public float distanceToPlayer = -1f;

        public bool IsPlayer => player != null;
        public bool IsEnemy => enemy != null;
        public bool IsDead => damageable != null && damageable.IsDead;
    }

    private const float RefreshIntervalMin = 0.05f;
    private const float RefreshIntervalMax = 2f;
    private const float HeaderHeight = 23f;
    private const float RowHeight = 22f;
    private const float TableWidth = 1750f;

    private readonly List<CharacterRow> _rows = new();
    private readonly List<SectorStateManager.RuntimeSectorStateEntry> _stageSectorStates = new();
    private readonly HashSet<int> _knownEntityRoots = new();
    private readonly StringBuilder _textBuilder = new(256);

    private MonitorScope _scope = MonitorScope.AllCharacters;
    private CharacterFilter _characterFilter = CharacterFilter.All;
    private bool _includeDead = true;
    private bool _autoRefresh = true;
    private bool _drawSceneOverlay = true;
    private bool _overlaySelectedOnly = true;
    private float _refreshInterval = 0.1f;
    private double _nextRefreshTime;
    private string _search = string.Empty;
    private Vector2 _tableScroll;
    private Vector2 _detailScroll;
    private int _selectedRowId = -1;
    private Transform _playerTransform;
    private SectorStateManager _sectorStateManager;
    private StageProgressionManager _stageProgressionManager;
    private SectorRuntime _currentSector;
    private bool _stageMapFoldout = true;
    private bool _teleportCoordInitialized;
    private int _teleportCoordX;
    private int _teleportCoordY;
    private Vector2 _stageMapScroll;
    private string _stageDebugStatus = string.Empty;
    private MessageType _stageDebugStatusType = MessageType.None;

    private GUIStyle _headerStyle;
    private GUIStyle _cellStyle;
    private GUIStyle _mutedCellStyle;
    private GUIStyle _goodCellStyle;
    private GUIStyle _warningCellStyle;
    private GUIStyle _badCellStyle;
    private GUIStyle _sceneLabelStyle;

    [MenuItem("Tools/Debug/Debug Monitor")]
    // Purpose: Opens the window UI or workflow.
    public static void OpenWindow()
    {
        DebugMonitorWindow window = GetWindow<DebugMonitorWindow>();
        window.titleContent = new GUIContent("DebugMonitor");
        window.minSize = new Vector2(900f, 480f);
        window.Show();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        EditorApplication.update += HandleEditorUpdate;
        SceneView.duringSceneGui += HandleSceneGUI;
        _nextRefreshTime = 0d;
        RefreshRows();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        EditorApplication.update -= HandleEditorUpdate;
        SceneView.duringSceneGui -= HandleSceneGUI;
    }

    // Purpose: Handles the incoming editor update request or event.
    private void HandleEditorUpdate()
    {
        if (!_autoRefresh || EditorApplication.timeSinceStartup < _nextRefreshTime)
            return;

        RefreshRows();
        Repaint();

        if (_drawSceneOverlay)
            SceneView.RepaintAll();
    }

    // Purpose: Responds to the gui callback.
    private void OnGUI()
    {
        EnsureStyles();
        DrawToolbar();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "DebugMonitor는 PlayMode 런타임 값을 읽습니다. PlayMode에 들어가면 Player/Enemy가 자동으로 표시됩니다.",
                MessageType.Info);
            return;
        }

        DrawSummary();
        DrawMonitorWorkspace();
    }

    // Purpose: Draws the toolbar authoring UI.
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("범위", GUILayout.Width(30f));
        MonitorScope newScope = (MonitorScope)EditorGUILayout.EnumPopup(
            _scope,
            EditorStyles.toolbarPopup,
            GUILayout.Width(115f));

        GUILayout.Label("대상", GUILayout.Width(30f));
        CharacterFilter newFilter = (CharacterFilter)EditorGUILayout.EnumPopup(
            _characterFilter,
            EditorStyles.toolbarPopup,
            GUILayout.Width(80f));

        bool newIncludeDead = GUILayout.Toggle(
            _includeDead,
            new GUIContent("시체 포함", "사망한 Character도 표에 유지합니다."),
            EditorStyles.toolbarButton,
            GUILayout.Width(70f));

        GUILayout.Space(6f);
        GUILayout.Label("검색", GUILayout.Width(30f));
        string newSearch = GUILayout.TextField(
            _search ?? string.Empty,
            EditorStyles.toolbarSearchField,
            GUILayout.MinWidth(120f),
            GUILayout.MaxWidth(220f));

        GUILayout.FlexibleSpace();

        _autoRefresh = GUILayout.Toggle(
            _autoRefresh,
            new GUIContent("자동 갱신", "설정된 주기로 런타임 상태를 다시 읽습니다."),
            EditorStyles.toolbarButton,
            GUILayout.Width(70f));

        _drawSceneOverlay = GUILayout.Toggle(
            _drawSceneOverlay,
            new GUIContent("Scene 표시", "시야 범위, 공격 거리, Target 선과 상태 라벨을 Scene View에 표시합니다."),
            EditorStyles.toolbarButton,
            GUILayout.Width(75f));

        using (new EditorGUI.DisabledScope(!_drawSceneOverlay))
        {
            _overlaySelectedOnly = GUILayout.Toggle(
                _overlaySelectedOnly,
                new GUIContent("선택만", "선택한 Character만 Scene View에 표시합니다."),
                EditorStyles.toolbarButton,
                GUILayout.Width(55f));
        }

        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(65f)))
            RefreshRows();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("갱신 주기", GUILayout.Width(55f));
        _refreshInterval = EditorGUILayout.Slider(
            _refreshInterval,
            RefreshIntervalMin,
            RefreshIntervalMax,
            GUILayout.Width(240f));
        GUILayout.Label($"{_refreshInterval:0.00}s", GUILayout.Width(45f));
        GUILayout.Space(10f);
        GUILayout.Label(
            "AI가 이미 계산한 Target/시야/전투 결과만 읽습니다. DebugMonitor가 추가 감지 Raycast를 실행하지 않습니다.",
            EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        if (newScope != _scope ||
            newFilter != _characterFilter ||
            newIncludeDead != _includeDead ||
            !string.Equals(newSearch, _search, StringComparison.Ordinal))
        {
            _scope = newScope;
            _characterFilter = newFilter;
            _includeDead = newIncludeDead;
            _search = newSearch;
            Repaint();

            if (_drawSceneOverlay)
                SceneView.RepaintAll();
        }
    }

    // Purpose: Draws the summary authoring UI.
    private void DrawSummary()
    {
        int visible = 0;
        int players = 0;
        int enemies = 0;
        int engaged = 0;
        int seesTarget = 0;

        for (int i = 0; i < _rows.Count; i++)
        {
            CharacterRow row = _rows[i];
            if (!MatchesFilter(row))
                continue;

            visible++;
            if (row.IsPlayer)
                players++;
            else
                enemies++;

            if (row.combat != null && row.combat.IsEngaged)
                engaged++;

            if (row.combat != null && row.combat.CanSeeTarget)
                seesTarget++;
        }

        string sectorText = _currentSector != null
            ? $"현재 섹터 {_currentSector.Coord}"
            : "현재 섹터 없음";

        EditorGUILayout.LabelField(
            $"표시 {visible}  |  Player {players}  |  Enemy {enemies}  |  교전 {engaged}  |  Target 시야 확보 {seesTarget}  |  {sectorText}",
            EditorStyles.boldLabel);
    }

    // Purpose: Draws the stage map debug authoring UI.
    private void DrawStageMapDebug()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        _stageMapFoldout = EditorGUILayout.Foldout(
            _stageMapFoldout,
            "Stage Map Debug",
            true);

        GUILayout.FlexibleSpace();

        string stageLabel = _stageProgressionManager != null
            ? _stageProgressionManager.DebugCurrentStageLabel
            : _sectorStateManager != null
                ? $"Stage {_sectorStateManager.CurrentStage}"
                : "Stage unavailable";

        GUILayout.Label(stageLabel, EditorStyles.miniLabel, GUILayout.MaxWidth(260f));

        using (new EditorGUI.DisabledScope(
                   _stageProgressionManager == null ||
                   !_stageProgressionManager.DebugHasNextStage))
        {
            if (GUILayout.Button(
                    new GUIContent(
                        "Next Stage -> Start",
                        "Immediately rebuild the next stage and use the existing StartSectorReady flow to move the player."),
                    GUILayout.Width(145f)))
            {
                ExecuteNextStageDebugMove();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (!_stageMapFoldout)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        if (_sectorStateManager == null)
        {
            EditorGUILayout.HelpBox(
                "SectorStateManager was not found in the active PlayMode scene.",
                MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Move player to coord", GUILayout.Width(125f));
        GUILayout.Label("X", GUILayout.Width(12f));
        _teleportCoordX = EditorGUILayout.IntField(_teleportCoordX, GUILayout.Width(55f));
        GUILayout.Label("Y", GUILayout.Width(12f));
        _teleportCoordY = EditorGUILayout.IntField(_teleportCoordY, GUILayout.Width(55f));

        using (new EditorGUI.DisabledScope(_playerTransform == null))
        {
            if (GUILayout.Button("Move", GUILayout.Width(70f)))
                ExecuteCoordDebugMove();
        }

        GUILayout.Space(8f);
        GUILayout.Label(
            "This also publishes CurrentSectorChanged, so room state and listeners stay synchronized.",
            EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_stageDebugStatus))
            EditorGUILayout.HelpBox(_stageDebugStatus, _stageDebugStatusType);

        DrawStageSectorStateTable();
        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws the stage sector state table authoring UI.
    private void DrawStageSectorStateTable()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Coord", EditorStyles.miniBoldLabel, GUILayout.Width(58f));
        GUILayout.Label("Room Type", EditorStyles.miniBoldLabel, GUILayout.Width(105f));
        GUILayout.Label("Sector", EditorStyles.miniBoldLabel, GUILayout.Width(230f));
        GUILayout.Label("Runtime State", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();

        _stageMapScroll = EditorGUILayout.BeginScrollView(
            _stageMapScroll,
            GUILayout.Height(Mathf.Min(150f, 24f + _stageSectorStates.Count * 19f)));

        for (int i = 0; i < _stageSectorStates.Count; i++)
        {
            SectorStateManager.RuntimeSectorStateEntry state = _stageSectorStates[i];
            GUIStyle rowStyle = state.isCurrentSector
                ? _goodCellStyle
                : state.isFailed
                    ? _badCellStyle
                    : state.isActiveStageSector
                        ? _cellStyle
                        : _mutedCellStyle;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"({state.coord.x}, {state.coord.y})", rowStyle, GUILayout.Width(58f));
            GUILayout.Label(state.roomType.ToString(), rowStyle, GUILayout.Width(105f));
            GUILayout.Label(
                state.sector != null ? state.sector.name : "Missing Sector",
                rowStyle,
                GUILayout.Width(230f));
            GUILayout.Label(FormatStageSectorFlags(state), rowStyle);
            EditorGUILayout.EndHorizontal();
        }

        if (_stageSectorStates.Count == 0)
            EditorGUILayout.LabelField("No generated stage sectors are registered.", _mutedCellStyle);

        EditorGUILayout.EndScrollView();
    }

    // Purpose: Performs the execute coord debug move workflow for this component.
    private void ExecuteCoordDebugMove()
    {
        if (_sectorStateManager == null)
        {
            SetStageDebugStatus("SectorStateManager is missing.", MessageType.Error);
            return;
        }

        Vector2Int coord = new Vector2Int(_teleportCoordX, _teleportCoordY);

        if (!_sectorStateManager.TryGetSector(coord, out SectorRuntime sector) || sector == null)
        {
            SetStageDebugStatus($"No generated sector exists at coord={coord}.", MessageType.Error);
            return;
        }

        Transform playerRoot = ResolveDebugPlayerRoot();
        if (playerRoot == null)
        {
            SetStageDebugStatus("PlayerCharacter was not found.", MessageType.Error);
            return;
        }

        if (!_sectorStateManager.DebugTryEnterSector(sector))
        {
            SetStageDebugStatus(
                $"CurrentSector transition was rejected for coord={coord}. Check the Console error.",
                MessageType.Error);
            return;
        }

        MoveDebugPlayerToSectorSafePoint(playerRoot, sector);
        SetStageDebugStatus(
            $"Moved player to coord={coord}, room={ResolveRoomTypeLabel(sector)}.",
            MessageType.Info);
        RefreshRows();
        Repaint();
    }

    // Purpose: Performs the execute next stage debug move workflow for this component.
    private void ExecuteNextStageDebugMove()
    {
        if (_stageProgressionManager == null)
        {
            SetStageDebugStatus("StageProgressionManager is missing.", MessageType.Error);
            return;
        }

        if (!_stageProgressionManager.DebugTryBeginNextStage())
        {
            SetStageDebugStatus(
                "Could not build the next stage. Check the Console for the exact reason.",
                MessageType.Error);
            return;
        }

        RefreshRows();

        if (_sectorStateManager != null && _sectorStateManager.StartSector != null)
        {
            Vector2Int startCoord = _sectorStateManager.GetSectorCoord(_sectorStateManager.StartSector);
            _teleportCoordX = startCoord.x;
            _teleportCoordY = startCoord.y;
        }

        SetStageDebugStatus(
            $"Built {_stageProgressionManager.DebugCurrentStageLabel} and moved to its Start sector.",
            MessageType.Info);
        Repaint();
    }

    // Purpose: Resolves the debug player root needed by this component.
    private Transform ResolveDebugPlayerRoot()
    {
        if (_playerTransform != null)
            return _playerTransform;

        PlayerCharacter player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        return player != null ? EntityComponentResolver.ResolveEntityRoot(player) : null;
    }

    // Purpose: Moves debug player to sector safe point toward its requested destination.
    private static void MoveDebugPlayerToSectorSafePoint(
        Transform playerRoot,
        SectorRuntime sector)
    {
        CharacterController controller = EntityComponentResolver.Resolve<CharacterController>(playerRoot);
        PlayerCharacter player = EntityComponentResolver.Resolve<PlayerCharacter>(playerRoot);
        playerRoot.SetParent(sector.transform, worldPositionStays: true);
        Vector3 preferred = sector.PlayerStartPoint != null
            ? sector.PlayerStartPoint.position
            : sector.GetWorldBounds().center;

        if (!ShooterStuckRecovery.TryResolveSafePosition(
                playerRoot,
                sector,
                preferred,
                out Vector3 safePosition))
        {
            Bounds bounds = sector.GetWorldBounds();
            float feetToRoot = controller != null
                ? Mathf.Max(0f, controller.height * 0.5f - controller.center.y)
                : 0.5f;
            safePosition = new Vector3(
                bounds.center.x,
                bounds.center.y + feetToRoot + 0.04f,
                bounds.center.z);
        }

        ShooterStuckRecovery.PlaceEntity(playerRoot, safePosition, controller);
        Physics.SyncTransforms();

        if (player != null)
        {
            player.movementInput = Vector3.zero;
            player.movementVector = Vector3.zero;
        }

        CameraManager cameraManager = UnityEngine.Object.FindAnyObjectByType<CameraManager>();
        cameraManager?.SnapToTarget();
    }

    // Purpose: Resolves the room type label needed by this component.
    private string ResolveRoomTypeLabel(SectorRuntime sector)
    {
        return _sectorStateManager != null &&
               _sectorStateManager.TryGetStageRoomType(sector, out StageRoomType roomType)
            ? roomType.ToString()
            : "Unknown";
    }

    // Purpose: Performs the format stage sector flags workflow for this component.
    private static string FormatStageSectorFlags(
        SectorStateManager.RuntimeSectorStateEntry state)
    {
        StringBuilder flags = new StringBuilder(64);

        AppendStageSectorFlag(flags, state.isCurrentSector, "Current");
        AppendStageSectorFlag(flags, state.isStartSector, "Start");
        AppendStageSectorFlag(flags, state.isActiveStageSector, "Active");
        AppendStageSectorFlag(flags, state.isOpened, "Open");
        AppendStageSectorFlag(flags, state.isRevealed, "Revealed");
        AppendStageSectorFlag(flags, state.isCleared, "Cleared");
        AppendStageSectorFlag(flags, state.isFailed, "Failed");

        return flags.Length > 0 ? flags.ToString() : "Inactive";
    }

    // Purpose: Performs the append stage sector flag workflow for this component.
    private static void AppendStageSectorFlag(
        StringBuilder builder,
        bool enabled,
        string label)
    {
        if (!enabled)
            return;

        if (builder.Length > 0)
            builder.Append(" | ");

        builder.Append(label);
    }

    // Purpose: Updates the stage debug status state.
    private void SetStageDebugStatus(string message, MessageType messageType)
    {
        _stageDebugStatus = message ?? string.Empty;
        _stageDebugStatusType = messageType;
    }

    // Purpose: Draws the table authoring UI.
    private void DrawTable()
    {
        _tableScroll = EditorGUILayout.BeginScrollView(
            _tableScroll,
            true,
            true,
            GUILayout.MinHeight(190f),
            GUILayout.MaxHeight(Mathf.Max(220f, position.height * 0.52f)));

        DrawTableHeader();

        for (int i = 0; i < _rows.Count; i++)
        {
            CharacterRow row = _rows[i];
            if (MatchesFilter(row))
                DrawTableRow(row, i);
        }

        GUILayout.Space(2f);
        EditorGUILayout.EndScrollView();
    }

    // Purpose: Draws the table header authoring UI.
    private void DrawTableHeader()
    {
        Rect rect = GUILayoutUtility.GetRect(TableWidth, HeaderHeight, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
            ? new Color(0.12f, 0.12f, 0.12f)
            : new Color(0.72f, 0.72f, 0.72f));

        float x = rect.x;
        DrawCell(ref x, rect.y, 24f, string.Empty, _headerStyle);
        DrawCell(ref x, rect.y, 62f, "거리", _headerStyle, "Player까지의 평면 거리");
        DrawCell(ref x, rect.y, 170f, "Character", _headerStyle);
        DrawCell(ref x, rect.y, 86f, "종류", _headerStyle);
        DrawCell(ref x, rect.y, 110f, "Enemy Role", _headerStyle);
        DrawCell(ref x, rect.y, 150f, "Attack Movement", _headerStyle);
        DrawCell(ref x, rect.y, 74f, "섹터", _headerStyle);
        DrawCell(ref x, rect.y, 220f, "현재 FSM 상태", _headerStyle);
        DrawCell(ref x, rect.y, 66f, "유지", _headerStyle);
        DrawCell(ref x, rect.y, 92f, "AI Goal", _headerStyle);
        DrawCell(ref x, rect.y, 64f, "교전", _headerStyle);
        DrawCell(ref x, rect.y, 130f, "Target", _headerStyle);
        DrawCell(ref x, rect.y, 54f, "시야", _headerStyle);
        DrawCell(ref x, rect.y, 108f, "Vision/FOV", _headerStyle);
        DrawCell(ref x, rect.y, 150f, "공격 판단", _headerStyle);
        DrawCell(ref x, rect.y, 86f, "HP", _headerStyle);
        DrawCell(ref x, rect.y, 120f, "NavMesh", _headerStyle);
    }

    // Purpose: Draws the table row authoring UI.
    private void DrawTableRow(CharacterRow row, int visibleIndex)
    {
        Rect rect = GUILayoutUtility.GetRect(TableWidth, RowHeight, GUILayout.ExpandWidth(false));
        bool selected = row.id == _selectedRowId;
        Color background = ResolveRowBackground(row, visibleIndex, selected);
        EditorGUI.DrawRect(rect, background);

        float x = rect.x;
        DrawCell(ref x, rect.y, 24f, selected ? "▼" : "▶", _mutedCellStyle);
        DrawCell(ref x, rect.y, 62f, FormatDistance(row), _cellStyle);
        DrawCell(ref x, rect.y, 170f, row.displayName, row.IsDead ? _mutedCellStyle : _cellStyle);
        DrawCell(ref x, rect.y, 86f, row.typeName, row.IsPlayer ? _goodCellStyle : _cellStyle);
        DrawCell(ref x, rect.y, 110f, ResolveEnemyRole(row), _cellStyle);
        DrawCell(ref x, rect.y, 150f, ResolveAttackMovement(row), _cellStyle);
        DrawCell(ref x, rect.y, 74f, FormatSector(row.sector), _cellStyle);
        DrawCell(ref x, rect.y, 220f, BuildStateSummary(row), _cellStyle);
        DrawCell(ref x, rect.y, 66f, FormatStateElapsed(row), _cellStyle);
        DrawCell(ref x, rect.y, 92f, row.combat != null ? row.combat.CurrentGoal.ToString() : "-", _cellStyle);
        DrawCell(
            ref x,
            rect.y,
            64f,
            row.combat != null && row.combat.IsEngaged ? "교전" : "비교전",
            row.combat != null && row.combat.IsEngaged ? _warningCellStyle : _mutedCellStyle);
        DrawCell(ref x, rect.y, 130f, ResolveTargetName(row), _cellStyle);
        DrawCell(
            ref x,
            rect.y,
            54f,
            ResolveVisionResult(row),
            ResolveVisionStyle(row));
        DrawCell(ref x, rect.y, 108f, FormatVision(row), _cellStyle);
        DrawCell(ref x, rect.y, 150f, ResolveAttackStatus(row), ResolveAttackStyle(row));
        DrawCell(ref x, rect.y, 86f, FormatHealth(row), ResolveHealthStyle(row));
        DrawCell(ref x, rect.y, 120f, FormatAgent(row.agent), _cellStyle);

        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition))
        {
            SelectRow(row, currentEvent.clickCount > 1);
            currentEvent.Use();
        }
    }

    // Purpose: Draws the selected details authoring UI.
    private void DrawSelectedDetails()
    {
        CharacterRow row = FindSelectedRow();
        if (row == null)
        {
            EditorGUILayout.HelpBox("표에서 Character를 선택하면 FSM·시야·전투·NavMesh 세부 상태가 표시됩니다.", MessageType.None);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"상세: {row.displayName}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Hierarchy 선택", GUILayout.Width(100f)))
            SelectRow(row, false);

        if (GUILayout.Button("Scene 포커스", GUILayout.Width(95f)))
            SelectRow(row, true);

        EditorGUILayout.EndHorizontal();

        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

        EditorGUILayout.BeginHorizontal();
        DrawGeneralDetails(row);
        DrawVisionAndCombatDetails(row);
        DrawNavigationDetails(row);
        EditorGUILayout.EndHorizontal();

        DrawStateMachineDetails(row);
        DrawSelectedEncounterDetails(row);
        DrawSelectedCombatTestControls(row);
        DrawSelectedCharacterRuntimeItems(row);
        DrawStatusEffectDebugControls(row);

        EditorGUILayout.EndScrollView();
    }

    // Purpose: Draws the general details authoring UI.
    private void DrawGeneralDetails(CharacterRow row)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(270f));
        EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("GameObject", row.root != null ? row.root.gameObject : null, typeof(GameObject), true);
        }

        DrawValue("종류", row.typeName);
        DrawValue("Enemy Role", ResolveEnemyRole(row));
        DrawValue("Attack Movement", ResolveAttackMovement(row));
        DrawValue("섹터", FormatSector(row.sector));
        DrawValue("Player 거리", FormatDistance(row));
        DrawValue("HP", FormatHealth(row));
        DrawValue(
            "Armor Class",
            row.damageable != null
                ? $"{row.damageable.EffectiveArmorClass} (base {row.stats?.ArmorClass ?? 0})"
                : row.stats != null ? row.stats.ArmorClass.ToString() : "-");

        if (row.infection != null)
        {
            DrawValue(
                "Infection",
                $"{row.infection.CurrentInfection:0.#}/{row.infection.InfectionLimit:0.#}");
            DrawValue(
                "Infection Heal",
                $"{row.infection.NaturalRecoveryPerSecond:0.##}/s  x{row.infection.InfectionRecoveryMultiplier:0.##}");
        }

        if (row.paintMark != null)
        {
            DrawValue(
                "Mark",
                $"{row.paintMark.DisplayMarkAmount:0.#} / stacks {row.paintMark.DisplayStacks} / AC {row.paintMark.ArmorClassDelta:+0;-0;0}");
        }

        DrawValue("Status", BuildStatusSummary(row.statusEffects));

        if (row.stats != null)
        {
            BulletSO bullet = row.stats.CurrentBullet;
            DrawValue(
                "Ammo",
                bullet != null
                    ? bullet.IsBasicAmmo
                        ? $"{bullet.name} T{(int)bullet.Tier} / Pen {row.stats.ResolvePenetrationClass(bullet)}"
                        : $"{bullet.name} / {bullet.GetType().Name}"
                    : "-");
            DrawValue(
                "Final Shot",
                bullet != null
                    ? bullet.IsBasicAmmo
                        ? $"HP {row.stats.ResolveAttackDamage(bullet):0.##} / INF {row.stats.ResolveInfectionDamage(bullet):0.##} / MARK {row.stats.ResolvePaintMarkDamage(bullet):0.##}"
                        : "Special-specific runtime"
                    : "-");
            DrawValue("Move Speed", $"{row.stats.MoveSpeed:0.##}");
        }

        if (row.equipment != null)
        {
            DrawValue(
                "Armor 내구도",
                row.equipment.CurrentArmor != null
                    ? $"{row.equipment.CurrentArmorDurability:0.#}/{row.equipment.MaxArmorDurability:0.#}"
                    : "없음");
        }

        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws the vision and combat details authoring UI.
    private void DrawVisionAndCombatDetails(CharacterRow row)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(330f));
        EditorGUILayout.LabelField("Vision / Combat", EditorStyles.boldLabel);

        if (row.vision != null)
        {
            DrawValue("Vision Range", $"{row.vision.VisionRange:0.##}");
            DrawValue("근거리 원", $"{row.vision.CloseVisionRadius:0.##}");
            DrawValue("FOV", row.vision.UsesForwardArc ? $"{row.vision.ForwardArcAngle:0.#}°" : "360°");
            DrawValue("LOS 사용", row.vision.UsesLineOfSight ? "Yes" : "No");
            DrawValue("최근 판정 거리", row.vision.LastCheckedPlanarDistance >= 0f ? $"{row.vision.LastCheckedPlanarDistance:0.00}" : "-");
            DrawValue("근거리 내부", row.vision.LastCheckInsideCloseRange ? "Yes" : "No");
            DrawValue("Vision 내부", row.vision.LastCheckInsideVisionRange ? "Yes" : "No");
            DrawValue("FOV 내부", row.vision.LastCheckInsideForwardArc ? "Yes" : "No");
            DrawValue("최근 LOS", row.vision.LastCheckHasLineOfSight ? "Clear" : "Blocked");
            DrawValue(
                "막은 Collider",
                row.vision.LastLineOfSightBlocker != null
                    ? row.vision.LastLineOfSightBlocker.name
                    : "-");
        }
        else
        {
            DrawValue("Vision", "없음");
        }

        if (row.combat != null)
        {
            DrawValue("Goal", row.combat.CurrentGoal.ToString());
            DrawValue("교전", row.combat.IsEngaged ? "Yes" : "No");
            DrawValue("Alerted", row.combat.IsAlerted ? "Yes" : "No");
            DrawValue("Target", ResolveTargetName(row));
            DrawValue("Target 거리", row.combat.TargetPlanarDistance >= 0f ? $"{row.combat.TargetPlanarDistance:0.00}" : "-");
            DrawValue("시야 확보", row.combat.CanSeeTarget ? "Yes" : "No");
            DrawValue("Target Lock", row.combat.IsTargetLocked ? $"{row.combat.TargetLockRemaining:0.00}s" : "No");
            DrawValue("공격 가능", row.combat.CanAttackTarget ? "Yes" : "No");
            DrawValue("공격 판단", row.combat.AttackDebugStatus);
            DrawValue("공격 범위", $"{row.combat.ActiveAttackRange:0.##} (Start {row.combat.AttackStartRange:0.##} / Keep {row.combat.AttackKeepRange:0.##})");
            DrawValue("Attack Phase", row.combat.AttackPhase.ToString());
            DrawValue("Burst Remaining", row.combat.BurstShotsRemaining.ToString());
            DrawValue("공격 Cooldown", $"{row.combat.AttackCooldownRemaining:0.00}s");
            DrawValue("Reaction", $"{row.combat.ReactionTimeRemaining:0.00}s");
            DrawValue("마지막 시야", FormatAge(row.combat.LastSeenSecondsAgo));
            DrawValue("Last Known", row.combat.HasLastKnownPosition ? FormatAge(row.combat.LastKnownSecondsAgo) : "없음");
            DrawValue("Sound 조사", row.combat.HasSoundInvestigation ? row.combat.CurrentInvestigationType.ToString() : "없음");
        }

        else
        {
            DrawValue("Combat Controller", "없음");
        }

        if (row.combat != null)
        {
            DrawValue("Last Sound", row.combat.LastSoundStimulusResult);
            DrawValue(
                "Sound Distance",
                row.combat.LastSoundStimulusDistance >= 0f
                    ? $"{row.combat.LastSoundStimulusDistance:0.00} / {row.combat.LastSoundStimulusRadius:0.00}"
                    : "-");
            DrawValue("Sound Reaction Delay", $"{row.combat.SoundReactionDelayRemaining:0.00}s");
        }

        if (row.statusEffects != null && row.statusEffects.HasAny)
            DrawValue("Status", BuildStatusSummary(row.statusEffects));

        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws the navigation details authoring UI.
    private void DrawNavigationDetails(CharacterRow row)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(280f));
        EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);

        NavMeshAgent agent = row.agent;
        if (agent == null)
        {
            DrawValue("NavMeshAgent", "없음");
            EditorGUILayout.EndVertical();
            return;
        }

        bool usable = agent.isActiveAndEnabled && agent.isOnNavMesh;
        DrawValue("Enabled", agent.isActiveAndEnabled ? "Yes" : "No");
        DrawValue("On NavMesh", agent.isOnNavMesh ? "Yes" : "No");
        DrawValue("Stopped", usable ? (agent.isStopped ? "Yes" : "No") : "-");
        DrawValue("속도", usable ? $"{agent.velocity.magnitude:0.00}" : "-");
        DrawValue("Desired 속도", usable ? $"{agent.desiredVelocity.magnitude:0.00}" : "-");
        DrawValue("Has Path", usable ? (agent.hasPath ? "Yes" : "No") : "-");
        DrawValue("Path Pending", usable ? (agent.pathPending ? "Yes" : "No") : "-");
        DrawValue("Path Status", usable && agent.hasPath ? agent.pathStatus.ToString() : "-");
        DrawValue("남은 거리", usable && agent.hasPath ? $"{agent.remainingDistance:0.00}" : "-");
        DrawValue("Stopping", $"{agent.stoppingDistance:0.00}");

        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws the state machine details authoring UI.
    private void DrawStateMachineDetails(CharacterRow row)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("State Machines", EditorStyles.boldLabel);

        if (row.stateMachines == null || row.stateMachines.Length == 0)
        {
            EditorGUILayout.HelpBox("이 Character에서 StateMachine을 찾지 못했습니다.", MessageType.None);
            return;
        }

        for (int i = 0; i < row.stateMachines.Length; i++)
        {
            StateMachine machine = row.stateMachines[i];
            if (machine == null)
                continue;

            string current = machine.CurrentStateSO != null ? machine.CurrentStateSO.name : "<null>";
            string previous = machine.PreviousStateSO != null ? machine.PreviousStateSO.name : "-";

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(machine.GetType().Name, GUILayout.Width(190f));
            EditorGUILayout.LabelField($"{previous}  →  {current}", GUILayout.MinWidth(300f));
            EditorGUILayout.LabelField($"{machine.CurrentStateElapsedSeconds:0.00}s", GUILayout.Width(70f));
            EditorGUILayout.LabelField($"frame {machine.LastTransitionFrame}", GUILayout.Width(95f));

            if (GUILayout.Button("선택", GUILayout.Width(48f)))
            {
                Selection.activeGameObject = machine.gameObject;
                EditorGUIUtility.PingObject(machine.gameObject);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    // Purpose: Draws the value authoring UI.
    private static void DrawValue(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(105f));
        EditorGUILayout.SelectableLabel(value ?? "-", EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();
    }

    // Purpose: Handles the incoming scene gui request or event.
    private void HandleSceneGUI(SceneView sceneView)
    {
        if (!_drawSceneOverlay || !EditorApplication.isPlaying)
            return;

        EnsureStyles();

        for (int i = 0; i < _rows.Count; i++)
        {
            CharacterRow row = _rows[i];
            if (!MatchesFilter(row))
                continue;

            if (_overlaySelectedOnly && row.id != _selectedRowId)
                continue;

            DrawCharacterOverlay(row);
        }
    }

    // Purpose: Draws the character overlay authoring UI.
    private void DrawCharacterOverlay(CharacterRow row)
    {
        if (row.root == null)
            return;

        Vector3 center = row.root.position;
        EntityVisionController vision = row.vision;

        if (vision != null)
        {
            center = vision.VisualOrigin.position;

            if (vision.CloseVisionRadius > 0f)
            {
                Handles.color = new Color(0f, 0.85f, 1f, 0.8f);
                Handles.DrawWireDisc(center, Vector3.up, vision.CloseVisionRadius);
            }

            if (vision.VisionRange > 0f)
            {
                Handles.color = new Color(1f, 0.78f, 0.08f, 0.9f);

                if (vision.UsesForwardArc)
                {
                    Vector3 forward = vision.ForwardDirection;
                    forward.y = 0f;

                    if (forward.sqrMagnitude > 0.0001f)
                    {
                        forward.Normalize();
                        float angle = Mathf.Clamp(vision.ForwardArcAngle, 1f, 360f);
                        Vector3 left = Quaternion.AngleAxis(-angle * 0.5f, Vector3.up) * forward;
                        Vector3 right = Quaternion.AngleAxis(angle * 0.5f, Vector3.up) * forward;
                        Handles.DrawWireArc(center, Vector3.up, left, angle, vision.VisionRange);
                        Handles.DrawLine(center, center + left * vision.VisionRange);
                        Handles.DrawLine(center, center + right * vision.VisionRange);
                    }
                }
                else
                {
                    Handles.DrawWireDisc(center, Vector3.up, vision.VisionRange);
                }
            }
        }

        EnemyShooterCombatController combat = row.combat;
        if (combat != null)
        {
            if (combat.AttackStartRange > 0f)
            {
                Handles.color = new Color(1f, 0.18f, 0.16f, 0.75f);
                Handles.DrawWireDisc(row.root.position, Vector3.up, combat.AttackStartRange);
            }

            if (combat.Target != null)
            {
                Handles.color = combat.CanSeeTarget
                    ? new Color(0.2f, 1f, 0.25f, 1f)
                    : new Color(1f, 0.16f, 0.12f, 1f);
                Handles.DrawAAPolyLine(3f, center, combat.Target.position);
            }

            if (combat.HasLastKnownPosition)
            {
                Handles.color = new Color(1f, 0.55f, 0f, 0.9f);
                Handles.DrawDottedLine(center, combat.LastKnownPosition, 4f);
                Handles.DrawWireDisc(combat.LastKnownPosition, Vector3.up, 0.35f);
            }

            if (combat.HasSoundInvestigation)
            {
                Handles.color = new Color(0.85f, 0.3f, 1f, 0.9f);
                Handles.DrawDottedLine(center, combat.SoundInvestigationPosition, 5f);
                Handles.DrawWireDisc(combat.SoundInvestigationPosition, Vector3.up, 0.3f);
            }
        }

        float handleSize = HandleUtility.GetHandleSize(center);
        Vector3 labelPosition = center + Vector3.up * handleSize * 0.25f;
        string label = BuildSceneLabel(row);
        Handles.Label(labelPosition, label, _sceneLabelStyle);
    }

    // Purpose: Builds the scene label from the current authored data.
    private string BuildSceneLabel(CharacterRow row)
    {
        _textBuilder.Clear();
        _textBuilder.Append(row.displayName);
        _textBuilder.Append("\nFSM: ");
        _textBuilder.Append(BuildStateSummary(row));

        if (row.combat != null)
        {
            _textBuilder.Append("\nGoal: ");
            _textBuilder.Append(row.combat.CurrentGoal);
            _textBuilder.Append(" | ");
            _textBuilder.Append(row.combat.AttackDebugStatus);
            _textBuilder.Append("\nDist ");
            _textBuilder.Append(row.combat.TargetPlanarDistance >= 0f
                ? row.combat.TargetPlanarDistance.ToString("0.0")
                : "-");
            _textBuilder.Append(" | Vision ");
            _textBuilder.Append(row.combat.CanSeeTarget ? "YES" : "NO");
        }

        if (row.statusEffects != null && row.statusEffects.HasAny)
        {
            _textBuilder.Append("\nStatus: ");
            _textBuilder.Append(BuildStatusSummary(row.statusEffects));
        }

        return _textBuilder.ToString();
    }

    // Purpose: Refreshes the displayed or cached rows.
    private void RefreshRows()
    {
        _nextRefreshTime = EditorApplication.timeSinceStartup + Mathf.Clamp(
            _refreshInterval,
            RefreshIntervalMin,
            RefreshIntervalMax);

        _rows.Clear();
        _stageSectorStates.Clear();
        _knownEntityRoots.Clear();
        _playerTransform = null;
        _sectorStateManager = null;
        _stageProgressionManager = null;
        _currentSector = null;

        if (!EditorApplication.isPlaying)
        {
            _teleportCoordInitialized = false;
            return;
        }

        _sectorStateManager = UnityEngine.Object.FindAnyObjectByType<SectorStateManager>();
        _stageProgressionManager = UnityEngine.Object.FindAnyObjectByType<StageProgressionManager>();
        _currentSector = _sectorStateManager != null ? _sectorStateManager.CurrentSector : null;

        if (_sectorStateManager != null)
        {
            IReadOnlyList<SectorStateManager.RuntimeSectorStateEntry> sectorStates =
                _sectorStateManager.RuntimeSectorStates;

            if (sectorStates != null)
            {
                for (int i = 0; i < sectorStates.Count; i++)
                    _stageSectorStates.Add(sectorStates[i]);

                _stageSectorStates.Sort(CompareStageSectorStates);
            }

            if (!_teleportCoordInitialized)
            {
                SectorRuntime initialSector = _currentSector != null
                    ? _currentSector
                    : _sectorStateManager.StartSector;

                if (initialSector != null)
                {
                    Vector2Int initialCoord = _sectorStateManager.GetSectorCoord(initialSector);
                    _teleportCoordX = initialCoord.x;
                    _teleportCoordY = initialCoord.y;
                    _teleportCoordInitialized = true;
                }
            }
        }

        PlayerCharacter[] players = UnityEngine.Object.FindObjectsByType<PlayerCharacter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
            TryAddCharacter(players[i]);

        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
            TryAddCharacter(enemies[i]);

        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].IsPlayer && _rows[i].root != null)
            {
                _playerTransform = _rows[i].root;
                break;
            }
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            CharacterRow row = _rows[i];
            row.distanceToPlayer = row.IsPlayer || _playerTransform == null || row.root == null
                ? -1f
                : PlanarDistance(row.root.position, _playerTransform.position);
        }

        _rows.Sort(CompareRows);

        if (_selectedRowId >= 0 && FindSelectedRow() == null)
            _selectedRowId = -1;
    }

    // Purpose: Performs the compare stage sector states workflow for this component.
    private static int CompareStageSectorStates(
        SectorStateManager.RuntimeSectorStateEntry left,
        SectorStateManager.RuntimeSectorStateEntry right)
    {
        int yCompare = right.coord.y.CompareTo(left.coord.y);
        return yCompare != 0 ? yCompare : left.coord.x.CompareTo(right.coord.x);
    }

    // Purpose: Attempts to add character and reports whether the request succeeded.
    private void TryAddCharacter(Component marker)
    {
        if (marker == null)
            return;

        Transform root = EntityComponentResolver.ResolveEntityRoot(marker);
        if (root == null || !_knownEntityRoots.Add(root.GetInstanceID()))
            return;

        PlayerCharacter player = ResolveInEntity<PlayerCharacter>(root);
        Enemy enemy = ResolveInEntity<Enemy>(root);

        if (player == null && enemy == null)
            return;

        CharacterRow row = new CharacterRow
        {
            id = root.GetInstanceID(),
            root = root,
            player = player,
            enemy = enemy,
            enemyShooter = ResolveInEntity<EnemyShooter>(root),
            damageable = ResolveInEntity<Damageable>(root),
            combat = ResolveInEntity<EnemyShooterCombatController>(root),
            vision = ResolveInEntity<EntityVisionController>(root),
            stats = ResolveInEntity<ShooterStatsRuntime>(root),
            equipment = ResolveInEntity<ShooterEquipmentController>(root),
            equipmentRuntime = ResolveInEntity<EntityEquipmentRuntime>(root),
            playerInventory = ResolveInEntity<PlayerInventoryRuntime>(root),
            lootInventory = ResolveInEntity<EnemyLootInventoryRuntime>(root),
            infection = ResolveInEntity<ShooterInfection>(root),
            paintMark = ResolveInEntity<EntityPaintMarkController>(root),
            statusEffects = ResolveInEntity<EntityStatusEffectRuntime>(root),
            agent = ResolveInEntity<NavMeshAgent>(root)
        };

        row.sector = ResolveSector(row);
        row.stateMachines = ResolveStateMachines(root);
        row.displayName = ResolveDisplayName(row);
        row.typeName = player != null
            ? "Player"
            : row.enemyShooter != null
                ? "Enemy Shooter"
                : "Enemy";

        _rows.Add(row);
    }

    // Purpose: Builds the status summary from the current authored data.
    private static string BuildStatusSummary(EntityStatusEffectRuntime runtime)
    {
        if (runtime == null || !runtime.HasAny)
            return "-";

        IReadOnlyList<EntityStatusEffectRuntime.ActiveStatusEffect> effects = runtime.ActiveEffects;
        StringBuilder builder = new StringBuilder(64);

        for (int i = 0; i < effects.Count; i++)
        {
            EntityStatusEffectRuntime.ActiveStatusEffect effect = effects[i];

            if (effect.Definition == null)
                continue;

            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(effect.Definition.Id);
            builder.Append(' ');
            builder.Append(effect.RemainingSeconds.ToString("0.0"));
            builder.Append('s');
        }

        return builder.Length > 0 ? builder.ToString() : "-";
    }

    // Purpose: Resolves the sector needed by this component.
    private SectorRuntime ResolveSector(CharacterRow row)
    {
        if (row.enemy != null && row.enemy.CurrentSector != null)
            return row.enemy.CurrentSector;

        SectorRuntime parentSector = row.root != null
            ? row.root.GetComponentInParent<SectorRuntime>()
            : null;

        if (parentSector != null)
            return parentSector;

        return row.IsPlayer && _sectorStateManager != null
            ? _sectorStateManager.CurrentSector
            : null;
    }

    private static T ResolveInEntity<T>(Transform root) where T : Component
    {
        if (root == null)
            return null;

        T direct = root.GetComponent<T>();
        return direct != null ? direct : root.GetComponentInChildren<T>(true);
    }

    // Purpose: Resolves the state machines needed by this component.
    private static StateMachine[] ResolveStateMachines(Transform root)
    {
        if (root == null)
            return Array.Empty<StateMachine>();

        StateMachine[] candidates = root.GetComponentsInChildren<StateMachine>(true);
        if (candidates == null || candidates.Length == 0)
            return Array.Empty<StateMachine>();

        List<StateMachine> resolved = new List<StateMachine>(candidates.Length);

        for (int i = 0; i < candidates.Length; i++)
        {
            StateMachine machine = candidates[i];
            if (machine == null)
                continue;

            Transform machineRoot = EntityComponentResolver.ResolveEntityRoot(machine.transform);
            if (machineRoot == root)
                resolved.Add(machine);
        }

        resolved.Sort((a, b) => string.CompareOrdinal(a.GetType().Name, b.GetType().Name));
        return resolved.ToArray();
    }

    // Purpose: Checks whether the current data matches filter.
    private bool MatchesFilter(CharacterRow row)
    {
        if (row == null || row.root == null)
            return false;

        if (!_includeDead && row.IsDead)
            return false;

        if (_characterFilter == CharacterFilter.Player && !row.IsPlayer)
            return false;

        if (_characterFilter == CharacterFilter.Enemy && !row.IsEnemy)
            return false;

        if (_scope == MonitorScope.CurrentSector &&
            !row.IsPlayer &&
            _currentSector != null &&
            row.sector != _currentSector)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_search))
            return true;

        string query = _search.Trim();
        if (ContainsIgnoreCase(row.displayName, query) ||
            ContainsIgnoreCase(row.typeName, query) ||
            ContainsIgnoreCase(ResolveEnemyRole(row), query) ||
            ContainsIgnoreCase(ResolveAttackMovement(row), query) ||
            ContainsIgnoreCase(BuildStateSummary(row), query))
        {
            return true;
        }

        return row.combat != null &&
               (ContainsIgnoreCase(row.combat.CurrentGoal.ToString(), query) ||
                ContainsIgnoreCase(row.combat.AttackDebugStatus, query));
    }

    // Purpose: Performs the contains ignore case workflow for this component.
    private static bool ContainsIgnoreCase(string source, string value)
    {
        return !string.IsNullOrEmpty(source) &&
               source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Purpose: Performs the compare rows workflow for this component.
    private static int CompareRows(CharacterRow a, CharacterRow b)
    {
        if (a.IsPlayer != b.IsPlayer)
            return a.IsPlayer ? -1 : 1;

        int distanceComparison = a.distanceToPlayer.CompareTo(b.distanceToPlayer);
        if (distanceComparison != 0)
            return distanceComparison;

        return string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
    }

    // Purpose: Resolves the display name needed by this component.
    private static string ResolveDisplayName(CharacterRow row)
    {
        if (row.enemyShooter != null &&
            row.enemyShooter.Config != null &&
            !string.IsNullOrWhiteSpace(row.enemyShooter.Config.DisplayName))
        {
            return row.enemyShooter.Config.DisplayName;
        }

        return row.root != null ? row.root.name : "<missing>";
    }

    // Purpose: Resolves the enemy role needed by this component.
    private static string ResolveEnemyRole(CharacterRow row)
    {
        EnemyShooterConfigSO config = row?.enemyShooter != null
            ? row.enemyShooter.Config
            : null;

        return config != null ? config.EnemyRole.ToString() : "-";
    }

    // Purpose: Resolves the attack movement needed by this component.
    private static string ResolveAttackMovement(CharacterRow row)
    {
        EnemyShooterConfigSO config = row?.enemyShooter != null
            ? row.enemyShooter.Config
            : null;

        return config != null ? config.AttackMovementMode.ToString() : "-";
    }

    // Purpose: Performs the planar distance workflow for this component.
    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Purpose: Finds the requested selected row.
    private CharacterRow FindSelectedRow()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].id == _selectedRowId)
                return _rows[i];
        }

        return null;
    }

    // Purpose: Selects row for the active workflow.
    private void SelectRow(CharacterRow row, bool frameScene)
    {
        if (row == null || row.root == null)
            return;

        _selectedRowId = row.id;
        Selection.activeGameObject = row.root.gameObject;
        EditorGUIUtility.PingObject(row.root.gameObject);

        if (frameScene && SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        Repaint();

        if (_drawSceneOverlay)
            SceneView.RepaintAll();
    }

    // Purpose: Builds the state summary from the current authored data.
    private static string BuildStateSummary(CharacterRow row)
    {
        StateMachine machine = ResolvePrimaryStateMachine(row);
        if (machine == null)
            return "-";

        return machine.CurrentStateSO != null ? machine.CurrentStateSO.name : "<null>";
    }

    // Purpose: Resolves the primary state machine needed by this component.
    private static StateMachine ResolvePrimaryStateMachine(CharacterRow row)
    {
        if (row == null || row.stateMachines == null || row.stateMachines.Length == 0)
            return null;

        for (int i = 0; i < row.stateMachines.Length; i++)
        {
            if (row.stateMachines[i] is ParentStateMachine)
                return row.stateMachines[i];
        }

        return row.stateMachines[0];
    }

    // Purpose: Performs the format state elapsed workflow for this component.
    private static string FormatStateElapsed(CharacterRow row)
    {
        StateMachine machine = ResolvePrimaryStateMachine(row);
        return machine != null ? $"{machine.CurrentStateElapsedSeconds:0.0}s" : "-";
    }

    // Purpose: Performs the format distance workflow for this component.
    private static string FormatDistance(CharacterRow row)
    {
        if (row == null || row.IsPlayer || row.distanceToPlayer < 0f)
            return "-";

        return $"{row.distanceToPlayer:0.0}m";
    }

    // Purpose: Performs the format sector workflow for this component.
    private static string FormatSector(SectorRuntime sector)
    {
        return sector != null ? $"{sector.Coord.x},{sector.Coord.y}" : "-";
    }

    // Purpose: Resolves the target name needed by this component.
    private static string ResolveTargetName(CharacterRow row)
    {
        if (row == null || row.combat == null || row.combat.Target == null)
            return "-";

        Transform targetRoot = EntityComponentResolver.ResolveEntityRoot(row.combat.Target);
        return targetRoot != null ? targetRoot.name : row.combat.Target.name;
    }

    // Purpose: Resolves the vision result needed by this component.
    private static string ResolveVisionResult(CharacterRow row)
    {
        if (row == null || row.combat == null || !row.combat.HasTarget)
            return "-";

        return row.combat.CanSeeTarget ? "확보" : "차단";
    }

    // Purpose: Resolves the vision style needed by this component.
    private GUIStyle ResolveVisionStyle(CharacterRow row)
    {
        if (row == null || row.combat == null || !row.combat.HasTarget)
            return _mutedCellStyle;

        return row.combat.CanSeeTarget ? _goodCellStyle : _badCellStyle;
    }

    // Purpose: Performs the format vision workflow for this component.
    private static string FormatVision(CharacterRow row)
    {
        if (row == null || row.vision == null)
            return "-";

        return row.vision.UsesForwardArc
            ? $"{row.vision.VisionRange:0.#}m / {row.vision.ForwardArcAngle:0.#}°"
            : $"{row.vision.VisionRange:0.#}m / 360°";
    }

    // Purpose: Resolves the attack status needed by this component.
    private static string ResolveAttackStatus(CharacterRow row)
    {
        return row != null && row.combat != null
            ? row.combat.AttackDebugStatus
            : "-";
    }

    // Purpose: Resolves the attack style needed by this component.
    private GUIStyle ResolveAttackStyle(CharacterRow row)
    {
        if (row == null || row.combat == null)
            return _mutedCellStyle;

        string status = row.combat.AttackDebugStatus;
        if (status == "Ready" ||
            status == "Attacking" ||
            status == EnemyShooterAttackPhase.FiringBurst.ToString() ||
            status.StartsWith("Finishing burst", StringComparison.Ordinal))
            return _goodCellStyle;

        if (status.StartsWith("Cooldown", StringComparison.Ordinal) ||
            status.StartsWith("Post-burst cooldown", StringComparison.Ordinal) ||
            status.StartsWith("Reaction", StringComparison.Ordinal) ||
            status == "Investigating")
        {
            return _warningCellStyle;
        }

        return row.IsDead ? _mutedCellStyle : _badCellStyle;
    }

    // Purpose: Performs the format health workflow for this component.
    private static string FormatHealth(CharacterRow row)
    {
        if (row == null || row.damageable == null)
            return "-";

        return $"{row.damageable.CurrentHealth:0.#}/{row.damageable.MaxHealth:0.#}";
    }

    // Purpose: Resolves the health style needed by this component.
    private GUIStyle ResolveHealthStyle(CharacterRow row)
    {
        if (row == null || row.damageable == null)
            return _mutedCellStyle;

        if (row.damageable.IsDead)
            return _mutedCellStyle;

        float normalized = row.damageable.MaxHealth > 0f
            ? row.damageable.CurrentHealth / row.damageable.MaxHealth
            : 0f;

        return normalized <= 0.25f ? _badCellStyle : _cellStyle;
    }

    // Purpose: Performs the format agent workflow for this component.
    private static string FormatAgent(NavMeshAgent agent)
    {
        if (agent == null)
            return "-";

        if (!agent.isActiveAndEnabled)
            return "Disabled";

        if (!agent.isOnNavMesh)
            return "Off NavMesh";

        if (agent.isStopped)
            return "Stopped";

        if (agent.pathPending)
            return "Path Pending";

        if (agent.hasPath)
            return $"Move {agent.velocity.magnitude:0.0}";

        return "Idle";
    }

    // Purpose: Performs the format age workflow for this component.
    private static string FormatAge(float seconds)
    {
        return seconds >= 0f ? $"{seconds:0.00}s 전" : "-";
    }

    // Purpose: Resolves the row background needed by this component.
    private Color ResolveRowBackground(CharacterRow row, int index, bool selected)
    {
        if (selected)
            return EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.31f, 0.5f, 0.95f)
                : new Color(0.45f, 0.68f, 0.9f, 0.95f);

        if (row.IsDead)
            return EditorGUIUtility.isProSkin
                ? new Color(0.17f, 0.17f, 0.17f, 0.9f)
                : new Color(0.78f, 0.78f, 0.78f, 0.9f);

        if (row.combat != null && row.combat.IsEngaged)
            return EditorGUIUtility.isProSkin
                ? new Color(0.29f, 0.13f, 0.11f, 0.9f)
                : new Color(0.98f, 0.78f, 0.73f, 0.9f);

        float value = index % 2 == 0 ? 0.18f : 0.205f;
        return EditorGUIUtility.isProSkin
            ? new Color(value, value, value, 0.9f)
            : new Color(0.9f - value * 0.12f, 0.9f - value * 0.12f, 0.9f - value * 0.12f, 0.9f);
    }

    // Purpose: Draws the cell authoring UI.
    private static void DrawCell(
        ref float x,
        float y,
        float width,
        string text,
        GUIStyle style,
        string tooltip = null)
    {
        Rect cell = new Rect(x, y, width, RowHeight);
        GUI.Label(cell, new GUIContent(text ?? string.Empty, tooltip), style);
        x += width;
    }

    // Purpose: Ensures styles is ready before the workflow continues.
    private void EnsureStyles()
    {
        if (_cellStyle != null)
            return;

        _headerStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
            padding = new RectOffset(4, 4, 0, 0)
        };

        _cellStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
            padding = new RectOffset(4, 4, 0, 0)
        };

        _mutedCellStyle = new GUIStyle(_cellStyle);
        _mutedCellStyle.normal.textColor = EditorGUIUtility.isProSkin
            ? new Color(0.58f, 0.58f, 0.58f)
            : new Color(0.38f, 0.38f, 0.38f);

        _goodCellStyle = new GUIStyle(_cellStyle);
        _goodCellStyle.normal.textColor = EditorGUIUtility.isProSkin
            ? new Color(0.35f, 1f, 0.45f)
            : new Color(0f, 0.45f, 0.08f);

        _warningCellStyle = new GUIStyle(_cellStyle);
        _warningCellStyle.normal.textColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 0.72f, 0.2f)
            : new Color(0.65f, 0.35f, 0f);

        _badCellStyle = new GUIStyle(_cellStyle);
        _badCellStyle.normal.textColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 0.35f, 0.3f)
            : new Color(0.72f, 0.05f, 0.02f);

        _sceneLabelStyle = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 11,
            richText = false
        };
        _sceneLabelStyle.normal.textColor = Color.white;
    }
}
