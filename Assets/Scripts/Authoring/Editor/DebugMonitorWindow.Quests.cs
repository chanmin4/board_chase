/*
 * Role hierarchy: Authoring > Editor > Debug Monitor > Quests
 * Primary responsibility: Inspects authored quests and lets developers accept or complete them in Play Mode.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed partial class DebugMonitorWindow
{
    private Vector2 _questOperationsScroll;

    // Purpose: Draws the quest operations authoring UI.
    private void DrawQuestOperations()
    {
        EditorGUILayout.LabelField("Quest Runtime", EditorStyles.boldLabel);

        QuestRuntime runtime = QuestRuntime.Instance;
        if (runtime == null)
        {
            EditorGUILayout.HelpBox(
                "QuestRuntime is created when a Home quest NPC is interacted with. You can also create it here for testing.",
                MessageType.Info);

            if (GUILayout.Button("Create Quest Runtime"))
            {
                QuestRuntime.EnsureRuntime();
                Repaint();
            }

            return;
        }

        HomeQuestNpc selectedGiver = runtime.SelectedQuestGiver;
        EditorGUILayout.LabelField("Selected Quest Giver", selectedGiver != null ? selectedGiver.DisplayName : "None");

        IReadOnlyList<QuestRuntimeState> states = runtime.States;
        EditorGUILayout.LabelField("Accepted / Completed", $"{CountAccepted(states)} / {CountCompleted(states)}");
        EditorGUILayout.Space(6f);

        string[] questGuids = AssetDatabase.FindAssets("t:QuestDefinitionSO");
        _questOperationsScroll = EditorGUILayout.BeginScrollView(_questOperationsScroll, GUILayout.MinHeight(180f));
        for (int i = 0; i < questGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(questGuids[i]);
            QuestDefinitionSO definition = AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>(path);
            if (definition == null)
                continue;

            QuestRuntimeState state = FindQuestState(states, definition);
            DrawQuestDebugRow(runtime, definition, state);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(
            "Debug Complete accepts the quest if needed, fills every objective, and runs the same completion message path. " +
            "Quest rewards remain authored data until the persistent reward inventory is connected.",
            MessageType.None);
    }

    // Purpose: Draws the quest debug row authoring UI.
    private static void DrawQuestDebugRow(QuestRuntime runtime, QuestDefinitionSO definition, QuestRuntimeState state)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(definition.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Id", definition.QuestId);

            QuestObjectiveDefinition[] objectives = definition.Objectives;
            for (int objectiveIndex = 0; objectiveIndex < objectives.Length; objectiveIndex++)
            {
                QuestObjectiveDefinition objective = objectives[objectiveIndex];
                int current = state != null && objectiveIndex < state.ObjectiveProgress.Count
                    ? state.ObjectiveProgress[objectiveIndex]
                    : 0;
                EditorGUILayout.LabelField($"- {objective.BuildSummary()}", $"{current}/{objective.RequiredCount}");
            }

            string stateLabel = state == null ? "Not accepted" : state.IsCompleted ? "Completed" : "Active";
            EditorGUILayout.LabelField("State", stateLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(state != null || !runtime.CanAccept(definition)))
                {
                    if (GUILayout.Button("Accept"))
                        runtime.TryAccept(definition);
                }

                if (GUILayout.Button("Debug Complete"))
                    runtime.DebugComplete(definition);
            }
        }
    }

    // Purpose: Finds the requested quest state.
    private static QuestRuntimeState FindQuestState(IReadOnlyList<QuestRuntimeState> states, QuestDefinitionSO definition)
    {
        for (int i = 0; i < states.Count; i++)
        {
            QuestRuntimeState state = states[i];
            if (state != null && state.Definition == definition)
                return state;
        }

        return null;
    }

    // Purpose: Counts the available accepted.
    private static int CountAccepted(IReadOnlyList<QuestRuntimeState> states)
    {
        int count = 0;
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i] != null && states[i].IsAccepted)
                count++;
        }

        return count;
    }

    // Purpose: Counts the available completed.
    private static int CountCompleted(IReadOnlyList<QuestRuntimeState> states)
    {
        int count = 0;
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i] != null && states[i].IsCompleted)
                count++;
        }

        return count;
    }
}
