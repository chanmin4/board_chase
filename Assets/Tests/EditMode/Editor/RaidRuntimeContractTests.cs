using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class RaidRuntimeContractTests
{
    private readonly List<Object> _created = new();
    private Random.State _randomState;
    [SetUp] public void Setup() => _randomState = Random.state;
    [TearDown] public void Cleanup()
    {
        for (int i = _created.Count - 1; i >= 0; i--) if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();
        Random.state = _randomState;
    }

    [Test]
    public void ShieldVestAndBaseRollIndependentlyAndOnlyAbsorbedDamageWearsEachLayer()
    {
        var go = Go();
        var damageable = go.AddComponent<Damageable>();
        damageable.ApplyMaxHealthFromStats(1000, true);
        var stats = Asset<PlayerStatsConfigSO>();
        Set(stats, "_baseArmorClass", 1);
        Set(damageable, "_statConfig", stats);
        var equipment = go.AddComponent<EntityEquipmentRuntime>();
        var armor = Asset<ArmorItemSO>();
        Set(armor, "_armorClass", 3);
        Set(armor, "_maxDurability", 1000f);
        equipment.EquipArmor(armor);
        Set(damageable, "_equipmentRuntime", equipment);
        var shield = go.AddComponent<ShieldArmorRuntime>();
        Random.InitState(125);
        float s = ArmorPenetrationDamageCurve.RollMultiplier(3, 0);
        float v = ArmorPenetrationDamageCurve.RollMultiplier(3, 0);
        float b = ArmorPenetrationDamageCurve.RollMultiplier(1, 0);
        Random.InitState(125);
        damageable.ReceiveAnAttack(100, null, 0);
        Assert.That(damageable.CurrentHealth, Is.EqualTo(1000 - 100 * s * v * b).Within(.001f));
        Assert.That(shield.CurrentDurability, Is.EqualTo(100 - 100 * (1 - s)).Within(.001f));
        Assert.That(equipment.CurrentArmorDurability, Is.EqualTo(1000 - 100 * s * (1 - v)).Within(.001f));
    }

    [Test]
    public void EqualPenetrationAndBrokenShieldPassFullDamage()
    {
        var shield = Go().AddComponent<ShieldArmorRuntime>();
        Assert.AreEqual(100, shield.Absorb(100, 3, 1));
        Assert.AreEqual(100, shield.CurrentDurability);
        shield.Restore(0);
        Assert.AreEqual(100, shield.Absorb(100, 0, 1));
    }

    [Test]
    public void ShieldBreakAffectsFollowingHitAndFixedMeleeNeverWearsIt()
    {
        var go = Go();
        var target = go.AddComponent<Damageable>();
        target.ApplyMaxHealthFromStats(1000, true);
        var shield = go.AddComponent<ShieldArmorRuntime>();
        target.ReceiveFixedMeleeAttack(5);
        Assert.AreEqual(995, target.CurrentHealth);
        Assert.AreEqual(100, shield.CurrentDurability);
        shield.Restore(.01f);
        Assert.Less(shield.Absorb(100, 0, 1), 100);
        Assert.AreEqual(0, shield.CurrentDurability);
        Assert.AreEqual(100, shield.Absorb(100, 0, 1));
    }

    [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
    public void InvalidOrPausedClockDoesNotAdvance(float delta)
    {
        var state = new RaidRunSaveData { active = true, timerEnabled = true, remainingSeconds = 10 };
        RaidRuntime.TickClock(state, delta);
        Assert.AreEqual(10, state.remainingSeconds);
        Assert.IsFalse(state.gasReleased);
    }

    [Test]
    public void ClockAndGasSurviveReloadAndStopAfterExtraction()
    {
        var state = new RaidRunSaveData { active = true, timerEnabled = true, remainingSeconds = 10 };
        RaidRuntime.TickClock(state, 4);
        state = JsonUtility.FromJson<RaidRunSaveData>(JsonUtility.ToJson(state));
        Assert.AreEqual(6, state.remainingSeconds);
        RaidRuntime.TickClock(state, 20);
        Assert.IsTrue(state.gasReleased);
        Assert.AreEqual(0, state.remainingSeconds);
        state.extractionCommitted = true;
        state.remainingSeconds = 7;
        RaidRuntime.TickClock(state, 3);
        Assert.AreEqual(7, state.remainingSeconds);
    }

    [Test]
    public void SortPreservesStackInstancesConditionsBoundariesAndStableRarityOrder()
    {
        var normal = Asset<ArmorItemSO>();
        var rare = Asset<ArmorItemSO>(); Set(rare, "_rarity", PlayerShopItemRarity.Rare);
        var unique = Asset<ArmorItemSO>(); Set(unique, "_rarity", PlayerShopItemRarity.Unique);
        var legendary = Asset<ArmorItemSO>(); Set(legendary, "_rarity", PlayerShopItemRarity.Legendary);
        var a = Stack(normal, 2, 31);
        var b = Stack(rare, 1, 42);
        var c = Stack(unique, 1, 53);
        var d = Stack(legendary, 1, 64);
        var e = Stack(rare, 1, 75);
        var pocket = Stack(normal, 1, 17);
        var stacks = new List<PlayerInventoryItemStack> { pocket, null, b, a, c, null, d, e };
        Assert.IsTrue(PlayerInventoryRuntime.SortStackRange(stacks, 2, 6, true));
        CollectionAssert.AreEqual(new[] { pocket, null, d, c, b, e, a, null }, stacks);
        Assert.AreEqual(31, a.ArmorDurability);
        Assert.AreEqual(2, a.Amount);
        Assert.AreEqual(42, b.ArmorDurability);
        Assert.IsFalse(PlayerInventoryRuntime.SortStackRange(stacks, 2, 6, true));
    }

    [Test]
    public void CompactKeepsOriginalOrderWithoutMerging()
    {
        var item = Asset<ArmorItemSO>();
        var a = Stack(item, 1, 21);
        var b = Stack(item, 1, 64);
        var empty = new PlayerInventoryItemStack();
        var stacks = new List<PlayerInventoryItemStack> { null, a, empty, b };
        Assert.IsTrue(PlayerInventoryRuntime.SortStackRange(stacks, 0, 4, false));
        CollectionAssert.AreEqual(new[] { a, b, null, empty }, stacks);
        Assert.AreEqual(21, stacks[0].ArmorDurability);
        Assert.AreEqual(64, stacks[1].ArmorDurability);
    }

    [Test]
    public void GeneratorRejectsNoBatteryConsumesOnceAndRestoresPower()
    {
        var definition = Asset<StageDefinitionSO>();
        var battery = Asset<ValuableItemSO>();
        definition.RaidRuntime.generatorBattery = battery;
        Assert.IsTrue(RaidMapGenerator.TryGenerate(11, 1, 7, Vector2Int.left,
            new RaidMapSettings(), out var layout, out _));
        var manager = Go().AddComponent<SectorStateManager>();
        manager.EnsureInitialized();
        var sector = Go().AddComponent<SectorRuntime>();
        sector.SetRuntimeInfo(layout.generatorRoomCoord, true, false);
        Set(manager, "_stageMapConfigured", true);
        Set(manager, "_runtimeSectors", new List<SectorRuntime> { sector });
        Set(manager, "_sectorByCoord", new Dictionary<Vector2Int, SectorRuntime> { [sector.Coord] = sector });
        Set(manager, "_coordBySector", new Dictionary<SectorRuntime, Vector2Int> { [sector] = sector.Coord });
        manager.SetPlayerCurrentSector(sector, false);
        var raid = Go().AddComponent<RaidRuntime>();
        raid.Initialize(definition, layout, manager);
        raid.MarkReady();
        var playerGo = Go(); playerGo.SetActive(false);
        var inventory = playerGo.AddComponent<PlayerInventoryRuntime>();
        Assert.IsFalse(raid.TryPowerGenerator(inventory));
        Assert.IsFalse(raid.State.generatorPowered);
        Set(inventory, "_items", new List<PlayerInventoryItemStack> { Stack(battery, 2) });
        Assert.IsTrue(raid.TryPowerGenerator(inventory));
        Assert.IsFalse(raid.TryPowerGenerator(inventory));
        Assert.AreEqual(1, inventory.GetInventoryItemAmount(battery));
        Assert.IsTrue(raid.State.hatchRevealed);
        var save = new RunSaveData();
        raid.Capture(save);
        Assert.IsTrue(save.raid.generatorPowered);
        raid.Initialize(definition, layout, manager, save.raid);
        Assert.IsFalse(raid.TryPowerGenerator(inventory));
        Assert.AreEqual(1, inventory.GetInventoryItemAmount(battery));
        Assert.IsFalse(raid.TryExtract(sector, RaidStationKind.Hatch, inventory));
        Assert.IsFalse(raid.TryExtract(sector, RaidStationKind.SpecialExtractionWip, inventory));
    }

    [Test]
    public void RolledEmptyContainerRemainsEmptyAndUnlockedAfterRestore()
    {
        var container = Go().AddComponent<LootableObject>();
        var lockable = container.gameObject.AddComponent<LockableLootObject>();
        var state = new RaidLootContainerSaveData { key = "3,2/test", rolled = true, unlocked = true };
        container.BindRaidState(state.key, 1, state, null);
        Assert.IsTrue(container.IsRolled);
        Assert.IsFalse(lockable.IsLocked);
        Assert.AreEqual(0, container.CaptureRaidState().items.Count);
    }

    [TestCase(0)] [TestCase(100)]
    public void ExitRulesAreIndependentOfBossAndRejectDuplicatesAndWrongCoordinates(int bossChance)
    {
        Assert.IsTrue(RaidMapGenerator.TryGenerate(3, 1, 7, Vector2Int.left,
            new RaidMapSettings { bossSpawnPercent = bossChance }, out var layout, out _));
        var state = new RaidRunSaveData { active = true, layout = layout };
        Assert.IsTrue(RaidRuntime.AllowsExtraction(state, layout.startSectorCoord, RaidStationKind.StartReturn));
        Assert.IsTrue(RaidRuntime.AllowsExtraction(state, layout.extractionSectorCoord, RaidStationKind.NormalExtraction));
        Assert.IsFalse(RaidRuntime.AllowsExtraction(state, layout.hatchRoomCoord, RaidStationKind.Hatch));
        Assert.IsFalse(RaidRuntime.AllowsExtraction(state, layout.bossRoomCoord, RaidStationKind.NormalExtraction));
        state.generatorPowered = true;
        Assert.IsTrue(RaidRuntime.AllowsExtraction(state, layout.hatchRoomCoord, RaidStationKind.Hatch));
        Assert.IsFalse(RaidRuntime.AllowsExtraction(state, layout.hatchRoomCoord, RaidStationKind.SpecialExtractionWip));
        state.extractionCommitted = true;
        Assert.IsFalse(RaidRuntime.AllowsExtraction(state, layout.extractionSectorCoord, RaidStationKind.NormalExtraction));
        Assert.IsFalse(RaidRuntime.AllowsExtraction(state, layout.startSectorCoord, RaidStationKind.StartReturn));
    }

    [Test]
    public void ContainerRestorePreservesSparseSlotsAndItemCondition()
    {
        var armor = Asset<ArmorItemSO>();
        Set(armor, "_guid", "test-raid-armor");
        var catalog = Asset<RuntimeContentCatalogSO>();
        Set(catalog, "_contentAssets", new Object[] { armor });
        var saved = new RaidLootContainerSaveData { key = "test", rolled = true };
        saved.items.Add(new PlayerRunInventorySlotSaveData
        {
            slotIndex = 5, itemGuid = armor.Guid, amount = 1, armorDurability = 21, armorMaxDurability = 74
        });
        var container = Go().AddComponent<LootableObject>();
        container.BindRaidState(saved.key, 11, saved, catalog);
        var captured = container.CaptureRaidState();
        Assert.AreEqual(1, captured.items.Count);
        Assert.AreEqual(5, captured.items[0].slotIndex);
        Assert.AreEqual(21, captured.items[0].armorDurability);
        Assert.AreEqual(74, captured.items[0].armorMaxDurability);
    }

    [Test]
    public void Es3RoundTripPreservesRaidAndContainerState()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BoardChaseRaidTest-" + Guid.NewGuid() + ".es3");
        // The isolated runner has ES3 binaries, not its authoring Resources folder.
        var defaultsField = typeof(ES3Settings).GetField("_defaultSettingsScriptableObject", BindingFlags.Static | BindingFlags.NonPublic);
        object originalDefaults = defaultsField.GetValue(null);
        var defaults = Asset<ES3Defaults>();
        // Discover the test host's assemblies as well as the normal Unity names.
        // Isolated verification may compile the same production sources under a different name.
        defaults.settings.assemblyNames = new[] { typeof(ES3).Assembly.GetName().Name, typeof(RunSaveData).Assembly.GetName().Name };
        defaultsField.SetValue(null, defaults);
        try
        {
            Assert.IsTrue(RaidMapGenerator.TryGenerate(993, 1, 7, Vector2Int.left,
                new RaidMapSettings(), out var layout, out _));
            var run = new RunSaveData
            {
                raid = new RaidRunSaveData
                {
                    active = true, layout = layout, playerSectorCoord = layout.generatorRoomCoord,
                    generatorPowered = true, hatchRevealed = true, timerEnabled = true, remainingSeconds = 83
                }
            };
            run.raid.containers.Add(new RaidLootContainerSaveData { key = "persisted-empty", rolled = true, unlocked = true });
            var settings = new ES3Settings(false) { path = path };
            ES3.Save("run", run, settings);
            var loaded = ES3.Load<RunSaveData>("run", settings);
            Assert.IsTrue(SaveMigration.TryMigrateRun(loaded, out string error), error);
            Assert.AreEqual(83, loaded.raid.remainingSeconds);
            Assert.IsTrue(loaded.raid.generatorPowered);
            Assert.AreEqual(layout.bossRoomCoord, loaded.raid.layout.bossRoomCoord);
            Assert.AreEqual(1, loaded.raid.containers.Count);
            Assert.IsTrue(loaded.raid.containers[0].rolled);
            Assert.IsTrue(loaded.raid.containers[0].unlocked);
        }
        finally
        {
            defaultsField.SetValue(null, originalDefaults);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    [Test]
    public void PublicLandmarksExistBeforeAnyOccupancyPublishesWithoutRevealingHatch()
    {
        Assert.IsTrue(RaidMapGenerator.TryGenerate(7, 1, 7, Vector2Int.left,
            new RaidMapSettings { bossSpawnPercent = 100 }, out var layout, out _));
        var manager = Go().AddComponent<SectorStateManager>();
        manager.EnsureInitialized();
        Set(manager, "<CurrentStageMapLayout>k__BackingField", layout);
        var occupancy = Go().AddComponent<SectorOccupancyManager>();
        Set(occupancy, "_sectorStateManager", manager);
        var channel = Asset<SectorMapSnapshotEventChannelSO>();
        Set(occupancy, "_mapSnapshotChangedChannel", channel);
        SectorMapSnapshot received = default;
        channel.OnEventRaised += snapshot => received = snapshot;
        occupancy.RefreshRaidMarkers();
        Assert.AreEqual(50, received.cells.Length);
        var generator = Array.Find(received.cells, cell => cell.coord == layout.generatorRoomCoord);
        var hatch = Array.Find(received.cells, cell => cell.coord == layout.hatchRoomCoord);
        var boss = Array.Find(received.cells, cell => cell.coord == layout.bossRoomCoord);
        var start = Array.Find(received.cells, cell => cell.coord == layout.startSectorCoord);
        Assert.IsTrue(generator.hasPublicRaidMarker);
        Assert.IsFalse(generator.isRevealed);
        Assert.IsTrue(boss.hasPublicRaidMarker);
        Assert.IsFalse(hatch.hasPublicRaidMarker);
        Assert.AreEqual(RaidRoomRole.None, hatch.raidRole);
        Assert.IsTrue(start.isExtraction);
    }

    [TestCase(0, 0)] [TestCase(6, 0)] [TestCase(6, 6)] [TestCase(0, 6)]
    public void EveryCornerHubLinksToTheEntryAndAllowsTheClearedReturnTrip(int x, int y)
    {
        StageMapLayout layout = null;
        for (int seed = 0; seed < 100; seed++)
        {
            RaidMapGenerator.TryGenerate(seed, 1, 7, Vector2Int.left, new RaidMapSettings(), out layout, out _);
            if (layout.firstRoomCoord == new Vector2Int(x, y)) break;
        }
        Assert.AreEqual(new Vector2Int(x, y), layout.firstRoomCoord);
        var manager = Go().AddComponent<SectorStateManager>();
        manager.EnsureInitialized();
        var sectors = new List<SectorRuntime>();
        foreach (var room in layout.rooms)
        {
            var sector = Go().AddComponent<SectorRuntime>();
            sector.SetRuntimeInfo(room.coord, room.isOpened, room.roomType == StageRoomType.Start);
            sector.XMin = Edge(sector);
            sector.XMax = Edge(sector);
            sector.ZMin = Edge(sector);
            sector.ZMax = Edge(sector);
            sectors.Add(sector);
        }
        manager.RegisterGeneratedStageMap(layout, sectors);
        var portals = Go().AddComponent<SectorPortalManager>();
        Set(portals, "_sectorStateManager", manager);
        portals.RebuildPortalLinks(true);
        Assert.AreEqual(layout.startSectorCoord, manager.StartSector.Coord);
        int connectedHubPortals = 0;
        foreach (var sector in sectors)
            foreach (var edge in new[] { sector.XMin, sector.XMax, sector.ZMin, sector.ZMax })
            {
                var portal = edge.portal.GetComponent<SectorPortal>();
                Vector2Int target = sector.Coord + SectorPortalDirectionUtility.ToCoordOffset(portal.Direction);
                bool expected = layout.TryGetRoom(target, out _);
                Assert.AreEqual(expected, portal.TargetSector != null);
                if (!expected) continue;
                Assert.AreEqual(target, portal.TargetSector.Coord);
                Assert.AreSame(portal, portal.TargetPortal.TargetPortal);
                if (sector != manager.StartSector) continue;
                connectedHubPortals++;
                Assert.AreEqual(layout.firstRoomCoord, target);
                Assert.IsTrue(portal.IsAvailable);
                portal.TargetSector.SetCleared(true);
                portals.RebuildPortalLinks(false);
                Assert.IsTrue(portal.TargetPortal.IsAvailable);
            }
        Assert.AreEqual(1, connectedHubPortals);
    }

    private SectorEdge Edge(SectorRuntime sector)
    {
        var portal = Go().AddComponent<SectorPortal>();
        portal.transform.SetParent(sector.transform, false);
        return new SectorEdge { portal = portal.gameObject };
    }

    private GameObject Go()
    {
        var go = new GameObject("RaidTest"); _created.Add(go); return go;
    }
    private T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>(); _created.Add(asset); return asset;
    }
    private static PlayerInventoryItemStack Stack(ItemSO item, int count, float armor = -1)
    { var stack = new PlayerInventoryItemStack(); stack.Set(item, count, armor); return stack; }
    private static void Set(object owner, string name, object value)
    {
        for (Type type = owner.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (field == null) continue;
            field.SetValue(owner, value); return;
        }
        throw new MissingFieldException(owner.GetType().Name, name);
    }
}
