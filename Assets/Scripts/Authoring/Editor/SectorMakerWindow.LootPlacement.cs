using UnityEditor;
using UnityEngine;

public sealed partial class SectorMakerWindow
{
    [SerializeField] private SectorLootObjectSpawnTableSO _lootSpawnTableDraft;
    [SerializeField] private SectorWorldItemSpawnTableSO _itemSpawnTableDraft;
    [SerializeField] private float _slotChanceDraft = 60f;
    [SerializeField] private bool _slotAlwaysDraft;
    [SerializeField] private bool _editSpawnTable;
    [SerializeField] private GameObject _lootPlacementPreview;

    private int PlacementTabIndex => _mode == PlacementMode.EnemySpawnPoint ? 3 :
        _mode == PlacementMode.ContentSlotAnchor ? (_contentSlotKind == SectorContentSlotKind.WorldItem ? 2 : 1) : 0;

    private void DrawLootPlacementToolbar()
    {
        if (_mode == PlacementMode.StageTopology) SetMode(PlacementMode.FixedSectorStructure);
        int index = GUILayout.Toolbar(PlacementTabIndex,
            new[] { "구조물 배치", "루팅 오브젝트", "아이템 스폰", "적 위치" }, GUILayout.Height(30));
        if (index == PlacementTabIndex) return;
        _paintingEnabled = false; _workbenchTab = 0;
        _lootPlacementPreview = null;
        if (index == 0) SetMode(PlacementMode.FixedSectorStructure);
        else if (index == 3) SetMode(PlacementMode.EnemySpawnPoint);
        else
        {
            _contentSlotKind = index == 2 ? SectorContentSlotKind.WorldItem : SectorContentSlotKind.LootContainer;
            _contentSlotId = index == 2 ? "item_01" : "loot_01";
            _slotChanceDraft = index == 2 ? 35f : 60f;
            SetMode(PlacementMode.ContentSlotAnchor);
        }
    }

    private void DrawLootPlacementDraft()
    {
        if (_contentSlotKind == SectorContentSlotKind.SpecialInteractable)
        {
            EditorGUILayout.HelpBox("NPC/상점 등 특수 상호작용 자리는 기존 프리팹 후보 설정을 유지합니다. 배치 후 자리를 선택해 후보를 넣으세요.", MessageType.None);
            return;
        }
        bool item = _contentSlotKind == SectorContentSlotKind.WorldItem;
        EditorGUILayout.LabelField(item ? "새 아이템 자리 기본값" : "새 루팅 자리 기본값", EditorStyles.boldLabel);
        _slotAlwaysDraft = EditorGUILayout.Toggle("항상 생성 · 상한 제외", _slotAlwaysDraft);
        using (new EditorGUI.DisabledScope(_slotAlwaysDraft))
            _slotChanceDraft = EditorGUILayout.Slider("자리 등장 확률 (%)", _slotChanceDraft, 0f, 100f);
        if (item)
        {
            _itemSpawnTableDraft = (SectorWorldItemSpawnTableSO)EditorGUILayout.ObjectField("아이템 테이블", _itemSpawnTableDraft, typeof(SectorWorldItemSpawnTableSO), false);
            DrawSpawnTableTools(_itemSpawnTableDraft, true, table => _itemSpawnTableDraft = (SectorWorldItemSpawnTableSO)table,
                _slotAlwaysDraft ? 100 : _slotChanceDraft);
        }
        else
        {
            _lootSpawnTableDraft = (SectorLootObjectSpawnTableSO)EditorGUILayout.ObjectField("루팅 오브젝트 테이블", _lootSpawnTableDraft, typeof(SectorLootObjectSpawnTableSO), false);
            DrawSpawnTableTools(_lootSpawnTableDraft, false, table => _lootSpawnTableDraft = (SectorLootObjectSpawnTableSO)table,
                _slotAlwaysDraft ? 100 : _slotChanceDraft);
        }
        EditorGUILayout.HelpBox("확률 실패 = 이 자리 비움. 성공 = 테이블에서 한 후보 선택.\n한 아이템 자리는 한 종류/한 스택입니다. 선택한 자리 설정은 아래에서 따로 수정하세요.", MessageType.None);
    }

    private void ApplyLootPlacementDraft(SectorContentSlotDefinition definition)
    {
        if (definition == null || _contentSlotKind == SectorContentSlotKind.SpecialInteractable) return;
        definition.spawnChancePercent = Mathf.Clamp(_slotChanceDraft, 0, 100);
        definition.alwaysSpawn = _slotAlwaysDraft;
        if (_contentSlotKind == SectorContentSlotKind.WorldItem) definition.worldItemTable = _itemSpawnTableDraft;
        else if (_lootSpawnTableDraft != null) definition.lootObjectTable = _lootSpawnTableDraft;
        var data = new SerializedObject(_contentProfile);
        data.FindProperty("_useAuthoredLootPlacement").boolValue = true;
        data.ApplyModifiedProperties();
        SectorSpawnCatalogSynchronizer.Queue();
    }

    private void DrawLootSpawnLimits()
    {
        if (_contentProfile == null) return;
        using (new EditorGUI.DisabledScope(!CanEditContentPlan()))
        {
            var data = new SerializedObject(_contentProfile);
            EditorGUILayout.PropertyField(data.FindProperty("_useAuthoredLootPlacement"), new GUIContent("이 방은 배치 루팅만 사용"));
            EditorGUILayout.HelpBox("켜면 자동 보급상자 추가/방 전체 루팅 테이블 덮어쓰기를 사용하지 않습니다. 기존 고정 배치물은 보존합니다.", MessageType.None);
            EditorGUILayout.PropertyField(data.FindProperty("_limitLootObjects"), new GUIContent("랜덤 루팅 오브젝트 수 제한"));
            if (data.FindProperty("_limitLootObjects").boolValue)
                data.FindProperty("_maxLootObjects").intValue = Mathf.Max(0, EditorGUILayout.IntField("최대 루팅 오브젝트", data.FindProperty("_maxLootObjects").intValue));
            EditorGUILayout.PropertyField(data.FindProperty("_limitWorldItems"), new GUIContent("랜덤 아이템 개체 수 제한"));
            if (data.FindProperty("_limitWorldItems").boolValue)
                data.FindProperty("_maxWorldItems").intValue = Mathf.Max(0, EditorGUILayout.IntField("최대 아이템 개체", data.FindProperty("_maxWorldItems").intValue));
            data.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("최소 생성 보장 없음. 제한 0 = 랜덤 생성 금지. 항상 생성/기존 고정 배치는 상한 제외.\n상한 초과 시 성공한 자리 중 무작위로 남기므로 최종 등장률은 낮아질 수 있습니다.", MessageType.None);
        }
    }

    private void DrawPlacedLootSelection()
    {
        var selected = Selection.activeGameObject;
        if (selected == null || _activeSectorRuntime == null || !selected.transform.IsChildOf(_activeSectorRuntime.transform)) return;
        var anchor = selected.GetComponent<SectorContentSlotAnchor>();
        if (anchor == null) return;
        using (new EditorGUI.DisabledScope(!CanEditContentPlan())) DrawAnchorDefinition(anchor);
    }

    private void DrawSpawnTableTools(ScriptableObject table, bool worldItem, System.Action<ScriptableObject> select, float chance)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("새 테이블…"))
                CreateOrCopySpawnTable(null, worldItem, select);
            using (new EditorGUI.DisabledScope(table == null))
            {
                if (GUILayout.Button("복사…")) CreateOrCopySpawnTable(table, worldItem, select);
                if (GUILayout.Button("Project에서 보기")) EditorGUIUtility.PingObject(table);
            }
        }
        if (table == null) return;
        _editSpawnTable = EditorGUILayout.Foldout(_editSpawnTable, "테이블 후보 · 가중치 편집 (공유 SO)", true);
        if (!_editSpawnTable) return;
        SectorSpawnTableGUI.Draw(table, chance, prefab => { _lootPlacementPreview = prefab; SceneView.RepaintAll(); });
        if (_lootPlacementPreview != null)
        {
            EditorGUILayout.LabelField("Scene 크기 미리보기: " + _lootPlacementPreview.name, EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("미리보기 끄기")) { _lootPlacementPreview = null; SceneView.RepaintAll(); }
        }
    }

    private void CreateOrCopySpawnTable(ScriptableObject source, bool worldItem, System.Action<ScriptableObject> select)
    {
        string path = EditorUtility.SaveFilePanelInProject(source != null ? "스폰 테이블 독립 복사" : "새 스폰 테이블",
            source != null ? source.name + "_Copy" : worldItem ? "WorldItemSpawnTable" : "LootObjectSpawnTable", "asset", "새 파일 경로를 선택하세요.");
        if (string.IsNullOrEmpty(path)) return;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) { ShowNotification(new GUIContent("기존 파일을 덮어쓰지 않습니다.")); return; }
        ScriptableObject table = source != null ? Instantiate(source) : worldItem ?
            (ScriptableObject)CreateInstance<SectorWorldItemSpawnTableSO>() : CreateInstance<SectorLootObjectSpawnTableSO>();
        AssetDatabase.CreateAsset(table, path); select(table);
        _editSpawnTable = true; SectorSpawnCatalogSynchronizer.Queue();
    }

    private void DrawLootPlacementPreview(Vector3 position)
    {
        if (_mode != PlacementMode.ContentSlotAnchor || _lootPlacementPreview == null || Event.current.type != EventType.Repaint) return;
        var prefab = _lootPlacementPreview.transform;
        Vector3 scale = Vector3.Scale(prefab.localScale, _contentSlotParent != null ? _contentSlotParent.lossyScale : Vector3.one);
        Matrix4x4 placement = Matrix4x4.TRS(position, Quaternion.Euler(0, _rotationY, 0), scale);
        foreach (var renderer in _lootPlacementPreview.GetComponentsInChildren<Renderer>(true))
        {
            using (new Handles.DrawingScope(new Color(0.4f, 1f, 0.6f, 0.8f),
                placement * prefab.worldToLocalMatrix * renderer.transform.localToWorldMatrix))
                Handles.DrawWireCube(renderer.localBounds.center, renderer.localBounds.size);
        }
    }

    private void SaveLootSpawnTables()
    {
        var saved = new System.Collections.Generic.HashSet<ScriptableObject>();
        foreach (var slot in _contentProfile.SlotDefinitions)
        {
            if (slot == null) continue;
            if (slot.lootObjectTable != null && saved.Add(slot.lootObjectTable)) AssetDatabase.SaveAssetIfDirty(slot.lootObjectTable);
            if (slot.worldItemTable != null && saved.Add(slot.worldItemTable)) AssetDatabase.SaveAssetIfDirty(slot.worldItemTable);
        }
    }
}

internal static class SectorSpawnTableGUI
{
    internal static void Draw(ScriptableObject table, float existenceChance = 100f, System.Action<GameObject> preview = null)
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            var data = new SerializedObject(table);
            var rows = data.FindProperty("_candidates");
            bool worldItem = table is SectorWorldItemSpawnTableSO;
            string sourceField = worldItem ? "item" : "lootObject";
            double total = 0;
            for (int i = 0; i < rows.arraySize; i++)
            {
                var row = rows.GetArrayElementAtIndex(i);
                if (IsValidSource(row.FindPropertyRelative(sourceField).objectReferenceValue))
                    total += Mathf.Max(0, row.FindPropertyRelative("weight").intValue);
            }
            int remove = -1, copy = -1;
            for (int i = 0; i < rows.arraySize; i++)
            {
                var row = rows.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(row.FindPropertyRelative(sourceField), new GUIContent(worldItem ? "아이템" : "루팅 오브젝트"));
                    var source = row.FindPropertyRelative(sourceField).objectReferenceValue;
                    if (source is ItemSO item && item.PreviewImage != null)
                        GUILayout.Label(AssetPreview.GetAssetPreview(item.PreviewImage) ?? AssetPreview.GetMiniThumbnail(item.PreviewImage), GUILayout.Width(36), GUILayout.Height(36));
                    row.FindPropertyRelative("weight").intValue = Mathf.Max(0, EditorGUILayout.IntField("가중치 (0 = 제외)", row.FindPropertyRelative("weight").intValue));
                    double percent = IsValidSource(source) && total > 0 ? row.FindPropertyRelative("weight").intValue / total * 100d : 0d;
                    EditorGUILayout.LabelField($"선택 비율 {percent:0.##}% · 자리 최종 추첨 {percent * existenceChance / 100d:0.##}% (상한 전)", EditorStyles.wordWrappedMiniLabel);
                    if (source != null && !IsValidSource(source)) EditorGUILayout.HelpBox(worldItem ? "World Visual Prefab이 필요합니다." : "World Prefab 안에 LootableObject가 있어야 합니다.", MessageType.Warning);
                    if (preview != null && IsValidSource(source) && GUILayout.Button("이 후보 배치 크기 미리보기"))
                        preview(source is ItemSO previewItem ? previewItem.WorldVisualPrefab : ((LootObjectSO)source).WorldPrefab);
                    if (worldItem)
                    {
                        int limit = source is ItemSO selectedItem ? selectedItem.MaxStack : int.MaxValue;
                        if (source is ArmorItemSO || source is GasMaskItemSO || source is RangedWeaponSO ||
                            source is ConsumableItemSO medical && medical.UsesDurability) limit = 1;
                        var min = row.FindPropertyRelative("minAmount"); var max = row.FindPropertyRelative("maxAmount");
                        min.intValue = Mathf.Clamp(EditorGUILayout.IntField("최소 수량", min.intValue), 1, limit);
                        max.intValue = Mathf.Clamp(EditorGUILayout.IntField("최대 수량", max.intValue), min.intValue, limit);
                        EditorGUILayout.PropertyField(row.FindPropertyRelative("overrideCondition"), new GUIContent("내구도 범위 설정"));
                        if (row.FindPropertyRelative("overrideCondition").boolValue)
                        {
                            var minCondition = row.FindPropertyRelative("minConditionPercent"); var maxCondition = row.FindPropertyRelative("maxConditionPercent");
                            minCondition.floatValue = EditorGUILayout.Slider("최소 상태 (%)", minCondition.floatValue, 0, 100);
                            maxCondition.floatValue = EditorGUILayout.Slider("최대 상태 (%)", maxCondition.floatValue, minCondition.floatValue, 100);
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("후보 복사")) copy = i;
                        if (GUILayout.Button("후보 삭제")) remove = i;
                        if (GUILayout.Button("↑") && i > 0) rows.MoveArrayElement(i, i - 1);
                        if (GUILayout.Button("↓") && i + 1 < rows.arraySize) rows.MoveArrayElement(i, i + 1);
                    }
                }
            }
            if (remove >= 0) rows.DeleteArrayElementAtIndex(remove);
            else if (copy >= 0) rows.InsertArrayElementAtIndex(copy);
            if (GUILayout.Button("+ 후보 추가"))
            {
                rows.arraySize++;
                var row = rows.GetArrayElementAtIndex(rows.arraySize - 1);
                row.FindPropertyRelative(sourceField).objectReferenceValue = null;
                row.FindPropertyRelative("weight").intValue = 1;
                if (worldItem)
                {
                    row.FindPropertyRelative("minAmount").intValue = row.FindPropertyRelative("maxAmount").intValue = 1;
                    row.FindPropertyRelative("overrideCondition").boolValue = false;
                    row.FindPropertyRelative("minConditionPercent").floatValue = row.FindPropertyRelative("maxConditionPercent").floatValue = 100;
                }
            }
            if (total <= 0) EditorGUILayout.HelpBox("유효한 후보와 0보다 큰 가중치가 필요합니다. 빈 테이블은 생성하지 않습니다.", MessageType.Info);
            if (data.ApplyModifiedProperties()) SectorSpawnCatalogSynchronizer.Queue();
        }
    }

    private static bool IsValidSource(Object source) => source is ItemSO item ? item.WorldVisualPrefab != null :
        source is LootObjectSO loot && loot.WorldPrefab != null && loot.WorldPrefab.GetComponentInChildren<LootableObject>(true) != null;
}

[CustomEditor(typeof(SectorLootObjectSpawnTableSO))]
internal sealed class SectorLootObjectSpawnTableEditor : Editor
{
    public override void OnInspectorGUI() => SectorSpawnTableGUI.Draw((ScriptableObject)target);
}
[CustomEditor(typeof(SectorWorldItemSpawnTableSO))]
internal sealed class SectorWorldItemSpawnTableEditor : Editor
{
    public override void OnInspectorGUI() => SectorSpawnTableGUI.Draw((ScriptableObject)target);
}
