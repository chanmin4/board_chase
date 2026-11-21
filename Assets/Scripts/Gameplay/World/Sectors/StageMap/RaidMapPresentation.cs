using UnityEngine;

/// <summary>One shared landmark/discovery policy for Mini Map and Full Map.</summary>
public static class RaidMapPresentation
{
    public static void Apply(StageMapLayout layout, StageRoomNode room, Vector2Int current, ref SectorMapCellSnapshot cell)
    {
        if (layout?.generationVersion != 3 || room == null) return;
        if(layout.landmarkSchemaVersion>0&&!string.IsNullOrEmpty(room.landmarkId))
        {
            cell.isRevealed=true;cell.isLocked=false;cell.hasPublicRaidMarker=true;
            cell.roomType=StageRoomType.Normal;cell.raidRole=RaidRoomRole.Normal;
            cell.mapIconKind=room.landmarkIcon;cell.specialState&=~SectorSpecialState.BossActive;
            cell.isExtraction=layout.IsExtractionSector(room.coord);
            return; // Public landmark identity must never disclose its random boss assignment.
        }
        bool known = room.isRevealed || room.coord==current;
        bool generator = room.coord == layout.generatorRoomCoord;
        bool candidate = layout.IsExtractionCandidate(room.coord);
        bool hatch = room.coord == layout.hatchRoomCoord;
        cell.isRevealed = known;
        cell.isLocked = !known;
        cell.isExtraction = known && (layout.IsExtractionSector(room.coord) ||
            (hatch && RaidRuntime.IsHatchRevealedFor(layout)));
        cell.hasPublicRaidMarker = generator || candidate || hatch ||
            (known && (room.hasMerchant || room.raidRole is RaidRoomRole.Merchant or RaidRoomRole.Vault));
        // Extraction is only the map marker; the underlying room keeps its ordinary content/encounter.
        cell.raidRole = generator ? RaidRoomRole.Generator : !known && candidate ? RaidRoomRole.UnknownCandidate :
            known && layout.IsExtractionSector(room.coord) ? RaidRoomRole.Extraction :
            hatch ? RaidRoomRole.Hatch : known && room.hasMerchant ? RaidRoomRole.Merchant : known && room.raidRole == RaidRoomRole.Boss && !layout.bossSpawned
                ? RaidRoomRole.Normal : known ? room.raidRole : RaidRoomRole.None;
        if (!known) cell.roomType = StageRoomType.Normal;
        if(MapDiscoveryRulesSO.Active?.map.hideUnvisited==true && !known)
        {
            cell.hasPublicRaidMarker=false;cell.isExtraction=false;cell.raidRole=RaidRoomRole.None;
            cell.mapIconKind=SectorMapIconKind.Unknown;
        }
        if(MapDiscoveryRulesSO.Active?.map.hideBossIcons==true)
        {
            cell.specialState &= ~SectorSpecialState.BossActive;
            if(cell.roomType==StageRoomType.Boss)cell.roomType=StageRoomType.Normal;
            if(cell.raidRole==RaidRoomRole.Boss)cell.raidRole=RaidRoomRole.Normal;
            if(cell.mapIconKind==SectorMapIconKind.Boss)cell.mapIconKind=SectorMapIconKind.ThemedCombat;
        }
    }
}
