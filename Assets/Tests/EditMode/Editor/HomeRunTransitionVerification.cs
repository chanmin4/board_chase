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

public static class HomeRunTransitionVerification
{
    private const string Output = "Library/HomeRunTransitionVerification";
    private static double _nextCheck;
    [InitializeOnLoadMethod] private static void Register() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < _nextCheck) return;
        _nextCheck = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Output + "/request") || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (typeof(SceneLoader).GetMethod("RequestHomeTransition") == null ||
            typeof(HomeRunTransitionTests).GetMethod("DeploymentMovesSamePlayerIntoRunAndSurvivesHomeUnload")?.GetCustomAttributes<TestCaseAttribute>().Any() != true) return;
        File.Delete(Output + "/request"); Run();
    }
    [MenuItem("Tools/Verification/Home RUN ownership and camera (read-only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Output); var passes = new List<string>();
        try
        {
            var save = SaveSystem.Active; string before = save != null ? JsonUtility.ToJson(save.RunData) : null;
            var active = SceneManager.GetActiveScene();
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            var dirty = scenes.Select(scene => scene.isDirty).ToArray();
            var files = new[] { "Assets/Scenes/HomeScene.unity", "Assets/Scenes/RUNScene.unity", "Assets/Prefabs/GameScene/Shooter/Player/Player.prefab" }
                .Concat(Directory.Exists(Application.persistentDataPath) ? Directory.GetFiles(Application.persistentDataPath, "*", SearchOption.AllDirectories) : Array.Empty<string>()).ToArray();
            var hashes = files.Select(Hash).ToArray();
            foreach (var type in new[] { typeof(HomeRunTransitionTests), typeof(SceneLoaderReadinessTests) })
                foreach (var method in type.GetMethods())
                {
                    var cases = method.GetCustomAttributes<TestCaseAttribute>().ToArray();
                    if (method.GetCustomAttribute<TestAttribute>() == null && cases.Length == 0) continue;
                    var arguments = cases.Length > 0 ? cases.Select(test => test.Arguments) : new[] { Array.Empty<object>() };
                    foreach (var args in arguments)
                    {
                        var tests = Activator.CreateInstance(type);
                        try { type.GetMethod("Setup").Invoke(tests, null); method.Invoke(tests, args); passes.Add("PASS " + method.Name + "(" + string.Join(",", args) + ")"); }
                        finally { type.GetMethod("Cleanup").Invoke(tests, null); }
                    }
                }
            Assert.AreSame(save, SaveSystem.Active); if (save != null) Assert.AreEqual(before, JsonUtility.ToJson(save.RunData));
            Assert.AreEqual(active, SceneManager.GetActiveScene()); Assert.AreEqual(scenes.Length, SceneManager.sceneCount);
            for (int i = 0; i < scenes.Length; i++) Assert.AreEqual(dirty[i], scenes[i].isDirty);
            for (int i = 0; i < files.Length; i++) Assert.AreEqual(hashes[i], Hash(files[i]), files[i]);
            passes.Add("PASS authored Home/RUN scenes, Player prefab, real save files/state and scene dirty flags preserved");
            File.WriteAllLines(Output + "/PASSED.txt", passes);
            if (File.Exists(Output + "/FAILED.txt")) File.Delete(Output + "/FAILED.txt");
            Debug.Log($"[Home RUN] {passes.Count} isolated ownership/camera/readiness checks passed; no real scene load or save.");
        }
        catch (Exception error)
        {
            File.WriteAllLines(Output + "/FAILED.txt", passes.Concat(new[] { error.ToString() }));
            if (File.Exists(Output + "/PASSED.txt")) File.Delete(Output + "/PASSED.txt");
            Debug.LogException(error);
        }
    }
    private static string Hash(string path)
    { using var file = File.OpenRead(path); using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(file)); }
}
