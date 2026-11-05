using UnityEngine;

/// <summary>Once-per-stage merchant overlay, assigned only after content pools are known.</summary>
public static class RaidMerchantPlacement
{
    public static bool IsEligible(StageMapLayout layout, StageRoomNode room) =>
        room != null && room.roomType == StageRoomType.Normal &&
        room.raidRole is RaidRoomRole.Normal or RaidRoomRole.None &&
        room.coord != layout.startSectorCoord && !layout.IsExtractionSector(room.coord) &&
        !layout.IsExtractionCandidate(room.coord) && !room.hasInternalCombatLock &&
        string.IsNullOrEmpty(room.specialSectorPoolId) &&
        (room.combatPoolId == "common" || string.IsNullOrEmpty(room.combatPoolId));

    public static bool TryAssign(StageMapLayout layout, RaidMapSettings settings, out string error)
    {
        error = null;
        if (layout == null || settings == null) { error = "상인 배치 설정/맵이 없습니다."; return false; }
        if (layout.generationVersion != 3 || layout.landmarkPlacementVersion != 1) return true;
        // Never reroll a completed/saved placement, even if the live SO probability changes.
        if (layout.merchantPlacementVersion != 0) return TryValidate(layout, out error);
        if (settings.merchantSpawnPercent < 0 || settings.merchantSpawnPercent > 100)
        { error = $"상인 등장 확률: 현재 {settings.merchantSpawnPercent}% → 허용 범위 0~100%."; return false; }
        layout.merchantPlacementVersion = 1;
        var roll = new System.Random(StageDefinitionSO.BuildSectorSeed(layout.stageSeed, Vector2Int.zero, 1321));
        layout.merchantRollSucceeded = roll.Next(100) < settings.merchantSpawnPercent;
        if (!layout.merchantRollSucceeded) return TryValidate(layout, out error);
        var candidates = layout.rooms.FindAll(room => IsEligible(layout, room));
        if (candidates.Count > 0)
        {
            var location = new System.Random(StageDefinitionSO.BuildSectorSeed(layout.stageSeed, Vector2Int.zero, 1323));
            var room = candidates[location.Next(candidates.Count)];
            room.hasMerchant = true;
            // Also use the explicit common pool when theme-ratio assignment is OFF.
            room.combatPoolId = "common";
        }
        return TryValidate(layout, out error);
    }

    public static bool TryValidate(StageMapLayout layout, out string error)
    {
        error = "상인 배치 저장 정보가 올바르지 않습니다.";
        if (layout?.rooms == null || layout.merchantPlacementVersion < 0 || layout.merchantPlacementVersion > 1) return false;
        if (layout.merchantPlacementVersion == 0 && layout.merchantRollSucceeded || layout.merchantPlacementVersion == 1 &&
            (layout.generationVersion != 3 || layout.landmarkPlacementVersion != 1)) return false;
        int count = 0;
        foreach (var room in layout.rooms)
        {
            if (room == null) return false;
            if (!room.hasMerchant) continue;
            if (++count > 1 || layout.merchantPlacementVersion != 1 || !layout.merchantRollSucceeded ||
                room.combatPoolId != "common" || !IsEligible(layout, room)) return false;
        }
        error = string.Empty; return true;
    }
}
