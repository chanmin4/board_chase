/*
 * Role hierarchy: Gameplay > World > Sectors > StageMap
 * Primary responsibility: Manages StageRoomNode stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public class StageRoomNode
{
    public Vector2Int coord;
    public StageRoomType roomType;
    public RaidRoomRole raidRole;
    public bool isCleared;
    public bool isRevealed;
    public bool isOpened;
    public bool isLocked;
    public StageRoomConnectionFlags connections;

    public StageRoomNode(
        Vector2Int coord,
        StageRoomType roomType,
        StageRoomConnectionFlags connections)
    {
        this.coord = coord;
        this.roomType = roomType;
        this.connections = connections;
    }

    // Purpose: Checks whether this component has connection.
    public bool HasConnection(StageRoomConnectionFlags flag)
    {
        return (connections & flag) != 0;
    }
}
