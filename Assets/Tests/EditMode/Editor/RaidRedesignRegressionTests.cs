using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RaidRedesignRegressionTests
{
    [TestCase(14, 1)] [TestCase(18, 2)] [TestCase(22, 3)] [TestCase(26, 4)] [TestCase(26, 5)]
    public void SparseSeedsAreConnectedDeterministicAndResumeWithoutReroll(int size, int stage)
    {
        var settings = new RaidMapSettings { minimumExtractionPathDistance = size / 3 };
        for (int seed = 0; seed < 100; seed++)
        {
            Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(seed, stage, size, settings, out var map, out var error), $"{seed}: {error}");
            Assert.IsTrue(RaidMapValidation.TryValidate(map, out error), error);
            Assert.Less(map.rooms.Count, size * size);
            Assert.GreaterOrEqual(map.rooms.Count, Mathf.RoundToInt(size * size * .32f));
            Assert.AreEqual(3, map.extractionCandidates.Count);
            Assert.AreEqual(2, map.extractionRooms.Count);
            Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(seed, stage, size, settings, out var second, out error), error);
            Assert.AreEqual(JsonUtility.ToJson(map), JsonUtility.ToJson(second));
            var state = new RaidRunSaveData { active = true, layout = map, playerSectorCoord = map.startSectorCoord };
            var restored = JsonUtility.FromJson<RaidRunSaveData>(JsonUtility.ToJson(state));
            Assert.IsTrue(restored.TryValidate(out error), error);
            Assert.IsFalse(RaidRuntime.AllowsExtraction(state, map.startSectorCoord, RaidStationKind.StartReturn));
            Assert.IsFalse(RaidRuntime.AllowsExtraction(state, map.startSectorCoord, RaidStationKind.Hatch));
            state.generatorPowered = state.hatchRevealed = true;
            Assert.IsTrue(RaidRuntime.AllowsExtraction(state, map.startSectorCoord, RaidStationKind.Hatch));
            foreach (var exit in map.extractionRooms) Assert.IsTrue(RaidRuntime.AllowsExtraction(state, exit, RaidStationKind.NormalExtraction));
            Assert.IsFalse(map.rooms.Exists(r => r.raidRole == RaidRoomRole.Armory || r.raidRole == RaidRoomRole.Medical));
            Assert.AreEqual(0, map.rooms.FindAll(r => r.raidRole == RaidRoomRole.Merchant).Count,
                "Merchant overlay is assigned after common/theme composition, not by the topology generator.");
            foreach (var coord in map.extractionCandidates)
            {
                map.TryGetRoom(coord, out var room);
                int connections = 0;
                foreach (var flag in new[] { StageRoomConnectionFlags.Left, StageRoomConnectionFlags.Right, StageRoomConnectionFlags.Up, StageRoomConnectionFlags.Down })
                    if (room.HasConnection(flag)) connections++;
                Assert.AreEqual(1, connections);
            }
        }
    }

    [Test] public void MapHidesDistantExitsAndBossAndRevealsCandidateOnApproach()
    {
        Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(12, 3, 22, new RaidMapSettings(), out var map, out _));
        Assert.IsTrue(map.TryGetRoom(map.extractionRooms[0], out var exit));
        var cell = new SectorMapCellSnapshot();
        RaidMapPresentation.Apply(map, exit, map.startSectorCoord, ref cell);
        Assert.IsFalse(cell.isExtraction); Assert.IsFalse(cell.isRevealed); Assert.AreEqual(RaidRoomRole.UnknownCandidate, cell.raidRole);
        RaidMapPresentation.Apply(map, exit, exit.coord, ref cell);
        Assert.IsTrue(cell.isExtraction); Assert.AreEqual(RaidRoomRole.Extraction, cell.raidRole);
        map.TryGetRoom(map.generatorRoomCoord, out var generator);
        RaidMapPresentation.Apply(map, generator, map.startSectorCoord, ref cell);
        Assert.IsTrue(cell.hasPublicRaidMarker); Assert.AreEqual(RaidRoomRole.Generator, cell.raidRole);
        map.TryGetRoom(map.bossRoomCoord, out var boss);
        RaidMapPresentation.Apply(map, boss, map.startSectorCoord, ref cell);
        Assert.AreEqual(StageRoomType.Normal, cell.roomType); Assert.AreEqual(RaidRoomRole.None, cell.raidRole);
        Assert.IsFalse(cell.hasPublicRaidMarker);
    }

    [Test] public void GeneralSupplySelectionIsDeterministicAndOptional()
    {
        var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/ScriptableObject/GameScene/Stage/Stage1Definition.asset");
        int count = 0;
        for (int i = 0; i < 1000; i++)
        {
            var coord = new Vector2Int(i % 14, i / 14);
            var first = RaidRoomContentSpawner.PickGeneralSupply(stage.RaidRuntime, i, coord);
            Assert.AreSame(first, RaidRoomContentSpawner.PickGeneralSupply(stage.RaidRuntime, i, coord));
            if (first != null) count++;
        }
        Assert.That(count, Is.InRange(180, 320));
        Assert.IsNull(RaidRoomContentSpawner.PickGeneralSupply(new RaidRuntimeSettings { normalSupplyChance = 0 }, 1, Vector2Int.zero));
    }

    [Test] public void EarlyGeneratorsAndRareMerchantBatteryUseTheExistingRareItem()
    {
        var battery = AssetDatabase.LoadAssetAtPath<ValuableItemSO>("Assets/ScriptableObject/GameScene/GameItem/Valuable/BigBattery.asset");
        Assert.NotNull(battery); Assert.AreEqual(PlayerShopItemRarity.Rare, battery.Rarity);
        for (int stageIndex = 1; stageIndex <= 2; stageIndex++)
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"Assets/ScriptableObject/GameScene/Stage/Stage{stageIndex}Definition.asset");
            Assert.AreSame(battery, stage.RaidRuntime.generatorBattery);
            Assert.AreEqual(1, stage.RaidRuntime.batteryAmount);
        }
        var shop = AssetDatabase.LoadAssetAtPath<ShopConfigSO>("Assets/ScriptableObject/GameScene/Shop/RaidEmergencyShopConfig.asset");
        bool present = false;
        foreach (var product in shop.Products) if (product.Item == battery) present = true;
        Assert.IsTrue(present);
    }

    [Test] public void StageSizesAndWipGateAreAuthored()
    {
        int[] sizes = { 14, 18, 22, 26, 26, 26 };
        for (int i = 1; i <= 6; i++)
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"Assets/ScriptableObject/GameScene/Stage/Stage{i}Definition.asset");
            Assert.NotNull(stage); Assert.AreEqual(sizes[i - 1], stage.RoomGridSize);
            Assert.AreEqual(i < 6, stage.CanEnter); Assert.AreEqual(StageMapPolicy.RadialBraidedRaid, stage.MapPolicy);
        }
        var flow = AssetDatabase.LoadAssetAtPath<StageRunFlowSO>("Assets/ScriptableObject/GameScene/Stage/StageRunFlow.asset");
        Assert.IsFalse(flow.TryGetStage(6, out _));
        Assert.IsTrue(flow.TryGetStage(5, out var final)); Assert.IsTrue(flow.IsFinalStage(final));
        Assert.IsFalse(flow.TryGetNextStage(final, out _));
    }

    [Test] public void BandageConditionSurvivesTransfersAndOldStackIsNotDiscarded()
    {
        var bandage = AssetDatabase.LoadAssetAtPath<ConsumableItemSO>("Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/BandageLV1.asset");
        var stack = new PlayerInventoryItemStack(); stack.Set(bandage, 10);
        Assert.IsTrue(stack.TrySpendConsumableDurability(bandage)); Assert.AreEqual(10, stack.Amount);
        Assert.AreEqual(75, stack.WeaponCurrentDurability);
        var binding = new PlayerQuickSlotBinding(); binding.SetUsable(bandage, 1, stack.WeaponCurrentDurability, stack.WeaponMaxDurability);
        Assert.AreEqual(75, binding.CurrentDurability); Assert.IsTrue(binding.SpendDurability(bandage));
        Assert.AreEqual(50, binding.CurrentDurability);
        Assert.IsTrue(stack.TrySpendConsumableDurability(bandage));
        Assert.IsTrue(stack.TrySpendConsumableDurability(bandage));
        Assert.IsTrue(stack.TrySpendConsumableDurability(bandage));
        Assert.AreEqual(9, stack.Amount); Assert.AreEqual(100, stack.WeaponCurrentDurability);
        var final = new PlayerInventoryItemStack(); final.Set(bandage, 1, -1, 25, 100);
        Assert.IsTrue(final.TrySpendConsumableDurability(bandage)); Assert.IsTrue(final.IsEmpty);
    }

    [Test] public void PersistentBleedingStacksTreatSeparatelyFromOldInjuries()
    {
        var obj = new GameObject("Bleed test");
        try
        {
            var effects = obj.AddComponent<EntityStatusEffectRuntime>(); var bleeding = Resources.Load<StatusEffectDefinitionSO>("Bleeding");
            Assert.NotNull(bleeding); effects.Apply(bleeding); effects.Apply(bleeding); effects.Apply(bleeding);
            Assert.AreEqual(3, effects.BleedingStacks); Assert.AreEqual(0, effects.RemoveInjuries());
            Assert.AreEqual(1, effects.TreatBleeding(1)); Assert.AreEqual(2, effects.BleedingStacks);
            var save = new PlayerRunStateSaveData { hasSnapshot = true }; effects.CaptureRunState(save); save.EnsureDefaults();
            var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
            Assert.Greater(catalog.ItemCount, 100); Assert.IsTrue(catalog.TryResolveStatusEffect(bleeding.Id, out _));
            Assert.IsTrue(effects.TryRestoreRunState(save, catalog, out var error), error);
            Assert.AreEqual(2, effects.BleedingStacks); Assert.AreEqual(2, effects.TreatBleeding(3)); Assert.AreEqual(0, effects.BleedingStacks);
        }
        finally { Object.DestroyImmediate(obj); }
    }

    [Test] public void BallisticProbabilityAndSingleLayerWearMatchPlan()
    {
        var equal = RaidArmorPenetration.Resolve(4, 4, 100, 100, 0);
        Assert.AreEqual(.90f, equal.probability, .0001f); Assert.AreEqual(1f, equal.healthMultiplier, .0001f);
        var one = RaidArmorPenetration.Resolve(4, 3, 100, 100, 1);
        Assert.AreEqual(.486f, one.probability, .0001f); Assert.AreEqual(.25f, one.healthMultiplier, .0001f);
        Assert.AreEqual(.35f, one.wearMultiplier); Assert.IsFalse(one.penetrated);
        var worn = RaidArmorPenetration.Resolve(4, 3, 0, 100, 0);
        Assert.AreEqual(.636f, worn.probability, .0001f);
        Assert.AreEqual(1, RaidArmorPenetration.Resolve(0, 1, 0, 0, 1).healthMultiplier);
    }

    [TestCase(0f, 1f, .30f)]
    [TestCase(.3f, .97f, .285f)]
    [TestCase(.5f, .95f, .275f)]
    [TestCase(.7f, .93f, .265f)]
    [TestCase(1f, .90f, .25f)]
    [TestCase(2f, .80f, .20f)]
    [TestCase(3f, .70f, .15f)]
    [TestCase(4f, .60f, .10f)]
    [TestCase(5f, .50f, .05f)]
    [TestCase(10f, .50f, .05f)]
    public void BallisticGapControlsSuccessfulAndFailedHealthDamage(float gap, float success, float failure)
    {
        var hit = RaidArmorPenetration.Resolve(1f + gap, 1f, 1000f, 1000f, 0f);
        var blocked = RaidArmorPenetration.Resolve(1f + gap, 1f, 1000f, 1000f, 1f);
        Assert.IsTrue(hit.penetrated);
        Assert.IsFalse(blocked.penetrated);
        Assert.AreEqual(success, hit.healthMultiplier, .0001f);
        Assert.AreEqual(failure, blocked.healthMultiplier, .0001f);
        Assert.AreEqual(1f, hit.wearMultiplier);
        Assert.AreEqual(.35f, blocked.wearMultiplier);
    }

    [TestCase(4f, 3.7f, .7481017f)]
    [TestCase(4f, 3.3f, .5846799f)]
    [TestCase(4f, 4.3f, .90f)]
    [TestCase(4f, 4.7f, .90f)]
    public void FractionalPenetrationIsNotRoundedBeforeProbabilityOrRoll(
        float armor, float penetration, float expectedProbability)
    {
        var success = RaidArmorPenetration.Resolve(armor, penetration, 1000f, 1000f,
            expectedProbability - .0005f);
        var failure = RaidArmorPenetration.Resolve(armor, penetration, 1000f, 1000f,
            expectedProbability + .0005f);
        Assert.AreEqual(expectedProbability, success.probability, .000001f);
        Assert.AreEqual(expectedProbability, failure.probability, .000001f);
        Assert.IsTrue(success.penetrated);
        Assert.IsFalse(failure.penetrated);
    }

    [Test]
    public void BallisticEqualOrHigherPenetrationCapsAtNinetyAndUnarmoredHitBypassesRoll()
    {
        var high = RaidArmorPenetration.Resolve(3f, 8f, 500f, 1000f, .899f);
        var threshold = RaidArmorPenetration.Resolve(3f, 8f, 500f, 1000f, .90f);
        Assert.AreEqual(.90f, high.probability, .0001f);
        Assert.IsTrue(high.penetrated);
        Assert.IsFalse(threshold.penetrated);
        Assert.AreEqual(1f, high.healthMultiplier, .0001f);
        Assert.AreEqual(.30f, threshold.healthMultiplier, .0001f);
        var bare = RaidArmorPenetration.Resolve(0f, 0f, 0f, 0f, 1f);
        Assert.AreEqual(1f, bare.probability);
        Assert.AreEqual(1f, bare.healthMultiplier);
        Assert.AreEqual(0f, bare.wearMultiplier);
        Assert.IsTrue(bare.penetrated);
    }

    [Test] public void AnomaliesAreTenPercentOnePerCombatRoomAndNoEarlyGasOrBoss()
    {
        var table = AssetDatabase.LoadAssetAtPath<SectorRoomTraitTableSO>("Assets/ScriptableObject/GameScene/Stage/RaidAnomalyTable.asset");
        int count = 0;
        for (int i = 0; i < 10000; i++)
        {
            var early = table.PickTrait(i, StageRoomType.Normal, 1);
            Assert.AreNotEqual(SectorRoomTraitType.GasBoost, early);
            if (early != SectorRoomTraitType.Normal) count++;
            Assert.AreEqual(SectorRoomTraitType.Normal, table.PickTrait(i, StageRoomType.Boss, 5));
            Assert.AreEqual(SectorRoomTraitType.Normal, table.PickTrait(i, StageRoomType.Normal, 5, false));
        }
        Assert.That(count, Is.InRange(800, 1200));
    }
}
