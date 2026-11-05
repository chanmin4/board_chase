using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public sealed class HfsmCompatibilityTests
{
    private sealed class ProbeAction : StateAction
    {
        public readonly List<string> Trace = new();
        public override void OnUpdate() => Trace.Add("tick");
        public override void OnStateEnter() => Trace.Add("enter");
        public override void OnStateExit() => Trace.Add("legacy exit");
        public override void OnInterrupted(StateExitReason reason) => Trace.Add("cancel " + reason);
    }
    private sealed class ProbeCondition : Condition
    {
        public int Prepared, Read; public bool Value;
        protected override void PrepareEvaluation() => Prepared++;
        protected override bool Statement() { Read++; return Value; }
    }
    private static State Node(StateAction action = null) => new State(null, null, Array.Empty<StateTransition>(), action == null ? Array.Empty<StateAction>() : new[] { action });
    private static void Rows(State state, params StateTransition[] rows) => typeof(State).GetField("_transitions", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(state, rows);

    [Test] public void EmptyLegacyConditions_DoNotFire()
    { Assert.IsFalse(new StateTransition(Node(), Array.Empty<StateCondition>()).TryGetTransiton(out _)); }
    [Test] public void Always_IsExplicit_AndDefaultsOff()
    { Assert.IsTrue(new StateTransition(Node(), Array.Empty<StateCondition>(), always: true).TryGetTransiton(out _)); }
    [Test] public void SameCondition_IsPreparedOncePerEvaluationPass()
    {
        var condition = new ProbeCondition { Value = false }; var usage = new StateCondition(null, condition, true); var a = Node();
        Rows(a, new StateTransition(Node(), new[] { usage }, new[] { 1 }), new StateTransition(Node(), new[] { usage }, new[] { 1 }));
        a.TryGetTransition(out _); Assert.AreEqual(1, condition.Prepared); Assert.AreEqual(1, condition.Read);
        a.TryGetTransition(out _); Assert.AreEqual(2, condition.Prepared);
    }
    [Test] public void AndShortCircuit_DoesNotPrepareSkippedCondition()
    {
        var no = new ProbeCondition { Value = false }; var skipped = new ProbeCondition { Value = true };
        var edge = new StateTransition(Node(), new[] { new StateCondition(null, no, true), new StateCondition(null, skipped, true) }, new[] { 2 });
        Assert.IsFalse(edge.TryGetTransiton(out _)); Assert.AreEqual(0, skipped.Prepared);
    }
    [Test] public void FirstTrueRowWins_AndLaterPreparationIsSkipped()
    {
        var first = new ProbeCondition { Value = true }; var later = new ProbeCondition { Value = true }; var a = Node(); var b = Node();
        Rows(a, new StateTransition(b, new[] { new StateCondition(null, first, true) }, new[] { 1 }), new StateTransition(Node(), new[] { new StateCondition(null, later, true) }, new[] { 1 }));
        Assert.IsTrue(a.TryGetTransition(out var result)); Assert.AreSame(b, result); Assert.AreEqual(0, later.Prepared);
    }
    [Test] public void Interruption_DoesNotCommitLegacyCompletionEffect()
    {
        var action = new ProbeAction(); var runtime = new HfsmRuntime(); runtime.Reset(Node(action)); runtime.Enter(); runtime.Stop(StateExitReason.Interrupted);
        CollectionAssert.AreEqual(new[] { "enter", "cancel Interrupted" }, action.Trace);
    }
    [Test] public void LegacyExplicitExitReset_RetainsExitCallback()
    {
        var action = new ProbeAction(); var runtime = new HfsmRuntime(); runtime.Reset(Node(action)); runtime.Enter(); runtime.Reset(Node(), true);
        CollectionAssert.AreEqual(new[] { "enter", "legacy exit" }, action.Trace);
    }
    [Test] public void CurrentAuthoredTables_StillUseLegacyDefaults()
    {
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:TransitionTableSO", new[] { "Assets/ScriptableObject/StateMachine" }))
        {
            var table = UnityEditor.AssetDatabase.LoadAssetAtPath<TransitionTableSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            Assert.IsFalse(table.HasExplicitInitialState, table.name); Assert.IsEmpty(table.Hierarchy, table.name);
            foreach (var row in table.Transitions) { Assert.AreEqual(0, row.Priority, table.name); Assert.IsFalse(row.Always, table.name); Assert.AreEqual(StateExitReason.Transition, row.ExitReason, table.name); }
        }
    }
}
