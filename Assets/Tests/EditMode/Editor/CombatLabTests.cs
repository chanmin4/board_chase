using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.Debugging;
using VSplatter.StateMachine.ScriptableObjects;

public sealed class CombatLabTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<UnityEngine.Object> _objects = new();
    private T Keep<T>(T obj) where T : UnityEngine.Object { _objects.Add(obj); return obj; }
    private T So<T>(string name) where T : ScriptableObject { var value = Keep(ScriptableObject.CreateInstance<T>()); value.name = name; return value; }
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Flags).SetValue(obj, value);
    [TearDown] public void Cleanup()
    {
        HfsmExecutionTrace.Enabled = false; HfsmExecutionTrace.Clear(); CombatPhysicsTrace.Enabled = false; CombatPhysicsTrace.Clear();
        foreach (var obj in _objects) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        _objects.Clear();
    }
    [Test] public void TraceObservesLazyAndAndFirstSuccessfulRowWithoutExtraReads()
    {
        var go = Keep(new GameObject("trace only")); go.SetActive(false);
        var owner = go.AddComponent<StateMachine>();
        var a = So<StateSO>("Idle"); var b = So<StateSO>("Attack");
        Set(a, "_actions", Array.Empty<StateActionSO>()); Set(b, "_actions", Array.Empty<StateActionSO>());
        var no = So<HfsmUnifiedProbeConditionSO>("blocked"); no.Value = false;
        var skipped = So<HfsmUnifiedProbeConditionSO>("must not read"); skipped.Value = true;
        var yes = So<HfsmUnifiedProbeConditionSO>("valid shot"); yes.Value = true;
        TransitionTableSO.ConditionUsage Use(HfsmUnifiedProbeConditionSO guard) => new()
        { Condition = guard, ExpectedResult = TransitionTableSO.Result.True, Operator = TransitionTableSO.Operator.And };
        var table = So<TransitionTableSO>("fixture");
        var shared = So<SharedTransitionRuleSO>("shared fire rule");
        shared.InitializeForAuthoring(new TransitionTableSO.TransitionItem { ToState = b, Conditions = new[] { Use(yes) } });
        Set(table, "_transitions", new[]
        {
            new TransitionTableSO.TransitionItem { FromState = a, ToState = b, Conditions = new[] { Use(no), Use(skipped) } },
            new TransitionTableSO.TransitionItem { FromState = a, UseSharedRule = true, SharedRule = shared },
            new TransitionTableSO.TransitionItem { FromState = a, ToState = b, Conditions = new[] { Use(skipped) } }
        });
        HfsmExecutionTrace.Enabled = true;
        var state = (State)typeof(TransitionTableSO).GetMethod("GetInitialState", Flags).Invoke(table, new object[] { owner });
        Assert.IsTrue(state.TryGetTransition(out var target)); Assert.AreSame(b, target.Definition);
        Assert.AreEqual(0, skipped.Instances[0].Calls); Assert.AreEqual(1, no.Instances[0].Calls); Assert.AreEqual(1, yes.Instances[0].Calls);
        var checks = HfsmExecutionTrace.LatestChecks(owner);
        Assert.AreEqual(2, checks.Count); Assert.IsFalse(checks[0].Passed);
        Assert.AreEqual(false, checks[0].Checks[0].Actual); Assert.IsNull(checks[0].Checks[1].Actual);
        Assert.AreEqual("shared fire rule", checks[1].Rule); Assert.AreEqual(2, checks[1].Row);
        Assert.AreEqual("Idle", checks[1].From); Assert.AreEqual("Attack", checks[1].To);
        Assert.AreEqual(1, HfsmExecutionTrace.Transitions.Count);
        state.TryGetTransition(out _);
        Assert.AreEqual(2, no.Instances[0].Calls, "normal cache clear still occurs");
        HfsmExecutionTrace.Enabled = false;
        state.TryGetTransition(out _);
        Assert.AreEqual(2, HfsmExecutionTrace.Transitions.Count, "recording off adds no entries");
    }
    [Test] public void ColliderCapsuleHonorsCenterDirectionRotationAndNonUniformScale()
    {
        var go = Keep(new GameObject("capsule geometry"));
        go.transform.SetPositionAndRotation(new Vector3(2, 3, 4), Quaternion.Euler(0, 0, 90));
        go.transform.localScale = new Vector3(2, 3, 4);
        DebugMonitorWindow.CapsuleGeometry(go.transform, Vector3.up, 1, .5f, 4, out var a, out var b, out var radius);
        Assert.AreEqual(2, radius); Assert.Less(Vector3.Distance((a + b) * .5f, go.transform.TransformPoint(Vector3.up)), .0001f);
        Assert.AreEqual(8, Vector3.Distance(a, b), .0001f);
        DebugMonitorWindow.CapsuleGeometry(go.transform, Vector3.zero, 0, 1, 1, out a, out b, out radius);
        Assert.AreEqual(4, radius); Assert.AreEqual(a, b, "short capsules collapse to a sphere");
    }
    [Test] public void TraceReportsNativeHierarchySourceAndDestinationLevels()
    {
        var go = Keep(new GameObject("native hierarchy trace")); go.SetActive(false);
        var owner = go.AddComponent<StateMachine>();
        var root = So<StateSO>("Alive"); var leaf = So<StateSO>("Idle"); var dead = So<StateSO>("Dead");
        foreach (var definition in new[] { root, leaf, dead }) Set(definition, "_actions", Array.Empty<StateActionSO>());
        var table = So<TransitionTableSO>("native hierarchy");
        Set(table, "_initialState", leaf);
        Set(table, "_hierarchy", new[] { new TransitionTableSO.StateHierarchyItem { State = leaf, Parent = root } });
        Set(table, "_transitions", new[] { new TransitionTableSO.TransitionItem { FromState = leaf, ToState = dead, Always = true } });
        HfsmExecutionTrace.Enabled = true;
        var state = (State)typeof(TransitionTableSO).GetMethod("GetInitialState", Flags).Invoke(table, new object[] { owner });
        Assert.IsTrue(state.TryGetTransition(out _));
        var trace = HfsmExecutionTrace.Transitions.Single();
        Assert.AreEqual(2, trace.Level); Assert.AreEqual(1, trace.TargetLevel);
    }
    [Test] public void ColliderCategoriesAndPreviewExclusionDoNotChangeAuthoredObjects()
    {
        var go = Keep(new GameObject("temporary environment")); go.layer = LayerMask.NameToLayer("SectorFloor");
        var box = go.AddComponent<BoxCollider>();
        Assert.IsTrue(DebugMonitorWindow.IsSceneCollider(box)); Assert.AreEqual(2, DebugMonitorWindow.ColliderCategory(box));
        go.SetActive(false); go.AddComponent<PlayerCharacter>();
        Assert.AreEqual(0, DebugMonitorWindow.ColliderCategory(box));
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var preview = new GameObject("preview"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(preview, scene);
            Assert.IsFalse(DebugMonitorWindow.IsSceneCollider(preview.AddComponent<BoxCollider>()));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        Assert.IsFalse(DebugMonitorWindow.IsSceneCollider(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/GameScene/Shooter/Player/Player.prefab").GetComponent<Collider>()));
    }
    [Test] public void BulletSweepRingIsBoundedAndDoesNotIssuePhysicsQueries()
    {
        var go = Keep(new GameObject("sweep recorder")); go.SetActive(false); var bullet = go.AddComponent<BasicBullet>();
        CombatPhysicsTrace.Enabled = true;
        for (int i = 0; i < 550; i++) CombatPhysicsTrace.Record(bullet, go, Vector3.zero, Vector3.forward, .2f, 4, false, default, false);
        Assert.AreEqual(512, CombatPhysicsTrace.Recent.Count);
        Assert.AreEqual(Vector3.forward * 4, CombatPhysicsTrace.Recent[0].End);
        Assert.AreEqual(go.GetInstanceID(), CombatPhysicsTrace.Recent[0].ProjectileId);
        CombatPhysicsTrace.Enabled = false;
        CombatPhysicsTrace.Record(bullet, go, Vector3.zero, Vector3.up, 1, 1, true, default, true);
        Assert.AreEqual(512, CombatPhysicsTrace.Recent.Count);
    }
    [Test] public void RuntimeHarnessIsEditorOnlyAndLabPlanClonesDoNotLinkMutablePositions()
    {
        var type = typeof(CombatLabRuntime);
        Assert.IsNotNull(type.GetField("Spawns"));
        Assert.IsFalse(type.GetFields().Any(f => f.FieldType == typeof(SaveSystem)));
        string source = System.IO.File.ReadAllText("Assets/Scripts/Diagnostics/Runtime/CombatLabRuntime.cs");
        Assert.That(source, Does.StartWith("#if UNITY_EDITOR"));
        Assert.That(source, Does.Not.Contain("SaveAssets").And.Not.Contain("SaveScene").And.Not.Contain("MarkReady()"));
    }
    [Test] public void MonitorSnapshotDoesNotEvaluateAttackPredicate()
    {
        var go = Keep(new GameObject("read-only monitor snapshot")); go.SetActive(false);
        var controller = go.AddComponent<EnemyShooterCombatController>();
        Assert.AreEqual(-1, controller.ObservedCanAttackFrame);
        for (int i = 0; i < 10; i++) controller.GetDiagnosticsSnapshot();
        Assert.AreEqual(-1, controller.ObservedCanAttackFrame, "Monitor must not cause AI evaluation");
        Assert.IsFalse(controller.CanAttackTarget); Assert.GreaterOrEqual(controller.ObservedCanAttackFrame, 0);
        int frame = controller.ObservedCanAttackFrame;
        var snapshot = controller.GetDiagnosticsSnapshot();
        Assert.AreEqual(frame, controller.ObservedCanAttackFrame); Assert.IsFalse(snapshot.CanAttackTarget);
    }
}
