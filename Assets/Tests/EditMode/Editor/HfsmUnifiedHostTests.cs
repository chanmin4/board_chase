using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

// 모든 실행 검사는 Preview 씬과 메모리 SO만 사용합니다. 실제 프리팹을 실행/저장하지 않습니다.
public class HfsmUnifiedProbeActionSO : StateActionSO<HfsmUnifiedProbeAction>
{ [NonSerialized] public readonly List<HfsmUnifiedProbeAction> Instances = new(); }
public class HfsmUnifiedProbeAction : StateAction
{
    public static readonly List<string> Trace = new();
    public StateMachine OwnerFromAwake;
    public int Enters, Ticks, Exits;
    public override void Awake(StateMachine machine)
    { OwnerFromAwake = machine; ((HfsmUnifiedProbeActionSO)OriginSO).Instances.Add(this); Trace.Add("awake " + OriginSO.name); }
    public override void OnStateEnter() { Enters++; Trace.Add("enter " + OriginSO.name); }
    public override void OnUpdate() { Ticks++; Trace.Add("tick " + OriginSO.name); }
    public override void OnStateExit(StateExitReason reason) { Exits++; Trace.Add("exit " + OriginSO.name + " " + reason); }
}
public class HfsmUnifiedProbeConditionSO : StateConditionSO<HfsmUnifiedProbeCondition>
{ public bool Value; [NonSerialized] public readonly List<HfsmUnifiedProbeCondition> Instances = new(); }
public class HfsmUnifiedProbeCondition : Condition
{
    public int Calls;
    public override void Awake(StateMachine machine) => ((HfsmUnifiedProbeConditionSO)OriginSO).Instances.Add(this);
    protected override bool Statement() { Calls++; return ((HfsmUnifiedProbeConditionSO)OriginSO).Value; }
}

public sealed class HfsmUnifiedHostTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly List<UnityEngine.Object> _owned = new();
    private Scene _scene;
    private HfsmUnifiedProbeConditionSO _advance, _dead, _revive, _attack;
    private StateSO _spawn, _alive, _deadState, _idle, _attackState;
    private TransitionTableSO _root, _inner;
    private HfsmDefinitionSO _definition;
    private static void Set(object obj, string name, object value)
    {
        for (var type = obj.GetType(); type != null; type = type.BaseType)
        { var f = type.GetField(name, Flags | BindingFlags.DeclaredOnly); if (f != null) { f.SetValue(obj, value); return; } }
        throw new MissingFieldException(obj.GetType().Name, name);
    }
    private static void Call(object obj, string name) => obj.GetType().GetMethod(name, Flags).Invoke(obj, null);
    private static HfsmUnifiedProbeActionSO ActionOf(StateSO state)
        => (HfsmUnifiedProbeActionSO)((StateActionSO[])typeof(StateSO).GetField("_actions", Flags).GetValue(state))[0];
    private T Temp<T>(string name) where T : ScriptableObject
    { var obj = ScriptableObject.CreateInstance<T>(); obj.name = name; _owned.Add(obj); return obj; }
    private GameObject Object(string name) => ShooterCustomizeTestObjects.Create(name, _scene);
    private StateSO State(string name, HfsmUnifiedProbeActionSO shared = null)
    { var s = Temp<StateSO>(name); Set(s, "_actions", new StateActionSO[] { shared ?? Temp<HfsmUnifiedProbeActionSO>(name) }); return s; }
    private TransitionTableSO.TransitionItem Row(StateSO from, StateSO to, HfsmUnifiedProbeConditionSO guard)
        => new() { FromState = from, ToState = to, Conditions = new[] { new TransitionTableSO.ConditionUsage { Condition = guard, ExpectedResult = TransitionTableSO.Result.True } } };
    private void Bind(HfsmDefinitionSO.ReentryPolicy reentry = HfsmDefinitionSO.ReentryPolicy.ResumePrevious)
        => Set(_definition, "_scopes", new[] { new HfsmDefinitionSO.ScopeBinding { Label = "Alive behaviour", OwnerState = _alive, Table = _inner, Entry = _idle, Reentry = reentry } });
    private HfsmStateMachine Host()
    {
        var host = Object("temporary unified host").AddComponent<HfsmStateMachine>();
        Set(host, "_definition", _definition); Call(host, "Awake"); Call(host, "Start"); host.RefreshActiveScopes();
        Call(host, "StartInnerScopes"); return host;
    }
    private static void Frame(HfsmStateMachine host)
    { host.Runtime.Tick(0); host.RefreshActiveScopes(); host.TickInnerScopes(0); }
    [SetUp] public void Setup()
    {
        _scene = EditorSceneManager.NewPreviewScene();
        _advance = Temp<HfsmUnifiedProbeConditionSO>("advance"); _dead = Temp<HfsmUnifiedProbeConditionSO>("dead");
        _revive = Temp<HfsmUnifiedProbeConditionSO>("revive"); _attack = Temp<HfsmUnifiedProbeConditionSO>("attack");
        _spawn = State("Spawning"); _alive = State("Alive"); _deadState = State("Dead"); _idle = State("Idle"); _attackState = State("Attack");
        _root = Temp<TransitionTableSO>("Mode"); Set(_root, "_transitions", new[] { Row(_spawn, _alive, _advance), Row(_alive, _deadState, _dead), Row(_deadState, _alive, _revive) });
        _inner = Temp<TransitionTableSO>("Behaviour"); Set(_inner, "_transitions", new[] { Row(_idle, _attackState, _attack), Row(_attackState, _idle, _revive) });
        _definition = Temp<HfsmDefinitionSO>("fixture graph"); Set(_definition, "_rootTable", _root); Set(_definition, "_entry", _spawn); Bind();
        HfsmUnifiedProbeAction.Trace.Clear();
    }
    [TearDown] public void Cleanup()
    {
        if (_scene.IsValid())
        {
            // 수동 lifecycle 검사는 수동으로 등록한 editor 구독도 해제해야 합니다.
            foreach (var machine in _scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StateMachine>(true))) Call(machine, "OnDisable");
            EditorSceneManager.ClosePreviewScene(_scene);
        }
        foreach (var obj in _owned) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        _owned.Clear(); HfsmUnifiedProbeAction.Trace.Clear();
    }
    [Test] public void LegacyThreeComponentsAndUnifiedHost_HaveIdenticalLifecycleTrace()
    {
        var go = Object("legacy comparison"); var parent = go.AddComponent<ParentStateMachine>(); var child = go.AddComponent<ChildStateMachine>();
        Set(parent, "_transitionTableSO", _root); Set(child, "_transitionTableSO", _inner);
        parent.ResetToInitialState(false); child.ResetToInitialState(false); child.Runtime.Suspend(); parent.Runtime.Enter();
        bool childStarted = false;
        void OldFrame()
        {
            parent.Runtime.Tick(0);
            if (parent.CurrentStateSO == _alive)
            { child.Runtime.Resume(); if (!childStarted) { child.Runtime.Enter(); childStarted = true; } child.Runtime.Tick(0); }
            else child.Runtime.Suspend();
        }
        OldFrame(); _advance.Value = true; _attack.Value = true; OldFrame(); _dead.Value = true; OldFrame();
        _dead.Value = false; _revive.Value = true; OldFrame();
        var expected = HfsmUnifiedProbeAction.Trace.ToArray(); HfsmUnifiedProbeAction.Trace.Clear();
        _advance.Value = _attack.Value = _dead.Value = _revive.Value = false;
        var host = Host(); Frame(host); _advance.Value = true; _attack.Value = true; Frame(host); _dead.Value = true; Frame(host);
        _dead.Value = false; _revive.Value = true; Frame(host);
        CollectionAssert.AreEqual(expected, HfsmUnifiedProbeAction.Trace, "Root -> gate -> inner order, resume and action callbacks must match");
    }
    [Test] public void RootAndInnerCanTransitionOnceEachInTheSameFrame()
    {
        var host = Host(); _advance.Value = _attack.Value = true; Frame(host);
        Assert.AreSame(_alive, host.GetActiveState(1)); Assert.AreSame(_attackState, host.GetActiveState(2));
        Assert.AreEqual("Alive_1 > Attack_2", host.ActiveStatePath);
        Assert.AreSame(_attackState, host.CurrentLeafState);
    }
    [Test] public void DeathSuspendsInnerBeforeItsConditionsOrActionsRun_NoSyntheticAttackExit()
    {
        var host = Host(); _advance.Value = _attack.Value = true; Frame(host);
        var runtime = host.GetScope(1); HfsmUnifiedProbeAction.Trace.Clear(); _dead.Value = true; Frame(host);
        Assert.AreSame(_deadState, host.CurrentLeafState); Assert.IsFalse(runtime.IsScopeActive); Assert.IsTrue(runtime.Runtime.IsSuspended);
        Assert.IsFalse(HfsmUnifiedProbeAction.Trace.Any(s => s.Contains("Attack")), "Death must not tick/exit inner Attack");
    }
    [Test] public void ResumeKeepsPreviousInnerState_WithoutDuplicateEnter()
    {
        var host = Host(); _advance.Value = _attack.Value = true; Frame(host);
        var action = ActionOf(_attackState).Instances.Last();
        host.enabled = false; Call(host, "OnDisable"); Assert.IsFalse(host.GetScope(1).IsScopeActive);
        host.enabled = true; Call(host, "OnEnable"); host.RefreshActiveScopes(); host.TickInnerScopes(0);
        Assert.AreSame(_attackState, host.GetActiveState(2)); Assert.AreEqual(1, action.Enters); Assert.AreEqual(0, action.Exits);
    }
    [Test] public void RestartPolicyRecreatesInnerInstancesAndEntersInitialState()
    {
        Bind(HfsmDefinitionSO.ReentryPolicy.RestartFromEntry); var host = Host(); _advance.Value = _attack.Value = true; Frame(host);
        var oldAction = ActionOf(_attackState).Instances.Last();
        _dead.Value = true; Frame(host); _attack.Value = _dead.Value = false; _revive.Value = true; Frame(host);
        Assert.AreSame(_idle, host.GetActiveState(2)); Assert.AreEqual(0, oldAction.Exits, "Restart is the existing no-exit reset policy");
        Assert.Greater(ActionOf(_attackState).Instances.Count, 1);
    }
    [Test] public void ExplicitStopCleansDeepestScopeFirst_WithRequestedReason()
    {
        var host = Host(); _advance.Value = _attack.Value = true; Frame(host); HfsmUnifiedProbeAction.Trace.Clear();
        host.StopMachine(StateExitReason.Interrupted);
        CollectionAssert.AreEqual(new[] { "exit Attack Interrupted", "exit Alive Interrupted" }, HfsmUnifiedProbeAction.Trace);
        Assert.IsNull(host.CurrentLeafState); Assert.IsFalse(host.enabled);
    }
    [Test] public void ExplicitResetExitsInnerThenRoot_AndRecompilesBothGraphs()
    {
        var host = Host(); _advance.Value = _attack.Value = true; Frame(host); HfsmUnifiedProbeAction.Trace.Clear();
        host.ResetToInitialState(true, true);
        Assert.AreEqual("exit Attack Reset", HfsmUnifiedProbeAction.Trace[0]); Assert.AreEqual("exit Alive Reset", HfsmUnifiedProbeAction.Trace[1]);
        Assert.AreSame(_spawn, host.GetActiveState(1)); Assert.IsFalse(host.GetScope(1).IsScopeActive);
        _dead.Value = false; _attack.Value = false; Frame(host); Assert.AreSame(_idle, host.GetActiveState(2));
    }
    [Test] public void SharedSOInstancesRemainSeparateForEachScopeAndEachCharacter()
    {
        var shared = Temp<HfsmUnifiedProbeActionSO>("shared action"); Set(_spawn, "_actions", new StateActionSO[] { shared }); Set(_idle, "_actions", new StateActionSO[] { shared });
        var first = Host(); var second = Host(); Assert.AreEqual(4, shared.Instances.Count);
        Assert.AreSame(first, shared.Instances[0].OwnerFromAwake); Assert.AreSame(first, shared.Instances[1].OwnerFromAwake);
        Assert.AreNotSame(shared.Instances[0].Context.Scope, shared.Instances[1].Context.Scope);
        Assert.AreNotSame(shared.Instances[0], shared.Instances[2]);
        Assert.AreEqual(1, shared.Instances[0].Context.Scope.Level); Assert.AreEqual(2, shared.Instances[1].Context.Scope.Level);
        Assert.AreSame(second, shared.Instances[2].OwnerFromAwake);
    }
    [Test] public void InnerContextReadsInnerState_NotRootState()
    {
        var host = Host(); _advance.Value = true; Frame(host);
        var action = ActionOf(_idle).Instances.Last();
        Assert.AreSame(_idle, action.Context.CurrentState); Assert.AreSame(_alive, action.Context.Owner.CurrentStateSO);
        Assert.AreEqual(host.GetScope(1).CurrentStateElapsedSeconds, action.Context.ElapsedSeconds);
        Assert.AreSame(host.transform, action.Context.GetComponent<Transform>());
    }
    [Test] public void InactiveInnerDoesNotEvaluateConditions_AndEachScopeHasItsOwnConditionCache()
    {
        // Same condition SO appears in both original graphs, yet must retain two runtime caches.
        Set(_inner, "_transitions", new[] { Row(_idle, _attackState, _advance) });
        var host = Host(); Frame(host); Assert.AreEqual(2, _advance.Instances.Count);
        Assert.AreEqual(1, _advance.Instances[0].Calls); Assert.AreEqual(0, _advance.Instances[1].Calls);
        _advance.Value = true; Frame(host); Assert.AreEqual(2, _advance.Instances[0].Calls); Assert.AreEqual(1, _advance.Instances[1].Calls);
    }
    [Test] public void ThirdLevelIsExplicit_AndAncestorDisablePausesDescendants()
    {
        var leaf = State("Leaf"); var third = Temp<TransitionTableSO>("third"); Set(third, "_initialState", leaf);
        Set(_definition, "_scopes", new[] {
            new HfsmDefinitionSO.ScopeBinding { OwnerState = _alive, Table = _inner },
            new HfsmDefinitionSO.ScopeBinding { OwnerScope = 1, OwnerState = _attackState, Table = third } });
        var host = Host(); _advance.Value = _attack.Value = true; Frame(host);
        Assert.IsFalse(host.GetScope(2).IsScopeActive, "Gate phase reads the previous inner state, as the old coordinator did");
        Frame(host); Assert.AreSame(leaf, host.GetActiveState(3)); Assert.AreEqual("Alive_1 > Attack_2 > Leaf_3", host.ActiveStatePath);
        _dead.Value = true; Frame(host); Assert.IsFalse(host.GetScope(1).IsScopeActive); Assert.IsFalse(host.GetScope(2).IsScopeActive);
    }
    [Test] public void NativeTreeAndBoundScopesUseActualDepthInExecutionPath()
    {
        var native = State("Native"); Set(_root, "_initialState", _alive); Set(_definition, "_entry", _alive);
        Set(_root, "_hierarchy", new[] { new TransitionTableSO.StateHierarchyItem { State = _alive, InitialChild = native }, new TransitionTableSO.StateHierarchyItem { State = native, Parent = _alive } });
        Set(_definition, "_scopes", new[] { new HfsmDefinitionSO.ScopeBinding { OwnerState = native, Table = _inner } });
        var host = Host(); Assert.AreEqual(3, host.GetScope(1).Level); Assert.AreSame(native, host.GetActiveState(2));
        Assert.AreEqual("Alive_1 > Native_2 > Idle_3", host.ActiveStatePath);
    }
    [Test] public void DefinitionRejectsCyclicOrMissingOwnerAndForeignEntry()
    {
        Set(_definition, "_scopes", new[] { new HfsmDefinitionSO.ScopeBinding { OwnerScope = 1, OwnerState = _alive, Table = _inner } });
        Assert.IsTrue(_definition.Validate().Any(e => e.Contains("순환")));
        Bind(); Set(_definition, "_entry", _attackState); Assert.IsTrue(_definition.Validate().Any());
    }
    [Test] public void PhaseSlotsMatchLegacyExecutionOrders()
    {
        int Order(Type type) => type.GetCustomAttribute<DefaultExecutionOrder>().order;
        Assert.AreEqual(Order(typeof(ParentStateMachine)), Order(typeof(HfsmStateMachine)));
        Assert.AreEqual(Order(typeof(StateMachineCoordinator)), Order(typeof(HfsmGatePhase)));
        Assert.AreEqual(Order(typeof(ChildStateMachine)), Order(typeof(HfsmBehaviourPhase)));
    }
    [Test] public void ShippedPlayerAndEnemyVariantsUseOneHost_WithoutMissingScripts()
    {
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/GameScene/Shooter" }).Select(AssetDatabase.GUIDToAssetPath);
        int enemies = 0, players = 0;
        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); var host = prefab.GetComponent<HfsmStateMachine>(); if (host == null) continue;
            Assert.AreEqual(1, prefab.GetComponents<StateMachine>().Length, path);
            Assert.IsNull(prefab.GetComponent<StateMachineCoordinator>(), path); Assert.IsNotNull(host.Definition, path);
            Assert.IsEmpty(host.Definition.Validate(), path);
            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab), path);
            if (host.Definition.name == "EnemyShooter_HFSM") enemies++; if (host.Definition.name == "Player_HFSM") players++;
        }
        Assert.GreaterOrEqual(enemies, 10, "Enemy Base plus its mob/boss variants"); Assert.GreaterOrEqual(players, 1);
    }
    [Test] public void GraphProjectsDefinitionWithoutCoordinator_ForPrefabAndStandaloneSO()
    {
        var host = Host();
        object Build(GameObject owner, StateMachine machine)
        { var m = Activator.CreateInstance(HfsmGraphPresentationTests.EditorType("HfsmGraphModel")); HfsmGraphPresentationTests.Call(m, "BuildDefinition", _root, machine, owner, _definition); return HfsmGraphPresentationTests.Get(m, "Root"); }
        foreach (var root in new[] { Build(host.gameObject, host), Build(null, null) })
        {
            var nodes = HfsmGraphPresentationTests.Items(HfsmGraphPresentationTests.Static("HfsmGraphModel", "AllNodes", root));
            Assert.AreEqual(5, nodes.Length);
            Assert.AreEqual(2, (int)HfsmGraphPresentationTests.Get(nodes.Single(n => (StateSO)HfsmGraphPresentationTests.Get(n, "State") == _idle), "Level"));
        }
    }
}
