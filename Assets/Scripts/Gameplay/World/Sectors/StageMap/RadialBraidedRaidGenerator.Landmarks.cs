using System.Collections.Generic;
using UnityEngine;

public static partial class RadialBraidedRaidGenerator
{
    // Only power/extraction reserve outer leaves. Boss, merchants and content have no distance gate.
    private static bool TryAssignBranchLandmarks(StageMapLayout layout, RaidMapSettings settings,
        RaidTopologyRules rules, out string error)
    {
        error = null;
        int size = layout.gridSize;
        var center = layout.startSectorCoord;
        var nodes = new Dictionary<Vector2Int, StageRoomNode>();
        foreach (var room in layout.rooms) nodes.Add(room.coord, room);
        int desired = 1 + settings.extractionCount;
        if (layout.compactTestLayout) desired = Mathf.Min(desired, nodes.Count - 2);
        var selected = new List<StageRoomNode>();
        var random = Rng(layout.stageSeed, 1307);
        var rank = new Dictionary<Vector2Int, int>();
        foreach (var room in layout.rooms) rank[room.coord] = random.Next();
        int OuterScore(Vector2Int c) => Mathf.Abs(c.x - center.x) + Mathf.Abs(c.y - center.y);
        int EdgeScore(Vector2Int c) => c.x == 0 || c.y == 0 || c.x == size - 1 || c.y == size - 1 ? 1 : 0;
        var ordered = new List<StageRoomNode>(layout.rooms);
        ordered.RemoveAll(room => room.coord == center);
        ordered.Sort((a, b) =>
        {
            int compare = EdgeScore(b.coord).CompareTo(EdgeScore(a.coord));
            if (compare == 0) compare = OuterScore(b.coord).CompareTo(OuterScore(a.coord));
            return compare != 0 ? compare : rank[a.coord].CompareTo(rank[b.coord]);
        });
        bool MakeLeaf(StageRoomNode room)
        {
            if (CountConnections(room) == 1) return true;
            var original = room.connections;
            for (int keep = 0; keep < 4; keep++)
            {
                if ((original & Flags[keep]) == 0) continue;
                for (int i = 0; i < 4; i++)
                    if (i != keep && (original & Flags[i]) != 0)
                    { room.connections &= ~Flags[i]; nodes[room.coord + Steps[i]].connections &= ~Flags[i ^ 1]; }
                if (RaidMapGenerator.GetDistances(layout, center).Count == nodes.Count) return true;
                room.connections = original;
                for (int i = 0; i < 4; i++) if ((original & Flags[i]) != 0)
                    nodes[room.coord + Steps[i]].connections |= Flags[i ^ 1];
            }
            return false;
        }
        foreach (var room in ordered)
        {
            if (MakeLeaf(room)) selected.Add(room);
            if (selected.Count == desired) break;
        }
        // Braiding can remove all original branch tips. Add a true side branch only when necessary.
        while (selected.Count < desired && nodes.Count < size * size &&
            (rules.maximumRoomCount == 0 || nodes.Count < rules.maximumRoomCount))
        {
            var branches = new List<(StageRoomNode parent, int direction)>();
            foreach (var parent in layout.rooms)
            {
                if (selected.Contains(parent)) continue;
                for (int i = 0; i < 4; i++)
                {
                    var coord = parent.coord + Steps[i];
                    if (coord.x >= 0 && coord.y >= 0 && coord.x < size && coord.y < size && !nodes.ContainsKey(coord))
                        branches.Add((parent, i));
                }
            }
            if (branches.Count == 0) break;
            branches.Sort((a, b) =>
            {
                var ac = a.parent.coord + Steps[a.direction]; var bc = b.parent.coord + Steps[b.direction];
                int compare = EdgeScore(bc).CompareTo(EdgeScore(ac));
                return compare != 0 ? compare : OuterScore(bc).CompareTo(OuterScore(ac));
            });
            var edge = branches[0];
            var child = new StageRoomNode(edge.parent.coord + Steps[edge.direction], StageRoomType.Normal, Flags[edge.direction ^ 1])
                { raidRole = RaidRoomRole.Normal };
            edge.parent.connections |= Flags[edge.direction];
            nodes.Add(child.coord, child); layout.rooms.Add(child); selected.Add(child);
        }
        if (selected.Count < 2 || (!layout.compactTestLayout && selected.Count < desired))
        { error = $"발전실·탈출구용 가지 끝 방이 부족합니다. 필요 {desired}개 / 확보 {selected.Count}개. 탈출구 수를 줄이거나 맵 크기·최대 방 수를 늘려주세요."; return false; }
        // Keep location selection separate from the boss probability/content RNG stream.
        for (int i = selected.Count - 1; i > 0; i--)
        { int j = random.Next(i + 1); var temp = selected[i]; selected[i] = selected[j]; selected[j] = temp; }
        var bossRooms = layout.rooms.FindAll(room => room.coord != center && !selected.Contains(room));
        var bossRandom = Rng(layout.stageSeed, 1309);
        var boss = bossRooms[bossRandom.Next(bossRooms.Count)];
        layout.bossSpawned = bossRandom.Next(100) < settings.bossSpawnPercent;
        if (layout.bossSpawned && settings.preventBossBlockingExtraction)
        {
            bool ExitsReachable(HashSet<Vector2Int> reached)
            {
                for (int i = 1; i < selected.Count; i++) if (!reached.Contains(selected[i].coord)) return false;
                return true;
            }
            if (!ExitsReachable(ReachableWithoutRoom(nodes, center, boss.coord)))
            {
                var safe = new List<StageRoomNode>();
                var alternatives = new List<(StageRoomNode room, int generatorIndex)>();
                foreach (var candidate in bossRooms)
                {
                    var reached = ReachableWithoutRoom(nodes, center, candidate.coord);
                    if (ExitsReachable(reached)) { safe.Add(candidate); continue; }
                    // Tiny test maps may only have one boss cell. Swap which terminal is power,
                    // never remove an exit or change the graph merely to permit a boss.
                    int unreachable = -1, count = 0;
                    for (int i = 0; i < selected.Count; i++) if (!reached.Contains(selected[i].coord))
                    { unreachable = i; count++; }
                    if (count == 1) alternatives.Add((candidate, unreachable));
                }
                var placementRandom = Rng(layout.stageSeed, 1310);
                if (safe.Count > 0) boss = safe[placementRandom.Next(safe.Count)];
                else if (alternatives.Count > 0)
                {
                    var pick = alternatives[placementRandom.Next(alternatives.Count)]; boss = pick.room;
                    var previous = selected[0]; selected[0] = selected[pick.generatorIndex]; selected[pick.generatorIndex] = previous;
                }
                else
                {
                    error = "보스가 탈출 경로를 막지 않는 배치가 없습니다. 맵 크기·방 수를 늘리거나 외곽 탈출구 수를 줄여주세요. 테스트하려면 '보스가 탈출 경로 막지 않기'를 OFF할 수 있습니다.";
                    return false;
                }
            }
        }
        selected[0].raidRole = RaidRoomRole.Generator;
        layout.generatorRoomCoord = selected[0].coord;
        foreach (var room in selected) layout.extractionCandidates.Add(room.coord);
        for (int i = 1; i < selected.Count; i++) layout.extractionRooms.Add(selected[i].coord);
        layout.extractionSectorCoord = layout.extractionRooms[0];
        boss.raidRole = RaidRoomRole.Boss;
        layout.bossRoomCoord = layout.goalRoomCoord = boss.coord;
        if (layout.bossSpawned) { boss.roomType = StageRoomType.Boss; boss.combatPoolId = "theme"; }
        var start = nodes[center]; start.raidRole = RaidRoomRole.Hatch;
        start.isOpened = start.isRevealed = start.isCleared = true;
        layout.hatchRoomCoord = center;
        return true;
    }

    private static HashSet<Vector2Int> ReachableWithoutRoom(Dictionary<Vector2Int, StageRoomNode> nodes,
        Vector2Int start, Vector2Int blocked)
    {
        var reached = new HashSet<Vector2Int>();
        if (start == blocked || !nodes.ContainsKey(start)) return reached;
        var queue = new Queue<Vector2Int>(); reached.Add(start); queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var coord = queue.Dequeue(); var room = nodes[coord];
            for (int i = 0; i < 4; i++)
            {
                var next = coord + Steps[i];
                if (next == blocked || !room.HasConnection(Flags[i]) || !nodes.TryGetValue(next, out var other) ||
                    !other.HasConnection(Flags[i ^ 1]) || !reached.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        return reached;
    }

    private static bool TryValidateBranchLandmarks(StageMapLayout layout, out string error)
    {
        error = "저장 맵의 가지 끝 배치·역할·연결 정보가 올바르지 않습니다.";
        if (!layout.usesRaidLayout || layout.usesBranchTopology || layout.generationVersion != GenerationVersion ||
            layout.gridSize < 1 || layout.gridSize > 64 || layout.compactTestLayout != (layout.gridSize < 8) ||
            layout.rooms == null || layout.rooms.Count < 1 || layout.rooms.Count > layout.gridSize * layout.gridSize ||
            !layout.hasExtraction || layout.extractionCandidates == null || layout.extractionRooms == null || layout.extractionRooms.Count < 1)
            return false;
        if (layout.gridSize == 1) return RaidMerchantPlacement.TryValidate(layout, out error) && TryValidateCompact(layout, out error);
        var center = new Vector2Int(layout.gridSize / 2, layout.gridSize / 2);
        if (layout.startSectorCoord != center || layout.firstRoomCoord != center || layout.hatchRoomCoord != center) return false;
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
        bool Role(Vector2Int c, RaidRoomRole role) => nodes.TryGetValue(c, out var room) && room.raidRole == role;
        var reserved = new HashSet<Vector2Int> { center };
        var candidates = new HashSet<Vector2Int>();
        foreach (var c in layout.extractionCandidates)
            if (!nodes.TryGetValue(c, out var room) || !candidates.Add(c) || CountConnections(room) != 1) return false;
        if (!Role(center, RaidRoomRole.Hatch) || !Role(layout.generatorRoomCoord, RaidRoomRole.Generator) ||
            !Role(layout.bossRoomCoord, RaidRoomRole.Boss) || !reserved.Add(layout.generatorRoomCoord) || !reserved.Add(layout.bossRoomCoord) ||
            !candidates.Contains(layout.generatorRoomCoord) || candidates.Contains(layout.bossRoomCoord) || candidates.Contains(center) ||
            layout.goalRoomCoord != layout.bossRoomCoord || candidates.Count != layout.extractionRooms.Count + 1 ||
            (nodes[layout.bossRoomCoord].roomType == StageRoomType.Boss) != layout.bossSpawned ||
            (layout.bossSpawned && nodes[layout.bossRoomCoord].combatPoolId != "theme" &&
             !(layout.bossMayUseSpecialRoom&&layout.stageIndex==1&&nodes[layout.bossRoomCoord].specialSectorPoolId is "office" or "storage" or "warehouse"))) return false;
        foreach (var c in layout.extractionRooms)
            if (!candidates.Contains(c) || !reserved.Add(c) || !Role(c, RaidRoomRole.Normal)) return false;
        foreach (var role in new[] { RaidRoomRole.Hatch, RaidRoomRole.Generator, RaidRoomRole.Boss })
            if (layout.rooms.FindAll(room => room.raidRole == role).Count != 1) return false;
        if (layout.rooms.FindAll(room => room.roomType == StageRoomType.Boss).Count != (layout.bossSpawned ? 1 : 0) ||
            layout.extractionSectorCoord != layout.extractionRooms[0] || layout.extractionPortalVersion != 1) return false;
        if (layout.bossSpawned && layout.bossAvoidsExtractionRoutes)
        {
            var reached = ReachableWithoutRoom(nodes, center, layout.bossRoomCoord);
            foreach (var exit in layout.extractionRooms) if (!reached.Contains(exit))
            { error = $"보스방을 통과하지 않으면 외곽 탈출구 {exit}에 도달할 수 없습니다."; return false; }
        }
        return RaidMerchantPlacement.TryValidate(layout, out error) && RaidExtractionPortalPolicy.TryValidate(layout, out error);
    }
}
