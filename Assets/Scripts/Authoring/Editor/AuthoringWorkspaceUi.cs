/*
 * Role hierarchy: Authoring > Editor > Shared UI
 * Primary responsibility: Draws the consistent workspace header used by permanent-content tools.
 * Execution boundary: Unity Editor only (Assembly-CSharp-Editor)
 */

using System;
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
