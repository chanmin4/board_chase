using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>명시적으로 실행하는 통합 검사. Preview 씬/임시 SO만 사용하며 실제 씬·프리팹·에셋을 저장하지 않습니다.</summary>
public static class HfsmUnifiedVerificationRunner
{
    public const string Report = "Library/HfsmUnified-20261002/unified-tests-v3.txt";
    public static bool Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Save first and run outside Play/Prefab Mode after compilation.");
        var before = Files(); string scenes = Scenes(); var lines = new List<string>(); int passed = 0, failed = 0;
        lines.Add("Loaded runtime assembly: " + typeof(VSplatter.StateMachine.HfsmStateMachine).Assembly.Location);
        foreach (var type in new[] { typeof(HfsmUnifiedHostTests), typeof(HfsmRuntimeTests), typeof(HfsmCompatibilityTests),
            typeof(HfsmDefinitionTests), typeof(HfsmSharedRuleTests), typeof(HfsmCombatDecisionPolicyTests),
            typeof(HfsmCombatDecisionIntegrationTests), typeof(HfsmGraphPresentationTests) })
        foreach (var test in type.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            if (test.Name == nameof(HfsmGraphPresentationTests.LongIdentifiersAndDescriptionsFitNarrowCardsWithoutFixedHeightClipping))
            { lines.Add("INFO GUI font/style measurement deferred until verification window has painted"); continue; }
            string testScenes = Scenes();
            var fixture = Activator.CreateInstance(type);
            try
            {
                foreach (var method in type.GetMethods().Where(m => m.IsDefined(typeof(SetUpAttribute)))) method.Invoke(fixture, null);
                test.Invoke(fixture, null); lines.Add("PASS " + type.Name + "." + test.Name); passed++;
            }
            catch (Exception ex) { lines.Add("FAIL " + type.Name + "." + test.Name + ": " + (ex.InnerException ?? ex)); failed++; }
            finally
            {
                try { foreach (var method in type.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute)))) method.Invoke(fixture, null); }
                catch (Exception ex) { lines.Add("FAIL cleanup: " + (ex.InnerException ?? ex)); failed++; }
            }
            if (testScenes != Scenes())
            {
                lines.Add("FAIL SCENE CHANGE during " + type.Name + "." + test.Name + "\nBEFORE " + testScenes + "\nAFTER " + Scenes());
                failed++; goto FinishChecks; // Do not continue more scene-based fixtures after a preservation failure.
            }
        }
        FinishChecks:
        var after = Files();
        if (before.Count == after.Count && before.All(p => after.TryGetValue(p.Key, out var h) && h == p.Value))
        { lines.Add("PASS authored asset/prefab/scene/meta bytes preserved during tests"); passed++; }
        else { lines.Add("FAIL authored files changed during tests"); failed++; }
        if (scenes == Scenes()) { lines.Add("PASS open scene membership, identities and dirty flags preserved"); passed++; }
        else { lines.Add("FAIL open scenes changed\nBEFORE " + scenes + "\nAFTER " + Scenes()); failed++; }
        lines.Add($"{passed} passed / {failed} failed. Unity EditMode lifecycle/asset checks; not a live NavMesh/projectile/animation PlayMode run.");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, lines);
        if (failed == 0) Debug.Log("[Unified HFSM] " + lines.Last() + " / " + Report);
        else Debug.LogError("[Unified HFSM] " + lines.Last() + " / " + Report);
        return failed == 0;
    }
    private static Dictionary<string, string> Files()
    {
        using var hash = SHA256.Create();
        return Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".meta"))
            .ToDictionary(p => p, p => Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(p))));
    }
    private static string Scenes() => string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
        .Select(s => s.handle + ":" + s.path + ":" + s.isDirty + ":" + s.isLoaded + ":" + string.Join(",", s.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.GetInstanceID()).OrderBy(id => id))));
}
