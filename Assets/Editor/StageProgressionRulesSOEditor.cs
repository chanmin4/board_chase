using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageProgressionRulesSO))]
public class StageProgressionRulesSOEditor : Editor
{
    private SerializedProperty _rules;

    private void OnEnable()
    {
        _rules = serializedObject.FindProperty("_rules");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();
        EditorGUILayout.Space(6f);
        DrawRules();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Script",
                MonoScript.FromScriptableObject((StageProgressionRulesSO)target),
                typeof(StageProgressionRulesSO),
                false);
        }
    }

    private void DrawRules()
    {
        if (_rules == null)
            return;

        EditorGUILayout.LabelField("Stage Progression Rules", EditorStyles.boldLabel);

        int size = Mathf.Max(0, EditorGUILayout.IntField("Rules", _rules.arraySize));

        if (size != _rules.arraySize)
            _rules.arraySize = size;

        EditorGUILayout.Space(4f);

        for (int i = 0; i < _rules.arraySize; i++)
        {
            SerializedProperty rule = _rules.GetArrayElementAtIndex(i);
            DrawRule(rule, i);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Rule"))
            _rules.arraySize++;

        using (new EditorGUI.DisabledScope(_rules.arraySize <= 0))
        {
            if (GUILayout.Button("Remove Last"))
                _rules.arraySize--;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRule(SerializedProperty rule, int index)
    {
        SerializedProperty stageIndex = rule.FindPropertyRelative("stageIndex");
        SerializedProperty displayName = rule.FindPropertyRelative("displayName");

        string label = $"Element {index}";

        if (stageIndex != null)
            label += $"  Stage {stageIndex.intValue}";

        if (displayName != null && !string.IsNullOrWhiteSpace(displayName.stringValue))
            label += $"  {displayName.stringValue}";

        rule.isExpanded = EditorGUILayout.Foldout(rule.isExpanded, label, true);

        if (!rule.isExpanded)
            return;

        EditorGUI.indentLevel++;

        DrawProperty(rule, "stageIndex");
        DrawProperty(rule, "displayName");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Stage Map", EditorStyles.boldLabel);
        DrawProperty(rule, "roomGridSize");

        SerializedProperty goalRoomType = rule.FindPropertyRelative("goalRoomType");
        if (goalRoomType != null)
        {
            EditorGUILayout.PropertyField(goalRoomType);
            DrawSelectedGoalOptions(rule, goalRoomType);
        }

        DrawProperty(rule, "goalDebugLogInterval");
        DrawProperty(rule, "useStartSectorOnly");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Stage Completion", EditorStyles.boldLabel);
        DrawProperty(rule, "advanceStageOnStartSectorComplete");
        DrawProperty(rule, "advanceStageOnBossBattleComplete");
        DrawProperty(rule, "isFinalStage");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Stage Rest", EditorStyles.boldLabel);
        DrawProperty(rule, "restSecondsBeforeNextStage");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Reward", EditorStyles.boldLabel);
        DrawProperty(rule, "infectionControlRecoverOnComplete");

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(8f);
    }

    private static void DrawSelectedGoalOptions(
        SerializedProperty rule,
        SerializedProperty goalRoomType)
    {
        string selectedGoalName = goalRoomType.enumNames[goalRoomType.enumValueIndex];

        switch (selectedGoalName)
        {
            case nameof(StageGoalRoomType.Named):
                DrawProperty(rule, "namedGoal");
                break;

            case nameof(StageGoalRoomType.Boss):
                DrawProperty(rule, "bossGoal");
                break;

            case nameof(StageGoalRoomType.BigMonsterWave):
                DrawProperty(rule, "bigMonsterWaveGoal");
                break;
        }
    }

    private static void DrawProperty(SerializedProperty owner, string relativeName)
    {
        SerializedProperty property = owner.FindPropertyRelative(relativeName);

        if (property != null)
            EditorGUILayout.PropertyField(property, includeChildren: true);
    }
}
