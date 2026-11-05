using UnityEditor;
using UnityEngine;

/// <summary>Scene 편집용 작은 조작 창. 화면 모드만 바꾸며 프리팹/씬/작성 데이터를 저장하거나 닫지 않는다.</summary>
public sealed partial class SectorMakerWindow
{
    private static readonly Vector2 ExpandedMinimum = new(980, 620);
    private static readonly Vector2 CompactMinimum = new(330, 280);
    [SerializeField] private bool _compactMode, _hasExpandedWindowRect, _compactSettings;
    [SerializeField] private Rect _expandedWindowRect;
    [SerializeField] private Vector2 _compactWindowSize = new(360, 340);
    [SerializeField] private Vector2 _compactScroll;
    private bool? _requestedCompactMode;

    private void ApplyWorkspaceSizeLimits() => minSize = _compactMode ? CompactMinimum : ExpandedMinimum;

    private void RequestCompactMode(bool compact)
    {
        // Defer layout/position changes until after OnGUI has ended. Never change
        // the GUILayout tree half-way through a button's Layout/Repaint cycle.
        if (!compact) _paintingEnabled = false;
        if (!_requestedCompactMode.HasValue) EditorApplication.delayCall += ApplyRequestedWorkspaceMode;
        _requestedCompactMode = compact;
    }

    private void ApplyRequestedWorkspaceMode()
    {
        if (this == null || !_requestedCompactMode.HasValue) return;
        bool compact = _requestedCompactMode.Value; _requestedCompactMode = null;
        SetCompactMode(compact);
    }

    private void SetCompactMode(bool compact)
    {
        if (_compactMode == compact) return;
        if (compact)
        {
            _expandedWindowRect = position; _hasExpandedWindowRect = true;
            _compactMode = true; ApplyWorkspaceSizeLimits();
            if (!docked)
                position = new Rect(_expandedWindowRect.position, Vector2.Max(_compactWindowSize, CompactMinimum));
        }
        else
        {
            _paintingEnabled = false;
            if (!docked) _compactWindowSize = Vector2.Max(position.size, CompactMinimum);
            _compactMode = false; ApplyWorkspaceSizeLimits();
            if (!docked && _hasExpandedWindowRect)
                position = new Rect(_expandedWindowRect.position, Vector2.Max(_expandedWindowRect.size, ExpandedMinimum));
        }
        // Keep room/preset/placement parents, chosen prefab and scroll positions.
        // Docked tabs keep their dock layout; undocked windows actually shrink.
        Repaint();
    }

    private void DrawCompactWorkspace()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Sector Maker · 축소", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("← 이전 화면으로", "배치를 멈추고 원래 크기로 돌아갑니다. Prefab Mode를 닫거나 저장하지 않습니다."), EditorStyles.toolbarButton))
                RequestCompactMode(false);
            Observe("CompactBack");
        }
        GUILayout.Label(_roomPrefab != null ? _roomPrefab.name : "방을 먼저 선택하세요", EditorStyles.boldLabel);
        if (docked) GUILayout.Label("탭을 드래그해 분리하면 작은 창으로 쓸 수 있어요.", EditorStyles.wordWrappedMiniLabel);
        _compactScroll = EditorGUILayout.BeginScrollView(_compactScroll);
        if (!IsEditingRoom)
        {
            EditorGUILayout.HelpBox("방을 Scene에서 열면 배치 버튼을 사용할 수 있어요.", MessageType.Info);
            using (new EditorGUI.DisabledScope(_roomPrefab == null || EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("▶ Scene에서 편집 시작", GUILayout.Height(30))) OpenRoomForEditing();
            Observe("CompactOpenRoom");
            EditorGUILayout.EndScrollView(); return;
        }
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button(_workbenchTab == 2 ? "← 배치로" : "바닥 / 벽 외형 바꾸기"))
            { _paintingEnabled = false; _workbenchTab = _workbenchTab == 2 ? 0 : 2; }
            Observe("CompactAppearance");
            if (_workbenchTab == 2)
            {
                DrawAppearanceWorkbench();
                if (GUILayout.Button("방 저장")) SaveEditingRoom();
                EditorGUILayout.EndScrollView(); return;
            }
            DrawLootPlacementToolbar();
            Observe("CompactTools");
            if (_mode == PlacementMode.FixedSectorStructure)
            {
                _structurePrefab = (GameObject)EditorGUILayout.ObjectField("구조물 프리팹", _structurePrefab, typeof(GameObject), false);
                DrawStructureVisionPlacement();
            }
            else if (_mode == PlacementMode.ContentSlotAnchor)
            {
                _contentSlotId = EditorGUILayout.TextField("스폰 위치 이름", _contentSlotId);
                DrawContentPlanSetup();
                using (new EditorGUI.DisabledScope(!CanEditContentPlan())) DrawLootPlacementDraft();
                DrawLootSpawnLimits();
            }
            bool canStart = _mode == PlacementMode.EnemySpawnPoint ||
                _mode == PlacementMode.FixedSectorStructure && _structurePrefab != null && PrefabUtility.IsPartOfPrefabAsset(_structurePrefab) ||
                _mode == PlacementMode.ContentSlotAnchor && CanEditContentPlan();
            using (new EditorGUI.DisabledScope(!canStart && !_paintingEnabled))
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = _paintingEnabled ? new Color(1, .65f, .4f) : new Color(.5f, .9f, .7f);
                if (GUILayout.Button(_paintingEnabled ? "■ 배치 종료 (Esc)" : "▶ 배치 시작 · Scene 바닥 클릭", GUILayout.Height(34)))
                { if (_paintingEnabled) _paintingEnabled = false; else BeginGuidedPlacement(); }
                Observe("CompactPlacement"); GUI.backgroundColor = old;
            }
            if (GUILayout.Button("선택·이동 모드 — 배치 끄기")) _paintingEnabled = false;
            Observe("CompactSelect");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("방 전체 보기")) FrameRoom(false); Observe("CompactFrame");
                if (GUILayout.Button("위에서 보기")) FrameRoom(true);
                if (GUILayout.Button("방 저장")) SaveEditingRoom(); Observe("CompactSave");
            }
            _compactSettings = EditorGUILayout.Foldout(_compactSettings, "배치 설정 · 높이 / 회전", true);
            if (_compactSettings)
            {
                _rotationY = EditorGUILayout.FloatField("방향 Y", _rotationY);
                if (_mode == PlacementMode.FixedSectorStructure) _structureBottomOffset = EditorGUILayout.FloatField("바닥 위 높이", _structureBottomOffset);
                else if (_mode == PlacementMode.EnemySpawnPoint) _enemySpawnPointYOffset = EditorGUILayout.FloatField("바닥 위 높이", _enemySpawnPointYOffset);
                else _contentSlotYOffset = EditorGUILayout.FloatField("바닥 위 높이", _contentSlotYOffset);
                _selectCreatedObject = EditorGUILayout.Toggle("배치한 물체 선택", _selectCreatedObject);
            }
            GUILayout.Label(_paintingEnabled ? "바닥 클릭: 배치 · Q/E: 회전 · Esc: 종료" : "Scene에서 선택 후 W 이동 / E 회전 / R 크기", EditorStyles.wordWrappedMiniLabel);
            DrawPlacedLootSelection();
            if (_mode == PlacementMode.FixedSectorStructure) DrawPlacedStructureVision();
            if (_paintingEnabled && !TryValidateConfiguration(out string reason))
                EditorGUILayout.HelpBox(reason, MessageType.Warning);
        }
        EditorGUILayout.EndScrollView();
    }
}
