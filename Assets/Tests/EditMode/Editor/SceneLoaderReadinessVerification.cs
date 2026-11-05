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

/// <summary>Explicit one-shot preview-scene tests. Never enters Play Mode or saves user data.</summary>
[InitializeOnLoad]
public static class SceneLoaderReadinessVerification
{
    private const string Folder = "Library/SceneLoaderReadinessVerification";
    private const string Request = Folder + "/request.json";

    static SceneLoaderReadinessVerification()
    {
        if (File.Exists(Request)) EditorApplication.delayCall += MaybeRun;
    }

    private static void MaybeRun()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += MaybeRun; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            return;
        Run();
    }

    [MenuItem("Tools/Authoring/Verification/Run Scene Loading Readiness Tests")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Stop Play / Prefab Mode before running this verification.");
        Directory.CreateDirectory(Folder);
        if (File.Exists(Request)) File.Delete(Request);
        var results = new List<string>(); int passed = 0, failed = 0;
        var before = Hashes();
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
            .ToDictionary(s => s.handle, s => s.isDirty);
        var active = SceneManager.GetActiveScene(); var selection = Selection.activeObject;
        var save = SaveSystem.Active; var random = UnityEngine.Random.state;
        foreach (var test in typeof(SceneLoaderReadinessTests).GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            var fixture = new SceneLoaderReadinessTests();
            try
            {
                fixture.Setup(); test.Invoke(fixture, null);
                results.Add("PASS " + test.Name); passed++;
            }
            catch (Exception ex) { results.Add("FAIL " + test.Name + " " + (ex.InnerException ?? ex)); failed++; }
            finally
            {
                try { fixture.Cleanup(); }
                catch (Exception ex) { results.Add("FAIL cleanup: " + ex); failed++; }
            }
        }
        UnityEngine.Random.state = random;
        try
        {
            var after = Hashes();
            Assert.That(after.Keys, Is.EquivalentTo(before.Keys));
            foreach (var entry in before) Assert.AreEqual(entry.Value, after[entry.Key], entry.Key);
            Assert.AreEqual(scenes.Count, SceneManager.sceneCount);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
                Assert.AreEqual(scenes[scene.handle], scene.isDirty, scene.path);
            Assert.AreEqual(active, SceneManager.GetActiveScene());
            Assert.AreSame(selection, Selection.activeObject); Assert.AreSame(save, SaveSystem.Active);
            results.Add("PASS original assets/scenes/save bytes, scene setup, dirty state and selection unchanged"); passed++;
        }
        catch (Exception ex) { results.Add("FAIL preservation: " + ex); failed++; }
        results.Add($"PASSED={passed} FAILED={failed}");
        File.WriteAllLines(Folder + "/results.txt", results);
        Debug.Log($"[SceneLoader readiness verification] PASSED={passed} FAILED={failed}. Report: {Folder}/results.txt");
        if (Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 1);
    }

    private static Dictionary<string, string> Hashes()
    {
        using var sha = SHA256.Create();
        var paths = new List<string>();
        foreach (var root in new[] { "Assets/Scenes", "Assets/ScriptableObject/GameScene", "Assets/Prefabs/GameScene" })
            if (Directory.Exists(root)) paths.AddRange(Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".meta")));
        if (Directory.Exists(Application.persistentDataPath))
            paths.AddRange(Directory.GetFiles(Application.persistentDataPath, "*", SearchOption.TopDirectoryOnly)
                .Where(p => p.Contains(".es3") || p.Contains(".vaccine")));
        return paths.ToDictionary(p => p, p =>
        { using var stream = File.OpenRead(p); return Convert.ToBase64String(sha.ComputeHash(stream)); });
    }
}
