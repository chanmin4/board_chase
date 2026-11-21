using System.Collections.Generic;
using UnityEngine;

public static class RaidMapValidation
{
    public static bool TryValidate(StageMapLayout layout, out string error)
    {
        if (layout?.landmarkSchemaVersion > 0)
            return LandmarkMapGenerator.TryValidate(layout, out error);
        if (layout?.generationVersion == RadialBraidedRaidGenerator.GenerationVersion)
            return RadialBraidedRaidGenerator.TryValidate(layout, out error);
        error = string.Empty;
        if (layout == null || !layout.usesRaidLayout || layout.gridSize < 5 || (layout.gridSize & 1) == 0 ||
            layout.rooms == null || layout.usesBranchTopology || !layout.hasExtraction ||
            !RaidMapGenerator.IsSupportedGenerationVersion(layout.generationVersion))
            return Fail("Invalid odd-square raid header.", out error);
        int size = layout.gridSize;
        int middle = size / 2;
        var center = new Vector2Int(middle, middle);
        Vector2Int first = layout.firstRoomCoord;
        Vector2Int start = layout.startSectorCoord;
        bool firstIsCorner = (first.x == 0 || first.x == size - 1) &&
                             (first.y == 0 || first.y == size - 1);
        bool externalStart = start != first;
        bool startInBounds = InBounds(start, size);
        if (!firstIsCorner || (externalStart && (startInBounds ||
            Mathf.Abs(start.x - first.x) + Mathf.Abs(start.y - first.y) != 1)))
            return Fail("Raid start must be the entry corner or an adjacent external hub.", out error);
        if (layout.generationVersion == 1 && (first != Vector2Int.zero ||
            (start != Vector2Int.zero && start != Vector2Int.left && start != Vector2Int.down)))
            return Fail("Version 1 raids must retain their original bottom-left entry.", out error);

        int startX = first.x < middle ? -1 : 1;
        int startY = first.y < middle ? -1 : 1;
        Vector2Int generator = layout.generatorRoomCoord;
        bool generatorInQuadrant = InQuadrant(generator, startX, -startY, middle, size) ||
                                   InQuadrant(generator, -startX, startY, middle, size);
        if (layout.hatchRoomCoord != center || !generatorInQuadrant ||
            !InQuadrant(layout.bossRoomCoord, -startX, -startY, middle, size) ||
            !InQuadrant(layout.extractionSectorCoord, -startX, -startY, middle, size) ||
            layout.bossRoomCoord == layout.extractionSectorCoord)
            return Fail("Special rooms overlap or fall outside their required quadrants.", out error);
        int expected = size * size + (externalStart ? 1 : 0);
        if (layout.rooms.Count != expected) return Fail("Saved raid room count is invalid.", out error);
        var coords = new HashSet<Vector2Int>();
        foreach (StageRoomNode room in layout.rooms)
        {
            if (room == null || !coords.Add(room.coord) ||
                (room.coord != layout.startSectorCoord &&
                 (room.coord.x < 0 || room.coord.y < 0 || room.coord.x >= size || room.coord.y >= size)))
                return Fail("Duplicate, null or out-of-bounds raid room.", out error);
            if ((room.roomType == StageRoomType.Start) != (room.coord == start))
                return Fail("Raid must have exactly one Start room at the saved hub coordinate.", out error);
        }
        if (!layout.TryGetRoom(start, out _) || !layout.TryGetRoom(first, out StageRoomNode entry) ||
            entry.raidRole != RaidRoomRole.Normal)
            return Fail("Raid entry room is missing or has a reserved role.", out error);
        if (!HasRole(layout, center, RaidRoomRole.Hatch) ||
            !HasRole(layout, generator, RaidRoomRole.Generator) ||
            !HasRole(layout, layout.bossRoomCoord, RaidRoomRole.Boss) ||
            !HasRole(layout, layout.extractionSectorCoord,
                layout.extractionPortalVersion == 0 ? RaidRoomRole.Extraction : RaidRoomRole.Normal))
            return Fail("Reserved raid room role is missing.", out error);
        if (!layout.TryGetRoom(layout.bossRoomCoord, out StageRoomNode boss) ||
            (boss.roomType == StageRoomType.Boss) != layout.bossSpawned)
            return Fail("Boss spawn result disagrees with the stored room.", out error);
        if (RaidMapGenerator.GetDistances(layout, layout.startSectorCoord).Count != expected)
            return Fail("A raid room cannot be reached from the start.", out error);
        if (layout.extractionPortalVersion != 0 && !RaidExtractionPortalPolicy.TryValidate(layout, out error)) return false;
        return true;
    }

    private static bool HasRole(StageMapLayout layout, Vector2Int coord, RaidRoomRole role) =>
        layout.TryGetRoom(coord, out StageRoomNode room) && room.raidRole == role;
    private static bool InBounds(Vector2Int coord, int size) =>
        coord.x >= 0 && coord.y >= 0 && coord.x < size && coord.y < size;
    private static bool InQuadrant(Vector2Int coord, int xSign, int ySign, int middle, int size) =>
        InBounds(coord, size) && (coord.x - middle) * xSign > 0 && (coord.y - middle) * ySign > 0;
    private static bool Fail(string reason, out string error) { error = reason; return false; }
}
