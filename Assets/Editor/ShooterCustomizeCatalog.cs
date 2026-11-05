using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>대상 검색/저장만 담당. 창을 열거나 카테고리를 선택하는 것만으로 에셋을 바꾸지 않는다.</summary>
public static class ShooterCustomizeCatalog
{
    public sealed class Target
    {
        public string Path, Group, Name;
        public EnemyShooterConfigSO Config;
        public GameObject Prefab, Visual;
        public string DisplayName => Config != null && !string.IsNullOrWhiteSpace(Config.DisplayName) ? Config.DisplayName : Name;
        public bool HasVisualRig => Visual != null && Visual.GetComponent<ShooterVisualRig>() != null;
        internal string OutputFolderOverride; // Verification fixtures stay entirely inside their temporary asset folder.
        public bool AllowsRandom => Config != null && Config.AllowsRandomSpawnLook;
        public ShooterCustomizeSO PrefabLook => Visual != null ? Visual.GetComponent<ShooterVisualRig>()?.DefaultCustomize : null;
        public ShooterCustomizeSO CharacterLook => Visual != null ? Visual.GetComponent<ShooterVisualRig>()?.CharacterCustomize : null;
        public string Folder => OutputFolderOverride ?? "Assets/ScriptableObject/GameScene/Character/Shooter/Customization/" +
            SafeName(Name) + "_" + AssetDatabase.AssetPathToGUID(Path).Substring(0, 8);
    }

    public static List<Target> Discover()
    {
        var result = new List<Target>();
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyShooterConfigSO"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/WIP/") || path.Contains("/__")) continue;
            var config = AssetDatabase.LoadAssetAtPath<EnemyShooterConfigSO>(path);
            if (config == null) continue;
            var prefab = config.EnemyShooterPrefab != null ? config.EnemyShooterPrefab.gameObject : null;
            result.Add(new Target { Path = path, Name = config.name, Config = config, Prefab = prefab,
                Visual = ShooterCustomizePreview.ResolveVisualSource(prefab), Group = GroupFor(config, path) });
        }
        AddPrefabs(result, "Assets/Prefabs/GameScene/Shooter/Player", "Player");
        AddPrefabs(result, "Assets/Prefabs/GameScene/Shooter/HomeNPC", "NPC");
        return result.OrderBy(t => GroupOrder(t.Group)).ThenBy(t => t.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string GroupFor(EnemyShooterConfigSO config, string path)
    {
        if (config.EnemyRole == EnemyShooterRole.Boss) return "Boss";
        if (config.EnemyRole == EnemyShooterRole.NamedEnemy) return "Named Enemy";
        if (config.EnemyRole == EnemyShooterRole.BossGuard) return "보스 호위";
        return "일반 적";
    }

    private static int GroupOrder(string group) => group == "Player" ? 0 : group == "NPC" ? 1 :
        group.StartsWith("일반 적", StringComparison.Ordinal) ? 2 : 3;

    private static void AddPrefabs(List<Target> result, string folder, string group)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/WIP/") || Path.GetFileNameWithoutExtension(path).EndsWith("BasePrefab", StringComparison.Ordinal)) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var visual = ShooterCustomizePreview.ResolveVisualSource(prefab);
            if (visual != null) result.Add(new Target { Path = path, Group = group, Name = prefab.name, Prefab = prefab, Visual = visual });
        }
    }

    public static string SafeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(c => invalid.Contains(c) ? '_' : c));
    }

    public static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = folder.Substring(0, folder.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder.Substring(parent.Length + 1));
    }

    public static ShooterCustomizeSO CreateLook(Target target, ShooterCustomizeDraft draft, string name)
    {
        if (target == null || target.Visual == null) throw new InvalidOperationException("유효한 캐릭터를 먼저 선택해 주세요.");
        EnsureFolder(target.Folder);
        var look = ScriptableObject.CreateInstance<ShooterCustomizeSO>();
        draft.CaptureTo(look);
        // IDs must not collide when names repeat or looks are duplicated.
        var serialized = new SerializedObject(look);
        serialized.FindProperty("_customizeId").stringValue = Guid.NewGuid().ToString("N");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(look, AssetDatabase.GenerateUniqueAssetPath(target.Folder + "/" + SafeName(name) + ".asset"));
        return look;
    }

    public static bool IsOwned(Target target, Object asset) => target != null && asset != null &&
        AssetDatabase.GetAssetPath(asset).StartsWith(target.Folder + "/", StringComparison.Ordinal);

    public static string Validate(Target target, ShooterSpawnLookMode mode, IReadOnlyList<ShooterCustomizeSO> looks, IReadOnlyList<float> weights)
    {
        if (target == null || target.Visual == null || target.Visual.GetComponent<ShooterVisualRig>() == null)
            return "대상 프리팹에 ShooterVisualRig가 없습니다. 기존 프리팹 연결을 먼저 확인해 주세요.";
        if (mode == ShooterSpawnLookMode.ExistingPrefab) return null;
        if (!target.AllowsRandom && mode == ShooterSpawnLookMode.RandomPool) return "Player / NPC / Boss / Named Enemy는 고정 외형만 사용합니다.";
        if (looks == null || looks.Count == 0 || looks.Any(l => l == null)) return "유효한 외형 프리셋을 하나 이상 추가해 주세요.";
        if (mode == ShooterSpawnLookMode.Fixed && looks.Count != 1) return "고정 모드는 프리셋을 정확히 하나만 사용합니다.";
        if (mode == ShooterSpawnLookMode.RandomPool && !looks.Select((l, i) =>
            weights != null && i < weights.Count ? weights[i] : 1).Any(w => w > 0 && !float.IsNaN(w) && !float.IsInfinity(w)))
            return "선택 비중이 모두 0입니다. 하나 이상을 0보다 크게 설정해 주세요.";
        return null;
    }

    public static void SaveBinding(Target target, ShooterSpawnLookMode mode, IReadOnlyList<ShooterCustomizeSO> looks, IReadOnlyList<float> weights)
    {
        string error = Validate(target, mode, looks, weights);
        if (error != null) throw new InvalidOperationException(error);
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Play Mode / Prefab Mode 밖에서 저장해 주세요. 열어 둔 프리팹은 자동 저장하지 않습니다.");
        if (target.Config != null)
        {
            var so = new SerializedObject(target.Config);
            var poolProperty = so.FindProperty("_spawnLookPool");
            if (mode == ShooterSpawnLookMode.RandomPool)
            {
                var pool = poolProperty.objectReferenceValue as ShooterCustomizePoolSO;
                // A pool borrowed from another target is copied on write, never edited through.
                if (!IsOwned(target, pool) || IsPoolShared(target, pool))
                {
                    EnsureFolder(target.Folder);
                    pool = ScriptableObject.CreateInstance<ShooterCustomizePoolSO>();
                    AssetDatabase.CreateAsset(pool, AssetDatabase.GenerateUniqueAssetPath(target.Folder + "/SpawnLooks.asset"));
                }
                Undo.RecordObject(pool, "Save spawn appearance weights");
                pool.SetEntries(looks, weights);
                EditorUtility.SetDirty(pool);
                AssetDatabase.SaveAssetIfDirty(pool);
                poolProperty.objectReferenceValue = pool;
            }
            so.FindProperty("_spawnLookMode").intValue = (int)mode;
            so.FindProperty("_fixedSpawnLook").objectReferenceValue = mode == ShooterSpawnLookMode.Fixed ? looks[0] : null;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(target.Config);
        }
        else
        {
            // Edit only the chosen player/NPC variant's nested rig override. Never apply to its base.
            var contents = PrefabUtility.LoadPrefabContents(target.Path);
            try
            {
                var rig = contents.GetComponentInChildren<ShooterVisualRig>(true);
                if (rig == null) throw new InvalidOperationException("프리팹의 ShooterVisualRig를 찾지 못했습니다.");
                rig.SetCharacterCustomize(mode == ShooterSpawnLookMode.Fixed ? looks[0] : null);
                PrefabUtility.SaveAsPrefabAsset(contents, target.Path, out bool saved);
                if (!saved) throw new IOException("프리팹 저장에 실패했습니다.");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
    }

    private static bool IsPoolShared(Target target, ShooterCustomizePoolSO pool) => pool != null &&
        Discover().Any(t => t.Path != target.Path && t.Config != null && t.Config.SpawnLookPool == pool);

    public static bool NeedsIndependentCopy(Target target, ShooterCustomizeSO look)
    {
        if (!IsOwned(target, look)) return true;
        return Discover().Any(t => t.Path != target.Path && (t.PrefabLook == look || t.CharacterLook == look || t.Config != null &&
            (t.Config.FixedSpawnLook == look || t.Config.SpawnLookPool != null && t.Config.SpawnLookPool.Customizes.Contains(look))));
    }
}
