using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit asset operations. Browsing never creates, copies, rebinds or deletes assets.</summary>
internal static class SectorMakerAssets
{
    internal const string TemplateRoot = "Assets/ScriptableObject/GameScene/Stage/Sector/Preset/Templates";
    internal static bool IsTemplate(SectorPresetSO preset) => preset != null &&
        AssetDatabase.GetAssetPath(preset).StartsWith(TemplateRoot + "/", StringComparison.Ordinal);

    /// <summary>Rename only the selected prefab file. Preserve GUID, Variant ancestry, and all SO/Stage references.</summary>
    internal static string RenameRoomPrefab(GameObject prefab, string requestedName)
    {
        RequireEditMode();
        if (!SectorMakerCatalog.TryPrefab(prefab, out prefab))
            throw new ArgumentException("Project의 SectorRuntime 방 프리팹을 선택하세요.");
        string path = AssetDatabase.GetAssetPath(prefab);
        if (!path.StartsWith("Assets/", StringComparison.Ordinal))
            throw new ArgumentException("Assets 안의 방 프리팹만 이름을 변경할 수 있습니다.");
        if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Prefab Mode의 편집 내용을 저장하고 Prefab Mode를 종료한 뒤 이름을 변경하세요.");
        if (EditorUtility.IsDirty(prefab))
            throw new InvalidOperationException("선택한 프리팹의 변경 내용을 먼저 저장하세요.");
        if (!AssetDatabase.IsOpenForEdit(path))
            throw new InvalidOperationException("읽기 전용 프리팹입니다: " + path);

        string name = (requestedName ?? string.Empty).Trim();
        if (name.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 7);
        if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.EndsWith(".") || name.EndsWith(" ") ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\'))
            throw new ArgumentException("폴더 경로나 특수문자 없이 유효한 프리팹 이름을 입력하세요.");
        string device = name.Split('.')[0].ToUpperInvariant();
        if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(device))
            throw new ArgumentException("Windows 예약 이름은 사용할 수 없습니다.");
        string target = Path.GetDirectoryName(path).Replace('\\', '/') + "/" + name + ".prefab";
        if (path == target) return path;
        string guid = AssetDatabase.AssetPathToGUID(path);
        string occupied = AssetDatabase.AssetPathToGUID(target);
        if ((!string.IsNullOrEmpty(occupied) && occupied != guid) ||
            (!string.Equals(path, target, StringComparison.OrdinalIgnoreCase) &&
             (File.Exists(target) || File.Exists(target + ".meta") || Directory.Exists(target))))
            throw new InvalidOperationException("이 폴더에 같은 이름이 이미 있습니다. 기존 파일은 변경하지 않았습니다.");
        string error = AssetDatabase.RenameAsset(path, name);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        string renamedPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(renamedPath)) throw new IOException("변경한 프리팹의 GUID를 찾을 수 없습니다.");
        return renamedPath;
    }

    internal static void RequireNewPath(string path, string extension)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
            path.Contains('\\') || path.Split('/').Any(p => p == ".." || p == ".") ||
            !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
            !AssetDatabase.IsValidFolder(Path.GetDirectoryName(path)?.Replace('\\', '/')) ||
            File.Exists(path) || File.Exists(path + ".meta") || Directory.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
            throw new ArgumentException("Assets 안의 기존 폴더에, 아직 존재하지 않는 새 파일 경로를 선택하세요: " + path);
    }

    internal static SectorPresetSO CopyPreset(SectorPresetSO source, GameObject targetPrefab, string destination)
    {
        RequireEditMode();
        RequireNewPath(destination, ".asset");
        if (source == null || !EditorUtility.IsPersistent(source) || !SectorMakerCatalog.TryPrefab(targetPrefab, out targetPrefab))
            throw new ArgumentException("복사할 SO와 SectorRuntime 프리팹을 선택하세요.");
        if (EditorUtility.IsDirty(source) || source.ContentProfile != null && EditorUtility.IsDirty(source.ContentProfile))
            throw new InvalidOperationException("원본 SO와 콘텐츠 설정을 먼저 저장한 뒤 복사하세요.");
        var copy = UnityEngine.Object.Instantiate(source);
        copy.name = Path.GetFileNameWithoutExtension(destination);
        string profilePath = null;
        try
        {
            var data = new SerializedObject(copy);
            if (source.SectorPrefab == null || source.SectorPrefab.gameObject != targetPrefab)
                data.FindProperty("_displayName").stringValue = targetPrefab.name;
            data.FindProperty("_sectorPrefab").objectReferenceValue = targetPrefab.GetComponent<SectorRuntime>();
            data.ApplyModifiedPropertiesWithoutUndo();
            // Content is mutable authoring data: new rooms must not accidentally edit the base's slots.
            if (source.ContentProfile != null)
            {
                profilePath = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(destination).Replace('\\', '/') + "/Content_" + Path.GetFileNameWithoutExtension(destination) + ".asset");
                RequireNewPath(profilePath, ".asset");
                var profile = UnityEngine.Object.Instantiate(source.ContentProfile);
                profile.name = Path.GetFileNameWithoutExtension(profilePath);
                AssetDatabase.CreateAsset(profile, profilePath);
                copy.SetContentProfile(profile);
            }
            AssetDatabase.CreateAsset(copy, destination);
            return copy;
        }
        catch
        {
            // Only paths reserved as absent above belong to this failed operation.
            if (File.Exists(destination)) AssetDatabase.DeleteAsset(destination);
            if (profilePath != null && File.Exists(profilePath)) AssetDatabase.DeleteAsset(profilePath);
            if (copy != null && !EditorUtility.IsPersistent(copy)) UnityEngine.Object.DestroyImmediate(copy);
            throw;
        }
    }

    internal static SectorPresetSO CreateRoom(GameObject sourcePrefab, SectorPresetSO sourcePreset,
        string prefabPath, string presetPath, StageRoomType kind)
    {
        RequireEditMode();
        RequireNewPath(prefabPath, ".prefab"); RequireNewPath(presetPath, ".asset");
        if (!SectorMakerCatalog.TryPrefab(sourcePrefab, out sourcePrefab)) throw new ArgumentException("기준 Base 프리팹을 선택하세요.");
        if (sourcePreset != null && (!SectorMakerCatalog.TryPrefab(sourcePreset, out var presetPrefab) || presetPrefab != sourcePrefab))
            throw new ArgumentException("기준 SO와 기준 프리팹의 연결이 다릅니다.");
        if (sourcePreset != null && (EditorUtility.IsDirty(sourcePreset) || sourcePreset.ContentProfile != null && EditorUtility.IsDirty(sourcePreset.ContentProfile)))
            throw new InvalidOperationException("기준 SO / 콘텐츠 설정을 먼저 저장하세요.");
        try
        {
            var variant = SectorMakerCatalog.CreateVariant(sourcePrefab, prefabPath);
            if (variant == null) throw new InvalidOperationException("새 Variant 프리팹 저장에 실패했습니다.");
            return sourcePreset != null ? CopyPreset(sourcePreset, variant, presetPath) : SectorMakerCatalog.CreatePreset(variant, presetPath, kind);
        }
        catch
        {
            if (File.Exists(prefabPath)) AssetDatabase.DeleteAsset(prefabPath);
            throw;
        }
    }

    internal static string[] ReferencesTo(SectorPresetSO preset)
    {
        return ReferencesToMany(new[] { preset })[preset];
    }
    private static Dictionary<SectorPresetSO, string[]> ReferencesToMany(SectorPresetSO[] presets)
    {
        var targets = new Dictionary<string, SectorPresetSO>(StringComparer.Ordinal);
        var users = new Dictionary<SectorPresetSO, HashSet<string>>();
        foreach (var preset in presets.Distinct())
        {
            string path = AssetDatabase.GetAssetPath(preset);
            if (preset == null || string.IsNullOrEmpty(path)) throw new ArgumentException("Project의 Room SO를 선택하세요.");
            targets.Add(path, preset); users.Add(preset, new HashSet<string>());
        }
        if (targets.Count == 0) return new Dictionary<SectorPresetSO, string[]>();
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
            (p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity"))).ToArray();
        // Batch native queries, then split only matching batches. A large art project must not
        // pay for thousands of managed/native calls whenever the Delete button is pressed.
        for (int start = 0; start < paths.Length; start += 128)
            FindDependencyUsers(paths.Skip(start).Take(128).ToArray(), targets, users);
        // Imported dependencies may lag behind an unsaved Inspector change. Inspect only loaded,
        // dirty project data, not every ScriptableObject asset (including unrelated third-party data).
        foreach (var source in Resources.FindObjectsOfTypeAll<ScriptableObject>())
        {
            string path = AssetDatabase.GetAssetPath(source);
            if (!EditorUtility.IsPersistent(source) || !EditorUtility.IsDirty(source) ||
                !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                source.GetType().Assembly != typeof(SectorPresetSO).Assembly && source.GetType().Assembly != typeof(SectorMakerWindow).Assembly) continue;
            using var data = new SerializedObject(source);
            var field = data.GetIterator();
            while (field.Next(true))
                if (field.propertyType == SerializedPropertyType.ObjectReference && field.objectReferenceValue is SectorPresetSO referenced &&
                    referenced != null && source != referenced && users.TryGetValue(referenced, out var references)) references.Add(path);
        }
        return users.ToDictionary(pair => pair.Key, pair => pair.Value.OrderBy(p => p).ToArray());
    }
    private static void FindDependencyUsers(string[] paths, Dictionary<string, SectorPresetSO> targets, Dictionary<SectorPresetSO, HashSet<string>> users)
    {
        var dependencies = AssetDatabase.GetDependencies(paths, false);
        if (!dependencies.Any(targets.ContainsKey)) return;
        if (paths.Length == 1)
        {
            foreach (string dependency in dependencies)
                if (dependency != paths[0] && targets.TryGetValue(dependency, out var preset)) users[preset].Add(paths[0]);
            return;
        }
        int middle = paths.Length / 2;
        FindDependencyUsers(paths.Take(middle).ToArray(), targets, users);
        FindDependencyUsers(paths.Skip(middle).ToArray(), targets, users);
    }
    internal sealed class TrashEntry
    {
        public SectorPresetSO preset;
        public string path, name, guid, reason;
        public string[] users = Array.Empty<string>();
        public bool CanDelete => string.IsNullOrEmpty(reason);
    }
    internal sealed class TrashResult
    {
        public readonly List<string> deleted = new();
        public readonly List<string> skipped = new();
        public readonly List<string> failed = new();
    }
    private static void RequireSafeDeletionContext()
    {
        RequireEditMode();
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null && prefabStage.scene.isDirty ||
            Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                .Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt)
                .Any(s => s.isLoaded && s.isDirty && !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(s)))
            throw new InvalidOperationException("열린 씬 / Prefab의 변경을 먼저 저장하세요. 저장되지 않은 SO 사용처가 있을 수 있습니다.");
    }
    internal static TrashEntry[] PlanTrashPresets(IEnumerable<SectorPresetSO> selection)
    {
        RequireSafeDeletionContext();
        var entries = selection.Where(p => p != null).Distinct().Select(p => new TrashEntry
        {
            preset = p, name = p.name, path = AssetDatabase.GetAssetPath(p)
        }).ToArray();
        foreach (var entry in entries)
        {
            if (!entry.path.StartsWith("Assets/", StringComparison.Ordinal) || !entry.path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                AssetDatabase.LoadMainAssetAtPath(entry.path) != entry.preset) entry.reason = "독립 .asset SO만 삭제 가능";
            else if (EditorUtility.IsDirty(entry.preset)) entry.reason = "저장되지 않은 변경 있음 · 먼저 SO 저장";
            else entry.guid = AssetDatabase.AssetPathToGUID(entry.path);
        }
        var references = ReferencesToMany(entries.Where(e => e.CanDelete).Select(e => e.preset).ToArray());
        foreach (var entry in entries.Where(e => e.CanDelete))
        {
            entry.users = references[entry.preset];
            if (entry.users.Length > 0) entry.reason = "사용 중 · " + entry.users.Length + "곳";
        }
        return entries;
    }
    internal static TrashResult TrashPresets(TrashEntry[] approved)
    {
        // Recheck after confirmation; never delete a previously blocked entry or a replaced asset.
        var result = new TrashResult();
        var candidates = approved.Where(e => e.CanDelete).ToArray();
        foreach (var entry in candidates.Where(e => e.preset == null)) result.skipped.Add(entry.path + " · 이미 제거된 SO");
        var current = PlanTrashPresets(candidates.Select(e => e.preset));
        foreach (var entry in current)
        {
            var original = candidates.First(e => e.preset == entry.preset);
            if (!entry.CanDelete || entry.path != original.path || entry.guid != original.guid)
            { result.skipped.Add(entry.name + " · " + (entry.reason ?? "대상 경로 / GUID 변경")); continue; }
            try
            {
                // Only the standalone SO goes to the recycle bin, never its prefab/profile/stage.
                if (AssetDatabase.MoveAssetToTrash(entry.path)) result.deleted.Add(entry.path);
                else result.failed.Add(entry.path + " · 휴지통 이동 실패");
            }
            catch (Exception ex) { result.failed.Add(entry.path + " · " + ex.Message); }
        }
        foreach (var entry in approved.Where(e => !e.CanDelete)) result.skipped.Add(entry.name + " · " + entry.reason);
        return result;
    }
    internal static bool TrashPreset(SectorPresetSO preset)
    {
        var plan = PlanTrashPresets(new[] { preset });
        if (plan.Length == 0) throw new ArgumentException("삭제할 SO를 선택하세요.");
        if (!plan[0].CanDelete) throw new InvalidOperationException(plan[0].reason + "\n" + string.Join("\n", plan[0].users));
        return TrashPresets(plan).deleted.Count == 1;
    }

    /// <summary>Explicit migration/button only; never called from Scan/OnEnable.</summary>
    internal static SectorPresetSO[] EnsureBasePresets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play Mode 밖에서 등록하세요.");
        EnsureFolder(TemplateRoot);
        var existing = AssetDatabase.FindAssets("t:SectorPresetSO", new[] { TemplateRoot })
            .Select(g => AssetDatabase.LoadAssetAtPath<SectorPresetSO>(AssetDatabase.GUIDToAssetPath(g))).Where(p => p != null).ToList();
        var created = new List<string>();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { SectorMakerCatalog.Root + "Bases" }))
            {
                if (!SectorMakerCatalog.TryPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)), out var prefab)) continue;
                if (existing.Any(p => p.SectorPrefab != null && AssetDatabase.GetAssetPath(p.SectorPrefab) == AssetDatabase.GetAssetPath(prefab))) continue;
                string path = TemplateRoot + "/SectorTemplate_" + prefab.name + ".asset";
                var category = SectorMakerCatalog.Infer(AssetDatabase.GetAssetPath(prefab));
                existing.Add(SectorMakerCatalog.CreatePreset(prefab, path, category == SectorMakerCategory.Boss ? StageRoomType.Boss : StageRoomType.Normal));
                created.Add(path);
            }
            return existing.OrderBy(p => p.name, Comparer<string>.Create(EditorUtility.NaturalCompare)).ToArray();
        }
        catch { foreach (string path in created) AssetDatabase.DeleteAsset(path); throw; }
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    private static void RequireEditMode()
    { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play Mode 밖에서 에셋을 변경하세요."); }
}
