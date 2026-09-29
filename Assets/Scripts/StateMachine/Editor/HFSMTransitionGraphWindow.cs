/*
 * Role hierarchy: StateMachine > Editor
 * Primary responsibility: Visual graph authoring for existing TransitionTableSO assets.
 * Execution boundary: Editor only (VSplatter.StateMachine.Editor)
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    /// <summary>
    /// Animator-like overview for an existing TransitionTableSO. The graph is a view of
    /// the table, not a second source of truth: every edit writes directly to
    /// TransitionTableSO._transitions through SerializedObject.
    /// </summary>
    internal sealed class HFSMTransitionGraphWindow : EditorWindow
    {
        private const string EnemyShooterBaseModePath =
            "Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Enemy_ShooterBase_Mode.asset";
        private const string EnemyShooterBaseAlivePath =
            "Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Enemy_ShooterBase_Alive.asset";
        private const float StateBrowserWidth = 218f;
        private const float InspectorWidth = 330f;
        private const float NodeWidth = 164f;
        private const float NodeHeight = 58f;
        private const float LayerSpacing = 210f;
        private const float RowSpacing = 88f;

        private readonly Dictionary<StateSO, Rect> _nodeRects = new();
        private readonly Dictionary<StateSO, List<StateSO>> _outgoing = new();
        private readonly List<StateSO> _states = new();
        private readonly List<StateSO> _initialStates = new();

        private TransitionTableSO _table;
        private SerializedObject _serializedTable;
        private StateSO _selectedState;
        private Vector2 _graphPan = new(26f, 54f);
        private Vector2 _stateBrowserScroll;
        private Vector2 _inspectorScroll;
        private bool _isPanning;
        private bool _layoutDirty = true;

        [MenuItem("Tools/Authoring/HFSM Transition Graph")]
        // Purpose: Opens the open UI or workflow.
        private static void Open()
        {
            HFSMTransitionGraphWindow window = GetWindow<HFSMTransitionGraphWindow>("HFSM Transition Graph");
            window.minSize = new Vector2(1080f, 560f);

            if (Selection.activeObject is TransitionTableSO selectedTable)
                window.SetTable(selectedTable);

            window.Show();
        }

        // Purpose: Activates runtime subscriptions and refreshes the enabled state.
        private void OnEnable()
        {
            Undo.undoRedoPerformed += RefreshGraph;

            if (_table != null)
                _serializedTable = new SerializedObject(_table);
        }

        // Purpose: Releases active subscriptions and transient state while disabled.
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= RefreshGraph;
        }

        // Purpose: Responds to the selection change callback.
        private void OnSelectionChange()
        {
            if (Selection.activeObject is TransitionTableSO selectedTable && selectedTable != _table)
            {
                SetTable(selectedTable);
                Repaint();
            }
        }

        // Purpose: Responds to the gui callback.
        private void OnGUI()
        {
            DrawToolbar();
            Rect workspaceHeader = DrawWorkspaceHeader();

            if (_table == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a TransitionTableSO in Project, or assign one above. This tool edits the existing transition table directly.",
                    MessageType.Info);
                return;
            }

            EnsureSerializedTable();
            _serializedTable.Update();

            if (_layoutDirty)
                RebuildLayout();

            float contentTop = workspaceHeader.yMax + 8f;
            float contentHeight = Mathf.Max(160f, position.height - contentTop - 8f);
            bool useCompactLayout = position.width < 1080f;
            Rect browserRect;
            Rect graphRect;
            Rect inspectorRect;

            if (useCompactLayout)
            {
                float graphHeight = Mathf.Clamp(contentHeight * 0.52f, 210f, 340f);
                float lowerTop = contentTop + graphHeight + 8f;
                float lowerHeight = Mathf.Max(140f, contentHeight - graphHeight - 8f);
                float browserWidth = Mathf.Clamp(position.width * 0.30f, 190f, StateBrowserWidth);

                graphRect = new Rect(8f, contentTop, Mathf.Max(260f, position.width - 16f), graphHeight);
                browserRect = new Rect(8f, lowerTop, browserWidth, lowerHeight);
                inspectorRect = new Rect(
                    browserRect.xMax + 8f,
                    lowerTop,
                    Mathf.Max(220f, position.width - browserRect.xMax - 16f),
                    lowerHeight);
            }
            else
            {
                browserRect = new Rect(
                    8f,
                    contentTop,
                    StateBrowserWidth,
                    contentHeight);

                graphRect = new Rect(
                    browserRect.xMax + 8f,
                    contentTop,
                    Mathf.Max(200f, position.width - StateBrowserWidth - InspectorWidth - 32f),
                    contentHeight);
                inspectorRect = new Rect(
                    graphRect.xMax + 8f,
                    graphRect.y,
                    Mathf.Max(220f, position.width - graphRect.xMax - 16f),
                    graphRect.height);
            }

            DrawStateBrowser(browserRect);
            HandleGraphPan(graphRect);
            DrawGraph(graphRect);
            DrawInspector(inspectorRect);
        }

        // Purpose: Draws the workspace header authoring UI.
        private static Rect DrawWorkspaceHeader()
        {
            Rect area = GUILayoutUtility.GetRect(1f, 58f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(area, new Color(0.065f, 0.11f, 0.14f));
            GUI.Box(area, GUIContent.none, EditorStyles.helpBox);
            GUI.Label(
                new Rect(area.x + 10f, area.y + 7f, area.width - 20f, 18f),
                "HFSM TRANSITION GRAPH",
                EditorStyles.boldLabel);
            GUI.Label(
                new Rect(area.x + 10f, area.y + 27f, area.width - 20f, 16f),
                "Owner: Combat / AI  •  Choose a Transition Table, select a state, then author only its outgoing transitions.",
                EditorStyles.miniLabel);
            return area;
        }

        // Purpose: Draws the toolbar authoring UI.
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                TransitionTableSO selected = (TransitionTableSO)EditorGUILayout.ObjectField(
                    _table,
                    typeof(TransitionTableSO),
                    false,
                    GUILayout.MinWidth(260f));
                if (EditorGUI.EndChangeCheck())
                    SetTable(selected);

                GUILayout.Space(8f);
                GUILayout.Label("Shared Enemy Shooter", EditorStyles.miniLabel, GUILayout.Width(122f));

                if (GUILayout.Button("Base Mode", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    SetEnemyShooterBaseTable(EnemyShooterBaseModePath);

                if (GUILayout.Button("Base Alive", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    SetEnemyShooterBaseTable(EnemyShooterBaseAlivePath);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(62f)))
                    RefreshGraph();

                if (GUILayout.Button("Frame All", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    _graphPan = new Vector2(26f, 54f);
                    _layoutDirty = true;
                    Repaint();
                }

                using (new EditorGUI.DisabledScope(_table == null))
                {
                    if (GUILayout.Button("Select Asset", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                        Selection.activeObject = _table;
                }
            }
        }

        // Purpose: Draws the graph authoring UI.
        private void DrawGraph(Rect graphRect)
        {
            EditorGUI.DrawRect(graphRect, new Color(0.09f, 0.11f, 0.14f));
            DrawGrid(graphRect, 24f, new Color(1f, 1f, 1f, 0.025f));
            DrawGrid(graphRect, 120f, new Color(0.3f, 0.75f, 1f, 0.06f));

            if (_states.Count == 0)
            {
                GUI.Label(graphRect, "No transitions yet. Select a StateSO in the right panel and add its first outgoing transition.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Handles.BeginGUI();
            foreach (KeyValuePair<StateSO, List<StateSO>> source in _outgoing)
            {
                if (!_nodeRects.TryGetValue(source.Key, out Rect fromRect))
                    continue;

                foreach (StateSO destination in source.Value)
                {
                    if (!_nodeRects.TryGetValue(destination, out Rect toRect))
                        continue;

                    Vector3 from = new Vector3(fromRect.xMax, fromRect.center.y, 0f);
                    Vector3 to = new Vector3(toRect.xMin, toRect.center.y, 0f);
                    bool selectedEdge = source.Key == _selectedState || destination == _selectedState;
                    Handles.color = selectedEdge
                        ? new Color(0.25f, 0.87f, 1f, 0.9f)
                        : new Color(0.66f, 0.73f, 0.8f, 0.55f);
                    Handles.DrawAAPolyLine(3f, from, new Vector3((from.x + to.x) * 0.5f, from.y, 0f), new Vector3((from.x + to.x) * 0.5f, to.y, 0f), to);
                }
            }
            Handles.EndGUI();

            foreach (StateSO state in _states)
            {
                if (!_nodeRects.TryGetValue(state, out Rect nodeRect) || !nodeRect.Overlaps(graphRect))
                    continue;

                bool isInitial = _initialStates.Contains(state);
                bool isSelected = state == _selectedState;
                Color nodeColor = isSelected
                    ? new Color(0.10f, 0.46f, 0.64f, 1f)
                    : isInitial
                        ? new Color(0.23f, 0.40f, 0.22f, 1f)
                        : new Color(0.20f, 0.24f, 0.30f, 1f);
                EditorGUI.DrawRect(nodeRect, nodeColor);
                GUI.Box(nodeRect, GUIContent.none, EditorStyles.helpBox);

                Rect titleRect = new Rect(nodeRect.x + 8f, nodeRect.y + 7f, nodeRect.width - 16f, 20f);
                GUI.Label(titleRect, state.name, EditorStyles.boldLabel);
                string subLabel = isInitial ? "Initial state" : $"Outgoing: {GetOutgoingCount(state)}";
                GUI.Label(
                    new Rect(nodeRect.x + 8f, nodeRect.y + 31f, nodeRect.width - 16f, 18f),
                    subLabel,
                    EditorStyles.miniLabel);

                if (GUI.Button(nodeRect, GUIContent.none, GUIStyle.none))
                {
                    _selectedState = state;
                    Repaint();
                }
            }

            GUI.Label(
                new Rect(graphRect.x + 10f, graphRect.y + 8f, graphRect.width - 20f, 18f),
                $"{_table.name}  |  {_states.Count} states  |  {_serializedTable.FindProperty("_transitions").arraySize} transitions  |  middle-drag to pan",
                EditorStyles.miniLabel);

            if (IsSharedEnemyShooterBaseTable(_table))
            {
                GUI.Label(
                    new Rect(graphRect.x + 10f, graphRect.y + 26f, graphRect.width - 20f, 18f),
                    "SHARED BASE: all EnemyShooter prefab variants use this table. Configure archetype, loadout, and capability toggles on EnemyShooterConfigSO.",
                    EditorStyles.miniLabel);
                GUI.Label(
                    new Rect(graphRect.x + 10f, graphRect.y + 44f, graphRect.width - 20f, 18f),
                    "Create a derived table only for a genuinely new phase/state loop. Weapon type, melee/ranged switching, throwable, dash, cover, flank, and stats remain Config/Loadout work.",
                    EditorStyles.miniLabel);
            }
        }

        // Purpose: Selects one of the two shared enemy shooter transition tables.
        private void SetEnemyShooterBaseTable(string assetPath)
        {
            TransitionTableSO table = AssetDatabase.LoadAssetAtPath<TransitionTableSO>(assetPath);
            if (table == null)
            {
                Debug.LogError($"[HFSM Transition Graph] Shared Enemy Shooter table is missing. path={assetPath}");
                return;
            }

            SetTable(table);
        }

        // Purpose: Checks whether the selected table is part of the shared enemy shooter FSM contract.
        private static bool IsSharedEnemyShooterBaseTable(TransitionTableSO table)
        {
            if (table == null)
                return false;

            return table == AssetDatabase.LoadAssetAtPath<TransitionTableSO>(EnemyShooterBaseModePath) ||
                   table == AssetDatabase.LoadAssetAtPath<TransitionTableSO>(EnemyShooterBaseAlivePath);
        }

        // Purpose: Draws the state browser authoring UI.
        private void DrawStateBrowser(Rect browserRect)
        {
            GUI.Box(browserRect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(browserRect.x + 8f, browserRect.y + 8f, browserRect.width - 16f, browserRect.height - 16f));
            using (EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_stateBrowserScroll))
            {
                _stateBrowserScroll = scroll.scrollPosition;
                EditorGUILayout.LabelField("STATE CATALOG", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Asset-style state cards. Select one to inspect only its outgoing transitions.", EditorStyles.miniLabel);
                EditorGUILayout.Space(4f);

                if (_states.Count == 0)
                {
                    EditorGUILayout.HelpBox("No states are available until this table contains at least one transition.", MessageType.Info);
                }
                else
                {
                    foreach (StateSO state in _states)
                    {
                        if (state == null)
                            continue;

                        bool isSelected = state == _selectedState;
                        Color originalColor = GUI.backgroundColor;
                        if (isSelected)
                            GUI.backgroundColor = new Color(0.16f, 0.58f, 0.72f, 0.78f);

                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            string stateLabel = _initialStates.Contains(state)
                                ? $"● {state.name}"
                                : state.name;
                            if (GUILayout.Button(stateLabel, isSelected ? EditorStyles.boldLabel : EditorStyles.label))
                            {
                                _selectedState = state;
                                Repaint();
                            }

                            EditorGUILayout.LabelField(
                                _initialStates.Contains(state)
                                    ? "Initial state  |  Transition entry point"
                                    : $"Outgoing transitions: {GetOutgoingCount(state)}",
                                EditorStyles.miniLabel);
                        }

                        GUI.backgroundColor = originalColor;
                    }
                }
            }
            GUILayout.EndArea();
        }

        // Purpose: Draws the inspector authoring UI.
        private void DrawInspector(Rect inspectorRect)
        {
            GUI.Box(inspectorRect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(inspectorRect.x + 8f, inspectorRect.y + 8f, inspectorRect.width - 16f, inspectorRect.height - 16f));
            using (EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_inspectorScroll))
            {
                _inspectorScroll = scroll.scrollPosition;
                EditorGUILayout.LabelField("STATE INSPECTOR", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Outgoing transitions for the selected state.", EditorStyles.miniLabel);
                EditorGUILayout.Space(3f);

                EditorGUI.BeginChangeCheck();
                StateSO chosenState = (StateSO)EditorGUILayout.ObjectField(
                    "State",
                    _selectedState,
                    typeof(StateSO),
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedState = chosenState;
                    Repaint();
                }

                if (_selectedState == null)
                {
                    EditorGUILayout.HelpBox(
                        "Click a graph node or assign a StateSO. The panel then shows every outgoing transition exactly as stored in this table.",
                        MessageType.Info);
                    GUILayout.EndArea();
                    return;
                }

                if (GUILayout.Button("Select State Asset"))
                    Selection.activeObject = _selectedState;

                EditorGUILayout.Space(7f);
                SerializedProperty transitions = _serializedTable.FindProperty("_transitions");
                int deleteIndex = -1;
                bool changed = false;
                int displayed = 0;

                for (int index = 0; index < transitions.arraySize; index++)
                {
                    SerializedProperty transition = transitions.GetArrayElementAtIndex(index);
                    SerializedProperty from = transition.FindPropertyRelative("FromState");
                    if (from.objectReferenceValue != _selectedState)
                        continue;

                    displayed++;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Transition {displayed}", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(transition.FindPropertyRelative("ToState"), new GUIContent("To State"));
                    EditorGUILayout.PropertyField(transition.FindPropertyRelative("Conditions"), true);
                    changed |= EditorGUI.EndChangeCheck();

                    if (GUILayout.Button("Delete Transition"))
                        deleteIndex = index;
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(3f);
                }

                if (displayed == 0)
                {
                    EditorGUILayout.HelpBox(
                        "This state has no outgoing transition. Add one below. A transition without conditions is evaluated as an unconditional transition by the runtime table.",
                        MessageType.Warning);
                }

                if (deleteIndex >= 0)
                {
                    Undo.RecordObject(_table, "Delete HFSM transition");
                    transitions.DeleteArrayElementAtIndex(deleteIndex);
                    CommitTableChanges();
                    GUILayout.EndArea();
                    return;
                }

                if (changed)
                    CommitTableChanges();

                EditorGUILayout.Space(5f);
                if (GUILayout.Button("Add Outgoing Transition"))
                {
                    Undo.RecordObject(_table, "Add HFSM transition");
                    int newIndex = transitions.arraySize;
                    transitions.InsertArrayElementAtIndex(newIndex);
                    SerializedProperty added = transitions.GetArrayElementAtIndex(newIndex);
                    added.FindPropertyRelative("FromState").objectReferenceValue = _selectedState;
                    added.FindPropertyRelative("ToState").objectReferenceValue = null;
                    added.FindPropertyRelative("Conditions").ClearArray();
                    CommitTableChanges();
                }
            }
            GUILayout.EndArea();
        }

        // Purpose: Repairs or rebuilds layout from the current source data.
        private void RebuildLayout()
        {
            _layoutDirty = false;
            _states.Clear();
            _outgoing.Clear();
            _initialStates.Clear();
            _nodeRects.Clear();

            if (_serializedTable == null)
                return;

            SerializedProperty transitions = _serializedTable.FindProperty("_transitions");
            if (transitions == null)
                return;

            for (int index = 0; index < transitions.arraySize; index++)
            {
                SerializedProperty transition = transitions.GetArrayElementAtIndex(index);
                StateSO from = transition.FindPropertyRelative("FromState").objectReferenceValue as StateSO;
                StateSO to = transition.FindPropertyRelative("ToState").objectReferenceValue as StateSO;

                AddState(from);
                AddState(to);

                if (index == 0 && from != null)
                    _initialStates.Add(from);

                if (from == null || to == null)
                    continue;

                if (!_outgoing.TryGetValue(from, out List<StateSO> destinations))
                {
                    destinations = new List<StateSO>();
                    _outgoing.Add(from, destinations);
                }

                if (!destinations.Contains(to))
                    destinations.Add(to);
            }

            Dictionary<StateSO, int> layers = new();
            Queue<StateSO> pending = new();
            foreach (StateSO initial in _initialStates)
            {
                layers[initial] = 0;
                pending.Enqueue(initial);
            }

            while (pending.Count > 0)
            {
                StateSO current = pending.Dequeue();
                if (!_outgoing.TryGetValue(current, out List<StateSO> destinations))
                    continue;

                foreach (StateSO destination in destinations)
                {
                    int nextLayer = layers[current] + 1;
                    if (layers.TryGetValue(destination, out int existingLayer) && existingLayer <= nextLayer)
                        continue;

                    layers[destination] = nextLayer;
                    pending.Enqueue(destination);
                }
            }

            int fallbackLayer = 0;
            foreach (StateSO state in _states)
            {
                if (layers.ContainsKey(state))
                    continue;

                layers[state] = fallbackLayer;
                fallbackLayer++;
            }

            Dictionary<int, int> rowByLayer = new();
            foreach (StateSO state in _states)
            {
                int layer = layers[state];
                rowByLayer.TryGetValue(layer, out int row);
                rowByLayer[layer] = row + 1;
                _nodeRects[state] = new Rect(
                    _graphPan.x + layer * LayerSpacing,
                    _graphPan.y + row * RowSpacing,
                    NodeWidth,
                    NodeHeight);
            }
        }

        // Purpose: Adds the requested state to this component.
        private void AddState(StateSO state)
        {
            if (state != null && !_states.Contains(state))
                _states.Add(state);
        }

        // Purpose: Returns the current outgoing count.
        private int GetOutgoingCount(StateSO state)
        {
            return _outgoing.TryGetValue(state, out List<StateSO> destinations)
                ? destinations.Count
                : 0;
        }

        // Purpose: Handles the incoming graph pan request or event.
        private void HandleGraphPan(Rect graphRect)
        {
            Event currentEvent = Event.current;
            if (currentEvent.button != 2)
                return;

            if (currentEvent.type == EventType.MouseDown && graphRect.Contains(currentEvent.mousePosition))
            {
                _isPanning = true;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && _isPanning)
            {
                _graphPan += currentEvent.delta;
                _layoutDirty = true;
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.type == EventType.MouseUp && _isPanning)
            {
                _isPanning = false;
                currentEvent.Use();
            }
        }

        // Purpose: Draws the grid authoring UI.
        private static void DrawGrid(Rect area, float spacing, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            for (float x = area.x; x < area.xMax; x += spacing)
                Handles.DrawLine(new Vector3(x, area.y, 0f), new Vector3(x, area.yMax, 0f));
            for (float y = area.y; y < area.yMax; y += spacing)
                Handles.DrawLine(new Vector3(area.x, y, 0f), new Vector3(area.xMax, y, 0f));
            Handles.EndGUI();
        }

        // Purpose: Updates the table state.
        private void SetTable(TransitionTableSO table)
        {
            if (_table == table)
                return;

            _table = table;
            _serializedTable = _table != null ? new SerializedObject(_table) : null;
            _selectedState = null;
            _graphPan = new Vector2(26f, 54f);
            _layoutDirty = true;
        }

        // Purpose: Ensures serialized table is ready before the workflow continues.
        private void EnsureSerializedTable()
        {
            if (_serializedTable == null || _serializedTable.targetObject != _table)
                _serializedTable = new SerializedObject(_table);
        }

        // Purpose: Commits the current table changes selection or change.
        private void CommitTableChanges()
        {
            _serializedTable.ApplyModifiedProperties();
            EditorUtility.SetDirty(_table);
            _layoutDirty = true;
            Repaint();
        }

        // Purpose: Refreshes the displayed or cached graph.
        private void RefreshGraph()
        {
            if (_table != null)
                _serializedTable = new SerializedObject(_table);

            _layoutDirty = true;
            Repaint();
        }
    }
}
