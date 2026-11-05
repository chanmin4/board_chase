using System.Collections.Generic;
using UnityEngine;

public static partial class RadialBraidedRaidGenerator
{
    private static readonly Vector2Int MissingCompactRole = new(-1, -1);

    // Small authored test grids are still real raid layouts, with saved exits and no hidden SO edits.
    // 8+ grids never enter this path. Landmark versioning independently preserves older saves.
    private static bool TryGenerateCompact(int runSeed, int stageIndex, int size, RaidMapSettings settings,
        out StageMapLayout layout, out string error)
    {
        layout = null; error = null;
        int seed = StageMapGenerator.BuildStageSeed(runSeed, stageIndex);
        var center = new Vector2Int(size / 2, size / 2);
        var result = new StageMapLayout
        {
            runSeed = runSeed, stageIndex = stageIndex, stageSeed = seed, gridSize = size,
            generationVersion = GenerationVersion, usesRaidLayout = true, compactTestLayout = true, landmarkPlacementVersion = 1,
            startSectorCoord = center, firstRoomCoord = center, goalRoomCoord = center, hasExtraction = true,
            bossAvoidsExtractionRoutes = settings.preventBossBlockingExtraction,
            generatorRoomCoord = MissingCompactRole, bossRoomCoord = MissingCompactRole, hatchRoomCoord = MissingCompactRole
        };
        var nodes = new Dictionary<Vector2Int, StageRoomNode>();
        StageRoomNode Add(Vector2Int coord)
        {
            if (!nodes.TryGetValue(coord, out var node))
            {
                node = new StageRoomNode(coord, StageRoomType.Normal, StageRoomConnectionFlags.None) { raidRole = RaidRoomRole.Normal };
                nodes.Add(coord, node); result.rooms.Add(node);
            }
            return node;
        }
        void Connect(Vector2Int a, Vector2Int b)
        {
            var left = Add(a); var right = Add(b);
            for (int i = 0; i < 4; i++) if (b - a == Steps[i])
            { left.connections |= Flags[i]; right.connections |= Flags[i ^ 1]; return; }
        }
        bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < size && c.y < size;

        var start = Add(center);
        start.isCleared = start.isOpened = start.isRevealed = true;
        if (size == 1)
        {
            // The sole ordinary room owns an unused edge portal back HOME. No overlapping fake roles.
            result.extractionCandidates.Add(center); result.extractionRooms.Add(center);
            result.extractionSectorCoord = center;
            if (!RaidExtractionPortalPolicy.TryPrepare(result, out error) || !TryValidate(result, out error)) return false;
            layout = result; return true;
        }

        var rules = settings.customizeTopology && settings.topology != null ? settings.topology : new RaidTopologyRules();
        int target = rules.targetRoomCount > 0 ? rules.targetRoomCount : Mathf.RoundToInt(size * size * settings.roomDensity);
        target = Mathf.Max(target, rules.minimumRoomCount);
        if (rules.maximumRoomCount > 0) target = Mathf.Min(target, rules.maximumRoomCount);
        // Four distinct cells hold hatch + generator + boss candidate + at least one ordinary exit.
        target = Mathf.Clamp(target, 4, size * size);
        // A start node with two arms cannot consume one of the only two dead ends in tiny grids.
        int arms = 0;
        foreach (var step in Steps) if (InBounds(center + step) && arms < 2) { Connect(center, center + step); arms++; }
        var growth = Rng(seed, 1301);
        while (nodes.Count < target)
        {
            var frontier = new List<(Vector2Int from, Vector2Int to)>();
            foreach (var room in result.rooms)
                foreach (var step in Steps)
                    if (InBounds(room.coord + step) && !nodes.ContainsKey(room.coord + step))
                        frontier.Add((room.coord, room.coord + step));
            if (frontier.Count == 0) { error = "소형 테스트 맵에 연결 가능한 빈 칸이 없습니다."; return false; }
            var edge = frontier[growth.Next(frontier.Count)]; Connect(edge.from, edge.to);
        }
        var braid = Rng(seed, 1303);
        foreach (var room in result.rooms)
            for (int i = 1; i <= 3; i += 2)
                if (nodes.ContainsKey(room.coord + Steps[i]) && !room.HasConnection(Flags[i]) &&
                    braid.Next(100) < settings.braidPercent) Connect(room.coord, room.coord + Steps[i]);

        if (!TryAssignBranchLandmarks(result, settings, rules, out error)) return false;

        // Common-room merchant overlay is assigned after the shared content composition step.
        // Legacy extra vault branches/rings are deliberately omitted in compact tests. The authored
        // SpecialSectors ratio/quest selection still runs through the shared pipeline afterwards.
        if (!RaidExtractionPortalPolicy.TryPrepare(result, out error) || !TryValidate(result, out error)) return false;
        layout = result; return true;
    }

    private static bool TryValidateCompact(StageMapLayout layout, out string error)
    {
        error = "소형 테스트 저장 맵의 크기·역할·연결·탈출 정보가 올바르지 않습니다.";
        if (layout == null || !layout.compactTestLayout || !layout.usesRaidLayout || layout.usesBranchTopology ||
            layout.generationVersion != GenerationVersion || layout.gridSize < 1 || layout.gridSize >= 8 ||
            layout.rooms == null || layout.rooms.Count < 1 || layout.rooms.Count > layout.gridSize * layout.gridSize ||
            !layout.hasExtraction || layout.extractionCandidates == null || layout.extractionRooms == null || layout.extractionRooms.Count < 1)
            return false;
        var center = new Vector2Int(layout.gridSize / 2, layout.gridSize / 2);
        if (layout.startSectorCoord != center || layout.firstRoomCoord != center) return false;
        var nodes = new Dictionary<Vector2Int, StageRoomNode>();
        foreach (var room in layout.rooms)
            if (room == null || room.coord.x < 0 || room.coord.y < 0 || room.coord.x >= layout.gridSize || room.coord.y >= layout.gridSize ||
                !nodes.TryAdd(room.coord, room) || (room.roomType != StageRoomType.Normal && room.roomType != StageRoomType.Boss) ||
                room.raidRole is not (RaidRoomRole.Normal or RaidRoomRole.Hatch or RaidRoomRole.Generator or RaidRoomRole.Boss or RaidRoomRole.Merchant or RaidRoomRole.Vault) ||
                room.gasLeakStacks < 0 || (room.hasTraitSnapshot && !System.Enum.IsDefined(typeof(SectorRoomTraitType), room.savedTrait))) return false;
        if (!nodes.ContainsKey(center)) return false;
        foreach (var room in layout.rooms)
        {
            if ((room.connections & ~(Flags[0] | Flags[1] | Flags[2] | Flags[3])) != 0) return false;
            for (int i = 0; i < 4; i++) if (room.HasConnection(Flags[i]) &&
                (!nodes.TryGetValue(room.coord + Steps[i], out var other) || !other.HasConnection(Flags[i ^ 1]))) return false;
        }
        if (RaidMapGenerator.GetDistances(layout, center).Count != nodes.Count) return false;
        var candidates = new HashSet<Vector2Int>();
        foreach (var c in layout.extractionCandidates) if (!nodes.ContainsKey(c) || !candidates.Add(c)) return false;
        if (layout.gridSize == 1)
        {
            if (nodes.Count != 1 || nodes[center].raidRole != RaidRoomRole.Normal || nodes[center].roomType != StageRoomType.Normal ||
                layout.bossSpawned || layout.generatorRoomCoord != MissingCompactRole || layout.bossRoomCoord != MissingCompactRole ||
                layout.hatchRoomCoord != MissingCompactRole || layout.goalRoomCoord != center || candidates.Count != 1 || !candidates.Contains(center) ||
                layout.extractionRooms.Count != 1 || layout.extractionRooms[0] != center) return false;
        }
        else
        {
            bool Role(Vector2Int c, RaidRoomRole role) => nodes.TryGetValue(c, out var room) && room.raidRole == role;
            var reserved = new HashSet<Vector2Int> { center };
            if (layout.rooms.Count < 4 || candidates.Count < 3 || candidates.Contains(center) || layout.hatchRoomCoord != center ||
                !Role(center, RaidRoomRole.Hatch) || !Role(layout.generatorRoomCoord, RaidRoomRole.Generator) ||
                !Role(layout.bossRoomCoord, RaidRoomRole.Boss) || !reserved.Add(layout.generatorRoomCoord) || !reserved.Add(layout.bossRoomCoord) ||
                !candidates.Contains(layout.generatorRoomCoord) || !candidates.Contains(layout.bossRoomCoord) || layout.goalRoomCoord != layout.bossRoomCoord ||
                (nodes[layout.bossRoomCoord].roomType == StageRoomType.Boss) != layout.bossSpawned) return false;
            foreach (var c in layout.extractionRooms)
                if (!candidates.Contains(c) || !reserved.Add(c) || !Role(c, RaidRoomRole.Normal)) return false;
            foreach (var role in new[] { RaidRoomRole.Hatch, RaidRoomRole.Generator, RaidRoomRole.Boss })
                if (layout.rooms.FindAll(room => room.raidRole == role).Count != 1) return false;
            if (layout.rooms.FindAll(room => room.roomType == StageRoomType.Boss).Count != (layout.bossSpawned ? 1 : 0)) return false;
        }
        if (layout.extractionSectorCoord != layout.extractionRooms[0] || layout.extractionPortalVersion != 1) return false;
        return RaidExtractionPortalPolicy.TryValidate(layout, out error);
    }
}
