using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit, temporary Preview Scene checks in the user's open Editor. Never enters Play Mode or saves assets/scenes.</summary>
public static class SectorHatchVerification
{
    private const string Output = "Library/SectorHatchVerification";
    private static double _nextCheck;
    [InitializeOnLoadMethod] private static void Register() => EditorApplication.update += CheckRequest;
    private static void CheckRequest()
    {
        if (EditorApplication.timeSinceStartup < _nextCheck) return;
        _nextCheck = EditorApplication.timeSinceStartup + 1;
        string request = Output + "/request-object-toggle";
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || !LatestTypesLoaded()) return;
        File.Delete(request); Run();
    }
    [MenuItem("Tools/Verification/Sector Hatch (read-only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!LatestTypesLoaded())
        {
            Debug.LogWarning("[SectorHatch] Assets > Refresh is required to import the complete authored-hatch types before verification. Hot Reload alone is not sufficient.");
            return;
        }
        Directory.CreateDirectory(Output);
        try
        {
            string[] originals = new[] { "Assets/Prefabs/GameScene/Sector", "Assets/Scenes", "Assets/ScriptableObject/GameScene/Stage" }
                .Where(Directory.Exists).SelectMany(p => Directory.GetFiles(p, "*", SearchOption.AllDirectories)).ToArray();
            string[] hashes = originals.Select(Hash).ToArray();
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            var dirty = scenes.Select(s => s.isDirty).ToArray(); Scene active = SceneManager.GetActiveScene();
            var passes = new System.Collections.Generic.List<string>();
            foreach (var method in typeof(SectorHatchTests).GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null))
            {
                var tests = new SectorHatchTests();
                try { tests.Setup(); method.Invoke(tests, null); passes.Add("PASS " + method.Name); }
                finally { tests.Cleanup(); }
            }
            Assert.Contains("PASS ProjectObjectsAreNotInstantiatedOrModified", passes,
                "The object-only test must run; a partially Hot Reload patched assembly is not a final verification.");
            Assert.Contains("PASS AlreadyPlacedPrefabReferencesAreReusedWithoutSpawningDuplicates", passes);
            Assert.AreEqual(active, SceneManager.GetActiveScene(), "User active scene is unchanged.");
            for (int i = 0; i < scenes.Length; i++) Assert.AreEqual(dirty[i], scenes[i].isDirty, "User scene dirty state: " + scenes[i].name);
            for (int i = 0; i < originals.Length; i++) Assert.AreEqual(hashes[i], Hash(originals[i]), "Unrequested asset write: " + originals[i]);
            passes.Add("PASS original prefab/scene/stage files and active scene/dirty flags preserved");
            File.WriteAllLines(Output + "/object-toggle-PASSED.txt", passes);
            if (File.Exists(Output + "/failure.txt")) File.Delete(Output + "/failure.txt");
            Debug.Log("[SectorHatch] " + passes.Count + " checks passed; no Play Mode, asset writes or scene saves.");
        }
        catch (Exception error)
        {
            if (File.Exists(Output + "/object-toggle-PASSED.txt")) File.Delete(Output + "/object-toggle-PASSED.txt");
            File.WriteAllText(Output + "/failure.txt", error.ToString()); Debug.LogException(error);
        }
    }
    private static string Hash(string path)
    { using var file = File.OpenRead(path); using var hash = SHA256.Create(); return Convert.ToBase64String(hash.ComputeHash(file)); }

    // Hot Reload can patch methods while retaining the old assembly's reflection metadata.
    // Do not report success from a partially patched test/type set.
    private static bool LatestTypesLoaded() =>
        typeof(SectorHatch).GetMethod("ValidateVisuals") != null &&
        typeof(SectorHatch).GetMethod("ValidateInteractionTrigger") != null &&
        typeof(SectorHatch).GetMethod("ResolveVisual", BindingFlags.Instance | BindingFlags.NonPublic) == null &&
        typeof(SectorHatch).GetField("_closedInstance", BindingFlags.Instance | BindingFlags.NonPublic) == null &&
        typeof(SectorHatchTests).GetMethod("ProjectPrefabsCreateOnceAndToggleOnlyInstances") == null &&
        typeof(SectorHatchTests).GetMethod("ProjectObjectsAreNotInstantiatedOrModified")?.GetCustomAttribute<TestAttribute>() != null;
}
