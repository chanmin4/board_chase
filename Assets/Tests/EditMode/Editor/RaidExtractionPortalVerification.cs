using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Read-only integration checks in isolated Preview Scenes; no real extraction/save/Play Mode.</summary>
public static class RaidExtractionPortalVerification
{
    private const string Output = "Library/RaidExtractionPortalVerification";
    private static double _nextCheck;
    [InitializeOnLoadMethod] private static void Register() => EditorApplication.update += Check;
    private static bool LatestTypesLoaded() => typeof(StageMapLayout).GetField("extractionPortalVersion") != null &&
        typeof(StageRoomNode).GetField("portalsActivated") != null &&
        typeof(RaidExtractionPortalTests).GetMethod("EveryAuthoredRoomInheritsExactlyOneDeviceAndItsReplaceableVisual") != null &&
        typeof(RaidRuntime).GetProperty("CanPersistCheckpoint", BindingFlags.Static | BindingFlags.NonPublic) != null &&
        typeof(RaidRuntime).GetMethod("TryExtractThroughPortal") != null &&
        typeof(RaidExtractionPortalTests).GetMethod("FiftySeedsUseNormalRoomsAndSparePortalsWithoutAddingRoomsOrStealingLinks")?.GetCustomAttribute<TestAttribute>() != null &&
        typeof(SectorHatchTests).GetMethod("ActualGeneratorTransactionConsumesBatteryAndOpensExistingHatchObjectsOnce")?.GetCustomAttribute<TestAttribute>() != null &&
        typeof(SectorHatch).GetMethod("ResolveVisual", BindingFlags.Instance | BindingFlags.NonPublic) == null;
    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < _nextCheck) return;
        _nextCheck = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Output + "/request")) return;
        string waiting = EditorApplication.isCompiling ? "Unity compilation is in progress." :
            EditorApplication.isUpdating ? "Unity asset import is in progress." :
            EditorApplication.isPlayingOrWillChangePlaymode ? "Waiting for Edit Mode; no real gameplay will be changed." :
            !LatestTypesLoaded() ? "Waiting for complete current types (Assets > Refresh, not Hot Reload only)." : null;
        if (waiting != null)
        {
            if (!File.Exists(Output + "/waiting.txt") || File.ReadAllText(Output + "/waiting.txt") != waiting)
                File.WriteAllText(Output + "/waiting.txt", waiting);
            return;
        }
        if (File.Exists(Output + "/waiting.txt")) File.Delete(Output + "/waiting.txt");
        File.Delete(Output + "/request"); Run();
    }
    [MenuItem("Tools/Verification/Raid extraction portals (read-only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !LatestTypesLoaded()) return;
        Directory.CreateDirectory(Output);
        var passes = new System.Collections.Generic.List<string>();
        try
        {
            string[] originals = new[] { "Assets/Prefabs/GameScene/Sector", "Assets/Scenes", "Assets/ScriptableObject/GameScene/Stage" }
                .SelectMany(path => Directory.GetFiles(path, "*", SearchOption.AllDirectories)).ToArray();
            string[] hashes = originals.Select(Hash).ToArray();
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            var dirty = scenes.Select(scene => scene.isDirty).ToArray(); var active = SceneManager.GetActiveScene();
            var save = SaveSystem.Active;
            string runBefore = save != null ? JsonUtility.ToJson(save.RunData) : null;
            string[] saveFiles = Directory.Exists(Application.persistentDataPath)
                ? Directory.GetFiles(Application.persistentDataPath, "*", SearchOption.AllDirectories) : Array.Empty<string>();
            string[] saveHashes = saveFiles.Select(Hash).ToArray();
            foreach (var method in typeof(RaidExtractionPortalTests).GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null))
            {
                var tests = new RaidExtractionPortalTests();
                try { tests.Setup(); method.Invoke(tests, null); passes.Add("PASS " + method.Name); }
                finally { tests.Cleanup(); }
            }
            foreach (var method in typeof(SectorHatchTests).GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null))
            {
                var tests = new SectorHatchTests();
                try { tests.Setup(); method.Invoke(tests, null); passes.Add("PASS " + method.Name); }
                finally { tests.Cleanup(); }
            }
            var foundation = new RaidMapFoundationTests();
            foundation.ContentWeightsDoNotChangeSpecialRoomPlacementOrBossRoll();
            foundation.StoredLayoutAndStateRoundTripWithoutReroll();
            foundation.SavedVersionOneMapKeepsItsOriginalCoordinatesAndState();
            passes.Add("PASS odd-square content streams, saved power/layout and V1 save compatibility");
            Assert.AreEqual(active, SceneManager.GetActiveScene());
            Assert.AreSame(save, SaveSystem.Active);
            if (save != null) Assert.AreEqual(runBefore, JsonUtility.ToJson(save.RunData));
            for (int i = 0; i < saveFiles.Length; i++) Assert.AreEqual(saveHashes[i], Hash(saveFiles[i]), "User save file unchanged.");
            CollectionAssert.AreEquivalent(saveFiles, Directory.Exists(Application.persistentDataPath)
                ? Directory.GetFiles(Application.persistentDataPath, "*", SearchOption.AllDirectories) : Array.Empty<string>());
            for (int i = 0; i < scenes.Length; i++) Assert.AreEqual(dirty[i], scenes[i].isDirty, scenes[i].name);
            for (int i = 0; i < originals.Length; i++) Assert.AreEqual(hashes[i], Hash(originals[i]), originals[i]);
            CollectionAssert.AreEquivalent(originals, new[] { "Assets/Prefabs/GameScene/Sector", "Assets/Scenes", "Assets/ScriptableObject/GameScene/Stage" }
                .SelectMany(path => Directory.GetFiles(path, "*", SearchOption.AllDirectories)).ToArray());
            passes.Add("PASS real save state/files, original prefab/scene/stage files, active scene and dirty flags preserved");
            File.WriteAllLines(Output + "/PASSED.txt", passes);
            if (File.Exists(Output + "/FAILED.txt")) File.Delete(Output + "/FAILED.txt");
            Debug.Log($"[Raid exits] {passes.Count} checks passed; 50 seeded maps, no real Home transition or save.");
        }
        catch (Exception error)
        {
            if (File.Exists(Output + "/PASSED.txt")) File.Delete(Output + "/PASSED.txt");
            File.WriteAllLines(Output + "/FAILED.txt", passes.Concat(new[] { error.ToString() }));
            Debug.LogException(error);
        }
    }
    private static string Hash(string path)
    { using var file = File.OpenRead(path); using var hash = SHA256.Create(); return Convert.ToBase64String(hash.ComputeHash(file)); }
}
