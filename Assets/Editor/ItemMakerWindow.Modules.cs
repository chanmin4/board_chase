using UnityEditor;
using UnityEngine;

public sealed partial class ItemMakerWindow
{
    private static readonly PlayerShopItemRarity[] ModuleRarities = {
        PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare, PlayerShopItemRarity.Unique,
        PlayerShopItemRarity.Legendary, PlayerShopItemRarity.Mythic
    };
    private static readonly string[] ModuleTierNames = {
        "Ⅰ · Normal", "Ⅱ · Rare", "Ⅲ · Unique", "Ⅳ · Legendary", "Ⅴ · Mythic"
    };
    private bool _showAdvancedModule;

    private static void DrawCatalogItemIcon(Rect rect, ItemSO item)
    {
        if (item is PassiveItemSO module) DrawModuleRune(rect, module.Rarity, module.PreviewImage);
        else GUI.DrawTexture(rect, GetItemPreview(item), ScaleMode.ScaleToFit, true);
    }

    private static void DrawModuleRune(Rect area, PlayerShopItemRarity rarity, Sprite icon)
    {
        if (Event.current.type != EventType.Repaint) return;
        float size = Mathf.Min(area.width, area.height);
        if (size <= 0f) return;
        Rect rect = new(area.center.x - size * .5f, area.center.y - size * .5f, size, size);
        GUI.DrawTexture(rect, ModuleRuneStyle.GetFrame(rarity).texture, ScaleMode.ScaleToFit, true);
        if (icon == null) return;
        Rect iconRect = new(rect.x + size * ModuleRuneStyle.IconLeft, rect.y + size * (1f - ModuleRuneStyle.IconTop),
            size * (ModuleRuneStyle.IconRight - ModuleRuneStyle.IconLeft),
            size * (ModuleRuneStyle.IconTop - ModuleRuneStyle.IconBottom));
        if (icon.packed)
        {
            var preview = AssetPreview.GetAssetPreview(icon);
            if (preview != null) GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit, true);
            return;
        }
        Rect region = icon.rect;
        float scale = Mathf.Min(iconRect.width / region.width, iconRect.height / region.height);
        Vector2 half = region.size * scale * .5f;
        Rect fitted = new(iconRect.center - half, half * 2f);
        Rect uv = new(region.x / icon.texture.width, region.y / icon.texture.height,
            region.width / icon.texture.width, region.height / icon.texture.height);
        GUI.DrawTextureWithTexCoords(fitted, icon.texture, uv, true);
    }

    private void DrawModuleWorkbench(PassiveItemSO module)
    {
        EditorGUILayout.LabelField("모듈 제작", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("중앙 효과 Sprite만 지정하면 룬 프레임, 공용 등급 색상, Ⅰ~Ⅴ가 자동으로 합성됩니다. 새 PNG를 만들 필요가 없습니다.", MessageType.Info);
        var serialized = new SerializedObject(module);
        serialized.Update();
        var rarity = serialized.FindProperty("_rarity");
        var category = serialized.FindProperty("_category");
        var icon = serialized.FindProperty("_previewImage");
        EditorGUILayout.PropertyField(icon, new GUIContent("중앙 효과 Sprite"));
        int tier = ModuleRuneStyle.Tier((PlayerShopItemRarity)rarity.intValue) - 1;
        tier = EditorGUILayout.Popup("룬 등급", tier, ModuleTierNames);
        rarity.intValue = (int)ModuleRarities[tier];
        category.enumValueIndex = EditorGUILayout.Popup("장착 분류", category.enumValueIndex, new[] { "전투", "생존" });
        using (new EditorGUILayout.HorizontalScope())
        {
            for (int i = 0; i < ModuleRarities.Length; i++)
            {
                Rect cell = GUILayoutUtility.GetRect(58f, 64f, GUILayout.ExpandWidth(true));
                if (i == tier) EditorGUI.DrawRect(cell, new Color(.12f, .43f, .52f, .38f));
                DrawModuleRune(cell, ModuleRarities[i], icon.objectReferenceValue as Sprite);
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serialized.FindProperty("_effectGroup"), new GUIContent("동일 효과 그룹 ID"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_statModifiers"), new GUIContent("장착 스탯 효과"), true);
        EditorGUILayout.PropertyField(serialized.FindProperty("_effectStrength"), new GUIContent("효과 강도"), true);
        _showAdvancedModule = EditorGUILayout.Foldout(_showAdvancedModule, "고급 · 개체별 랜덤 수치 / 추가 효과", true);
        if (_showAdvancedModule)
        {
            var random = serialized.FindProperty("_randomStatsEnabled");
            EditorGUILayout.PropertyField(random, new GUIContent("개체별 랜덤 사용"));
            if (random.boolValue)
            {
                EditorGUILayout.PropertyField(serialized.FindProperty("_rarityRolls"), new GUIContent("등급별 수치 범위"), true);
                EditorGUILayout.PropertyField(serialized.FindProperty("_allowNumericReroll"), new GUIContent("리롤 허용"));
            }
            EditorGUILayout.PropertyField(serialized.FindProperty("_featureFlags"), new GUIContent("추가 기능"), true);
        }
        if (serialized.ApplyModifiedProperties()) Repaint();
        if (GUILayout.Button("등급 공용 색상 원본 보기")) Selection.activeObject = ItemRarityStyleSO.Active;
        if (GUILayout.Button("모듈 저장")) AssetDatabase.SaveAssetIfDirty(module);
    }

    private static void RegisterModuleForPersistence(ItemSO item)
    {
        string id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item));
        if (string.IsNullOrEmpty(id)) return;
        var serialized = new SerializedObject(item);
        serialized.FindProperty("_guid").stringValue = id;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        if (catalog == null) return;
        Undo.RecordObject(catalog, "Register module for save");
        if (catalog.RegisterSectorItemForAuthoring(item)) EditorUtility.SetDirty(catalog);
    }
}
