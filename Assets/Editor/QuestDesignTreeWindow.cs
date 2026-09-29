/*
 * Role hierarchy: Authoring > Editor > Progression > Quests
 * Primary responsibility: Quest Maker branch graph, objectives and rewards. No in-game UI changes.
 */
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Keep class/file/GUID stable for existing layouts and authoring hub navigation.
public sealed partial class QuestDesignTreeWindow : EditorWindow
{
    [SerializeField] private QuestLineSO _questLine;
    [SerializeField] private QuestDefinitionSO _selectedQuest;
    [SerializeField] private int _branchIndex;
    [SerializeField] private float _zoom = .8f;
    [SerializeField] private Vector2 _pan;
    private List<QuestLineSO> _lines = new();
    private QuestMakerGraph.Layout _layout;
    private Vector2 _lineScrollPosition, _detailsScrollPosition;
    private string _search = "", _newQuestName = "New Quest";
    private QuestDefinitionSO _questToAdd, _linkSource, _prerequisiteToAdd, _nextQuestToLink;
    private int _detailTab;
    private bool _showSetSettings, _showAdvanced, _frameRequested = true;
    private bool _fitAllRequested;
    private readonly List<string> _lineValidationIssues = new();
    private bool _hasValidatedLine;
    private Rect _lastViewport;
    private QuestBranch? Filter => _branchIndex > 0 ? QuestMakerGraph.Branches[Mathf.Clamp(_branchIndex - 1, 0, 2)] : null;

    [MenuItem("Tools/Authoring/Quest Maker")]
    public static void Open()
    {
        var window = GetWindow<QuestDesignTreeWindow>("Quest Maker");
        window.minSize = new Vector2(1080, 620);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Quest Maker");
        minSize = new Vector2(1080, 620);
        wantsMouseMove = true;
        RestorePanelWidths();
        RefreshCatalog();
        Undo.undoRedoPerformed += Invalidate;
    }
    private void OnDisable() { Undo.undoRedoPerformed -= Invalidate; }
    private void OnProjectChange() { RefreshCatalog(); }
    private void Invalidate() { _layout = null; _hasValidatedLine = false; Repaint(); }

    private void RefreshCatalog()
    {
        _lines.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:QuestLineSO"))
        {
            var line = AssetDatabase.LoadAssetAtPath<QuestLineSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (line != null) _lines.Add(line);
        }
        _lines.Sort((a,b) => string.Compare(a.DisplayName,b.DisplayName,StringComparison.OrdinalIgnoreCase));
        if (_questLine == null)
        {
            // Show existing authored content on first open, not an empty assignment screen.
            int largest = -1;
            foreach (var line in _lines)
            {
                if (AssetDatabase.GetAssetPath(line).Contains("/ContentTemplates/")) continue;
                if (line.Quests.Length <= largest) continue;
                largest = line.Quests.Length; _questLine = line;
            }
        }
        Invalidate();
    }

    private void OnGUI()
    {
        DrawToolbar();
        float height = Mathf.Max(490, position.height - 56);
        DrawResizablePanels(height);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Arrows = prerequisites. All incoming quests must be completed (AND).", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Design tool only - NPC offerings are configured separately.", EditorStyles.miniLabel);
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Quest Maker", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Quest Set", GUILayout.Width(58));
            var selected = (QuestLineSO)EditorGUILayout.ObjectField(_questLine, typeof(QuestLineSO), false, GUILayout.MinWidth(170));
            if (selected != _questLine) SelectLine(selected);
            if (GUILayout.Button("New Set...", EditorStyles.toolbarButton, GUILayout.Width(76))) CreateLine();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(62))) RefreshCatalog();
            using (new EditorGUI.DisabledScope(_questLine == null))
            {
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(62))) { ValidateQuestLine(); _detailTab = 4; }
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) SaveCurrentSet();
            }
        }
    }

    private void SelectLine(QuestLineSO line)
    {
        _questLine = line; _selectedQuest = null; CancelConnection();
        _edgeSource = null; _edgeTarget = null;
        _search = ""; _frameRequested = true; _pan = Vector2.zero;
        Invalidate();
    }

    private void DrawBrowser(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height)))
        {
            GUILayout.Label("QUEST BRANCHES", EditorStyles.boldLabel);
            if (_lines.Count > 0)
            {
                int current = _lines.IndexOf(_questLine);
                var labels = _lines.ConvertAll(l => l.DisplayName).ToArray();
                EditorGUI.BeginChangeCheck();
                int next = EditorGUILayout.Popup(Mathf.Max(0,current), labels);
                if (EditorGUI.EndChangeCheck()) SelectLine(_lines[next]);
            }
            var quests = GetLineQuests();
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                int count = i == 0 ? quests.Count : quests.FindAll(q => q.Branch == QuestMakerGraph.Branches[i-1]).Count;
                string label = i == 0 ? "All Branches" : QuestMakerGraph.BranchLabel(QuestMakerGraph.Branches[i-1]);
                var color = GUI.backgroundColor;
                if (_branchIndex == i) GUI.backgroundColor = i == 0 ? new Color(.2f,.7f,.8f) : BranchColor(QuestMakerGraph.Branches[i-1]) * 1.5f;
                if (GUILayout.Button(label + " (" + count + ")", GUILayout.Height(30)))
                { _branchIndex = index; _search = ""; _selectedQuest = null; _frameRequested = true; Invalidate(); }
                GUI.backgroundColor = color;
            }
            GUILayout.Space(6);
            EditorGUI.BeginChangeCheck(); _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck()) { _frameRequested = true; Invalidate(); }
            _lineScrollPosition = EditorGUILayout.BeginScrollView(_lineScrollPosition);
            var questStyle = new GUIStyle(EditorStyles.miniButton) { wordWrap = true, fixedHeight = 0, alignment = TextAnchor.MiddleLeft };
            float listWidth = Mathf.Max(100, width - 30);
            foreach (var quest in quests)
            {
                if (Filter.HasValue && quest.Branch != Filter.Value) continue;
                if (!string.IsNullOrEmpty(_search) && quest.DisplayName.IndexOf(_search,StringComparison.OrdinalIgnoreCase)<0 &&
                    quest.QuestId.IndexOf(_search,StringComparison.OrdinalIgnoreCase)<0) continue;
                var color = GUI.backgroundColor;
                if (quest == _selectedQuest) GUI.backgroundColor = new Color(.25f,.7f,.85f);
                var content = new GUIContent(quest.DisplayName, quest.DisplayName + "\n" + quest.QuestId);
                if (GUILayout.Button(content, questStyle, GUILayout.Width(listWidth), GUILayout.Height(Mathf.Max(28, questStyle.CalcHeight(content,listWidth)))))
                { SelectQuest(quest); FocusQuest(quest); }
                GUI.backgroundColor = color;
            }
            EditorGUILayout.EndScrollView();
            using (new EditorGUI.DisabledScope(_questLine == null))
            {
                GUILayout.Label("CREATE QUEST", EditorStyles.miniBoldLabel);
                _newQuestName = EditorGUILayout.TextField(_newQuestName);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Root", GUILayout.Height(26))) CreateQuest(null, Filter ?? QuestBranch.Main);
                    using (new EditorGUI.DisabledScope(_selectedQuest == null))
                        if (GUILayout.Button("+ Next", GUILayout.Height(26))) CreateQuest(_selectedQuest, Filter ?? _selectedQuest.Branch);
                }
                _showSetSettings = EditorGUILayout.Foldout(_showSetSettings, "Existing Quests / Set", true);
                if (_showSetSettings && _questLine != null)
                {
                    _questToAdd = (QuestDefinitionSO)EditorGUILayout.ObjectField(_questToAdd,typeof(QuestDefinitionSO),false);
                    using (new EditorGUI.DisabledScope(_questToAdd == null))
                        if (GUILayout.Button("Add to this set"))
                        { QuestMakerGraph.AddToLine(_questLine,_questToAdd); SelectQuest(_questToAdd); _questToAdd = null; Invalidate(); _frameRequested = true; }
                    using var line = new SerializedObject(_questLine);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(line.FindProperty("_displayName"), GUIContent.none);
                    if (EditorGUI.EndChangeCheck()) { line.ApplyModifiedProperties(); Invalidate(); }
                    if (GUILayout.Button("Select Set Asset")) SelectAsset(_questLine);
                }
            }
        }
    }

    private void DrawFlow(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height)))
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(Filter.HasValue ? QuestMakerGraph.BranchLabel(Filter.Value) : "ALL QUEST FLOW", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Fit", EditorStyles.toolbarButton, GUILayout.Width(30)))
                { _frameRequested = true; _fitAllRequested = true; }
                if (GUILayout.Button("1:1", EditorStyles.toolbarButton, GUILayout.Width(30))) ZoomAt(_lastViewport.center,1);
                using (new EditorGUI.DisabledScope(_selectedQuest == null))
                    if (GUILayout.Button("+ Branch", EditorStyles.toolbarButton, GUILayout.Width(65))) ShowBranchMenu();
            }
            if (HasPendingConnection)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(ConnectionHint, EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("Cancel", GUILayout.Width(52))) CancelConnection();
                }
            }
            Rect area = GUILayoutUtility.GetRect(50,50,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
            DrawGraph(area);
            GUILayout.Label("Right-click: edit quest / arrow   |   Wheel: zoom   |   Middle/right drag: pan   |   Select arrow + Del: unlink",
                EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void DrawGraph(Rect area)
    {
        _lastViewport = area;
        EditorGUI.DrawRect(area, new Color(.075f,.09f,.115f));
        _layout ??= QuestMakerGraph.Build(GetLineQuests(),Filter,_search);
        if (_frameRequested && Event.current.type == EventType.Repaint)
        {
            float fit = Mathf.Min((area.width-24)/_layout.Size.x,(area.height-24)/_layout.Size.y);
            _zoom = Mathf.Clamp(fit, _fitAllRequested ? .025f : .7f, 1);
            _pan = (_layout.Size * _zoom - area.size) * .5f;
            if (!_fitAllRequested && fit < .7f && _layout.Focus.Count > 0)
            {
                // Long chains should open as readable cards, not a strip of tiny rectangles.
                QuestDefinitionSO first = null;
                foreach (var quest in _layout.Nodes)
                    if (_layout.Focus.Contains(quest) && (first == null ||
                        _layout.Rects[quest].x < _layout.Rects[first].x ||
                        (_layout.Rects[quest].x == _layout.Rects[first].x && _layout.Rects[quest].y < _layout.Rects[first].y))) first = quest;
                if (_selectedQuest != null && _layout.Focus.Contains(_selectedQuest)) first = _selectedQuest;
                _pan = _layout.Rects[first].position * _zoom - new Vector2(20, 45);
            }
            _frameRequested = false;
            _fitAllRequested = false;
        }
        HandleGraphInput(area);
        HandleQuestDrop(area);
        _layout ??= QuestMakerGraph.Build(GetLineQuests(),Filter,_search);
        GUI.BeginGroup(area);
        try
        {
            if (_questLine == null || _layout.Nodes.Count == 0)
            {
                GUI.Label(new Rect(20,24,Mathf.Max(100,area.width-40),90),
                    _questLine == null ? "Choose a Quest Set on the left, or create a New Set.\n\nExisting sets are discovered automatically." :
                    "No quests in this branch yet.\n\nClick + Root to begin a chain. Use + Next to continue it.\nDrop existing Quest assets here to add them.",
                    new GUIStyle(EditorStyles.wordWrappedLabel) { normal = { textColor = Color.gray } });
                return;
            }
            foreach (var lane in _layout.Lanes)
            {
                Rect logical = lane.Value; logical.width = _layout.Size.x - 16;
                Rect screen = ScreenRect(logical);
                var color = BranchColor(lane.Key);
                EditorGUI.DrawRect(screen,new Color(color.r,color.g,color.b,.10f));
                EditorGUI.DrawRect(new Rect(screen.x,screen.y,screen.width,24*_zoom),new Color(color.r,color.g,color.b,.4f));
                GUI.Label(new Rect(screen.x+6,screen.y,screen.width,26),QuestMakerGraph.BranchLabel(lane.Key),ScaledStyle(true));
            }
            foreach (var quest in _layout.Nodes)
                foreach (var parent in quest.Prerequisites)
                    if (parent != null && _layout.Rects.TryGetValue(parent,out var from) && _layout.Rects.TryGetValue(quest,out var to))
                        DrawConnection(ScreenRect(from),ScreenRect(to),quest == _selectedQuest || parent == _selectedQuest,
                            (parent == _edgeSource && quest == _edgeTarget) || (parent == _hoverSource && quest == _hoverTarget));
            foreach (var quest in _layout.Nodes) DrawNode(quest,ScreenRect(_layout.Rects[quest]));
            DrawPendingConnection();
        }
        finally { GUI.EndGroup(); }
    }

    private Rect ScreenRect(Rect rect) => new(rect.position * _zoom - _pan, rect.size * _zoom);
    private GUIStyle ScaledStyle(bool bold = false) => new(bold ? EditorStyles.boldLabel : EditorStyles.miniLabel)
    { fontSize = Mathf.Max(8,Mathf.RoundToInt((bold?12:11)*_zoom)), clipping = TextClipping.Clip,
        normal = { textColor = new Color(.88f,.92f,.95f) } };
    private static Color BranchColor(QuestBranch branch) => branch switch
    { QuestBranch.Main => new Color(.12f,.55f,.7f), QuestBranch.Stats => new Color(.22f,.6f,.38f),
      QuestBranch.Item => new Color(.72f,.45f,.16f), _ => Color.gray };

    private void DrawNode(QuestDefinitionSO quest, Rect rect)
    {
        bool context = !_layout.Focus.Contains(quest);
        var color = BranchColor(quest.Branch);
        EditorGUI.DrawRect(rect, quest == _selectedQuest ? new Color(.16f,.38f,.48f) :
            context ? new Color(.14f,.16f,.18f) : new Color(.11f,.16f,.21f));
        EditorGUI.DrawRect(new Rect(rect.x,rect.y,rect.width,4*_zoom),color);
        if (quest == _selectedQuest) GUI.Box(rect,GUIContent.none,EditorStyles.helpBox);
        if (_zoom < .45f)
        {
            if (_zoom >= .16f) GUI.Label(new Rect(rect.x+3,rect.y+4,rect.width-6,rect.height-8),quest.DisplayName,ScaledStyle());
            if (GUI.Button(rect,new GUIContent("",quest.DisplayName+" - click to focus"),GUIStyle.none))
            {
                if (HasPendingConnection) CompleteConnection(quest);
                else { SelectQuest(quest); FocusQuest(quest); }
                GUIUtility.ExitGUI();
            }
            return;
        }
        GUI.Label(new Rect(rect.x+8*_zoom,rect.y+9*_zoom,rect.width-16*_zoom,24*_zoom),quest.DisplayName,ScaledStyle(true));
        string goal = quest.Objectives.Length > 0 && quest.Objectives[0] != null ?
            quest.Objectives[0].BuildSummary()+" x"+quest.Objectives[0].RequiredCount : "Draft: set objectives later";
        GUI.Label(new Rect(rect.x+8*_zoom,rect.y+38*_zoom,rect.width-16*_zoom,19*_zoom),goal,ScaledStyle());
        GUI.Label(new Rect(rect.x+8*_zoom,rect.y+59*_zoom,rect.width-16*_zoom,19*_zoom),"Reward: "+BuildQuestRewardPreview(quest),ScaledStyle());
        string note = context ? "Referenced prerequisite" : quest.Prerequisites.Length == 0 ? "START" : "Needs "+quest.Prerequisites.Length+" prerequisite(s)";
        GUI.Label(new Rect(rect.x+8*_zoom,rect.y+80*_zoom,rect.width-16*_zoom,19*_zoom),note,ScaledStyle());
        if (GUI.Button(new Rect(rect.x,rect.y,rect.width,100*_zoom),GUIContent.none,GUIStyle.none))
        {
            if (HasPendingConnection) { CompleteConnection(quest); GUIUtility.ExitGUI(); }
            else SelectQuest(quest);
        }
        if (GUI.Button(new Rect(rect.x+8*_zoom,rect.yMax-27*_zoom,100*_zoom,22*_zoom),"+ Next",ScaledButton()))
            CreateQuest(quest,Filter ?? quest.Branch);
        if (GUI.Button(new Rect(rect.xMax-112*_zoom,rect.yMax-27*_zoom,104*_zoom,22*_zoom),"Link >",ScaledButton()))
        { BeginLink(quest); }
    }
    private GUIStyle ScaledButton() => new(EditorStyles.miniButton) { fontSize = Mathf.Max(8,Mathf.RoundToInt(11*_zoom)) };
    private static void DrawConnection(Rect source,Rect target,bool highlighted,bool selected)
    {
        Vector3 start = new(source.xMax,source.center.y), end = new(target.xMin,target.center.y);
        Color color = selected ? new Color(1,.75f,.25f) : highlighted ? new Color(.3f,.95f,1) : new Color(.38f,.57f,.65f,.75f);
        Handles.DrawBezier(start,end,start+Vector3.right*30,end-Vector3.right*30,color,null,highlighted||selected?3:2);
        Handles.color=color;
        Handles.DrawAAConvexPolygon(end,end+new Vector3(-7,-4),end+new Vector3(-7,4));
    }

    private void HandleQuestDrop(Rect area)
    {
        Event e=Event.current;
        if(_questLine==null || !area.Contains(e.mousePosition) || (e.type!=EventType.DragUpdated && e.type!=EventType.DragPerform)) return;
        var quests=new List<QuestDefinitionSO>();
        foreach(var item in DragAndDrop.objectReferences) if(item is QuestDefinitionSO quest) quests.Add(quest);
        if(quests.Count==0) return;
        DragAndDrop.visualMode=DragAndDropVisualMode.Copy;
        if(e.type==EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach(var quest in quests) QuestMakerGraph.AddToLine(_questLine,quest);
            SelectQuest(quests[0]); Invalidate(); _frameRequested=true;
        }
        e.Use();
    }

    private void FocusQuest(QuestDefinitionSO quest)
    {
        _layout ??= QuestMakerGraph.Build(GetLineQuests(),Filter,_search);
        if(_layout.Rects.TryGetValue(quest,out var rect))
        { _zoom=Mathf.Max(.75f,_zoom); _pan=rect.center*_zoom-_lastViewport.size*.5f; _frameRequested=false; }
    }
    private void SelectQuest(QuestDefinitionSO quest) { _selectedQuest=quest; _detailsScrollPosition=Vector2.zero; Repaint(); }
    private void ShowBranchMenu()
    {
        var source=_selectedQuest; if(source==null) return;
        var menu=new GenericMenu();
        foreach(var branch in QuestMakerGraph.Branches)
        { var captured=branch; menu.AddItem(new GUIContent("New "+QuestMakerGraph.BranchLabel(branch)+" after selected"),false,()=>CreateQuest(source,captured)); }
        menu.ShowAsContext();
    }

    private void DrawInspector(float width,float height)
    {
        using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox,GUILayout.Width(width),GUILayout.Height(height)))
        {
            GUILayout.Label("QUEST WORKBENCH",EditorStyles.boldLabel);
            _detailTab=GUILayout.Toolbar(_detailTab,new[]{"Details","Goals","Rewards","Links","Check"},GUILayout.Height(26));
            _detailsScrollPosition=EditorGUILayout.BeginScrollView(_detailsScrollPosition);
            float label=EditorGUIUtility.labelWidth; EditorGUIUtility.labelWidth=112;
            try
            {
                if(_detailTab==4) DrawChecks();
                else if(_selectedQuest==null)
                    EditorGUILayout.HelpBox("Click a quest card in the tree.\nUse + Root to start or + Next to continue a chain.",MessageType.Info);
                else
                {
                    if(!GetLineQuests().Contains(_selectedQuest))
                        EditorGUILayout.HelpBox("This is a prerequisite outside the current set. Changes edit its original asset.",MessageType.Warning);
                    using var serialized=new SerializedObject(_selectedQuest); serialized.Update();
                    if(_detailTab==0) DrawDetails(serialized);
                    else if(_detailTab==1) DrawGoals(serialized);
                    else if(_detailTab==2) DrawRewards(serialized);
                    else DrawLinks();
                    if(serialized.hasModifiedProperties) { serialized.ApplyModifiedProperties(); Invalidate(); }
                }
            }
            finally { EditorGUIUtility.labelWidth=label; }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawDetails(SerializedObject serialized)
    {
        EditorGUILayout.PropertyField(serialized.FindProperty("_displayName"),new GUIContent("Quest name"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_description"),new GUIContent("Description"));
        int branch=Array.IndexOf(QuestMakerGraph.Branches,_selectedQuest.Branch);
        EditorGUI.BeginChangeCheck();
        int next=EditorGUILayout.Popup("Branch",Mathf.Max(0,branch),new[]{"MainQuest","StatUnlock","GearUnlock"});
        if(EditorGUI.EndChangeCheck())
        {
            serialized.FindProperty("_branch").intValue=(int)QuestMakerGraph.Branches[next];
            if (_branchIndex > 0) _branchIndex = next + 1;
            _frameRequested=true;
        }
        EditorGUILayout.HelpBox("Branch also selects the Home NPC role (Main / Stats / Item). It does not automatically add this quest to an NPC's offered list.",MessageType.None);
        GUILayout.Label("Goal: "+(_selectedQuest.Objectives.Length==0?"Not set yet":_selectedQuest.Objectives[0]?.BuildSummary()),EditorStyles.wordWrappedMiniLabel);
        GUILayout.Label("Reward: "+BuildQuestRewardPreview(_selectedQuest),EditorStyles.wordWrappedMiniLabel);
        if(GUILayout.Button("Select Quest Asset")) SelectAsset(_selectedQuest);
        if(GUILayout.Button("Duplicate as new quest (no links)")) DuplicateQuest();
        using(new EditorGUI.DisabledScope(!GetLineQuests().Contains(_selectedQuest)))
            if(GUILayout.Button("Remove from this set...") && EditorUtility.DisplayDialog("Remove from set?",
                "The quest asset and all prerequisite links will remain. Other quests may still show it as a referenced prerequisite.","Remove from set","Cancel"))
            { QuestMakerGraph.RemoveFromLine(_questLine,_selectedQuest); _selectedQuest=null; Invalidate(); GUIUtility.ExitGUI(); }
        _showAdvanced=EditorGUILayout.Foldout(_showAdvanced,"Advanced: ID / Audio",true);
        if(_showAdvanced)
        {
            EditorGUILayout.HelpBox("Quest ID is used by saves. Do not rename IDs already used in player saves.",MessageType.Warning);
            EditorGUILayout.PropertyField(serialized.FindProperty("_questId"),new GUIContent("Quest ID"));
            EditorGUILayout.PropertyField(serialized.FindProperty("_persistentRewardSummary"),new GUIContent("Legacy reward note"));
            EditorGUILayout.PropertyField(serialized.FindProperty("_acceptedAudioCue"),new GUIContent("Accepted audio"));
            EditorGUILayout.PropertyField(serialized.FindProperty("_completionAudioCue"),new GUIContent("Completed audio"));
        }
    }

    private void DrawGoals(SerializedObject serialized)
    {
        var goals=serialized.FindProperty("_objectives");
        if(goals.arraySize==0) EditorGUILayout.HelpBox("You can design links first. Add the actual goal when ready.",MessageType.Info);
        for(int i=0;i<goals.arraySize;i++)
        {
            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Goal "+(i+1),EditorStyles.boldLabel);
                    if(GUILayout.Button("Remove",GUILayout.Width(60))) { goals.DeleteArrayElementAtIndex(i); ApplyArrayEdit(serialized); }
                }
                DrawObjectiveFields(goals.GetArrayElementAtIndex(i));
            }
        }
        GUILayout.Space(6);
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("+ Kill")) AddGoal(serialized,QuestObjectiveType.Kill);
            if(GUILayout.Button("+ Hit")) AddGoal(serialized,QuestObjectiveType.Hit);
            if(GUILayout.Button("+ Collect")) AddGoal(serialized,QuestObjectiveType.CollectItem);
        }
    }

    private void DrawRewards(SerializedObject serialized)
    {
        var rewards=serialized.FindProperty("_rewards");
        if(rewards.arraySize==0) EditorGUILayout.HelpBox("Choose what completing this quest grants.",MessageType.Info);
        for(int i=0;i<rewards.arraySize;i++)
        {
            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Reward "+(i+1),EditorStyles.boldLabel);
                    if(GUILayout.Button("Remove",GUILayout.Width(60))) { rewards.DeleteArrayElementAtIndex(i); ApplyArrayEdit(serialized); }
                }
                DrawRewardFields(rewards.GetArrayElementAtIndex(i));
            }
        }
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("+ Item")) AddReward(serialized,QuestRewardType.Item);
            if(GUILayout.Button("+ Stat")) AddReward(serialized,QuestRewardType.Stat);
        }
        if(GUILayout.Button("+ Gear / Shop Unlock")) AddReward(serialized,QuestRewardType.ShopUnlock);
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("+ Stage Unlock")) AddReward(serialized,QuestRewardType.StageUnlock);
            if(GUILayout.Button("+ Warehouse")) AddReward(serialized,QuestRewardType.WarehouseSlots);
        }
    }

    private void DrawLinks()
    {
        GUILayout.Label("REQUIRES ALL OF",EditorStyles.miniBoldLabel);
        if(_selectedQuest.Prerequisites.Length==0) EditorGUILayout.HelpBox("Start quest: no prerequisite.",MessageType.None);
        foreach(var parent in _selectedQuest.Prerequisites)
        {
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button(parent!=null?parent.DisplayName:"Missing quest") && parent!=null) { SelectQuest(parent); return; }
                if(GUILayout.Button("X",GUILayout.Width(24)))
                { QuestMakerGraph.Unlink(parent,_selectedQuest); Invalidate(); GUIUtility.ExitGUI(); }
            }
        }
        _prerequisiteToAdd=(QuestDefinitionSO)EditorGUILayout.ObjectField("Prerequisite",_prerequisiteToAdd,typeof(QuestDefinitionSO),false);
        using(new EditorGUI.DisabledScope(_prerequisiteToAdd==null))
            if(GUILayout.Button("Add prerequisite"))
            { Link(_prerequisiteToAdd,_selectedQuest); _prerequisiteToAdd=null; }
        GUILayout.Space(14);
        GUILayout.Label("UNLOCKS NEXT",EditorStyles.miniBoldLabel);
        foreach(var quest in GetLineQuests())
            if(Array.IndexOf(quest.Prerequisites,_selectedQuest)>=0)
                using(new EditorGUILayout.HorizontalScope())
                {
                    if(GUILayout.Button(quest.DisplayName)) { SelectQuest(quest); return; }
                    if(GUILayout.Button("X",GUILayout.Width(24))) { QuestMakerGraph.Unlink(_selectedQuest,quest); Invalidate(); GUIUtility.ExitGUI(); }
                }
        _nextQuestToLink=(QuestDefinitionSO)EditorGUILayout.ObjectField("Next quest",_nextQuestToLink,typeof(QuestDefinitionSO),false);
        using(new EditorGUI.DisabledScope(_nextQuestToLink==null))
            if(GUILayout.Button("Link existing next quest"))
            { if(Link(_selectedQuest,_nextQuestToLink)) QuestMakerGraph.AddToLine(_questLine,_nextQuestToLink); _nextQuestToLink=null; }
        if(GUILayout.Button("+ Create next quest")) CreateQuest(_selectedQuest,Filter??_selectedQuest.Branch);
        EditorGUILayout.HelpBox("Multiple incoming arrows mean ALL predecessors, not a choice between them. Circular links are blocked.",MessageType.Info);
    }

    private bool Link(QuestDefinitionSO source,QuestDefinitionSO target)
    {
        bool changed=QuestMakerGraph.TryLink(source,target,out string message);
        ShowNotification(new GUIContent(message));
        if(changed) Invalidate();
        return changed;
    }

    private void ApplyArrayEdit(SerializedObject serialized)
    { serialized.ApplyModifiedProperties(); Invalidate(); GUIUtility.ExitGUI(); }
    private void AddGoal(SerializedObject serialized,QuestObjectiveType type)
    {
        // InsertArrayElement duplicates the last entry: initialize ALL fields so old targets do not leak.
        var values=serialized.FindProperty("_objectives"); int index=values.arraySize; values.arraySize++;
        var value=values.GetArrayElementAtIndex(index);
        value.FindPropertyRelative("_objectiveType").intValue=(int)type;
        value.FindPropertyRelative("_requiredCount").intValue=1;
        value.FindPropertyRelative("_targetConfig").objectReferenceValue=null;
        value.FindPropertyRelative("_targetItem").objectReferenceValue=null;
        value.FindPropertyRelative("_requiredWeapon").objectReferenceValue=null;
        value.FindPropertyRelative("_distanceRule").intValue=(int)QuestDistanceRule.Any;
        value.FindPropertyRelative("_distanceMeters").floatValue=10;
        Invalidate(); ApplyArrayEdit(serialized);
    }
    private void AddReward(SerializedObject serialized,QuestRewardType type)
    {
        var values=serialized.FindProperty("_rewards"); int index=values.arraySize; values.arraySize++;
        var value=values.GetArrayElementAtIndex(index);
        value.FindPropertyRelative("_rewardType").intValue=(int)type;
        value.FindPropertyRelative("_item").objectReferenceValue=null;
        value.FindPropertyRelative("_itemAmount").intValue=1;
        value.FindPropertyRelative("_stat").intValue=(int)PlayerStatId.MaxHealth;
        value.FindPropertyRelative("_statModifierType").intValue=(int)StatModifierType.PercentAdd;
        value.FindPropertyRelative("_statValue").floatValue=5;
        value.FindPropertyRelative("_shopConfig").objectReferenceValue=null;
        value.FindPropertyRelative("_shopUnlockNodeId").stringValue="";
        value.FindPropertyRelative("_stageToUnlock").objectReferenceValue=null;
        value.FindPropertyRelative("_warehouseSlotAmount").intValue=1;
        Invalidate(); ApplyArrayEdit(serialized);
    }

    private static void DrawShopUnlockNodePicker(SerializedProperty shopProperty,SerializedProperty idProperty)
    {
        var shop=shopProperty.objectReferenceValue as ShopConfigSO;
        if(shop==null) { EditorGUILayout.HelpBox("Choose the shop catalog to select its unlock node.",MessageType.Info); return; }
        var nodes=shop.UnlockNodes;
        var labels=new List<string> { string.IsNullOrEmpty(idProperty.stringValue)?"Choose unlock node...":"Missing node: "+idProperty.stringValue };
        var ids=new List<string>{idProperty.stringValue}; int selected=0;
        for(int i=0;i<nodes.Count;i++)
        {
            if(nodes[i]==null) continue;
            labels.Add(nodes[i].DisplayName+" ("+nodes[i].NodeId+")"); ids.Add(nodes[i].NodeId);
            if(nodes[i].NodeId==idProperty.stringValue) selected=ids.Count-1;
        }
        EditorGUI.BeginChangeCheck();
        int next=EditorGUILayout.Popup("Unlock node",selected,labels.ToArray());
        if(EditorGUI.EndChangeCheck() && next>0) idProperty.stringValue=ids[next];
        // Merely viewing a blank or stale selector never assigns its first node.
    }

    private List<QuestDefinitionSO> GetLineQuests() => QuestMakerGraph.Unique(_questLine!=null?_questLine.Quests:null);
    private void CreateLine()
    {
        string path=EditorUtility.SaveFilePanelInProject("New Quest Set","QuestLine","asset","A set groups related quest trees. It does not register NPC offerings.");
        if(string.IsNullOrWhiteSpace(path)) return;
        if(AssetDatabase.LoadMainAssetAtPath(path)!=null)
        { ShowNotification(new GUIContent("Choose a new filename; existing data was not replaced.")); return; }
        var line=CreateInstance<QuestLineSO>(); var serialized=new SerializedObject(line);
        serialized.FindProperty("_displayName").stringValue=Path.GetFileNameWithoutExtension(path);
        serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(line,path);
        AssetDatabase.SaveAssetIfDirty(line); RefreshCatalog(); SelectLine(line);
    }
    private void CreateQuest(QuestDefinitionSO parent,QuestBranch branch)
    {
        if(_questLine==null) return;
        string folder=Path.GetDirectoryName(AssetDatabase.GetAssetPath(_questLine))?.Replace('\\','/')??"Assets";
        string name=SanitizeAssetName(_newQuestName,"New Quest");
        string path=AssetDatabase.GenerateUniqueAssetPath(folder+"/"+name+".asset");
        var quest=CreateInstance<QuestDefinitionSO>();
        var serialized=new SerializedObject(quest);
        // Include a stable unique suffix: equal filenames in different folders must not collide in saves.
        serialized.FindProperty("_questId").stringValue=BuildQuestId(Path.GetFileNameWithoutExtension(path))+"_"+Guid.NewGuid().ToString("N").Substring(0,8);
        serialized.FindProperty("_displayName").stringValue=Path.GetFileNameWithoutExtension(path);
        serialized.FindProperty("_branch").intValue=(int)branch;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(quest,path); AssetDatabase.SaveAssetIfDirty(quest);
        QuestMakerGraph.AddToLine(_questLine,quest);
        if(parent!=null) QuestMakerGraph.TryLink(parent,quest,out _);
        _branchIndex=Array.IndexOf(QuestMakerGraph.Branches,branch)+1;
        _search=""; Invalidate(); SelectQuest(quest); FocusQuest(quest);
        _newQuestName="New Quest";
        GUIUtility.ExitGUI();
    }
    private void DuplicateQuest()
    {
        if(_selectedQuest==null || _questLine==null) return;
        var original=_selectedQuest;
        string path=AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(AssetDatabase.GetAssetPath(original))?.Replace('\\','/')+"/"+original.name+"_Copy.asset");
        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original),path)) return;
        var copy=AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>(path);
        var serialized=new SerializedObject(copy);
        serialized.FindProperty("_questId").stringValue=BuildQuestId(Path.GetFileNameWithoutExtension(path))+"_"+Guid.NewGuid().ToString("N").Substring(0,8);
        serialized.FindProperty("_displayName").stringValue=Path.GetFileNameWithoutExtension(path);
        serialized.FindProperty("_prerequisites").arraySize=0; serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(copy); QuestMakerGraph.AddToLine(_questLine,copy);
        _branchIndex = Array.IndexOf(QuestMakerGraph.Branches, copy.Branch) + 1;
        _search = "";
        Invalidate(); SelectQuest(copy); FocusQuest(copy); GUIUtility.ExitGUI();
    }
    private void SaveCurrentSet()
    {
        AssetDatabase.SaveAssetIfDirty(_questLine);
        // Save referenced prerequisite edits too, but not unrelated project assets.
        var graph=QuestMakerGraph.Build(GetLineQuests(),null,null);
        foreach(var quest in graph.Nodes) AssetDatabase.SaveAssetIfDirty(quest);
        if(_selectedQuest!=null) AssetDatabase.SaveAssetIfDirty(_selectedQuest);
        ShowNotification(new GUIContent("Quest set and quest edits saved."));
    }
    private void DrawChecks()
    {
        if(GUILayout.Button("Validate this set")) ValidateQuestLine();
        if(!_hasValidatedLine) { EditorGUILayout.HelpBox("Checks IDs, cycles, missing objectives and reward bindings.",MessageType.Info); return; }
        if(_lineValidationIssues.Count==0) EditorGUILayout.HelpBox("No basic authoring issues found.",MessageType.Info);
        foreach(var issue in _lineValidationIssues) EditorGUILayout.HelpBox(issue,MessageType.Warning);
    }
    private static void SelectAsset(UnityEngine.Object asset) { Selection.activeObject=asset; EditorGUIUtility.PingObject(asset); }

    private static void DrawRewardFields(SerializedProperty reward)
    {
        SerializedProperty rewardType = reward.FindPropertyRelative("_rewardType");
        EditorGUILayout.PropertyField(rewardType, new GUIContent("Reward Type"));

        QuestRewardType type = (QuestRewardType)rewardType.enumValueIndex;
        if (type == QuestRewardType.Item)
        {
            EditorGUILayout.PropertyField(reward.FindPropertyRelative("_item"), new GUIContent("Item"));
            EditorGUILayout.PropertyField(reward.FindPropertyRelative("_itemAmount"), new GUIContent("Amount"));
            return;
        }

        if (type == QuestRewardType.ShopUnlock)
        {
            SerializedProperty shopConfig = reward.FindPropertyRelative("_shopConfig");
            SerializedProperty nodeId = reward.FindPropertyRelative("_shopUnlockNodeId");
            EditorGUILayout.PropertyField(shopConfig, new GUIContent("Shop Config"));
            DrawShopUnlockNodePicker(shopConfig, nodeId);
            return;
        }

        if (type == QuestRewardType.StageUnlock)
        {
            EditorGUILayout.PropertyField(reward.FindPropertyRelative("_stageToUnlock"), new GUIContent("Stage To Unlock"));
            return;
        }

        if (type == QuestRewardType.WarehouseSlots)
        {
            EditorGUILayout.PropertyField(
                reward.FindPropertyRelative("_warehouseSlotAmount"),
                new GUIContent("Slots To Add"));
            return;
        }

        EditorGUILayout.PropertyField(reward.FindPropertyRelative("_stat"), new GUIContent("Stat"));
        EditorGUILayout.PropertyField(reward.FindPropertyRelative("_statModifierType"), new GUIContent("Modifier"));
        EditorGUILayout.PropertyField(reward.FindPropertyRelative("_statValue"), new GUIContent("Value"));

        PlayerStatId stat = (PlayerStatId)reward.FindPropertyRelative("_stat").enumValueIndex;
        if (stat == PlayerStatId.ArmorClass)
        {
            EditorGUILayout.HelpBox(
                "Armor Class is a direct value. Use Flat Add and +0.5 for one permanent half-class upgrade.",
                MessageType.Info);
        }
        else if (stat == PlayerStatId.MaxHealth ||
                 stat == PlayerStatId.AttackDamage ||
                 stat == PlayerStatId.ShotsPerSecond ||
                 stat == PlayerStatId.ReloadDurationSeconds ||
                 stat == PlayerStatId.MoveSpeed)
        {
            EditorGUILayout.HelpBox(
                "Progression stats use Percent Add. Reload Speed is authored as a negative Reload Duration value: -5 means +5% reload speed.",
                MessageType.Info);
        }
    }

    private static void DrawObjectiveFields(SerializedProperty objective)
    {
        SerializedProperty type = objective.FindPropertyRelative("_objectiveType");
        SerializedProperty requiredCount = objective.FindPropertyRelative("_requiredCount");
        SerializedProperty targetConfig = objective.FindPropertyRelative("_targetConfig");
        SerializedProperty targetItem = objective.FindPropertyRelative("_targetItem");
        SerializedProperty requiredWeapon = objective.FindPropertyRelative("_requiredWeapon");
        SerializedProperty distanceRule = objective.FindPropertyRelative("_distanceRule");
        SerializedProperty distanceMeters = objective.FindPropertyRelative("_distanceMeters");

        EditorGUILayout.PropertyField(type, new GUIContent("Objective Type"));
        EditorGUILayout.PropertyField(requiredCount, new GUIContent("Required Count"));

        QuestObjectiveType objectiveType = (QuestObjectiveType)type.enumValueIndex;
        if (objectiveType == QuestObjectiveType.CollectItem)
        {
            EditorGUILayout.PropertyField(targetItem, new GUIContent("Required Item"));
            EditorGUILayout.HelpBox(
                "Collect Item records the highest amount held in the player's main inventory after this quest is accepted. It does not consume the item.",
                MessageType.None);
            return;
        }

        EditorGUILayout.PropertyField(targetConfig, new GUIContent("Target Enemy", "Leave empty to count any enemy."));
        EditorGUILayout.PropertyField(requiredWeapon, new GUIContent("Weapon (optional)", "Leave empty to count any weapon."));
        EditorGUILayout.PropertyField(distanceRule, new GUIContent("Distance Rule"));
        if ((QuestDistanceRule)distanceRule.enumValueIndex != QuestDistanceRule.Any)
            EditorGUILayout.PropertyField(distanceMeters, new GUIContent("Distance (m)"));
    }

    private static string BuildQuestRewardPreview(QuestDefinitionSO quest)
    {
        if (quest == null)
            return "None";

        QuestRewardDefinition[] rewards = quest.Rewards;
        for (int index = 0; index < rewards.Length; index++)
        {
            if (rewards[index] != null)
                return rewards[index].BuildSummary();
        }

        return string.IsNullOrWhiteSpace(quest.PersistentRewardSummary)
            ? "None"
            : quest.PersistentRewardSummary;
    }

    private static string SanitizeAssetName(string value, string fallback)
    {
        string result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidCharacters.Length; i++)
            result = result.Replace(invalidCharacters[i], '_');

        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    private static string BuildQuestId(string assetName)
    {
        string normalized = assetName.Trim().ToLowerInvariant();
        System.Text.StringBuilder builder = new(normalized.Length);

        for (int i = 0; i < normalized.Length; i++)
        {
            char character = normalized[i];
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        string result = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "quest" : result;
    }

    private void ValidateQuestLine()
    {
        _lineValidationIssues.Clear();
        _hasValidatedLine = true;

        var entries = _questLine != null ? _questLine.Quests : Array.Empty<QuestDefinitionSO>();
        var uniqueEntries = new HashSet<QuestDefinitionSO>();
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null) _lineValidationIssues.Add($"Quest entry {i + 1} is missing.");
            else if (!uniqueEntries.Add(entries[i])) _lineValidationIssues.Add($"{entries[i].DisplayName}: duplicated set entry.");
        }
        // Requirements outside this set also affect whether its quests can actually unlock.
        List<QuestDefinitionSO> quests = QuestMakerGraph.Build(entries, null, null).Nodes;
        if (quests.Count == 0)
        {
            _lineValidationIssues.Add("The Quest Set contains no quests.");
            return;
        }

        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        for (int questIndex = 0; questIndex < quests.Count; questIndex++)
        {
            QuestDefinitionSO quest = quests[questIndex];
            if (quest == null)
            {
                _lineValidationIssues.Add($"Quest entry {questIndex + 1} is missing.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(quest.QuestId) || !ids.Add(quest.QuestId))
                _lineValidationIssues.Add($"{quest.name}: Quest ID is missing or duplicated.");

            var parents = new HashSet<QuestDefinitionSO>();
            foreach (var parent in quest.Prerequisites)
            {
                if (parent == null) _lineValidationIssues.Add($"{quest.DisplayName}: missing prerequisite reference.");
                else if (!parents.Add(parent)) _lineValidationIssues.Add($"{quest.DisplayName}: duplicated prerequisite reference.");
            }
            if (!Enum.IsDefined(typeof(QuestBranch), quest.Branch))
                _lineValidationIssues.Add($"{quest.DisplayName}: unrecognized branch value.");

            QuestObjectiveDefinition[] objectives = quest.Objectives;
            if (objectives.Length == 0)
                _lineValidationIssues.Add($"{quest.DisplayName}: no objective is configured.");

            for (int objectiveIndex = 0; objectiveIndex < objectives.Length; objectiveIndex++)
            {
                QuestObjectiveDefinition objective = objectives[objectiveIndex];
                if (objective == null)
                {
                    _lineValidationIssues.Add($"{quest.DisplayName}: objective {objectiveIndex + 1} is missing.");
                    continue;
                }

                if (objective.ObjectiveType == QuestObjectiveType.CollectItem && objective.TargetItem == null)
                    _lineValidationIssues.Add($"{quest.DisplayName}: Collect objective {objectiveIndex + 1} has no item.");
            }

            if (quest.Rewards.Length == 0 && string.IsNullOrWhiteSpace(quest.PersistentRewardSummary))
                _lineValidationIssues.Add($"{quest.DisplayName}: no reward is configured.");

            QuestRewardDefinition[] rewards = quest.Rewards;
            for (int rewardIndex = 0; rewardIndex < rewards.Length; rewardIndex++)
            {
                QuestRewardDefinition reward = rewards[rewardIndex];
                if (reward == null)
                {
                    _lineValidationIssues.Add($"{quest.DisplayName}: reward {rewardIndex + 1} is missing.");
                    continue;
                }

                if (reward.RewardType == QuestRewardType.Item && reward.Item == null)
                {
                    _lineValidationIssues.Add($"{quest.DisplayName}: item reward {rewardIndex + 1} has no ItemSO.");
                    continue;
                }

                if (reward.RewardType == QuestRewardType.ShopUnlock)
                {
                    if (reward.ShopConfig == null || string.IsNullOrWhiteSpace(reward.ShopUnlockNodeId))
                    {
                        _lineValidationIssues.Add($"{quest.DisplayName}: shop unlock reward {rewardIndex + 1} is incomplete.");
                    }
                    else if (!reward.ShopConfig.TryGetNode(reward.ShopUnlockNodeId, out _))
                    {
                        _lineValidationIssues.Add(
                            $"{quest.DisplayName}: shop unlock reward {rewardIndex + 1} references a missing node " +
                            $"'{reward.ShopUnlockNodeId}'.");
                    }

                    continue;
                }

                if (reward.RewardType == QuestRewardType.StageUnlock && reward.StageToUnlock == null)
                {
                    _lineValidationIssues.Add($"{quest.DisplayName}: stage unlock reward {rewardIndex + 1} has no StageDefinitionSO.");
                }

                if (reward.RewardType == QuestRewardType.WarehouseSlots && reward.WarehouseSlotAmount <= 0)
                {
                    _lineValidationIssues.Add($"{quest.DisplayName}: warehouse slot reward {rewardIndex + 1} must grant at least one slot.");
                }
            }

            if (HasPrerequisiteCycle(quest, new HashSet<QuestDefinitionSO>(), new HashSet<QuestDefinitionSO>()))
                _lineValidationIssues.Add($"{quest.DisplayName}: a prerequisite cycle exists.");
        }

        ShowNotification(new GUIContent(
            _lineValidationIssues.Count == 0
                ? "Quest Set validation passed."
                : $"Found {_lineValidationIssues.Count} Quest Set issue(s)."));
    }

    private static bool HasPrerequisiteCycle(
        QuestDefinitionSO quest,
        HashSet<QuestDefinitionSO> visited,
        HashSet<QuestDefinitionSO> path)
    {
        if (quest == null)
            return false;

        if (!path.Add(quest))
            return true;

        if (visited.Add(quest))
        {
            QuestDefinitionSO[] prerequisites = quest.Prerequisites;
            for (int i = 0; i < prerequisites.Length; i++)
            {
                if (HasPrerequisiteCycle(prerequisites[i], visited, path))
                    return true;
            }
        }

        path.Remove(quest);
        return false;
    }
}
