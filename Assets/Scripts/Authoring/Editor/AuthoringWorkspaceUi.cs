/*
 * Role hierarchy: Authoring > Editor > Shared UI
 * Primary responsibility: Draws the consistent workspace header used by permanent-content tools.
 * Execution boundary: Unity Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AuthoringWorkspaceUi
{
    // Purpose: Draws the header authoring UI.
    public static void DrawHeader(
        string title,
        string owner,
        string purpose,
        params string[] workflowSteps)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(owner, EditorStyles.miniBoldLabel, GUILayout.Width(145f));
            }

            EditorGUILayout.LabelField(purpose, EditorStyles.wordWrappedMiniLabel);

            if (workflowSteps == null || workflowSteps.Length == 0)
                return;

            if (EditorGUIUtility.currentViewWidth < 650f)
            {
                EditorGUILayout.LabelField(
                    "WORKFLOW  " + string.Join("  ->  ", workflowSteps),
                    EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("WORKFLOW", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
                    for (int i = 0; i < workflowSteps.Length; i++)
                    {
                        string step = workflowSteps[i];
                        if (string.IsNullOrWhiteSpace(step))
                            continue;

                        GUILayout.Label(step, EditorStyles.miniButton, GUILayout.Height(18f));
                    }
                }
            }
        }

        EditorGUILayout.Space(4f);
    }
}

/// <summary>View-only category trees shared by the Maker browsers; never changes authored assets.</summary>
internal static class MakerCategoryTreeUi
{
    private static EditorWindow _drawingWindow;
    private static Vector2 _screenOrigin;
    // Optional read-only IMGUI observation for interaction regression tests.
    internal static Action<EditorWindow, string, Rect> ControlObserved;

    internal static string PreferenceKey(string scope, string key) =>
        "BoardChase.MakerTree.v1." + Application.dataPath + "." + scope + "." + key;

    internal static bool IsExpanded(string scope, string key) => EditorPrefs.GetBool(PreferenceKey(scope, key), false);
    internal static void SetExpanded(string scope, string key, bool expanded) => EditorPrefs.SetBool(PreferenceKey(scope, key), expanded);
    internal static void SetAll(string scope, IEnumerable<string> keys, bool expanded)
    {
        foreach (string key in keys) SetExpanded(scope, key, expanded);
    }

    internal static void BeginWindow(EditorWindow window)
    {
        _drawingWindow = window;
        _screenOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
        if (ControlObserved != null && Event.current.type == EventType.Repaint)
            ControlObserved(window, "Begin", Rect.zero);
    }

    internal static void Observe(string id, Rect rect)
    {
        if (ControlObserved == null || Event.current.type != EventType.Repaint) return;
        rect.position = GUIUtility.GUIToScreenPoint(rect.position) - _screenOrigin;
        ControlObserved(_drawingWindow, id, rect);
    }

    internal static void DrawToolbar(string scope, IEnumerable<string> keys)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent("Expand All", "Show all category children. Does not change the selected filter."), EditorStyles.miniButtonLeft))
                SetAll(scope, keys, true);
            Observe(scope + "/ExpandAll", GUILayoutUtility.GetLastRect());
            if (GUILayout.Button(new GUIContent("Collapse All", "Hide category children. The current selection and data are kept."), EditorStyles.miniButtonRight))
                SetAll(scope, keys, false);
            Observe(scope + "/CollapseAll", GUILayoutUtility.GetLastRect());
        }
    }

    internal static void SplitRow(Rect row, bool hasChildren, float indent, out Rect toggle, out Rect select)
    {
        row.xMin += Mathf.Clamp(indent, 0, Mathf.Max(0, row.width - 40));
        toggle = new Rect(row.x, row.y, hasChildren ? Mathf.Min(18, row.width) : 0, row.height);
        select = row;
        select.xMin = toggle.xMax;
    }

    // The disclosure arrow has its own hit target. Selecting a category never toggles it, and vice versa.
    internal static bool DrawRow(Rect rect, string scope, string key, GUIContent label,
        bool selected, bool hasChildren, float indent = 0, Color? tint = null)
    {
        SplitRow(rect, hasChildren, indent, out Rect toggle, out Rect select);
        if (hasChildren)
        {
            bool expanded = IsExpanded(scope, key);
            bool next = EditorGUI.Foldout(toggle, expanded, GUIContent.none, false);
            if (next != expanded) SetExpanded(scope, key, next);
            Observe(scope + "/" + key + "/toggle", toggle);
        }
        Color original = GUI.backgroundColor;
        if (selected) GUI.backgroundColor = tint ?? new Color(.23f, .65f, .86f, .8f);
        var style = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
        bool clicked = GUI.Button(select, label, style);
        GUI.backgroundColor = original;
        Observe(scope + "/" + key + "/select", select);
        return clicked;
    }
}
