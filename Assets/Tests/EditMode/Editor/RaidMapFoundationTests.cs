using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RaidMapFoundationTests
{
    [TestCase(7, 1)] [TestCase(9, 2)] [TestCase(11, 3)] [TestCase(13, 4)] [TestCase(13, 5)]
    public void ThousandSeedsHaveValidDistinctReachableSpecialRooms(int size, int stage)
    {
        int bosses = 0;
        int upperLeft = 0;
        var corners = new Dictionary<Vector2Int, int>();
        var settings = new RaidMapSettings { minimumExtractionPathDistance = size };
        for (int seed = 0; seed < 1000; seed++)
        {
            Assert.IsTrue(RaidMapGenerator.TryGenerate(seed, stage, size, Vector2Int.left, settings,
                out StageMapLayout map, out string error), error);
            Assert.IsTrue(RaidMapValidation.TryValidate(map, out error), $"Seed {seed}: {error}");
            Assert.AreEqual(size * size + 1, map.rooms.Count);
            Assert.AreEqual(new Vector2Int(size / 2, size / 2), map.hatchRoomCoord);
            Assert.AreNotEqual(map.bossRoomCoord, map.extractionSectorCoord);
            Assert.IsTrue(map.IsExtractionPortal(map.extractionSectorCoord, map.extractionDirection));
            Assert.IsTrue(RaidExtractionPortalPolicy.TryValidate(map, out error), error);
            Assert.AreEqual(1, map.rooms.FindAll(r => r.raidRole == RaidRoomRole.Generator).Count);
            Assert.AreEqual(1, map.rooms.FindAll(r => r.raidRole == RaidRoomRole.Hatch).Count);
            AssertCornerLayout(map);
            Assert.GreaterOrEqual(RaidMapGenerator.GetDistances(map, map.startSectorCoord)[map.extractionSectorCoord], size);
            corners.TryGetValue(map.firstRoomCoord, out int count);
            corners[map.firstRoomCoord] = count + 1;
            if (map.bossSpawned) bosses++;
            if (map.generatorRoomCoord.x < size / 2) upperLeft++;
        }
        Assert.That(bosses, Is.InRange(550, 750));
        Assert.That(upperLeft, Is.InRange(400, 600));
        Assert.AreEqual(4, corners.Count);
        foreach (int count in corners.Values) Assert.That(count, Is.InRange(150, 350));
    }

    [TestCase(0)] [TestCase(65)] [TestCase(100)]
    public void GenerationIsDeterministicAndBossRollHasCorrectBounds(int percent)
    {
        var settings = new RaidMapSettings { bossSpawnPercent = percent };
        for (int seed = -10; seed < 20; seed++)
        {
            Assert.IsTrue(RaidMapGenerator.TryGenerate(seed, 1, 7, Vector2Int.left, settings, out var a, out _));
            Assert.IsTrue(RaidMapGenerator.TryGenerate(seed, 1, 7, Vector2Int.left, settings, out var b, out _));
            Assert.AreEqual(JsonUtility.ToJson(a), JsonUtility.ToJson(b));
            if (percent == 0) Assert.IsFalse(a.bossSpawned);
            if (percent == 100) Assert.IsTrue(a.bossSpawned);
        }
    }

    [Test]
    public void ContentWeightsDoNotChangeSpecialRoomPlacementOrBossRoll()
    {
        RaidMapGenerator.TryGenerate(55, 3, 11, Vector2Int.left, new RaidMapSettings(), out var a, out _);
        RaidMapGenerator.TryGenerate(55, 3, 11, Vector2Int.left,
            new RaidMapSettings { normalWeight = 0, armoryWeight = 100, medicalWeight = 0, vaultWeight = 0 }, out var b, out _);
        Assert.AreEqual(a.bossRoomCoord, b.bossRoomCoord);
        Assert.AreEqual(a.bossSpawned, b.bossSpawned);
        Assert.AreEqual(a.generatorRoomCoord, b.generatorRoomCoord);
        Assert.AreEqual(a.extractionSectorCoord, b.extractionSectorCoord);
        Assert.AreEqual(a.startSectorCoord, b.startSectorCoord);
        Assert.AreEqual(a.firstRoomCoord, b.firstRoomCoord);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(3)] [TestCase(4)] [TestCase(8)]
    public void InvalidSizesAreRejected(int size)
    {
        Assert.IsFalse(RaidMapGenerator.TryGenerate(1, 1, size, Vector2Int.left,
            new RaidMapSettings(), out var map, out string error));
        Assert.IsNull(map);
        Assert.IsNotEmpty(error);
    }

    [Test]
    public void ImpossibleDistanceFailsWithoutRetryAndExactDistanceSucceeds()
    {
        var settings = new RaidMapSettings { minimumExtractionPathDistance = 13 };
        Assert.IsTrue(RaidMapGenerator.TryGenerate(1, 1, 7, Vector2Int.left, settings, out var map, out _));
        Assert.AreEqual(new Vector2Int(6 - map.firstRoomCoord.x, 6 - map.firstRoomCoord.y), map.extractionSectorCoord);
        Assert.AreEqual(13, RaidMapGenerator.GetDistances(map, map.startSectorCoord)[map.extractionSectorCoord]);
        settings.minimumExtractionPathDistance = 14;
        Assert.IsFalse(RaidMapGenerator.TryGenerate(1, 1, 7, Vector2Int.left, settings, out map, out _));
        Assert.IsNull(map);
    }

    [Test]
    public void StoredLayoutAndStateRoundTripWithoutReroll()
    {
        RaidMapGenerator.TryGenerate(101, 4, 13, Vector2Int.left, new RaidMapSettings(), out var layout, out _);
        var run = new RunSaveData { raid = new RaidRunSaveData
        { active = true, layout = layout, playerSectorCoord = layout.generatorRoomCoord,
          generatorPowered = true, hatchRevealed = true, remainingSeconds = 123.25f, gasReleased = true } };
        string json = JsonUtility.ToJson(run);
        RunSaveData restored = JsonUtility.FromJson<RunSaveData>(json);
        Assert.IsNotNull(restored.raid.layout);
        Assert.IsTrue(SaveMigration.TryMigrateRun(restored, out string error), error);
        Assert.AreEqual(json, JsonUtility.ToJson(restored));
    }

    [Test]
    public void AllCornersRoundTripAndKeepTheirExternalReturnHub()
    {
        var corners = new HashSet<Vector2Int>();
        for (int seed = 0; seed < 100 && corners.Count < 4; seed++)
        {
            RaidMapGenerator.TryGenerate(seed, 1, 7, Vector2Int.left, new RaidMapSettings(), out var map, out _);
            if (!corners.Add(map.firstRoomCoord)) continue;
            var run = new RunSaveData { raid = new RaidRunSaveData
            { active = true, layout = map, playerSectorCoord = map.startSectorCoord } };
            string json = JsonUtility.ToJson(run);
            var restored = JsonUtility.FromJson<RunSaveData>(json);
            Assert.IsTrue(SaveMigration.TryMigrateRun(restored, out string error), error);
            Assert.AreEqual(json, JsonUtility.ToJson(restored));
            Assert.IsTrue(RaidRuntime.AllowsExtraction(restored.raid, map.startSectorCoord, RaidStationKind.StartReturn));
            Assert.IsFalse(RaidRuntime.AllowsExtraction(restored.raid, map.firstRoomCoord, RaidStationKind.StartReturn));
        }
        Assert.AreEqual(4, corners.Count);
    }

    [Test]
    public void SavedVersionOneMapKeepsItsOriginalCoordinatesAndState()
    {
        // Construct a stored V1 graph independently of the V2 generator.
        var map = StageMapGenerator.GenerateFullGrid(42, 3, 11, Vector2Int.left, StageRoomType.Boss);
        map.usesRaidLayout = true;
        map.generationVersion = 1;
        map.hatchRoomCoord = new Vector2Int(5, 5);
        map.generatorRoomCoord = new Vector2Int(2, 8);
        map.bossRoomCoord = map.goalRoomCoord = new Vector2Int(9, 9);
        map.extractionSectorCoord = new Vector2Int(10, 10);
        map.bossSpawned = true;
        foreach (var room in map.rooms)
        {
            room.roomType = room.coord == map.startSectorCoord ? StageRoomType.Start : StageRoomType.Normal;
            room.raidRole = room.coord == map.startSectorCoord ? RaidRoomRole.None : RaidRoomRole.Normal;
            if (room.coord == map.hatchRoomCoord) room.raidRole = RaidRoomRole.Hatch;
            if (room.coord == map.generatorRoomCoord) room.raidRole = RaidRoomRole.Generator;
            if (room.coord == map.extractionSectorCoord) room.raidRole = RaidRoomRole.Extraction;
            if (room.coord == map.bossRoomCoord) { room.raidRole = RaidRoomRole.Boss; room.roomType = StageRoomType.Boss; }
        }
        var run = new RunSaveData { raid = new RaidRunSaveData
        { active = true, layout = map, playerSectorCoord = map.generatorRoomCoord,
          generatorPowered = true, hatchRevealed = true, remainingSeconds = 51 } };
        string before = JsonUtility.ToJson(run);
        var restored = JsonUtility.FromJson<RunSaveData>(before);
        Assert.IsTrue(SaveMigration.TryMigrateRun(restored, out string error), error);
        Assert.AreEqual(before, JsonUtility.ToJson(restored));
        Assert.AreEqual(Vector2Int.left, restored.raid.layout.startSectorCoord);
        Assert.AreEqual(1, restored.raid.layout.generationVersion);
    }

    [TestCase(0, 0)] [TestCase(-1, 0)] [TestCase(0, -1)]
    public void SupportedBaseAnchorsKeepConnectedEntriesAtEveryCorner(int x, int y)
    {
        var corners = new HashSet<Vector2Int>();
        for (int seed = 0; seed < 100; seed++)
        {
            Assert.IsTrue(RaidMapGenerator.TryGenerate(seed, 1, 7, new Vector2Int(x, y),
                new RaidMapSettings(), out var map, out string error), error);
            Assert.IsTrue(RaidMapValidation.TryValidate(map, out error), error);
            corners.Add(map.firstRoomCoord);
            Assert.AreEqual(49 + (x == 0 && y == 0 ? 0 : 1), map.rooms.Count);
            Assert.AreEqual(x == 0 && y == 0 ? 0 : 1,
                RaidMapGenerator.GetDistances(map, map.startSectorCoord)[map.firstRoomCoord]);
        }
        Assert.AreEqual(4, corners.Count);
    }

    [Test]
    public void ValidationRejectsMisplacedEntryHubAndSpecialRooms()
    {
        RaidMapGenerator.TryGenerate(3, 1, 7, Vector2Int.left, new RaidMapSettings(), out var map, out _);
        Vector2Int first = map.firstRoomCoord;
        map.firstRoomCoord = new Vector2Int(3, 3);
        Assert.IsFalse(RaidMapValidation.TryValidate(map, out _));
        map.firstRoomCoord = first;
        Vector2Int start = map.startSectorCoord;
        map.startSectorCoord += Vector2Int.one;
        Assert.IsFalse(RaidMapValidation.TryValidate(map, out _));
        map.startSectorCoord = start;
        Vector2Int generator = map.generatorRoomCoord;
        map.generatorRoomCoord = map.bossRoomCoord;
        Assert.IsFalse(RaidMapValidation.TryValidate(map, out _));
        map.generatorRoomCoord = generator;
        map.extractionSectorCoord = first;
        Assert.IsFalse(RaidMapValidation.TryValidate(map, out _));
    }

    private static void AssertCornerLayout(StageMapLayout map)
    {
        int middle = map.gridSize / 2;
        Vector2Int first = map.firstRoomCoord;
        Assert.That(first.x, Is.EqualTo(0).Or.EqualTo(map.gridSize - 1));
        Assert.That(first.y, Is.EqualTo(0).Or.EqualTo(map.gridSize - 1));
        int xSign = first.x == 0 ? -1 : 1;
        int ySign = first.y == 0 ? -1 : 1;
        foreach (var coord in new[] { map.bossRoomCoord, map.extractionSectorCoord })
        {
            Assert.Less((coord.x - middle) * xSign, 0);
            Assert.Less((coord.y - middle) * ySign, 0);
        }
        Assert.AreNotEqual(middle, map.generatorRoomCoord.x);
        Assert.AreNotEqual(middle, map.generatorRoomCoord.y);
        Assert.IsTrue(((map.generatorRoomCoord.x - middle) * xSign > 0) ^
                      ((map.generatorRoomCoord.y - middle) * ySign > 0));
        Assert.AreEqual(1, RaidMapGenerator.GetDistances(map, map.startSectorCoord)[first]);
        map.TryGetRoom(first, out var entry);
        Assert.AreEqual(RaidRoomRole.Normal, entry.raidRole);
        Assert.IsTrue(entry.isOpened);
        map.TryGetRoom(map.startSectorCoord, out var hub);
        Assert.IsTrue(hub.isCleared);
        Assert.IsTrue(hub.isRevealed);
        var offsets = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
        var flags = new[] { StageRoomConnectionFlags.Left, StageRoomConnectionFlags.Right,
            StageRoomConnectionFlags.Down, StageRoomConnectionFlags.Up };
        int hubLinks = 0;
        foreach (var room in map.rooms)
            for (int i = 0; i < 4; i++)
            {
                bool hasNeighbor = map.TryGetRoom(room.coord + offsets[i], out var neighbor);
                Assert.AreEqual(hasNeighbor, room.HasConnection(flags[i]), $"{room.coord}: {flags[i]}");
                if (!hasNeighbor) continue;
                Assert.IsTrue(neighbor.HasConnection(flags[i ^ 1]));
                if (room == hub) hubLinks++;
            }
        Assert.AreEqual(1, hubLinks);
    }

    [TestCase(0)] [TestCase(1)]
    public void OldRunMigrationDoesNotInventAMapOrLosePlayerState(int version)
    {
        var run = new RunSaveData { schemaVersion = version, raid = null };
        run.PlayerRunState.currentHealth = 72f;
        run.PlayerRunState.hasSnapshot = true;
        Assert.IsTrue(SaveMigration.TryMigrateRun(run, out string error), error);
        Assert.AreEqual(2, run.schemaVersion);
        Assert.AreEqual(72f, run.PlayerRunState.currentHealth);
        Assert.IsFalse(run.raid.active);
        Assert.IsNull(run.raid.layout);
    }

    [Test]
    public void UnknownGenerationOrCorruptGraphCannotSilentlyReroll()
    {
        RaidMapGenerator.TryGenerate(1, 1, 7, Vector2Int.left, new RaidMapSettings(), out var layout, out _);
        var run = new RunSaveData { raid = new RaidRunSaveData { active = true, layout = layout, playerSectorCoord = Vector2Int.zero } };
        layout.generationVersion++;
        Assert.IsFalse(SaveMigration.TryMigrateRun(run, out _));
        layout.generationVersion--;
        layout.rooms.RemoveAt(1);
        Assert.IsFalse(SaveMigration.TryMigrateRun(run, out _));
    }

    [Test]
    public void ExistingFullGridKeepsLegacyEdgeExitAndSerializedTypes()
    {
        var layout = StageMapGenerator.GenerateFullGrid(1, 1, 4, Vector2Int.left, StageRoomType.Boss);
        Assert.IsFalse(layout.usesRaidLayout);
        Assert.AreEqual(0, layout.generationVersion);
        Assert.IsTrue(layout.IsExtractionPortal(layout.extractionSectorCoord, layout.extractionDirection));
        Assert.AreEqual(2, (int)StageRoomType.Normal);
        Assert.AreEqual(6, (int)StageRoomType.Boss);
    }
}
