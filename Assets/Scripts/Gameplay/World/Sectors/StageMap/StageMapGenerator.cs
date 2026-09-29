/*
 * Role hierarchy: Gameplay > World > Sectors > StageMap
 * Primary responsibility: Manages StageMapGenerator stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

public static class StageMapGenerator
{
    /// <summary>
    /// Generates an authored sparse graph. Connections are derived only from
    /// orthogonally adjacent authored rooms, so empty cells never receive an
    /// accidental portal link.
    /// </summary>
    public static bool TryGenerateBranchTopology(
        int runSeed,
        int stageIndex,
        Vector2Int startSectorCoord,
        StageRoomType goalRoomType,
        StageTopologySO topology,
        out StageMapLayout layout,
        out string error)
    {
        layout = null;

        if (topology == null)
        {
            error = "StageTopologySO is missing.";
            return false;
        }

        if (!topology.TryValidate(out error))
            return false;

        Vector2Int entryCoord = topology.EntryRoomCoord;
        if (Mathf.Abs(entryCoord.x - startSectorCoord.x) +
            Mathf.Abs(entryCoord.y - startSectorCoord.y) != 1)
        {
            error = $"Topology entry {entryCoord} must be orthogonally adjacent to generated Start Sector {startSectorCoord}.";
            return false;
        }

        if (!topology.TryGetGoalRoomCoord(out Vector2Int goalCoord))
        {
            error = "Topology has no Boss goal room.";
            return false;
        }

        HashSet<Vector2Int> roomCoords = new();
        IReadOnlyList<StageTopologyRoom> authoredRooms = topology.Rooms;
        for (int i = 0; i < authoredRooms.Count; i++)
        {
            StageTopologyRoom authoredRoom = authoredRooms[i];
            if (authoredRoom == null || !roomCoords.Add(authoredRoom.coord))
            {
                error = "Topology room data changed while it was being generated.";
                return false;
            }
        }

        if (roomCoords.Contains(startSectorCoord))
        {
            error = $"Topology cannot occupy Start Sector coordinate {startSectorCoord}.";
            return false;
        }

        roomCoords.Add(startSectorCoord);

        layout = new StageMapLayout
        {
            runSeed = runSeed,
            stageIndex = stageIndex,
            stageSeed = BuildStageSeed(runSeed, stageIndex),
            // Existing consumers use gridSize only as a generated-map flag.
            gridSize = Mathf.Max(1, topology.RoomCount),
            usesBranchTopology = true,
            branchTopologyName = topology.name,
            startSectorCoord = startSectorCoord,
            firstRoomCoord = entryCoord,
            goalRoomCoord = goalCoord
        };

        layout.rooms.Add(new StageRoomNode(
            startSectorCoord,
            StageRoomType.Start,
            BuildBranchConnections(startSectorCoord, roomCoords))
        {
            isOpened = true,
            isRevealed = true
        });

        for (int i = 0; i < authoredRooms.Count; i++)
        {
            StageTopologyRoom authoredRoom = authoredRooms[i];
            StageRoomType roomType = authoredRoom.coord == goalCoord
                ? goalRoomType
                : StageRoomType.Normal;

            layout.rooms.Add(new StageRoomNode(
                authoredRoom.coord,
                roomType,
                BuildBranchConnections(authoredRoom.coord, roomCoords))
            {
                isOpened = authoredRoom.coord == entryCoord
            });
        }

        AssignSingleOuterExtraction(layout);

        error = string.Empty;
        return true;
    }

    // Purpose: Generates full grid from the authored data.
    public static StageMapLayout GenerateFullGrid(
        int runSeed,
        int stageIndex,
        int gridSize,
        Vector2Int startSectorCoord,
        StageRoomType goalRoomType)
    {
        return GenerateFullGrid(
            runSeed,
            stageIndex,
            gridSize,
            startSectorCoord,
            goalRoomType,
            StageTreasureRoomGenerationSettings.Disabled,
            StageShopRoomGenerationSettings.Disabled);
    }

    // Purpose: Generates full grid from the authored data.
    public static StageMapLayout GenerateFullGrid(
        int runSeed,
        int stageIndex,
        int gridSize,
        Vector2Int startSectorCoord,
        StageRoomType goalRoomType,
        StageTreasureRoomGenerationSettings treasureSettings)
    {
        return GenerateFullGrid(
            runSeed,
            stageIndex,
            gridSize,
            startSectorCoord,
            goalRoomType,
            treasureSettings,
            StageShopRoomGenerationSettings.Disabled);
    }

    // Purpose: Generates full grid from the authored data.
    public static StageMapLayout GenerateFullGrid(
        int runSeed,
        int stageIndex,
        int gridSize,
        Vector2Int startSectorCoord,
        StageRoomType goalRoomType,
        StageTreasureRoomGenerationSettings treasureSettings,
        StageShopRoomGenerationSettings shopSettings)
    {
        gridSize = Mathf.Max(0, gridSize);

        StageMapLayout layout = new StageMapLayout
        {
            runSeed = runSeed,
            stageIndex = stageIndex,
            stageSeed = BuildStageSeed(runSeed, stageIndex),
            gridSize = gridSize,
            startSectorCoord = startSectorCoord,
            firstRoomCoord = Vector2Int.zero,
            goalRoomCoord = gridSize > 0
                ? new Vector2Int(gridSize - 1, gridSize - 1)
                : startSectorCoord
        };

        StageRoomNode startNode = new StageRoomNode(
            startSectorCoord,
            StageRoomType.Start,
            gridSize > 0 ? StageRoomConnectionFlags.Right : StageRoomConnectionFlags.None)
        {
            isOpened = true,
            isRevealed = true
        };

        layout.rooms.Add(startNode);

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                bool isGoalRoom = coord == layout.goalRoomCoord;

                StageRoomNode node = new StageRoomNode(
                    coord,
                    isGoalRoom ? goalRoomType : StageRoomType.Normal,
                    BuildFullGridConnections(coord, gridSize))
                {
                    // 0,0 stays a normal room, but it opens first so StartSector can enter it.
                    isOpened = coord == layout.firstRoomCoord
                };

                layout.rooms.Add(node);
            }
        }

        // WIP legacy path: Treasure rooms were replaced by loot-content slots
        // inside Normal Sector rooms. Keep the data type and helper below for
        // serialized-asset migration, but never change a generated room into a
        // dedicated Treasure room again.
        // ApplyTreasureRooms(layout, treasureSettings);
        // Field merchants are intentionally not generated. Shopping and meta
        // progression belong to the reusable Start hub instead of run sectors.

        AssignSingleOuterExtraction(layout);

        return layout;
    }

    /// <summary>
    /// Selects one known early-extraction route. The route always uses the top
    /// or right outer edge so the Start corner cannot receive a trivial exit.
    /// Boss and Start rooms never own this portal. If a sparse topology has no
    /// eligible room on the initially selected side, the other side is used.
    /// </summary>
    private static void AssignSingleOuterExtraction(StageMapLayout layout)
    {
        if (layout == null || layout.rooms == null || layout.rooms.Count == 0)
            return;

        System.Random random = new System.Random(BuildSaltedSeed(layout.stageSeed, 3101));
        bool preferTopSide = random.Next(0, 2) == 0;

        if (TryAssignOuterExtraction(layout, preferTopSide, random))
            return;

        TryAssignOuterExtraction(layout, !preferTopSide, random);
    }

    // Purpose: Attempts to assign the single extraction portal on one requested outer map side.
    private static bool TryAssignOuterExtraction(
        StageMapLayout layout,
        bool topSide,
        System.Random random)
    {
        int outerCoordinate = int.MinValue;

        for (int i = 0; i < layout.rooms.Count; i++)
        {
            StageRoomNode room = layout.rooms[i];
            if (room == null || room.roomType == StageRoomType.Empty)
                continue;

            int coordinate = topSide ? room.coord.y : room.coord.x;
            if (coordinate > outerCoordinate)
                outerCoordinate = coordinate;
        }

        if (outerCoordinate == int.MinValue)
            return false;

        List<StageRoomNode> candidates = new();

        for (int i = 0; i < layout.rooms.Count; i++)
        {
            StageRoomNode room = layout.rooms[i];
            if (room == null ||
                room.roomType == StageRoomType.Empty ||
                room.roomType == StageRoomType.Boss ||
                room.roomType == StageRoomType.Start)
            {
                continue;
            }

            int coordinate = topSide ? room.coord.y : room.coord.x;
            if (coordinate == outerCoordinate)
                candidates.Add(room);
        }

        if (candidates.Count == 0)
            return false;

        StageRoomNode selected = candidates[random.Next(0, candidates.Count)];
        layout.hasExtraction = true;
        layout.extractionSectorCoord = selected.coord;
        layout.extractionDirection = topSide
            ? SectorPortalDirection.ZMax
            : SectorPortalDirection.XMax;
        return true;
    }

    // Purpose: Builds the branch connections from the current authored data.
    private static StageRoomConnectionFlags BuildBranchConnections(
        Vector2Int coord,
        ISet<Vector2Int> roomCoords)
    {
        StageRoomConnectionFlags flags = StageRoomConnectionFlags.None;

        if (roomCoords.Contains(coord + Vector2Int.left))
            flags |= StageRoomConnectionFlags.Left;
        if (roomCoords.Contains(coord + Vector2Int.right))
            flags |= StageRoomConnectionFlags.Right;
        if (roomCoords.Contains(coord + Vector2Int.down))
            flags |= StageRoomConnectionFlags.Down;
        if (roomCoords.Contains(coord + Vector2Int.up))
            flags |= StageRoomConnectionFlags.Up;

        return flags;
    }

    // Purpose: Builds the stage seed from the current authored data.
    public static int BuildStageSeed(int runSeed, int stageIndex)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + runSeed;
            hash = hash * 31 + stageIndex;
            return hash;
        }
    }

    // Purpose: Builds the full grid connections from the current authored data.
    private static StageRoomConnectionFlags BuildFullGridConnections(
        Vector2Int coord,
        int gridSize)
    {
        StageRoomConnectionFlags flags = StageRoomConnectionFlags.None;

        if (coord.x > 0)
            flags |= StageRoomConnectionFlags.Left;
        else if (coord == Vector2Int.zero)
            flags |= StageRoomConnectionFlags.Left;

        if (coord.x < gridSize - 1)
            flags |= StageRoomConnectionFlags.Right;

        if (coord.y > 0)
            flags |= StageRoomConnectionFlags.Down;

        if (coord.y < gridSize - 1)
            flags |= StageRoomConnectionFlags.Up;

        return flags;
    }

    // WIP legacy migration helper. Intentionally not called by generated maps.
    // Purpose: Applies the requested treasure rooms change.
    private static void ApplyTreasureRooms(
        StageMapLayout layout,
        StageTreasureRoomGenerationSettings settings)
    {
        if (layout == null || !settings.Enabled || layout.gridSize <= 0)
            return;

        System.Random countRng = new System.Random(BuildSaltedSeed(layout.stageSeed, 1301));
        int treasureCount = settings.RollTreasureRoomCount(layout.stageIndex, countRng);

        if (treasureCount <= 0)
            return;

        StageRoomNode[] candidates = BuildTreasureCandidates(
            layout,
            settings.ExcludeFirstRoom);

        if (candidates.Length < treasureCount && settings.ExcludeFirstRoom)
            candidates = BuildTreasureCandidates(layout, excludeFirstRoom: false);

        if (candidates.Length <= 0)
            return;

        treasureCount = Mathf.Min(treasureCount, candidates.Length);
        System.Random pickRng = new System.Random(BuildSaltedSeed(layout.stageSeed, 1303));

        for (int i = 0; i < treasureCount; i++)
        {
            int pickedIndex = pickRng.Next(i, candidates.Length);
            StageRoomNode picked = candidates[pickedIndex];
            candidates[pickedIndex] = candidates[i];
            candidates[i] = picked;

            candidates[i].roomType = StageRoomType.Treasure;
        }
    }

    // Purpose: Builds the treasure candidates from the current authored data.
    private static StageRoomNode[] BuildTreasureCandidates(
        StageMapLayout layout,
        bool excludeFirstRoom)
    {
        int count = 0;

        for (int i = 0; i < layout.rooms.Count; i++)
        {
            if (IsTreasureCandidate(layout, layout.rooms[i], excludeFirstRoom))
                count++;
        }

        StageRoomNode[] candidates = new StageRoomNode[count];
        int cursor = 0;

        for (int i = 0; i < layout.rooms.Count; i++)
        {
            StageRoomNode room = layout.rooms[i];

            if (IsTreasureCandidate(layout, room, excludeFirstRoom))
                candidates[cursor++] = room;
        }

        return candidates;
    }

    // Purpose: Checks whether treasure candidate is currently true.
    private static bool IsTreasureCandidate(
        StageMapLayout layout,
        StageRoomNode room,
        bool excludeFirstRoom)
    {
        if (layout == null || room == null)
            return false;

        if (room.roomType != StageRoomType.Normal)
            return false;

        if (room.coord == layout.goalRoomCoord)
            return false;

        if (excludeFirstRoom && room.coord == layout.firstRoomCoord)
            return false;

        return true;
    }

    // Purpose: Applies the requested shop rooms change.
    private static void ApplyShopRooms(
        StageMapLayout layout,
        StageShopRoomGenerationSettings settings)
    {
        if (layout == null || !settings.Enabled || layout.gridSize <= 0)
            return;

        System.Random countRng = new System.Random(BuildSaltedSeed(layout.stageSeed, 2301));
        int shopCount = settings.RollShopRoomCount(countRng);

        if (shopCount <= 0)
            return;

        StageRoomNode[] candidates = BuildSpecialRoomCandidates(
            layout,
            settings.ExcludeFirstRoom);

        if (candidates.Length < shopCount && settings.ExcludeFirstRoom)
            candidates = BuildSpecialRoomCandidates(layout, excludeFirstRoom: false);

        if (candidates.Length <= 0)
            return;

        shopCount = Mathf.Min(shopCount, candidates.Length);
        System.Random pickRng = new System.Random(BuildSaltedSeed(layout.stageSeed, 2303));

        for (int i = 0; i < shopCount; i++)
        {
            int pickedIndex = pickRng.Next(i, candidates.Length);
            StageRoomNode picked = candidates[pickedIndex];
            candidates[pickedIndex] = candidates[i];
            candidates[i] = picked;

            candidates[i].roomType = StageRoomType.Shop;
        }
    }

    // Purpose: Builds the special room candidates from the current authored data.
    private static StageRoomNode[] BuildSpecialRoomCandidates(
        StageMapLayout layout,
        bool excludeFirstRoom)
    {
        int count = 0;

        for (int i = 0; i < layout.rooms.Count; i++)
        {
            if (IsSpecialRoomCandidate(layout, layout.rooms[i], excludeFirstRoom))
                count++;
        }

        StageRoomNode[] candidates = new StageRoomNode[count];
        int cursor = 0;

        for (int i = 0; i < layout.rooms.Count; i++)
        {
            StageRoomNode room = layout.rooms[i];

            if (IsSpecialRoomCandidate(layout, room, excludeFirstRoom))
                candidates[cursor++] = room;
        }

        return candidates;
    }

    // Purpose: Checks whether special room candidate is currently true.
    private static bool IsSpecialRoomCandidate(
        StageMapLayout layout,
        StageRoomNode room,
        bool excludeFirstRoom)
    {
        if (layout == null || room == null)
            return false;

        if (room.roomType != StageRoomType.Normal)
            return false;

        if (room.coord == layout.goalRoomCoord)
            return false;

        if (excludeFirstRoom && room.coord == layout.firstRoomCoord)
            return false;

        return true;
    }

    // Purpose: Builds the salted seed from the current authored data.
    private static int BuildSaltedSeed(int baseSeed, int salt)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + baseSeed;
            hash = hash * 31 + salt;
            return hash;
        }
    }
}
