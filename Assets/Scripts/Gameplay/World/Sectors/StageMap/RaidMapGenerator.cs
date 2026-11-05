using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Pure, versioned generation. Coordinates are zero based, +Y is up.</summary>
public static class RaidMapGenerator
{
    public const int GenerationVersion = 2;

    // V1 is the saved bottom-left layout. Loading it must never rotate/reroll it.
    public static bool IsSupportedGenerationVersion(int version) => version == 1 || version == GenerationVersion ||
        version == RadialBraidedRaidGenerator.GenerationVersion;
    private static readonly Vector2Int[] Offsets =
        { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
    private static readonly StageRoomConnectionFlags[] Flags =
        { StageRoomConnectionFlags.Left, StageRoomConnectionFlags.Right,
          StageRoomConnectionFlags.Down, StageRoomConnectionFlags.Up };

    /// <summary>
    /// Start is the base bottom-left entry anchor (normally the external hub at -1,0).
    /// New raids rotate that entire layout to one of four corners, using a separate seed stream.
    /// </summary>
    public static bool TryGenerate(int runSeed, int stageIndex, int size, Vector2Int start,
        RaidMapSettings settings, out StageMapLayout layout, out string error)
    {
        layout = null;
        if (size < 5 || (size & 1) == 0)
        {
            error = "Odd-square raids require an odd grid size of at least 5.";
            return false;
        }
        if (start != Vector2Int.zero && start != Vector2Int.left && start != Vector2Int.down)
        {
            error = "Raid base entry anchor must be (0,0), (-1,0), or (0,-1); the final corner is randomized.";
            return false;
        }
        if (settings == null) { error = "Raid map settings are missing."; return false; }
        if (!settings.TryValidate(out error)) return false;

        int seed = StageMapGenerator.BuildStageSeed(runSeed, stageIndex);
        int middle = size / 2;
        var candidate = new StageMapLayout
        {
            runSeed = runSeed, stageIndex = stageIndex, stageSeed = seed, gridSize = size,
            generationVersion = GenerationVersion, usesRaidLayout = true,
            startSectorCoord = start, firstRoomCoord = Vector2Int.zero,
            hatchRoomCoord = new Vector2Int(middle, middle), hasExtraction = true
        };
        var nodes = new Dictionary<Vector2Int, StageRoomNode>();
        if (start != Vector2Int.zero) AddNode(candidate, nodes, start, StageRoomType.Start);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var coord = new Vector2Int(x, y);
                AddNode(candidate, nodes, coord, coord == start ? StageRoomType.Start : StageRoomType.Normal);
            }
        foreach (StageRoomNode node in candidate.rooms)
            for (int direction = 0; direction < Offsets.Length; direction++)
                if (nodes.ContainsKey(node.coord + Offsets[direction])) node.connections |= Flags[direction];

        Dictionary<Vector2Int, int> distances = GetDistances(candidate, start);
        var northEast = new List<Vector2Int>();
        var exits = new List<Vector2Int>();
        for (int y = middle + 1; y < size; y++)
            for (int x = middle + 1; x < size; x++)
            {
                var coord = new Vector2Int(x, y);
                northEast.Add(coord);
                if ((x == size - 1 || y == size - 1) && distances.TryGetValue(coord, out int distance) && distance >= settings.minimumExtractionPathDistance)
                    exits.Add(coord);
            }
        if (exits.Count == 0)
        {
            error = "No extraction room in the opposite quadrant satisfies the configured path distance.";
            return false;
        }
        candidate.extractionSectorCoord = exits[Random(seed, 101).Next(exits.Count)];
        northEast.Remove(candidate.extractionSectorCoord);
        candidate.bossRoomCoord = northEast[Random(seed, 103).Next(northEast.Count)];
        candidate.goalRoomCoord = candidate.bossRoomCoord; // Legacy inspection only; extraction is independent.
        candidate.bossSpawned = Random(seed, 107).Next(100) < settings.bossSpawnPercent;

        var generatorRandom = Random(seed, 109);
        bool upperLeft = generatorRandom.Next(2) == 0;
        candidate.generatorRoomCoord = upperLeft
            ? new Vector2Int(generatorRandom.Next(middle), middle + 1 + generatorRandom.Next(middle))
            : new Vector2Int(middle + 1 + generatorRandom.Next(middle), generatorRandom.Next(middle));

        var contentRandom = Random(seed, 113);
        foreach (StageRoomNode node in candidate.rooms)
        {
            if (node.roomType == StageRoomType.Start) continue;
            node.raidRole = PickRole(contentRandom, settings);
        }
        nodes[Vector2Int.zero].raidRole = RaidRoomRole.Normal;
        nodes[candidate.hatchRoomCoord].raidRole = RaidRoomRole.Hatch;
        nodes[candidate.generatorRoomCoord].raidRole = RaidRoomRole.Generator;
        nodes[candidate.extractionSectorCoord].raidRole = RaidRoomRole.Extraction;
        // An empty Boss site stays reserved but contains no Boss encounter/marker.
        nodes[candidate.bossRoomCoord].raidRole = RaidRoomRole.Boss;
        nodes[candidate.bossRoomCoord].roomType = candidate.bossSpawned ? StageRoomType.Boss : StageRoomType.Normal;
        // Rotate coordinates AND connections. Distances, room roles and the single external
        // hub link stay identical; room prefab art itself retains its world-axis portals.
        RotateLayout(candidate, GetQuarterTurns(seed));
        if (!RaidExtractionPortalPolicy.TryPrepare(candidate, out error)) return false;
        layout = candidate;
        error = string.Empty;
        return true;
    }

    private static int GetQuarterTurns(int seed)
    {
        // Avalanche the corner seed so adjacent XOR salts cannot correlate the
        // rotation with the existing generator-side and boss random streams.
        unchecked
        {
            uint mixed = (uint)seed ^ 0x9E3779B9u;
            mixed = (mixed ^ (mixed >> 16)) * 0x7FEB352Du;
            mixed = (mixed ^ (mixed >> 15)) * 0x846CA68Bu;
            return (int)((mixed ^ (mixed >> 16)) & 3u);
        }
    }

    private static void RotateLayout(StageMapLayout layout, int quarterTurns)
    {
        int edge = layout.gridSize - 1;
        for (int turn = 0; turn < quarterTurns; turn++)
        {
            layout.startSectorCoord = RotateCoord(layout.startSectorCoord, edge);
            layout.firstRoomCoord = RotateCoord(layout.firstRoomCoord, edge);
            layout.goalRoomCoord = RotateCoord(layout.goalRoomCoord, edge);
            layout.bossRoomCoord = RotateCoord(layout.bossRoomCoord, edge);
            layout.extractionSectorCoord = RotateCoord(layout.extractionSectorCoord, edge);
            layout.generatorRoomCoord = RotateCoord(layout.generatorRoomCoord, edge);
            layout.hatchRoomCoord = RotateCoord(layout.hatchRoomCoord, edge);
            foreach (StageRoomNode node in layout.rooms)
            {
                node.coord = RotateCoord(node.coord, edge);
                StageRoomConnectionFlags previous = node.connections;
                node.connections = StageRoomConnectionFlags.None;
                if ((previous & StageRoomConnectionFlags.Left) != 0) node.connections |= StageRoomConnectionFlags.Down;
                if ((previous & StageRoomConnectionFlags.Right) != 0) node.connections |= StageRoomConnectionFlags.Up;
                if ((previous & StageRoomConnectionFlags.Down) != 0) node.connections |= StageRoomConnectionFlags.Right;
                if ((previous & StageRoomConnectionFlags.Up) != 0) node.connections |= StageRoomConnectionFlags.Left;
            }
        }
    }

    private static Vector2Int RotateCoord(Vector2Int coord, int edge) => new Vector2Int(edge - coord.y, coord.x);

    public static Dictionary<Vector2Int, int> GetDistances(StageMapLayout layout, Vector2Int from)
    {
        var result = new Dictionary<Vector2Int, int>();
        if (layout?.rooms == null) return result;
        var nodes = new Dictionary<Vector2Int, StageRoomNode>();
        foreach (StageRoomNode node in layout.rooms)
            if (node != null && !nodes.ContainsKey(node.coord)) nodes.Add(node.coord, node);
        if (!nodes.ContainsKey(from)) return result;
        var queue = new Queue<Vector2Int>();
        result.Add(from, 0);
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            Vector2Int coord = queue.Dequeue();
            for (int i = 0; i < Offsets.Length; i++)
            {
                Vector2Int next = coord + Offsets[i];
                if (!nodes[coord].HasConnection(Flags[i]) || !nodes.TryGetValue(next, out StageRoomNode neighbor) ||
                    !neighbor.HasConnection(Flags[i ^ 1]) || result.ContainsKey(next)) continue;
                result.Add(next, result[coord] + 1);
                queue.Enqueue(next);
            }
        }
        return result;
    }

    private static void AddNode(StageMapLayout layout, Dictionary<Vector2Int, StageRoomNode> nodes,
        Vector2Int coord, StageRoomType type)
    {
        var node = new StageRoomNode(coord, type, StageRoomConnectionFlags.None)
        { isOpened = coord == layout.startSectorCoord || coord == Vector2Int.zero,
          isRevealed = coord == layout.startSectorCoord, isCleared = type == StageRoomType.Start };
        nodes.Add(coord, node);
        layout.rooms.Add(node);
    }

    private static System.Random Random(int seed, int salt) => new System.Random(unchecked(seed * 397 ^ salt));
    private static RaidRoomRole PickRole(System.Random random, RaidMapSettings settings)
    {
        int roll = random.Next(settings.normalWeight + settings.armoryWeight + settings.medicalWeight + settings.vaultWeight);
        if ((roll -= settings.normalWeight) < 0) return RaidRoomRole.Normal;
        if ((roll -= settings.armoryWeight) < 0) return RaidRoomRole.Armory;
        if ((roll -= settings.medicalWeight) < 0) return RaidRoomRole.Medical;
        return RaidRoomRole.Vault;
    }
}
