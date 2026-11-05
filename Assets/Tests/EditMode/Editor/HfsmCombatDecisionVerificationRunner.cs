using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>명시적으로 호출하는 읽기 전용 검사. 테스트용 Preview 씬만 생성하며 작업 씬/실제 작성 에셋은 저장하지 않습니다.</summary>
public static class HfsmCombatDecisionVerificationRunner
{
    public const string Report = "Library/HfsmRefactor-20260929/combat-decisions-verification-v2.txt";
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Run outside Play Mode and after compilation/import.");
        var lines = new List<string>(); int passed = 0, failed = 0;
        lines.Add("Loaded game assembly: " + typeof(EnemyShooterConfigSO).Assembly.Location);
        for (Type type = typeof(EnemyShooterConfigSO); type != null; type = type.BaseType)
            lines.Add("Loaded config type: " + type.FullName);
        var before = Files(); string scenes = Scenes();
        foreach (var type in new[] { typeof(HfsmCombatDecisionPolicyTests), typeof(HfsmCombatDecisionIntegrationTests),
            typeof(HfsmRuntimeTests), typeof(HfsmCompatibilityTests), typeof(HfsmSharedRuleTests) })
        foreach (var test in type.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            object fixture = Activator.CreateInstance(type);
            try
            {
                foreach (var setup in type.GetMethods().Where(m => m.IsDefined(typeof(SetUpAttribute)))) setup.Invoke(fixture, null);
                test.Invoke(fixture, null); lines.Add("PASS " + type.Name + "." + test.Name); passed++;
            }
            catch (Exception ex) { lines.Add("FAIL " + type.Name + "." + test.Name + ": " + (ex.InnerException ?? ex)); failed++; }
            finally
            {
                try { foreach (var cleanup in type.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute)))) cleanup.Invoke(fixture, null); }
                catch (Exception ex) { lines.Add("FAIL cleanup: " + ex); failed++; }
            }
        }
        var after = Files();
        if (before.Count == after.Count && before.All(p => after.TryGetValue(p.Key, out string hash) && hash == p.Value))
        { lines.Add("PASS all authored assets/prefabs/scenes/meta bytes preserved"); passed++; }
        else { lines.Add("FAIL authored files changed during verification"); failed++; }
        if (scenes == Scenes()) { lines.Add("PASS open scene identities/dirty flags preserved"); passed++; }
        else { lines.Add("FAIL open scene state changed"); failed++; }
        lines.Add($"{passed} passed / {failed} failed (Unity EditMode; not a NavMesh/animation/projectile PlayMode run)");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, lines);
        if (failed == 0) Debug.Log("[HFSM combat decisions] " + lines.Last() + " / " + Report);
        else Debug.LogError("[HFSM combat decisions] " + lines.Last() + " / " + Report);
    }
    private static Dictionary<string, string> Files()
    {
        using var hash = SHA256.Create();
        return Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".meta"))
            .ToDictionary(p => p, p => Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(p))));
    }
    private static string Scenes() => string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount)
        .Select(i => SceneManager.GetSceneAt(i)).Select(s => s.handle + ":" + s.path + ":" + s.isDirty + ":" + s.isLoaded));
}
