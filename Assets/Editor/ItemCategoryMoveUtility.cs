using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Transactional ItemSO type/folder changes. Never delete/recreate the .meta.</summary>
public static class ItemCategoryMoveUtility
{
    private sealed class ReferenceBinding
    {
        public Object owner;
        public string propertyPath;
    }

    [Serializable]
    public sealed class MoveRecord
    {
        public string originalPath;
        public string currentPath;
        public string guid;
        public long localId;
        public string originalType;
        public string currentHash;
        public string backupDirectory;
    }

    public sealed class MoveRequest
    {
        public ItemSO item;
        public Type destinationType;
        public string destinationFolder;
        public RangedWeaponType? destinationWeaponType;
    }

    [Serializable]
    public sealed class BatchMoveRecord
    {
        public List<MoveRecord> moves = new();
    }

    private static string HistoryKey => "BoardChase.ItemMaker.LastMove." + Application.dataPath;
    private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
    private static string BackupRoot => Path.GetFullPath(Path.Combine(ProjectRoot, "UserSettings/ItemMakerMoveBackups"));
    private static string AssetFile(string path) => Path.GetFullPath(Path.Combine(ProjectRoot, path));
    public static bool HasLastMove => EditorPrefs.HasKey(HistoryKey);

    /// <summary>Explicit firearm classification, included inside the move backup/rollback boundary.</summary>
    public static void ConfigureWeaponType(ItemSO item, RangedWeaponType weaponType)
    {
        if (item is not RangedWeaponSO firearm || !Enum.IsDefined(typeof(RangedWeaponType), weaponType))
            throw new ArgumentException("A valid firearm type is required.");
        using SerializedObject serialized = new(item);
        SerializedProperty property = serialized.FindProperty("_weaponType");
        if (property == null) throw new InvalidOperationException("The firearm has no serialized type field.");
        int previous = property.intValue;
        property.intValue = (int)weaponType;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (firearm.WeaponType != weaponType)
        {
            property.intValue = previous;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            throw new InvalidOperationException("This concrete firearm class fixes its weapon type. Use the matching template.");
        }
    }

    private static void ValidateWeaponType(Type destinationType, RangedWeaponType? weaponType)
    {
        if (!weaponType.HasValue) return;
        ItemSO probe = (ItemSO)ScriptableObject.CreateInstance(destinationType);
        try { ConfigureWeaponType(probe, weaponType.Value); }
        finally { Object.DestroyImmediate(probe); }
    }

    public static string[] GetRetiredFields(ItemSO item, Type destinationType)
    {
        ItemSO destination = (ItemSO)ScriptableObject.CreateInstance(destinationType);
        try
        {
            using SerializedObject source = new(item);
            using SerializedObject target = new(destination);
            List<string> fields = new();
            SerializedProperty iterator = source.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (!iterator.name.StartsWith("m_", StringComparison.Ordinal) &&
                    target.FindProperty(iterator.propertyPath) == null)
                    fields.Add(iterator.displayName);
            }
            return fields.ToArray();
        }
        finally { Object.DestroyImmediate(destination); }
    }

    /// <summary>Base ItemSO references survive; subtype-only references must not be silently broken.</summary>
    public static List<string> FindIncompatibleReferences(ItemSO item, Type destinationType, bool showProgress = false)
        => ScanReferences(item, destinationType, showProgress, null);

    private static List<string> ScanReferences(ItemSO item, Type destinationType, bool showProgress,
        List<ReferenceBinding> bindings)
    {
        List<string> problems = new();
        if (item.GetType() == destinationType && bindings == null)
            return problems;

        string sourcePath = AssetDatabase.GetAssetPath(item);
        string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
        if (string.IsNullOrEmpty(sourceGuid))
            throw new InvalidOperationException("The source item has no asset GUID. No asset was changed.");
        string[] paths = AssetDatabase.GetAllAssetPaths().Where(path =>
            path.StartsWith("Assets/", StringComparison.Ordinal) && path != sourcePath &&
            (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith(".overrideController", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith(".playable", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))).ToArray();
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (showProgress && i % 100 == 0 && EditorUtility.DisplayCancelableProgressBar(
                        "Item Maker - Check references", path, (float)i / Math.Max(1, paths.Length)))
                {
                    problems.Add("Reference check cancelled. No asset was changed.");
                    break;
                }
                if (!ReferencesItem(path, sourcePath, sourceGuid))
                    continue;

                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    Scene scene = SceneManager.GetSceneByPath(path);
                    if (!scene.isLoaded)
                    {
                        // Never open a user's scene (and execute its editor scripts) just to accept a drop.
                        problems.Add(path + " — open this scene first so its direct item references can be checked.");
                        continue;
                    }
                    foreach (GameObject root in scene.GetRootGameObjects())
                        InspectHierarchy(root, path, item, destinationType, problems, bindings);
                }
                else if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    InspectHierarchy(AssetDatabase.LoadAssetAtPath<GameObject>(path), path, item, destinationType, problems, bindings);
                }
                else
                {
                    foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (asset != null && !(asset is MonoScript))
                            InspectReferences(asset, path, item, destinationType, problems, bindings);
                }
            }

            // The dependency database describes saved assets. Also inspect loaded
            // asset objects in case another Inspector has unsaved reference edits.
            foreach (ScriptableObject asset in Resources.FindObjectsOfTypeAll<ScriptableObject>())
                if (asset != item && EditorUtility.IsPersistent(asset))
                    InspectReferences(asset, AssetDatabase.GetAssetPath(asset), item, destinationType, problems, bindings);

            // Unsaved objects in already open scenes can also hold typed references.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                    InspectHierarchy(root, string.IsNullOrEmpty(scene.path) ? scene.name : scene.path,
                        item, destinationType, problems, bindings);
            }
        }
        finally { if (showProgress) EditorUtility.ClearProgressBar(); }
        return problems.Distinct().ToList();
    }

    private static bool ReferencesItem(string path, string sourcePath, string sourceGuid)
    {
        // Do not ask Unity to parse every prefab/scene in the project. An unrelated
        // malformed GUID makes GetDependencies log an error even without throwing.
        // Text assets can be filtered without importing them. Binary assets still
        // need Unity's dependency database; they must not be silently skipped.
        using StreamReader reader = new(AssetFile(path));
        bool? textReference = TextContainsGuidReference(reader, sourceGuid);
        return textReference ?? AssetDatabase.GetDependencies(path, false).Contains(sourcePath);
    }

    internal static bool? TextContainsGuidReference(TextReader reader, string sourceGuid)
    {
        if (string.IsNullOrEmpty(sourceGuid))
            throw new ArgumentException("An asset GUID is required.", nameof(sourceGuid));
        if (reader.ReadLine()?.TrimStart('\uFEFF') != "%YAML 1.1")
            return null;

        // A GUID in a string value may conservatively include an extra candidate;
        // the existing SerializedObject inspection verifies actual references.
        string line;
        while ((line = reader.ReadLine()) != null)
            if (line.IndexOf(sourceGuid, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }

    private static void InspectHierarchy(GameObject root, string path, ItemSO item, Type target, List<string> problems,
        List<ReferenceBinding> bindings)
    {
        if (root == null) return;
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
            if (component != null)
                InspectReferences(component, path, item, target, problems, bindings);
    }

    private static void InspectReferences(Object owner, string path, ItemSO item, Type target, List<string> problems,
        List<ReferenceBinding> bindings)
    {
        using SerializedObject serialized = new(owner);
        SerializedProperty property = serialized.GetIterator();
        while (property.Next(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != item)
                continue;
            Type fieldType = ResolveFieldType(owner.GetType(), property.propertyPath);
            if (fieldType == null)
            {
                // Managed-reference subtypes may not be visible through their declared base field.
                string pointer = property.type.Replace("PPtr<", "").Replace("$", "").TrimEnd('>');
                fieldType = typeof(ItemSO).Assembly.GetTypes().FirstOrDefault(type =>
                    typeof(Object).IsAssignableFrom(type) && type.Name == pointer);
                if (pointer == "Object") fieldType = typeof(Object);
            }
            if (fieldType == null || !fieldType.IsAssignableFrom(target))
                problems.Add($"{path} — {owner.name}.{property.propertyPath} requires {fieldType?.Name ?? property.type}.");
            else if (bindings != null && !bindings.Any(binding => binding.owner == owner && binding.propertyPath == property.propertyPath))
                bindings.Add(new ReferenceBinding { owner = owner, propertyPath = property.propertyPath });
        }
    }

    private static void RebindReferences(List<ReferenceBinding> bindings, ItemSO item)
    {
        // Replacing a ScriptableObject's managed type preserves its file identity,
        // but Unity can leave already loaded fields pointing at the old wrapper.
        // Rebind those fields too; do not save unrelated assets or user scenes.
        foreach (ReferenceBinding binding in bindings)
        {
            if (binding.owner == null) continue;
            using SerializedObject serialized = new(binding.owner);
            SerializedProperty property = serialized.FindProperty(binding.propertyPath);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException("A reference owner changed during the move: " + binding.propertyPath);
            property.objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Type ResolveFieldType(Type type, string path)
    {
        string[] parts = path.Split('.');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data["))
            {
                type = type.IsArray ? type.GetElementType() : type.IsGenericType ? type.GetGenericArguments()[0] : null;
                i++;
            }
            else
            {
                FieldInfo field = null;
                for (Type current = type; current != null && field == null; current = current.BaseType)
                    field = current.GetField(parts[i], BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                type = field?.FieldType;
            }
            if (type == null) return null;
        }
        return type;
    }

    public static ItemSO Move(ItemSO item, Type destinationType, string destinationFolder, out MoveRecord record,
        bool recordHistory = true, RangedWeaponType? destinationWeaponType = null)
    {
        record = null;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before moving an item.");
        if (item == null || destinationType == null || destinationType.IsAbstract ||
            !typeof(ItemSO).IsAssignableFrom(destinationType))
            throw new ArgumentException("Select a concrete ItemSO destination type.");
        ValidateWeaponType(destinationType, destinationWeaponType);

        string sourcePath = AssetDatabase.GetAssetPath(item);
        RequireAssetPath(sourcePath);
        RequireAssetPath(destinationFolder + "/item.asset");
        if (!AssetDatabase.IsMainAsset(item) || AssetDatabase.LoadAllAssetsAtPath(sourcePath).Length != 1)
            throw new InvalidOperationException("Only standalone ItemSO assets can be moved; sub-assets are not converted.");
        if (!AssetDatabase.IsOpenForEdit(sourcePath) || !AssetDatabase.IsOpenForEdit(sourcePath + ".meta"))
            throw new InvalidOperationException("The ItemSO asset or its .meta is read-only / not checked out.");

        List<ReferenceBinding> bindings = new();
        List<string> incompatible = item.GetType() == destinationType
            ? new List<string>() : ScanReferences(item, destinationType, recordHistory, bindings);
        if (incompatible.Count > 0)
            throw new InvalidOperationException("Move blocked to preserve references:\n\n" + string.Join("\n", incompatible.Take(12)));

        EnsureFolder(destinationFolder);
        string targetPath = destinationFolder + "/" + Path.GetFileName(sourcePath);
        if (targetPath != sourcePath && File.Exists(AssetFile(targetPath)))
            throw new IOException("Another asset already has this name in the destination. Rename it first:\n" + targetPath);

        AssetDatabase.SaveAssetIfDirty(item);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item, out string guid, out long localId);
        // Newly created assets can reach this path before their first OnValidate.
        // An empty identity has no valid save reference to preserve; initialize it
        // from the existing asset GUID, never generate a different identity.
        string runtimeGuid = string.IsNullOrWhiteSpace(item.Guid) ? guid : item.Guid;
        string originalName = item.name;
        Sprite originalIcon = item.PreviewImage;
        GameObject originalVisual = item.WorldVisualPrefab;
        Type originalType = item.GetType();
        string backup = Path.Combine(BackupRoot, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + guid);
        Directory.CreateDirectory(backup);
        File.Copy(AssetFile(sourcePath), Path.Combine(backup, "original.asset"));
        File.Copy(AssetFile(sourcePath + ".meta"), Path.Combine(backup, "original.meta"));
        record = new MoveRecord { originalPath = sourcePath, currentPath = sourcePath, guid = guid,
            localId = localId, originalType = originalType.AssemblyQualifiedName, backupDirectory = backup };
        WriteRecord(record);

        ItemSO replacement = null;
        try
        {
            if (originalType != destinationType)
            {
                replacement = (ItemSO)ScriptableObject.CreateInstance(destinationType);
                // Unity copies matching serializable fields (including FormerlySerializedAs).
                // Destination-only fields keep their defaults; removed fields remain in the backup.
                EditorUtility.CopySerializedManagedFieldsOnly(item, replacement);
                replacement.name = originalName;
                AssetDatabase.CreateAsset(replacement, sourcePath);
                AssetDatabase.SaveAssetIfDirty(replacement);
                AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            if (sourcePath != targetPath)
            {
                string error = AssetDatabase.MoveAsset(sourcePath, targetPath);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                record.currentPath = targetPath;
            }

            ItemSO moved = AssetDatabase.LoadAssetAtPath<ItemSO>(targetPath);
            if (moved == null || moved.GetType() != destinationType)
                throw new InvalidOperationException("Unity did not import the requested item type.");
            if (destinationWeaponType.HasValue)
            {
                ConfigureWeaponType(moved, destinationWeaponType.Value);
                AssetDatabase.SaveAssetIfDirty(moved);
            }
            if (string.IsNullOrWhiteSpace(moved.Guid))
            {
                using SerializedObject serialized = new(moved);
                serialized.FindProperty("_guid").stringValue = runtimeGuid;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(moved);
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(moved, out string newGuid, out long newId);
            if (guid != newGuid || localId != newId || runtimeGuid != moved.Guid ||
                originalIcon != moved.PreviewImage || originalVisual != moved.WorldVisualPrefab)
                throw new InvalidOperationException("Identity or shared visual data changed unexpectedly; restoring the original.");

            RebindReferences(bindings, moved);

            record.currentHash = Hash(targetPath);
            WriteRecord(record);
            if (recordHistory) EditorPrefs.SetString(HistoryKey, Path.Combine(backup, "record.json"));
            return moved;
        }
        catch
        {
            RestoreFiles(record);
            RebindReferences(bindings, AssetDatabase.LoadAssetAtPath<ItemSO>(record.originalPath));
            throw;
        }
        finally
        {
            if (replacement != null && !AssetDatabase.Contains(replacement))
                Object.DestroyImmediate(replacement);
        }
    }

    /// <summary>Validate the whole drop before changing assets; roll back completed moves on failure.</summary>
    public static ItemSO[] MoveBatch(IReadOnlyList<MoveRequest> requests, out BatchMoveRecord batch,
        bool recordHistory = true)
    {
        batch = new BatchMoveRecord();
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before moving items.");
        if (requests == null || requests.Count == 0)
            throw new ArgumentException("Select at least one item to move.");

        HashSet<string> sources = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> destinations = new(StringComparer.OrdinalIgnoreCase);
        List<string> guids = new();
        string historyPath = null;
        try
        {
            for (int i = 0; i < requests.Count; i++)
            {
                MoveRequest request = requests[i];
                if (request?.item == null || request.destinationType == null || request.destinationType.IsAbstract ||
                    !typeof(ItemSO).IsAssignableFrom(request.destinationType))
                    throw new ArgumentException("Every destination must be a concrete ItemSO type.");
                ValidateWeaponType(request.destinationType, request.destinationWeaponType);
                string source = AssetDatabase.GetAssetPath(request.item);
                string target = request.destinationFolder + "/" + Path.GetFileName(source);
                RequireAssetPath(source);
                RequireAssetPath(target);
                if (!sources.Add(source) || !destinations.Add(target))
                    throw new IOException("Duplicate source or destination in this selection:\n" + target);
                if (source != target && File.Exists(AssetFile(target)))
                    throw new IOException("Another asset already has this name in the destination:\n" + target);
                if (!AssetDatabase.IsMainAsset(request.item) || AssetDatabase.LoadAllAssetsAtPath(source).Length != 1 ||
                    !AssetDatabase.IsOpenForEdit(source) || !AssetDatabase.IsOpenForEdit(source + ".meta"))
                    throw new InvalidOperationException("Only editable standalone ItemSO assets can be moved:\n" + source);
                if (recordHistory && EditorUtility.DisplayCancelableProgressBar("Item Maker - Check batch",
                        source, (float)i / requests.Count))
                    throw new OperationCanceledException("Batch move cancelled. No asset was changed.");
                List<string> incompatible = FindIncompatibleReferences(request.item, request.destinationType);
                if (incompatible.Count > 0)
                    throw new InvalidOperationException(source + "\nMove blocked to preserve references:\n" +
                        string.Join("\n", incompatible.Take(12)));
                guids.Add(AssetDatabase.AssetPathToGUID(source));
            }

            string batchFolder = Path.Combine(BackupRoot, "batch-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(batchFolder);
            historyPath = Path.Combine(batchFolder, "batch.json");
            for (int i = 0; i < requests.Count; i++)
            {
                if (recordHistory) EditorUtility.DisplayProgressBar("Item Maker - Move batch",
                    $"{i + 1} / {requests.Count}", (float)i / requests.Count);
                MoveRequest request = requests[i];
                // A previous conversion may have replaced another loaded managed object.
                ItemSO current = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
                Move(current, request.destinationType, request.destinationFolder, out MoveRecord record, false,
                    request.destinationWeaponType);
                batch.moves.Add(record);
                File.WriteAllText(historyPath, JsonUtility.ToJson(batch, true));
            }
            // Rebinding references between selected items can dirty an earlier item.
            // Save only members of this transaction, then finalize their restore guards.
            foreach (MoveRecord record in batch.moves)
            {
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<ItemSO>(record.currentPath));
                record.currentHash = Hash(record.currentPath);
                WriteRecord(record);
            }
            File.WriteAllText(historyPath, JsonUtility.ToJson(batch, true));
            if (recordHistory) EditorPrefs.SetString(HistoryKey, historyPath);
            return batch.moves.Select(record => AssetDatabase.LoadAssetAtPath<ItemSO>(record.currentPath)).ToArray();
        }
        catch (Exception error)
        {
            List<string> rollbackErrors = new();
            for (int i = batch.moves.Count - 1; i >= 0; i--)
            {
                try { RestoreCore(batch.moves[i], false); }
                catch (Exception rollback) { rollbackErrors.Add(batch.moves[i].backupDirectory + "\n" + rollback.Message); }
            }
            if (rollbackErrors.Count > 0)
                throw new InvalidOperationException(error.Message + "\n\nSome items could not be rolled back. Backups:\n" +
                    string.Join("\n", rollbackErrors), error);
            throw;
        }
        finally { if (recordHistory) EditorUtility.ClearProgressBar(); }
    }

    public static ItemSO RestoreLastMove() => RestoreLastMoves().LastOrDefault();

    public static ItemSO[] RestoreLastMoves()
    {
        string recordPath = EditorPrefs.GetString(HistoryKey, "");
        if (string.IsNullOrEmpty(recordPath) || !File.Exists(recordPath))
            throw new InvalidOperationException("No move backup is available.");
        RequireBackupPath(recordPath);
        string json = File.ReadAllText(recordPath);
        BatchMoveRecord batch = JsonUtility.FromJson<BatchMoveRecord>(json);
        // Preserve recovery of single-item records written by older versions.
        if (batch.moves == null || batch.moves.Count == 0)
            batch.moves = new List<MoveRecord> { JsonUtility.FromJson<MoveRecord>(json) };
        ItemSO[] restored;
        try { restored = RestoreBatch(batch); }
        catch
        {
            // If an I/O failure interrupted restoration, retain only pending entries.
            File.WriteAllText(recordPath, JsonUtility.ToJson(batch, true));
            throw;
        }
        EditorPrefs.DeleteKey(HistoryKey);
        return restored;
    }

    public static ItemSO[] RestoreBatch(BatchMoveRecord batch)
    {
        if (batch?.moves == null || batch.moves.Count == 0)
            throw new InvalidOperationException("No move backup is available.");
        // Newer edits or conflicts in ANY member stop the entire restore before writes.
        foreach (MoveRecord record in batch.moves) ValidateRestore(record);
        List<ItemSO> restored = new();
        int total = batch.moves.Count;
        try
        {
            while (batch.moves.Count > 0)
            {
                int index = batch.moves.Count - 1;
                restored.Add(RestoreCore(batch.moves[index], true));
                batch.moves.RemoveAt(index);
            }
        }
        catch (Exception error)
        {
            throw new InvalidOperationException($"Restored {restored.Count} / {total} items. Remaining backups are retained.\n" + error.Message, error);
        }
        restored.Reverse();
        return restored.ToArray();
    }

    public static ItemSO Restore(MoveRecord record)
    {
        ValidateRestore(record);
        return RestoreCore(record, true);
    }

    private static void ValidateRestore(MoveRecord record)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before restoring items.");
        if (record == null) throw new ArgumentException("Invalid move backup.");
        RequireAssetPath(record.currentPath);
        RequireAssetPath(record.originalPath);
        RequireBackupPath(record.backupDirectory);
        if (!File.Exists(AssetFile(record.currentPath)) || Hash(record.currentPath) != record.currentHash ||
            AssetDatabase.AssetPathToGUID(record.currentPath) != record.guid)
            throw new InvalidOperationException("The item changed after the move. Restore was stopped to protect newer edits.\nBackup: " + record.backupDirectory);
        ItemSO current = AssetDatabase.LoadAssetAtPath<ItemSO>(record.currentPath);
        if (current == null || EditorUtility.IsDirty(current))
            throw new InvalidOperationException("The item has unsaved edits. Restore was stopped to protect them.");
        if (!AssetDatabase.IsOpenForEdit(record.currentPath) || !AssetDatabase.IsOpenForEdit(record.currentPath + ".meta"))
            throw new InvalidOperationException("The item or its .meta is read-only: " + record.currentPath);
        if (record.currentPath != record.originalPath && File.Exists(AssetFile(record.originalPath)))
            throw new IOException("Restore destination is occupied: " + record.originalPath);
        if (!File.Exists(Path.Combine(record.backupDirectory, "original.asset")) ||
            !File.Exists(Path.Combine(record.backupDirectory, "original.meta")))
            throw new IOException("The move backup is incomplete: " + record.backupDirectory);
        Type originalType = Type.GetType(record.originalType);
        if (originalType == null) throw new InvalidOperationException("The original item script is no longer available.");
        List<string> incompatible = FindIncompatibleReferences(current, originalType, true);
        if (incompatible.Count > 0) throw new InvalidOperationException(string.Join("\n", incompatible.Take(12)));
    }

    private static ItemSO RestoreCore(MoveRecord record, bool showProgress)
    {
        ItemSO current = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(record.guid));
        Type originalType = Type.GetType(record.originalType);
        if (current == null || originalType == null)
            throw new InvalidOperationException("The item or its original script is unavailable: " + record.originalPath);
        // Resolve current owners again: an earlier restore can replace their CLR type.
        List<ReferenceBinding> bindings = new();
        List<string> incompatible = current.GetType() == originalType
            ? new List<string>() : ScanReferences(current, originalType, showProgress, bindings);
        if (incompatible.Count > 0) throw new InvalidOperationException(string.Join("\n", incompatible.Take(12)));
        RestoreFiles(record);
        ItemSO restored = AssetDatabase.LoadAssetAtPath<ItemSO>(record.originalPath);
        RebindReferences(bindings, restored);
        return restored;
    }

    private static void RestoreFiles(MoveRecord record)
    {
        RequireAssetPath(record.originalPath);
        RequireAssetPath(record.currentPath);
        RequireBackupPath(record.backupDirectory);
        if (record.currentPath != record.originalPath)
        {
            if (File.Exists(AssetFile(record.originalPath))) throw new IOException("Restore destination is occupied: " + record.originalPath);
            string error = AssetDatabase.MoveAsset(record.currentPath, record.originalPath);
            if (!string.IsNullOrEmpty(error)) throw new IOException("Could not restore the item path: " + error);
        }
        File.Copy(Path.Combine(record.backupDirectory, "original.asset"), AssetFile(record.originalPath), true);
        File.Copy(Path.Combine(record.backupDirectory, "original.meta"), AssetFile(record.originalPath + ".meta"), true);
        AssetDatabase.ImportAsset(record.originalPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    }

    private static void WriteRecord(MoveRecord record) =>
        File.WriteAllText(Path.Combine(record.backupDirectory, "record.json"), JsonUtility.ToJson(record, true));

    private static string Hash(string path)
    {
        using SHA256 algorithm = SHA256.Create();
        return Convert.ToBase64String(algorithm.ComputeHash(File.ReadAllBytes(AssetFile(path))));
    }

    private static void RequireAssetPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
            !AssetFile(path).StartsWith(Application.dataPath.Replace('/', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Item moves must stay inside Assets.");
    }

    private static void RequireBackupPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !Path.GetFullPath(path).StartsWith(BackupRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid Item Maker backup path.");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(parent)) throw new IOException("Invalid asset folder.");
        EnsureFolder(parent);
        if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(folder))))
            throw new IOException("Could not create item folder: " + folder);
    }
}
