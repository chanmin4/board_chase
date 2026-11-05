using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class LootCategorySelectionTests
{
    private readonly List<Object> _objects = new();
    private string _folder;
    private T Temp<T>() where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); _objects.Add(o); return o; }
    private LootTableSO Table() { var table = Temp<LootTableSO>(); table.EnsurePool(); return table; }
    private LootItemEntry Add<T>(LootTableSO table, PlayerShopItemRarity rarity = PlayerShopItemRarity.Normal) where T : ItemSO
    {
        var item = Temp<T>(); item.name = typeof(T).Name;
        var entry = new LootItemEntry { item = item, minAmount = 1, maxAmount = 1 };
        table.Pool.FindTier(rarity).items.Add(entry); return entry;
    }
    private LootObjectSO Box(LootTableSO table)
    {
        var box = Temp<LootObjectSO>(); box.AssignOwnLootTable(table); box.SetGeneratedSlotRange(1, 1);
        box.RarityWeights.normal = 80; box.RarityWeights.rare = 20; box.RarityWeights.unique = 0; box.RarityWeights.legendary = 0;
        return box;
    }
    private static LootRarityCategorySettings Custom(LootTableSO table, PlayerShopItemRarity rarity, float armor = 70, float passive = 30)
    {
        var rules = table.Pool.EnsureCategorySettings(rarity); rules.useCustomCategoryWeights = true;
        rules.SetWeight("Armor", armor); rules.SetWeight("Passive", passive); return rules;
    }
    [TearDown] public void Cleanup()
    {
        foreach (var obj in _objects) if (obj != null && !AssetDatabase.Contains(obj)) { Undo.ClearUndo(obj); Object.DestroyImmediate(obj); }
        _objects.Clear();
        if (_folder != null && _folder.StartsWith("Assets/__LootCategoryTest_", StringComparison.Ordinal)) AssetDatabase.DeleteAsset(_folder);
    }

    [Test] public void AutoModePreservesExistingMarginalsIncludingMixedManualItems()
    {
        var table = Table(); var a = Add<ArmorItemSO>(table); var b = Add<ArmorItemSO>(table); var c = Add<PassiveItemSO>(table);
        a.useManualShare = true; a.manualSharePercent = 20;
        var tier = table.Pool.FindTier(PlayerShopItemRarity.Normal); string before = JsonUtility.ToJson(table);
        var distribution = table.GetCategorySelection(tier);
        Assert.That(distribution.CategoryPercent("Armor"), Is.EqualTo(60).Within(.001));
        Assert.That(distribution.ItemPercentInTier(a), Is.EqualTo(20).Within(.001));
        Assert.That(distribution.ItemPercentInTier(b), Is.EqualTo(40).Within(.001));
        Assert.That(distribution.ItemPercentInTier(c), Is.EqualTo(40).Within(.001));
        Assert.That(JsonUtility.ToJson(table), Is.EqualTo(before));
    }
    [Test] public void FinalPreviewMultipliesRarityCategoryAndConditionalItemChance()
    {
        var table = Table(); var a = Add<ArmorItemSO>(table); var b = Add<ArmorItemSO>(table); var c = Add<PassiveItemSO>(table);
        Add<PassiveItemSO>(table, PlayerShopItemRarity.Rare);
        a.useManualShare = true; a.manualSharePercent = 25;
        Custom(table, PlayerShopItemRarity.Normal); var box = Box(table); var tier = table.Pool.FindTier(PlayerShopItemRarity.Normal);
        Assert.That(table.GetEffectiveItemPercent(tier, a, box.RarityWeights), Is.EqualTo(14).Within(.001));
        Assert.That(table.GetEffectiveItemPercent(tier, b, box.RarityWeights), Is.EqualTo(42).Within(.001));
        Assert.That(table.GetEffectiveItemPercent(tier, c, box.RarityWeights), Is.EqualTo(24).Within(.001));
        var distribution = table.GetCategorySelection(tier);
        c.useManualShare = true; c.manualSharePercent = 1;
        Assert.That(table.GetCategorySelection(tier).ItemPercentInTier(a), Is.EqualTo(distribution.ItemPercentInTier(a)).Within(.001),
            "An item setting in Passive must not alter Armor's internal shares.");
    }
    [Test] public void CategoryWeightsAreIndependentAcrossRaritiesAndTables()
    {
        var table = Table(); var other = Table();
        foreach (var t in new[] { table, other }) foreach (var rarity in new[] { PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare })
        { Add<ArmorItemSO>(t, rarity); Add<PassiveItemSO>(t, rarity); }
        Custom(table, PlayerShopItemRarity.Normal, 9, 1); Custom(table, PlayerShopItemRarity.Rare, 1, 9);
        Assert.That(table.GetCategorySelection(table.Pool.FindTier(PlayerShopItemRarity.Normal)).CategoryPercent("Armor"), Is.EqualTo(90));
        Assert.That(table.GetCategorySelection(table.Pool.FindTier(PlayerShopItemRarity.Rare)).CategoryPercent("Armor"), Is.EqualTo(10));
        Assert.That(other.GetCategorySelection(other.Pool.FindTier(PlayerShopItemRarity.Normal)).CategoryPercent("Armor"), Is.EqualTo(50));
    }
    [Test] public void MissingAndDisabledCategoriesRenormalizeWithoutFailingFilledSlots()
    {
        var table = Table(); var armor = Add<ArmorItemSO>(table); Add<PassiveItemSO>(table);
        var rules = Custom(table, PlayerShopItemRarity.Normal, 8, 0); rules.SetWeight("Medical", 999);
        var tier = table.Pool.FindTier(PlayerShopItemRarity.Normal); var box = Box(table);
        Assert.That(table.GetCategorySelection(tier).CategoryPercent("Armor"), Is.EqualTo(100));
        Assert.That(table.GetCategorySelection(tier).CategoryPercent("Medical"), Is.Zero);
        box.SetGeneratedSlotRange(3, 3); var results = new List<LootRollResult>();
        Assert.That(table.TryGenerateSlots(new System.Random(5), results, box), Is.True);
        Assert.That(results.Count, Is.EqualTo(3)); Assert.That(results.All(r => r.Item == armor.item), Is.True);
    }
    [Test] public void AllDisabledCategoriesExcludeTierAndNoCandidatesFailWithoutCommittingEmpty()
    {
        var table = Table(); Add<ArmorItemSO>(table); var rare = Add<PassiveItemSO>(table, PlayerShopItemRarity.Rare);
        var rules = Custom(table, PlayerShopItemRarity.Normal, 0, 0); var box = Box(table);
        Assert.That(table.GetEffectiveTierPercent(PlayerShopItemRarity.Normal, box.RarityWeights), Is.Zero);
        Assert.That(table.GetEffectiveTierPercent(PlayerShopItemRarity.Rare, box.RarityWeights), Is.EqualTo(100));
        var results = new List<LootRollResult>(); Assert.That(table.TryGenerateSlots(new System.Random(1), results, box), Is.True);
        Assert.That(results[0].Item, Is.SameAs(rare.item));
        Custom(table, PlayerShopItemRarity.Rare, 0, 0);
        Assert.That(table.HasSlotItems(box.RarityWeights), Is.False); Assert.That(table.HasRollableItems(), Is.False);
        Assert.That(table.TryGenerateSlots(new System.Random(1), results, box), Is.False); Assert.That(results, Is.Empty);
        box.SetGeneratedSlotRange(0, 0); Assert.That(table.TryGenerateSlots(new System.Random(1), results, box), Is.True);
    }
    [Test] public void CategoryWithNoPositiveItemSharesIsExcluded()
    {
        var table = Table(); var a = Add<ArmorItemSO>(table); Add<PassiveItemSO>(table);
        a.useManualShare = true; a.manualSharePercent = 0;
        Custom(table, PlayerShopItemRarity.Normal, 999, 1);
        var distribution = table.GetCategorySelection(table.Pool.FindTier(PlayerShopItemRarity.Normal));
        Assert.That(distribution.CategoryPercent("Armor"), Is.Zero); Assert.That(distribution.CategoryPercent("Passive"), Is.EqualTo(100));
    }
    [Test] public void InvalidWeightsCannotPoisonDistributionAndUnconfiguredCategoriesUseOne()
    {
        var table = Table(); Add<ArmorItemSO>(table); Add<PassiveItemSO>(table); Add<MedicalItemSO>(table);
        var rules = Custom(table, PlayerShopItemRarity.Normal, float.NaN, float.PositiveInfinity);
        Assert.That(rules.GetWeight("Armor"), Is.Zero); Assert.That(rules.GetWeight("Passive"), Is.Zero);
        Assert.That(rules.GetWeight("Medical"), Is.EqualTo(1));
        var distribution = table.GetCategorySelection(table.Pool.FindTier(PlayerShopItemRarity.Normal));
        Assert.That(distribution.CategoryPercent("Medical"), Is.EqualTo(100));
        rules.Find("Armor").weight = float.NegativeInfinity;
        Assert.That(table.GetCategorySelection(table.Pool.FindTier(PlayerShopItemRarity.Normal)).CategoryPercent("Medical"), Is.EqualTo(100));
    }
    private sealed class SequenceRandom : System.Random
    {
        private readonly Queue<double> _values;
        public int Draws;
        public SequenceRandom(params double[] values) { _values = new Queue<double>(values); }
        public override int Next(int minValue, int maxValue) => minValue;
        public override double NextDouble() { Draws++; return _values.Dequeue(); }
    }
    [Test] public void EverySlotDrawsRarityThenCategoryThenItemFreshWithReplacement()
    {
        var table = Table(); Add<ArmorItemSO>(table); var b = Add<ArmorItemSO>(table); var passive = Add<PassiveItemSO>(table);
        Custom(table, PlayerShopItemRarity.Normal); var box = Box(table); box.SetGeneratedSlotRange(2, 2);
        var random = new SequenceRandom(.1, .9, .3, 0, .8, .1, .8, 0);
        var results = new List<LootRollResult>(); Assert.That(table.TryGenerateSlots(random, results, box), Is.True);
        Assert.That(results[0].Item, Is.SameAs(passive.item)); Assert.That(results[1].Item, Is.SameAs(b.item));
        Assert.That(random.Draws, Is.EqualTo(8), "Rarity/category/item/quantity are fresh draws for each slot.");
    }
    [Test] public void SeededFrequencyMatchesPreviewDespiteDifferentCategoryItemCounts()
    {
        var table = Table(); var a = Add<ArmorItemSO>(table); Add<ArmorItemSO>(table); Add<PassiveItemSO>(table);
        Add<PassiveItemSO>(table, PlayerShopItemRarity.Rare); Custom(table, PlayerShopItemRarity.Normal);
        var box = Box(table); var rng = new System.Random(90929); var results = new List<LootRollResult>();
        int armorCount = 0, firstArmorCount = 0;
        for (int i = 0; i < 10000; i++)
        {
            Assert.That(table.TryGenerateSlots(rng, results, box), Is.True);
            if (results[0].Item is ArmorItemSO) armorCount++;
            if (results[0].Item == a.item) firstArmorCount++;
        }
        Assert.That(armorCount / 100f, Is.EqualTo(56).Within(2));
        Assert.That(firstArmorCount / 100f, Is.EqualTo(28).Within(2));
    }
    [Test] public void AuthoringToggleAndWeightSupportUndoWithoutChangingItems()
    {
        var table = Table(); Add<ArmorItemSO>(table); Add<PassiveItemSO>(table); var tier = table.Pool.FindTier(PlayerShopItemRarity.Normal);
        string items = JsonUtility.ToJson(tier);
        LootMakerAuthoring.SetCustomCategoryChances(table, tier, true);
        Assert.That(table.Pool.FindCategorySettings(tier.rarity).GetWeight("Armor"), Is.EqualTo(50));
        Undo.IncrementCurrentGroup(); LootMakerAuthoring.SetCategoryWeight(table, tier, "Armor", 77);
        Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
        Assert.That(table.Pool.FindCategorySettings(tier.rarity).GetWeight("Armor"), Is.EqualTo(50));
        Undo.PerformRedo();
        Assert.That(table.Pool.FindCategorySettings(tier.rarity).GetWeight("Armor"), Is.EqualTo(77));
        tier = table.Pool.FindTier(PlayerShopItemRarity.Normal);
        LootMakerAuthoring.SetCustomCategoryChances(table, tier, false); LootMakerAuthoring.SetCustomCategoryChances(table, tier, true);
        Assert.That(table.Pool.FindCategorySettings(tier.rarity).GetWeight("Armor"), Is.EqualTo(77));
        Assert.That(JsonUtility.ToJson(tier), Is.EqualTo(items));
    }
    [Test] public void CopyPasteIncludesCategoryRulesButNeverSharesThemOrChangesBoxQuality()
    {
        var source = Table(); Add<ArmorItemSO>(source); Custom(source, PlayerShopItemRarity.Normal, 73, 27);
        var snapshot = LootMakerAuthoring.Snapshot(source); _objects.Add(snapshot);
        var target = Table(); var box = Box(target); box.RarityWeights.normal = 91;
        string boxBefore = JsonUtility.ToJson(box);
        LootMakerAuthoring.PasteToBox(snapshot, box);
        var copied = box.LootTable.Pool.FindCategorySettings(PlayerShopItemRarity.Normal);
        Assert.That(copied.GetWeight("Armor"), Is.EqualTo(73)); Assert.That(copied.useCustomCategoryWeights, Is.True);
        source.Pool.FindCategorySettings(PlayerShopItemRarity.Normal).SetWeight("Armor", 1);
        Assert.That(copied.GetWeight("Armor"), Is.EqualTo(73));
        Assert.That(JsonUtility.ToJson(box), Is.EqualTo(boxBefore));
    }
    [Test] public void CategorySettingsPersistThroughAssetSaveAndReload()
    {
        _folder = "Assets/__LootCategoryTest_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(_folder));
        var table = Table(); var item = Add<ArmorItemSO>(table);
        AssetDatabase.CreateAsset(item.item, _folder + "/Armor.asset");
        Custom(table, PlayerShopItemRarity.Normal, 17, 83);
        AssetDatabase.CreateAsset(table, _folder + "/Table.asset"); AssetDatabase.SaveAssetIfDirty(table);
        AssetDatabase.ImportAsset(_folder + "/Table.asset", ImportAssetOptions.ForceUpdate);
        var loaded = AssetDatabase.LoadAssetAtPath<LootTableSO>(_folder + "/Table.asset");
        Assert.That(loaded.Pool.FindCategorySettings(PlayerShopItemRarity.Normal).GetWeight("Armor"), Is.EqualTo(17));
        Assert.That(loaded.GetCategorySelection(loaded.Pool.FindTier(PlayerShopItemRarity.Normal)).CategoryPercent("Armor"), Is.EqualTo(100));
    }
    [Test] public void RuntimeAndMakerUseExactlyTheSameCategoryKeys()
    {
        foreach (var item in new ItemSO[] { Temp<ArmorItemSO>(), Temp<PassiveItemSO>(), Temp<MedicalItemSO>(), Temp<ThrowableUsableItemSO>(),
            Temp<ValuableItemSO>(), Temp<BackpackItemSO>(), Temp<KeyToolItemSO>(), Temp<GasMaskItemSO>(), Temp<CurrencyItemSO>(), Temp<RangedWeaponSO>(), Temp<LightBulletSO>(), Temp<MeleeWeaponSO>() })
            Assert.That(ItemMakerWindow.LootCategoryKey(item), Is.EqualTo(LootItemCategories.GetKey(item)));
    }
}
