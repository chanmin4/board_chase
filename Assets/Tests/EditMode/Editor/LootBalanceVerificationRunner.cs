using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class LootBalanceVerificationRunner
{
    public const string Report = "Library/LootBalanceVerification/unity-tests.txt";

    [MenuItem("Tools/Authoring/Verification/Run Repair and Stage Loot Tests")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var lines = new List<string>(); int passed = 0, failed = 0;
        var scene = SceneManager.GetActiveScene(); bool dirty = scene.isDirty; int roots = scene.rootCount;
        var paths = new[] { "Assets/ScriptableObject/GameScene/GameItem", "Assets/ScriptableObject/GameScene/Stage", "Assets/Prefabs/GameScene" };
        var snapshots = paths.SelectMany(p => Directory.GetFiles(p, "*", SearchOption.AllDirectories))
            .Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab")).ToDictionary(p => p, File.ReadAllText);
        // These old regressions use only transient ScriptableObjects. Skip the disk/Undo integration cases here.
        var skipped = new[] { "AuthoringToggleAndWeightSupportUndoWithoutChangingItems", "CategorySettingsPersistThroughAssetSaveAndReload" };
        foreach (var type in new[] { typeof(LootBalanceTests), typeof(LootCategorySelectionTests) })
        foreach (var method in type.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute)) && !skipped.Contains(m.Name)))
        {
            object fixture = Activator.CreateInstance(type);
            try
            {
                foreach (var setup in type.GetMethods().Where(m => m.IsDefined(typeof(SetUpAttribute)))) setup.Invoke(fixture, null);
                method.Invoke(fixture, null); passed++; lines.Add("PASS " + type.Name + "." + method.Name);
            }
            catch (Exception ex) { failed++; lines.Add("FAIL " + type.Name + "." + method.Name + ": " + (ex.InnerException ?? ex)); }
            finally
            {
                try { foreach (var cleanup in type.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute)))) cleanup.Invoke(fixture, null); }
                catch (Exception ex) { failed++; lines.Add("FAIL Cleanup: " + ex); }
            }
        }
        try { AuditCoverage(lines); }
        catch (Exception ex) { failed++; lines.Add("FAIL Coverage audit: " + ex); }
        if (snapshots.All(p => File.Exists(p.Key) && File.ReadAllText(p.Key) == p.Value)) { passed++; lines.Add("PASS Real asset and prefab files unchanged"); }
        else { failed++; lines.Add("FAIL Real files changed during tests"); }
        if (SceneManager.GetActiveScene() == scene && scene.isDirty == dirty && scene.rootCount == roots) { passed++; lines.Add("PASS User scene unchanged"); }
        else { failed++; lines.Add("FAIL User scene state changed"); }
        lines.Add($"{passed} passed / {failed} failed");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, lines);
        Debug.Log("[Repair / stage loot verification] " + lines.Last() + " " + Report);
    }

    private static void AuditCoverage(List<string> lines)
    {
        int boxes = 0, incomplete = 0, unavailable = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:LootObjectSO"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var source = AssetDatabase.LoadAssetAtPath<LootObjectSO>(path);
            if (source == null || source.LootTable == null) continue;
            // Pool normalization is performed only on detached copies, never on the user's working data.
            var table = Object.Instantiate(source.LootTable);
            try
            {
                boxes++;
                var baseline = table.GetStageDistribution(source.RarityWeights, 0);
                var families = baseline.Rows.Where(r => r.Family != null).GroupBy(r => r.Family).ToArray();
                for (int stage = 1; stage <= 5; stage++)
                {
                    var d = table.GetStageDistribution(source.RarityWeights, stage);
                    if (source.MaxGeneratedSlots > 0 && !d.HasCandidates)
                    { unavailable++; lines.Add($"DATA No eligible loot: {source.name}, Stage {stage}"); }
                    foreach (var family in families)
                    {
                        var missing = Enumerable.Range(stage, 4).Except(family.Select(r => r.Level)).ToArray();
                        if (missing.Length == 0) continue;
                        incomplete++;
                        lines.Add($"DATA Partial LV candidates: {source.name}, Stage {stage}, {family.Key}, missing {string.Join(",", missing)}");
                    }
                }
            }
            finally { Object.DestroyImmediate(table); }
        }
        lines.Add($"COVERAGE {boxes} authored boxes; {incomplete} family/stage combinations renormalize missing LVs; {unavailable} box/stage combinations have no eligible loot. Data was NOT edited.");
    }
}
