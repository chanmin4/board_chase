using UnityEditor;
using UnityEngine;

public sealed partial class SectorMakerWindow
{
    private enum StructureVisionMode { PrefabDefault, Block, PassThrough }
    [SerializeField] private StructureVisionMode _structureVisionMode;
    [SerializeField] private bool _showStructureVisionList;
    [SerializeField] private bool _placeDestructibleCover;
    [SerializeField] private float _coverDurability = 500f;
    private static readonly string[] StructureVisionLabels = { "프리팹 기본값", "차단 ON", "통과 OFF" };

    private void DrawStructureVisionPlacement()
    {
        DrawInternalCombatLockTools();
        if (GUILayout.Button("공용 설정 · 파괴 연기 / 특별 섹터 / 해치")) ProgressionFoundationWindow.Open();
        _placeDestructibleCover = EditorGUILayout.Toggle("이번 배치 · 파괴 엄폐", _placeDestructibleCover);
        if (_placeDestructibleCover) _coverDurability = Mathf.Max(1f, EditorGUILayout.FloatField("엄폐 내구도", _coverDurability));
        _structureVisionMode = (StructureVisionMode)EditorGUILayout.Popup("이번 배치 시야", (int)_structureVisionMode, StructureVisionLabels);
        VisionOccluder setting = _structurePrefab != null ? VisionOccluder.ResolveSetting(_structurePrefab.transform) : null;
        bool blocks = setting != null && setting.BlocksVision && setting.enabled;
        EditorGUILayout.LabelField("프리팹 기본값", blocks ? "[시야 차단 ON]" : "[시야 통과 OFF · 기본]");
        EditorGUILayout.HelpBox("설정 없는 새 프리팹은 OFF. 물리 충돌 레이어는 바꾸지 않습니다.\n‘이번 배치’는 이 배치물에만 적용합니다. 공유 기본값은 아래 버튼으로 저장하세요.", MessageType.None);
        using (new EditorGUI.DisabledScope(_structurePrefab == null || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("프리팹 기본 OFF 저장")) SaveStructureVisionDefault(false);
            if (GUILayout.Button("프리팹 기본 ON 저장")) SaveStructureVisionDefault(true);
        }
    }

    private void SaveStructureVisionDefault(bool blocks)
    {
        string path = AssetDatabase.GetAssetPath(_structurePrefab);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) return;
        if (!EditorUtility.DisplayDialog("공유 프리팹 시야 기본값", _structurePrefab.name + " 기본값을 " +
            (blocks ? "ON" : "OFF") + "으로 저장합니다. 이 프리팹을 사용하는 다른 방에도 반영됩니다.\n배치별 덮어쓰기는 유지됩니다.", "저장", "취소")) return;
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            VisionOccluder setting = root.GetComponent<VisionOccluder>();
            if (setting == null) setting = root.AddComponent<VisionOccluder>();
            setting.enabled = true;
            setting.Configure(blocks, true);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        ShowNotification(new GUIContent("프리팹 시야 기본값 저장 · " + (blocks ? "ON" : "OFF")));
    }

    private void ApplyStructureVisionPlacement(GameObject instance)
    {
        if (_placeDestructibleCover) SetCover(instance, true, _coverDurability);
        if (_structureVisionMode == StructureVisionMode.PrefabDefault) return;
        SetPlacedVision(instance, _structureVisionMode == StructureVisionMode.Block);
    }

    private static void SetPlacedVision(GameObject obj, bool blocks)
    {
        VisionOccluder setting = obj.GetComponent<VisionOccluder>();
        if (setting == null) setting = Undo.AddComponent<VisionOccluder>(obj);
        Undo.RecordObject(setting, "배치물 시야 차단 변경");
        setting.enabled = true;
        setting.Configure(blocks, true);
        EditorUtility.SetDirty(setting);
        PrefabUtility.RecordPrefabInstancePropertyModifications(setting);
        EditorSceneManagerMarkDirty(obj);
    }

    private static void EditorSceneManagerMarkDirty(GameObject obj)
    {
        if (obj.scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(obj.scene);
    }

    private void DrawPlacedStructureVision()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null && IsVisionEditableSelection(selected))
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("선택 · 파괴 엄폐 ON")) SetSelectedCover(true);
                if (GUILayout.Button("선택 · 파괴 엄폐 OFF")) SetSelectedCover(false);
            }
            VisionOccluder setting = VisionOccluder.ResolveSetting(selected.transform);
            var selectedCover=selected.GetComponent<SectorDestructibleCover>();
            if(selectedCover!=null)
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling))
            {
                var data=new SerializedObject(selectedCover);data.Update();
                EditorGUILayout.PropertyField(data.FindProperty("_armorClass"),new GUIContent("엄폐 방탄 등급"));
                EditorGUILayout.PropertyField(data.FindProperty("_lowCover"),new GUIContent("낮은 엄폐 · 탄환 통과"));
                if(data.ApplyModifiedProperties())EditorSceneManagerMarkDirty(selected);
                if(selectedCover.AllowsBulletPass&&GUILayout.Button("낮은 엄폐 시야도 OFF로 적용"))SetPlacedVision(selected,false);
            }
            EditorGUILayout.LabelField("선택 물체 시야", setting != null && setting.BlocksVision && setting.enabled
                ? "[시야 차단 ON] " + selected.name : "[시야 통과 OFF] " + selected.name);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("선택한 물체들 ON")) SetSelectedStructuresVision(true);
                if (GUILayout.Button("선택한 물체들 OFF")) SetSelectedStructuresVision(false);
            }
            EditorGUILayout.HelpBox("Hierarchy에서 여러 물체 선택 가능 · Ctrl+Z로 되돌리기 · 변경 후 방 저장", MessageType.None);
        }
        _showStructureVisionList = EditorGUILayout.Foldout(_showStructureVisionList, "배치물 시야 분류 목록", true);
        if (!_showStructureVisionList || _sectorObjectParent == null) return;
        foreach (Transform child in _sectorObjectParent)
        {
            VisionOccluder setting = VisionOccluder.ResolveSetting(child);
            string label = setting != null && setting.BlocksVision && setting.enabled ? "[차단 ON] " : "[통과 OFF] ";
            if (GUILayout.Button(label + child.name, EditorStyles.miniButton)) Selection.activeGameObject = child.gameObject;
        }
    }

    private bool IsVisionEditableSelection(GameObject obj)
    {
        return _activeSectorRuntime != null && obj != _activeSectorRuntime.gameObject &&
            obj.transform.IsChildOf(_activeSectorRuntime.transform) &&
            obj.GetComponent<SectorContentSlotAnchor>() == null && obj.GetComponent<EnemySpawnPoint>() == null &&
            obj.GetComponentInParent<LootableObject>() == null && obj.GetComponentInParent<WorldItemPickup>() == null &&
            obj.GetComponentInParent<EnemyLootInventoryRuntime>() == null && obj.GetComponentInParent<Enemy>() == null &&
            obj.GetComponentInParent<PlayerCharacter>() == null;
    }

    private void SetSelectedCover(bool enabledForDamage)
    {
        foreach (var obj in Selection.gameObjects)
            if (IsVisionEditableSelection(obj)) SetCover(obj, enabledForDamage, _coverDurability);
    }
    private static void SetCover(GameObject obj, bool enabledForDamage, float durability)
    {
        var cover = obj.GetComponent<SectorDestructibleCover>();
        if (cover == null && !enabledForDamage) return;
        if (cover == null) cover = Undo.AddComponent<SectorDestructibleCover>(obj);
        Undo.RecordObject(cover, "파괴 엄폐 설정");
        cover.Configure(enabledForDamage, durability);
        EditorUtility.SetDirty(cover);
        PrefabUtility.RecordPrefabInstancePropertyModifications(cover);
        EditorSceneManagerMarkDirty(obj);
    }

    private void SetSelectedStructuresVision(bool blocks)
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("배치물 시야 일괄 변경");
        foreach (GameObject obj in Selection.gameObjects)
            if (IsVisionEditableSelection(obj)) SetPlacedVision(obj, blocks);
        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll();
    }
}
