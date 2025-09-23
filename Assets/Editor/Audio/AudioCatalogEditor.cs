// Assets/Editor/AudioCatalogEditor.cs
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioCatalog))]
public class AudioCatalogEditor : Editor
{
    SerializedProperty itemsProp;

    static readonly string[] TopOrder = { "bgm", "ui", "sfx", "voice", "amb" };

    void OnEnable() => itemsProp = serializedObject.FindProperty("items");

    public override void OnInspectorGUI()
    {
        var catalog = (AudioCatalog)target;

        EditorGUILayout.Space(2);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Sort by Namespace", GUILayout.Height(22)))
            {
                // 컨텍스트메뉴 메서드 호출
                var m = typeof(AudioCatalog).GetMethod("Editor_Sort",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                m?.Invoke(catalog, null);
                serializedObject.Update();
            }
            if (GUILayout.Button("Rebuild Map", GUILayout.Height(22)))
            {
                catalog.Build();
                EditorUtility.SetDirty(catalog);
            }
        }


        EditorGUILayout.Space(6);
        DrawGrouped();
    }

    void DrawGrouped()
    {
        if (itemsProp == null) return;

        // 그룹별 헤더 출력
        for (int g = -1; g < TopOrder.Length; g++)
        {
            string group = g < 0 ? "others" : TopOrder[g];
            bool hasAny = false;

            // 그룹 헤더
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(group.ToUpper(), EditorStyles.boldLabel);

                for (int i = 0; i < itemsProp.arraySize; i++)
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
                            itemsProp.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                    EditorGUILayout.Space(2);
                }

                if (!hasAny) EditorGUILayout.LabelField("— empty —", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
