using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class SectorMapIconTests
{
    private readonly List<Object> _objects = new();
    private T Keep<T>(T value) where T : Object { _objects.Add(value); return value; }
    private Sprite Icon()
    {
        var texture = Keep(new Texture2D(4, 4));
        return Keep(Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero));
    }
    private static void Set(object owner, string field, object value) => owner.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    [TearDown] public void Cleanup()
    {
        for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
        _objects.Clear();
    }

    [Test] public void OneSharedTypeSpriteUpdatesBothMapsAcrossStagesWithoutPresetEdits()
    {
        var icons = Keep(ScriptableObject.CreateInstance<SectorMapIconSetSO>());
        var mini = Keep(ScriptableObject.CreateInstance<MiniMapUISettingsSO>());
        var full = Keep(ScriptableObject.CreateInstance<FullMapUISettingsSO>());
        Set(mini, "_sectorIcons", icons); Set(full, "_sectorIcons", icons);
        foreach (var sprite in new[] { Icon(), Icon() })
        {
            var data = new SerializedObject(icons); data.FindProperty("_normalCombat").objectReferenceValue = sprite;
            data.ApplyModifiedPropertiesWithoutUndo();
            foreach (var coord in new[] { Vector2Int.zero, new Vector2Int(7, 4) })
            {
                var snapshot = new SectorMapCellSnapshot { coord = coord, isRevealed = true,
                    roomType = StageRoomType.Normal, mapIconKind = SectorMapIconKind.NormalCombat, mapIcon = Icon() };
                Assert.AreSame(sprite, SectorMapIconPolicy.MiniMapIcon(snapshot, mini));
                Assert.AreSame(sprite, SectorMapIconPolicy.SharedIcon(snapshot, full.SectorIcons, null));
            }
        }
    }

    [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, true)]
    public void HiddenOrLockedRoomsNeverRevealAuthoredArt(bool revealed, bool locked)
    {
        var settings = Keep(ScriptableObject.CreateInstance<MiniMapUISettingsSO>());
        var icons = Keep(ScriptableObject.CreateInstance<SectorMapIconSetSO>());
        var fallback = Icon(); Set(icons, "_unknown", fallback); Set(icons, "_office", Icon()); Set(settings, "_sectorIcons", icons);
        var snapshot = new SectorMapCellSnapshot { isRevealed = revealed, isLocked = locked,
            mapIconKind = SectorMapIconKind.Office, mapIcon = Icon() };
        Assert.AreSame(fallback, SectorMapIconPolicy.MiniMapIcon(snapshot, settings));
        Assert.AreSame(fallback, SectorMapIconPolicy.SharedIcon(snapshot, icons, null));
    }

    [TestCase(RaidRoomRole.Merchant, "_merchant")]
    [TestCase(RaidRoomRole.Boss, "_boss")]
    public void BossAndMerchantTypesOverrideOrdinaryCombatKind(RaidRoomRole role, string field)
    {
        var settings = Keep(ScriptableObject.CreateInstance<MiniMapUISettingsSO>());
        var icons = Keep(ScriptableObject.CreateInstance<SectorMapIconSetSO>());
        var marker = Icon(); Set(icons, field, marker); Set(icons, "_normalCombat", Icon()); Set(settings, "_sectorIcons", icons);
        var snapshot = new SectorMapCellSnapshot { isRevealed = true, mapIcon = Icon(), raidRole = role,
            mapIconKind = SectorMapIconKind.NormalCombat,
            roomType = role == RaidRoomRole.Boss ? StageRoomType.Boss : StageRoomType.Normal };
        Assert.AreSame(marker, SectorMapIconPolicy.MiniMapIcon(snapshot, settings));
        Assert.AreSame(marker, SectorMapIconPolicy.SharedIcon(snapshot, icons, null));
    }

    [Test] public void ExitAndUnknownPublicLandmarksDoNotLoseTheirMarker()
    {
        var settings = Keep(ScriptableObject.CreateInstance<MiniMapUISettingsSO>());
        var icons = Keep(ScriptableObject.CreateInstance<SectorMapIconSetSO>()); Set(settings, "_sectorIcons", icons);
        var extraction = Icon(); var generator = Icon();
        Set(icons, "_extraction", extraction); Set(icons, "_generator", generator);
        var snapshot = new SectorMapCellSnapshot { isRevealed = true, isExtraction = true, mapIconKind = SectorMapIconKind.Office, mapIcon = Icon() };
        Assert.AreSame(extraction, SectorMapIconPolicy.MiniMapIcon(snapshot, settings));
        snapshot.isExtraction = false; snapshot.isRevealed = false; snapshot.isLocked = true;
        snapshot.hasPublicRaidMarker = true; snapshot.raidRole = RaidRoomRole.Generator;
        Assert.AreSame(generator, SectorMapIconPolicy.MiniMapIcon(snapshot, settings));
        snapshot.isRevealed = true; snapshot.isLocked = false;
        Assert.AreSame(generator, SectorMapIconPolicy.MiniMapIcon(snapshot, settings));
    }

    [TestCase(SectorMapIconKind.NormalCombat, "_normalCombat")]
    [TestCase(SectorMapIconKind.ThemedCombat, "_themedCombat")]
    [TestCase(SectorMapIconKind.Office, "_office")]
    public void ExteriorExitShowsUnderlyingRoomAndSeparateExit(SectorMapIconKind kind, string field)
    {
        var icons = Keep(ScriptableObject.CreateInstance<SectorMapIconSetSO>());
        var room = Icon(); var exit = Icon(); var unknown = Icon();
        Set(icons, field, room); Set(icons, "_extraction", exit); Set(icons, "_unknown", unknown);
        var snapshot = new SectorMapCellSnapshot { isExtraction = true, isRevealed = true,
            raidRole = RaidRoomRole.Extraction, mapIconKind = kind, hasPublicRaidMarker = true };
        Assert.AreSame(room, SectorMapIconPolicy.SharedIcon(SectorMapIconPolicy.RoomIconSnapshot(snapshot), icons, null));
        Assert.AreSame(exit, SectorMapIconPolicy.ExtractionOverlay(snapshot, icons, null));
        snapshot.isRevealed = false;
        Assert.AreSame(unknown, SectorMapIconPolicy.SharedIcon(SectorMapIconPolicy.RoomIconSnapshot(snapshot), icons, null));
        Assert.AreSame(exit, SectorMapIconPolicy.ExtractionOverlay(snapshot, icons, null));
        snapshot.hasPublicRaidMarker = false;
        Assert.IsNull(SectorMapIconPolicy.ExtractionOverlay(snapshot, icons, null));
        Assert.IsTrue(snapshot.isExtraction, "Display normalization must not mutate the source snapshot.");
    }

    [Test] public void MiniMapPrefabHasAuthoredPairAndReusedCellsClearIt()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GamePlay/MAP/MiniMapCell.prefab");
        Assert.IsNotNull(prefab);
        var root = Keep(Object.Instantiate(prefab));
        var cell = root.GetComponent<MiniMapCellUI>(); var data = new SerializedObject(cell);
        var original = (UnityEngine.UI.Image)data.FindProperty("_icon").objectReferenceValue;
        var left = (UnityEngine.UI.Image)data.FindProperty("_pairedRoomIcon").objectReferenceValue;
        var right = (UnityEngine.UI.Image)data.FindProperty("_extractionIcon").objectReferenceValue;
        Assert.IsNotNull(left); Assert.IsNotNull(right);
        Assert.Less(left.rectTransform.anchorMax.x, right.rectTransform.anchorMin.x);
        var room = Icon(); var exit = Icon();
        cell.SetOpened(Color.white, room, "", "", true, false, "", false);
        cell.SetExtractionOverlay(room, exit);
        Assert.IsFalse(original.enabled); Assert.IsTrue(left.enabled); Assert.IsTrue(right.enabled);
        Assert.AreSame(room, left.sprite); Assert.AreSame(exit, right.sprite);
        cell.SetLocked(Color.white, room);
        Assert.IsTrue(original.enabled); Assert.IsFalse(left.enabled); Assert.IsFalse(right.enabled);
        cell.SetExtractionOverlay(room, exit); cell.SetMissing();
        Assert.IsFalse(left.enabled); Assert.IsFalse(right.enabled);
    }

    [TestCase("office", SectorMapIconKind.Office)] [TestCase("storage", SectorMapIconKind.Warehouse)]
    [TestCase("warehouse", SectorMapIconKind.Warehouse)] [TestCase("security", SectorMapIconKind.Armory)]
    [TestCase("armory", SectorMapIconKind.Armory)] [TestCase("medical", SectorMapIconKind.Medical)]
    [TestCase("vault", SectorMapIconKind.Vault)] [TestCase("server", SectorMapIconKind.Server)]
    public void SpecialPoolIdentifiersResolveTheirGlobalType(string id, SectorMapIconKind expected)
    {
        var room = new StageRoomNode(Vector2Int.zero, StageRoomType.Normal, StageRoomConnectionFlags.None)
            { raidRole = RaidRoomRole.Normal, specialSectorPoolId = id };
        Assert.AreEqual(expected, SectorMapIconPolicy.KindForRoom(room));
    }

    [Test] public void ThemedAndAbsentBossAndMerchantUseTheirCorrectType()
    {
        var room = new StageRoomNode(Vector2Int.zero, StageRoomType.Normal, StageRoomConnectionFlags.None)
            { raidRole = RaidRoomRole.Normal, combatPoolId = "theme" };
        Assert.AreEqual(SectorMapIconKind.ThemedCombat, SectorMapIconPolicy.KindForRoom(room));
        room.combatPoolId = null; room.raidRole = RaidRoomRole.Boss;
        Assert.AreEqual(SectorMapIconKind.NormalCombat, SectorMapIconPolicy.KindForRoom(room));
        room.hasMerchant = true;
        Assert.AreEqual(SectorMapIconKind.Merchant, SectorMapIconPolicy.KindForRoom(room));
    }

    [Test] public void ActualUiSettingsReferenceTheSameSharedTypeSpriteAsset()
    {
        var mini = AssetDatabase.LoadAssetAtPath<MiniMapUISettingsSO>("Assets/ScriptableObject/GamePlay/UI/Map/MiniMapUISettings.asset");
        var full = AssetDatabase.LoadAssetAtPath<FullMapUISettingsSO>("Assets/ScriptableObject/GamePlay/UI/Map/FullMapUISettings.asset");
        Assert.IsNotNull(mini.SectorIcons); Assert.AreSame(mini.SectorIcons, full.SectorIcons);
    }

    [Test] public void ClearingSharedFieldClearsBothMapsAndOnlyUnlinkedProfilesUseLegacyFallback()
    {
        var icons = Keep(ScriptableObject.CreateInstance<SectorMapIconSetSO>());
        var mini = Keep(ScriptableObject.CreateInstance<MiniMapUISettingsSO>()); Set(mini, "_sectorIcons", icons);
        Set(mini, "_bossIcon", Icon());
        var snapshot = new SectorMapCellSnapshot { isRevealed = true, roomType = StageRoomType.Boss };
        Assert.IsNull(SectorMapIconPolicy.MiniMapIcon(snapshot, mini));
        Assert.IsNull(SectorMapIconPolicy.SharedIcon(snapshot, icons, Icon()));
        Set(mini, "_sectorIcons", null);
        Assert.AreSame(mini.BossIcon, SectorMapIconPolicy.MiniMapIcon(snapshot, mini));
    }
}
