using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class RaidExtractionPortalTests
{
    private Scene _preview;
    private readonly List<Object> _assets = new();
    private SectorStateManager _states;
    private SectorPortalManager _portals;
    private SectorRuntime _sector;
    private RaidRuntime _raid;
    private RaidRunSaveData _state;
    private StageDefinitionSO _definition;
    private StageMapLayout _layout;
    private PlayerInventoryRuntime _inventory;
    private SectorPortal _exit;

    [SetUp] public void Setup()
    {
        _preview = EditorSceneManager.NewPreviewScene();
        Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(17, 1, 14, new RaidMapSettings(), out _layout, out var error), error);
        _states = Go("Temporary states").AddComponent<SectorStateManager>();
        Set(_states, "_isInitialized", true);
        Set(_states, "<CurrentStageMapLayout>k__BackingField", _layout);
        _sector = Go("Ordinary exit owner").AddComponent<SectorRuntime>();
        _sector.coord = _layout.extractionRooms[0]; _sector.isOpened = true;
        ((Dictionary<SectorRuntime, Vector2Int>)Get(_states, "_coordBySector")).Add(_sector, _sector.Coord);
        ((Dictionary<Vector2Int, SectorRuntime>)Get(_states, "_sectorByCoord")).Add(_sector.Coord, _sector);
        Set(_states, "<CurrentSector>k__BackingField", _sector);
        _raid = Go("Temporary raid").AddComponent<RaidRuntime>();
        _state = new RaidRunSaveData { active = true, layout = _layout, playerSectorCoord = _sector.Coord };
        _layout.TryGetRoom(_sector.Coord, out var poweredRoom);
        poweredRoom.portalsActivated = true; // Existing extraction tests begin after operating the room device.
        Set(_raid, "<State>k__BackingField", _state); Set(_raid, "_sectors", _states); Set(_raid, "_ready", true);
        _definition = Asset<StageDefinitionSO>(); Set(_raid, "<Definition>k__BackingField", _definition);
        _portals = Go("Temporary portals").AddComponent<SectorPortalManager>();
        Set(_portals, "_sectorStateManager", _states); Call(_portals, "BindRaidRuntime", _raid);
        ((Dictionary<Vector2Int, SectorRuntime>)Get(_portals, "_sectorByCoord")).Add(_sector.Coord, _sector);
        var route = _layout.extractionPortals[0];
        _exit = Go("Preplaced exit portal", _sector.transform).AddComponent<SectorPortal>();
        Set(_exit, "_interactionCollider", _exit.gameObject.AddComponent<BoxCollider>());
        var edge = new SectorEdge { portal = _exit.gameObject };
        switch (route.direction)
        {
            case SectorPortalDirection.XMin: _sector.XMin = edge; break;
            case SectorPortalDirection.XMax: _sector.XMax = edge; break;
            case SectorPortalDirection.ZMin: _sector.ZMin = edge; break;
            case SectorPortalDirection.ZMax: _sector.ZMax = edge; break;
        }
        Call(_portals, "BindPortal", _sector, route.direction);
        _inventory = Go("Temporary inventory").AddComponent<PlayerInventoryRuntime>();
    }

    [TearDown] public void Cleanup()
    {
        if (_portals != null) Call(_portals, "BindRaidRuntime", (object)null);
        if (_preview.IsValid()) EditorSceneManager.ClosePreviewScene(_preview);
        foreach (var asset in _assets) if (asset != null) Object.DestroyImmediate(asset);
        _assets.Clear();
    }

    [Test] public void FiftySeedsUseNormalRoomsAndSparePortalsWithoutAddingRoomsOrStealingLinks()
    {
        foreach (int size in new[] { 14, 18, 22, 26, 26 })
            for (int seed = 0; seed < 10; seed++)
            {
                Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(seed, 1, size, new RaidMapSettings(), out var map, out var error), error);
                Assert.IsTrue(RaidMapValidation.TryValidate(map, out error), error);
                Assert.AreEqual(Mathf.RoundToInt(size * size * .32f), map.rooms.Count);
                Assert.IsFalse(map.rooms.Exists(room => room.raidRole == RaidRoomRole.Extraction));
                Assert.AreEqual(2, map.extractionPortals.Count);
                foreach (var route in map.extractionPortals)
                {
                    Assert.IsTrue(map.TryGetRoom(route.coord, out var room));
                    Assert.AreEqual(StageRoomType.Normal, room.roomType); Assert.AreEqual(RaidRoomRole.Normal, room.raidRole);
                    Assert.IsTrue(map.IsExtractionPortal(route.coord, route.direction));
                    Assert.IsFalse(room.HasConnection(Flag(route.direction)));
                    Assert.AreEqual(1, Array.FindAll((SectorPortalDirection[])Enum.GetValues(typeof(SectorPortalDirection)),
                        direction => map.IsExtractionPortal(route.coord, direction)).Length);
                }
                Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(seed, 1, size, new RaidMapSettings(), out var second, out error), error);
                Assert.AreEqual(JsonUtility.ToJson(map), JsonUtility.ToJson(second));
                string json = JsonUtility.ToJson(map);
                var restored = JsonUtility.FromJson<StageMapLayout>(json);
                Assert.IsTrue(RaidExtractionPortalPolicy.TryPrepare(restored, out error), error);
                Assert.AreEqual(json, JsonUtility.ToJson(restored));
            }
    }

    [Test] public void PreplacedPortalIsHiddenUntilRoomClearThenOpensWithoutPowerAndClosesOnExtractionCommit()
    {
        Assert.IsTrue(_exit.IsExtraction); Assert.IsNull(_exit.TargetSector); Assert.IsNull(_exit.TargetPortal);
        int children = _sector.transform.childCount;
        Call(_portals, "RefreshPortal", _sector, _exit.Direction);
        Assert.IsFalse(_exit.IsAvailable); Assert.IsFalse(_exit.gameObject.activeSelf);
        _sector.isCleared = true; Call(_portals, "RefreshPortal", _sector, _exit.Direction);
        Assert.IsTrue(_exit.IsAvailable); Assert.IsTrue(_exit.gameObject.activeSelf);
        Assert.IsFalse(_state.generatorPowered, "Normal exit is independent of the power-gated hatch.");
        _state.extractionCommitted = true; Call(_raid, "NotifyStateChanged");
        Assert.IsFalse(_exit.IsAvailable); Assert.IsFalse(_exit.gameObject.activeSelf);
        Assert.AreEqual(children, _sector.transform.childCount);
    }

    [Test] public void WrongDirectionOtherSectorAndDuplicateExtractionCannotUseRaidPortal()
    {
        _sector.isCleared = true; Call(_portals, "RefreshPortal", _sector, _exit.Direction);
        Set(_states, "<CurrentSector>k__BackingField", null);
        Assert.IsFalse(_raid.TryExtractThroughPortal(_exit, _inventory));
        Set(_states, "<CurrentSector>k__BackingField", _sector);
        _exit.Initialize(_portals, _sector, SectorPortalDirectionUtility.Opposite(_exit.Direction));
        Assert.IsFalse(_raid.TryExtractThroughPortal(_exit, _inventory));
        _state.extractionCommitted = true;
        Assert.IsFalse(_raid.TryExtractThroughPortal(_exit, _inventory));
        Assert.IsTrue(_state.active, "No real Home transition or save was requested.");
    }

    [Test] public void ExistingNeighbourTravelAndArrivalPortalArePreservedBesideExtraction()
    {
        _layout.TryGetRoom(_sector.Coord, out var room);
        var direction = Array.Find((SectorPortalDirection[])Enum.GetValues(typeof(SectorPortalDirection)),
            candidate => room.HasConnection(Flag(candidate)));
        var opposite = SectorPortalDirectionUtility.Opposite(direction);
        var neighbour = Go("Existing neighbour").AddComponent<SectorRuntime>();
        neighbour.coord = _sector.Coord + SectorPortalDirectionUtility.ToCoordOffset(direction); neighbour.isOpened = true;
        ((Dictionary<Vector2Int, SectorRuntime>)Get(_states, "_sectorByCoord")).Add(neighbour.Coord, neighbour);
        ((Dictionary<SectorRuntime, Vector2Int>)Get(_states, "_coordBySector")).Add(neighbour, neighbour.Coord);
        ((Dictionary<Vector2Int, SectorRuntime>)Get(_portals, "_sectorByCoord")).Add(neighbour.Coord, neighbour);
        var travel = Go("Existing travel portal", _sector.transform).AddComponent<SectorPortal>();
        var arrival = Go("Existing arrival portal", neighbour.transform).AddComponent<SectorPortal>();
        AssignEdge(_sector, direction, travel); AssignEdge(neighbour, opposite, arrival);
        Call(_portals, "BindPortal", _sector, direction); Call(_portals, "BindPortal", neighbour, opposite);
        Assert.AreSame(neighbour, travel.TargetSector); Assert.AreSame(arrival, travel.TargetPortal);
        Assert.AreSame(_sector, arrival.TargetSector); Assert.AreSame(travel, arrival.TargetPortal);
        Assert.IsFalse(travel.IsExtraction); Assert.IsFalse(arrival.IsExtraction);
        _sector.isCleared = true; Call(_portals, "RefreshPortal", _sector, direction);
        Assert.IsTrue(travel.IsAvailable);
        _exit.SetLink(neighbour, arrival); Call(_portals, "BindPortal", _sector, _exit.Direction);
        Assert.IsNull(_exit.TargetSector); Assert.IsNull(_exit.TargetPortal);
        Assert.AreSame(arrival, travel.TargetPortal);
    }

    [Test] public void ExtractionPortalUsesTheExistingInteractionTypeAndLocalizedExtractLabel()
    {
        var interactions = _inventory.gameObject.AddComponent<InteractionManager>();
        Assert.That((string)Call(interactions, "ResolveActionLabel", new Interaction(InteractionType.Portal, _exit.gameObject)),
            Is.EqualTo(UIRuntimeLocalization.Get("UI_Map", "Map_Extract", "탈출")));
        _exit.SetExtraction(false);
        Assert.AreEqual("섹터 이동", Call(interactions, "ResolveActionLabel", new Interaction(InteractionType.Portal, _exit.gameObject)));
    }

    [Test] public void DeviceActivationIsRequiredEvenAfterClearAndPersistsWithoutRerollOrClear()
    {
        _layout.TryGetRoom(_sector.Coord, out var room);
        room.portalsActivated = false;
        _sector.isCleared = true;
        Call(_portals, "RefreshPortal", _sector, _exit.Direction);
        Assert.IsFalse(_exit.IsAvailable, "Room clear must not bypass its activation device.");
        _sector.isCleared = false;
        float clock = _state.remainingSeconds;
        Assert.IsTrue(_raid.TryActivatePortals(_sector), "Operating the device never requires killing enemies.");
        Assert.IsFalse(_sector.isCleared);
        Assert.AreEqual(clock, _state.remainingSeconds);
        Assert.IsFalse(_exit.IsAvailable, "Extraction still requires clearing the exit room.");
        Assert.IsFalse(_raid.TryActivatePortals(_sector), "Opening a powered room twice is a no-op.");
        _sector.isCleared = true;
        Call(_portals, "RefreshPortal", _sector, _exit.Direction);
        Assert.IsTrue(_exit.IsAvailable);
        var restored = JsonUtility.FromJson<RaidRunSaveData>(JsonUtility.ToJson(_state));
        Assert.IsTrue(restored.layout.TryGetRoom(_sector.Coord, out var restoredRoom));
        Assert.IsTrue(restoredRoom.portalsActivated);
        Assert.AreEqual(_layout.stageSeed, restored.layout.stageSeed);
        Set(_states, "<CurrentSector>k__BackingField", null);
        Assert.IsTrue(_raid.HasActivatedPortals(_sector));
        Set(_states, "<CurrentSector>k__BackingField", _sector);
        Assert.IsTrue(_raid.HasActivatedPortals(_sector), "Revisiting preserves power independently of encounter state.");
    }

    [Test] public void FreshStartAndOtherRoomsDoNotPowerEachOther()
    {
        Assert.IsTrue(_layout.TryGetRoom(_layout.startSectorCoord, out var start));
        Assert.IsFalse(start.portalsActivated, "Even an already-cleared starting hatch needs its device.");
        var other = Go("Wrong room").AddComponent<SectorRuntime>();
        other.coord = _layout.startSectorCoord;
        Assert.IsFalse(_raid.TryActivatePortals(other));
        _layout.TryGetRoom(_sector.Coord, out var current);
        current.portalsActivated = false;
        Assert.IsTrue(_raid.TryActivatePortals(_sector));
        Assert.IsFalse(start.portalsActivated, "A source device must not remotely activate a destination.");
        var old = JsonUtility.FromJson<StageRoomNode>("{\"isOpened\":true,\"isCleared\":true}");
        Assert.IsFalse(old.portalsActivated, "Legacy snapshots start unpowered without discarding topology.");
    }

    [Test] public void DeviceAndEveryPortalUseSeparateFiveSecondCastsWithoutCooldown()
    {
        var interactions = _inventory.gameObject.AddComponent<InteractionManager>();
        Assert.AreEqual(5f, Call(interactions, "ResolveInteractionCastSeconds", new Interaction(InteractionType.Portal, _exit.gameObject)));
        Assert.AreEqual(5f, Call(interactions, "ResolveInteractionCastSeconds", new Interaction(InteractionType.PortalActivation, _exit.gameObject)));
        interactions.NotifyPortalTravelCompleted();
        Assert.AreEqual(0f, Get(interactions, "_nextPortalInteractAllowedTime"));
        Assert.AreEqual(0f, _portals.LinkedTravelCooldownRemaining);
    }

    [Test] public void FactionAssignmentsIncludeStageTwoAndThreeBossesAndGuards()
    {
        Assert.AreEqual(4, Enum.GetValues(typeof(ShooterFaction)).Length);
        const string root = "Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/";
        foreach (var entry in new[] {
            ("NormalEnemy/Scraper/ScraperShooterConfig.asset", ShooterFaction.Scraper),
            ("NormalEnemy/Butcher/ButcherShooterConfig.asset", ShooterFaction.Scraper),
            ("Boss/PunisherConfig.asset", ShooterFaction.Scraper),
            ("NormalEnemy/Raider/RaiderShooterConfig.asset", ShooterFaction.Raider),
            ("Boss/Stage2SniperBossConfig.asset", ShooterFaction.Raider),
            ("BossGuard/Stage2BossGuardConfig.asset", ShooterFaction.Raider),
            ("NormalEnemy/Agent/AgentShooterConfig.asset", ShooterFaction.Agent),
            ("Boss/Stage3RocketBossConfig.asset", ShooterFaction.Agent),
            ("BossGuard/Stage3BossGuardConfig.asset", ShooterFaction.Agent) })
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyShooterConfigSO>(root + entry.Item1);
            Assert.NotNull(config, entry.Item1);
            Assert.AreEqual(entry.Item2, config.Faction, entry.Item1);
        }
        foreach (ShooterFaction a in Enum.GetValues(typeof(ShooterFaction)))
            foreach (ShooterFaction b in Enum.GetValues(typeof(ShooterFaction)))
                Assert.AreEqual(a != b, ShooterFactionUtility.AreHostile(a, b));
    }

    [Test] public void EveryAuthoredRoomInheritsExactlyOneDeviceAndItsReplaceableVisual()
    {
        const string path = "Assets/Prefabs/GameScene/Sector/SectorObject/InteractableObject/PortalActivationDevice.prefab";
        var devicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.NotNull(devicePrefab);
        Assert.NotNull(devicePrefab.GetComponent<SectorPortalActivator>());
        Assert.IsTrue(devicePrefab.GetComponent<SphereCollider>().isTrigger);
        Assert.AreEqual(LayerMask.NameToLayer("ObjectItem"), devicePrefab.layer);
        Assert.NotNull(devicePrefab.transform.Find("Visual/TemporaryPanel_ReplaceThis"));
        foreach (string prefabPath in Directory.GetFiles("Assets/Prefabs/GameScene/Sector/Rooms", "*.prefab", SearchOption.AllDirectories))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath.Replace('\\', '/'));
            if (prefab == null || prefab.GetComponent<SectorRuntime>() == null) continue;
            var devices = prefab.GetComponentsInChildren<SectorPortalActivator>(true);
            Assert.AreEqual(1, devices.Length, prefabPath);
            Assert.IsFalse(devices[0].CanInteract, "Prefab assets must never activate the real raid.");
        }
    }

    [Test] public void FactoryDoesNotCreateAnyInteriorExtractionTerminal()
    {
        int before = _sector.transform.childCount;
        RaidStationFactory.Attach(_raid, _sector, new StageRoomNode(_sector.Coord, StageRoomType.Normal,
            StageRoomConnectionFlags.None) { raidRole = RaidRoomRole.Extraction }, new RaidRuntimeSettings());
        Assert.AreEqual(before, _sector.transform.childCount);
        Assert.IsEmpty(_sector.GetComponentsInChildren<RaidStation>(true));
        var settings = new RaidRuntimeSettings { rooms = new() { new RaidRoomPresetBinding { role = RaidRoomRole.Extraction } } };
        Assert.IsNull(settings.FindRoom(RaidRoomRole.Extraction));
    }

    [Test] public void OldSparseExitRolesMigrateWithoutRerollingAnyGraphConnections()
    {
        var old = JsonUtility.FromJson<StageMapLayout>(JsonUtility.ToJson(_layout));
        old.extractionPortalVersion = 0; old.extractionPortals.Clear();
        foreach (var coord in old.extractionRooms) { old.TryGetRoom(coord, out var room); room.raidRole = RaidRoomRole.Extraction; }
        var graph = old.rooms.ConvertAll(room => (room.coord, room.connections));
        Assert.IsTrue(RaidMapValidation.TryValidate(old, out var error), error);
        Assert.IsTrue(RaidExtractionPortalPolicy.TryPrepare(old, out error), error);
        Assert.IsTrue(RaidMapValidation.TryValidate(old, out error), error);
        CollectionAssert.AreEqual(graph, old.rooms.ConvertAll(room => (room.coord, room.connections)));
        CollectionAssert.AreEqual(_layout.extractionRooms, old.extractionRooms);
    }

    [Test] public void LegacyFourWayExitRelocatesOnlyItsMarkerAndKeepsTravelGraphAndPowerState()
    {
        Assert.IsTrue(RaidMapGenerator.TryGenerate(42, 1, 7, Vector2Int.left, new RaidMapSettings(), out var old, out var error), error);
        old.TryGetRoom(old.extractionSectorCoord, out var previous); previous.raidRole = RaidRoomRole.Normal;
        var middle = new Vector2Int(old.gridSize / 2, old.gridSize / 2);
        var interior = old.rooms.Find(room => room.roomType == StageRoomType.Normal && room.raidRole == RaidRoomRole.Normal &&
            !RaidExtractionPortalPolicy.TryGetUnusedDirection(room, old.stageSeed, out _) &&
            (room.coord.x - middle.x) * (old.extractionSectorCoord.x - middle.x) > 0 &&
            (room.coord.y - middle.y) * (old.extractionSectorCoord.y - middle.y) > 0);
        Assert.NotNull(interior);
        old.extractionSectorCoord = interior.coord; interior.raidRole = RaidRoomRole.Extraction;
        old.extractionPortalVersion = 0; old.extractionPortals.Clear();
        var graph = old.rooms.ConvertAll(room => (room.coord, room.connections));
        var state = new RaidRunSaveData { active = true, layout = old, playerSectorCoord = old.generatorRoomCoord,
            generatorPowered = true, hatchRevealed = true, remainingSeconds = 99 };
        Assert.IsTrue(state.TryValidate(out error), error);
        Assert.IsTrue(RaidExtractionPortalPolicy.TryPrepare(old, out error), error);
        Assert.IsTrue(state.TryValidate(out error), error);
        CollectionAssert.AreEqual(graph, old.rooms.ConvertAll(room => (room.coord, room.connections)));
        Assert.AreNotEqual(interior.coord, old.extractionSectorCoord);
        Assert.IsTrue(state.generatorPowered); Assert.AreEqual(99, state.remainingSeconds);
    }

    [Test] public void CorruptSavedRouteCannotOverwriteATravelDoorOrSilentlyReroll()
    {
        var route = _layout.extractionPortals[0]; _layout.TryGetRoom(route.coord, out var room);
        var connected = Array.Find((SectorPortalDirection[])Enum.GetValues(typeof(SectorPortalDirection)),
            direction => room.HasConnection(Flag(direction)));
        route.direction = connected;
        string before = JsonUtility.ToJson(_layout);
        Assert.IsFalse(RaidMapValidation.TryValidate(_layout, out _));
        Assert.IsFalse(RaidExtractionPortalPolicy.TryPrepare(_layout, out _));
        Assert.AreEqual(before, JsonUtility.ToJson(_layout));
    }

    [Test] public void SixStageDefinitionsHaveNoDedicatedExitBindingAndTheirNormalRoomsHaveFourPreplacedPortals()
    {
        for (int index = 1; index <= 6; index++)
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"Assets/ScriptableObject/GameScene/Stage/Stage{index}Definition.asset");
            Assert.NotNull(stage);
            Assert.IsFalse(stage.RaidRuntime.rooms.Exists(row => row != null && row.role == RaidRoomRole.Extraction));
            Assert.IsTrue(stage.TryPickSectorPreset(_layout.stageSeed, _sector.Coord, StageRoomType.Normal, out var preset));
            var runtime = preset.SectorPrefab;
            foreach (var edge in new[] { runtime.XMin, runtime.XMax, runtime.ZMin, runtime.ZMax })
                Assert.NotNull(edge?.portal?.GetComponentInChildren<SectorPortal>(true), preset.name);
        }
        Assert.IsFalse(File.Exists("Assets/ScriptableObject/GameScene/Stage/Sector/Preset/RaidRooms/Extraction/SectorPreset_Extraction_01.asset"));
        Assert.IsFalse(File.Exists("Assets/ScriptableObject/GameScene/Stage/Sector/Preset/Templates/SectorTemplate_ExtractionSectorBase.asset"));
    }

    private GameObject Go(string name, Transform parent = null)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        SceneManager.MoveGameObjectToScene(go, _preview); if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }
    private T Asset<T>() where T : ScriptableObject { var asset = ScriptableObject.CreateInstance<T>(); asset.hideFlags = HideFlags.HideAndDontSave; _assets.Add(asset); return asset; }
    private static void AssignEdge(SectorRuntime sector, SectorPortalDirection direction, SectorPortal portal)
    {
        var edge = new SectorEdge { portal = portal.gameObject };
        switch (direction)
        { case SectorPortalDirection.XMin: sector.XMin = edge; break; case SectorPortalDirection.XMax: sector.XMax = edge; break;
          case SectorPortalDirection.ZMin: sector.ZMin = edge; break; case SectorPortalDirection.ZMax: sector.ZMax = edge; break; }
    }
    private static object Get(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static StageRoomConnectionFlags Flag(SectorPortalDirection direction) => direction switch
    { SectorPortalDirection.XMin => StageRoomConnectionFlags.Left, SectorPortalDirection.XMax => StageRoomConnectionFlags.Right,
      SectorPortalDirection.ZMin => StageRoomConnectionFlags.Down, SectorPortalDirection.ZMax => StageRoomConnectionFlags.Up, _ => StageRoomConnectionFlags.None };
}
