using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageMapLayout
{
    public int runSeed;
    public int stageIndex;
    public int stageSeed;
    public int gridSize;
    public Vector2Int startSectorCoord;
    public Vector2Int firstRoomCoord;
    public Vector2Int goalRoomCoord;
    public List<StageRoomNode> rooms = new();

    public bool HasGeneratedRooms => gridSize > 0;

    public bool TryGetRoom(Vector2Int coord, out StageRoomNode room)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            StageRoomNode candidate = rooms[i];

            if (candidate != null && candidate.coord == coord)
            {
                room = candidate;
                return true;
            }
        }

        room = null;
        return false;
    }

    public bool TryGetGoalRoom(out StageRoomNode room)
    {
        return TryGetRoom(goalRoomCoord, out room);
    }
}
