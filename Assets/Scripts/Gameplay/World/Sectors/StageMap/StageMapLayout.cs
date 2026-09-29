/*
 * Role hierarchy: Gameplay > World > Sectors > StageMap
 * Primary responsibility: Manages StageMapLayout stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
    public bool usesBranchTopology;
    public string branchTopologyName;
    public Vector2Int startSectorCoord;
    public Vector2Int firstRoomCoord;
    public Vector2Int goalRoomCoord;
    public bool hasExtraction;
    public Vector2Int extractionSectorCoord;
    public SectorPortalDirection extractionDirection;
    // Version zero is a legacy edge-portal layout. Never reinterpret it on load.
    public int generationVersion;
    public bool usesRaidLayout;
    public Vector2Int bossRoomCoord;
    public bool bossSpawned;
    public Vector2Int generatorRoomCoord;
    public Vector2Int hatchRoomCoord;
    public List<StageRoomNode> rooms = new();

    public bool HasGeneratedRooms => gridSize > 0;

    // Purpose: Attempts to get room and reports whether the request succeeded.
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

    // Purpose: Attempts to get goal room and reports whether the request succeeded.
    public bool TryGetGoalRoom(out StageRoomNode room)
    {
        return TryGetRoom(goalRoomCoord, out room);
    }

    // Purpose: Checks whether the supplied outer portal is the single generated extraction portal.
    public bool IsExtractionPortal(Vector2Int sectorCoord, SectorPortalDirection direction)
    {
        return !usesRaidLayout && hasExtraction &&
               extractionSectorCoord == sectorCoord &&
               extractionDirection == direction;
    }

    // Purpose: Checks whether the supplied sector owns the single generated extraction portal.
    public bool IsExtractionSector(Vector2Int sectorCoord)
    {
        return hasExtraction && extractionSectorCoord == sectorCoord;
    }
}
