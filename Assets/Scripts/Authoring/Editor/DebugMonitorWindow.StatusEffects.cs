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
    private int _debugStatusEffectStacks = 1;
    private int _debugBleedingTreatment = 1;
    private readonly List<StatusEffectDefinitionSO> _debugStatusEffectDefinitions = new();
    private bool _debugStatusEffectDefinitionsLoaded;

    // Purpose: Draws the status effect debug controls authoring UI.
    private void DrawStatusEffectDebugControls(CharacterRow row)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("상태이상 — 실행 중인 캐릭터", EditorStyles.boldLabel);

        EntityStatusEffectRuntime runtime = row.statusEffects;
        if (runtime == null)
        {
            EditorGUILayout.HelpBox("Selected entity has no EntityStatusEffectRuntime.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        DrawStatusEffectDefinitionPicker();

        bool persistent = _debugStatusEffectDefinition != null && _debugStatusEffectDefinition.PersistentStacks;
        if (_debugStatusEffectDefinition != null)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("선택한 상태이상 SO", _debugStatusEffectDefinition, typeof(StatusEffectDefinitionSO), false);
            if (GUILayout.Button("SO 기본 설정 보기 — Inspector에서 편집"))
            { Selection.activeObject = _debugStatusEffectDefinition; EditorGUIUtility.PingObject(_debugStatusEffectDefinition); }
        }
        if (persistent)
        {
            _debugStatusEffectStacks = Mathf.Clamp(EditorGUILayout.IntField("추가할 스택", _debugStatusEffectStacks),
                1, _debugStatusEffectDefinition.MaximumStacks);
            EditorGUILayout.LabelField($"영구 · 최대 {_debugStatusEffectDefinition.MaximumStacks}스택 · 시간 만료 없음",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField($"스택당 피해 {_debugStatusEffectDefinition.DamagePerStackPerSecond:0.##} {(_debugStatusEffectDefinition.DealsInfectionDamage ? "감염" : "HP")}/초 · " +
                $"피해 주기 {_debugStatusEffectDefinition.DamageTickInterval:0.##}초", EditorStyles.wordWrappedMiniLabel);
        }
        else
        {
            _debugStatusEffectDurationOverride = Mathf.Max(0f, EditorGUILayout.FloatField(
                new GUIContent("적용 시간 (초)", "0이면 선택한 상태이상 SO의 기본 지속시간을 사용합니다."),
                _debugStatusEffectDurationOverride));
        }

        using (new EditorGUI.DisabledScope(_debugStatusEffectDefinition == null || !Application.isPlaying))
        {
            if (GUILayout.Button(persistent ? "선택 효과 스택 추가" : "선택 효과 적용 / 시간 갱신"))
            {
                float duration = _debugStatusEffectDurationOverride > 0f
                    ? _debugStatusEffectDurationOverride
                    : -1f;
                ApplyDebugStatusEffect(runtime, _debugStatusEffectDefinition, duration, _debugStatusEffectStacks);
            }

            if (GUILayout.Button("선택 효과 완전 제거"))
                runtime.Remove(_debugStatusEffectDefinition);
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying || runtime.ActiveEffects.Count == 0))
        {
            if (GUILayout.Button("모든 효과 제거"))
                runtime.RemoveAll();
        }

        EditorGUILayout.LabelField("출혈 치료 — 아이템을 소비하지 않는 디버그 조작", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("현재 출혈", $"{runtime.BleedingStacks}스택");
        _debugBleedingTreatment = Mathf.Max(1, EditorGUILayout.IntField("치료할 출혈 스택", _debugBleedingTreatment));
        using (new EditorGUI.DisabledScope(!Application.isPlaying || runtime.BleedingStacks == 0))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("지정 스택 치료")) runtime.TreatBleeding(_debugBleedingTreatment);
            if (GUILayout.Button("출혈 전부 치료")) runtime.TreatBleeding(runtime.BleedingStacks);
        }
        EditorGUILayout.LabelField("이 버튼은 현재 캐릭터에만 적용됩니다. SO 기본값·붕대 내구도는 변경하지 않습니다.",
            EditorStyles.wordWrappedMiniLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play Mode에서 캐릭터의 상태이상을 변경할 수 있습니다.", MessageType.None);
        }
        if (runtime.ActiveEffects.Count == 0)
        {
            EditorGUILayout.LabelField("적용 중인 효과", "없음");
        }
        else
        {
            EditorGUILayout.LabelField("적용 중인 효과", runtime.ActiveEffects.Count.ToString());
            for (int index = 0; index < runtime.ActiveEffects.Count; index++)
            {
                EntityStatusEffectRuntime.ActiveStatusEffect activeEffect = runtime.ActiveEffects[index];
                StatusEffectDefinitionSO definition = activeEffect.Definition;
                string id = definition != null ? definition.Id : "Missing Definition";
                EditorGUILayout.LabelField($"• {id}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(FormatDebugStatusEffect(activeEffect), EditorStyles.wordWrappedMiniLabel);
            }
        }

        EditorGUILayout.EndVertical();
    }

    // Both the character table and the detail panel use the same lifetime/stack description.
    internal static string FormatDebugStatusEffect(EntityStatusEffectRuntime.ActiveStatusEffect effect)
    {
        var definition = effect?.Definition;
        if (definition == null) return "정의 없음";
        if (!definition.PersistentStacks) return $"남은 시간 {effect.RemainingSeconds:0.0}초";
        string damage = definition.DamagePerStackPerSecond > 0f
            ? $" · 피해 {definition.DamagePerStackPerSecond * effect.Stacks:0.##} {(definition.DealsInfectionDamage ? "감염" : "HP")}/초" : "";
        if (definition is InfectionAggravationStatusSO aggravation)
            return $"감염악화 · {effect.Stacks}/{definition.MaximumStacks}스택 · 원형 {effect.Gauge:0.##}/{aggravation.Settings.baseGaugeCapacity:0.##} · 완주마다 최대 HP -{aggravation.Settings.maximumHealthLossPerStack:0.##} (주기 피해 없음)";
        return $"영구 · {effect.Stacks}/{definition.MaximumStacks}스택{damage}";
    }

    internal static bool ApplyDebugStatusEffect(EntityStatusEffectRuntime runtime, StatusEffectDefinitionSO definition,
        float duration, int stacks)
    {
        if (runtime == null || definition == null) return false;
        int count = definition.PersistentStacks ? Mathf.Clamp(stacks, 1, definition.MaximumStacks) : 1;
        bool applied = false;
        for (int i = 0; i < count; i++) applied |= runtime.Apply(definition, duration);
        return applied;
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
