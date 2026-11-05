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
    // Explicit save discriminator: only 1~7 grids may omit the normal V3 ring/role invariants.
    public bool compactTestLayout;
    // 0: legacy distant candidates include the boss. 1: leaf generator/exits, independent themed boss.
    public int landmarkPlacementVersion;
    public Vector2Int bossRoomCoord;
    public bool bossSpawned;
    // Snapshot, not a live SO setting. Missing on older saves means their topology stays valid.
    public bool bossAvoidsExtractionRoutes;
    // Zero means old/fresh graph. One stores the post-composition, once-per-stage merchant roll.
    public int merchantPlacementVersion;
    public int gameSystemPolicyVersion;
    public bool bossMayUseSpecialRoom;
    public bool merchantRollSucceeded;
    public Vector2Int generatorRoomCoord;
    public Vector2Int hatchRoomCoord;
    // Empty on saved V1/V2 layouts. Never infer candidates from a new seed on load.
    public List<Vector2Int> extractionCandidates = new();
    public List<Vector2Int> extractionRooms = new();
    // Independent from graph generationVersion: old graphs can keep all their travel links.
    public int extractionPortalVersion;
    public List<RaidExtractionPortalRoute> extractionPortals = new();
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
        if (!hasExtraction) return false;
        if (usesRaidLayout)
            return extractionPortalVersion == 1 && extractionPortals != null &&
                   extractionPortals.Exists(route => route != null && route.coord == sectorCoord && route.direction == direction);
        return extractionSectorCoord == sectorCoord && extractionDirection == direction;
    }

    // Purpose: Checks whether the supplied sector owns the single generated extraction portal.
    public bool IsExtractionSector(Vector2Int sectorCoord)
    {
        return hasExtraction && (generationVersion == 3
            ? extractionRooms != null && extractionRooms.Contains(sectorCoord)
            : extractionSectorCoord == sectorCoord);
    }

    public bool IsExtractionCandidate(Vector2Int coord) => generationVersion == 3 &&
        extractionCandidates != null && extractionCandidates.Contains(coord);
}

[Serializable]
public sealed class RaidExtractionPortalRoute
{
    public Vector2Int coord;
    public SectorPortalDirection direction;
}
