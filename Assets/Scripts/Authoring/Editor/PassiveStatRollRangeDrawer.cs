using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>All inspectors/makers use the same allowed IDs; existing retired values remain visible, never silently rewritten.</summary>
[CustomPropertyDrawer(typeof(PlayerStatId))]
public sealed class ShooterStatIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool passive = property.serializedObject.targetObject is PassiveItemSO;
        var ids = new List<int>();
        var labels = new List<string>();
        int selected = -1;
        foreach (PlayerStatId stat in Enum.GetValues(typeof(PlayerStatId)))
        {
            if (!(passive ? ShooterStatPolicy.IsPassiveStat(stat) : ShooterStatPolicy.IsCharacterStat(stat))) continue;
            if ((int)stat == property.intValue) selected = ids.Count;
            ids.Add((int)stat);
            labels.Add(ObjectNames.NicifyVariableName(stat.ToString()));
        }
        if (selected < 0)
        {
            selected = ids.Count;
            ids.Add(property.intValue);
            labels.Add($"미지원 · 계산 제외 ({(PlayerStatId)property.intValue})");
        }
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        int next = EditorGUI.Popup(position, label.text, selected, labels.ToArray());
        if (EditorGUI.EndChangeCheck()) property.intValue = ids[next];
        EditorGUI.EndProperty();
    }
}

[CustomEditor(typeof(ShooterStatConfigSO), true)]
public sealed class ShooterStatConfigEditor : Editor
{
    private CharacterStatusMakerInspector _inspector;
    private void OnEnable() => _inspector = new CharacterStatusMakerInspector((ScriptableObject)target);
    public override void OnInspectorGUI()
    {
        if (targets.Length != 1)
        {
            EditorGUILayout.HelpBox("공통값/개별 Override는 한 Config씩 선택해서 편집하세요.", MessageType.Info);
            return;
        }
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            _inspector.Draw(true);
    }
}

[CustomPropertyDrawer(typeof(PassiveStatRollRange))]
public sealed class PassiveStatRollRangeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 4f + 11f;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        float height = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, height), property.FindPropertyRelative("stat"), new GUIContent("합연산 스탯"));
        float half = (position.width - 8f) * .5f;
        EditorGUI.PropertyField(new Rect(position.x, position.y + height + 3f, half, height), property.FindPropertyRelative("minimum"), new GUIContent("최소"));
        EditorGUI.PropertyField(new Rect(position.x + half + 8f, position.y + height + 3f, half, height), property.FindPropertyRelative("maximum"), new GUIContent("최대"));
        EditorGUI.PropertyField(new Rect(position.x,position.y+(height+3)*2,position.width,height),property.FindPropertyRelative("type"),new GUIContent("합산 / % 보정"));
        EditorGUI.PropertyField(new Rect(position.x,position.y+(height+3)*3,position.width,height),property.FindPropertyRelative("step"),new GUIContent("추첨 간격 · 0=연속"));
        EditorGUI.EndProperty();
    }
}
