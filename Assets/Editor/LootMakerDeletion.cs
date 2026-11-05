using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Explicit deletion of one catalog record, never its prefab, table, or family.</summary>
public static class LootMakerDeletion
{
    public sealed class Usage
    {
        public readonly List<string> References = new();
        public readonly List<string> Blockers = new();
        internal readonly List<Binding> Bindings = new();
    }

    internal sealed class Binding
    {
        internal Object Owner;
        internal string Property, AssetPath;
    }

    [Serializable] private sealed class BackupEntry { public string assetPath, backupFile; }
    [Serializable] private sealed class BackupManifest
    {
        public string deletedRecord, replacementRecord;
        public List<BackupEntry> files = new();
    }

    private static string RecordPath(LootObjectSO box)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Exit Play Mode and wait for compilation before deleting.");
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Save and close Prefab Mode before deleting a loot record.");
        string path = AssetDatabase.GetAssetPath(box);
        if (box == null || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
            !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) || !AssetDatabase.IsMainAsset(box))
            throw new InvalidOperationException("Select a saved LootObjectSO asset under Assets.");
        if (!AssetDatabase.IsOpenForEdit(path)) throw new InvalidOperationException("Read-only record: " + path);
        return path;
    }

    public static Usage FindUsage(LootObjectSO box, bool showProgress = false)
    {
        string sourcePath = RecordPath(box), guid = AssetDatabase.AssetPathToGUID(sourcePath);
        if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("The selected record has no GUID.");
        var result = new Usage();
        var inspected = new HashSet<int>();
        string[] paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
            p != sourcePath && IsSerializedAsset(p)).ToArray();
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (showProgress && i % 100 == 0 && EditorUtility.DisplayCancelableProgressBar(
                    "Loot Maker - Check uses before deletion", path, (float)i / Math.Max(1, paths.Length)))
                    throw new OperationCanceledException("Reference check cancelled. Nothing was changed.");
                if (!HasSavedReference(path, sourcePath, guid)) continue;
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    result.Blockers.Add("Scene reference: " + path + ". Reassign it and save the scene first.");
                    continue; // Never open/save a user's scene as a side effect of deletion.
                }
                int before = result.Bindings.Count;
                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (root != null)
                        foreach (var component in root.GetComponentsInChildren<Component>(true))
                            Inspect(component, path, box, result, inspected);
                }
                else
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) Inspect(asset, path, box, result, inspected);
                if (result.Bindings.Count == before)
                    result.Blockers.Add("Unresolved or GUID/string reference: " + path + ". Reassign it manually first.");
            }

            // Include unsaved loaded assets, which the dependency database cannot see.
            foreach (var asset in Resources.FindObjectsOfTypeAll<ScriptableObject>())
                if (asset != box && EditorUtility.IsPersistent(asset))
                    Inspect(asset, AssetDatabase.GetAssetPath(asset), box, result, inspected);
            foreach (var component in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (component == null) continue;
                if (EditorUtility.IsPersistent(component))
                    Inspect(component, AssetDatabase.GetAssetPath(component), box, result, inspected);
                else if (component.gameObject.scene.IsValid() && !EditorSceneManager.IsPreviewScene(component.gameObject.scene))
                {
                    if (References(component, box))
                        result.Blockers.Add("Open scene object: " + component.gameObject.name + ". Reassign it and save first.");
                }
            }
        }
        finally { if (showProgress) EditorUtility.ClearProgressBar(); }
        return result;
    }

    private static bool IsSerializedAsset(string path) => new[]
        { ".asset", ".prefab", ".unity", ".controller", ".overrideController", ".playable", ".anim" }
        .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static bool HasSavedReference(string path, string source, string guid)
    {
        using var reader = new StreamReader(path);
        // Do not make GetDependencies parse every YAML prefab: unrelated malformed GUIDs can spam the Console.
        if (reader.ReadLine()?.TrimStart('\uFEFF') != "%YAML 1.1")
            return AssetDatabase.GetDependencies(path, false).Contains(source);
        string line;
        while ((line = reader.ReadLine()) != null)
            if (line.IndexOf(guid, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    private static bool References(Object owner, LootObjectSO box)
    {
        using var data = new SerializedObject(owner);
        var property = data.GetIterator();
        while (property.Next(true))
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == box) return true;
        return false;
    }

    private static void Inspect(Object owner, string path, LootObjectSO box, Usage result, HashSet<int> inspected)
    {
        if (owner == null || owner == box || owner is MonoScript || !inspected.Add(owner.GetInstanceID())) return;
        using var data = new SerializedObject(owner);
        var property = data.GetIterator();
        while (property.Next(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != box) continue;
            string label = path + " : " + property.propertyPath;
            result.References.Add(label);
            result.Bindings.Add(new Binding { Owner = owner, Property = property.propertyPath, AssetPath = path });
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !AssetDatabase.IsOpenForEdit(path))
                result.Blockers.Add("Read-only reference: " + label);
            if (EditorUtility.IsDirty(owner) || EditorUtility.IsDirty(AssetDatabase.LoadMainAssetAtPath(path)))
                result.Blockers.Add("Unsaved reference owner: " + path + ". Save it before deleting.");
        }
    }

    /// <returns>Recovery backup directory. File operation: not Unity Undo.</returns>
    public static string DeleteRecord(LootObjectSO box, LootObjectSO replacement, bool showProgress = false)
    {
        string path = RecordPath(box);
        if (replacement != null)
        {
            if (replacement == box) throw new InvalidOperationException("The replacement must be a different loot record.");
            RecordPath(replacement);
        }
        // Re-scan on confirmation. A stale UI scan must never authorize deletion after new references appear.
        var usage = FindUsage(box, showProgress);
        if (usage.Blockers.Count > 0) throw new InvalidOperationException(string.Join("\n", usage.Blockers.Distinct().Take(12)));
        if (usage.Bindings.Count > 0 && replacement == null)
            throw new InvalidOperationException("This record is in use. Choose a replacement first.\n" + string.Join("\n", usage.References.Take(10)));

        AssetDatabase.SaveAssetIfDirty(box); // Preserve current edits in the recovery copy.
        string backup = "Library/LootMakerDeletedRecords/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(backup);
        var manifest = new BackupManifest { deletedRecord = path, replacementRecord = AssetDatabase.GetAssetPath(replacement) };
        foreach (string source in usage.Bindings.Select(b => b.AssetPath).Prepend(path).Distinct())
        {
            string file = manifest.files.Count.ToString("D3") + Path.GetExtension(source);
            File.Copy(source, backup + "/" + file);
            File.Copy(source + ".meta", backup + "/" + file + ".meta");
            manifest.files.Add(new BackupEntry { assetPath = source, backupFile = file });
        }
        File.WriteAllText(backup + "/manifest.json", JsonUtility.ToJson(manifest, true));
        try
        {
            foreach (var group in usage.Bindings.GroupBy(b => b.Owner))
            {
                using var data = new SerializedObject(group.Key);
                foreach (var binding in group)
                {
                    var property = data.FindProperty(binding.Property);
                    if (property == null || property.objectReferenceValue != box)
                        throw new InvalidOperationException("A reference changed during deletion. Nothing was deleted.");
                    property.objectReferenceValue = replacement;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (string ownerPath in usage.Bindings.Select(b => b.AssetPath).Distinct())
            {
                var owner = AssetDatabase.LoadMainAssetAtPath(ownerPath);
                if (owner is GameObject prefab)
                {
                    PrefabUtility.SavePrefabAsset(prefab, out bool success);
                    if (!success) throw new IOException("Could not save replacement links: " + ownerPath);
                }
                else AssetDatabase.SaveAssetIfDirty(owner);
            }
            if (!AssetDatabase.MoveAssetToTrash(path)) throw new IOException("Could not move the loot record to the Recycle Bin.");
        }
        catch (Exception error)
        {
            // Roll back only the exact files backed up for this explicit operation.
            foreach (var entry in manifest.files)
            {
                File.Copy(backup + "/" + entry.backupFile, entry.assetPath, true);
                File.Copy(backup + "/" + entry.backupFile + ".meta", entry.assetPath + ".meta", true);
                AssetDatabase.ImportAsset(entry.assetPath, ImportAssetOptions.ForceUpdate);
            }
            throw new IOException("Deletion failed; original files restored. Backup: " + backup, error);
        }
        return backup;
    }
}
