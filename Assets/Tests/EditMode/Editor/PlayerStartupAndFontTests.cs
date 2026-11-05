using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

public sealed class PlayerStartupAndFontTests
{
    private const string BagRoot = "Assets/ScriptableObject/GameScene/GameItem/Backpack/";
    private const string PlayerPath = "Assets/Prefabs/GameScene/Shooter/Player/Player.prefab";
    private const string LoadoutPath = "Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/PlayerBasicShooterLoadout.asset";
    private const string MissingLogCharacters = "가게과기는능니다됩또레롯루리면무버벤보상선성세슬습시신아어없에여와용은을이인임적점정중진처태택템토팅표프플하한행활효";
    private readonly List<Object> _assets = new();
    private Scene _scene;
    private PlayerInventoryRuntime _inventory;
    private EntityEquipmentRuntime _equipment;
    private SaveSystem _save;
    private UnityEngine.Random.State _random;

    [SetUp] public void Setup()
    {
        _save = SaveSystem.Active; _random = UnityEngine.Random.state;
        _scene = EditorSceneManager.NewPreviewScene();
        var actor = Go("Temporary startup inventory");
        _equipment = actor.AddComponent<EntityEquipmentRuntime>();
        _inventory = actor.AddComponent<PlayerInventoryRuntime>();
        Set(_inventory, "_pocketCapacity", 2);
        Set(_inventory, "_equipmentRuntime", _equipment);
        Set(_inventory, "_logExternalLootStacking", false);
    }

    [TearDown] public void Cleanup()
    {
        if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        for (int i = _assets.Count - 1; i >= 0; i--) if (_assets[i] != null) Object.DestroyImmediate(_assets[i]);
        _assets.Clear(); UnityEngine.Random.state = _random;
        Assert.AreSame(_save, SaveSystem.Active);
    }

    [Test] public void BackpackAtTheLastRowIsPreparedBeforeCarriedItems()
    {
        var bag = Bag(6); var first = Item(); var second = Item(); var third = Item();
        Snapshot(new[] { new ShooterLoadoutItemStack(first, 1), new(second, 1), new(third, 1), new(bag, 1) });
        Call(_inventory, "ApplyStartingLoadoutInventoryIfNeeded");
        Assert.AreSame(bag, _inventory.EquippedBackpack); Assert.AreEqual(12, _inventory.Capacity);
        foreach (var item in new[] { first, second, third }) Assert.AreEqual(1, _inventory.GetInventoryItemAmount(item));
        Assert.AreEqual(3, _inventory.UsedSlots); Assert.AreEqual(0, _inventory.GetInventoryItemAmount(bag));
    }

    [Test] public void StartingItemsAndBagAreNotDuplicatedOnRepeatedInitialization()
    {
        var bag = Bag(6); var item = Item(); Snapshot(new[] { new ShooterLoadoutItemStack(item, 1), new(bag, 1) });
        Call(_inventory, "ApplyStartingLoadoutInventoryIfNeeded");
        Call(_inventory, "ApplyStartingLoadoutInventoryIfNeeded");
        Assert.AreEqual(1, _inventory.GetInventoryItemAmount(item)); Assert.AreEqual(1, _inventory.UsedSlots);
        Assert.AreSame(bag, _inventory.EquippedBackpack);
    }

    [Test] public void AmmoReadyCallbackPreparesBagBeforeInventoryStart()
    {
        var bag = Bag(6); var bullet = Temp<LightBulletSO>(); Set(bullet, "_maxStack", 120);
        Snapshot(new[] { new ShooterLoadoutItemStack(bag, 1) }, new[] { new ShooterLoadoutAmmoStack(0, 0, bullet, 99) });
        var bullets = _inventory.gameObject.AddComponent<PlayerBulletLoadoutRuntime>();
        Set(bullets, "_equipmentRuntime", _equipment); Set(bullets, "_inventoryRuntime", _inventory);
        Set(bullets, "_statsRuntime", _inventory.gameObject.AddComponent<PlayerStatsRuntime>());
        Set(bullets, "_debugAmmoTransactionTrace", false);
        Call(bullets, "BuildRuntimeSlots"); Call(bullets, "ApplyStartingLoadoutAmmoIfNeeded");
        Assert.AreSame(bag, _inventory.EquippedBackpack); Assert.AreEqual(99, _inventory.GetInventoryItemAmount(bullet));
        Call(_inventory, "ApplyStartingLoadoutInventoryIfNeeded");
        Call(bullets, "ApplyStartingLoadoutAmmoIfNeeded");
        Assert.AreEqual(99, _inventory.GetInventoryItemAmount(bullet)); Assert.AreEqual(2, _inventory.UsedSlots);
    }

    [Test] public void CurrencyAndAmmoSplitAtTheirOwnStackLimits()
    {
        var money = Temp<CurrencyItemSO>();
        var ammo = Temp<LightBulletSO>();
        Assert.IsTrue(_inventory.TryPickup(money, 500001, out var moneyMessage), moneyMessage);
        Assert.IsTrue(_inventory.TryPickup(ammo, 61, out var ammoMessage), ammoMessage);
        CollectionAssert.AreEquivalent(new[] { 500000, 1 },
            _inventory.Items.Where(stack => stack.Item == money).Select(stack => stack.Amount).ToArray());
        CollectionAssert.AreEquivalent(new[] { 60, 1 },
            _inventory.Items.Where(stack => stack.Item == ammo).Select(stack => stack.Amount).ToArray());
    }

    [Test] public void RestoredRunNeverReceivesTheStarterBagAgain()
    {
        var restored = Bag(3); var starter = Bag(6);
        Set(_inventory, "_equippedBackpack", restored);
        Set(_inventory, "_startingLoadoutInventoryApplied", true);
        var snapshot = Snapshot(new[] { new ShooterLoadoutItemStack(starter, 1) });
        Assert.IsTrue(Prepare(snapshot, out var reason), reason);
        Assert.AreSame(restored, _inventory.EquippedBackpack); Assert.AreEqual(9, _inventory.Capacity);
        Assert.IsEmpty(_inventory.Items);
    }

    [Test] public void ConflictingStarterBagsAreRejectedBeforeChangingEquipment()
    {
        var snapshot = Snapshot(new[] { new ShooterLoadoutItemStack(Bag(6), 1), new(Bag(9), 1) });
        Assert.IsFalse(Prepare(snapshot, out var reason)); StringAssert.Contains("more than one", reason);
        Assert.IsNull(_inventory.EquippedBackpack);
    }

    [Test] public void ExistingDifferentBagIsNotSilentlyReplaced()
    {
        var owned = Bag(6); Set(_inventory, "_equippedBackpack", owned);
        var snapshot = Snapshot(new[] { new ShooterLoadoutItemStack(Bag(9), 1) });
        Assert.IsFalse(Prepare(snapshot, out _)); Assert.AreSame(owned, _inventory.EquippedBackpack);
    }

    [Test] public void NormalInventoryCapacityIsStillEnforced()
    {
        var bag = Bag(1); var snapshot = Snapshot(new[] { new ShooterLoadoutItemStack(bag, 1) });
        Assert.IsTrue(Prepare(snapshot, out _)); Assert.AreEqual(7, _inventory.Capacity);
        for (int i = 0; i < 7; i++) Assert.IsTrue(_inventory.TryPickup(Item(), 1, out var message), message);
        Assert.IsFalse(_inventory.TryPickup(Item(), 1, out var reason)); StringAssert.Contains("full", reason);
        Assert.AreEqual(7, _inventory.UsedSlots);
    }

    [Test] public void CurrentLoadoutAndPlayerPrefabReferenceTheSameExistingBag()
    {
        var loadout = AssetDatabase.LoadAssetAtPath<ShooterLoadoutSO>(LoadoutPath);
        Assert.NotNull(loadout);
        var data = new SerializedObject(loadout); var rows = data.FindProperty("_inventoryEntries");
        var bags = new List<BackpackItemSO>();
        for (int i = 0; i < rows.arraySize; i++)
        {
            var item = rows.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue;
            Assert.NotNull(item, "Starting inventory row " + i + " has a broken asset GUID.");
            if (item is BackpackItemSO bag) bags.Add(bag);
        }
        Assert.AreEqual(1, bags.Count);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
        var inventory = prefab.GetComponentInChildren<PlayerInventoryRuntime>(true);
        Assert.NotNull(inventory); Assert.AreSame(bags[0], inventory.EquippedBackpack);
    }

    [Test] public void AllFiveBagsCanBeResolvedForSaveRestoration()
    {
        var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        for (int i = 1; i <= 5; i++)
        {
            var bag = AssetDatabase.LoadAssetAtPath<BackpackItemSO>(BagRoot + $"BackPack_LV{i}.asset");
            Assert.NotNull(bag); Assert.IsTrue(catalog.TryResolveItem(bag.Guid, out var resolved), bag.name);
            Assert.AreSame(bag, resolved);
        }
    }

    [Test] public void AuthoredStarterContentsAndAmmoFitWithoutDiscardingAnything()
    {
        var snapshot = AssetDatabase.LoadAssetAtPath<ShooterLoadoutSO>(LoadoutPath).Roll();
        Set(_equipment, "_lastStartingLoadoutSnapshot", snapshot); Set(_equipment, "_hasLastStartingLoadoutSnapshot", true);
        Assert.IsTrue(Prepare(snapshot, out var reason), reason);
        var counts = new Dictionary<ItemSO, int>();
        foreach (var row in snapshot.inventoryStacks)
            if (row.HasItem && row.item is not BackpackItemSO) counts[row.item] = counts.GetValueOrDefault(row.item) + row.amount;
        foreach (var row in snapshot.ammoStacks)
            if (row.HasBullet) counts[row.bullet] = counts.GetValueOrDefault(row.bullet) + row.amount;
        int required = counts.Sum(p => Mathf.CeilToInt(p.Value / (float)(p.Key is ConsumableItemSO medical && medical.UsesDurability ? 1 : Mathf.Max(1, p.Key.MaxStack))));
        Assert.LessOrEqual(required, _inventory.Capacity,
            $"Starter needs {required} slots but pockets + base Inventory + bag provide {_inventory.Capacity}.");
        foreach (var row in snapshot.ammoStacks) if (row.HasBullet)
            Assert.IsTrue(_inventory.TryPickup(row.bullet, row.amount, out reason), reason);
        Call(_inventory, "ApplyStartingLoadoutInventoryIfNeeded");
        foreach (var entry in counts) Assert.AreEqual(entry.Value, _inventory.GetInventoryItemAmount(entry.Key), entry.Key.name);
        Assert.AreEqual(required, _inventory.UsedSlots);
    }

    [Test] public void BaglessInventoryHasFourSlotsInAdditionToUnchangedPockets()
    {
        Assert.AreEqual(2, _inventory.PocketCapacity); Assert.AreEqual(4, _inventory.BaseInventoryCapacity);
        Assert.AreEqual(0, _inventory.BackpackCapacity); Assert.AreEqual(4, _inventory.InventoryCapacity);
        Assert.AreEqual(6, _inventory.Capacity);
        for (int i = 0; i < 6; i++) Assert.IsTrue(_inventory.TryPickup(Item(), 1, out var reason), reason);
        Assert.IsFalse(_inventory.TryPickup(Item(), 1, out _)); Assert.AreEqual(6, _inventory.UsedSlots);
        Set(_inventory, "_pocketCapacity", 0);
        Assert.AreEqual(4, _inventory.Capacity); // Pocket configuration never changes the base Inventory rule.
    }

    [Test] public void AllFiveAuthoredBagsAddTheirUnchangedSOValue()
    {
        for (int i = 1; i <= 5; i++)
        {
            var bag = AssetDatabase.LoadAssetAtPath<BackpackItemSO>(BagRoot + $"BackPack_LV{i}.asset");
            Assert.NotNull(bag); Set(_inventory, "_equippedBackpack", bag);
            Assert.AreEqual(4 + bag.InventorySlotCapacity, _inventory.InventoryCapacity, bag.name);
            Assert.AreEqual(2 + 4 + bag.InventorySlotCapacity, _inventory.Capacity, bag.name);
        }
    }

    [Test] public void DropAndReequipRetainBaseAndPocketItemsAndTransportOnlyBonusCargo()
    {
        var bag = Bag(2); Assert.IsTrue(_inventory.TryEquipBackpackFromWorld(bag, null, out var reason), reason);
        var items = Enumerable.Range(0, 8).Select(_ => Item()).ToArray();
        foreach (var item in items) Assert.IsTrue(_inventory.TryPickup(item, 1, out reason), reason);
        var firstCargoStack = _inventory.Items[6];
        Assert.IsTrue(_inventory.TryDetachEquippedBackpack(out var dropped, out var cargo, out reason), reason);
        Assert.AreSame(bag, dropped); Assert.AreEqual(4, _inventory.InventoryCapacity);
        Assert.AreEqual(6, _inventory.Capacity); Assert.AreEqual(6, _inventory.UsedSlots);
        Assert.AreEqual(2, cargo.Count); Assert.AreNotSame(firstCargoStack, cargo[0]);
        for (int i = 0; i < 6; i++) Assert.AreSame(items[i], _inventory.Items[i].Item);
        Assert.AreSame(items[6], cargo[0].Item); Assert.AreSame(items[7], cargo[1].Item);
        Assert.IsFalse(_inventory.TryPickup(Item(), 1, out _));
        Assert.IsTrue(_inventory.TryEquipBackpackFromWorld(dropped, cargo, out reason), reason);
        Assert.AreEqual(8, _inventory.UsedSlots);
        for (int i = 0; i < 8; i++) Assert.AreSame(items[i], _inventory.Items[i].Item);
        // Repeating the cycle must neither move permanent slots nor duplicate cargo.
        Assert.IsTrue(_inventory.TryDetachEquippedBackpack(out dropped, out cargo, out reason), reason);
        Assert.IsTrue(_inventory.TryEquipBackpackFromWorld(dropped, cargo, out reason), reason);
        Assert.AreEqual(8, _inventory.UsedSlots);
    }

    [Test] public void BackpackFromInventoryAlsoAddsBonusWithoutConsumingBasicCapacity()
    {
        var bag = Bag(3); Assert.IsTrue(_inventory.TryPickup(bag, 1, out var reason), reason);
        Assert.IsTrue(_inventory.TryEquipInventoryItem(0, out reason), reason);
        Assert.AreEqual(7, _inventory.InventoryCapacity); Assert.AreEqual(9, _inventory.Capacity);
        Assert.AreEqual(0, _inventory.GetInventoryItemAmount(bag)); Assert.AreSame(bag, _inventory.EquippedBackpack);
    }

    [Test] public void BackpackCargoKeepsCurrentAndReducedMaximumCondition()
    {
        var armor = Temp<ArmorItemSO>(); var mask = Temp<GasMaskItemSO>();
        var bag = Bag(2); Assert.IsTrue(_inventory.TryEquipBackpackFromWorld(bag, null, out var reason), reason);
        Call(_inventory, "NormalizeStorage");
        _inventory.Items[6].Set(armor, 1, 12, -1, -1, -1, 40, -1);
        _inventory.Items[7].Set(mask, 1, -1, -1, -1, 15, -1, 50);
        Assert.IsTrue(_inventory.TryDetachEquippedBackpack(out _, out var cargo, out reason), reason);
        Assert.AreEqual(12, cargo[0].ArmorDurability); Assert.AreEqual(40, cargo[0].ArmorMaxDurability);
        Assert.AreEqual(15, cargo[1].GasMaskFilterDurability); Assert.AreEqual(50, cargo[1].GasMaskMaxFilterDurability);
        Assert.IsTrue(_inventory.TryEquipBackpackFromWorld(bag, cargo, out reason), reason);
        Assert.AreEqual(12, _inventory.Items[6].ArmorDurability); Assert.AreEqual(40, _inventory.Items[6].ArmorMaxDurability);
        Assert.AreEqual(15, _inventory.Items[7].GasMaskFilterDurability); Assert.AreEqual(50, _inventory.Items[7].GasMaskMaxFilterDurability);
    }

    [Test] public void DroppingAnEmptyBagDoesNotAllowPartialPickupIntoHiddenSlots()
    {
        var bag = Bag(6); Assert.IsTrue(_inventory.TryEquipBackpackFromWorld(bag, null, out var reason), reason);
        for (int i = 0; i < 5; i++) Assert.IsTrue(_inventory.TryPickup(Item(), 1, out reason), reason);
        var ammo = Temp<LightBulletSO>(); Set(ammo, "_maxStack", 10);
        Assert.IsTrue(_inventory.TryPickup(ammo, 1, out reason), reason);
        Assert.IsTrue(_inventory.TryDetachEquippedBackpack(out _, out _, out reason), reason);
        Assert.IsFalse(_inventory.TryPickup(ammo, 10, out reason));
        Assert.AreEqual(1, _inventory.GetInventoryItemAmount(ammo), "Failed pickup must not partly add ammo.");
        Assert.IsTrue(_inventory.TryPickup(ammo, 9, out reason), reason);
        Assert.AreEqual(10, _inventory.GetInventoryItemAmount(ammo));
        Assert.IsFalse(_inventory.TryPickup(Item(), 1, out _));
    }

    [Test] public void OversizedBackpackPayloadIsRejectedWithoutMutatingPermanentItems()
    {
        var owned = Item(); Assert.IsTrue(_inventory.TryPickup(owned, 1, out var reason), reason);
        var cargo = new[] { new PlayerInventoryItemStack(), new PlayerInventoryItemStack() };
        foreach (var stack in cargo) stack.Set(Item(), 1);
        Assert.IsFalse(_inventory.TryEquipBackpackFromWorld(Bag(1), cargo, out _));
        Assert.IsNull(_inventory.EquippedBackpack); Assert.AreSame(owned, _inventory.Items[0].Item);
        Assert.AreEqual(1, _inventory.UsedSlots); Assert.AreEqual(4, _inventory.InventoryCapacity);
    }

    [Test] public void RunCaptureRestoreAndPruningPreserveBasicAndBonusSlotIndices()
    {
        // Use transient catalog/SO instances only; never touch the real player's save.
        foreach (int bonus in new[] { 0, 3 })
        {
            var bag = bonus > 0 ? Bag(bonus) : null; Set(_inventory, "_equippedBackpack", bag);
            var items = Enumerable.Range(0, 6 + bonus).Select(_ => Item()).ToArray();
            Set(_inventory, "_items", new List<PlayerInventoryItemStack>());
            foreach (var item in items) Assert.IsTrue(_inventory.TryPickup(item, 1, out var reason), reason);
            var catalog = Temp<RuntimeContentCatalogSO>();
            Set(catalog, "_contentAssets", items.Cast<Object>().Concat(bag != null ? new Object[] { bag } : Array.Empty<Object>()).ToArray());
            var save = new PlayerRunStateSaveData { hasSnapshot = true };
            _inventory.CaptureRunState(save);
            var args = new object[] { save, catalog, _inventory.CapacityWithoutBackpack, _equipment.WeaponSlotCount, null };
            int pruned = (int)typeof(PlayerRunStatePersistence).GetMethod("PruneUnavailableReferences",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
            Assert.AreEqual(0, pruned, args[4] as string);
            Assert.AreEqual(items.Length, save.inventorySlots.Count);
            Assert.IsTrue(_inventory.TryRestoreRunState(save, catalog, out var restoreReason), restoreReason);
            Assert.AreEqual(4 + bonus, _inventory.InventoryCapacity); Assert.AreEqual(items.Length, _inventory.UsedSlots);
            for (int i = 0; i < items.Length; i++) Assert.AreSame(items[i], _inventory.Items[i].Item);
        }
    }

    [Test] public void RunLossKeepsFourPermanentInventorySlotsAndSeparatePockets()
    {
        Assert.IsTrue(_inventory.TryEquipBackpackFromWorld(Bag(3), null, out var reason), reason);
        Call(_inventory, "ClearEquippedBackpackForRunLoss");
        Assert.IsNull(_inventory.EquippedBackpack); Assert.AreEqual(4, _inventory.InventoryCapacity);
        Assert.AreEqual(2, _inventory.PocketCapacity); Assert.AreEqual(6, _inventory.Capacity);
    }

    [Test] public void RealPanelDisplaysBasicInventoryWithoutABagAndResizesOnBagChanges()
    {
        var host = Go("Temporary inactive inventory panel", typeof(RectTransform)); host.SetActive(false);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab");
        Assert.NotNull(prefab);
        var panel = Object.Instantiate(prefab, host.transform).GetComponent<PlayerInventoryUI>();
        Assert.NotNull(panel); Set(panel, "_inventoryRuntime", _inventory);
        Call(panel, "EnsureInventorySlots"); AssertPanelSlots(panel, 4);
        Assert.IsTrue(_inventory.TryEquipBackpackFromWorld(Bag(9), null, out var reason), reason);
        Call(panel, "EnsureInventorySlots"); AssertPanelSlots(panel, 13);
        Assert.IsTrue(_inventory.TryDetachEquippedBackpack(out _, out _, out reason), reason);
        Call(panel, "EnsureInventorySlots"); AssertPanelSlots(panel, 4);
    }

    private void AssertPanelSlots(PlayerInventoryUI panel, int inventoryCount)
    {
        var section = Get<RectTransform>(panel, "_inventorySection");
        var pocket = Get<RectTransform>(panel, "_pocketSlotGrid");
        var slots = Get<List<InventoryItemSlotUI>>(panel, "_inventorySlots").Where(s => s.gameObject.activeSelf).ToArray();
        Assert.IsTrue(section.gameObject.activeSelf, "Inventory must remain visible without a backpack.");
        Assert.AreEqual(2, slots.Count(s => s.transform.parent == pocket));
        Assert.AreEqual(inventoryCount, slots.Count(s => s.transform.IsChildOf(section)));
        Assert.AreEqual(2 + inventoryCount, slots.Length);
    }

    [Test] public void GlobalFallbackReferencesAnEmbeddedDynamicKoreanFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/NEXONLv1GothicRegular SDF.asset");
        Assert.That(TMP_Settings.fallbackFontAssets, Does.Contain(font));
        Assert.AreEqual(AtlasPopulationMode.Dynamic, font.atlasPopulationMode);
        Assert.NotNull(font.sourceFontFile); Assert.IsTrue(font.isMultiAtlasTexturesEnabled);
        var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(font.sourceFontFile));
        Assert.IsTrue(importer.includeFontData); Assert.IsFalse(TMP_Settings.warningsDisabled);
    }

    [Test] public void MissingLogCharactersRenderViaFallbackWithoutBoxesOrWarnings()
    {
        var original = TMP_Settings.instance;
        var field = typeof(TMP_Settings).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
        var settings = Object.Instantiate(original); settings.hideFlags = HideFlags.HideAndDontSave; _assets.Add(settings);
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NEXONLv1GothicRegular.ttf");
        // Native atlas/material are new, never clones sharing an authored font's texture.
        var fallback = TMP_FontAsset.CreateFontAsset(source, 48, 4, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, true);
        fallback.hideFlags = HideFlags.HideAndDontSave; _assets.Add(fallback);
        var warnings = new List<string>();
        Application.LogCallback capture = (message, trace, type) =>
        { if (message.Contains("character with Unicode value")) warnings.Add(message); };
        try
        {
            field.SetValue(null, settings); TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset> { fallback };
            var canvas = Go("Temporary font canvas", typeof(RectTransform), typeof(Canvas));
            var node = Go("Details", typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(canvas.transform, false);
            var text = node.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 26; text.rectTransform.sizeDelta = new Vector2(900, 300);
            text.text = MissingLogCharacters + " 리 점검 / 인벤토리 상태 · Loot 123";
            Application.logMessageReceived += capture;
            _ = text.preferredHeight; Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate(true, true);
            Assert.AreEqual(text.text.Length, text.textInfo.characterCount);
            for (int i = 0; i < text.text.Length; i++)
            {
                Assert.AreEqual(text.text[i], text.textInfo.characterInfo[i].character, "Character replaced at " + i);
                if (text.text[i] >= 0xAC00 && text.text[i] <= 0xD7A3)
                    Assert.AreSame(fallback, text.textInfo.characterInfo[i].fontAsset);
            }
            Assert.IsEmpty(warnings, string.Join("\n", warnings));
        }
        finally { Application.logMessageReceived -= capture; field.SetValue(null, original); }
    }

    private GameObject Go(string name, params Type[] components)
    {
        var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave, components);
        SceneManager.MoveGameObjectToScene(go, _scene); return go;
    }
    private T Temp<T>() where T : ScriptableObject
    {
        var item = ScriptableObject.CreateInstance<T>(); item.hideFlags = HideFlags.HideAndDontSave; _assets.Add(item);
        if (item is ItemSO) Set(item, "_guid", Guid.NewGuid().ToString("N"));
        return item;
    }
    private ItemSO Item() { var item = Temp<ValuableItemSO>(); Set(item, "_maxStack", 1); return item; }
    private BackpackItemSO Bag(int capacity) { var bag = Temp<BackpackItemSO>(); Set(bag, "_inventorySlotCapacity", capacity); return bag; }
    private ShooterLoadoutSnapshot Snapshot(ShooterLoadoutItemStack[] items, ShooterLoadoutAmmoStack[] ammo = null)
    {
        var snapshot = new ShooterLoadoutSnapshot((WeaponSO)null, null, -1, ammo, items);
        Set(_equipment, "_lastStartingLoadoutSnapshot", snapshot); Set(_equipment, "_hasLastStartingLoadoutSnapshot", true);
        return snapshot;
    }
    private bool Prepare(ShooterLoadoutSnapshot snapshot, out string reason)
    { var args = new object[] { snapshot, null }; bool ready = (bool)Call(_inventory, "TryPrepareStartingLoadoutBackpack", args); reason = (string)args[1]; return ready; }
    private static object Call(object target, string name, params object[] args) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static T Get<T>(object target, string name) => (T)target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string name, object value)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) { field.SetValue(target, value); return; }
        }
        throw new MissingFieldException(target.GetType().Name, name);
    }
}
