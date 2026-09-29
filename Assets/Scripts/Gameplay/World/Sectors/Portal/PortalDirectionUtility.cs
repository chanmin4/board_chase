/*
 * Role hierarchy: Gameplay > World > Sectors > Portal
 * Primary responsibility: Manages SectorPortalDirection stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
public enum SectorPortalDirection
{
    XMin,
    XMax,
    ZMin,
    ZMax
}
public static class SectorPortalDirectionUtility
{
    // Purpose: Performs the to coord offset workflow for this component.
    public static Vector2Int ToCoordOffset(SectorPortalDirection direction)
    {
        switch (direction)
        {
            case SectorPortalDirection.XMin:
                return Vector2Int.left;

            case SectorPortalDirection.XMax:
                return Vector2Int.right;

            case SectorPortalDirection.ZMin:
                return Vector2Int.down;

            case SectorPortalDirection.ZMax:
                return Vector2Int.up;

            default:
                return Vector2Int.zero;
        }
    }

    // Purpose: Performs the opposite workflow for this component.
    public static SectorPortalDirection Opposite(SectorPortalDirection direction)
    {
        switch (direction)
        {
            case SectorPortalDirection.XMin:
                return SectorPortalDirection.XMax;

            case SectorPortalDirection.XMax:
                return SectorPortalDirection.XMin;

            case SectorPortalDirection.ZMin:
                return SectorPortalDirection.ZMax;

            case SectorPortalDirection.ZMax:
                return SectorPortalDirection.ZMin;

            default:
                return direction;
        }
    }
}
