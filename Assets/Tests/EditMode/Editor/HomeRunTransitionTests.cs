using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Disposable Home/RUN Preview Scenes; never loads the authored Home or touches a real player/save.</summary>
public sealed class HomeRunTransitionTests
{
    private Scene _home, _run;
    private readonly List<Object> _assets = new();
    private PlayerCharacter _player;
    private PlayerInventoryRuntime _inventory;
    private SpawnSystem _spawn;
    private CameraManager _camera;
    private Camera _view;
    private SceneLoader _loader;
    private bool _paused;
    private float _timeScale, _previousTimeScale;

    [SetUp] public void Setup()
    {
        _paused = GamePause.IsPaused; _timeScale = Time.timeScale;
        _previousTimeScale = (float)Field(typeof(GamePause), "_prevTimeScale").GetValue(null);
        Field(typeof(GamePause), "_isPaused").SetValue(null, false); Time.timeScale = 1;
        _home = EditorSceneManager.NewPreviewScene(); _run = EditorSceneManager.NewPreviewScene();
        _player = Go("Temporary player", _home).AddComponent<PlayerCharacter>();
        _inventory = _player.gameObject.AddComponent<PlayerInventoryRuntime>();
        _spawn = Go("Temporary spawner", _run).AddComponent<SpawnSystem>();
        Set(_spawn, "_spawnedPlayer", _player.transform);
        _view = Go("Temporary camera", _run).AddComponent<Camera>();
        _camera = Go("Temporary camera manager", _run).AddComponent<CameraManager>();
        Set(_camera, "mainCamera", _view); Set(_camera, "cameraTarget", _view.transform);
        Set(_camera, "enableCharacterOcclusionSilhouette", false);
        Set(_spawn, "_cameraManager", _camera);
        _loader = Go("Temporary scene loader", _run).AddComponent<SceneLoader>();
        Set(_loader, "_loadingGroup", Go("Temporary loading UI", _run).AddComponent<CanvasGroup>());
    }
    [TearDown] public void Cleanup()
    {
        if (_home.IsValid()) EditorSceneManager.ClosePreviewScene(_home);
        if (_run.IsValid()) EditorSceneManager.ClosePreviewScene(_run);
        foreach (var asset in _assets) if (asset != null) Object.DestroyImmediate(asset);
        _assets.Clear();
        Field(typeof(GamePause), "_isPaused").SetValue(null, _paused);
        Field(typeof(GamePause), "_prevTimeScale").SetValue(null, _previousTimeScale);
        Time.timeScale = _timeScale;
    }

    [TestCase(true)] [TestCase(false)]
    public void DeploymentMovesSamePlayerIntoRunAndSurvivesHomeUnload(bool parentToRoom)
    {
        var homeParent = Go("Home room", _home).transform; _player.transform.SetParent(homeParent, true);
        var destination = Go("Stage entry", _run).transform; destination.position = new Vector3(21000, 0, 21000);
        Set(_spawn, "_parentPlayerToCurrentSector", parentToRoom);
        var inventory = _inventory; var player = _player;
        Call(_spawn, "MovePlayerTo", destination, destination);
        Assert.AreEqual(_run, _player.gameObject.scene);
        Assert.AreEqual(parentToRoom ? destination : null, _player.transform.parent);
        Assert.IsTrue(SceneLoader.CanUnloadHomeEnvironment(_home, new[] { _player }));
        AssertCameraFramesPlayer();
        EditorSceneManager.ClosePreviewScene(_home);
        Assert.IsTrue(_player != null); Assert.AreSame(player, _player); Assert.AreSame(inventory, _player.GetComponent<PlayerInventoryRuntime>());
        AssertCameraFramesPlayer();
    }

    [Test] public void HomeUnloadRefusesEvenInactivePlayerStillOwnedByHome()
    {
        Assert.IsFalse(SceneLoader.CanUnloadHomeEnvironment(_home, new[] { _player }));
        _player.gameObject.SetActive(false);
        Assert.IsFalse(SceneLoader.CanUnloadHomeEnvironment(_home, new[] { _player }));
        Assert.IsFalse(SceneLoader.CanUnloadHomeEnvironment(default, new[] { _player }));
    }

    [Test] public void RecreatedHomeReceivesExistingPlayerAndCameraWithoutRecreatingInventory()
    {
        var entry = Go("RUN entry", _run).transform; entry.position = new Vector3(21000, 0, 21000);
        Call(_spawn, "MovePlayerTo", entry, entry); EditorSceneManager.ClosePreviewScene(_home);
        _home = EditorSceneManager.NewPreviewScene();
        var arrival = Go("Reloaded Home arrival", _home).transform; arrival.position = new Vector3(12, 0, 23);
        Call(_spawn, "MovePlayerTo", arrival, arrival);
        Assert.AreEqual(_home, _player.gameObject.scene);
        Assert.AreSame(_inventory, _player.GetComponent<PlayerInventoryRuntime>()); AssertCameraFramesPlayer();
    }

    [Test] public void TeleportReplacesStaleCameraTargetAndClearsLookAheadAndVelocity()
    {
        var stale = Go("Previous target", _run).transform;
        _camera.SetFollowTarget(stale);
        Set(_camera, "_followVelocity", Vector3.one * 1000); Set(_camera, "_smoothedLookAhead", Vector3.one * 500);
        var arrival = Go("Far room", _run).transform; arrival.position = new Vector3(-18000, 0, 25000);
        Call(_spawn, "MovePlayerTo", arrival, arrival);
        Assert.AreSame(_player.transform, Get(_camera, "followTarget"));
        Assert.AreEqual(Vector3.zero, Get(_camera, "_followVelocity"));
        Assert.AreEqual(Vector3.zero, Get(_camera, "_smoothedLookAhead")); AssertCameraFramesPlayer();
    }

    [Test] public void ReturnWaitsForDepartureBeforeEnteringHomeOrCommittingExtraction()
    {
        Set(_loader, "_homeEnvironmentUnloadInProgress", true);
        int enters = 0, completions = 0;
        var routine = (IEnumerator)Call(_loader, "HomeTransitionRoutine", new Func<bool>(() => { enters++; return true; }),
            new Action<bool>(_ => completions++));
        Assert.IsTrue(routine.MoveNext()); Assert.IsTrue(routine.MoveNext());
        Assert.AreEqual(0, enters); Assert.AreEqual(0, completions);
        Assert.IsTrue(GamePause.IsPaused); Assert.AreEqual(1f, ((CanvasGroup)Get(_loader, "_loadingGroup")).alpha);
        (routine as IDisposable)?.Dispose();
    }

    [Test] public void MissingReferencesOrBusyLoaderRejectReturnWithoutStartingAnyLoad()
    {
        int invoked = 0;
        Assert.IsFalse(_loader.RequestHomeTransition(() => { invoked++; return true; }, _ => invoked++));
        var homeAsset = Asset<GameSceneSO>(); Set(_loader, "_homeScene", homeAsset); Set(_loader, "_isLoading", true);
        Assert.IsFalse(_loader.RequestHomeTransition(() => { invoked++; return true; }, _ => invoked++));
        Assert.AreEqual(0, invoked); Assert.IsFalse(GamePause.IsPaused);
    }

    [Test] public void PendingExtractionSnapshotResumesRaidInsteadOfRecordingPrematureSuccess()
    {
        var raid = Go("Temporary raid", _run).AddComponent<RaidRuntime>();
        var state = new RaidRunSaveData { active = true, extractionCommitted = true, generatorPowered = true,
            layout = new StageMapLayout { gridSize = 14, runSeed = 42, stageIndex = 1 } };
        Set(raid, "<State>k__BackingField", state); Set(raid, "_ready", true); Set(raid, "_transaction", true);
        var snapshot = new RunSaveData(); raid.Capture(snapshot);
        Assert.IsTrue(snapshot.raid.active); Assert.IsFalse(snapshot.raid.extractionCommitted);
        Assert.IsTrue(state.extractionCommitted); Assert.IsTrue(snapshot.raid.generatorPowered);
        Assert.AreEqual(42, snapshot.raid.layout.runSeed);
        state.active = false; raid.Capture(snapshot);
        Assert.IsFalse(snapshot.raid.active); Assert.IsTrue(snapshot.raid.extractionCommitted);
    }

    [Test] public void FailedReturnReleasesExtractionLockAndAllowsRetryWithoutInventoryMutation()
    {
        var raid = Go("Temporary raid", _run).AddComponent<RaidRuntime>();
        var state = new RaidRunSaveData { active = true, extractionCommitted = true };
        Set(raid, "<State>k__BackingField", state); Set(raid, "_transaction", true);
        int notifications = 0; raid.StateChanged += () => notifications++;
        Call(raid, "CompleteExtraction", false, Vector3.zero);
        Assert.IsFalse(state.extractionCommitted); Assert.IsTrue(state.active); Assert.IsFalse((bool)Get(raid, "_transaction"));
        Assert.AreSame(_inventory, _player.GetComponent<PlayerInventoryRuntime>()); Assert.AreEqual(1, notifications);
        Call(raid, "CompleteExtraction", false, Vector3.zero); Assert.AreEqual(1, notifications);
    }

    private void AssertCameraFramesPlayer()
    {
        Assert.AreSame(_player.transform, Get(_camera, "followTarget"));
        Vector3 focus = _player.transform.position + Vector3.up;
        Assert.That(Vector3.Dot(_view.transform.forward, (focus - _view.transform.position).normalized), Is.GreaterThan(.9999f));
        Assert.That(Vector3.Distance(_view.transform.position, focus), Is.EqualTo(120f).Within(.01f));
    }
    private GameObject Go(string name, Scene scene)
    { var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave }; SceneManager.MoveGameObjectToScene(go, scene); return go; }
    private T Asset<T>() where T : ScriptableObject
    { var asset = ScriptableObject.CreateInstance<T>(); asset.hideFlags = HideFlags.HideAndDontSave; _assets.Add(asset); return asset; }
    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new MissingFieldException(type.Name, name);
    private static object Get(object obj, string name) => Field(obj.GetType(), name).GetValue(obj);
    private static void Set(object obj, string name, object value) => Field(obj.GetType(), name).SetValue(obj, value);
    private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
}
