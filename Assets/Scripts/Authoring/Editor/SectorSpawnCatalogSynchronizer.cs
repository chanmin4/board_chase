using System;
using UnityEditor;
using UnityEngine;

/// <summary>배치용 저장 ID와 빌드용 참조 등록만 수행합니다. 씬/프리팹/기존 후보는 변경하지 않습니다.</summary>
[InitializeOnLoad]
internal sealed class SectorSpawnCatalogSynchronizer : AssetPostprocessor
{
    private static bool _queued;
    static SectorSpawnCatalogSynchronizer()
    {
        Queue();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Synchronize();
        };
    }
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (string path in imported)
        {
            if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == typeof(SectorContentProfileSO) || type == typeof(SectorLootObjectSpawnTableSO) ||
                type == typeof(SectorWorldItemSpawnTableSO) || type == typeof(LootObjectSO)) { Queue(); break; }
        }
    }
    internal static void Queue()
    {
        if (_queued) return;
        _queued = true;
        EditorApplication.update += SynchronizeWhenIdle;
    }
    private static void SynchronizeWhenIdle()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= SynchronizeWhenIdle;
        _queued = false;
        Synchronize();
    }
    internal static void Synchronize()
    {
        var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        if (catalog == null) { Debug.LogWarning("[Sector Maker] Resources/RuntimeContentCatalog가 없어 배치 루팅 이어하기 참조를 등록할 수 없습니다."); return; }
        bool catalogChanged = false;
        foreach (string guid in AssetDatabase.FindAssets("t:SectorContentProfileSO"))
        {
            var profile = AssetDatabase.LoadAssetAtPath<SectorContentProfileSO>(AssetDatabase.GUIDToAssetPath(guid));
            bool changed = false;
            foreach (var slot in profile.SlotDefinitions)
            {
                if (slot?.prefabCandidates == null) continue;
                foreach (var row in slot.prefabCandidates)
                {
                    if (row == null || row.prefab == null) continue;
                    string key = catalog.RegisterSectorSpawnForAuthoring(row.prefab, null, out bool added);
                    catalogChanged |= added;
                    if (row.persistenceKey != key) { row.persistenceKey = key; changed = true; }
                }
            }
            if (changed) { EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile); }
        }
        foreach (string guid in AssetDatabase.FindAssets("t:SectorLootObjectSpawnTableSO"))
        {
            var table = AssetDatabase.LoadAssetAtPath<SectorLootObjectSpawnTableSO>(AssetDatabase.GUIDToAssetPath(guid));
            bool changed = false;
            foreach (var row in table.Candidates)
            {
                if (row?.lootObject == null) continue;
                string key = catalog.RegisterSectorSpawnForAuthoring(row.lootObject.WorldPrefab, row.lootObject, out bool added);
                catalogChanged |= added;
                if (row.persistenceKey != key) { row.persistenceKey = key; changed = true; }
            }
            if (changed) { EditorUtility.SetDirty(table); AssetDatabase.SaveAssetIfDirty(table); }
        }
        foreach (string guid in AssetDatabase.FindAssets("t:SectorWorldItemSpawnTableSO"))
        {
            var table = AssetDatabase.LoadAssetAtPath<SectorWorldItemSpawnTableSO>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var row in table.Candidates)
                if (row?.item != null) catalogChanged |= catalog.RegisterSectorItemForAuthoring(row.item);
        }
        if (catalogChanged) { EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog); }
    }
}

// 빌드 직전에 저장 ID의 실제 에셋 참조를 포함합니다. 테스트/검증/씬 조작은 하지 않습니다.
internal sealed class SectorSpawnCatalogBuildPreparation : UnityEditor.Build.IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report) => SectorSpawnCatalogSynchronizer.Synchronize();
}
