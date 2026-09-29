using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// EditMode regression tests for persistent-profile repair and the run-lifecycle
/// decisions that must not depend on a loaded scene or the real save file.
/// </summary>
public sealed class RunLifecycleAndSaveTests
{
    [Test]
    public void ApplicationPauseRules_SuspendOnlyActiveRunsAndRestoreTheRecordedPhase()
    {
        Assert.That(
            RunLifecyclePolicy.ResolveApplicationPausePhase(
                RunLifecyclePhase.RunActive,
                true,
                RunLifecyclePhase.RunActive),
            Is.EqualTo(RunLifecyclePhase.RunSuspended));

        Assert.That(
            RunLifecyclePolicy.ResolveApplicationPausePhase(
                RunLifecyclePhase.Home,
                true,
                RunLifecyclePhase.Home),
            Is.EqualTo(RunLifecyclePhase.Home));

        Assert.That(
            RunLifecyclePolicy.ResolveApplicationPausePhase(
                RunLifecyclePhase.RunSuspended,
                false,
                RunLifecyclePhase.RunActive),
            Is.EqualTo(RunLifecyclePhase.RunActive));

        Assert.That(
            RunLifecyclePolicy.ResolveApplicationPausePhase(
                RunLifecyclePhase.Home,
                false,
                RunLifecyclePhase.RunActive),
            Is.EqualTo(RunLifecyclePhase.Home));
    }

    [Test]
    public void RunLifecycleRules_ProtectStageBoundsAndDuplicateDeathHandling()
    {
        Assert.That(RunLifecyclePolicy.NormalizeActiveStageIndex(-3), Is.EqualTo(0));
        Assert.That(RunLifecyclePolicy.NormalizeActiveStageIndex(4), Is.EqualTo(4));
        Assert.That(RunLifecyclePolicy.CanProcessRunLoss(RunLifecyclePhase.RunActive), Is.True);
        Assert.That(RunLifecyclePolicy.CanProcessRunLoss(RunLifecyclePhase.GameOver), Is.False);
    }

    [Test]
    public void EnsureRuntimeDefaults_CleansBrokenEntriesWithoutRemovingValidPersistentProgress()
    {
        Save save = new Save
        {
            _roguelikeCurrency = -15,
            _highestUnlockedStageIndex = -9,
            _metaUpgrades = new MetaUpgradeSaveData
            {
                maxHealthLevel = -1,
                attackDamageLevel = 3,
                moveSpeedLevel = -2
            },
            _inheritanceWarehouse = new InheritanceWarehouseSaveData
            {
                capacity = 0,
                items = new List<InheritanceWarehouseItemSaveData>
                {
                    new InheritanceWarehouseItemSaveData { slotIndex = 0, itemGuid = "keep", amount = 3 },
                    new InheritanceWarehouseItemSaveData { slotIndex = 0, itemGuid = "duplicate_slot", amount = 1 },
                    new InheritanceWarehouseItemSaveData { slotIndex = -1, itemGuid = "invalid_slot", amount = 1 },
                    new InheritanceWarehouseItemSaveData { slotIndex = 1, itemGuid = "", amount = 1 }
                }
            },
            _shopUnlockProgress = new List<ShopUnlockProgressSaveData>
            {
                null,
                new ShopUnlockProgressSaveData { configId = "", unlockedNodeIds = new List<string> { "ignored" } },
                new ShopUnlockProgressSaveData
                {
                    configId = "home_shop",
                    unlockedNodeIds = new List<string> { "ammo", "ammo", "", "medicine" }
                }
            },
            _questProgress = new List<QuestProgressSaveData>
            {
                null,
                new QuestProgressSaveData { questId = "" },
                new QuestProgressSaveData { questId = "main_stage_1", objectiveProgress = new[] { -2, 4 } }
            }
        };

        save.EnsureRuntimeDefaults();

        Assert.That(save._roguelikeCurrency, Is.Zero);
        Assert.That(save.HighestUnlockedStageIndex, Is.EqualTo(-1));
        Assert.That(save.MetaUpgrades.maxHealthLevel, Is.Zero);
        Assert.That(save.MetaUpgrades.attackDamageLevel, Is.EqualTo(3));
        Assert.That(save.MetaUpgrades.moveSpeedLevel, Is.Zero);

        Assert.That(save.InheritanceWarehouse.capacity, Is.EqualTo(1));
        Assert.That(save.InheritanceWarehouse.items, Has.Count.EqualTo(1));
        Assert.That(save.InheritanceWarehouse.items[0].itemGuid, Is.EqualTo("keep"));
        Assert.That(save.InheritanceWarehouse.items[0].amount, Is.EqualTo(3));

        Assert.That(save.ShopUnlockProgress, Has.Count.EqualTo(1));
        CollectionAssert.AreEquivalent(
            new[] { "ammo", "medicine" },
            save.ShopUnlockProgress[0].unlockedNodeIds);

        Assert.That(save.QuestProgress, Has.Count.EqualTo(1));
        CollectionAssert.AreEqual(new[] { 0, 4 }, save.QuestProgress[0].objectiveProgress);
    }

    [Test]
    public void LoadFromJson_RepairsLegacyNullCollectionsAndNegativeProfileValues()
    {
        Save save = new Save();
        save.LoadFromJson(
            "{\"_roguelikeCurrency\":-7,\"_highestUnlockedStageIndex\":-12,\"_shopUnlockProgress\":[{\"configId\":\"home_shop\",\"unlockedNodeIds\":null}],\"_questProgress\":[{\"questId\":\"main_stage_1\",\"objectiveProgress\":null}]}" );

        Assert.That(save._roguelikeCurrency, Is.Zero);
        Assert.That(save.HighestUnlockedStageIndex, Is.EqualTo(-1));
        Assert.That(save.ShopUnlockProgress, Has.Count.EqualTo(1));
        Assert.That(save.ShopUnlockProgress[0].unlockedNodeIds, Is.Empty);
        Assert.That(save.QuestProgress, Has.Count.EqualTo(1));
        Assert.That(save.QuestProgress[0].objectiveProgress, Is.Empty);
    }

    [Test]
    public void PartitionMigrations_UpgradeVersionZeroAndPreserveEachPartitionBoundary()
    {
        Save profile = new Save
        {
            schemaVersion = 0,
            _highestUnlockedStageIndex = 3,
            _inheritanceWarehouse = new InheritanceWarehouseSaveData
            {
                capacity = 2,
                items = new List<InheritanceWarehouseItemSaveData>
                {
                    new InheritanceWarehouseItemSaveData { slotIndex = 0, itemGuid = "medkit", amount = 2 }
                }
            }
        };
        RunSaveData run = new RunSaveData
        {
            schemaVersion = 0,
            playerRunState = new PlayerRunStateSaveData
            {
                hasSnapshot = true,
                inventorySlots = new List<PlayerRunInventorySlotSaveData>
                {
                    new PlayerRunInventorySlotSaveData { slotIndex = 1, itemGuid = "ammo", amount = 30 }
                }
            }
        };
        DeviceSettingsSaveData settings = new DeviceSettingsSaveData
        {
            schemaVersion = 0,
            localeCode = "ko",
            inputBindingOverridesJson = "{}",
            masterVolume = 2f,
            targetFrameRate = 0
        };

        Assert.That(SaveMigration.TryMigrateProfile(profile, out _), Is.True);
        Assert.That(SaveMigration.TryMigrateRun(run, out _), Is.True);
        Assert.That(SaveMigration.TryMigrateSettings(settings, out _), Is.True);

        Assert.That(profile.schemaVersion, Is.EqualTo(Save.CurrentSchemaVersion));
        Assert.That(profile.HighestUnlockedStageIndex, Is.EqualTo(3));
        Assert.That(profile.InheritanceWarehouse.items[0].itemGuid, Is.EqualTo("medkit"));
        Assert.That(run.schemaVersion, Is.EqualTo(RunSaveData.CurrentSchemaVersion));
        Assert.That(run.PlayerRunState.hasSnapshot, Is.True);
        Assert.That(run.PlayerRunState.inventorySlots[0].itemGuid, Is.EqualTo("ammo"));
        Assert.That(settings.schemaVersion, Is.EqualTo(DeviceSettingsSaveData.CurrentSchemaVersion));
        Assert.That(settings.localeCode, Is.EqualTo("ko"));
        Assert.That(settings.masterVolume, Is.EqualTo(1f));
        Assert.That(settings.targetFrameRate, Is.EqualTo(1));
    }

    [Test]
    public void PartitionMigrations_RejectFutureSchemaWithoutMutatingData()
    {
        Save profile = new Save { schemaVersion = Save.CurrentSchemaVersion + 1 };

        Assert.That(SaveMigration.TryMigrateProfile(profile, out string error), Is.False);
        Assert.That(error, Does.Contain("supports only up to"));
        Assert.That(profile.schemaVersion, Is.EqualTo(Save.CurrentSchemaVersion + 1));
    }
}
