using UnityEngine;

/// <summary>One sector-type sprite policy for Mini Map, Full Map and Stage Maker.</summary>
public static class SectorMapIconPolicy
{
    public static SectorMapIconKind KindForRole(RaidRoomRole role, bool extraction = false)
    {
        if (extraction) return SectorMapIconKind.Extraction;
        return role switch
        {
            RaidRoomRole.Hatch => SectorMapIconKind.Hatch, RaidRoomRole.Generator => SectorMapIconKind.Generator,
            RaidRoomRole.Extraction => SectorMapIconKind.Extraction, RaidRoomRole.Merchant => SectorMapIconKind.Merchant,
            RaidRoomRole.Boss => SectorMapIconKind.Boss, RaidRoomRole.Armory => SectorMapIconKind.Armory,
            RaidRoomRole.Medical => SectorMapIconKind.Medical, RaidRoomRole.Vault => SectorMapIconKind.Vault,
            RaidRoomRole.UnknownCandidate => SectorMapIconKind.Unknown, _ => SectorMapIconKind.None
        };
    }

    public static SectorMapIconKind KindForRoom(StageRoomNode room)
    {
        if (room == null) return SectorMapIconKind.None;
        if (room.roomType == StageRoomType.Boss) return SectorMapIconKind.Boss;
        if (room.hasMerchant) return SectorMapIconKind.Merchant;
        // Exterior extraction is an overlay on a combat room, not its underlying room type.
        var role = KindForRole(room.raidRole is RaidRoomRole.Boss or RaidRoomRole.Extraction ? RaidRoomRole.Normal : room.raidRole);
        if (role != SectorMapIconKind.None) return role;
        if (room.roomType == StageRoomType.Start) return SectorMapIconKind.Hatch;
        if (room.roomType == StageRoomType.Named) return SectorMapIconKind.Named;
        if (room.roomType == StageRoomType.Treasure) return SectorMapIconKind.Vault;
        if (room.roomType == StageRoomType.Shop) return SectorMapIconKind.Merchant;
        return room.specialSectorPoolId switch
        {
            "office" => SectorMapIconKind.Office, "storage" or "warehouse" => SectorMapIconKind.Warehouse,
            "security" or "armory" => SectorMapIconKind.Armory, "medical" => SectorMapIconKind.Medical,
            "vault" => SectorMapIconKind.Vault, "server" => SectorMapIconKind.Server,
            _ => room.combatPoolId == "theme" ? SectorMapIconKind.ThemedCombat : SectorMapIconKind.NormalCombat
        };
    }

    public static SectorMapIconKind VisibleKind(SectorMapCellSnapshot snapshot)
    {
        if (snapshot.isLocked || !snapshot.isRevealed)
        {
            var publicKind = snapshot.hasPublicRaidMarker ? KindForRole(snapshot.raidRole, snapshot.isExtraction) : SectorMapIconKind.None;
            return publicKind != SectorMapIconKind.None ? publicKind : SectorMapIconKind.Unknown;
        }
        if (snapshot.isExtraction) return SectorMapIconKind.Extraction;
        if (snapshot.isStartSector || snapshot.roomType == StageRoomType.Start) return SectorMapIconKind.Hatch;
        if (snapshot.roomType == StageRoomType.Boss || (snapshot.specialState & SectorSpecialState.BossActive) != 0) return SectorMapIconKind.Boss;
        var role = KindForRole(snapshot.raidRole);
        if (role != SectorMapIconKind.None) return role;
        if (snapshot.mapIconKind != SectorMapIconKind.None) return snapshot.mapIconKind;
        if (snapshot.roomType == StageRoomType.Named || (snapshot.specialState & SectorSpecialState.NamedActive) != 0) return SectorMapIconKind.Named;
        if (snapshot.roomType == StageRoomType.Treasure) return SectorMapIconKind.Vault;
        if (snapshot.roomType == StageRoomType.Shop) return SectorMapIconKind.Merchant;
        return SectorMapIconKind.NormalCombat;
    }

    public static Sprite SharedIcon(SectorMapCellSnapshot snapshot, SectorMapIconSetSO icons, Sprite fallback)
    {
        // A connected shared SO is authoritative, including an explicitly empty sprite slot.
        return icons != null ? icons.GetIcon(VisibleKind(snapshot)) : fallback;
    }

    public static bool HasExteriorExtraction(SectorMapCellSnapshot snapshot) => snapshot.isExtraction &&
        !snapshot.isStartSector && snapshot.roomType != StageRoomType.Start &&
        snapshot.raidRole != RaidRoomRole.Hatch && snapshot.mapIconKind != SectorMapIconKind.Hatch;

    public static SectorMapCellSnapshot RoomIconSnapshot(SectorMapCellSnapshot snapshot)
    {
        if (!HasExteriorExtraction(snapshot)) return snapshot;
        snapshot.isExtraction = false;
        // Public knowledge of the exit must not disclose the room's underlying type.
        snapshot.hasPublicRaidMarker = false;
        if (snapshot.raidRole == RaidRoomRole.Extraction) snapshot.raidRole = RaidRoomRole.Normal;
        if (snapshot.mapIconKind == SectorMapIconKind.Extraction) snapshot.mapIconKind = SectorMapIconKind.NormalCombat;
        return snapshot;
    }

    public static Sprite ExtractionOverlay(SectorMapCellSnapshot snapshot, SectorMapIconSetSO icons, Sprite fallback)
    {
        if (!HasExteriorExtraction(snapshot) || (!snapshot.isRevealed && !snapshot.hasPublicRaidMarker)) return null;
        return icons != null ? icons.GetIcon(SectorMapIconKind.Extraction) : fallback;
    }

    public static Sprite MiniMapIcon(SectorMapCellSnapshot snapshot, MiniMapUISettingsSO settings,
        Sprite lockedFallback = null, Sprite namedFallback = null, Sprite bossFallback = null)
    {
        var icons = settings != null ? settings.SectorIcons : null;
        if (snapshot.isStartSector)
            return SharedIcon(snapshot, icons, settings != null ? settings.GetLandmarkIcon(RaidRoomRole.Hatch, snapshot.isExtraction) : null);
        Sprite locked = settings != null ? settings.LockedIcon : lockedFallback;
        Sprite named = settings != null ? settings.NamedIcon : namedFallback;
        Sprite boss = settings != null ? settings.BossIcon : bossFallback;
        Sprite icon = null;
        if (settings != null && (snapshot.hasPublicRaidMarker || snapshot.isRevealed || snapshot.isExtraction))
            icon = settings.GetLandmarkIcon(snapshot.raidRole, snapshot.isExtraction);
        if (icon == null)
        {
            if (snapshot.isLocked || !snapshot.isRevealed) icon = locked;
            else if (snapshot.roomType == StageRoomType.Boss || (snapshot.specialState & SectorSpecialState.BossActive) != 0) icon = boss;
            else if ((snapshot.specialState & SectorSpecialState.NamedActive) != 0) icon = named;
        }
        return SharedIcon(snapshot, icons, icon);
    }
}
