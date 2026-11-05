using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>Preview-scene encounter eligibility regressions. No authored assets or real event channels are changed.</summary>
public sealed class SectorEnemySpawnerEncounterTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private Scene _scene;
    private SectorRuntime _sector;
    private SectorEnemySpawner _spawner;
    private SectorStateManager _manager;
    private readonly List<ScriptableObject> _temporaryData = new();

    [SetUp] public void Setup()
    {
        _scene = EditorSceneManager.NewPreviewScene();
        var host = Go("Isolated encounter eligibility test");
        _sector = host.AddComponent<SectorRuntime>();
        _spawner = host.AddComponent<SectorEnemySpawner>();
        _manager = Go("Isolated map registry").AddComponent<SectorStateManager>();
        _manager.EnsureInitialized();
        Set(_spawner, "_sectorRuntime", _sector);
        Set(_spawner, "_sectorStateManager", _manager);
        Set(_spawner, "_currentStageIndex", 1);
        _sector.SetRuntimeInfo(new Vector2Int(3, 4), true, false);
    }

    [TearDown] public void Cleanup()
    {
        if (_spawner != null) Call(_spawner, "OnDisable");
        if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        foreach (var data in _temporaryData)
            if (data != null) Object.DestroyImmediate(data);
        _temporaryData.Clear();
    }

    [Test] public void GeneratedHatchRoomsSkipMissingPresetAcrossCurrentStages()
    {
        for (int index = 1; index <= 5; index++)
        {
            var stage = Stage(index);
            for (int seed = 0; seed < 3; seed++)
            {
                Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(seed, index, stage.RoomGridSize,
                    stage.RaidMap, out var map, out var error), error);
                int checkedRooms = 0;
                foreach (var node in map.rooms)
                {
                    if (node.raidRole != RaidRoomRole.Hatch && node.raidRole != RaidRoomRole.Merchant) continue;
                    Assert.AreEqual(StageRoomType.Normal, node.roomType);
                    Assert.IsTrue(node.isCleared);
                    _sector.SetRuntimeInfo(node.coord, node.isOpened, node.coord == map.startSectorCoord);
                    _sector.SetCleared(node.isCleared);
                    Set(_spawner, "_currentStageIndex", index);
                    Resolve(node.roomType);
                    AssertNoEncounter();
                    checkedRooms++;
                }
                Assert.GreaterOrEqual(checkedRooms, 1, "Hatch must be tested; merchants are now ordinary combat-room overlays.");
            }
        }
    }

    [Test] public void StartSectorSkipsLookupEvenBeforeItIsMarkedCleared()
    {
        _sector.SetRuntimeInfo(Vector2Int.zero, true, true);
        Assert.IsFalse(_sector.IsCleared);
        Resolve(StageRoomType.Normal);
        AssertNoEncounter();
    }

    [Test] public void RestoredClearedNormalAndBossRoomsDoNotRebuildWavePlans()
    {
        foreach (var type in new[] { StageRoomType.Normal, StageRoomType.Boss })
        {
            _sector.SetCleared(true);
            Resolve(type); AssertNoEncounter();
            Assert.IsTrue(_spawner.ConfigureEncounter(Encounter(type), 1));
            Resolve(type); AssertNoEncounter();
        }
    }

    [Test] public void ExplicitNoncombatNormalPolicyStillSkipsUnclearedRooms()
    {
        Policy(NormalSectorEncounterPolicy.None);
        Assert.IsFalse(_sector.IsCleared);
        Resolve(StageRoomType.Normal);
        AssertNoEncounter();
    }

    [Test] public void MissingNormalAndOptionalBattlePresetsStillReportErrors()
    {
        ExpectMissingPreset(StageRoomType.Normal);
        Resolve(StageRoomType.Normal); AssertNoEncounter();
        Policy(NormalSectorEncounterPolicy.Optional);
        ExpectMissingPreset(StageRoomType.Normal);
        Resolve(StageRoomType.Normal); AssertNoEncounter();
    }

    [Test] public void MissingBossPresetStillReportsAnErrorRegardlessOfNormalPolicy()
    {
        Policy(NormalSectorEncounterPolicy.None);
        ExpectMissingPreset(StageRoomType.Boss);
        Resolve(StageRoomType.Boss); AssertNoEncounter();
    }

    [Test] public void ConfiguredNormalAndBossWavePlansRemainDeterministic()
    {
        foreach (var type in new[] { StageRoomType.Normal, StageRoomType.Boss })
        {
            var preset = Encounter(type);
            Assert.IsTrue(_spawner.ConfigureEncounter(preset, 1));
            Resolve(type);
            Assert.IsTrue(Get<bool>(_spawner, "_hasEnemyEncounterConfiguration"));
            Assert.AreEqual(preset.DisplayName, _spawner.SelectedEncounterPresetName);
            Assert.Greater(_spawner.EncounterTotalSpawnCount, 0);
            Assert.AreEqual(preset.SectorMaxAlive, _spawner.EncounterMaxAlive);
            int count = _spawner.EncounterTotalSpawnCount;
            string plan = _spawner.DebugResolvedWavePlan;
            Resolve(type);
            Assert.AreEqual(count, _spawner.EncounterTotalSpawnCount);
            Assert.AreEqual(plan, _spawner.DebugResolvedWavePlan);
            Assert.IsFalse(_spawner.EncounterConfigurationFailed);
        }
    }

    [Test] public void ClearedCombatRoomDiscardsPendingWavesInsteadOfRespawning()
    {
        var preset = Encounter(StageRoomType.Normal);
        Assert.IsTrue(_spawner.ConfigureEncounter(preset, 1));
        Resolve(StageRoomType.Normal);
        Assert.IsTrue(preset.TryPickEnemyWavePreset(0, 42, out var wave));
        var queue = Get<Queue<EnemyShooterConfigSO>>(_spawner, "_pendingEnemySpawnQueue");
        queue.Enqueue(wave.Enemies[0].archetype);
        Set(_spawner, "_pendingEnemySpawnCount", 1);
        _sector.SetCleared(true);
        Resolve(StageRoomType.Normal);
        Assert.IsEmpty(queue); AssertNoEncounter();
    }

    [Test] public void ReopenedRoomCanResolveItsExistingConfiguredEncounter()
    {
        Assert.IsTrue(_spawner.ConfigureEncounter(Encounter(StageRoomType.Normal), 1));
        _sector.SetCleared(true); Resolve(StageRoomType.Normal); AssertNoEncounter();
        _sector.SetCleared(false); Resolve(StageRoomType.Normal);
        Assert.IsTrue(Get<bool>(_spawner, "_hasEnemyEncounterConfiguration"));
        Assert.Greater(_spawner.EncounterTotalSpawnCount, 0);
    }

    [Test] public void SectorOpenedAndStaleStageZeroCallbacksStaySilentForNoncombatRooms()
    {
        var progress = Data<StageProgressSnapshotEventChannelSO>();
        var opened = Data<SectorRuntimeEventChannelSO>();
        var ready = Data<SectorStateManagerReadyEventChannelSO>();
        Set(_spawner, "_stageProgressChangedChannel", progress);
        Set(_spawner, "_sectorOpenedEvent", opened);
        Set(_spawner, "_sectorStateManagerReadyChannel", ready);
        foreach (bool start in new[] { true, false })
        {
            _sector.SetRuntimeInfo(new Vector2Int(7, 7), true, start);
            _sector.SetCleared(true);
            RegisterTestNode(StageRoomType.Normal);
            progress.RaiseEvent(new StageProgressSnapshot { stageIndex = 0 });
            ready.RaiseEvent(_manager);
            Call(_spawner, "OnEnable");
            progress.RaiseEvent(new StageProgressSnapshot { stageIndex = 1 });
            opened.RaiseEvent(_sector);
            Assert.AreEqual(1, Get<int>(_spawner, "_currentStageIndex"));
            AssertNoEncounter();
            Call(_spawner, "OnDisable");
        }
    }

    [Test] public void UnregisteredRoomWaitsButRegisteredUnclearedBattleStillRejectsMissingPreset()
    {
        Call(_spawner, "TryRefreshResolvedSettings", "BeforeRegistration");
        AssertNoEncounter();
        RegisterTestNode(StageRoomType.Normal);
        ExpectMissingPreset(StageRoomType.Normal);
        Call(_spawner, "TryRefreshResolvedSettings", "AfterRegistration");
        AssertNoEncounter();
    }

    private void Resolve(StageRoomType type) => Call(_spawner, "RefreshResolvedSettings", type, "EligibilityRegression");

    private void AssertNoEncounter()
    {
        Assert.IsFalse(Get<bool>(_spawner, "_hasEncounterConfiguration"));
        Assert.IsFalse(Get<bool>(_spawner, "_hasEnemyEncounterConfiguration"));
        Assert.IsFalse(_spawner.HasStartedEncounter);
        Assert.AreEqual(0, _spawner.EncounterTotalSpawnCount);
        Assert.AreEqual(0, _spawner.PendingEncounterSpawnCount);
        Assert.AreEqual(0, _spawner.AliveEnemyCount);
        Assert.AreEqual("<none>", _spawner.SelectedEncounterPresetName);
        Assert.IsFalse((bool)Call(_spawner, "CanRunEncounter"));
    }

    private void ExpectMissingPreset(StageRoomType type) => LogAssert.Expect(LogType.Error,
        "[SectorEnemySpawner] Battle room has no configured encounter preset. " +
        $"stage=1, roomType={type}, coord={_sector.Coord}, sector={_sector.name}");

    private void RegisterTestNode(StageRoomType type)
    {
        var node = new StageRoomNode(_sector.Coord, type, StageRoomConnectionFlags.None);
        // Populate only this preview manager's registry; do not publish events or change static player-sector state.
        typeof(SectorStateManager).GetProperty("CurrentStageMapLayout").SetValue(_manager,
            new StageMapLayout { stageIndex = 1, stageSeed = 12345, rooms = new List<StageRoomNode> { node } });
        Get<Dictionary<SectorRuntime, StageRoomNode>>(_manager, "_roomBySector")[_sector] = node;
    }

    private StageBattleEncounterPresetSO Encounter(StageRoomType type)
    {
        var stage = Stage(1);
        StageBattleEncounterPresetSO preset;
        if (type == StageRoomType.Boss)
            Assert.IsTrue(stage.TryPickBossBattle(12345, _sector.Coord, out preset, out _));
        else
            Assert.IsTrue(stage.TryPickNormalSectorEncounter(12345, _sector.Coord, out preset));
        Assert.IsTrue(preset.IsValid);
        return preset;
    }

    private static StageDefinitionSO Stage(int index)
    {
        var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(
            $"Assets/ScriptableObject/GameScene/Stage/Stage{index}Definition.asset");
        Assert.NotNull(stage); return stage;
    }

    private void Policy(NormalSectorEncounterPolicy policy)
    {
        var data = Data<NormalSectorPolicySO>(); Set(data, "_encounterPolicy", policy);
        _sector.ConfigureNormalSectorPolicy(data);
    }

    private T Data<T>() where T : ScriptableObject
    {
        var data = ScriptableObject.CreateInstance<T>(); data.hideFlags = HideFlags.HideAndDontSave;
        _temporaryData.Add(data); return data;
    }

    private GameObject Go(string name)
    {
        var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
        go.SetActive(false); SceneManager.MoveGameObjectToScene(go, _scene); return go;
    }

    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Flags).SetValue(obj, value);
    private static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, Flags).GetValue(obj);
    private static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Flags).Invoke(obj, args);
}
