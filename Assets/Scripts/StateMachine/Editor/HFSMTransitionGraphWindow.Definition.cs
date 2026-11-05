using UnityEditor;
using UnityEngine;
namespace VSplatter.StateMachine.Editor
{
    internal sealed partial class HFSMTransitionGraphWindow
    {
        // Host definitions own scope entry overrides; table-only views keep legacy table editing.
        private void SetScopeEntry(HfsmGraphModel.Node node)
        {
            if (EditorApplication.isPlaying) return;
            var entry = ScopeEntryProperty(node.Scope, out var so);
            if (entry != null)
            {
                entry.objectReferenceValue = node.State; so.ApplyModifiedProperties();
            }
            RefreshGraph();
        }
        private static SerializedProperty ScopeEntryProperty(HfsmGraphModel.Scope scope, out SerializedObject so)
        {
            // Native tree nodes share a scope runtime; their entry belongs to the owner state's InitialChild.
            bool native = scope.OwnerNode != null && scope.OwnerNode.Scope.Table == scope.Table
                && scope.OwnerNode.Scope.DefinitionScope == scope.DefinitionScope;
            so = new SerializedObject(native || scope.DefinitionScope < 0 ? (Object)scope.Table : scope.Definition); so.Update();
            if (!native) return scope.DefinitionScope < 0 ? so.FindProperty("_initialState")
                : scope.DefinitionScope == 0 ? so.FindProperty("_entry")
                : so.FindProperty("_scopes").GetArrayElementAtIndex(scope.DefinitionScope - 1).FindPropertyRelative("Entry");
            var hierarchy = so.FindProperty("_hierarchy");
            for (int i = 0; i < hierarchy.arraySize; i++)
            {
                var row = hierarchy.GetArrayElementAtIndex(i);
                if (row.FindPropertyRelative("State").objectReferenceValue == scope.OwnerNode.State)
                    return row.FindPropertyRelative("InitialChild");
            }
            return null;
        }
    }
}
