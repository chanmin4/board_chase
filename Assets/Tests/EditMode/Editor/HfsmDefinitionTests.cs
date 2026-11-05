using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public class HfsmProbeActionSO : StateActionSO<HfsmProbeAction> { }
public class HfsmProbeAction : StateAction
{
    public static readonly List<string> Trace = new();
    public override void Awake(StateMachine machine) => Trace.Add("awake " + OriginSO.name);
    public override void OnStateEnter() => Trace.Add("enter " + OriginSO.name);
    public override void OnUpdate() => Trace.Add("tick " + OriginSO.name);
    public override void OnStateExit() => Trace.Add("exit " + OriginSO.name);
}
public class HfsmProbeConditionSO : StateConditionSO<HfsmProbeCondition> { public bool Value; }
public class HfsmProbeCondition : Condition
{
    protected override bool Statement() => ((HfsmProbeConditionSO)OriginSO).Value;
}

public sealed class HfsmDefinitionTests
{
    private readonly List<UnityEngine.Object> _objects = new();
    private T Temp<T>(string name) where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); obj.name = name; _objects.Add(obj); return obj; }
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    private StateSO State(string name, StateActionSO action)
    { var state = Temp<StateSO>(name); Set(state, "_actions", new[] { action }); return state; }
    private State Compile(TransitionTableSO table, bool legacy = false) => (State)typeof(TransitionTableSO).GetMethod(legacy ? "CompileLegacyForCharacterization" : "GetInitialState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(table, new object[] { null });
    [TearDown] public void Cleanup() { foreach (var obj in _objects) UnityEngine.Object.DestroyImmediate(obj); _objects.Clear(); HfsmProbeAction.Trace.Clear(); }

    [Test] public void OriginalAndNewCompiler_ProduceSameFlatAwakeAndExecutionTrace()
    {
        var actionA = Temp<HfsmProbeActionSO>("A"); var shared = Temp<HfsmProbeActionSO>("shared");
        var a = State("a", actionA); var b = State("b", shared); var c = State("c", shared);
        var guard = Temp<HfsmProbeConditionSO>("guard"); guard.Value = true;
        var condition = new[] { new TransitionTableSO.ConditionUsage { Condition = guard, ExpectedResult = TransitionTableSO.Result.True } };
        var table = Temp<TransitionTableSO>("fixture");
        Set(table, "_transitions", new[] { new TransitionTableSO.TransitionItem { FromState = a, ToState = b, Conditions = condition }, new TransitionTableSO.TransitionItem { FromState = b, ToState = c, Conditions = condition }, new TransitionTableSO.TransitionItem { FromState = c, ToState = c, Conditions = condition } });
        HfsmProbeAction.Trace.Clear(); var legacy = Compile(table, true); legacy.OnStateEnter();
        for (int i = 0; i < 4; i++) { if (legacy.TryGetTransition(out var next)) { legacy.OnStateExit(); legacy = next; legacy.OnStateEnter(); } legacy.OnUpdate(); }
        var expected = HfsmProbeAction.Trace.ToArray(); HfsmProbeAction.Trace.Clear();
        var runtime = new HfsmRuntime(); runtime.Reset(Compile(table)); for (int i = 0; i < 4; i++) runtime.Tick(0);
        CollectionAssert.AreEqual(expected, HfsmProbeAction.Trace);
        Assert.AreEqual(1, expected.Count(t => t == "awake shared"), "Legacy per-graph sharing retained");
    }
    [Test] public void ExplicitEntryAndStablePriority_AreIndependentOfLayoutOrRowOrder()
    {
        var a = State("a", Temp<HfsmProbeActionSO>("A")); var b = State("b", Temp<HfsmProbeActionSO>("B")); var c = State("c", Temp<HfsmProbeActionSO>("C"));
        var table = Temp<TransitionTableSO>("fixture"); Set(table, "_initialState", b);
        Set(table, "_transitions", new[] { new TransitionTableSO.TransitionItem { FromState = a, ToState = a, Always = true }, new TransitionTableSO.TransitionItem { FromState = b, ToState = a, Always = true }, new TransitionTableSO.TransitionItem { FromState = b, ToState = c, Always = true, Priority = 5 } });
        var runtime = new HfsmRuntime(); runtime.Reset(Compile(table)); Assert.AreEqual("b", runtime.Current.Name); runtime.Tick(0); Assert.AreEqual("c", runtime.Current.Name);
    }
    [Test] public void NativeHierarchy_CompilesAndEntersItsInitialChild()
    {
        var a = State("parent", Temp<HfsmProbeActionSO>("parent")); var b = State("child", Temp<HfsmProbeActionSO>("child")); var table = Temp<TransitionTableSO>("fixture");
        Set(table, "_initialState", a); Set(table, "_hierarchy", new[] { new TransitionTableSO.StateHierarchyItem { State = a, InitialChild = b }, new TransitionTableSO.StateHierarchyItem { State = b, Parent = a } });
        var runtime = new HfsmRuntime(); runtime.Reset(Compile(table)); runtime.Enter(); Assert.AreEqual("child", runtime.Current.Name); Assert.AreEqual(2, runtime.ActivePath.Count);
    }
    [Test] public void CompilationCreatesSeparateRuntimeGraphsForTwoActors()
    {
        var a = State("state", Temp<HfsmProbeActionSO>("action")); var table = Temp<TransitionTableSO>("fixture"); Set(table, "_initialState", a);
        Assert.AreNotSame(Compile(table), Compile(table));
    }
}
