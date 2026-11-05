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
using UnityEngine.TestTools;

/// <summary>Opt-in native Unity regressions in preview scenes. Never enters Play Mode or saves authored data.</summary>
[InitializeOnLoad]
public static class SectorEnemySpawnerEncounterVerification
{
    private const string Folder = "Library/SectorEnemySpawnerEncounterVerification";
    private const string Request = Folder + "/request.json";
    private static double _nextCheck;
    private static bool _running;
    private static string _waiting;

    static SectorEnemySpawnerEncounterVerification()
    {
        EditorApplication.update -= MaybeRun;
        EditorApplication.update += MaybeRun;
    }

    private static void MaybeRun()
    {
        if (_running || EditorApplication.timeSinceStartup < _nextCheck) return;
        _nextCheck = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { Waiting("Waiting for Unity compilation / import."); return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Waiting("Waiting for Play Mode to end."); return; }
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
        { Waiting("Waiting for Prefab Mode to end: " + stage.assetPath); return; }
        // The earlier name verification captures native errors while its UI is open.
        // Run the expected-error cases only after that isolated verification finishes.
        if (Resources.FindObjectsOfTypeAll<SectorMakerWindow>()
            .Any(w => w.titleContent.text == "Sector Maker asset UI verification"))
        { Waiting("Waiting for Sector Maker UI verification to finish."); return; }
        Run();
    }

    private static void Waiting(string reason)
    {
        if (_waiting == reason) return;
        _waiting = reason; Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/waiting.txt", DateTime.Now.ToString("s") + " " + reason);
    }

    [MenuItem("Tools/Authoring/Verification/Run Sector Encounter Eligibility Tests")]
    public static void Run()
    {
        if (_running || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Run outside Play / Prefab Mode after compilation.");
        _running = true;
        Directory.CreateDirectory(Folder);
        if (File.Exists(Request)) File.Delete(Request);
        var lines = new List<string> { "Sector encounter eligibility verification (isolated preview scenes)",
            "INFO negative cases expect missing-preset errors; no production logs are suppressed." };
        int passed = 0, failed = 0;
        var before = Hashes();
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
            .ToDictionary(s => s.handle, s => s.isDirty);
        var active = SceneManager.GetActiveScene(); var selection = Selection.activeObject;
        var save = SaveSystem.Active; var random = UnityEngine.Random.state;
        try
        {
            // Give standard LogAssert.Expect tests a real isolated Unity log scope.
            // This package's LogScope is internal, so the manual entrypoint accesses it by reflection.
            // EvaluateLogScope(true) fails for unexpected errors AND missing expected errors.
            var logScopeType = typeof(LogAssert).Assembly.GetType("UnityEngine.TestTools.Logging.LogScope", true);
            foreach (var test in typeof(SectorEnemySpawnerEncounterTests).GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
            {
                using var scope = (IDisposable)Activator.CreateInstance(logScopeType, true);
                var fixture = new SectorEnemySpawnerEncounterTests();
                bool success = true;
                try { fixture.Setup(); test.Invoke(fixture, null); }
                catch (Exception ex) { success = false; lines.Add("FAIL " + test.Name + ": " + (ex.InnerException ?? ex)); failed++; }
                finally
                {
                    try { fixture.Cleanup(); }
                    catch (Exception ex) { success = false; lines.Add("FAIL cleanup " + test.Name + ": " + ex); failed++; }
                }
                try { logScopeType.GetMethod("EvaluateLogScope").Invoke(scope, new object[] { true }); }
                catch (Exception ex) { success = false; lines.Add("FAIL logs " + test.Name + ": " + (ex.InnerException ?? ex)); failed++; }
                if (success) { lines.Add("PASS " + test.Name); passed++; }
            }
            UnityEngine.Random.state = random;
            var after = Hashes();
            Assert.That(after.Keys, Is.EquivalentTo(before.Keys));
            foreach (var item in before) Assert.AreEqual(item.Value, after[item.Key], item.Key);
            Assert.AreEqual(scenes.Count, SceneManager.sceneCount);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
                Assert.AreEqual(scenes[scene.handle], scene.isDirty, scene.path);
            Assert.AreEqual(active, SceneManager.GetActiveScene());
            Assert.AreSame(selection, Selection.activeObject); Assert.AreSame(save, SaveSystem.Active);
            lines.Add("PASS original assets / prefab GUIDs / scene and save bytes / scene setup / dirty state / selection preserved"); passed++;
        }
        catch (Exception ex) { lines.Add("FAIL verification: " + ex); failed++; }
        finally
        {
            UnityEngine.Random.state = random;
            lines.Add($"PASSED={passed} FAILED={failed}");
            File.WriteAllLines(Folder + "/results.txt", lines);
            _running = false;
        }
        Debug.Log($"[Sector encounter eligibility verification] PASSED={passed} FAILED={failed}. Report: {Folder}/results.txt");
        if (Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 1);
    }

    private static Dictionary<string, string> Hashes()
    {
        using var sha = SHA256.Create();
        var paths = new List<string>();
        foreach (var root in new[] { "Assets/Scenes", "Assets/ScriptableObject/GameScene", "Assets/Prefabs/GameScene" })
            if (Directory.Exists(root)) paths.AddRange(Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".meta")));
        if (File.Exists("ProjectSettings/SectorMakerCatalog.asset")) paths.Add("ProjectSettings/SectorMakerCatalog.asset");
        if (Directory.Exists(Application.persistentDataPath))
            paths.AddRange(Directory.GetFiles(Application.persistentDataPath, "*", SearchOption.TopDirectoryOnly)
                .Where(p => p.Contains(".es3") || p.Contains(".vaccine")));
        return paths.ToDictionary(p => p, p =>
        { using var stream = File.OpenRead(p); return Convert.ToBase64String(sha.ComputeHash(stream)); });
    }
}
