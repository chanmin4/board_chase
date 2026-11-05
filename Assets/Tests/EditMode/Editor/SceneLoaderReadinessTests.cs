using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Readiness contract fixtures only; never loads a save or changes an authored scene.</summary>
public sealed class SceneLoaderReadinessTests
{
    private Scene _scene;
    private SectorStateManager _manager;
    private StageProgressionManager _progression;
    private StageSectorInstantiator _builder;
    private SectorRuntime _home;
    private SaveSystem _originalSave;

    [SetUp] public void Setup()
    {
        _originalSave = SaveSystem.Active;
        _scene = EditorSceneManager.NewPreviewScene();
        var host = Go("Readiness fixture");
        _manager = host.AddComponent<SectorStateManager>();
        _builder = host.AddComponent<StageSectorInstantiator>();
        _progression = host.AddComponent<StageProgressionManager>();
        Set(_progression, "_sectorStateManager", _manager);
        Set(_progression, "_stageSectorInstantiator", _builder);
        _home = Go("Prepared authored Home fixture").AddComponent<SectorRuntime>();
        _home.gameObject.AddComponent<HomeSector>().Initialize(_home);
        _home.gameObject.AddComponent<HideoutRuntime>().Initialize(_home);
        _home.SetRuntimeInfo(Vector2Int.zero, true, true);
        _home.SetCleared(true);
        Set(_builder, "_hideoutSector", _home);
        Property(_manager, "CurrentSector", _home);
        Property(_progression, "IsInitialRuntimePrepared", true);
        Property(_progression, "InitialEntryRequiresGeneratedMap", false);
    }

    [TearDown] public void Cleanup()
    {
        if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        Assert.AreSame(_originalSave, SaveSystem.Active, "Fixture changed the real SaveSystem.");
    }

    [Test] public void FreshHomeDoesNotRequireAnUnbuiltRaidMap()
    {
        Assert.IsNull(_manager.CurrentStageMapLayout);
        Assert.IsNull(_manager.StartSector);
        Assert.IsEmpty(_manager.Sectors);
        Assert.IsTrue(Ready(true, false, false, out var reason), reason);
    }

    [Test] public void InitialPreparationMustFinishBeforeOpeningTheGate()
    {
        RegisterMap(); // Even an old/partial map cannot imply successful initial entry.
        Property(_progression, "IsInitialRuntimePrepared", false);
        Assert.IsFalse(Ready(true, false, false, out var reason));
        StringAssert.Contains("still preparing", reason);
    }

    [Test] public void ExplicitPreparationFailureCannotFallBackToAValidHome()
    {
        Property(_progression, "IsInitialRuntimePrepared", false);
        Property(_progression, "InitialRuntimePreparationFailure", "Saved raid could not be resumed.");
        Assert.IsFalse(Ready(true, false, false, out var reason));
        Assert.AreEqual("Saved raid could not be resumed.", reason);
    }

    [Test] public void ResumeAndLegacyStartupStillRequireTheirGeneratedMap()
    {
        Property(_progression, "InitialEntryRequiresGeneratedMap", true);
        Assert.IsFalse(Ready(true, false, false, out var reason));
        StringAssert.Contains("StageMapLayout is missing", reason);
        RegisterMap();
        Assert.IsTrue(Ready(true, false, false, out reason), reason);
    }

    [Test] public void DeploymentNeverUsesTheInitialHomeException()
    {
        Assert.IsFalse(Ready(false, true, true, out var reason));
        StringAssert.Contains("StageMapLayout is missing", reason);
    }

    [Test] public void BothHomeAndDeploymentRequireAPlayerWhenRequested()
    {
        Assert.IsFalse(Ready(true, true, false, out var reason));
        StringAssert.Contains("PlayerCharacter", reason);
        Assert.IsTrue(Ready(true, true, true, out reason), reason);
        RegisterMap();
        Assert.IsFalse(Ready(false, true, false, out reason));
        StringAssert.Contains("PlayerCharacter", reason);
        Assert.IsTrue(Ready(false, true, true, out reason), reason);
    }

    [Test] public void HomeMustBeTheActualCurrentSector()
    {
        Property(_manager, "CurrentSector", null);
        Assert.IsFalse(Ready(true, false, false, out var reason));
        StringAssert.Contains("CurrentSector", reason);
        Property(_manager, "CurrentSector", Go("Unprepared replacement").AddComponent<SectorRuntime>());
        Assert.IsFalse(Ready(true, false, false, out reason));
    }

    [Test] public void MissingCachedHideoutCannotPassUsingMarkersAlone()
    {
        Set(_builder, "_hideoutSector", null);
        Assert.IsFalse(Ready(true, false, false, out _));
    }

    [Test] public void MissingManagerCannotPassHomeOrDeployment()
    {
        Assert.IsFalse(Check(null, _progression, true, false, false, out var reason));
        StringAssert.Contains("SectorStateManager is missing", reason);
        Assert.IsFalse(Check(null, null, false, false, false, out reason));
    }

    [Test] public void DisabledProgressionCannotReuseItsPreparedSignal()
    {
        _progression.enabled = false;
        Assert.IsFalse(Ready(true, false, false, out _));
    }

    [Test] public void DisablingProgressionClearsTheInitialPreparedSignal()
    {
        typeof(StageProgressionManager).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_progression, null);
        Assert.IsFalse(_progression.IsInitialRuntimePrepared);
    }

    [Test] public void InactiveOrDisabledHomeIsNotReady()
    {
        _home.gameObject.SetActive(false);
        Assert.IsFalse(Ready(true, false, false, out var reason));
        StringAssert.Contains("not active", reason);
        _home.gameObject.SetActive(true); _home.enabled = false;
        Assert.IsFalse(Ready(true, false, false, out reason));
    }

    [Test] public void HomeMustHaveBothEnabledRuntimeMarkers()
    {
        var marker = _home.GetComponent<HomeSector>();
        marker.enabled = false;
        Assert.IsFalse(Ready(true, false, false, out _));
        marker.enabled = true;
        var hideout = _home.GetComponent<HideoutRuntime>();
        hideout.enabled = false;
        Assert.IsFalse(Ready(true, false, false, out _));
        Object.DestroyImmediate(hideout);
        Assert.IsFalse(Ready(true, false, false, out _));
    }

    [Test] public void HomeMarkersMustReferenceTheSameRuntimeSector()
    {
        var other = Go("Wrong sector reference").AddComponent<SectorRuntime>();
        _home.GetComponent<HomeSector>().Initialize(other);
        Assert.IsFalse(Ready(true, false, false, out _));
        _home.GetComponent<HomeSector>().Initialize(_home);
        _home.GetComponent<HideoutRuntime>().Initialize(other);
        Assert.IsFalse(Ready(true, false, false, out _));
    }

    [Test] public void UnopenedOrUnclearedHomeIsNotPrepared()
    {
        _home.SetRuntimeInfo(Vector2Int.zero, false, true);
        _home.SetCleared(true);
        Assert.IsFalse(Ready(true, false, false, out _));
        _home.SetRuntimeInfo(Vector2Int.zero, true, true);
        Assert.IsFalse(Ready(true, false, false, out _));
    }

    [Test] public void GeneratedMapAndAllRegisteredSectorsCanPassDeployment()
    {
        RegisterMap();
        Assert.IsTrue(Ready(false, true, true, out var reason), reason);
    }

    [Test] public void GeneratedRoomCountMismatchStillBlocksDeployment()
    {
        var layout = RegisterMap();
        layout.rooms.Add(new StageRoomNode(Vector2Int.right, StageRoomType.Normal, StageRoomConnectionFlags.None));
        Assert.IsFalse(Ready(false, true, true, out var reason));
        StringAssert.Contains("expected 2", reason);
    }

    [Test] public void MissingGeneratedStartOrCurrentSectorStillBlocksDeployment()
    {
        RegisterMap(); Property(_manager, "StartSector", null);
        Assert.IsFalse(Ready(false, true, true, out var reason));
        StringAssert.Contains("StartSector", reason);
        Property(_manager, "StartSector", _manager.Sectors[0]);
        Property(_manager, "CurrentSector", null);
        Assert.IsFalse(Ready(false, true, true, out reason));
        StringAssert.Contains("CurrentSector", reason);
    }

    [Test] public void InactiveGeneratedSectorStillBlocksDeployment()
    {
        RegisterMap(); _manager.Sectors[0].gameObject.SetActive(false);
        Assert.IsFalse(Ready(false, true, true, out var reason));
        StringAssert.Contains("not active", reason);
    }

    [Test] public void EmptyOrMissingLayoutStillBlocksDeployment()
    {
        var layout = RegisterMap(); layout.rooms.Clear();
        Assert.IsFalse(Ready(false, true, true, out var reason));
        StringAssert.Contains("no playable rooms", reason);
        layout.rooms = null;
        Assert.IsFalse(Ready(false, true, true, out reason));
        StringAssert.Contains("StageMapLayout is missing", reason);
    }

    [Test] public void WithoutProgressionTheOldGeneratedMapContractIsPreserved()
    {
        Assert.IsFalse(Check(_manager, null, true, false, false, out _));
        RegisterMap();
        Assert.IsTrue(Check(_manager, null, true, false, false, out var reason), reason);
    }

    [Test] public void ReadinessDoesNotCreateAMapOrMutateThePreparedHome()
    {
        string before = EditorJsonUtility.ToJson(_home);
        for (int i = 0; i < 5; i++) Assert.IsTrue(Ready(true, false, false, out _));
        Assert.AreEqual(before, EditorJsonUtility.ToJson(_home));
        Assert.IsNull(_manager.CurrentStageMapLayout); Assert.IsEmpty(_manager.Sectors);
        Assert.AreSame(_home, _manager.CurrentSector);
    }

    [Test] public void CurrentAuthoredStagesUseOnDemandRaidMaps()
    {
        var flow = AssetDatabase.LoadAssetAtPath<StageRunFlowSO>(
            "Assets/ScriptableObject/GameScene/Stage/StageRunFlow.asset");
        Assert.NotNull(flow);
        for (int i = 1; i <= 5; i++)
        {
            Assert.IsTrue(flow.TryGetStage(i, out var stage));
            Assert.AreEqual(StageMapPolicy.RadialBraidedRaid, stage.MapPolicy);
        }
    }

    private GameObject Go(string name)
    {
        var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
        SceneManager.MoveGameObjectToScene(go, _scene);
        return go;
    }

    private StageMapLayout RegisterMap()
    {
        var sector = Go("Generated entry fixture").AddComponent<SectorRuntime>();
        sector.SetRuntimeInfo(Vector2Int.zero, true, true);
        var layout = new StageMapLayout
        {
            stageIndex = 1, gridSize = 1, startSectorCoord = Vector2Int.zero,
            rooms = new List<StageRoomNode>
                { new(Vector2Int.zero, StageRoomType.Normal, StageRoomConnectionFlags.None) }
        };
        // Do not publish runtime events or touch static player-sector state in editor tests.
        Property(_manager, "CurrentStageMapLayout", layout);
        Property(_manager, "StartSector", sector); Property(_manager, "CurrentSector", sector);
        ((List<SectorRuntime>)typeof(SectorStateManager).GetField("_runtimeSectors",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager)).Add(sector);
        return layout;
    }

    private bool Ready(bool initial, bool requirePlayer, bool hasPlayer, out string reason) =>
        Check(_manager, _progression, initial, requirePlayer, hasPlayer, out reason);

    internal static bool Check(SectorStateManager manager, StageProgressionManager progression,
        bool initial, bool requirePlayer, bool hasPlayer, out string reason)
    {
        var args = new object[] { manager, progression, initial, requirePlayer, hasPlayer, null };
        bool ready = (bool)typeof(SceneLoader).GetMethod("IsRuntimeReady",
            BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
        reason = (string)args[5]; return ready;
    }

    internal static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static void Property(object target, string name, object value) => target.GetType()
        .GetProperty(name).SetValue(target, value);
}
