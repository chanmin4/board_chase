/*
 * Role hierarchy: Authoring > Editor > Debug Monitor > Status Effects
 * Primary responsibility: Provides explicit runtime controls for applying, refreshing, and clearing entity status effects during Play Mode.
 * Execution boundary: Editor (Assembly-CSharp-Editor)
 */
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed partial class DebugMonitorWindow
{
    private StatusEffectDefinitionSO _debugStatusEffectDefinition;
    private float _debugStatusEffectDurationOverride;
    private readonly List<StatusEffectDefinitionSO> _debugStatusEffectDefinitions = new();
    private bool _debugStatusEffectDefinitionsLoaded;

    // Purpose: Draws the status effect debug controls authoring UI.
    private void DrawStatusEffectDebugControls(CharacterRow row)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Runtime Status Effects", EditorStyles.boldLabel);

        EntityStatusEffectRuntime runtime = row.statusEffects;
        if (runtime == null)
        {
            EditorGUILayout.HelpBox("Selected entity has no EntityStatusEffectRuntime.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        DrawStatusEffectDefinitionPicker();

        _debugStatusEffectDurationOverride = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent("Duration Override", "0 uses the duration authored in the selected StatusEffectDefinitionSO."),
            _debugStatusEffectDurationOverride));

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(_debugStatusEffectDefinition == null || !Application.isPlaying))
        {
            if (GUILayout.Button("Apply / Refresh"))
            {
                float duration = _debugStatusEffectDurationOverride > 0f
                    ? _debugStatusEffectDurationOverride
                    : -1f;
                runtime.Apply(_debugStatusEffectDefinition, duration);
            }

            if (GUILayout.Button("Remove Selected"))
                runtime.Remove(_debugStatusEffectDefinition);
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying || runtime.ActiveEffects.Count == 0))
        {
            if (GUILayout.Button("Clear All"))
                runtime.RemoveAll();
        }

        EditorGUILayout.EndHorizontal();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to modify runtime effects.", MessageType.None);
        }
        else if (runtime.ActiveEffects.Count == 0)
        {
            EditorGUILayout.LabelField("Active Effects", "None");
        }
        else
        {
            EditorGUILayout.LabelField("Active Effects", runtime.ActiveEffects.Count.ToString());
            for (int index = 0; index < runtime.ActiveEffects.Count; index++)
            {
                EntityStatusEffectRuntime.ActiveStatusEffect activeEffect = runtime.ActiveEffects[index];
                StatusEffectDefinitionSO definition = activeEffect.Definition;
                string id = definition != null ? definition.Id : "Missing Definition";
                EditorGUILayout.LabelField($"• {id}", $"{activeEffect.RemainingSeconds:0.0}s");
            }
        }

        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws the status effect definition picker authoring UI.
    private void DrawStatusEffectDefinitionPicker()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel(new GUIContent(
                "Definition",
                "Project StatusEffectDefinitionSO assets. The selected effect is applied to or removed from the current entity."));

            if (!_debugStatusEffectDefinitionsLoaded)
                ScanDebugStatusEffectDefinitions();

            string[] options = new string[_debugStatusEffectDefinitions.Count + 1];
            options[0] = "<None>";

            int currentIndex = 0;
            for (int index = 0; index < _debugStatusEffectDefinitions.Count; index++)
            {
                StatusEffectDefinitionSO definition = _debugStatusEffectDefinitions[index];
                options[index + 1] = definition != null
                    ? $"{definition.Id} ({definition.Polarity})"
                    : "<Missing>";

                if (definition == _debugStatusEffectDefinition)
                    currentIndex = index + 1;
            }

            int nextIndex = EditorGUILayout.Popup(currentIndex, options);
            _debugStatusEffectDefinition = nextIndex > 0 && nextIndex - 1 < _debugStatusEffectDefinitions.Count
                ? _debugStatusEffectDefinitions[nextIndex - 1]
                : null;

            if (GUILayout.Button("Rescan", GUILayout.Width(62f)))
                ScanDebugStatusEffectDefinitions();
        }

        if (_debugStatusEffectDefinitions.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No StatusEffectDefinitionSO assets were found under Assets. Create one or rescan after importing it.",
                MessageType.Info);
        }
    }

    // Purpose: Performs the scan debug status effect definitions workflow for this component.
    private void ScanDebugStatusEffectDefinitions()
    {
        _debugStatusEffectDefinitions.Clear();

        string[] guids = AssetDatabase.FindAssets("t:StatusEffectDefinitionSO", new[] { "Assets" });
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            StatusEffectDefinitionSO definition = AssetDatabase.LoadAssetAtPath<StatusEffectDefinitionSO>(path);
            if (definition != null)
                _debugStatusEffectDefinitions.Add(definition);
        }

        _debugStatusEffectDefinitions.Sort((left, right) => string.Compare(
            left != null ? left.Id : string.Empty,
            right != null ? right.Id : string.Empty,
            StringComparison.OrdinalIgnoreCase));
        _debugStatusEffectDefinitionsLoaded = true;

        if (_debugStatusEffectDefinition != null &&
            !_debugStatusEffectDefinitions.Contains(_debugStatusEffectDefinition))
        {
            _debugStatusEffectDefinition = null;
        }
    }
}
