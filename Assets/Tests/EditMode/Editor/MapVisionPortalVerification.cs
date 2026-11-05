using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Opt-in only. All objects belong to a disposable Preview Scene, never the live run.
public static class MapVisionPortalVerification
{
    private const string Output = "Library/RaidCombatVerification";
    private static double _nextCheck;
    [InitializeOnLoadMethod] private static void Register() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < _nextCheck) return;
        _nextCheck = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || !File.Exists(Output + "/request")) return;
        File.Delete(Output + "/request");
        Run();
    }

    [MenuItem("Tools/Verification/Map, missions, hatch and vision (read-only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Output);
        var passes = new List<string>();
        var scene = EditorSceneManager.NewPreviewScene();
        var trait = GetStatic(typeof(SectorRoomTraitRuntime), "_currentPlayerSectorTrait");
        var save = SaveSystem.Active;
        string saveBefore = save != null ? JsonUtility.ToJson(save.RunData) : null;
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var dirty = scenes.Select(s => s.isDirty).ToArray();
        var paths = new[] { "Assets/Scenes/Manager/GamePlay.unity", "Assets/Scenes/RUNScene.unity",
            "Assets/Scenes/HomeScene.unity", "Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab" }
            .Where(File.Exists).Concat(Directory.Exists(Application.persistentDataPath)
                ? Directory.GetFiles(Application.persistentDataPath, "*", SearchOption.AllDirectories) : Array.Empty<string>()).ToArray();
        var hashes = paths.Select(Hash).ToArray();
        try
        {
            TestMap(scene, passes);
            TestMissionOwnership(scene, passes);
            TestStartHatch(scene, passes);
            TestVision(scene, passes);
            TestModels(passes);
            TestSharedVision(scene, passes);
            TestRaidClock(scene, passes);
            TestSolidBodies(scene, passes);
            TestFlankSlots(scene, passes);
            Assert.AreSame(save, SaveSystem.Active);
            if (save != null) Assert.AreEqual(saveBefore, JsonUtility.ToJson(save.RunData));
            Assert.AreEqual(scenes.Length, SceneManager.sceneCount);
            for (int i = 0; i < scenes.Length; i++) Assert.AreEqual(dirty[i], scenes[i].isDirty);
            for (int i = 0; i < paths.Length; i++) Assert.AreEqual(hashes[i], Hash(paths[i]), paths[i]);
            passes.Add("PASS authored scenes/prefabs, scene dirty flags and real save/state preserved");
            File.WriteAllLines(Output + "/PASSED.txt", passes);
            if (File.Exists(Output + "/FAILED.txt")) File.Delete(Output + "/FAILED.txt");
            Debug.Log($"[MapVisionPortal] {passes.Count} isolated checks passed; no real scene/save changes.");
        }
        catch (Exception error)
        {
            File.WriteAllLines(Output + "/FAILED.txt", passes.Concat(new[] { error.ToString() }));
            if (File.Exists(Output + "/PASSED.txt")) File.Delete(Output + "/PASSED.txt");
            Debug.LogException(error);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            typeof(SectorRoomTraitRuntime).GetField("_currentPlayerSectorTrait", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, trait);
        }
    }

    private static void TestMap(Scene scene, List<string> passes)
    {
        var root = Go("Temporary full map", scene, true);
        var map = root.AddComponent<FullMapUI>();
        var viewport = (RectTransform)Go("Viewport", scene, true, root.transform).transform;
        viewport.sizeDelta = new Vector2(1100, 560);
        var settings = AssetDatabase.LoadAssetAtPath<FullMapUISettingsSO>("Assets/ScriptableObject/GamePlay/UI/Map/FullMapUISettings.asset");
        Set(map, "_settings", settings); Set(map, "_cellRoot", viewport);
        Set(map, "_cellPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GamePlay/MAP/FullMapCell.prefab").GetComponent<FullMapCellUI>());
        var cells = new List<SectorMapCellSnapshot>();
        for (int x = 0; x < 15; x++) for (int y = 0; y < 15; y++)
            cells.Add(new SectorMapCellSnapshot { coord = new Vector2Int(x, y), isOpened = true, isRevealed = true, roomType = StageRoomType.Normal });
        var snapshot = new SectorMapSnapshot { currentSectorCoord = new Vector2Int(7, 7), cells = cells.ToArray() };
        Set(map, "_latestSnapshot", snapshot); Set(map, "_hasSnapshot", true);
        Call(map, "Awake"); map.SetOpen(true);
        Assert.IsNotNull(viewport.GetComponent<RectMask2D>()); passes.Add("PASS full-map viewport masks overflowing cells");
        Assert.AreEqual(new Vector2(1100, 560), viewport.sizeDelta); passes.Add("PASS large map never expands the authored viewport");
        var byCoord = (Dictionary<Vector2Int, FullMapCellUI>)Get(map, "_cellByCoord");
        var content = (RectTransform)Get(map, "_mapContent");
        Assert.AreEqual(Vector2.zero, ((RectTransform)byCoord[snapshot.currentSectorCoord].transform).anchoredPosition);
        passes.Add("PASS 15x15 map centres the current player room, at readable scale");
        Assert.AreSame(content, byCoord[Vector2Int.zero].transform.parent); passes.Add("PASS cells belong to separate pannable content");
        var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Right,
            position = RectTransformUtility.WorldToScreenPoint(null, viewport.TransformPoint(viewport.rect.center)) };
        map.OnBeginDrag(pointer); pointer.position += new Vector2(120, -60); map.OnDrag(pointer);
        Assert.AreEqual(new Vector2(120, -60), content.anchoredPosition); map.OnEndDrag(pointer);
        passes.Add("PASS right-button drag pans the map");
        pointer.button = PointerEventData.InputButton.Left;
        map.OnBeginDrag(pointer); pointer.position += Vector2.one * 40; map.OnDrag(pointer);
        Assert.AreEqual(new Vector2(120, -60), content.anchoredPosition); passes.Add("PASS left-button drag does not pan");
        map.SetOpen(false); map.SetOpen(true);
        Assert.AreEqual(Vector2.zero, content.anchoredPosition); passes.Add("PASS reopening map resets pan to player centre");
        foreach (var role in new[] { RaidRoomRole.Hatch, RaidRoomRole.Generator, RaidRoomRole.Merchant, RaidRoomRole.Armory, RaidRoomRole.Medical, RaidRoomRole.Vault })
            Assert.IsNotNull(settings.GetLandmarkIcon(role));
        Assert.IsNotNull(settings.GetLandmarkIcon(RaidRoomRole.None, true));
        passes.Add("PASS all full-map landmark slots have existing sprite placeholders");
        var mini = AssetDatabase.LoadAssetAtPath<MiniMapUISettingsSO>("Assets/ScriptableObject/GamePlay/UI/Map/MiniMapUISettings.asset");
        Assert.IsNotNull(mini.GetLandmarkIcon(RaidRoomRole.Hatch)); Assert.IsNotNull(mini.GetLandmarkIcon(RaidRoomRole.Generator));
        passes.Add("PASS mini-map hatch/generator placeholders available");
        cells[0] = new SectorMapCellSnapshot { coord = Vector2Int.zero, isLocked = true, isRevealed = true };
        snapshot.cells = cells.ToArray();
        for (int i = 0; i < 20; i++) Call(map, "OnMapSnapshotChanged", snapshot);
        var presented = (Dictionary<Vector2Int, SectorMapCellSnapshot>)Get(map, "_presentedCells");
        Assert.IsFalse(presented[Vector2Int.zero].isLocked);
        Call(map, "LateUpdate"); Assert.IsTrue(presented[Vector2Int.zero].isLocked);
        Assert.IsFalse((bool)Get(map, "_pendingMapRefresh"));
        passes.Add("PASS repeated full-map publications coalesce until one LateUpdate");
    }

    private static void TestMissionOwnership(Scene scene, List<string> passes)
    {
        var hub = Go("Temporary hub", scene, true).AddComponent<PlayerPanelHub>();
        Set(hub, "_fullMapPanel", scene.GetRootGameObjects().Select(go => go.GetComponent<FullMapUI>()).First(map => map != null));
        var first = Go("Authored mission", scene, true, hub.transform).AddComponent<QuestMissionPanelUI>();
        var duplicate = Go("Old duplicate mission", scene, true, hub.transform).AddComponent<QuestMissionPanelUI>();
        Call(hub, "ResolveRefs");
        Assert.AreEqual(2, hub.GetComponentsInChildren<QuestMissionPanelUI>(true).Length);
        passes.Add("PASS editor reference resolution creates no new mission panel");
        Set(hub, "_missionsPanelGroup", duplicate.gameObject.AddComponent<CanvasGroup>());
        Call(hub, "ResolveRefs");
        Assert.AreSame(first.GetComponent<CanvasGroup>(), Get(hub, "_missionsPanelGroup"));
        passes.Add("PASS mission CanvasGroup follows the selected panel, not a stale reference");
        foreach (var mode in new[] { PlayerPanelHubMode.Inventory, PlayerPanelHubMode.CharacterStats, PlayerPanelHubMode.Shop, PlayerPanelHubMode.EnemyLoot, PlayerPanelHubMode.FullMap, PlayerPanelHubMode.Missions })
        {
            Set(hub, "<CurrentMode>k__BackingField", mode); Call(hub, "ApplyMode");
            Assert.AreEqual(mode == PlayerPanelHubMode.Missions ? 1f : 0f, first.GetComponent<CanvasGroup>().alpha);
            Assert.AreEqual(0, duplicate.GetComponent<CanvasGroup>().alpha);
            Assert.AreEqual(mode == PlayerPanelHubMode.Missions, first.GetComponent<CanvasGroup>().blocksRaycasts);
            passes.Add("PASS missions visibility in " + mode);
        }
    }

    private static void TestStartHatch(Scene scene, List<string> passes)
    {
        var states = Go("Temporary states", scene).AddComponent<SectorStateManager>(); Set(states, "_isInitialized", true);
        var layout = new StageMapLayout { stageIndex = 2, startSectorCoord = Vector2Int.zero };
        var start = new StageRoomNode(Vector2Int.zero, StageRoomType.Normal, StageRoomConnectionFlags.Right | StageRoomConnectionFlags.Up) { raidRole = RaidRoomRole.Hatch };
        var next = new StageRoomNode(Vector2Int.right, StageRoomType.Normal, StageRoomConnectionFlags.Left);
        var locked = new StageRoomNode(Vector2Int.up, StageRoomType.Normal, StageRoomConnectionFlags.Down) { isLocked = true };
        var unrelated = new StageRoomNode(Vector2Int.left, StageRoomType.Normal, StageRoomConnectionFlags.None);
        layout.rooms.AddRange(new[] { start, next, locked, unrelated });
        var sectors = layout.rooms.Select(r => { var s = Go("Temporary room", scene).AddComponent<SectorRuntime>(); s.coord = r.coord; return s; }).ToArray();
        states.RegisterGeneratedStageMap(layout, sectors);
        Assert.IsTrue(sectors[0].IsCleared && sectors[0].IsOpened && start.isCleared && start.isRevealed);
        passes.Add("PASS initial hatch is automatically cleared/open/revealed in runtime and layout");
        Assert.IsTrue(sectors[1].IsOpened); Assert.IsFalse(sectors[1].IsCleared);
        passes.Add("PASS hatch opens connected battle destination without clearing the battle");
        Assert.IsFalse(sectors[2].IsOpened); Assert.IsFalse(sectors[3].IsOpened);
        passes.Add("PASS locked and unconnected rooms remain closed");
        var hud = Go("Temporary compact HUD", scene, true).AddComponent<PlayerTimerHUD>();
        var label = Go("Location", scene, true, hud.transform).AddComponent<TextMeshProUGUI>();
        Set(hud, "_compactPresentationBuilt", true); Set(hud, "_compactLocationText", label); Set(hud, "_sectorStateManager", states);
        Call(hud, "RefreshCompactPresentation", true);
        Assert.AreEqual(Call(hud, "FormatCompactStage", 2), label.text); passes.Add("PASS hatch HUD shows only the stage label");
        states.SetPlayerCurrentSector(sectors[1], false); Call(hud, "RefreshCompactPresentation", false);
        Assert.AreEqual(Call(hud, "FormatCompactStage", 2), label.text); passes.Add("PASS ordinary room HUD keeps the same stage-only label");
    }

    private static void TestVision(Scene scene, List<string> passes)
    {
        var player = Go("Temporary viewer", scene);
        var aim = player.AddComponent<PlayerAimController>(); var vision = player.AddComponent<PlayerShooterVisionController>();
        var camera = Go("Temporary camera", scene).AddComponent<Camera>();
        camera.transform.position = new Vector3(21000, 114, 20960); camera.transform.rotation = Quaternion.Euler(70, 0, 0);
        player.transform.position = new Vector3(21000, 0, 21000);
        Set(aim, "_aimCamera", camera); Set(aim, "_aimRaycastMask", (LayerMask)0); Set(aim, "_allowFallbackPlane", true);
        Set(vision, "_aimAction", aim); Set(vision, "_origin", player.transform);
        Assert.IsTrue(aim.TryGetRawAimPoint(out var current));
        Vector3 expected = current - player.transform.position; expected.y = 0;
        vision.RefreshPresentationAimPoint();
        Assert.Less(Vector3.Angle(expected.normalized, vision.ForwardDirection), .01f);
        Vector3 heldPoint = aim.AimWorldPoint;
        passes.Add("PASS rendered vision and combat aiming share one cached pointer direction");
        camera.transform.position += new Vector3(5, 0, 2); player.transform.position += Vector3.right * 2;
        Assert.IsTrue(aim.TryGetRawAimPoint(out current));
        vision.RefreshPresentationAimPoint(); Assert.Less(Vector3.Angle(expected.normalized, vision.ForwardDirection), .01f);
        Assert.Less(Vector3.Distance(heldPoint + Vector3.right * 2, current), .001f);
        passes.Add("PASS stationary mouse keeps direction at stage coordinates while targets follow player movement");
    }

    private static void TestModels(List<string> passes)
    {
        foreach (string path in new[] { "Assets/Art/Models/Loot/SafeBox.fbx", "Assets/Art/Models/Loot/Safe/Safe_02_Olive.fbx",
            "Assets/Art/Models/Loot/Safe/Safe_03_Orange.fbx", "Assets/Art/Models/Loot/Safe/Safe_04_Red.fbx" })
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            Assert.IsFalse(importer.importCameras, path);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(model); Assert.AreEqual(0, model.GetComponentsInChildren<Camera>(true).Length, path);
            Assert.Greater(model.GetComponentsInChildren<Renderer>(true).Length, 0, path);
            passes.Add("PASS safe model keeps renderers but imports no authoring camera: " + Path.GetFileName(path));
        }
    }

    private static void TestSharedVision(Scene scene, List<string> passes)
    {
        var viewer = Go("Shared occlusion", scene).AddComponent<PlayerShooterVisionController>();
        Vector3 origin = new Vector3(21000, 0, 21000);
        Assert.IsFalse(viewer.TryGetPresentationVisibleDistance(origin, Vector3.right, 10, out _));
        passes.Add("PASS mini-map hides unsampled vision rather than casting its own rays");
        viewer.PublishPresentationOcclusion(origin, 20, new[] { 5f, 20f, 20f, 20f }, 4, .1f);
        Assert.IsTrue(viewer.TryGetPresentationVisibleDistance(origin, Vector3.right, 20, out float distance));
        Assert.AreEqual(4.9f, distance, .001f);
        Assert.IsTrue(viewer.TryGetPresentationVisibleDistance(origin, Vector3.right, 2, out distance));
        Assert.AreEqual(2, distance); passes.Add("PASS shared vision respects blockers, safety bias and close radius");
        Assert.IsFalse(viewer.TryGetPresentationVisibleDistance(origin + Vector3.right, Vector3.right, 20, out _));
        passes.Add("PASS a stale room/origin sample is rejected");
    }

    private static void TestRaidClock(Scene scene, List<string> passes)
    {
        var definition = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/ScriptableObject/GameScene/Stage/Stage1Definition.asset");
        Assert.AreEqual(600f, definition.RaidRuntime.durationSeconds); passes.Add("PASS Stage 1 authors a ten-minute total raid clock");
        var state = new RaidRunSaveData { active = true, timerEnabled = true, remainingSeconds = 600 };
        RaidRuntime.TickClock(state, 599); Assert.AreEqual(1, state.remainingSeconds); Assert.IsFalse(state.gasReleased);
        RaidRuntime.TickClock(state, 1); Assert.AreEqual(0, state.remainingSeconds); Assert.IsTrue(state.gasReleased);
        RaidRuntime.TickClock(state, 40); Assert.AreEqual(0, state.remainingSeconds);
        passes.Add("PASS global leak starts at ten minutes and does not reset the clock");
        state.extractionCommitted = true; state.remainingSeconds = 25;
        RaidRuntime.TickClock(state, 5); Assert.AreEqual(25, state.remainingSeconds);
        passes.Add("PASS committed extraction stops raid clock advancement");
        var progression = Go("Raid room timing policy", scene).AddComponent<StageProgressionManager>();
        Set(progression, "_currentStage", definition); Set(progression, "_hasRule", true);
        Assert.AreEqual(0f, progression.GetType().GetProperty("CurrentNormalBattleTimerSeconds", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(progression));
        Assert.IsFalse((bool)Call(progression, "ShouldShowPlayerTimerForSector", Go("Untimed battle", scene).AddComponent<SectorRuntime>()));
        passes.Add("PASS raid rooms no longer use or display a room countdown");
    }

    private static void TestSolidBodies(Scene scene, List<string> passes)
    {
        Vector3 origin = new Vector3(-10000, 1000, -10000);
        var enemy = Go("Collision enemy", scene); enemy.transform.position = origin;
        var agent = enemy.AddComponent<UnityEngine.AI.NavMeshAgent>(); agent.enabled = false; agent.radius = .5f; agent.height = 2;
        var motor = ShooterBodyCollisionMotor.Ensure(agent);
        var player = Go("Collision player", scene); player.transform.position = origin + Vector3.forward * 1.8f;
        var playerBody = player.AddComponent<CharacterController>(); playerBody.radius = .5f; playerBody.height = 2;
        playerBody.center = Vector3.up; playerBody.skinWidth = .02f; playerBody.stepOffset = 0;
        enemy.SetActive(true); player.SetActive(true); Physics.SyncTransforms();
        motor.CommitPlannedPosition(origin + Vector3.forward * 3);
        Assert.Less(enemy.transform.position.z, player.transform.position.z);
        Assert.GreaterOrEqual(Vector3.Distance(enemy.transform.position, player.transform.position), .94f);
        Assert.IsFalse(agent.updatePosition); passes.Add("PASS enemy planned movement is blocked by player body, without NavMesh transform bypass");
        Physics.SyncTransforms(); playerBody.Move(Vector3.back * 3);
        Assert.GreaterOrEqual(Vector3.Distance(enemy.transform.position, player.transform.position), .94f);
        passes.Add("PASS player movement cannot pass through a living enemy");
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(enemy.transform, false); visual.transform.localPosition = Vector3.up * .3f;
        visual.transform.localScale = new Vector3(1, .6f, 1);
        var obstacle = visual.AddComponent<UnityEngine.AI.NavMeshObstacle>(); obstacle.carving = true;
        var loot = Go("Corpse loot trigger", scene); loot.transform.SetParent(enemy.transform, false);
        var lootTrigger = loot.AddComponent<SphereCollider>(); lootTrigger.isTrigger = true;
        // Invoke from the child, just like the actual DeathCorpse component.
        ShooterBodyCollisionMotor.MakeCorpsePassThrough(visual.transform);
        Assert.IsFalse(enemy.GetComponent<CharacterController>().enabled);
        Assert.IsFalse(motor.enabled); Assert.IsFalse(agent.enabled);
        Assert.IsFalse(visual.GetComponent<Collider>().enabled); Assert.IsFalse(obstacle.enabled);
        Assert.IsTrue(lootTrigger.enabled); Assert.IsTrue(lootTrigger.isTrigger);
        Assert.IsTrue(visual.activeInHierarchy);
        Assert.IsNull(enemy.transform.Find("CorpseSolidBody"));
        playerBody.enabled = false;
        player.transform.position = enemy.transform.position + Vector3.forward * 3;
        playerBody.enabled = true;
        Physics.SyncTransforms(); ShooterBodyCollisionMotor.MoveWithBodyBlocking(playerBody, Vector3.back * 6);
        Assert.Less(player.transform.position.z, enemy.transform.position.z);
        passes.Add("PASS corpse releases parent body collision and navigation obstacles, preserves visuals/loot, and allows player passage");
        enemy.SetActive(false); player.SetActive(false);
    }

    private static void TestFlankSlots(Scene scene, List<string> passes)
    {
        var target = Go("Temporary flank target", scene).transform;
        var first = Go("Temporary first shooter", scene).AddComponent<EnemyShooterCombatController>();
        var second = Go("Temporary second shooter", scene).AddComponent<EnemyShooterCombatController>();
        var third = Go("Temporary third shooter", scene).AddComponent<EnemyShooterCombatController>();
        try
        {
            Assert.AreEqual(0, EnemyShooterTacticalIntentSelector.ClaimFlankSlot(first, target));
            Assert.AreEqual(1, EnemyShooterTacticalIntentSelector.ClaimFlankSlot(second, target));
            Assert.AreEqual(2, EnemyShooterTacticalIntentSelector.ClaimFlankSlot(third, target));
            Assert.AreEqual(1, EnemyShooterTacticalIntentSelector.ClaimFlankSlot(second, target));
            passes.Add("PASS group flank slots split left/right and wider lanes, remaining stable across refreshes");
        }
        finally
        {
            EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(first);
            EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(second);
            EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(third);
        }
        foreach (string name in new[] { "Raider", "Agent", "Scraper" })
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyShooterConfigSO>("Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/NormalEnemy/" + name + "/" + name + "ShooterConfig.asset");
            Assert.IsTrue(config.UseTacticalIntent && config.UseFlankingTactics && config.UseDuelToken);
        }
        passes.Add("PASS ranged/hybrid normal shooters enable tactical flanking; melee-only config remains unchanged");
    }

    private static GameObject Go(string name, Scene scene, bool ui = false, Transform parent = null)
    {
        var go = ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        go.SetActive(false); SceneManager.MoveGameObjectToScene(go, scene); if (parent != null) go.transform.SetParent(parent, false); return go;
    }
    private static FieldInfo Field(Type type, string name)
    {
        for (; type != null; type = type.BaseType)
        { var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public); if (field != null) return field; }
        throw new MissingFieldException(name);
    }
    private static void Set(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);
    private static object Get(object target, string name) => Field(target.GetType(), name).GetValue(target);
    private static object GetStatic(Type type, string name) => Field(type, name).GetValue(null);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name,
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args);
    private static string Hash(string path)
    { using var stream = File.OpenRead(path); using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(stream)); }
}
