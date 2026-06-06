using UnityEngine;

public static class StageMapGenerator
{
    public static StageMapLayout GenerateFullGrid(
        int runSeed,
        int stageIndex,
        int gridSize,
        Vector2Int startSectorCoord,
        StageRoomType goalRoomType)
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
                    isGoalRoom ? goalRoomType : StageRoomType.NormalBattle,
                    BuildFullGridConnections(coord, gridSize))
                {
                    // 0,0 stays a normal room, but it opens first so StartSector can enter it.
                    isOpened = coord == layout.firstRoomCoord
                };

                layout.rooms.Add(node);
            }
        }

        return layout;
    }

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
}
