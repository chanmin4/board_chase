using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    [CustomEditor(typeof(HfsmStateMachine))]
    internal sealed class HfsmStateMachineEditor : UnityEditor.Editor
    {
        private bool _advanced;
        public override void OnInspectorGUI()
        {
            var host = (HfsmStateMachine)target; serializedObject.Update();
            EditorGUILayout.LabelField("캐릭터 HFSM · 계층을 하나의 실행기로 관리", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_definition"), new GUIContent("HFSM 그래프"));
            serializedObject.ApplyModifiedProperties();
            if (host.Definition == null) { EditorGUILayout.HelpBox("HFSM 그래프 SO를 연결하세요. 1계층과 내부 행동 연결은 그래프에서 관리합니다.", MessageType.Warning); return; }
            EditorGUILayout.LabelField("시작 상태", host.Definition.Entry != null ? host.Definition.Entry.name + "_1" : "미연결");
            if (GUILayout.Button("HFSM 그래프 열기")) HFSMTransitionGraphWindow.OpenDefinition(host.Definition, host.gameObject);
            if (GUILayout.Button("계층 연결 설정 보기")) Selection.activeObject = host.Definition;
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("현재 실행 경로\n" + host.ActiveStatePath, MessageType.None);
                for (int i = 0; i < host.ScopeCount; i++)
                { var scope = host.GetScope(i); EditorGUILayout.LabelField(scope.Level + "계층 · " + scope.Label, scope.IsScopeActive ? scope.CurrentStateSO?.name : "비활성"); }
            }
            else EditorGUILayout.HelpBox("1계층의 Alive 안에서 2계층 행동 하나를 실행합니다.\n상태 이름 뒤 _숫자는 실제 계층입니다. 전환 순서는 기존 테이블을 따릅니다.", MessageType.Info);
            foreach (string error in host.Definition.Validate()) EditorGUILayout.HelpBox(error, MessageType.Error);
            _advanced = EditorGUILayout.Foldout(_advanced, "고급 · 로그 설정", true);
            if (_advanced)
            { foreach (var field in new[] { "_logStateChanges", "_debugOwnerLabel", "_logInitialState" }) EditorGUILayout.PropertyField(serializedObject.FindProperty(field)); serializedObject.ApplyModifiedProperties(); }
        }
    }
    [CustomEditor(typeof(HfsmDefinitionSO))]
    internal sealed class HfsmDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("① 1계층 테이블 연결 → ② 어떤 상태 안에서 실행할지 선택 → ③ 내부 테이블 연결.\n기존 상태·행동·공유 규칙은 복사하지 않고 그대로 참조합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_rootTable"), new GUIContent("1계층 테이블"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_entry"), new GUIContent("1계층 시작 상태", "비어 있으면 테이블의 기존 시작 상태를 사용합니다."));
                var scopes = serializedObject.FindProperty("_scopes");
                for (int i = 0; i < scopes.arraySize; i++)
                {
                    var binding = scopes.GetArrayElementAtIndex(i);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("내부 행동 연결 " + (i + 1), EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(binding.FindPropertyRelative("Label"), new GUIContent("표시 이름"));
                        var options = new string[i + 1]; options[0] = "1계층 · 최상위";
                        for (int j = 1; j <= i; j++) options[j] = "내부 연결 " + j + " · " + scopes.GetArrayElementAtIndex(j - 1).FindPropertyRelative("Label").stringValue;
                        var owner = binding.FindPropertyRelative("OwnerScope"); owner.intValue = EditorGUILayout.Popup("소속 실행 단위", owner.intValue, options);
                        EditorGUILayout.PropertyField(binding.FindPropertyRelative("OwnerState"), new GUIContent("이 상태 안에서 실행"));
                        EditorGUILayout.PropertyField(binding.FindPropertyRelative("Table"), new GUIContent("내부 행동 테이블"));
                        EditorGUILayout.PropertyField(binding.FindPropertyRelative("Entry"), new GUIContent("시작 행동", "비어 있으면 해당 테이블의 기존 시작 상태를 사용합니다."));
                        var reentry = binding.FindPropertyRelative("Reentry"); reentry.enumValueIndex = EditorGUILayout.Popup("다시 들어올 때", reentry.enumValueIndex, new[] { "이전 행동 이어서", "시작 행동부터" });
                        // 뒤쪽 연결이 이 번호를 참조할 수 있으므로, 마지막부터 지워 잘못된 재연결을 방지합니다.
                        using (new EditorGUI.DisabledScope(i != scopes.arraySize - 1))
                            if (GUILayout.Button(new GUIContent("이 연결 삭제", "뒤쪽 연결부터 삭제하세요. 다른 연결의 소속 번호를 임의로 바꾸지 않습니다.")))
                            { scopes.DeleteArrayElementAtIndex(i); break; }
                    }
                }
                if (GUILayout.Button("+ 내부 행동 연결 추가"))
                { int index = scopes.arraySize++; var row = scopes.GetArrayElementAtIndex(index); row.FindPropertyRelative("Label").stringValue = ""; row.FindPropertyRelative("OwnerScope").intValue = 0; foreach (string name in new[] { "OwnerState", "Table", "Entry" }) row.FindPropertyRelative(name).objectReferenceValue = null; row.FindPropertyRelative("Reentry").enumValueIndex = 0; }
            }
            serializedObject.ApplyModifiedProperties();
            var definition = (HfsmDefinitionSO)target;
            foreach (var error in definition.Validate()) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (GUILayout.Button("HFSM 그래프로 보기")) HFSMTransitionGraphWindow.OpenDefinition(definition);
        }
    }
}
