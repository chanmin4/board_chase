using System;
using UnityEditor;
using UnityEngine;

public sealed partial class QuestDesignTreeWindow
{
    [SerializeField] private float _browserWidth = 280, _workbenchWidth = 380;
    private Rect _browserSplitter, _workbenchSplitter, _flowPaneRect;
    private float _splitStartX, _splitStartWidth;
    private QuestDefinitionSO _edgeSource, _edgeTarget, _hoverSource, _hoverTarget;
    private QuestDefinitionSO _movingSource, _movingTarget;
    private bool _moveSourceEndpoint, _graphFocused, _panDragged;
    private Vector2 _panMouseDown, _graphMouse;
    private int _panButton;
    private bool HasPendingConnection => _linkSource != null || (_movingSource != null && _movingTarget != null);
    private static string PanelPrefKey => "BoardChase.QuestMaker." + Application.dataPath;

    private void RestorePanelWidths()
    {
        _browserWidth = EditorPrefs.GetFloat(PanelPrefKey + ".Left", _browserWidth);
        _workbenchWidth = EditorPrefs.GetFloat(PanelPrefKey + ".Right", _workbenchWidth);
    }
    private void SavePanelWidths()
    {
        EditorPrefs.SetFloat(PanelPrefKey + ".Left", _browserWidth);
        EditorPrefs.SetFloat(PanelPrefKey + ".Right", _workbenchWidth);
    }

    public static void ConstrainPanelWidths(float totalWidth, ref float left, ref float right)
    {
        float budget = Mathf.Max(496, totalWidth - 12 - 300);
        left = Mathf.Clamp(left, 196, budget - 300);
        right = Mathf.Clamp(right, 300, budget - left);
    }

    private void DrawResizablePanels(float height)
    {
        Rect body = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
        // GUILayout returns a dummy rect during Layout; never clamp persisted widths to it.
        body.width = position.width;
        body.height = height;
        ConstrainPanelWidths(body.width, ref _browserWidth, ref _workbenchWidth);
        var left = new Rect(body.x, body.y, _browserWidth, body.height);
        var right = new Rect(body.xMax - _workbenchWidth, body.y, _workbenchWidth, body.height);
        _browserSplitter = new Rect(left.xMax, body.y, 6, body.height);
        _workbenchSplitter = new Rect(right.x - 6, body.y, 6, body.height);
        _flowPaneRect = new Rect(_browserSplitter.xMax, body.y, _workbenchSplitter.x - _browserSplitter.xMax, body.height);
        if (Event.current.type == EventType.MouseDown && !_flowPaneRect.Contains(Event.current.mousePosition)) _graphFocused = false;
        DrawSplitter(_browserSplitter, true, body.width);
        DrawSplitter(_workbenchSplitter, false, body.width);
        DrawPane(left, () => DrawBrowser(left.width, height));
        DrawPane(_flowPaneRect, () => DrawFlow(_flowPaneRect.width, height));
        DrawPane(right, () => DrawInspector(right.width, height));
    }

    private static void DrawPane(Rect rect, Action draw)
    {
        // Explicit clipping owns the window->pane transform, independent of GUILayout's
        // cached area rect. Both the drawn curve and its mouse hit test use this origin.
        GUI.BeginGroup(rect);
        GUILayout.BeginArea(new Rect(0,0,rect.width,rect.height));
        try { draw(); }
        finally { GUILayout.EndArea(); GUI.EndGroup(); }
    }

    private void DrawSplitter(Rect rect, bool left, float totalWidth)
    {
        int id = GUIUtility.GetControlID(left ? 17961 : 17962, FocusType.Passive, rect);
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal, id);
        bool active = GUIUtility.hotControl == id;
        EditorGUI.DrawRect(rect, active || rect.Contains(Event.current.mousePosition) ? new Color(.18f,.65f,.76f) : new Color(.14f,.17f,.19f));
        GUI.Label(rect, new GUIContent("", "Drag to resize. Double-click to reset this panel."));
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        {
            if (e.clickCount == 2)
            {
                if (left) _browserWidth = 280; else _workbenchWidth = 380;
                ConstrainPanelWidths(totalWidth, ref _browserWidth, ref _workbenchWidth);
                SavePanelWidths(); e.Use(); Repaint(); GUIUtility.ExitGUI();
            }
            _splitStartX = e.mousePosition.x;
            _splitStartWidth = left ? _browserWidth : _workbenchWidth;
            GUIUtility.hotControl = id; GUI.FocusControl(null); e.Use();
        }
        else if (e.type == EventType.MouseDrag && active)
        {
            float value = _splitStartWidth + (e.mousePosition.x - _splitStartX) * (left ? 1 : -1);
            // Dragging one divider never steals width from the opposite panel.
            if (left) _browserWidth = Mathf.Clamp(value,196,totalWidth-12-300-_workbenchWidth);
            else _workbenchWidth = Mathf.Clamp(value,300,totalWidth-12-300-_browserWidth);
            e.Use(); Repaint(); GUIUtility.ExitGUI();
        }
        else if (e.type == EventType.MouseUp && active)
        { GUIUtility.hotControl = 0; SavePanelWidths(); e.Use(); Repaint(); }
    }

    private void CancelConnection()
    { _linkSource = null; _movingSource = null; _movingTarget = null; Repaint(); }
    private void BeginLink(QuestDefinitionSO source)
    { CancelConnection(); _linkSource = source; SelectQuest(source); }
    private void BeginMoveConnection(QuestDefinitionSO source, QuestDefinitionSO target, bool moveSource)
    {
        CancelConnection(); _movingSource = source; _movingTarget = target; _moveSourceEndpoint = moveSource;
        _edgeSource = source; _edgeTarget = target; Repaint();
    }
    private string ConnectionHint => _movingSource != null
        ? "Move " + _movingSource.DisplayName + " -> " + _movingTarget.DisplayName + ": click the NEW " + (_moveSourceEndpoint ? "PREREQUISITE" : "NEXT quest") + ". Esc cancels."
        : "Link from " + _linkSource.DisplayName + ": click the NEXT quest. Esc cancels.";
    private void CompleteConnection(QuestDefinitionSO quest)
    {
        bool changed;
        if (_movingSource != null)
        {
            var source = _moveSourceEndpoint ? quest : _movingSource;
            var target = _moveSourceEndpoint ? _movingTarget : quest;
            changed = QuestMakerGraph.TryMoveLink(_movingSource,_movingTarget,source,target,out string message);
            ShowNotification(new GUIContent(message));
            if (changed) { _edgeSource = source; _edgeTarget = target; Invalidate(); }
        }
        else changed = Link(_linkSource,quest);
        if (changed) CancelConnection();
    }
    private void DeleteConnection(QuestDefinitionSO source, QuestDefinitionSO target)
    {
        if (QuestMakerGraph.Unlink(source,target))
        { CancelConnection(); _edgeSource = null; _edgeTarget = null; Invalidate(); }
    }

    private QuestDefinitionSO FindNodeAt(Vector2 point)
    {
        if (_layout == null) return null;
        for (int i = _layout.Nodes.Count - 1; i >= 0; i--)
        {
            var quest = _layout.Nodes[i];
            if (ScreenRect(_layout.Rects[quest]).Contains(point)) return quest;
        }
        return null;
    }
    private bool FindConnectionAt(Vector2 point, out QuestDefinitionSO source, out QuestDefinitionSO target)
    {
        source = null; target = null;
        if (_layout == null || FindNodeAt(point) != null) return false;
        float nearest = 9;
        foreach (var quest in _layout.Nodes)
            foreach (var parent in quest.Prerequisites)
            {
                if (parent == null || !_layout.Rects.TryGetValue(parent,out var from)) continue;
                float distance = QuestMakerGraph.DistanceToConnection(point,ScreenRect(from),ScreenRect(_layout.Rects[quest]));
                if (distance > nearest) continue;
                nearest = distance; source = parent; target = quest;
            }
        return source != null;
    }

    private GenericMenu BuildGraphContextMenu(Vector2 point)
    {
        var menu = new GenericMenu();
        var quest = FindNodeAt(point);
        if (quest != null)
        {
            menu.AddDisabledItem(new GUIContent(quest.DisplayName.Replace('/','-')));
            menu.AddItem(new GUIContent("Focus quest"),false,()=> { SelectQuest(quest); FocusQuest(quest); });
            menu.AddItem(new GUIContent("Edit prerequisites and links"),false,()=> { SelectQuest(quest); _detailTab=3; });
            menu.AddItem(new GUIContent("Connect to another quest..."),false,()=>BeginLink(quest));
            foreach (var branch in QuestMakerGraph.Branches)
            {
                var captured = branch;
                menu.AddItem(new GUIContent("Create next/"+QuestMakerGraph.BranchLabel(branch)),false,()=>CreateQuest(quest,captured));
            }
            return menu;
        }
        if (FindConnectionAt(point,out var source,out var target))
        {
            _edgeSource=source; _edgeTarget=target;
            menu.AddDisabledItem(new GUIContent((source.DisplayName+" -> "+target.DisplayName).Replace('/','-')));
            menu.AddItem(new GUIContent("Change prerequisite (start)..."),false,()=>BeginMoveConnection(source,target,true));
            menu.AddItem(new GUIContent("Change next quest (end)..."),false,()=>BeginMoveConnection(source,target,false));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete connection"),false,()=>DeleteConnection(source,target));
            return menu;
        }
        menu.AddItem(new GUIContent("Fit entire tree"),false,()=> { _frameRequested=true; _fitAllRequested=true; Repaint(); });
        menu.AddItem(new GUIContent("Reset zoom (1:1)"),false,()=> { ZoomAt(_lastViewport.center,1); Repaint(); });
        if (_selectedQuest != null) menu.AddItem(new GUIContent("Focus selected quest"),false,()=>FocusQuest(_selectedQuest));
        if (_questLine != null)
            foreach (var branch in QuestMakerGraph.Branches)
            {
                var captured=branch;
                menu.AddItem(new GUIContent("Create root/"+QuestMakerGraph.BranchLabel(branch)),false,()=>CreateQuest(null,captured));
            }
        return menu;
    }

    private void ZoomAt(Vector2 pointInPane, float zoom)
    {
        Vector2 anchor=(pointInPane-_lastViewport.position+_pan)/_zoom;
        _zoom=Mathf.Clamp(zoom,.025f,1.6f);
        _pan=anchor*_zoom-(pointInPane-_lastViewport.position);
        _frameRequested=false;
    }

    private void HandleGraphInput(Rect area)
    {
        var e=Event.current;
        Vector2 point=e.mousePosition-area.position;
        if (e.type==EventType.MouseMove)
        {
            _graphMouse=point;
            _hoverSource=null; _hoverTarget=null;
            if (area.Contains(e.mousePosition)) FindConnectionAt(point,out _hoverSource,out _hoverTarget);
            Repaint();
        }
        if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Escape && HasPendingConnection)
        { CancelConnection(); e.Use(); }
        if(e.type==EventType.KeyDown && _graphFocused && !EditorGUIUtility.editingTextField &&
            (e.keyCode==KeyCode.Delete || e.keyCode==KeyCode.Backspace) && _edgeSource!=null && _edgeTarget!=null)
        { DeleteConnection(_edgeSource,_edgeTarget); e.Use(); GUIUtility.ExitGUI(); }
        if(e.type==EventType.KeyDown && _graphFocused && !EditorGUIUtility.editingTextField && e.keyCode==KeyCode.F && _selectedQuest!=null)
        { FocusQuest(_selectedQuest); e.Use(); }
        int id=GUIUtility.GetControlID("QuestGraphPan".GetHashCode(),FocusType.Passive,area);
        if(e.type==EventType.ScrollWheel && area.Contains(e.mousePosition))
        { ZoomAt(e.mousePosition,_zoom*Mathf.Exp(-e.delta.y*.06f)); e.Use(); Repaint(); }
        if(e.type==EventType.MouseDown && area.Contains(e.mousePosition))
        {
            _graphFocused=true;
            if(e.button==1 || e.button==2)
            {
                GUIUtility.hotControl=id; GUI.FocusControl(null);
                _panButton=e.button; _panMouseDown=e.mousePosition; _panDragged=false; e.Use();
            }
            else if(e.button==0)
            {
                var quest=FindNodeAt(point);
                if(quest!=null && e.clickCount==2 && !HasPendingConnection)
                { SelectQuest(quest); FocusQuest(quest); e.Use(); }
                else if(quest==null)
                {
                    FindConnectionAt(point,out _edgeSource,out _edgeTarget);
                    if (_edgeTarget != null) { SelectQuest(_edgeTarget); _detailTab = 3; }
                    GUI.FocusControl(null); e.Use(); Repaint();
                }
                else { _edgeSource=null; _edgeTarget=null; }
            }
        }
        if(e.type==EventType.MouseDrag && GUIUtility.hotControl==id)
        {
            if (!_panDragged && Vector2.Distance(e.mousePosition,_panMouseDown)>4)
            { _panDragged=true; _pan-=e.mousePosition-_panMouseDown; }
            else if (_panDragged) _pan-=e.delta;
            _frameRequested=false; e.Use(); Repaint();
        }
        if(e.type==EventType.MouseUp && GUIUtility.hotControl==id)
        {
            GUIUtility.hotControl=0;
            if (_panButton==1 && !_panDragged && area.Contains(e.mousePosition)) BuildGraphContextMenu(point).ShowAsContext();
            e.Use(); Repaint();
        }
        // Right click is handled on release above; right drag retains the existing pan gesture.
        if(e.type==EventType.ContextClick && area.Contains(e.mousePosition)) e.Use();
    }

    private void DrawPendingConnection()
    {
        if (!HasPendingConnection || Event.current.type!=EventType.Repaint) return;
        QuestDefinitionSO anchor=_movingSource!=null ? (_moveSourceEndpoint?_movingTarget:_movingSource) : _linkSource;
        if (anchor==null || !_layout.Rects.TryGetValue(anchor,out var rect)) return;
        Rect screen=ScreenRect(rect);
        bool incoming=_movingSource!=null && _moveSourceEndpoint;
        Vector3 start=incoming?_graphMouse:new Vector2(screen.xMax,screen.center.y);
        Vector3 end=incoming?new Vector2(screen.xMin,screen.center.y):_graphMouse;
        Handles.DrawBezier(start,end,start+Vector3.right*30,end-Vector3.right*30,new Color(1,.75f,.25f),null,3);
    }
}
