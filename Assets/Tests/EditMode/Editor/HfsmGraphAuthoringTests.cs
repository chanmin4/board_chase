using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.Editor;
using VSplatter.StateMachine.ScriptableObjects;

public sealed class HfsmGraphAuthoringTests
{
    private readonly List<UnityEngine.Object> _objects = new();
    private T Temp<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); _objects.Add(obj); return obj; }
    [TearDown] public void Cleanup() { foreach (var obj in _objects) { Undo.ClearUndo(obj); UnityEngine.Object.DestroyImmediate(obj); } _objects.Clear(); }
    [Test] public void DeleteFirstTransition_DoesNotSilentlyChangeEntry()
    {
        var table = Temp<TransitionTableSO>(); var a = Temp<StateSO>(); var b = Temp<StateSO>();
        HfsmGraphAuthoring.AddTransition(table, a, b); HfsmGraphAuthoring.AddTransition(table, b, a);
        HfsmGraphAuthoring.DeleteTransition(table, 0); Assert.AreSame(a, table.InitialState); Assert.AreEqual(1, table.Transitions.Count);
    }
    [Test] public void Reconnect_PreservesSourceConditionsAndPriority()
    {
        var table = Temp<TransitionTableSO>(); var a = Temp<StateSO>(); var b = Temp<StateSO>(); var c = Temp<StateSO>(); HfsmGraphAuthoring.AddTransition(table, a, b);
        var so = new SerializedObject(table); var row = so.FindProperty("_transitions").GetArrayElementAtIndex(0); row.FindPropertyRelative("Priority").intValue = 9; row.FindPropertyRelative("Always").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
        HfsmGraphAuthoring.Reconnect(table, 0, c); Assert.AreSame(a, table.Transitions[0].FromState); Assert.AreSame(c, table.Transitions[0].ToState); Assert.AreEqual(9, table.Transitions[0].Priority); Assert.IsTrue(table.Transitions[0].Always);
    }
    [Test] public void NewTransition_DoesNotInheritLastRowsSettings()
    {
        var table = Temp<TransitionTableSO>(); var a = Temp<StateSO>(); var b = Temp<StateSO>(); HfsmGraphAuthoring.AddTransition(table, a, b);
        var so = new SerializedObject(table); var row = so.FindProperty("_transitions").GetArrayElementAtIndex(0); row.FindPropertyRelative("Priority").intValue = 12; row.FindPropertyRelative("Always").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
        int added = HfsmGraphAuthoring.AddTransition(table, b, a); Assert.AreEqual(0, table.Transitions[added].Priority); Assert.IsFalse(table.Transitions[added].Always); Assert.IsEmpty(table.Transitions[added].Conditions);
    }
    [Test] public void GraphMutation_SupportsUndoAndRedo()
    {
        var table = Temp<TransitionTableSO>(); var a = Temp<StateSO>(); var b = Temp<StateSO>();
        Undo.IncrementCurrentGroup();
        HfsmGraphAuthoring.AddTransition(table, a, b);
        Undo.FlushUndoRecordObjects();
        Undo.PerformUndo();
        Assert.IsEmpty(table.Transitions);
        Undo.PerformRedo();
        Assert.AreEqual(1, table.Transitions.Count);
        Assert.AreSame(a, table.Transitions[0].FromState);
        Assert.AreSame(b, table.Transitions[0].ToState);
        Undo.IncrementCurrentGroup();
    }
    [Test] public void ChildRegistration_SetsActualEntry_NotJustAVisualGroup()
    {
        var table = Temp<TransitionTableSO>(); var parent = Temp<StateSO>(); var child = Temp<StateSO>(); HfsmGraphAuthoring.RegisterState(table, parent); HfsmGraphAuthoring.RegisterState(table, child, parent);
        Assert.AreSame(parent, table.InitialState); Assert.AreSame(child, table.Hierarchy.First(h => h.State == parent).InitialChild); Assert.IsEmpty(HfsmGraphAuthoring.Validate(table));
        Assert.Throws<InvalidOperationException>(() => HfsmGraphAuthoring.RegisterState(table, parent, child));
    }
    [Test] public void EnemyAndPlayerExposeExistingCoordinatorHierarchy_WithoutAssetMutation()
    {
        foreach (var path in new[] { "Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyBasePrefab.prefab", "Assets/Prefabs/GameScene/Shooter/Player/Player.prefab" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); Assert.IsNotNull(prefab, path);
            var sources = prefab.GetComponentsInChildren<MonoBehaviour>(true).OfType<IStateMachineHierarchySource>().ToArray(); Assert.IsNotEmpty(sources, path);
            var bindings = new List<StateMachineChildBinding>(); foreach (var source in sources) source.GetHierarchyBindings(bindings);
            Assert.IsTrue(bindings.Any(b => b.Enabled && b.ParentState != null && b.ParentState.name.Contains("Alive") && b.Child != null), path);
            Assert.IsFalse(EditorUtility.IsDirty(prefab), "Reading hierarchy must not dirty prefab");
        }
    }
}
