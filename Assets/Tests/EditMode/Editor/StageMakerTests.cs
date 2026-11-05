using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class StageMakerTests
{
    [TestCase(700, 650, 3, 7, 8, 12)]
    [TestCase(320, 240, -12, -8, 9, 14)]
    [TestCase(900, 700, 30, -40, 30, -40)]
    [TestCase(700, 650, 0, 0, 63, 63)]
    public void MapFitCentersOccupiedRoomsIncludingLabelsAndBadges(float width, float height,
        float minX, float minY, float maxX, float maxY)
    {
        var viewport = new Vector2(width, height);
        StageMakerMapView.Fit(viewport, new Vector2(minX, minY), new Vector2(maxX, maxY),
            new Vector2(168, 116), out float zoom, out Vector2 pan);
        var low = viewport * .5f + pan + new Vector2(minX * 168 - 80, -maxY * 116 - 60) * zoom;
        var high = viewport * .5f + pan + new Vector2(maxX * 168 + 80, -minY * 116 + 75) * zoom;
        Assert.GreaterOrEqual(low.x, 23.9f); Assert.GreaterOrEqual(low.y, 23.9f);
        Assert.LessOrEqual(high.x, width - 23.9f); Assert.LessOrEqual(high.y, height - 23.9f);
        Assert.Less(((low + high) * .5f - viewport * .5f).magnitude, .01f);
        if (maxX - minX == 5) Assert.Greater(zoom, .5f, "Small occupied maps must not remain tiny.");
    }

    private readonly List<Object> _objects = new();
    private T Keep<T>(T value) where T : Object { _objects.Add(value); return value; }
    private static void Set(object owner, string field, object value) => owner.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    private StageDefinitionSO Stage()
    {
        var stage = Keep(ScriptableObject.CreateInstance<StageDefinitionSO>());
        Set(stage, "_mapPolicy", StageMapPolicy.RadialBraidedRaid); Set(stage, "_roomGridSize", 10);
        stage.RaidMap.bossSpawnPercent = 0; stage.RaidMap.vaultNearCandidateChance = 0;
        stage.RaidMap.merchantSpawnPercent = 0; stage.RaidMap.minimumExtractionPathDistance = 0;
        stage.SpecialSectors.useRatio = false; // Explicit regression coverage for the former count mode.
        return stage;
    }
    private SectorPresetSO Preset(string name)
    {
        var preset = Keep(ScriptableObject.CreateInstance<SectorPresetSO>()); preset.name = name;
        var root = Keep(new GameObject(name)); root.SetActive(false);
        Set(preset, "_sectorPrefab", root.AddComponent<SectorRuntime>());
        return preset;
    }
    private SpecialSectorPool Pool(string id, SectorPresetSO preset) => new()
    { id = id, variants = new List<SpecialSectorVariant> { new() { id = "a", preset = preset } } };
    [TearDown] public void Cleanup()
    {
        for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
        _objects.Clear();
    }

    [TestCase(8, 20)] [TestCase(12, 46)] [TestCase(14, 63)] [TestCase(16, 82)]
    public void ProposedSizesUseRealConnectedGenerator(int size, int rooms)
    {
        var settings = new RaidMapSettings { vaultNearCandidateChance = 0 };
        for (int seed = 0; seed < 16; seed++)
        {
            Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(seed, 1, size, settings, out var layout, out var error, true), error);
            Assert.GreaterOrEqual(layout.rooms.Count, rooms, "Only required terminal branches may exceed the density target.");
            Assert.IsTrue(RaidMapValidation.TryValidate(layout, out error), error);
        }
    }
    [Test] public void AdvancedDefaultRulesPreserveOldSeedResults()
    {
        var settings = new RaidMapSettings { vaultNearCandidateChance = 0 };
        Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(17, 1, 12, settings, out var old, out var error, true), error);
        settings.customizeTopology = true;
        Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(17, 1, 12, settings, out var current, out error, true), error);
        Assert.AreEqual(JsonUtility.ToJson(old), JsonUtility.ToJson(current));
        settings.topology.maximumRoomCount = 1;
        Assert.IsFalse(RadialBraidedRaidGenerator.TryGenerate(17, 1, 12, settings, out _, out error, true));
        StringAssert.Contains("기본 링", error);
    }
    [Test] public void RetiredDistanceAndCandidateSettingsDoNotAffectNewMaps()
    {
        var settings = new RaidMapSettings { vaultNearCandidateChance = 0 };
        Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(0, 1, 8, settings, out var baseline, out var error, true), error);
        settings.minimumExtractionPathDistance = 500; settings.candidateCount = -100;
        settings.customizeTopology = true;
        settings.topology.candidateSeparationWeight = settings.topology.candidateDistanceWeight = -10;
        settings.topology.vaultCandidateDistance = settings.topology.merchantMinimumDistance = 500;
        Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(0, 1, 8, settings, out var changed, out error, true), error);
        Assert.AreEqual(JsonUtility.ToJson(baseline), JsonUtility.ToJson(changed));
    }
    [Test] public void PreviewUsesSharedPipelineWithoutChangingSourceRandomOrScene()
    {
        var stage = Stage();
        string json = EditorJsonUtility.ToJson(stage);
        bool dirty = EditorUtility.IsDirty(stage);
        var random = Random.state;
        int objects = Resources.FindObjectsOfTypeAll<GameObject>().Length;
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out var runtime, out var error, readRuntimeQuests: false), error);
        Assert.IsTrue(StageMakerPreview.TryBuild(stage, 7, Vector2Int.left, null, out var preview, out error), error);
        Assert.AreEqual(JsonUtility.ToJson(runtime), JsonUtility.ToJson(preview.layout));
        Assert.AreEqual(json, EditorJsonUtility.ToJson(stage));
        Assert.AreEqual(dirty, EditorUtility.IsDirty(stage));
        Assert.AreEqual(random, Random.state);
        Assert.AreEqual(objects, Resources.FindObjectsOfTypeAll<GameObject>().Length);
        Assert.Greater(preview.errors.Count, 0, "Missing content should be visible, not hidden by graph success.");
    }
    [Test] public void SpecialPoolsSupportRepetitionAndIgnoreDisabledTypes()
    {
        var stage = Stage(); var settings = stage.SpecialSectors;
        settings.enabled = settings.allowRepeatedTypes = true; settings.minimumCount = settings.maximumCount = 4;
        var pool = Pool("office", Preset("office")); pool.minimumCount = 2; pool.maximumCount = 4;
        settings.pools.Add(pool);
        var disabled = Pool("server", Preset("server")); disabled.disabled = true; disabled.weight = 999;
        settings.pools.Add(disabled);
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out var map, out var error, readRuntimeQuests: false), error);
        Assert.AreEqual(4, map.rooms.Count(r => r.specialSectorPoolId == "office"));
        Assert.AreEqual(0, map.rooms.Count(r => r.specialSectorPoolId == "server"));
    }
    [Test] public void ExplicitQuestSimulationReservesTypeWithoutRuntimeQuestState()
    {
        var stage = Stage(); var settings = stage.SpecialSectors;
        settings.enabled = true; settings.minimumCount = 0; settings.maximumCount = 1;
        var pool = Pool("medical", Preset("medical"));
        pool.direction = SpecialSectorDirection.North; pool.minimumStartDistance = 2;
        settings.pools.Add(pool);
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out var map, out var error,
            new[] { "medical" }, false), error);
        var room = map.rooms.Single(r => r.specialSectorPoolId == "medical");
        Assert.Greater(room.coord.y, map.startSectorCoord.y);
        Assert.IsFalse(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out _, out error,
            new[] { "missing" }, false));
        StringAssert.Contains("missing", error);
    }
    [Test] public void VariantAvoidanceUsesActualPrefabRatherThanOnlyVariantId()
    {
        var stage = Stage(); var settings = stage.SpecialSectors;
        settings.enabled = settings.allowRepeatedTypes = settings.avoidRepeatedVariants = true;
        settings.minimumCount = settings.maximumCount = 2;
        var pool = Pool("office", Preset("office_a")); pool.maximumCount = 2;
        pool.variants[0].weight = 1000000;
        pool.variants.Add(new SpecialSectorVariant { id = "b", preset = Preset("office_b") });
        settings.pools.Add(pool);
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out var map, out var error, readRuntimeQuests: false), error);
        Assert.AreEqual(2, map.rooms.Where(r => r.specialSectorPoolId == "office")
            .Select(r => r.specialSectorVariantId).Distinct().Count());
    }
    [Test] public void DirectionalMinimumIsNotBlockedByAnUnrestrictedType()
    {
        var settings = new StageSpecialSectorSettings { enabled = true, useRatio = false, minimumCount = 2, maximumCount = 2 };
        var unrestricted = Pool("any", Preset("any")); unrestricted.minimumCount = 1;
        var north = Pool("north", Preset("north")); north.minimumCount = 1; north.direction = SpecialSectorDirection.North;
        settings.pools.Add(unrestricted); settings.pools.Add(north);
        var map = new StageMapLayout { startSectorCoord = Vector2Int.zero, stageSeed = 7 };
        map.rooms.Add(new StageRoomNode(Vector2Int.zero, StageRoomType.Normal, StageRoomConnectionFlags.Up | StageRoomConnectionFlags.Down)
        { raidRole = RaidRoomRole.Hatch });
        map.rooms.Add(new StageRoomNode(Vector2Int.up, StageRoomType.Normal, StageRoomConnectionFlags.Down) { raidRole = RaidRoomRole.Normal });
        map.rooms.Add(new StageRoomNode(Vector2Int.down, StageRoomType.Normal, StageRoomConnectionFlags.Up) { raidRole = RaidRoomRole.Normal });
        Assert.IsTrue(settings.TryAssign(map, out var error, readRuntimeQuests: false), error);
        Assert.AreEqual("north", map.rooms.Single(r => r.coord == Vector2Int.up).specialSectorPoolId);
        Assert.AreEqual("any", map.rooms.Single(r => r.coord == Vector2Int.down).specialSectorPoolId);
    }
    [Test] public void CompositionStoresCategoryAndResumeDoesNotReassignIt()
    {
        var stage = Stage(); var composition = stage.RoomComposition;
        composition.enabled = true; composition.themePercent = 100; composition.specialPercent = composition.commonPercent = 0;
        var preset = Preset("themed"); composition.themePresets.Add(new StageDefinitionSO.WeightedSectorPreset { preset = preset });
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out var map, out var error, readRuntimeQuests: false), error);
        var saved = JsonUtility.FromJson<StageMapLayout>(JsonUtility.ToJson(map));
        var room = saved.rooms.First(r => r.combatPoolId == "theme");
        composition.enabled = false;
        Assert.IsTrue(StageMapBuildPipeline.TryResolveRoomPreset(stage, saved, room, out var restored, out _, out error), error);
        Assert.AreSame(preset, restored);
        composition.enabled = true; composition.commonPercent = 20;
        Assert.IsFalse(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out _, out error, readRuntimeQuests: false));
        StringAssert.Contains("100", error);
    }
    [Test] public void PrefabResolutionIsIdenticalToRuntimeAndReportsItsName()
    {
        var stage = Stage(); var preset = Preset("Normal_Test");
        Set(stage, "_normalBattle", new StageDefinitionSO.NormalBattleSettings
        { sectorPresets = new List<StageDefinitionSO.WeightedSectorPreset> { new() { preset = preset } } });
        Assert.IsTrue(StageMakerPreview.TryBuild(stage, 7, Vector2Int.left, null, out var preview, out var error), error);
        foreach (var cell in preview.cells)
        {
            Assert.IsTrue(StageMapBuildPipeline.TryResolveRoomPreset(stage, preview.layout, cell.room, out var actual, out _, out error), error);
            Assert.AreSame(actual, cell.preset);
            Assert.AreEqual("Normal_Test", cell.preset.DisplayName);
        }
    }

    [TestCase(940)] [TestCase(1280)] [TestCase(1920)]
    public void WorkspaceGivesSettingsMoreSpaceAndKeepsPanelsInsideWindow(int width)
    {
        var layout = StageMakerWorkspaceLayout.Calculate(new Vector2(width, 720), true, true, 205, .58f);
        Assert.GreaterOrEqual(layout.settings.width, layout.preview.width);
        Assert.GreaterOrEqual(layout.settings.width, 430);
        Assert.Greater(layout.preview.width, 0);
        Assert.AreEqual(layout.library.xMax + 5, layout.settings.xMin, .01f);
        Assert.AreEqual(layout.settings.xMax + 5, layout.preview.xMin, .01f);
        Assert.AreEqual(width, layout.preview.xMax, .01f);
        Assert.AreEqual(720, layout.settings.yMax, .01f);
    }
    [Test] public void HidingPanelsReturnsTheirSpaceToSettings()
    {
        var withList = StageMakerWorkspaceLayout.Calculate(new Vector2(1280, 720), true, false, 205, .58f);
        Assert.AreEqual(1070, withList.settings.width, .01f);
        Assert.AreEqual(Rect.zero, withList.preview);
        var settingsOnly = StageMakerWorkspaceLayout.Calculate(new Vector2(1280, 720), false, false, 205, .58f);
        Assert.AreEqual(0, settingsOnly.settings.xMin);
        Assert.AreEqual(1280, settingsOnly.settings.width);
        var mapFocused = StageMakerWorkspaceLayout.Calculate(new Vector2(1920, 720), false, true, 205, .38f);
        Assert.Greater(mapFocused.preview.width, mapFocused.settings.width);
    }
    [Test] public void GenerationDiagnosticsPreserveExactFieldErrors()
    {
        const string error = "발전실·탈출구용 가지 끝 방이 부족합니다. 필요 7개 / 확보 3개.";
        Assert.AreEqual(error, StageMakerGenerationDiagnostics.Summary(error));
    }
    [Test] public void NewBranchRolesAreLeavesWhileBossIsIndependent()
    {
        var settings = new RaidMapSettings { bossSpawnPercent = 100, extractionCount = 6, braidPercent = 100, vaultNearCandidateChance = 1 };
        for (int seed = 0; seed < 32; seed++)
        {
            Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(seed, 1, 10, settings, out var map, out string error, true), error);
            Assert.AreEqual(1, map.landmarkPlacementVersion); Assert.AreEqual(6, map.extractionRooms.Count);
            Assert.AreEqual(7, map.extractionCandidates.Count);
            Assert.IsFalse(map.extractionCandidates.Contains(map.bossRoomCoord));
            foreach (var coord in map.extractionCandidates)
            {
                var room = map.rooms.Single(r => r.coord == coord);
                Assert.AreEqual(1, new[] { StageRoomConnectionFlags.Left, StageRoomConnectionFlags.Right,
                    StageRoomConnectionFlags.Up, StageRoomConnectionFlags.Down }.Count(room.HasConnection));
            }
            Assert.AreEqual("theme", map.rooms.Single(r => r.coord == map.bossRoomCoord).combatPoolId);
            var restored = JsonUtility.FromJson<StageMapLayout>(JsonUtility.ToJson(map));
            Assert.IsTrue(RadialBraidedRaidGenerator.TryValidate(restored, out error), error);
        }
    }

    [Test] public void RatioUsesOnlyAllowedKindsAndIgnoresOldCountLimits()
    {
        var stage = Stage(); var settings = stage.SpecialSectors;
        settings.enabled = settings.useRatio = true;
        settings.minimumCount = 100; settings.maximumCount = 0;
        stage.RoomComposition.specialPercent = 100;
        var allowed = Pool("office", Preset("office")); allowed.minimumCount = 100; allowed.maximumCount = 0;
        settings.pools.Add(allowed);
        var disabled = Pool("server", Preset("server")); disabled.disabled = true; disabled.minimumCount = 100;
        settings.pools.Add(disabled);
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 1, Vector2Int.left, out var map, out var error, readRuntimeQuests: false), error);
        Assert.AreEqual(1, map.rooms.Count(r => r.specialSectorPoolId == "office"));
        Assert.IsFalse(map.rooms.Any(r => r.specialSectorPoolId == "server"));
        Assert.Greater(stage.RoomComposition.AdditionalSpecialBudget(map, true), 1);
    }

    [Test] public void RatioWithNoAllowedKindsDoesNotFailOrForceOldMinimum()
    {
        var stage = Stage(); var settings = stage.SpecialSectors;
        settings.enabled = settings.useRatio = true;
        var pool = Pool("office", Preset("office")); pool.disabled = true;
        settings.pools.Add(pool);
        Assert.IsFalse(stage.RoomComposition.enabled, "Special ratio must work without enabling three-way composition.");
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 1, Vector2Int.left, out var map, out var error, readRuntimeQuests: false), error);
        Assert.IsFalse(map.rooms.Any(r => !string.IsNullOrEmpty(r.specialSectorPoolId)));
        Assert.IsTrue(StageMakerPreview.TryBuild(stage, 1, Vector2Int.left, null, out var preview, out error), error);
        Assert.Greater(preview.requestedSpecialRooms, 0);
        Assert.AreEqual(0, preview.actualSpecialRooms);
    }

    [Test] public void RatioRepetitionFillsTargetWithoutLegacyPerTypeCap()
    {
        var stage = Stage(); var settings = stage.SpecialSectors;
        settings.enabled = settings.useRatio = settings.allowRepeatedTypes = true;
        settings.pools.Add(Pool("office", Preset("office")));
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 3, Vector2Int.left, out var map, out var error, readRuntimeQuests: false), error);
        int requested = stage.RoomComposition.AdditionalSpecialBudget(map, true);
        Assert.Greater(requested, 1);
        Assert.AreEqual(requested, map.rooms.Count(r => r.specialSectorPoolId == "office"));
        Assert.IsFalse(map.rooms.Any(r => map.extractionCandidates.Contains(r.coord) && !string.IsNullOrEmpty(r.specialSectorPoolId)));
    }

    [Test] public void RatioStillHonorsQuestGuaranteesAndOptInHardLimits()
    {
        var stage = Stage(); var settings = stage.SpecialSectors;
        settings.enabled = settings.useRatio = true; stage.RoomComposition.specialPercent = 0;
        var pool = Pool("medical", Preset("medical")); settings.pools.Add(pool);
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 1, Vector2Int.left, out var map, out var error, new[] { "medical" }, false), error);
        Assert.AreEqual(1, map.rooms.Count(r => r.specialSectorPoolId == "medical"));
        pool.disabled = true;
        Assert.IsFalse(StageMapBuildPipeline.TryGenerate(stage, 1, Vector2Int.left, out _, out error, new[] { "medical" }, false));
        pool.disabled = false; settings.useAdvancedCountLimits = true;
        settings.minimumCount = settings.maximumCount = 5;
        Assert.IsFalse(StageMapBuildPipeline.TryGenerate(stage, 1, Vector2Int.left, out _, out error, readRuntimeQuests: false));
    }

    [Test] public void OptionalRatioDirectionShortfallIsOrdinaryButMandatoryDirectionFails()
    {
        var map = new StageMapLayout { startSectorCoord = Vector2Int.zero, stageSeed = 1 };
        map.rooms.Add(new StageRoomNode(Vector2Int.zero, StageRoomType.Normal, StageRoomConnectionFlags.Right) { raidRole = RaidRoomRole.Hatch });
        map.rooms.Add(new StageRoomNode(Vector2Int.right, StageRoomType.Normal, StageRoomConnectionFlags.Left) { raidRole = RaidRoomRole.Normal });
        var settings = new StageSpecialSectorSettings { enabled = true, useRatio = true };
        var north = Pool("north", Preset("north")); north.direction = SpecialSectorDirection.North;
        settings.pools.Add(north);
        Assert.IsTrue(settings.TryAssign(map, out var error, readRuntimeQuests: false, requestedCount: 1), error);
        Assert.IsFalse(map.rooms.Any(r => !string.IsNullOrEmpty(r.specialSectorPoolId)));
        settings.useAdvancedCountLimits = true; settings.minimumCount = settings.maximumCount = 1;
        Assert.IsFalse(settings.TryAssign(map, out error, readRuntimeQuests: false, requestedCount: 1));
        StringAssert.Contains("강제 최소", error);
        settings.useAdvancedCountLimits = false;
        Assert.IsFalse(settings.TryAssign(map, out error, new[] { "north" }, false, 1));
        StringAssert.Contains("필수 배치", error);
    }

    [Test] public void RatioBudgetExcludesInfrastructureAndExtractionCandidates()
    {
        var map = new StageMapLayout { startSectorCoord = Vector2Int.zero };
        map.rooms.Add(new StageRoomNode(Vector2Int.zero, StageRoomType.Normal, StageRoomConnectionFlags.None) { raidRole = RaidRoomRole.Hatch });
        for (int i = 1; i <= 5; i++) map.rooms.Add(new StageRoomNode(new Vector2Int(i, 0), StageRoomType.Normal, StageRoomConnectionFlags.None) { raidRole = RaidRoomRole.Normal });
        map.extractionCandidates.Add(new Vector2Int(5, 0));
        map.rooms.Add(new StageRoomNode(Vector2Int.up, StageRoomType.Normal, StageRoomConnectionFlags.None) { raidRole = RaidRoomRole.Generator });
        map.rooms.Add(new StageRoomNode(Vector2Int.left, StageRoomType.Normal, StageRoomConnectionFlags.None) { raidRole = RaidRoomRole.Merchant });
        var composition = new StageRoomCompositionSettings { specialPercent = 50 };
        Assert.AreEqual(4, StageRoomCompositionSettings.RatioCandidateCount(map));
        Assert.AreEqual(2, composition.AdditionalSpecialBudget(map, true));
    }

    [Test] public void SparseValidationNamesActualExitCountWithoutCandidateDependency()
    {
        var settings = new RaidMapSettings { candidateCount = 3, extractionCount = 7 };
        Assert.IsFalse(RadialBraidedRaidGenerator.TryGenerate(1, 1, 8, settings, out _, out string error));
        StringAssert.Contains("탈출구 수: 현재 7개", error);
        StringAssert.Contains("허용 범위 1~6개", error);
        StringAssert.DoesNotContain("방 밀도:", error);
        StringAssert.DoesNotContain("Invalid sparse", error);
        Assert.AreEqual(3, settings.candidateCount, "Validation must not auto-correct settings.");
    }

    [Test] public void SparseValidationReportsCurrentFaultsButIgnoresRetiredDistances()
    {
        var settings = new RaidMapSettings { candidateCount = 4, extractionCount = 7, roomDensity = .7f,
            merchantSpawnPercent = -1, braidPercent = 101, bossSpawnPercent = 102, minimumExtractionPathDistance = -3,
            vaultNearCandidateChance = 2 };
        string before = JsonUtility.ToJson(settings);
        Assert.IsFalse(settings.TryValidateSparse(0, out string error));
        foreach (string field in new[] { "맵 가로·세로: 현재 0칸", "방 밀도: 현재 0.7", "탈출구 수: 현재 7개",
            "상인 등장 확률: 현재 -1%", "인접 방 추가 연결: 현재 101%", "보스 출현 확률: 현재 102%",
            "별도 금고 추가 확률: 현재 2" }) StringAssert.Contains(field, error);
        StringAssert.DoesNotContain("후보 최소 경로", error);
        Assert.AreEqual(before, JsonUtility.ToJson(settings));
    }

    [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
    public void SparseValidationRejectsNonFiniteDensityWithTheFieldName(float density)
    {
        var settings = new RaidMapSettings { roomDensity = density };
        Assert.IsFalse(settings.TryValidateSparse(8, out string error));
        StringAssert.Contains("방 밀도: 현재", error);
        StringAssert.Contains("0.15~0.65", error);
    }

    [TestCase(8, .15f, 4, 2)] [TestCase(64, .65f, 8, 6)]
    public void SparseValidationAcceptsExistingBoundaryValues(int grid, float density, int candidates, int exits)
    {
        var settings = new RaidMapSettings { roomDensity = density, candidateCount = candidates, extractionCount = exits };
        Assert.IsTrue(settings.TryValidateSparse(grid, out string error), error);
    }

    [Test] public void AdvancedValidationExplainsConflictingCountsAndRingSize()
    {
        var settings = new RaidMapSettings { customizeTopology = true };
        settings.topology.minimumRoomCount = 5; settings.topology.maximumRoomCount = 3;
        settings.topology.ringRadius = 4;
        settings.topology.candidateSeparationWeight = settings.topology.candidateDistanceWeight = 0;
        Assert.IsFalse(settings.TryValidateSparse(8, out string error));
        StringAssert.Contains("최소 5개 > 최대 3개", error);
        StringAssert.Contains("중심 링 반경: 현재 4칸", error);
        StringAssert.Contains("허용 범위 1~3칸", error);
        StringAssert.DoesNotContain("후보 배치 가중치", error);
        settings.customizeTopology = false;
        Assert.IsTrue(settings.TryValidateSparse(8, out error), "Disabled override values must stay ignored: " + error);
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void SmallTestGridsGenerateSaveAndRestoreWithoutEditingSettings(int size)
    {
        var settings = new RaidMapSettings { candidateCount = 8, extractionCount = 6, minimumExtractionPathDistance = 100,
            preventBossBlockingExtraction = false }; // Baseline compact geometry, independent of the optional bypass constraint.
        string before = JsonUtility.ToJson(settings); var random = Random.state;
        foreach (float density in new[] { .15f, .32f, .65f })
        {
            settings.roomDensity = density;
            string densitySnapshot = JsonUtility.ToJson(settings);
            for (int seed = 0; seed < 16; seed++)
            {
                Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(seed, 1, size, settings, out var map, out string error, true), error);
                Assert.IsTrue(map.compactTestLayout);
                Assert.IsTrue(RaidMapValidation.TryValidate(map, out error), error);
                Assert.AreEqual(map.rooms.Count, RaidMapGenerator.GetDistances(map, map.startSectorCoord).Count);
                Assert.IsTrue(map.rooms.All(r => r.coord.x >= 0 && r.coord.x < size && r.coord.y >= 0 && r.coord.y < size));
                var saved = new RaidRunSaveData { active = true, layout = map, playerSectorCoord = map.startSectorCoord };
                var restored = JsonUtility.FromJson<RaidRunSaveData>(JsonUtility.ToJson(saved));
                Assert.IsTrue(restored.TryValidate(out error), error);
                Assert.AreEqual(JsonUtility.ToJson(map), JsonUtility.ToJson(restored.layout));
                Assert.AreEqual(densitySnapshot, JsonUtility.ToJson(settings));
            }
        }
        settings.roomDensity = .32f;
        Assert.AreEqual(before, JsonUtility.ToJson(settings));
        Assert.AreEqual(random, Random.state);
    }

    [Test] public void OneByOneTestIsOneOrdinaryRoomWithARealHomeExit()
    {
        var stage = Stage(); Set(stage, "_roomGridSize", 1);
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 1, Vector2Int.left, out var map, out string error, readRuntimeQuests: false), error);
        Assert.AreEqual(1, map.rooms.Count); Assert.AreEqual(RaidRoomRole.Normal, map.rooms[0].raidRole);
        Assert.IsTrue(map.rooms[0].isCleared); Assert.AreEqual(1, map.extractionPortals.Count);
        Assert.AreEqual(StageRoomConnectionFlags.None, map.rooms[0].connections);
        Assert.IsTrue(RaidRuntime.AllowsExtraction(new RaidRunSaveData { active = true, layout = map }, map.startSectorCoord, RaidStationKind.NormalExtraction));
        map.rooms[0].connections = StageRoomConnectionFlags.Right;
        Assert.IsFalse(RaidMapValidation.TryValidate(map, out _));
    }

    [Test] public void CompactFlagCannotBypassNormalRaidValidation()
    {
        Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(1, 1, 10, new RaidMapSettings(), out var normal, out string error, true), error);
        Assert.IsFalse(normal.compactTestLayout);
        normal.compactTestLayout = true;
        Assert.IsFalse(RaidMapValidation.TryValidate(normal, out _));
        Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(1, 1, 1, new RaidMapSettings(), out var small, out error, true), error);
        small.compactTestLayout = false;
        Assert.IsFalse(RaidMapValidation.TryValidate(small, out _));
    }

    [Test] public void LegacyCompactSaveKeepsOldCandidateAndPresetSemantics()
    {
        var center = new Vector2Int(1, 1); var generator = new Vector2Int(0, 1);
        var boss = Vector2Int.zero; var exit = new Vector2Int(1, 0);
        var saved = new StageMapLayout { generationVersion = 3, usesRaidLayout = true, compactTestLayout = true,
            gridSize = 2, firstRoomCoord = center, startSectorCoord = center, hatchRoomCoord = center,
            generatorRoomCoord = generator, bossRoomCoord = boss, goalRoomCoord = boss, extractionSectorCoord = exit, hasExtraction = true };
        saved.rooms.Add(new StageRoomNode(center, StageRoomType.Normal, StageRoomConnectionFlags.Left) { raidRole = RaidRoomRole.Hatch });
        saved.rooms.Add(new StageRoomNode(generator, StageRoomType.Normal, StageRoomConnectionFlags.Right | StageRoomConnectionFlags.Down) { raidRole = RaidRoomRole.Generator });
        saved.rooms.Add(new StageRoomNode(boss, StageRoomType.Normal, StageRoomConnectionFlags.Up | StageRoomConnectionFlags.Right) { raidRole = RaidRoomRole.Boss });
        saved.rooms.Add(new StageRoomNode(exit, StageRoomType.Normal, StageRoomConnectionFlags.Left) { raidRole = RaidRoomRole.Normal });
        saved.extractionCandidates.AddRange(new[] { generator, boss, exit }); saved.extractionRooms.Add(exit);
        Assert.IsTrue(RaidExtractionPortalPolicy.TryPrepare(saved, out string error), error);
        string before = JsonUtility.ToJson(saved);
        var restored = JsonUtility.FromJson<StageMapLayout>(before);
        Assert.IsTrue(RaidMapValidation.TryValidate(restored, out error), error);
        Assert.AreEqual(0, restored.landmarkPlacementVersion);
        Assert.AreEqual(before, JsonUtility.ToJson(restored));
        restored.landmarkPlacementVersion = 1;
        Assert.IsFalse(RaidMapValidation.TryValidate(restored, out _));
    }

    [Test] public void NewBossUsesThemeMapAndEncounterWithoutLegacySectorPair()
    {
        var stage = Stage(); stage.RaidMap.bossSpawnPercent = 100;
        var encounter = AssetDatabase.LoadAssetAtPath<StageBattleEncounterPresetSO>(
            "Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage1PunisherEncounter.asset");
        Assert.IsNotNull(encounter); Assert.IsTrue(encounter.IsValid);
        Set(stage, "_bossBattles", new List<StageDefinitionSO.BossBattleEntry> { new() { encounterPreset = encounter, sectorPreset = null } });
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out var map, out string error, readRuntimeQuests: false), error);
        var boss = map.rooms.Single(r => r.roomType == StageRoomType.Boss);
        Assert.IsFalse(StageMapBuildPipeline.TryResolveRoomPreset(stage, map, boss, out _, out _, out error));
        StringAssert.Contains("전용맵 풀", error);
        var theme = Preset("ThemeForBoss");
        stage.RoomComposition.themePresets.Add(new StageDefinitionSO.WeightedSectorPreset { preset = theme });
        Assert.IsFalse(stage.RoomComposition.enabled, "The theme ratio toggle does not disable a saved boss theme category.");
        Assert.IsTrue(StageMapBuildPipeline.TryResolveRoomPreset(stage, map, boss, out var picked, out var pickedEncounter, out error), error);
        Assert.AreSame(theme, picked); Assert.AreSame(encounter, pickedEncounter);
        Assert.IsFalse(stage.TryPickBossBattle(map.stageSeed, boss.coord, out _, out _));
        map.landmarkPlacementVersion = 0;
        Assert.IsFalse(StageMapBuildPipeline.TryResolveRoomPreset(stage, map, boss, out _, out _, out error));
        StringAssert.Contains("보스 전투/섹터 쌍", error);
    }

    private static bool AllExitsReachableWithoutBoss(StageMapLayout map)
    {
        map.TryGetRoom(map.bossRoomCoord, out var boss);
        var connections = boss.connections;
        try
        {
            boss.connections = StageRoomConnectionFlags.None;
            var reached = RaidMapGenerator.GetDistances(map, map.startSectorCoord);
            return map.extractionRooms.All(reached.ContainsKey);
        }
        finally { boss.connections = connections; }
    }

    [TestCase(2)] [TestCase(7)] [TestCase(8)] [TestCase(16)]
    public void ProtectedBossCannotBeRequiredToReachAnyExteriorExit(int grid)
    {
        var settings = new RaidMapSettings { bossSpawnPercent = 100, extractionCount = 2, vaultNearCandidateChance = 1 };
        Assert.IsTrue(settings.preventBossBlockingExtraction, "Safe placement is the default.");
        int successes = 0;
        for (int seed = 0; seed < 32; seed++)
        {
            if (!RadialBraidedRaidGenerator.TryGenerate(seed, 1, grid, settings, out var map, out string error, true))
            {
                Assert.Less(grid, 8, error); StringAssert.Contains("보스가 탈출 경로", error);
                continue;
            }
            successes++;
            Assert.IsTrue(map.bossSpawned); Assert.IsTrue(map.bossAvoidsExtractionRoutes);
            Assert.IsTrue(AllExitsReachableWithoutBoss(map));
            var restored = JsonUtility.FromJson<StageMapLayout>(JsonUtility.ToJson(map));
            settings.preventBossBlockingExtraction = false; // Live SO edits must not reinterpret the saved rule.
            Assert.IsTrue(restored.bossAvoidsExtractionRoutes);
            Assert.IsTrue(RadialBraidedRaidGenerator.TryValidate(restored, out error), error);
            settings.preventBossBlockingExtraction = true;
        }
        Assert.Greater(successes, 0);
    }

    [Test] public void ProtectionOffAndOlderSavesAllowArticulationBossButOnRejectsIt()
    {
        var settings = new RaidMapSettings { bossSpawnPercent = 100, extractionCount = 6, vaultNearCandidateChance = 0,
            preventBossBlockingExtraction = false };
        StageMapLayout blocked = null;
        for (int seed = 0; seed < 64 && blocked == null; seed++)
        {
            Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(seed, 1, 3, settings, out var map, out string error, true), error);
            if (!AllExitsReachableWithoutBoss(map)) blocked = map;
        }
        Assert.IsNotNull(blocked, "Need a real blocked OFF fixture.");
        Assert.IsFalse(blocked.bossAvoidsExtractionRoutes);
        Assert.IsTrue(RadialBraidedRaidGenerator.TryValidate(blocked, out _));
        string olderJson = JsonUtility.ToJson(blocked).Replace("\"bossAvoidsExtractionRoutes\":false,", "");
        var olderSave = JsonUtility.FromJson<StageMapLayout>(olderJson);
        Assert.IsFalse(olderSave.bossAvoidsExtractionRoutes);
        Assert.IsTrue(RadialBraidedRaidGenerator.TryValidate(olderSave, out _));
        blocked.bossAvoidsExtractionRoutes = true;
        Assert.IsFalse(RadialBraidedRaidGenerator.TryValidate(blocked, out string invalid));
        StringAssert.Contains("보스방을 통과하지 않으면", invalid);
    }

    [Test] public void ImpossibleProtectedSmallMapReportsReasonWithoutRemovingBossOrExit()
    {
        var settings = new RaidMapSettings { bossSpawnPercent = 100, extractionCount = 6, roomDensity = .65f,
            vaultNearCandidateChance = 0 };
        string before = JsonUtility.ToJson(settings);
        Assert.IsFalse(RadialBraidedRaidGenerator.TryGenerate(2, 1, 3, settings, out var failed, out string error, true));
        Assert.IsNull(failed); StringAssert.Contains("보스가 탈출 경로", error);
        Assert.AreEqual(before, JsonUtility.ToJson(settings));
        settings.preventBossBlockingExtraction = false;
        Assert.IsTrue(RadialBraidedRaidGenerator.TryGenerate(2, 1, 3, settings, out var map, out error, true), error);
        Assert.IsTrue(map.bossSpawned); Assert.IsFalse(map.bossAvoidsExtractionRoutes);
    }

    private static StageMapLayout MerchantFixture(int seed)
    {
        var map = new StageMapLayout { generationVersion = 3, landmarkPlacementVersion = 1,
            usesRaidLayout = true, stageSeed = StageMapGenerator.BuildStageSeed(seed, 1), startSectorCoord = Vector2Int.zero };
        for (int i = 0; i < 9; i++) map.rooms.Add(new StageRoomNode(new Vector2Int(i, 0), StageRoomType.Normal, StageRoomConnectionFlags.None)
            { raidRole = RaidRoomRole.Normal, combatPoolId = "common" });
        map.rooms[3].combatPoolId = "theme";
        map.rooms[4].specialSectorPoolId = "office";
        map.rooms[5].roomType = StageRoomType.Boss; map.rooms[5].raidRole = RaidRoomRole.Boss;
        map.rooms[6].raidRole = RaidRoomRole.Generator;
        map.extractionRooms.Add(map.rooms[7].coord); map.extractionCandidates.Add(map.rooms[7].coord);
        map.rooms[8].hasInternalCombatLock = true;
        return map;
    }

    [TestCase(SectorPortalDirection.XMin, "← 서쪽")]
    [TestCase(SectorPortalDirection.XMax, "→ 동쪽")]
    [TestCase(SectorPortalDirection.ZMin, "↓ 남쪽")]
    [TestCase(SectorPortalDirection.ZMax, "↑ 북쪽")]
    public void PreviewExitBadgeUsesActualPortalMetadataNotTheNormalRoomCategory(SectorPortalDirection direction, string label)
    {
        var coord = new Vector2Int(2, 1);
        var map = new StageMapLayout { generationVersion = 3, hasExtraction = true, extractionPortalVersion = 1 };
        map.extractionRooms.Add(coord);
        map.extractionPortals.Add(new RaidExtractionPortalRoute { coord = coord, direction = direction });
        var normal = new StageRoomNode(coord, StageRoomType.Normal, StageRoomConnectionFlags.None) { combatPoolId = "common" };
        Assert.AreEqual("공용 전투", StageMakerPreview.Category(normal));
        Assert.AreEqual("외곽 탈출 " + label, StageMakerPreview.ExtractionLabel(map, coord));
        Assert.AreEqual("외곽 탈출 " + label[0], StageMakerPreview.ExtractionLabel(map, coord, true));
        Assert.AreEqual(string.Empty, StageMakerPreview.ExtractionLabel(map, Vector2Int.zero));
        map.hasExtraction = false;
        Assert.AreEqual(string.Empty, StageMakerPreview.ExtractionLabel(map, coord));
    }

    [Test] public void MerchantIsOncePerStageTenPercentNotPerRoomAndNeverClearsCombat()
    {
        var settings = new RaidMapSettings(); Assert.AreEqual(10, settings.merchantSpawnPercent);
        int spawned = 0;
        for (int seed = 0; seed < 10000; seed++)
        {
            var map = MerchantFixture(seed);
            Assert.IsTrue(RaidMerchantPlacement.TryAssign(map, settings, out string error), error);
            var merchants = map.rooms.Where(room => room.hasMerchant).ToList();
            Assert.LessOrEqual(merchants.Count, 1);
            if (merchants.Count == 0) continue;
            spawned++;
            Assert.That(merchants[0].coord.x, Is.EqualTo(1).Or.EqualTo(2));
            Assert.AreEqual(RaidRoomRole.Normal, merchants[0].raidRole);
            Assert.IsFalse(merchants[0].isCleared); Assert.AreEqual("common", merchants[0].combatPoolId);
        }
        Assert.That(spawned, Is.InRange(850, 1150), "Stage-level roll should be near 10%, not one roll per room.");
    }

    [Test] public void MerchantZeroHundredMissingCommonAndResumeUseStoredDecision()
    {
        var off = MerchantFixture(1); var settings = new RaidMapSettings { merchantSpawnPercent = 0 };
        Assert.IsTrue(RaidMerchantPlacement.TryAssign(off, settings, out string error), error);
        Assert.IsFalse(off.merchantRollSucceeded); Assert.IsFalse(off.rooms.Any(room => room.hasMerchant));
        settings.merchantSpawnPercent = 100;
        Assert.IsTrue(RaidMerchantPlacement.TryAssign(off, settings, out error), error);
        Assert.IsFalse(off.rooms.Any(room => room.hasMerchant), "Failed roll is also a stored decision.");
        var map = MerchantFixture(1);
        Assert.IsTrue(RaidMerchantPlacement.TryAssign(map, settings, out error), error);
        Assert.IsTrue(map.merchantRollSucceeded); Assert.AreEqual(1, map.rooms.Count(room => room.hasMerchant));
        string before = JsonUtility.ToJson(map);
        var saved = JsonUtility.FromJson<StageMapLayout>(before);
        settings.merchantSpawnPercent = 0;
        Assert.IsTrue(RaidMerchantPlacement.TryAssign(saved, settings, out error), error);
        Assert.AreEqual(before, JsonUtility.ToJson(saved));
        var empty = MerchantFixture(1); foreach (var room in empty.rooms) room.combatPoolId = "theme";
        settings.merchantSpawnPercent = 100;
        Assert.IsTrue(RaidMerchantPlacement.TryAssign(empty, settings, out error), error);
        Assert.IsTrue(empty.merchantRollSucceeded); Assert.IsFalse(empty.rooms.Any(room => room.hasMerchant));
    }

    [Test] public void PipelineAddsMerchantAfterCompositionAndPreservesGraphAndRoomIdentity()
    {
        var stage = Stage(); stage.RoomComposition.enabled = true;
        stage.RoomComposition.themePercent = 50; stage.RoomComposition.commonPercent = 50; stage.RoomComposition.specialPercent = 0;
        stage.RoomComposition.themePresets.Add(new StageDefinitionSO.WeightedSectorPreset { preset = Preset("Theme") });
        var common = Preset("Common");
        stage.RoomComposition.commonPresets.Add(new StageDefinitionSO.WeightedSectorPreset { preset = common });
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out var baseline, out string error, readRuntimeQuests: false), error);
        stage.RaidMap.merchantSpawnPercent = 100;
        Assert.IsTrue(StageMapBuildPipeline.TryGenerate(stage, 7, Vector2Int.left, out var map, out error, readRuntimeQuests: false), error);
        Assert.AreEqual(baseline.rooms.Count, map.rooms.Count);
        var merchant = map.rooms.Single(room => room.hasMerchant);
        Assert.AreEqual(RaidRoomRole.Normal, merchant.raidRole); Assert.IsFalse(merchant.isCleared);
        Assert.AreEqual("공용 전투 · 상인", StageMakerPreview.Category(merchant));
        Assert.IsTrue(StageMapBuildPipeline.TryResolveRoomPreset(stage, map, merchant, out var actual, out _, out error), error);
        Assert.AreSame(common, actual);
        Assert.IsTrue(RadialBraidedRaidGenerator.TryValidate(map, out error), error);
        var restored = JsonUtility.FromJson<StageMapLayout>(JsonUtility.ToJson(map));
        Assert.IsTrue(RadialBraidedRaidGenerator.TryValidate(restored, out error), error);
        var cell = new SectorMapCellSnapshot(); RaidMapPresentation.Apply(map, merchant, merchant.coord, ref cell);
        Assert.AreEqual(RaidRoomRole.Merchant, cell.raidRole); Assert.AreEqual(RaidRoomRole.Normal, merchant.raidRole);
    }
}
