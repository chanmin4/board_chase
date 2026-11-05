using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VisionOccluder)), CanEditMultipleObjects]
public sealed class VisionOccluderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_blocksVision"), new GUIContent("시야 차단 ON"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_overrideChildren"), new GUIContent("하위 설정 덮어쓰기"));
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.HelpBox("기본 OFF = 뒤쪽 시야 통과. ON = 벽처럼 차단.\n이동·총알 충돌 레이어/콜라이더는 그대로 유지합니다. Trigger는 시야를 막지 않습니다.\n루팅 물체·바닥 아이템·캐릭터는 차단에서 제외합니다.", MessageType.None);
    }
}
