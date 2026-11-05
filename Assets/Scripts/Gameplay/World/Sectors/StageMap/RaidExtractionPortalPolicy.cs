using System.Collections.Generic;
using UnityEngine;

/// <summary>Exit metadata on ordinary rooms. Never adds a room, portal or travel connection.</summary>
public static class RaidExtractionPortalPolicy
{
    private static readonly SectorPortalDirection[] Directions = { SectorPortalDirection.XMin,
        SectorPortalDirection.XMax, SectorPortalDirection.ZMin, SectorPortalDirection.ZMax };
    private static readonly StageRoomConnectionFlags[] Flags = { StageRoomConnectionFlags.Left,
        StageRoomConnectionFlags.Right, StageRoomConnectionFlags.Down, StageRoomConnectionFlags.Up };

    public static bool TryGetUnusedDirection(StageRoomNode room, int seed, out SectorPortalDirection direction)
    {
        direction = default;
        if (room == null) return false;
        int start = (int)((uint)StageDefinitionSO.BuildSectorSeed(seed, room.coord, 1709) % 4);
        for (int offset = 0; offset < 4; offset++)
        {
            int index = (start + offset) % 4;
            if (room.HasConnection(Flags[index])) continue;
            direction = Directions[index]; return true;
        }
        return false;
    }

    // Called only on a newly generated graph or the validated, in-memory COPY of an old save.
    // Old four-way exit rooms cannot sacrifice a travel door: relocate only the exit marker.
    public static bool TryPrepare(StageMapLayout layout, out string error)
    {
        if (layout == null || !layout.usesRaidLayout || !layout.hasExtraction)
        { error = "Raid extraction layout is missing."; return false; }
        if (layout.extractionPortalVersion != 0) return TryValidate(layout, out error);
        if (layout.rooms == null || layout.generationVersion == 3 &&
            (layout.extractionRooms == null || layout.extractionRooms.Count == 0 || layout.extractionCandidates == null))
        { error = "Raid extraction owners are missing."; return false; }
        var owners = layout.generationVersion == 3 ? new List<Vector2Int>(layout.extractionRooms)
            : new List<Vector2Int> { layout.extractionSectorCoord };
        var routes = new List<RaidExtractionPortalRoute>();
        var used = new HashSet<Vector2Int>();
        if (owners.Count == 0 || layout.generationVersion == 3 &&
            owners.Exists(coord => !layout.extractionCandidates.Contains(coord)))
        { error = "Raid exit is not in the saved candidate list."; return false; }
        foreach (var original in owners)
        {
            layout.TryGetRoom(original, out var chosen);
            if (!CanHost(chosen) || !TryGetUnusedDirection(chosen, layout.stageSeed, out _))
            {
                chosen = null;
                int best = int.MaxValue;
                foreach (var room in layout.rooms)
                {
                    if (!CanHostLegacyReplacement(room) || used.Contains(room.coord) || owners.Contains(room.coord) ||
                        !TryGetUnusedDirection(room, layout.stageSeed, out _)) continue;
                    // Legacy quadrant constraints and all graph connections remain intact.
                    if (layout.generationVersion < 3 && !SameQuadrant(original, room.coord, layout.gridSize / 2)) continue;
                    int distance = Mathf.Abs(original.x - room.coord.x) + Mathf.Abs(original.y - room.coord.y) +
                        (CanHost(room) ? 0 : 10000);
                    if (distance < best || (distance == best && chosen != null &&
                        (room.coord.y < chosen.coord.y || room.coord.y == chosen.coord.y && room.coord.x < chosen.coord.x)))
                    { chosen = room; best = distance; }
                }
            }
            if (chosen == null || !used.Add(chosen.coord) || !TryGetUnusedDirection(chosen, layout.stageSeed, out var direction))
            { error = "No ordinary room has a spare portal for extraction; travel links were not overwritten."; return false; }
            routes.Add(new RaidExtractionPortalRoute { coord = chosen.coord, direction = direction });
        }
        for (int i = 0; i < routes.Count; i++)
        {
            if (layout.generationVersion == 3 && owners[i] != routes[i].coord &&
                !layout.extractionCandidates.Contains(routes[i].coord))
                layout.extractionCandidates[layout.extractionCandidates.IndexOf(owners[i])] = routes[i].coord;
            layout.TryGetRoom(routes[i].coord, out var owner);
            owner.raidRole = RaidRoomRole.Normal;
        }
        foreach (var room in layout.rooms)
            if (room.raidRole == RaidRoomRole.Extraction) room.raidRole = RaidRoomRole.Normal;
        if (layout.generationVersion == 3)
            layout.extractionRooms = routes.ConvertAll(route => route.coord);
        layout.extractionSectorCoord = routes[0].coord;
        layout.extractionDirection = routes[0].direction;
        layout.extractionPortals = routes;
        layout.extractionPortalVersion = 1;
        return TryValidate(layout, out error);
    }

    public static bool TryValidate(StageMapLayout layout, out string error)
    {
        error = "Invalid raid extraction portal routes.";
        if (layout == null || !layout.usesRaidLayout || !layout.hasExtraction || layout.rooms == null ||
            layout.generationVersion == 3 && layout.extractionRooms == null ||
            layout.extractionPortalVersion != 1 || layout.extractionPortals == null || layout.extractionPortals.Count == 0 ||
            layout.extractionPortals.Count != (layout.generationVersion == 3 ? layout.extractionRooms.Count : 1)) return false;
        var coords = new HashSet<Vector2Int>();
        foreach (var route in layout.extractionPortals)
        {
            if (route == null || !coords.Add(route.coord) || !layout.IsExtractionSector(route.coord) ||
                !layout.TryGetRoom(route.coord, out var room) || room.roomType != StageRoomType.Normal ||
                room.raidRole != RaidRoomRole.Normal) return false;
            int index = System.Array.IndexOf(Directions, route.direction);
            if (index < 0 || room.HasConnection(Flags[index])) return false;
        }
        if (layout.rooms.Exists(room => room == null || room.raidRole == RaidRoomRole.Extraction)) return false;
        error = string.Empty; return true;
    }

    private static bool CanHost(StageRoomNode room) => room != null && room.roomType == StageRoomType.Normal &&
        room.raidRole is RaidRoomRole.Normal or RaidRoomRole.Extraction;
    private static bool CanHostLegacyReplacement(StageRoomNode room) => CanHost(room) ||
        room != null && room.roomType == StageRoomType.Normal &&
        room.raidRole is RaidRoomRole.Armory or RaidRoomRole.Medical or RaidRoomRole.Vault;
    private static bool SameQuadrant(Vector2Int a, Vector2Int b, int center) =>
        (a.x - center) * (b.x - center) > 0 && (a.y - center) * (b.y - center) > 0;
}
