using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>메모리 데이터와 preview scene만 사용. 실제 아이템/상자/세이브는 수정하지 않는다.</summary>
public sealed class LootBalanceTests
{
    private readonly List<Object> _objects = new();
    private Scene _scene;
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private T Make<T>() where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); _objects.Add(o); return o; }
    private static void Set(Object o, string field, int value)
    { using var s = new SerializedObject(o); s.FindProperty(field).intValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    private GameObject Root(string name)
    {
        if (!_scene.IsValid()) _scene = EditorSceneManager.NewPreviewScene();
        var o = new GameObject(name, typeof(RectTransform)); o.SetActive(false);
        SceneManager.MoveGameObjectToScene(o, _scene); return o;
    }
    [TearDown] public void Cleanup()
    {
        if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        foreach (var o in _objects) if (o != null) Object.DestroyImmediate(o);
        _objects.Clear();
    }
    private LootTableSO Table() { var t = Make<LootTableSO>(); t.EnsurePool(); return t; }
    private LootObjectSO Box(LootTableSO table)
    { var b = Make<LootObjectSO>(); b.AssignOwnLootTable(table); b.SetGeneratedSlotRange(1, 1); return b; }
    private LootItemEntry Add<T>(LootTableSO table, int level, bool separateRarities = false) where T : ItemSO
    {
        var item = Make<T>(); item.name = typeof(T).Name + level;
        if (item is ArmorItemSO) Set(item, "_armorClass", level);
        if (item is BasicBulletSO) Set(item, "_tier", level);
        PlayerShopItemRarity rarity = separateRarities ? level <= 2 ? PlayerShopItemRarity.Normal : level <= 4 ? PlayerShopItemRarity.Rare : level <= 6 ? PlayerShopItemRarity.Unique : PlayerShopItemRarity.Legendary : PlayerShopItemRarity.Normal;
        Set(item, "_rarity", (int)rarity);
        var entry = new LootItemEntry { item = item };
        table.Pool.FindTier(rarity).items.Add(entry); return entry;
    }
    private List<LootItemEntry> Ladder<T>(LootTableSO table, bool rarities = false) where T : ItemSO =>
        Enumerable.Range(1, 8).Select(lv => Add<T>(table, lv, rarities)).ToList();

    [Test] public void ArmorAndAmmoUseExactWeightsInAllFiveStagesDespiteRaritySkew()
    {
        var t = Table(); var armor = Ladder<ArmorItemSO>(t, true); var ammo = Ladder<LightBulletSO>(t, true);
        var box = Box(t); box.RarityWeights.normal = 100; box.RarityWeights.rare = 7;
        box.RarityWeights.unique = 2; box.RarityWeights.legendary = .1f;
        string before = EditorJsonUtility.ToJson(t);
        for (int stage = 1; stage <= 5; stage++)
        {
            var d = t.GetStageDistribution(box.RarityWeights, stage);
            for (int level = 1; level <= 8; level++)
            {
                float expected = StageEquipmentLootDistribution.LevelWeight(stage, level) * .5f;
                Assert.That(d.ItemPercent(armor[level - 1]), Is.EqualTo(expected).Within(.0001), $"Armor stage {stage}, LV{level}");
                Assert.That(d.ItemPercent(ammo[level - 1]), Is.EqualTo(expected).Within(.0001), $"Ammo stage {stage}, LV{level}");
            }
        }
        Assert.That(EditorJsonUtility.ToJson(t), Is.EqualTo(before));
    }

    [Test] public void OtherLootAndAmmoCaliberMassStayUnchanged()
    {
        var t = Table(); var light = Ladder<LightBulletSO>(t); var heavy = Ladder<HeavyBulletSO>(t);
        var armor = Ladder<ArmorItemSO>(t); var passive = Add<PassiveItemSO>(t, 0); var box = Box(t);
        var baseline = t.GetStageDistribution(box.RarityWeights, 0);
        var d = t.GetStageDistribution(box.RarityWeights, 4);
        foreach (var family in new[] { light, heavy, armor })
            Assert.That(family.Sum(d.ItemPercent), Is.EqualTo(family.Sum(baseline.ItemPercent)).Within(.0001));
        Assert.That(d.ItemPercent(passive), Is.EqualTo(baseline.ItemPercent(passive)).Within(.0001));
    }

    [Test] public void SameLevelRelativeWeightsAndMissingLevelsAreRenormalized()
    {
        var t = Table(); var a = Add<ArmorItemSO>(t, 1); var b = Add<ArmorItemSO>(t, 1); var c = Add<ArmorItemSO>(t, 2);
        a.useManualShare = b.useManualShare = c.useManualShare = true;
        a.manualSharePercent = 10; b.manualSharePercent = 30; c.manualSharePercent = 60;
        var d = t.GetStageDistribution(Box(t).RarityWeights, 1);
        Assert.That(d.ItemPercent(a), Is.EqualTo(100f / 6).Within(.001));
        Assert.That(d.ItemPercent(b), Is.EqualTo(50).Within(.001));
        Assert.That(d.ItemPercent(c), Is.EqualTo(100f / 3).Within(.001));
    }

    [Test] public void ZeroItemCategoryOrRarityWeightsNeverBecomeEligible()
    {
        var t = Table(); var rows = Ladder<ArmorItemSO>(t, true); var p = Add<PassiveItemSO>(t, 0);
        rows[0].useManualShare = true; rows[0].manualSharePercent = 0;
        var rules = t.Pool.EnsureCategorySettings(PlayerShopItemRarity.Rare);
        rules.useCustomCategoryWeights = true; rules.SetWeight("Armor", 0);
        var box = Box(t); box.RarityWeights.unique = 0;
        var d = t.GetStageDistribution(box.RarityWeights, 3);
        foreach (var row in rows) Assert.That(d.ItemPercent(row), Is.Zero);
        Assert.That(d.ItemPercent(p), Is.EqualTo(100).Within(.001));
    }

    [Test] public void MissingStageCandidatesFailWithoutInventingItemsButZeroSlotsRemainValid()
    {
        var t = Table(); Add<ArmorItemSO>(t, 1); var box = Box(t); var output = new List<LootRollResult>();
        Assert.That(t.HasSlotItems(box.RarityWeights, 5), Is.False);
        Assert.That(t.TryGenerateSlots(new System.Random(1), output, box, 5), Is.False);
        Assert.That(output, Is.Empty);
        box.SetGeneratedSlotRange(0, 0);
        Assert.That(t.TryGenerateSlots(new System.Random(1), output, box, 5), Is.True);
        Assert.That(output, Is.Empty);
    }

    [Test] public void SlotGenerationMatchesPreviewAndNeverDropsOutOfBandOrOverStack()
    {
        var t = Table(); var rows = Ladder<LightBulletSO>(t, true); var box = Box(t);
        foreach (var row in rows) { Set(row.item, "_maxStack", 5); row.minAmount = 3; row.maxAmount = 99; }
        var rng = new System.Random(711); var output = new List<LootRollResult>();
        for (int stage = 1; stage <= 5; stage++)
        {
            int[] counts = new int[8];
            for (int i = 0; i < 6000; i++)
            {
                Assert.That(t.TryGenerateSlots(rng, output, box, stage), Is.True);
                Assert.That(output.Count, Is.EqualTo(1));
                var r = output[0]; counts[(int)((BulletSO)r.Item).Tier - 1]++;
                Assert.That(r.Amount, Is.InRange(3, 5)); Assert.That(r.Rarity, Is.EqualTo(r.Item.Rarity));
            }
            for (int lv = 1; lv <= 8; lv++)
            {
                int expected = StageEquipmentLootDistribution.LevelWeight(stage, lv);
                if (expected == 0) Assert.That(counts[lv - 1], Is.Zero);
                else Assert.That(counts[lv - 1] / 60f, Is.EqualTo(expected).Within(2));
            }
        }
    }

    [Test] public void NonLeveledLootKeepsLegacySeedSequenceIncludingRocket()
    {
        var t = Table(); Add<PassiveItemSO>(t, 0); Add<RocketBulletSO>(t, 1); Add<MedicalItemSO>(t, 0);
        var box = Box(t); var a = new List<LootRollResult>(); var b = new List<LootRollResult>();
        var ra = new System.Random(14); var rb = new System.Random(14);
        for (int i = 0; i < 100; i++)
        {
            t.TryGenerateSlots(ra, a, box); t.TryGenerateSlots(rb, b, box, 5);
            Assert.That(b.Select(r => (r.Item, r.Amount, r.Rarity)), Is.EqualTo(a.Select(r => (r.Item, r.Amount, r.Rarity))));
        }
    }

    [Test] public void HomeAndUnknownStageLeaveAllEightLevelsAvailable()
    {
        var t = Table(); var rows = Ladder<ArmorItemSO>(t); var box = Box(t);
        foreach (int stage in new[] { -1, 0, 6 })
        {
            var d = t.GetStageDistribution(box.RarityWeights, stage);
            foreach (var row in rows) Assert.That(d.ItemPercent(row), Is.EqualTo(12.5).Within(.001));
        }
    }

    [Test] public void EnemyDropsUseSameRuleWithoutChangingTheAuthoredStagePool()
    {
        var t = Table(); Ladder<ArmorItemSO>(t, true);
        var config = Make<LootProgressionConfigSO>(); var enemy = Make<EnemyShooterConfigSO>();
        var pool = new LootStagePool { rarityTiers = t.Pool.rarityTiers };
        var source = new LootSourceDefinition { sourceKind = LootSourceKind.EnemyShooter, enemyShooterConfig = enemy, stagePools = new List<LootStagePool> { pool } };
        typeof(LootProgressionConfigSO).GetField("_sources", Fields).SetValue(config, new List<LootSourceDefinition> { source });
        var valuable = Make<ValuableItemSO>();
        typeof(LootProgressionConfigSO).GetField("_valuableLootItems", Fields).SetValue(config, new List<ItemSO> { valuable });
        typeof(LootProgressionConfigSO).GetField("_valuableLootRollChance", Fields).SetValue(config, 1f);
        string before = JsonUtility.ToJson(config); var rng = new System.Random(213); var output = new List<LootRollResult>();
        int[] counts = new int[8];
        for (int i = 0; i < 6000; i++)
        {
            Assert.That(config.TryRollEnemy(enemy, rng, output, 5), Is.True);
            Assert.That(output.Count, Is.EqualTo(2)); Assert.That(output[1].Item, Is.SameAs(valuable));
            counts[((ArmorItemSO)output[0].Item).ArmorClass - 1]++;
        }
        for (int lv = 1; lv <= 8; lv++) Assert.That(counts[lv - 1] / 60f, Is.EqualTo(StageEquipmentLootDistribution.LevelWeight(5, lv)).Within(2));
        Assert.That(JsonUtility.ToJson(config), Is.EqualTo(before));
    }

    [Test] public void DisabledRepairPreservesDamagedEquipmentAndEmitsNoChangeEvent()
    {
        var equipment = Root("Repair equipment fixture").AddComponent<EntityEquipmentRuntime>();
        var armor = Make<ArmorItemSO>(); var weapon = Make<RangedWeaponSO>();
        equipment.EquipArmor(armor, 20, 80); equipment.EquipWeapon(0, weapon, 1, 15, 70);
        int changed = 0; equipment.OnEquipmentChanged += () => changed++;
        string before = EditorJsonUtility.ToJson(equipment);
        equipment.RepairArmor(90); equipment.RepairArmorToFull();
        Assert.That(equipment.RepairWeaponToFull(0), Is.False);
        Assert.That(equipment.CurrentArmorDurability, Is.EqualTo(20)); Assert.That(equipment.MaxArmorDurability, Is.EqualTo(80));
        Assert.That(equipment.GetWeaponCurrentDurability(0), Is.EqualTo(15)); Assert.That(equipment.GetWeaponMaxDurability(0), Is.EqualTo(70));
        Assert.That(changed, Is.Zero); Assert.That(EditorJsonUtility.ToJson(equipment), Is.EqualTo(before));
        equipment.ApplyArmorAbsorbedDamage(5, 0, 1, 1);
        Assert.That(equipment.CurrentArmorDurability, Is.EqualTo(15), "Damage consumption must still work.");
    }

    [Test] public void EveryInventoryRepairEntryRejectsBeforeTouchingStorageOrEquipment()
    {
        var inventory = Root("Repair inventory fixture").AddComponent<PlayerInventoryRuntime>();
        string before = EditorJsonUtility.ToJson(inventory);
        var methods = typeof(PlayerInventoryRuntime).GetMethods().Where(m => m.Name.StartsWith("TryRepair", StringComparison.Ordinal)).ToArray();
        Assert.That(methods.Length, Is.EqualTo(6));
        foreach (var method in methods)
        {
            var args = method.GetParameters().Select(p => p.ParameterType == typeof(int) ? (object)0 : p.ParameterType == typeof(float).MakeByRefType() ? 99f : (object)null).ToArray();
            Assert.That(method.Invoke(inventory, args), Is.False, method.Name);
            Assert.That(args[args.Length - 2], Is.EqualTo(0f));
            Assert.That(args[args.Length - 1], Is.EqualTo(EquipmentRepairPolicy.DisabledMessage));
        }
        Assert.That(EditorJsonUtility.ToJson(inventory), Is.EqualTo(before));
    }

    [Test] public void ShopCannotRepairAndHidesRepairControlsWhileKeepingSell()
    {
        var root = Root("Repair shop fixture"); var ui = root.AddComponent<PlayerShopPanelUI>();
        var sell = Root("SellSection"); sell.transform.SetParent(root.transform, false); sell.SetActive(true);
        var repair = Root("RepairSection"); repair.transform.SetParent(root.transform, false); repair.SetActive(true);
        var button = repair.AddComponent<Button>(); var group = repair.AddComponent<CanvasGroup>();
        typeof(PlayerShopPanelUI).GetField("_repairButton", Fields).SetValue(ui, button);
        typeof(PlayerShopPanelUI).GetField("_repairActionGroup", Fields).SetValue(ui, group);
        typeof(PlayerShopPanelUI).GetMethod("ApplyRepairAvailability", Fields).Invoke(ui, null);
        Assert.That(repair.activeSelf, Is.False); Assert.That(button.interactable, Is.False);
        Assert.That(group.interactable, Is.False); Assert.That(group.blocksRaycasts, Is.False);
        Assert.That(sell.activeSelf, Is.True); Assert.That(((RectTransform)sell.transform).anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(((RectTransform)sell.transform).anchorMax, Is.EqualTo(Vector2.one));
        string before = EditorJsonUtility.ToJson(ui);
        Assert.That(ui.TryRepairSelectedItem(), Is.False); Assert.That(ui.TryRepairSelectedArmor(), Is.False);
        Assert.That(EditorJsonUtility.ToJson(ui), Is.EqualTo(before));
    }

    [Test] public void AllEquipmentTypesAdvertiseNoRepair()
    {
        foreach (var item in new ItemSO[] { Make<ArmorItemSO>(), Make<RangedWeaponSO>(), Make<GasMaskItemSO>() })
        {
            var selection = new PlayerInventoryItemSelection(PlayerInventoryItemSelectionKind.Inventory, 0, default, -1, item, 1, 10, 80, -1, 10, 80, 10, 80);
            Assert.That(selection.HasItem, Is.True); Assert.That(selection.IsRepairable, Is.False);
        }
    }
}
