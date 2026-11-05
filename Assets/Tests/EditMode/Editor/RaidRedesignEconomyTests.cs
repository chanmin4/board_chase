using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class RaidRedesignEconomyTests
{
    private readonly List<Object> _objects = new();
    private UnityEngine.Random.State _random;
    [SetUp] public void Setup() { _random = UnityEngine.Random.state; Assert.IsNull(SaveSystem.Active, "Tests must not write an authored save."); }
    [TearDown] public void Cleanup()
    {
        for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
        _objects.Clear(); UnityEngine.Random.state = _random;
    }
    private GameObject Go() { var go = new GameObject("Raid temporary test"); go.SetActive(false); _objects.Add(go); return go; }
    private T Asset<T>() where T : ScriptableObject { var asset = ScriptableObject.CreateInstance<T>(); _objects.Add(asset); return asset; }
    private static void Set(object target, string field, object value)
    {
        var type = target.GetType();
        while (type != null) { var info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic); if (info != null) { info.SetValue(target, value); return; } type = type.BaseType; }
        throw new MissingFieldException(field);
    }
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static PlayerInventoryItemStack Stack(ItemSO item, int amount)
    { var stack = new PlayerInventoryItemStack(); stack.Set(item, amount); return stack; }

    [Test] public void DestroyedEquipmentIsRemovedAndCannotReappearInCorpseLoot()
    {
        var go = Go(); var equipment = go.AddComponent<EntityEquipmentRuntime>();
        var armor = Asset<ArmorItemSO>(); Set(armor, "_maxDurability", 10f);
        var weapon = Asset<RangedWeaponSO>(); Set(weapon, "_maxDurability", 1f); Set(weapon, "_durabilityLossPerFire", 1f);
        var loot = go.AddComponent<EnemyLootInventoryRuntime>(); Set(loot, "_equipmentRuntime", equipment);
        equipment.EquipArmor(armor); equipment.EquipWeapon(weapon);
        loot.Initialize(new EnemyShooterLoadout(new[] { new ShooterLoadoutWeaponStack(0, weapon, 1) }, null, armor));
        int changes = 0; equipment.OnEquipmentChanged += () => changes++;
        equipment.ApplyArmorAbsorbedDamage(10); Assert.IsNull(equipment.CurrentArmor);
        Assert.IsTrue(equipment.ApplyCurrentWeaponFireWear()); Assert.IsNull(equipment.CurrentWeapon);
        loot.SyncEquipmentFromRuntime(); Assert.IsNull(loot.Armor); Assert.IsFalse(loot.WeaponSlots[0].IsAvailable);
        Assert.GreaterOrEqual(changes, 2); Assert.IsFalse(equipment.RepairWeaponToFull(0));
        Assert.IsFalse(EquipmentRepairPolicy.Enabled);
    }

    [Test] public void SubmissionConsumesAggregatedRowsOnceAndInsufficientItemsConsumeNothing()
    {
        var item = Asset<ItemSO>(); var inventory = Go().AddComponent<PlayerInventoryRuntime>();
        Set(inventory, "_items", new List<PlayerInventoryItemStack> { Stack(item, 4) });
        var quest = Asset<QuestDefinitionSO>();
        var a = new QuestObjectiveDefinition(); Set(a, "_objectiveType", QuestObjectiveType.SubmitItem); Set(a, "_targetItem", item); Set(a, "_requiredCount", 2);
        var b = new QuestObjectiveDefinition(); Set(b, "_objectiveType", QuestObjectiveType.SubmitItem); Set(b, "_targetItem", item); Set(b, "_requiredCount", 3);
        Set(quest, "_objectives", new[] { a, b });
        var state = new QuestRuntimeState(); state.Accept(quest);
        var runtime = Go().AddComponent<QuestRuntime>(); Set(runtime, "_states", new List<QuestRuntimeState> { state }); Set(runtime, "_playerInventory", inventory);
        Assert.IsFalse(runtime.TrySubmitItems(quest, out _)); Assert.AreEqual(4, inventory.GetInventoryItemAmount(item));
        CollectionAssert.AreEqual(new[] { 0, 0 }, state.ObjectiveProgress);
        Set(inventory, "_items", new List<PlayerInventoryItemStack> { Stack(item, 6) });
        Assert.IsTrue(runtime.TrySubmitItems(quest, out var error), error); Assert.AreEqual(1, inventory.GetInventoryItemAmount(item));
        Assert.IsTrue(state.IsCompleted); Assert.IsFalse(runtime.TrySubmitItems(quest, out _));
        Assert.AreEqual(1, inventory.GetInventoryItemAmount(item));
        var collect = new QuestObjectiveDefinition(); Set(collect, "_objectiveType", QuestObjectiveType.CollectItem); Set(collect, "_targetItem", item);
        Assert.IsFalse(collect.Matches(null, 0)); Assert.IsFalse(a.Matches(null, 0));
    }

    [Test] public void QuickSlotConditionRoundTripsAndDuplicateOrWrongTypeSlotsAreRejected()
    {
        var medical = AssetDatabase.LoadAssetAtPath<ConsumableItemSO>("Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/BandageLV2.asset");
        var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        var quick = Go().AddComponent<PlayerQuickSlotRuntime>();
        var bindings = Get<PlayerQuickSlotBinding[]>(quick, "_bindings"); bindings[2].SetUsable(medical, 1, 25, 100);
        var saved = new List<PlayerRunInventorySlotSaveData>(); quick.CaptureUsableRunSlots(saved);
        Assert.IsTrue(catalog.TryResolveItem(medical.Guid, out _), "Bandage must be in runtime save catalog.");
        Assert.AreEqual(25, saved[0].weaponCurrentDurability); Assert.IsTrue(quick.CanRestoreUsableRunSlots(saved, catalog));
        bindings[2].Clear(); quick.RestoreUsableRunSlots(saved, catalog);
        Assert.IsTrue(quick.TryGetSnapshot(2, out var snapshot)); Assert.AreEqual(25, snapshot.currentDurability); Assert.AreEqual(100, snapshot.maxDurability);
        saved.Add(saved[0]); Assert.IsFalse(quick.CanRestoreUsableRunSlots(saved, catalog));
        saved.RemoveAt(1); saved[0].slotIndex = 0; Assert.IsFalse(quick.CanRestoreUsableRunSlots(saved, catalog));
    }

    [Test] public void MerchantBudgetCannotGoNegativeAndFailedPayoutCanBeRefunded()
    {
        var funds = new MerchantFunds { balance = 100 };
        Assert.IsFalse(funds.TryReserve(101)); Assert.AreEqual(100, funds.balance);
        Assert.IsFalse(funds.TryReserve(-1)); Assert.IsTrue(funds.TryReserve(75)); Assert.AreEqual(25, funds.balance);
        funds.Credit(75); Assert.AreEqual(100, funds.balance);
        funds.Credit(int.MaxValue); Assert.AreEqual(int.MaxValue, funds.balance);
        funds.Credit(-1); Assert.AreEqual(int.MaxValue, funds.balance);
    }

    [Test] public void MedicalWorldDropAndOldStackUnfoldingPreserveOnePartialUnit()
    {
        var medical = AssetDatabase.LoadAssetAtPath<ConsumableItemSO>("Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/BandageLV1.asset");
        var pickup = Go().AddComponent<WorldItemPickup>();
        Set(pickup, "_logPickupFailures", false);
        pickup.Initialize(medical, 2, 1, 0, -1, 25, 100);
        Assert.AreEqual(25, pickup.WeaponCurrentDurability);
        var inventory = Go().AddComponent<PlayerInventoryRuntime>();
        Assert.IsTrue(inventory.TryPickupMedicalCondition(medical, 2, pickup.WeaponCurrentDurability,
            pickup.WeaponMaxDurability, out var error), error);
        var conditions = new List<float>();
        foreach (var stack in inventory.Items) if (!stack.IsEmpty && stack.Item == medical) conditions.Add(stack.WeaponCurrentDurability);
        CollectionAssert.AreEqual(new[] { 25f, 100f }, conditions);
        Assert.IsFalse(inventory.TryPickupMedicalCondition(medical, 1, 0, 100, out _));
    }

    [Test] public void MerchantSaveIdentityBudgetAndStockDoNotOverwriteHomeUnlocks()
    {
        var home = AssetDatabase.LoadAssetAtPath<ShopConfigSO>("Assets/ScriptableObject/GameScene/Shop/HomeShopConfig.asset");
        var raidConfig = AssetDatabase.LoadAssetAtPath<ShopConfigSO>("Assets/ScriptableObject/GameScene/Shop/RaidEmergencyShopConfig.asset");
        var unlock = Go().AddComponent<ShopUnlockRuntime>(); Set(unlock, "_config", home);
        Go().AddComponent<ShopRunRuntime>();
        var sector = Go().AddComponent<SectorRuntime>(); sector.SetRuntimeInfo(new Vector2Int(2, 3), true, false);
        var shop = Go().AddComponent<SectorShop>(); shop.ConfigureForRaid(sector, raidConfig, 456, 1000, null, null);
        Assert.AreSame(home, Get<ShopConfigSO>(unlock, "_config"));
        Assert.IsFalse(shop.CanInteract); Assert.IsFalse(shop.TryReservePayout(1));
        var funds = Get<MerchantFunds>(shop, "_funds"); Assert.IsTrue(funds.TryReserve(750));
        Assert.IsTrue(shop.DebugTryAddOffer(raidConfig.Products[0].Item, 100, 2, 30, out var error), error);
        var save = new PlayerRunShopStateSaveData(); Assert.IsTrue(shop.CaptureRunState(save)); Assert.AreEqual(250, save.merchantMoney);
        StringAssert.StartsWith("raid:456:", save.shopStateId);
        var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        funds.Credit(999); Assert.IsTrue(shop.TryRestoreRunState(save, catalog, out error), error);
        Assert.AreEqual(250, shop.MerchantMoney); Assert.AreEqual(2, shop.DebugBulletOffers[0].RemainingStock);
        Assert.IsFalse(raidConfig.AllowReroll); Assert.IsFalse(raidConfig.HasTimedRestock);
    }

    [Test] public void AuthoredRaidServicesAndVaultPatrolsAreConnectedWithoutRemovingLegacyPools()
    {
        for (int i = 1; i <= 6; i++)
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"Assets/ScriptableObject/GameScene/Stage/Stage{i}Definition.asset");
            Assert.NotNull(stage.RaidRuntime.merchantConfig); Assert.NotNull(stage.RaidRuntime.merchantOpenChannel);
            Assert.NotNull(stage.RaidRuntime.merchantOverlayChannel);
            var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
            foreach (var product in stage.RaidRuntime.merchantConfig.Products)
                Assert.IsTrue(catalog.TryResolveItem(product.Item.Guid, out _), product.Item.name);
            Assert.NotNull(stage.RaidRuntime.normalWeaponSupply); Assert.NotNull(stage.RaidRuntime.normalMedicalSupply);
            Assert.NotNull(stage.RaidRuntime.FindRoom(RaidRoomRole.Vault).encounter);
            Assert.NotNull(stage.RaidRuntime.FindRoom(RaidRoomRole.Armory)); Assert.NotNull(stage.RaidRuntime.FindRoom(RaidRoomRole.Medical));
        }
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
    public void GearGradesKeepWeaponArchetypeAndAmmoFamily(int stage)
    {
        var table = Resources.Load<RaidGearTableSO>("RaidGearTable"); Assert.NotNull(table);
        var weapon = AssetDatabase.LoadAssetAtPath<RangedWeaponSO>("Assets/ScriptableObject/GameScene/GameItem/Weapon/AssaultRifle1.asset");
        if (weapon == null)
            foreach (var guid in AssetDatabase.FindAssets("AssaultRifle1 t:RangedWeaponSO"))
            { weapon = AssetDatabase.LoadAssetAtPath<RangedWeaponSO>(AssetDatabase.GUIDToAssetPath(guid)); if (weapon != null) break; }
        Assert.NotNull(weapon);
        var original = new EnemyShooterLoadout(new[] { new ShooterLoadoutWeaponStack(0, weapon, 1) },
            new[] { new ShooterLoadoutAmmoStack(0, 0, table.FindAmmo(weapon, 1), 30) }, table.FindArmor(1));
        for (int i = 0; i < 200; i++)
        {
            var changed = table.Adjust(original, stage, false);
            Assert.AreSame(weapon, changed.GetWeapon(0)); Assert.That(changed.Armor.ArmorClass, Is.InRange(stage, stage + 2));
            Assert.IsTrue(weapon.IsCompatibleWith(changed.Bullet)); Assert.That(changed.Bullet.PenetrationClass, Is.InRange(stage, stage + 2));
            Assert.AreEqual(30, changed.AmmoStacks[0].amount);
            var strong = table.Adjust(original, stage, true); Assert.That(strong.Armor.ArmorClass, Is.InRange(stage + 1, stage + 2));
        }
        Assert.IsNull(table.Adjust(new EnemyShooterLoadout(null, null, null), stage, false).Armor);
    }

    [Test] public void PassiveStrengthScalesBonusesAndPenaltiesWithoutMutatingAuthoredValues()
    {
        var passive = Asset<PassiveItemSO>();
        var original = new[] {
            new PlayerStatModifier { type = StatModifierType.FlatAdd, value = -2 },
            new PlayerStatModifier { type = StatModifierType.PercentAdd, value = 10 },
            new PlayerStatModifier { type = StatModifierType.PercentMultiply, value = 1.5f } };
        Set(passive, "_statModifiers", original); Set(passive, "_effectStrength", .5f);
        Assert.AreEqual(-1, passive.StatModifiers[0].value); Assert.AreEqual(5, passive.StatModifiers[1].value);
        Assert.AreEqual(1.25f, passive.StatModifiers[2].value); Assert.AreEqual(10, original[1].value);
        var owned = AssetDatabase.LoadAssetAtPath<PassiveItemSO>("Assets/ScriptableObject/GameScene/GameItem/PassiveItem/Shield.asset");
        Assert.AreEqual(.5f, owned.EffectStrength);
        var inventory = Go().AddComponent<PlayerInventoryRuntime>(); Set(inventory, "_items", new List<PlayerInventoryItemStack> { Stack(owned, 1) });
        inventory.ClearForRunLoss(); Assert.AreEqual(0, inventory.GetInventoryItemAmount(owned));
    }

    [Test] public void AllGradedAmmunitionFamiliesResolveOneThroughEightInSaveCatalog()
    {
        var table = Resources.Load<RaidGearTableSO>("RaidGearTable");
        var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        var families = new HashSet<BulletFamily>();
        foreach (var guid in AssetDatabase.FindAssets("t:RangedWeaponSO", new[] { "Assets/ScriptableObject/GameScene/GameItem/Weapon" }))
        {
            var weapon = AssetDatabase.LoadAssetAtPath<RangedWeaponSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (table.FindAmmo(weapon, 1) == null) continue; // Non-graded rocket/special prototypes stay authored.
            families.Add(weapon.RequiredBulletFamily);
            for (int grade = 1; grade <= 8; grade++)
            {
                var bullet = table.FindAmmo(weapon, grade); Assert.NotNull(bullet, weapon.name + ":" + grade);
                Assert.IsTrue(catalog.TryResolveItem(bullet.Guid, out _), bullet.name);
                Assert.AreEqual(grade, bullet.PenetrationClass);
            }
        }
        Assert.GreaterOrEqual(families.Count, 5);
    }

    [Test] public void DamageAnomalyScalesNonFirearmsAndGrenadeSnapshotsSourceBeforeDeath()
    {
        var source = Go(); var modifier = source.AddComponent<SectorEnemyTraitModifier>();
        modifier.Configure(null, false, true);
        Assert.AreEqual(1.25f, SectorEnemyTraitModifier.NonFirearmAnomalyMultiplier(source.transform));
        var projectile = Go().AddComponent<ThrowableProjectile>();
        projectile.Launch(null, Vector3.zero, Vector3.zero, source, ShooterFaction.Scraper, null, null, null, 1, false);
        Assert.AreEqual(1.25f, Get<float>(projectile, "_healthDamageMultiplier"));
        Object.DestroyImmediate(source);
        Assert.AreEqual(1.25f, Get<float>(projectile, "_healthDamageMultiplier"));
        var other = Go().AddComponent<SectorEnemyTraitModifier>(); other.Configure(null, false, false);
        Assert.AreEqual(1f, SectorEnemyTraitModifier.NonFirearmAnomalyMultiplier(other));
    }
}
