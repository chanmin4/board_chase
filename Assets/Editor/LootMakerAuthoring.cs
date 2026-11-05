using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Explicit authoring operations only: no startup migration, no runtime loot changes.</summary>
public static class LootMakerAuthoring
{
    public const string Root = "Assets/ScriptableObject/GameScene/GameItem/Loot";

    public static void SetCustomCategoryChances(LootTableSO table, LootRarityTier tier, bool enabled)
    {
        if (table == null || tier == null || !table.Pool.rarityTiers.Contains(tier)) throw new ArgumentException("Choose a tier in this table.");
        var prior = table.GetCategorySelection(tier);
        Undo.RecordObject(table, "Set Loot Category Chances");
        var settings = table.Pool.EnsureCategorySettings(tier.rarity);
        // Seed only the first time; toggling Auto does not throw away authored weights.
        if (enabled && (settings.categories == null || settings.categories.Count == 0))
            foreach (var group in prior.Groups) settings.SetWeight(group.Key, prior.HasCandidates ? prior.CategoryPercent(group.Key) : 1f);
        settings.useCustomCategoryWeights = enabled;
        EditorUtility.SetDirty(table);
    }

    public static void SetCategoryWeight(LootTableSO table, LootRarityTier tier, string categoryKey, float weight)
    {
        if (table == null || tier == null || !table.Pool.rarityTiers.Contains(tier)) throw new ArgumentException("Choose a tier in this table.");
        var prior = table.GetCategorySelection(tier);
        Undo.RecordObject(table, "Edit Loot Category Weight");
        var settings = table.Pool.EnsureCategorySettings(tier.rarity);
        // Materialize inherited legacy budgets before editing one child, keeping
        // every other child's effective weight unchanged.
        // Legacy manual shares were scoped across ALL Valuables. Normalize those
        // entries inside each child before switching it to an explicit weight;
        // otherwise a 20/40 sibling ratio would silently become 20/80.
        if (settings.Find("Valuable") != null)
            foreach (var group in prior.Groups)
                if ((group.Key == "Valuable" || group.Key.StartsWith("Valuable:", StringComparison.Ordinal)) &&
                    (group.Key == "Valuable" || settings.FindEffective(group.Key) == null))
                    foreach (var entry in group.Entries)
                    {
                        float share = prior.ItemPercentInCategory(entry);
                        entry.useManualShare = true;
                        entry.manualSharePercent = share;
                    }
        foreach (var group in prior.Groups) settings.SetWeight(group.Key, (float)group.CategoryWeight);
        settings.SetWeight(categoryKey, weight);
        EditorUtility.SetDirty(table);
    }

    /// <summary>Classify only this box. Never rebind/copy its table or edit a shared family.</summary>
    public static bool MoveContainerCategory(LootObjectSO box, LootContainerCategory category)
    {
        if (box == null) throw new ArgumentNullException(nameof(box));
        if (!Enum.IsDefined(typeof(LootContainerCategory), category))
            throw new ArgumentOutOfRangeException(nameof(category));
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Move boxes outside Play Mode and after compilation finishes.");
        if (AssetDatabase.Contains(box) && !AssetDatabase.IsOpenForEdit(box))
            throw new InvalidOperationException("Loot record is read-only: " + AssetDatabase.GetAssetPath(box));
        if (box.HasCategoryAssignment && box.Category == category) return false;
        Undo.RecordObject(box, "Move Loot Box Category");
        box.AssignCategory(category);
        EditorUtility.SetDirty(box);
        return true;
    }

    /// <summary>Rename the prefab shown on the card, preserving its GUID, variants and all SO links.</summary>
    public static string RenameContainerPrefab(LootObjectSO box, string requestedName)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before renaming a prefab.");
        if (box == null || box.WorldPrefab == null)
            throw new InvalidOperationException("This box has no linked world prefab.");
        string path = AssetDatabase.GetAssetPath(box.WorldPrefab);
        if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
            !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
            AssetDatabase.LoadAssetAtPath<GameObject>(path) != box.WorldPrefab)
            throw new InvalidOperationException("Choose a root prefab asset under Assets, not a scene object or model.");
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Save and close Prefab Mode before renaming.");
        if (!AssetDatabase.IsOpenForEdit(path)) throw new InvalidOperationException("Prefab is read-only: " + path);
        string name = ValidatePrefabName(requestedName);
        string targetPath = Path.GetDirectoryName(path).Replace('\\', '/') + "/" + name + ".prefab";
        if (path == targetPath) return path;
        string guid = AssetDatabase.AssetPathToGUID(path);
        string occupiedGuid = AssetDatabase.AssetPathToGUID(targetPath);
        if ((!string.IsNullOrEmpty(occupiedGuid) && occupiedGuid != guid) ||
            ((File.Exists(targetPath) || Directory.Exists(targetPath)) && !string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A prefab with that name already exists in this folder. Nothing was replaced.");
        string error = AssetDatabase.RenameAsset(path, name);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        string renamedPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(renamedPath)) throw new IOException("Could not resolve the renamed prefab GUID.");
        return renamedPath;
    }

    private static string ValidatePrefabName(string requestedName)
    {
        string name = (requestedName ?? string.Empty).Trim();
        if (name.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 7);
        if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.EndsWith(".") ||
            name.EndsWith(" ") || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\'))
            throw new ArgumentException("Enter a file name only, without folders, invalid characters, or a trailing dot.");
        string device = name.Split('.')[0].ToUpperInvariant();
        if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(device))
            throw new ArgumentException("That name is reserved by Windows. Choose another name.");
        return name;
    }

    // A fallback display title is independent of the prefab file and any shared loot list.
    public static void SetContainerDisplayName(LootObjectSO box, string value)
    {
        if (box == null) throw new ArgumentNullException(nameof(box));
        Undo.RecordObject(box, "Rename Loot Display Title");
        using var data = new SerializedObject(box);
        data.FindProperty("_displayName").stringValue = value?.Trim() ?? string.Empty;
        data.ApplyModifiedProperties();
        EditorUtility.SetDirty(box);
    }

    public static LootTableSO Snapshot(LootTableSO source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var copy = Object.Instantiate(source);
        copy.name = source.name;
        copy.hideFlags = HideFlags.HideAndDontSave;
        return copy;
    }

    // Copies only table contents, never asset identity/name or per-box settings.
    public static void PasteContents(LootTableSO snapshot, LootTableSO destination)
    {
        if (snapshot == null || destination == null) throw new ArgumentNullException();
        int users = AssetDatabase.FindAssets("t:LootObjectSO").Select(guid =>
            AssetDatabase.LoadAssetAtPath<LootObjectSO>(AssetDatabase.GUIDToAssetPath(guid)))
            .Count(box => box != null && box.LootTable == destination);
        if (users > 1) throw new InvalidOperationException("Cannot overwrite a shared box table. Use PasteToBox to create an independent copy first.");
        // Freeze legacy inherited counts before replacing their table's old defaults.
        foreach (string guid in AssetDatabase.FindAssets("t:LootObjectSO"))
        {
            var box = AssetDatabase.LoadAssetAtPath<LootObjectSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (box == null || box.LootTable != destination || box.HasSlotGenerationSettings) continue;
            Undo.RecordObject(box, "Keep Box Filled Slots");
            box.InitializeSlotGenerationSettings();
            EditorUtility.SetDirty(box);
        }
        using var source = new SerializedObject(snapshot);
        using var target = new SerializedObject(destination);
        Undo.RecordObject(destination, "Paste Loot Table Contents");
        target.CopyFromSerializedProperty(source.FindProperty("_pool"));
        target.ApplyModifiedProperties();
        EditorUtility.SetDirty(destination);
    }

    public static LootTableSO MakeIndependent(LootObjectSO box, LootTableSO source, string folder = Root)
    {
        if (box == null) throw new ArgumentNullException(nameof(box));
        var table = CreateTable(source, box.WorldPrefab != null ? box.WorldPrefab.name : box.name, folder);
        Undo.RecordObject(box, "Assign Independent Loot Table");
        box.AssignOwnLootTable(table);
        EditorUtility.SetDirty(box);
        return table;
    }

    public static bool NeedsIndependentTable(LootObjectSO box)
    {
        if (box == null) throw new ArgumentNullException(nameof(box));
        if (box.LootObjectType != null || box.LootTable == null) return true;
        // Handles assets duplicated outside Loot Maker (Project Ctrl+D) without editing the original.
        return AssetDatabase.FindAssets("t:LootObjectSO").Select(guid =>
            AssetDatabase.LoadAssetAtPath<LootObjectSO>(AssetDatabase.GUIDToAssetPath(guid)))
            .Any(other => other != null && other != box && other.LootTable == box.LootTable);
    }

    public static LootTableSO PasteToBox(LootTableSO snapshot, LootObjectSO box, string folder = Root)
    {
        if (snapshot == null || box == null) throw new ArgumentNullException();
        if (NeedsIndependentTable(box)) MakeIndependent(box, box.LootTable, folder);
        PasteContents(snapshot, box.LootTable);
        return box.LootTable;
    }

    public static LootTableSO CreateTable(LootTableSO source, string label, string folder = Root)
    {
        EnsureFolder(folder + "/LootTable");
        string safe = SafeName(label);
        var table = source != null ? Object.Instantiate(source) : ScriptableObject.CreateInstance<LootTableSO>();
        table.hideFlags = HideFlags.None;
        table.name = "LootTable_" + safe;
        table.EnsurePool();
        string tablePath = AssetDatabase.GenerateUniqueAssetPath(folder + "/LootTable/" + table.name + ".asset");
        table.name = Path.GetFileNameWithoutExtension(tablePath);
        try
        {
            AssetDatabase.CreateAsset(table, tablePath);
            return table;
        }
        catch
        {
            if (AssetDatabase.Contains(table)) AssetDatabase.DeleteAsset(tablePath); else Object.DestroyImmediate(table);
            throw;
        }
    }

    public static bool CanRegister(Object candidate, out GameObject prefab, out string reason)
    {
        prefab = null;
        string path = AssetDatabase.GetAssetPath(candidate);
        if (candidate is not GameObject || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        { reason = "Drop a prefab asset from Project, not a scene object or model file."; return false; }
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null || candidate != prefab)
        { reason = "Choose the root prefab asset, not one of its children."; return false; }
        var lootables = prefab.GetComponentsInChildren<LootableObject>(true);
        if (lootables.Length != 1)
        { reason = "The prefab needs exactly one LootableObject. Use a loot-box prefab or its variant."; return false; }
        if (lootables[0].GetComponent<StructureLootInventoryRuntime>() == null)
        { reason = "Add StructureLootInventoryRuntime beside LootableObject before registering this prefab."; return false; }
        reason = null;
        return true;
    }

    public static LootObjectSO Register(GameObject prefab, LootObjectTypeSO copyFrom, LootContainerCategory category,
        out bool created, string folder = Root)
        => RegisterWithTable(prefab, copyFrom?.LootTable, category, out created, folder);

    public static LootObjectSO RegisterWithTable(GameObject prefab, LootTableSO copyFrom, LootContainerCategory category,
        out bool created, string folder = Root)
    {
        created = false;
        if (!CanRegister(prefab, out prefab, out string reason)) throw new InvalidOperationException(reason);
        string path = AssetDatabase.GetAssetPath(prefab);
        var existing = prefab.GetComponentInChildren<LootableObject>(true).LootObject;
        // Re-registering an existing asset is selection only. Never overwrite its authored list.
        if (existing != null && SamePrefab(existing.WorldPrefab, prefab)) return existing;
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == path)
            throw new InvalidOperationException("Close this prefab's Prefab Mode before registering it.");
        if (!AssetDatabase.IsOpenForEdit(path)) throw new InvalidOperationException("Prefab is read-only: " + path);

        // A copied/variant prefab may inherit another box's record. Give it its OWN record,
        // preserving capacity/yield and making a deep copy of the selected/template list.
        copyFrom ??= existing?.LootTable;
        var record = existing != null ? Object.Instantiate(existing) : ScriptableObject.CreateInstance<LootObjectSO>();
        record.hideFlags = HideFlags.None;
        record.name = "LootObject_" + SafeName(prefab.name);
        EnsureFolder(folder + "/LootObject");
        string recordPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/LootObject/" + record.name + ".asset");
        GameObject contents = null;
        LootTableSO ownTable = null;
        string backup = null;
        bool savingPrefab = false;
        try
        {
            contents = PrefabUtility.LoadPrefabContents(path);
            var lootable = contents.GetComponentInChildren<LootableObject>(true);
            ownTable = CreateTable(copyFrom, prefab.name, folder);
            record.AssignOwnLootTable(ownTable);
            if (existing == null) record.SetGeneratedSlotRange(ownTable.Pool.minRollCount, ownTable.Pool.maxRollCount);
            record.AssignCategory(category);
            using (var data = new SerializedObject(record))
            {
                data.FindProperty("_worldPrefab").objectReferenceValue = prefab;
                data.FindProperty("_displayName").stringValue = prefab.name;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(record, recordPath);
            using (var data = new SerializedObject(lootable))
            {
                data.FindProperty("_lootObject").objectReferenceValue = record;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            // Saved assets are kept on Undo of a table assignment so they remain reusable.
            // Prefab registration is a file operation; keep an explicit recovery copy.
            Directory.CreateDirectory("Library/LootMakerRegistrationBackups");
            backup = "Library/LootMakerRegistrationBackups/" + AssetDatabase.AssetPathToGUID(path) + "-" + Guid.NewGuid().ToString("N") + ".prefab";
            File.Copy(path, backup);
            savingPrefab = true;
            PrefabUtility.SaveAsPrefabAsset(contents, path, out bool success);
            if (!success) throw new IOException("Could not save prefab: " + path);
            created = true;
            return record;
        }
        catch
        {
            if (savingPrefab && backup != null) File.Copy(backup, path, true);
            if (AssetDatabase.Contains(record)) AssetDatabase.DeleteAsset(recordPath); else Object.DestroyImmediate(record);
            if (ownTable != null) AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(ownTable));
            if (savingPrefab) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            throw;
        }
        finally { if (contents != null) PrefabUtility.UnloadPrefabContents(contents); }
    }

    private static bool SamePrefab(GameObject a, GameObject b) => a != null && b != null &&
        AssetDatabase.GetAssetPath(a) == AssetDatabase.GetAssetPath(b);

    private static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        string safe = new string((name ?? "Loot").Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().Trim('.');
        return string.IsNullOrEmpty(safe) ? "Loot" : safe;
    }

    private static void EnsureFolder(string folder)
    {
        if (!folder.StartsWith("Assets/", StringComparison.Ordinal) || folder.Split('/').Any(p => p == ".."))
            throw new ArgumentException("Loot assets must be saved under Assets.");
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        if (parent != "Assets") EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
