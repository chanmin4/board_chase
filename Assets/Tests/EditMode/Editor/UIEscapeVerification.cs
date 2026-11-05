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

/// <summary>Native Unity checks of isolated UI components; leaves authoring, assets and real saves untouched.</summary>
public static class UIEscapeVerification
{
    private const string Output = "Library/UIEscapeVerification";
    private static double _nextCheck;
    [InitializeOnLoadMethod] private static void Register() => EditorApplication.update += Check;
    private static bool LatestTypesLoaded() => typeof(EscPauseMenuController).GetMethod("HandleEscapePressed") != null &&
        typeof(UIEscapeTests).GetMethod("EscapeClosesOnlyPopupAndDoesNotPauseOrChangeGameState")?.GetCustomAttributes<TestCaseAttribute>().Any() == true;
    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < _nextCheck) return;
        _nextCheck = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Output + "/request")) return;
        string waiting = EditorApplication.isCompiling ? "Waiting for Unity compilation." :
            EditorApplication.isUpdating ? "Waiting for asset import." :
            EditorApplication.isPlayingOrWillChangePlaymode ? "Waiting for Edit Mode; does not touch ongoing gameplay." :
            !LatestTypesLoaded() ? "Waiting for complete types from Assets > Refresh." : null;
        if (waiting != null)
        {
            if (!File.Exists(Output + "/waiting.txt") || File.ReadAllText(Output + "/waiting.txt") != waiting)
                File.WriteAllText(Output + "/waiting.txt", waiting);
            return;
        }
        if (File.Exists(Output + "/waiting.txt")) File.Delete(Output + "/waiting.txt");
        File.Delete(Output + "/request"); Run();
    }
    [MenuItem("Tools/Verification/Popup Escape (read-only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !LatestTypesLoaded()) return;
        Directory.CreateDirectory(Output);
        var passes = new List<string>();
        try
        {
            string[] paths = { "Assets/Prefabs/UI", "Assets/Scenes" };
            var originals = paths.SelectMany(path => Directory.GetFiles(path, "*", SearchOption.AllDirectories)).ToArray();
            var hashes = originals.Select(Hash).ToArray();
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            var dirty = scenes.Select(scene => scene.isDirty).ToArray(); var active = SceneManager.GetActiveScene();
            var save = SaveSystem.Active;
            string runBefore = save != null ? JsonUtility.ToJson(save.RunData) : null;
            var saveFiles = SaveFiles(); var saveHashes = saveFiles.Select(Hash).ToArray();
            bool paused = GamePause.IsPaused; float timeScale = Time.timeScale;
            bool blocked = GameplayActionInputBlocker.IsCombatInputBlocked;
            foreach (var method in typeof(UIEscapeTests).GetMethods())
            {
                var cases = method.GetCustomAttributes<TestCaseAttribute>().ToArray();
                if (method.GetCustomAttribute<TestAttribute>() == null && cases.Length == 0) continue;
                var arguments = cases.Length > 0 ? cases.Select(test => test.Arguments).ToArray() : new[] { Array.Empty<object>() };
                foreach (var args in arguments)
                {
                    var tests = new UIEscapeTests();
                    try { tests.Setup(); method.Invoke(tests, args); passes.Add("PASS " + method.Name + "(" + string.Join(",", args) + ")"); }
                    finally { tests.Cleanup(); }
                }
            }
            Assert.AreEqual(active, SceneManager.GetActiveScene());
            Assert.AreEqual(scenes.Length, SceneManager.sceneCount);
            Assert.AreSame(save, SaveSystem.Active);
            if (save != null) Assert.AreEqual(runBefore, JsonUtility.ToJson(save.RunData));
            Assert.AreEqual(paused, GamePause.IsPaused); Assert.AreEqual(timeScale, Time.timeScale);
            Assert.AreEqual(blocked, GameplayActionInputBlocker.IsCombatInputBlocked);
            CollectionAssert.AreEquivalent(saveFiles, SaveFiles());
            for (int i = 0; i < saveFiles.Length; i++) Assert.AreEqual(saveHashes[i], Hash(saveFiles[i]), saveFiles[i]);
            for (int i = 0; i < scenes.Length; i++) Assert.AreEqual(dirty[i], scenes[i].isDirty, scenes[i].name);
            for (int i = 0; i < originals.Length; i++) Assert.AreEqual(hashes[i], Hash(originals[i]), originals[i]);
            CollectionAssert.AreEquivalent(originals, paths.SelectMany(path => Directory.GetFiles(path, "*", SearchOption.AllDirectories)).ToArray());
            passes.Add("PASS real UI prefabs/scenes/saves, active scene, dirty flags, pause/time and combat blocker preserved");
            File.WriteAllLines(Output + "/PASSED.txt", passes);
            if (File.Exists(Output + "/FAILED.txt")) File.Delete(Output + "/FAILED.txt");
            Debug.Log($"[Popup Escape] {passes.Count} checks passed; isolated UI only, no real gameplay or save changes.");
        }
        catch (Exception error)
        {
            if (File.Exists(Output + "/PASSED.txt")) File.Delete(Output + "/PASSED.txt");
            File.WriteAllLines(Output + "/FAILED.txt", passes.Concat(new[] { error.ToString() }));
            Debug.LogException(error);
        }
    }
    private static string[] SaveFiles() => Directory.Exists(Application.persistentDataPath)
        ? Directory.GetFiles(Application.persistentDataPath, "*", SearchOption.AllDirectories) : Array.Empty<string>();
    private static string Hash(string path)
    { using var file = File.OpenRead(path); using var hash = SHA256.Create(); return Convert.ToBase64String(hash.ComputeHash(file)); }
}
