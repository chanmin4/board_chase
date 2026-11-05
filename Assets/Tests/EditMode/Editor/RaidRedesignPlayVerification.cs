using System;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Explicit batch-only smoke test in an unsaved empty scene. Never loads a working
/// gameplay scene, creates SaveSystem, saves assets, or runs automatically in the editor.
/// </summary>
public static class RaidRedesignPlayVerification
{
    private const string Pending = "RaidRedesign.PlayVerification";
    private const string Stage = "RaidRedesign.PlayStage";
    private const string LastStage = "RaidRedesign.PlayLastStage";
    private static GameObject _host, _player;
    private static StageSectorInstantiator _builder;
    private static SectorStateManager _manager;
    private static Damageable _health;
    private static EntityStatusEffectRuntime _effects;
    private static float _treatedHealth;
    private static int _phase, _frames;
    private static double _tickDeadline;

    public static void Run() => Begin(5);
    public static void RunStageOne() => Begin(1);

    private static void Begin(int lastStage)
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("This smoke test is batch-only.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play mode must be stopped before this verification.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true); SessionState.SetInt(Stage, 1);
        SessionState.SetInt(LastStage, lastStage);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod] private static void Register()
    {
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

    private static void Tick()
    {
        if (!Application.isBatchMode || !SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying) return;
        try
        {
            int index = SessionState.GetInt(Stage, 1);
            if (_phase == 0)
            {
                Assert.IsNull(SaveSystem.Active, "Smoke scene must not have access to a real save.");
                var definition = Load<StageDefinitionSO>($"Assets/ScriptableObject/GameScene/Stage/Stage{index}Definition.asset");
                _host = new GameObject("Raid smoke only");
                _manager = _host.AddComponent<SectorStateManager>();
                _builder = _host.AddComponent<StageSectorInstantiator>();
                // Merchant is probabilistic now. Find a reproducible merchant seed without editing Stage assets.
                int smokeSeed = 10404; bool foundMerchant = false;
                for (int attempt = 0; attempt < 256; attempt++, smokeSeed++)
                    if (StageMapBuildPipeline.TryGenerate(definition, smokeSeed, Vector2Int.left, out var candidate, out _, readRuntimeQuests: false) &&
                        candidate.rooms.Exists(room => room.hasMerchant)) { foundMerchant = true; break; }
                Assert.IsTrue(foundMerchant, "Merchant smoke needs a stage with nonzero chance and an eligible common room.");
                Set(_builder, "_randomizeRunSeedOnAwake", false); Set(_builder, "_runSeed", smokeSeed);
                var portals = _host.AddComponent<SectorPortalManager>();
                Set(portals, "_sectorStateManager", _manager);
                Set(_builder, "_sectorPortalManager", portals);
                Assert.IsTrue(_builder.BuildStage(definition, _manager), $"Stage {index} build failed.");
                Assert.AreEqual(_builder.CurrentLayout.rooms.Count, _manager.Sectors.Count);
                Assert.IsTrue(RaidMapValidation.TryValidate(_builder.CurrentLayout, out var error), error);
                foreach (var sector in _manager.Sectors)
                {
                    Assert.NotNull(sector.PlayerStartPoint); Assert.NotNull(sector.GetComponent<SectorRoomTraitRuntime>());
                    var surface = sector.GetComponent<NavMeshSurface>();
                    Assert.NotNull(surface, sector.name + ": unified NavMesh missing");
                    Assert.NotNull(surface.navMeshData, sector.name + ": runtime NavMesh data missing");
                }
                var shops = _host.GetComponentsInChildren<SectorShop>(true);
                SectorShop merchant = null; foreach (var shop in shops) if (shop.IsRaidMerchant) merchant = shop;
                Assert.NotNull(merchant);
                var merchantSector = merchant.GetComponentInParent<SectorRuntime>();
                _manager.SetPlayerCurrentSector(merchantSector, false);
                Assert.IsFalse(merchantSector.IsCleared, "Merchant overlay must not auto-clear its combat room.");
                Assert.IsFalse(merchant.CanInteract, "Existing raid-shop rule requires room clear.");
                // This shop-budget smoke explicitly skips combat; generation itself must not do so.
                merchantSector.GetComponentInChildren<SectorEnemySpawner>(true)?.CancelEncounterForSectorSkip();
                merchantSector.SetCleared(true);
                Assert.IsTrue(merchant.CanInteract); Assert.IsTrue(merchant.TryReservePayout(75));
                Assert.AreEqual(925, merchant.MerchantMoney); Assert.IsFalse(merchant.TryReservePayout(926));
                var shopState = new PlayerRunShopStateSaveData(); Assert.IsTrue(merchant.CaptureRunState(shopState));
                merchant.RefundPayout(75); Assert.IsTrue(merchant.TryRestoreRunState(shopState,
                    Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog"), out error), error);
                Assert.AreEqual(925, merchant.MerchantMoney);
                Debug.Log($"[Raid Play PASS] Stage {index}: {_manager.Sectors.Count} rooms, runtime NavMeshes, merchant budget/resume.");
                if (index == 1) VerifyStageOneServices(definition);
                _frames = 0; _phase = index == 1 ? 1 : 3;
            }
            else if (_phase == 1)
            {
                if (EditorApplication.timeSinceStartup < _tickDeadline) return;
                Assert.Less(_health.CurrentHealth, 600, "Persistent bleeding did not tick in Play Mode.");
                Assert.AreEqual(2, _effects.TreatBleeding(int.MaxValue));
                _treatedHealth = _health.CurrentHealth;
                _tickDeadline = EditorApplication.timeSinceStartup + 1.1; _phase = 2;
            }
            else if (_phase == 2)
            {
                if (EditorApplication.timeSinceStartup < _tickDeadline) return;
                Assert.AreEqual(0, _effects.BleedingStacks);
                Assert.AreEqual(_treatedHealth, _health.CurrentHealth, "Treatment must stop further bleeding damage.");
                Debug.Log("[Raid Play PASS] Persistent bleeding ticks and explicit treatment removes its stacks.");
                _phase = 3;
            }
            else if (_phase == 3)
            {
                _builder.ClearGeneratedSectors(); Object.Destroy(_host);
                if (_player != null) Object.Destroy(_player);
                _frames = 0; _phase = 4;
            }
            else if (_phase == 4 && ++_frames >= 3)
            {
                if (index >= SessionState.GetInt(LastStage, 5))
                {
                    SessionState.SetBool(Pending, false);
                    Debug.Log($"[Raid Play verification] PASSED: Stage 1-{index} build/NavMesh/services; no authored scene/save written.");
                    EditorApplication.Exit(0);
                }
                else { SessionState.SetInt(Stage, index + 1); _phase = 0; }
            }
        }
        catch (Exception ex)
        {
            SessionState.SetBool(Pending, false); Debug.LogError("[Raid Play verification FAIL] " + ex);
            EditorApplication.Exit(1);
        }
    }

    private static void VerifyStageOneServices(StageDefinitionSO definition)
    {
        var raid = _host.GetComponent<RaidRuntime>(); var map = _builder.CurrentLayout;
        _player = new GameObject("Temporary verification actor");
        _health = _player.AddComponent<Damageable>(); _health.ApplyMaxHealthFromStats(600, true);
        var inventory = _player.AddComponent<PlayerInventoryRuntime>();
        Assert.IsTrue(inventory.TryPickup(definition.RaidRuntime.generatorBattery, 1, out var error), error);
        Assert.IsTrue(_manager.TryGetSector(map.generatorRoomCoord, out var generator));
        _manager.SetPlayerCurrentSector(generator, false);
        Assert.IsFalse(raid.TryReturnThroughShortcut(inventory), "Shortcut must require generator power.");
        Assert.IsTrue(raid.TryPowerGenerator(inventory)); Assert.IsFalse(raid.TryPowerGenerator(inventory));
        Assert.AreEqual(0, inventory.GetInventoryItemAmount(definition.RaidRuntime.generatorBattery));
        Assert.IsTrue(RaidRuntime.AllowsExtraction(raid.State, map.hatchRoomCoord, RaidStationKind.Hatch));
        bool shortcut = false;
        foreach (var station in generator.GetComponentsInChildren<RaidStation>(true))
            if (station.Kind == RaidStationKind.GeneratorShortcut) shortcut = true;
        Assert.IsTrue(shortcut);
        Assert.IsTrue(_manager.TryGetSector(map.hatchRoomCoord, out var hatch));
        var controller = _player.AddComponent<CharacterController>();
        Assert.IsTrue(raid.TryReturnThroughShortcut(inventory));
        Assert.AreSame(hatch, _manager.CurrentSector);
        Assert.AreSame(hatch.transform, _player.transform.parent);
        Assert.Less(Vector3.Distance(hatch.PlayerStartPoint.position, _player.transform.position), .001f);
        Assert.AreEqual(hatch.Coord, raid.State.playerSectorCoord);
        Assert.IsTrue(controller.enabled, "Shortcut must restore the character controller.");
        Assert.IsFalse(raid.TryReturnThroughShortcut(inventory), "Shortcut is one-way from the generator room.");
        var vault = map.rooms.Find(r => r.raidRole == RaidRoomRole.Vault);
        Assert.NotNull(vault, "Smoke seed needs one vault.");
        Assert.IsTrue(_manager.TryGetSector(vault.coord, out var vaultSector));
        Assert.IsFalse(raid.TryAuthorizeVaultEntry(vaultSector, inventory));
        var tool = Load<KeyToolItemSO>("Assets/ScriptableObject/GameScene/GameItem/KeyTool/KeyTool_Basic.asset");
        if (tool == null)
            tool = Load<KeyToolItemSO>(AssetDatabase.GUIDToAssetPath("f6d0a7eb8e4a4eb1a4f6f02fbe64021a"));
        KeyItemSO normalKey = null;
        foreach (var guid in AssetDatabase.FindAssets("t:KeyItemSO"))
        { var candidate = Load<KeyItemSO>(AssetDatabase.GUIDToAssetPath(guid)); if (candidate.Tier == LootKeyTier.Normal) { normalKey = candidate; break; } }
        Assert.NotNull(normalKey); Assert.IsTrue(inventory.TryPickup(tool, 1, out error), error);
        Assert.IsTrue(inventory.TryPickup(normalKey, 1, out error), error);
        int uses = inventory.GetKeyToolTierSnapshot(LootKeyTier.Normal).TotalRemainingUses;
        Assert.IsTrue(raid.TryAuthorizeVaultEntry(vaultSector, inventory));
        Assert.IsTrue(raid.TryAuthorizeVaultEntry(vaultSector, inventory));
        Assert.AreEqual(uses - 1, inventory.GetKeyToolTierSnapshot(LootKeyTier.Normal).TotalRemainingUses);
        var restored = JsonUtility.FromJson<StageMapLayout>(JsonUtility.ToJson(map));
        Assert.IsTrue(restored.TryGetRoom(vault.coord, out var savedVault)); Assert.IsTrue(savedVault.vaultEntryUnlocked);
        Debug.Log("[Raid Play PASS] Generator consumes once; powered shortcut moves actor/controller/current sector to hatch; vault key consumes once and survives layout save.");
        _effects = _player.AddComponent<EntityStatusEffectRuntime>();
        var bleeding = Resources.Load<StatusEffectDefinitionSO>("Bleeding");
        _effects.Apply(bleeding); _effects.Apply(bleeding);
        _tickDeadline = EditorApplication.timeSinceStartup + 1.3;
    }
}
