// Assets/Editor/AudioCatalogEditor.cs
using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomEditor(typeof(AudioCatalog))]
public class AudioCatalogEditor : Editor
{
    SerializedProperty itemsProp;
    static readonly string[] TopOrder = { "bgm", "ui", "sfx", "voice", "amb" };

    void OnEnable()
    {
        // 최초 1회
        itemsProp = serializedObject.FindProperty("items");
    }

    public override void OnInspectorGUI()
    {
        var catalog = (AudioCatalog)target;

        // ⬇⬇⬇ 핵심: 외부에서 리스트가 바뀐 걸 매 프레임 가져오기
        serializedObject.UpdateIfRequiredOrScript();
        itemsProp = serializedObject.FindProperty("items");

        EditorGUILayout.Space(2);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Sort by Namespace", GUILayout.Height(22)))
            {
                // ✅ 실제 메서드명으로 호출
                var m = typeof(AudioCatalog).GetMethod(
                    "Editor_SortByNamespace",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                Undo.RecordObject(catalog, "Sort Audio Catalog");
                m?.Invoke(catalog, null);

                EditorUtility.SetDirty(catalog);
                // 리스트 구조가 바뀌니 다시 가져오도록
                serializedObject.UpdateIfRequiredOrScript();
                itemsProp = serializedObject.FindProperty("items");
                Repaint();
            }

            if (GUILayout.Button("Rebuild Map", GUILayout.Height(22)))
            {
                Undo.RecordObject(catalog, "Rebuild Map");
                catalog.Build();
                EditorUtility.SetDirty(catalog);
                Repaint();
            }
        }

        EditorGUILayout.Space(6);
        DrawGrouped();

        // 변경 반영
        serializedObject.ApplyModifiedProperties();
    }

    void DrawGrouped()
    {
        if (itemsProp == null) return;

        // 그룹별 헤더 출력
        for (int g = -1; g < TopOrder.Length; g++)
        {
            string group = g < 0 ? "others" : TopOrder[g];
            bool hasAny = false;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(group.ToUpper(), EditorStyles.boldLabel);

                // arraySize가 도중에 바뀌면 예외날 수 있으므로 캐싱
                int count = itemsProp.arraySize;
                for (int i = 0; i < count; i++)
                {
                    var elem = itemsProp.GetArrayElementAtIndex(i);
                    var keyProp = elem.FindPropertyRelative("key");
                    string key = keyProp.stringValue?.Trim().ToLowerInvariant();

                    string top = "";
                    if (!string.IsNullOrEmpty(key))
                    {
                        int dot = key.IndexOf('.');
                        top = dot >= 0 ? key.Substring(0, dot) : key;
                    }

                    bool inGroup = (g < 0 && System.Array.IndexOf(TopOrder, top) < 0)
                                   || (g >= 0 && top == group);

                    if (!inGroup) continue;
                    hasAny = true;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(elem, GUIContent.none, includeChildren: true);

                        if (GUILayout.Button("X", GUILayout.Width(22)))
                        {
                            // ✅ Undo + 즉시 갱신
                            Undo.RecordObject(target, "Remove Audio Entry");
                            itemsProp.DeleteArrayElementAtIndex(i);
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(target);
                            Repaint();
                            GUIUtility.ExitGUI(); // 그리기 루프 안전 종료
                        }
                    }
                    EditorGUILayout.Space(2);
                }

                if (!hasAny) EditorGUILayout.LabelField("— empty —", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);
        }
    }
}
