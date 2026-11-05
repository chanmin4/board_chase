using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// V3: central hub, initial ring/spokes, connected sparse growth and optional non-tree edges.
/// Terminal generator/exits are reserved afterwards. Local RNG streams separate content.
/// Both even and odd bounds are supported; no silent grid-size rounding.
/// </summary>
public static partial class RadialBraidedRaidGenerator
{
    public const int GenerationVersion = 3;
    private static readonly Vector2Int[] Steps = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
    private static readonly StageRoomConnectionFlags[] Flags = { StageRoomConnectionFlags.Left,
        StageRoomConnectionFlags.Right, StageRoomConnectionFlags.Down, StageRoomConnectionFlags.Up };

    public static bool TryGenerate(int runSeed, int stageIndex, int size, RaidMapSettings settings,
        out StageMapLayout layout, out string error, bool internalCombatVaults = false)
    {
        layout = null;
        error = null;
        if (settings == null) { error = "맵 생성 설정 오류: Raid Map 설정이 없습니다."; return false; }
        if (!settings.TryValidateSparse(size, out error)) return false;
        if (size < 8) return TryGenerateCompact(runSeed, stageIndex, size, settings, out layout, out error);

        var rules = settings.customizeTopology && settings.topology != null ? settings.topology : new RaidTopologyRules();
        int seed = StageMapGenerator.BuildStageSeed(runSeed, stageIndex);
        var center = new Vector2Int(size / 2, size / 2);
        var result = new StageMapLayout { runSeed = runSeed, stageIndex = stageIndex, stageSeed = seed,
            gridSize = size, generationVersion = GenerationVersion, usesRaidLayout = true, landmarkPlacementVersion = 1,
            startSectorCoord = center, firstRoomCoord = center, hatchRoomCoord = center, hasExtraction = true,
            bossAvoidsExtractionRoutes = settings.preventBossBlockingExtraction };
        var nodes = new Dictionary<Vector2Int, StageRoomNode>();
        StageRoomNode Add(Vector2Int coord)
        {
            if (!nodes.TryGetValue(coord, out var node))
            {
                node = new StageRoomNode(coord, StageRoomType.Normal, StageRoomConnectionFlags.None)
                    { raidRole = RaidRoomRole.Normal };
                nodes.Add(coord, node);
                result.rooms.Add(node);
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

        Add(center);
        int radius = rules.ringRadius > 0 ? rules.ringRadius : Mathf.Max(1, Mathf.FloorToInt(size * rules.ringRadiusRatio));
        var ring = new List<Vector2Int>();
        for (int x = -radius; x < radius; x++) ring.Add(center + new Vector2Int(x, -radius));
        for (int y = -radius; y < radius; y++) ring.Add(center + new Vector2Int(radius, y));
        for (int x = radius; x > -radius; x--) ring.Add(center + new Vector2Int(x, radius));
        for (int y = radius; y > -radius; y--) ring.Add(center + new Vector2Int(-radius, y));
        for (int i = 0; i < ring.Count; i++) Connect(ring[i], ring[(i + 1) % ring.Count]);
        // Four spokes reach the bounds. The center is a real traversable room, not an external hub.
        for (int i = 0; i < 4; i++)
        {
            Vector2Int previous = center;
            while (InBounds(previous + Steps[i]))
            { Vector2Int next = previous + Steps[i]; Connect(previous, next); previous = next; }
        }
        if (rules.maximumRoomCount > 0 && rules.maximumRoomCount < nodes.Count)
        { error = $"기본 링/연결 통로에 {nodes.Count}방이 필요하지만 최대 방 수가 {rules.maximumRoomCount}입니다."; return false; }
        int target = Mathf.Max(nodes.Count, Mathf.Max(rules.minimumRoomCount,
            rules.targetRoomCount > 0 ? rules.targetRoomCount : Mathf.RoundToInt(size * size * settings.roomDensity)));
        if (rules.maximumRoomCount > 0) target = Mathf.Min(target, rules.maximumRoomCount);
        var growth = Rng(seed, 1301);
        while (nodes.Count < target)
        {
            // Stable ordered frontier; Dictionary enumeration order is not a seed contract.
            var frontier = new List<(Vector2Int from, Vector2Int to)>();
            foreach (var room in result.rooms)
                foreach (var step in Steps)
                    if (InBounds(room.coord + step) && !nodes.ContainsKey(room.coord + step))
                        frontier.Add((room.coord, room.coord + step));
            if (frontier.Count == 0) break;
            var edge = frontier[growth.Next(frontier.Count)]; Connect(edge.from, edge.to);
        }
        var braid = Rng(seed, 1303);
        foreach (var room in result.rooms)
            for (int i = 1; i <= 3; i += 2)
                if (nodes.ContainsKey(room.coord + Steps[i]) && !room.HasConnection(Flags[i]) &&
                    braid.Next(100) < settings.braidPercent) Connect(room.coord, room.coord + Steps[i]);

        if (!TryAssignBranchLandmarks(result, settings, rules, out error)) return false;
        foreach (var room in result.rooms) nodes.TryAdd(room.coord, room);
        var vaultRandom = Rng(seed, 1313);
        foreach (var candidate in result.extractionCandidates)
        {
            if (vaultRandom.NextDouble() >= settings.vaultNearCandidateChance) continue;
            var nearbyRooms = result.rooms.FindAll(r => r.raidRole == RaidRoomRole.Normal &&
                !result.extractionCandidates.Contains(r.coord) &&
                (internalCombatVaults || CountConnections(r) == 1));
            if (nearbyRooms.Count > 0)
                nearbyRooms[vaultRandom.Next(nearbyRooms.Count)].raidRole = RaidRoomRole.Vault;
            else if (!internalCombatVaults)
            {
                // A vault is a leaf, never a locked room on a through-route.
                // Add one side branch AFTER braiding so it cannot gain extra links.
                var branches = new List<(Vector2Int parent, Vector2Int child)>();
                foreach (var parent in result.rooms)
                {
                    if (parent.coord == center || parent.raidRole is RaidRoomRole.Vault or RaidRoomRole.Boss ||
                        result.extractionCandidates.Contains(parent.coord)) continue;
                    foreach (var step in Steps)
                    {
                        Vector2Int child = parent.coord + step;
                        if (InBounds(child) && !nodes.ContainsKey(child)) branches.Add((parent.coord, child));
                    }
                }
                if (branches.Count > 0)
                {
                    var branch = branches[vaultRandom.Next(branches.Count)];
                    Connect(branch.parent, branch.child);
                    nodes[branch.child].raidRole = RaidRoomRole.Vault;
                }
            }
        }
        // Merchant overlay is assigned by the shared pipeline AFTER common/theme/special composition.
        if (!RaidExtractionPortalPolicy.TryPrepare(result, out error) || !TryValidate(result, out error)) return false;
        layout = result; return true;
    }

    public static bool TryValidate(StageMapLayout layout, out string error)
    {
        if (layout != null && layout.landmarkPlacementVersion == 1) return TryValidateBranchLandmarks(layout, out error);
        if (layout != null && layout.landmarkPlacementVersion != 0) { error = "알 수 없는 필수방 배치 버전입니다."; return false; }
        if (layout != null && layout.compactTestLayout) return TryValidateCompact(layout, out error);
        error = "Invalid saved sparse raid graph.";
        if (layout == null || !layout.usesRaidLayout || layout.generationVersion != GenerationVersion ||
            layout.gridSize < 8 || layout.gridSize > 64 || layout.usesBranchTopology || !layout.hasExtraction ||
            layout.rooms == null || layout.rooms.Count < 8 || layout.rooms.Count >= layout.gridSize * layout.gridSize ||
            layout.extractionCandidates == null || layout.extractionCandidates.Count < 4 ||
            layout.extractionRooms == null || layout.extractionRooms.Count < 1) return false;
        var center = new Vector2Int(layout.gridSize / 2, layout.gridSize / 2);
        if (layout.startSectorCoord != center || layout.firstRoomCoord != center || layout.hatchRoomCoord != center) return false;
        var nodes = new Dictionary<Vector2Int, StageRoomNode>();
        foreach (var room in layout.rooms)
            if (room == null || room.coord.x < 0 || room.coord.y < 0 || room.coord.x >= layout.gridSize ||
                room.coord.y >= layout.gridSize || !nodes.TryAdd(room.coord, room) ||
                (room.roomType != StageRoomType.Normal && room.roomType != StageRoomType.Boss) ||
                room.raidRole is not (RaidRoomRole.Normal or RaidRoomRole.Vault or RaidRoomRole.Boss or
                    RaidRoomRole.Generator or RaidRoomRole.Hatch or RaidRoomRole.Extraction or RaidRoomRole.Merchant) ||
                room.gasLeakStacks < 0 || (room.hasTraitSnapshot && !System.Enum.IsDefined(typeof(SectorRoomTraitType), room.savedTrait))) return false;
        int edges = 0;
        foreach (var room in layout.rooms)
        {
            if ((room.connections & ~(Flags[0] | Flags[1] | Flags[2] | Flags[3])) != 0) return false;
            for (int i = 0; i < 4; i++) if (room.HasConnection(Flags[i]))
            { if (!nodes.TryGetValue(room.coord + Steps[i], out var next) || !next.HasConnection(Flags[i ^ 1])) return false; edges++; }
        }
        if (edges / 2 < nodes.Count || RaidMapGenerator.GetDistances(layout, center).Count != nodes.Count) return false;
        var candidates = new HashSet<Vector2Int>();
        foreach (var c in layout.extractionCandidates) if (c == center || !nodes.ContainsKey(c) || !candidates.Add(c)) return false;
        var reserved = new HashSet<Vector2Int> { center };
        bool Role(Vector2Int c, RaidRoomRole role) => nodes.TryGetValue(c, out var room) && room.raidRole == role;
        if (!Role(center, RaidRoomRole.Hatch) || !Role(layout.generatorRoomCoord, RaidRoomRole.Generator) ||
            !Role(layout.bossRoomCoord, RaidRoomRole.Boss) || !candidates.Contains(layout.generatorRoomCoord) ||
            !candidates.Contains(layout.bossRoomCoord) || !reserved.Add(layout.generatorRoomCoord) ||
            !reserved.Add(layout.bossRoomCoord) || (nodes[layout.bossRoomCoord].roomType == StageRoomType.Boss) != layout.bossSpawned)
            return false;
        foreach (var c in layout.extractionRooms)
            if (!candidates.Contains(c) || !reserved.Add(c) ||
                !Role(c, layout.extractionPortalVersion == 0 ? RaidRoomRole.Extraction : RaidRoomRole.Normal)) return false;
        if (layout.rooms.FindAll(r => r.raidRole == RaidRoomRole.Hatch).Count != 1 ||
            layout.rooms.FindAll(r => r.raidRole == RaidRoomRole.Generator).Count != 1 ||
            layout.rooms.FindAll(r => r.raidRole == RaidRoomRole.Boss).Count != 1 ||
            layout.rooms.FindAll(r => r.raidRole == RaidRoomRole.Extraction).Count !=
                (layout.extractionPortalVersion == 0 ? layout.extractionRooms.Count : 0) ||
            layout.rooms.FindAll(r => r.roomType == StageRoomType.Boss).Count != (layout.bossSpawned ? 1 : 0)) return false;
        if (layout.extractionSectorCoord != layout.extractionRooms[0]) return false;
        if (layout.extractionPortalVersion != 0 && !RaidExtractionPortalPolicy.TryValidate(layout, out error)) return false;
        error = string.Empty; return true;
    }

    private static System.Random Rng(int seed, int salt) => new System.Random(StageDefinitionSO.BuildSectorSeed(seed, Vector2Int.zero, salt));

    private static int CountConnections(StageRoomNode room)
    {
        int count = 0;
        foreach (var flag in Flags) if (room.HasConnection(flag)) count++;
        return count;
    }
}
