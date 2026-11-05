using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>순수 외형 장식 정의의 등록/검색. Item SO는 만들지 않고, 기존 캐릭터 외형을 갱신하지 않는다.</summary>
[InitializeOnLoad]
public static class ShooterCustomizeAccessoryCatalog
{
    public const string PrefabFolder = "Assets/Prefabs/GameScene/Shooter/Accesory";
    public const string DefinitionsFolder = "Assets/ScriptableObject/GameScene/Character/Visual/Accessories";
    private static List<ShooterVisualAttachmentSO> _definitions;
    static ShooterCustomizeAccessoryCatalog() { EditorApplication.projectChanged += () => _definitions = null; }

    public static IReadOnlyList<ShooterVisualAttachmentSO> Definitions => _definitions ??= AssetDatabase.FindAssets("t:ShooterVisualAttachmentSO")
        .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ShooterVisualAttachmentSO>)
        .Where(d => d != null).OrderBy(d => d.name, StringComparer.OrdinalIgnoreCase).ToList();

    public static ShooterVisualAttachmentSO Find(GameObject prefab) => prefab == null ? null : Definitions.FirstOrDefault(d => d != null && d.VisualPrefab == prefab);

    public static bool IsEligible(GameObject prefab)
    {
        if (prefab == null || !EditorUtility.IsPersistent(prefab) || !PrefabUtility.IsPartOfPrefabAsset(prefab)) return false;
        string path = AssetDatabase.GetAssetPath(prefab);
        if (path.Contains("/WIP/") || path.Contains("/GasMask/") || path.Contains("/GameItemObject/")) return false;
        // A gameplay equipment prefab must be tried through its Item SO, never registered as permanent cosmetic data.
        foreach (var behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour != null && new[] { "WeaponView", "ItemPickup", "LootableObject", "Shooter", "EnemyShooter", "PlayerShooter", "ShooterVisualRig", "EntityEquipmentRuntime" }
                .Contains(behaviour.GetType().Name)) return false;
        return prefab.GetComponentInChildren<Renderer>(true) != null;
    }

    public static ShooterVisualSocketId SuggestedSocket(string name)
    {
        if (name.StartsWith("Eyes_", StringComparison.OrdinalIgnoreCase)) return ShooterVisualSocketId.Eyes;
        if (name.StartsWith("Mouth_", StringComparison.OrdinalIgnoreCase)) return ShooterVisualSocketId.Mouth;
        if (name.StartsWith("Body_", StringComparison.OrdinalIgnoreCase)) return ShooterVisualSocketId.Body;
        if (name.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Sunglass", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("VR_", StringComparison.OrdinalIgnoreCase) >= 0) return ShooterVisualSocketId.Eyes;
        return ShooterVisualSocketId.Hair;
    }

    public static List<GameObject> SourcePrefabs() => AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder })
        .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
        .Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(IsEligible).ToList();

    public static ShooterVisualAttachmentSO Register(GameObject prefab, string outputFolder = DefinitionsFolder)
    {
        if (!IsEligible(prefab)) throw new ArgumentException("순수 장식 프리팹만 등록할 수 있습니다. WIP·기능 장비는 제외됩니다.");
        var existing = Find(prefab); if (existing != null) return existing; // Never overwrite a calibrated default.
        EnsureFolder(outputFolder);
        var definition = ScriptableObject.CreateInstance<ShooterVisualAttachmentSO>();
        definition.Configure(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)), prefab, SuggestedSocket(prefab.name),
            ShooterVisualLocalTransform.FromTransform(prefab.transform), ShooterVisualOccupancy.None, ShooterVisualEquipmentKind.None);
        string path = AssetDatabase.GenerateUniqueAssetPath(outputFolder + "/" + prefab.name + ".asset");
        AssetDatabase.CreateAsset(definition, path); AssetDatabase.SaveAssetIfDirty(definition); _definitions = null;
        return definition;
    }

    public static void RegisterCurrentFolder()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        int made = 0;
        foreach (var prefab in SourcePrefabs()) { if (Find(prefab) == null) made++; Register(prefab); }
        Debug.Log("[Shooter accessories] " + made + " new definitions; " + SourcePrefabs().Count + " active cosmetic prefabs. " + DefinitionsFolder);
    }

    public static ShooterCustomizeAttachment CopyDefault(ShooterVisualAttachmentSO definition)
    {
        if (definition == null || definition.VisualPrefab == null) throw new ArgumentException("유효한 액세서리 SO가 필요합니다.");
        return definition.CreatePlacement();
    }

    public static void SaveDefault(ShooterVisualAttachmentSO definition, ShooterCustomizeAttachment placement)
    {
        if (definition == null || definition.VisualPrefab != placement.visualPrefab) throw new ArgumentException("같은 장식의 기본 정의만 수정할 수 있습니다.");
        Undo.RecordObject(definition, "Save Accessory Default Placement");
        definition.Configure(definition.AttachmentId, placement.visualPrefab, placement.socket, placement.localTransform,
            placement.occupiedRegions, placement.hideWhenEquipped);
        EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
    }

    private static void EnsureFolder(string path)
    {
        if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..")) throw new ArgumentException("정의 저장 경로는 Assets 내부여야 합니다.");
        string current = "Assets";
        foreach (string part in path.Substring(7).Split('/'))
        { string next = current + "/" + part; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part); current = next; }
    }
}
