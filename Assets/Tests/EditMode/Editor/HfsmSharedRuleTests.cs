using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.Editor;
using VSplatter.StateMachine.ScriptableObjects;

public static class HfsmSharedTrace { public static readonly List<string> Events = new(); }
public class HfsmSharedProbeActionSO : StateActionSO<HfsmSharedProbeAction> { }
public class HfsmSharedProbeAction : StateAction
{
    public override void Awake(StateMachine machine) => HfsmSharedTrace.Events.Add("awake action " + OriginSO.name);
    public override void OnStateEnter() => HfsmSharedTrace.Events.Add("enter action " + OriginSO.name);
    public override void OnUpdate() => HfsmSharedTrace.Events.Add("tick action " + OriginSO.name);
    public override void OnStateExit() => HfsmSharedTrace.Events.Add("exit action " + OriginSO.name);
    public override void OnInterrupted(StateExitReason reason) => HfsmSharedTrace.Events.Add("cancel action " + OriginSO.name + " " + reason);
}
public class HfsmSharedProbeGuardSO : StateConditionSO<HfsmSharedProbeGuard> { public bool Value; }
public class HfsmSharedProbeGuard : Condition
{
    public override void Awake(StateMachine machine) => HfsmSharedTrace.Events.Add("awake condition " + OriginSO.name);
    public override void OnStateEnter() => HfsmSharedTrace.Events.Add("enter condition " + OriginSO.name);
    public override void OnStateExit() => HfsmSharedTrace.Events.Add("exit condition " + OriginSO.name);
    protected override void PrepareEvaluation() => HfsmSharedTrace.Events.Add("prepare " + OriginSO.name);
    protected override bool Statement() { HfsmSharedTrace.Events.Add("read " + OriginSO.name); return ((HfsmSharedProbeGuardSO)OriginSO).Value; }
}

/// <summary>Memory definitions only; no global Undo, scene objects or real asset saves.</summary>
public sealed class HfsmSharedRuleTests
{
    private readonly List<UnityEngine.Object> _objects = new();
    private readonly Dictionary<string, HfsmSharedProbeGuardSO> _guards = new();
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Flags).SetValue(obj, value);
    private T Temp<T>(string name) where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); o.name = name; _objects.Add(o); return o; }
    private StateSO State(string name) { var s = Temp<StateSO>(name); Set(s, "_actions", new[] { Temp<HfsmSharedProbeActionSO>(name) }); return s; }
    private TransitionTableSO.ConditionUsage Guard(string name, TransitionTableSO.Operator op = TransitionTableSO.Operator.And)
    {
        if (!_guards.TryGetValue(name, out var g)) _guards.Add(name, g = Temp<HfsmSharedProbeGuardSO>(name));
        return new TransitionTableSO.ConditionUsage { Condition = g, ExpectedResult = TransitionTableSO.Result.True, Operator = op };
    }
    private TransitionTableSO Fixture()
    {
        var a = State("Idle"); var b = State("Patrol"); var c = State("Melee"); var d = State("Throwable");
        TransitionTableSO.TransitionItem Row(StateSO from, StateSO to, params string[] guards)
            => new() { FromState = from, ToState = to, Conditions = guards.Select(g => Guard(g)).ToArray() };
        var table = Temp<TransitionTableSO>("fixture");
        Set(table, "_transitions", new[] { Row(a, c, "melee"), Row(a, d, "throw"), Row(a, b, "patrol"),
            Row(b, c, "melee"), Row(b, d, "throw"), Row(b, a, "idle"),
            Row(c, c, "meleeFinished", "melee"), Row(c, b, "meleeFinished"), Row(d, b, "throwFinished") });
        return table;
    }
    private SharedTransitionRuleSO Share(TransitionTableSO table, int seed)
    {
        var rule = Temp<SharedTransitionRuleSO>("shared " + seed); rule.InitializeForAuthoring(table.ResolveTransition(seed));
        HfsmSharedRuleAuthoring.Bind(table, HfsmSharedRuleAuthoring.MatchingInlineRows(table, seed), rule); return rule;
    }
    private static State Compile(TransitionTableSO table)
        => (State)typeof(TransitionTableSO).GetMethod("GetInitialState", Flags).Invoke(table, new object[] { null });
    private string[] Trace(TransitionTableSO table, bool[][] inputs)
    {
        HfsmSharedTrace.Events.Clear(); var runtime = new HfsmRuntime(); runtime.Reset(Compile(table));
        for (int i = 0; i < inputs.Length; i++)
        {
            int k = 0; foreach (var guard in _guards.Values) guard.Value = inputs[i][k++];
            if (i % 13 == 5) runtime.Suspend();
            if (i % 13 == 6) runtime.Resume();
            if (i % 19 == 18) runtime.Reset(Compile(table), i % 2 == 0);
            runtime.Tick(0);
        }
        runtime.Stop(StateExitReason.Interrupted); return HfsmSharedTrace.Events.ToArray();
    }
    [TearDown] public void Cleanup()
    { foreach (var o in _objects) { Undo.ClearUndo(o); UnityEngine.Object.DestroyImmediate(o); } _objects.Clear(); _guards.Clear(); HfsmSharedTrace.Events.Clear(); }

    [Test] public void ThirtyTwoInputSequencesKeepAwakeConditionAndLifecycleTracesIdentical()
    {
        var table = Fixture(); var inline = table.Transitions.ToArray();
        for (int seed = 0; seed < 32; seed++)
        {
            Set(table, "_transitions", (TransitionTableSO.TransitionItem[])inline.Clone()); var random = new System.Random(seed);
            var inputs = Enumerable.Range(0, 100).Select(_ => Enumerable.Range(0, _guards.Count).Select(i => random.Next(2) == 0).ToArray()).ToArray();
            var expected = Trace(table, inputs); Share(table, 0); Share(table, 1);
            CollectionAssert.AreEqual(expected, Trace(table, inputs), "seed " + seed);
        }
    }
    [Test] public void SourcesPrioritiesEntryAndPhysicalSlotsRemainOwnedByTheUsage()
    {
        var table = Fixture(); var rows = table.Transitions.ToArray(); rows[3].Priority = 19; Set(table, "_transitions", rows);
        var entry = table.InitialState; Share(table, 0);
        Assert.AreEqual(rows.Length, table.Transitions.Count); Assert.AreSame(entry, table.InitialState); Assert.IsFalse(table.HasExplicitInitialState);
        for (int i = 0; i < rows.Length; i++)
        { var actual = table.ResolveTransition(i); Assert.AreSame(rows[i].FromState, actual.FromState); Assert.AreEqual(rows[i].Priority, actual.Priority); Assert.IsTrue(HfsmSharedRuleAuthoring.SameDefinition(rows[i], actual)); }
    }
    [Test] public void FirstSuccessfulSharedRuleSkipsLaterPreparation()
    {
        var table = Fixture(); Share(table, 0); Share(table, 1); _guards["melee"].Value = _guards["throw"].Value = true;
        var runtime = new HfsmRuntime(); runtime.Reset(Compile(table)); HfsmSharedTrace.Events.Clear(); runtime.Tick(0);
        Assert.AreEqual("Melee", runtime.Current.Name); Assert.IsFalse(HfsmSharedTrace.Events.Contains("prepare throw"));
    }
    [Test] public void AttackCompletionGatesRemainLocalAndDoNotReceiveGeneralSelection()
    {
        var table = Fixture(); Share(table, 0); Share(table, 1);
        Assert.IsFalse(table.Transitions[6].UseSharedRule); Assert.IsFalse(table.Transitions[7].UseSharedRule); Assert.IsFalse(table.Transitions[8].UseSharedRule);
        _guards["melee"].Value = true; var runtime = new HfsmRuntime(); runtime.Reset(Compile(table)); runtime.Tick(0);
        _guards["melee"].Value = false; _guards["throw"].Value = true; HfsmSharedTrace.Events.Clear(); runtime.Tick(0);
        Assert.AreEqual("Melee", runtime.Current.Name); Assert.IsFalse(HfsmSharedTrace.Events.Contains("prepare throw"));
        _guards["meleeFinished"].Value = true; runtime.Tick(0); Assert.AreEqual("Patrol", runtime.Current.Name);
    }
    [Test] public void SharedEditPropagatesOnlyToExplicitUsagesAndDetachCopiesCurrentDefinition()
    {
        var table = Fixture(); var original = table.ResolveTransition(0); var rule = Share(table, 0);
        var target = table.Transitions[1].ToState; Set(rule, "_toState", target);
        Assert.AreSame(target, table.ResolveTransition(0).ToState); Assert.AreSame(target, table.ResolveTransition(3).ToState);
        Assert.AreSame(original.ToState, table.Transitions[0].ToState, "snapshot preserved, not executed");
        HfsmSharedRuleAuthoring.Detach(table, 0); Set(rule, "_toState", original.ToState);
        Assert.AreSame(target, table.ResolveTransition(0).ToState); Assert.AreSame(original.ToState, table.ResolveTransition(3).ToState);
        Assert.IsFalse(table.Transitions[0].UseSharedRule); Assert.IsNull(table.Transitions[0].SharedRule);
    }
    [Test] public void MissingSharedReferenceFailsClosedAndIsReportedWithoutDeletingRows()
    {
        var table = Fixture(); var rows = table.Transitions.ToArray(); rows[0].UseSharedRule = true; Set(table, "_transitions", rows);
        Assert.Throws<InvalidOperationException>(() => table.ResolveTransition(0)); Assert.IsNotEmpty(HfsmGraphAuthoring.Validate(table));
        Assert.AreEqual(rows.Length, table.Transitions.Count);
    }
    [Test] public void TwoActorsShareDefinitionsButNotStatesOrConditionCaches()
    {
        var table = Fixture(); Share(table, 0); var a = Compile(table); var b = Compile(table);
        Assert.AreNotSame(a, b);
        var transitions = typeof(State).GetField("_transitions", Flags);
        var conditions = typeof(StateTransition).GetField("_conditions", Flags);
        var firstA = ((StateTransition[])transitions.GetValue(a))[0]; var firstB = ((StateTransition[])transitions.GetValue(b))[0];
        var guardField = typeof(StateCondition).GetField("_condition", Flags);
        Assert.AreNotSame(guardField.GetValue(((StateCondition[])conditions.GetValue(firstA))[0]), guardField.GetValue(((StateCondition[])conditions.GetValue(firstB))[0]));
    }
    [Test] public void DifferentExpectedResultsOperatorsAndExitReasonsAreNotMerged()
    {
        var table = Fixture(); var a = table.ResolveTransition(0); var b = a; b.Conditions = (TransitionTableSO.ConditionUsage[])a.Conditions.Clone();
        b.Conditions[0].ExpectedResult = TransitionTableSO.Result.False; Assert.IsFalse(HfsmSharedRuleAuthoring.SameDefinition(a, b));
        b.Conditions[0] = a.Conditions[0]; b.Conditions[0].Operator = TransitionTableSO.Operator.Or; Assert.IsFalse(HfsmSharedRuleAuthoring.SameDefinition(a, b));
        b.Conditions[0] = a.Conditions[0]; b.ExitReason = StateExitReason.Interrupted; Assert.IsFalse(HfsmSharedRuleAuthoring.SameDefinition(a, b));
    }
    [Test] public void RuleMapGroupsOnlyExplicitReferencesAndRetainsEveryOriginalRow()
    {
        var table = Fixture(); Share(table, 0); Share(table, 1);
        var type = typeof(HfsmGraphAuthoring).Assembly.GetType("VSplatter.StateMachine.Editor.HfsmGraphModel"); var model = Activator.CreateInstance(type);
        type.GetMethod("Build").Invoke(model, new object[] { table, null, null }); var root = type.GetProperty("Root").GetValue(model);
        var map = type.Assembly.GetType("VSplatter.StateMachine.Editor.HfsmRuleMap");
        var groups = ((System.Collections.IEnumerable)map.GetMethod("Build").Invoke(null, new[] { root, root })).Cast<object>().ToArray();
        Assert.AreEqual(7, groups.Length);
        Assert.AreEqual(table.Transitions.Count, groups.Sum(g => ((Array)g.GetType().GetField("Usages").GetValue(g)).Length));
    }
    [Test] public void ReconnectDetachesOneUsageWithoutChangingOtherSources()
    {
        var table = Fixture(); var original = table.ResolveTransition(0); Share(table, 0); var target = table.Transitions[1].ToState;
        HfsmGraphAuthoring.Reconnect(table, 0, target);
        Assert.IsFalse(table.Transitions[0].UseSharedRule); Assert.AreSame(target, table.ResolveTransition(0).ToState);
        Assert.AreSame(original.ToState, table.ResolveTransition(3).ToState); Assert.IsTrue(table.Transitions[3].UseSharedRule);
    }
}
