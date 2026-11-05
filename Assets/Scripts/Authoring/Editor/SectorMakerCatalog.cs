using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only organization. A category never silently changes a StageDefinition's runtime role.
internal enum SectorMakerCategory { Normal, Boss, Armory, Medical, Vault, Generator, Hatch, Extraction, Start, Home, Other }

[FilePath("ProjectSettings/SectorMakerCatalog.asset", FilePathAttribute.Location.ProjectFolder)]
internal sealed class SectorMakerCatalogSettings : ScriptableSingleton<SectorMakerCatalogSettings>
{
    [Serializable] internal sealed class Entry { public string guid; public SectorMakerCategory category; }
    [SerializeField] private List<Entry> _entries = new();
    internal IEnumerable<string> Guids => _entries.Select(e => e.guid);
    internal SectorMakerCategory Category(string guid, SectorMakerCategory fallback) => _entries.Find(e => e.guid == guid)?.category ?? fallback;
    internal void Set(string guid, SectorMakerCategory category)
    {
        if (string.IsNullOrEmpty(guid)) return;
        var entry = _entries.Find(e => e.guid == guid);
        if (entry == null) { entry = new Entry { guid = guid }; _entries.Add(entry); }
        entry.category = category;
        Save(true);
    }
}

internal static class SectorMakerCatalog
{
    internal const string Root = "Assets/Prefabs/GameScene/Sector/";
    // Keep the serialized Start category index for existing catalog entries.
    // Raid entry is the central Hatch; the old Start-sector library is temporary/legacy content.
    internal static readonly string[] Labels = { "일반방", "보스방", "무기고", "메디컬방", "금고 / 보물방", "발전소", "해치", "탈출구", "임시방", "홈", "기타" };
    internal static SectorPresetSO[] Presets = Array.Empty<SectorPresetSO>();
    internal static StageDefinitionSO[] Stages = Array.Empty<StageDefinitionSO>();
    internal sealed class Room
    {
        public GameObject prefab;
        public string path, guid;
        public SectorMakerCategory category;
        public bool isBase, isLegacy;
        public readonly List<SectorPresetSO> presets = new();
        public readonly List<StageDefinitionSO> stages = new();
    }

    internal static bool TryPrefab(UnityEngine.Object input, out GameObject prefab)
    {
        prefab = null;
        if (input == null) return false;
        if (input is SectorPresetSO preset)
        {
            var runtime = preset.SectorPrefab;
            prefab = runtime != null ? runtime.gameObject : null;
        }
        else prefab = input as GameObject;
        if (input is Component component) prefab = component.gameObject;
        if (prefab == null || !EditorUtility.IsPersistent(prefab)) { prefab = null; return false; }
        string path = AssetDatabase.GetAssetPath(prefab);
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && prefab != null &&
               prefab.GetComponent<SectorRuntime>() != null;
    }

    internal static SectorMakerCategory Infer(string path)
    {
        path = (path ?? "").Replace('\\', '/');
        foreach (var pair in new[] {
            ("Home", SectorMakerCategory.Home), ("Start", SectorMakerCategory.Start),
            ("Medical", SectorMakerCategory.Medical), ("Armory", SectorMakerCategory.Armory),
            ("Treasure", SectorMakerCategory.Vault), ("Vault", SectorMakerCategory.Vault),
            ("Generator", SectorMakerCategory.Generator), ("Hatch", SectorMakerCategory.Hatch),
            ("Extraction", SectorMakerCategory.Extraction), ("Boss", SectorMakerCategory.Boss),
            ("Normal", SectorMakerCategory.Normal) })
            if (path.IndexOf(pair.Item1, StringComparison.OrdinalIgnoreCase) >= 0) return pair.Item2;
        return SectorMakerCategory.Other;
    }

    internal static List<Room> Scan()
    {
        var presets = AssetDatabase.FindAssets("t:SectorPresetSO").Select(g => AssetDatabase.LoadAssetAtPath<SectorPresetSO>(AssetDatabase.GUIDToAssetPath(g))).Where(p => p != null).ToArray();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string directory in new[] { Root + "Rooms", Root + "Bases", Root + "WIP/LegacyRooms" })
            if (AssetDatabase.IsValidFolder(directory))
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { directory })) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        paths.Add(Root + "HomeSector.prefab");
        foreach (var preset in presets) if (preset.SectorPrefab != null) paths.Add(AssetDatabase.GetAssetPath(preset.SectorPrefab));
        foreach (string guid in SectorMakerCatalogSettings.instance.Guids) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        var stages = AssetDatabase.FindAssets("t:StageDefinitionSO").Select(g => AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(AssetDatabase.GUIDToAssetPath(g))).Where(s => s != null).ToArray();
        Presets = presets; Stages = stages;
        var dependencies = stages.ToDictionary(s => s, s => new HashSet<string>(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(s), false)));
        var result = new List<Room>();
        foreach (string path in paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(path) || !TryPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), out var prefab)) continue;
            var room = new Room { path = path, prefab = prefab, guid = AssetDatabase.AssetPathToGUID(path), isBase = path.Contains("/Bases/"), isLegacy = path.Contains("/WIP/") };
            room.category = SectorMakerCatalogSettings.instance.Category(room.guid, Infer(path));
            room.presets.AddRange(presets.Where(p => p.SectorPrefab != null && AssetDatabase.GetAssetPath(p.SectorPrefab) == path));
            room.stages.AddRange(stages.Where(s => room.presets.Any(p => dependencies[s].Contains(AssetDatabase.GetAssetPath(p)))));
            result.Add(room);
        }
        return result.OrderBy(r => r.category).ThenBy(r => r.prefab.name, Comparer<string>.Create(EditorUtility.NaturalCompare)).ToList();
    }

    internal static SectorPresetSO CreatePreset(GameObject prefab, string path, StageRoomType roomType)
    {
        SectorMakerAssets.RequireNewPath(path, ".asset");
        if (!TryPrefab(prefab, out prefab) || roomType == StageRoomType.Empty)
            throw new ArgumentException("새 Sector Preset 경로와 SectorRuntime 프리팹이 필요합니다.");
        // The user explicitly chooses the runtime kind when creating a new preset.
        SectorPresetSO preset = roomType == StageRoomType.Boss
            ? ScriptableObject.CreateInstance<SectorBossBattlePresetSO>() : roomType == StageRoomType.Normal
                ? ScriptableObject.CreateInstance<SectorNormalBattlePresetSO>() : ScriptableObject.CreateInstance<SectorPresetSO>();
        preset.name = System.IO.Path.GetFileNameWithoutExtension(path);
        var data = new SerializedObject(preset);
        data.FindProperty("_displayName").stringValue = prefab.name;
        data.FindProperty("_sectorPrefab").objectReferenceValue = prefab.GetComponent<SectorRuntime>();
        data.FindProperty("_roomType").intValue = (int)roomType;
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(preset, path);
        return preset;
    }

    internal static GameObject CreateVariant(GameObject source, string path)
    {
        SectorMakerAssets.RequireNewPath(path, ".prefab");
        if (!TryPrefab(source, out source))
            throw new ArgumentException("기존 파일을 덮어쓰지 않는 새 Variant 경로가 필요합니다.");
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            instance.name = System.IO.Path.GetFileNameWithoutExtension(path);
            return PrefabUtility.SaveAsPrefabAsset(instance, path);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
