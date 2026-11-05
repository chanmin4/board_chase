using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Opt-in pool budgets. Existing assets/saves retain their old Normal pool when OFF.</summary>
[Serializable]
public sealed class StageRoomCompositionSettings
{
    public bool enabled;
    [Range(0, 100)] public int themePercent = 50;
    [Range(0, 100)] public int specialPercent = 25;
    [Range(0, 100)] public int commonPercent = 25;
    [Tooltip("해당 스테이지의 테마를 살린 일반 전투방 목록.")]
    public List<StageDefinitionSO.WeightedSectorPreset> themePresets = new();
    [Tooltip("비어 있으면 기존 Normal Sector 풀을 공용 전투방으로 사용합니다.")]
    public List<StageDefinitionSO.WeightedSectorPreset> commonPresets = new();

    public bool TryValidate(out string error, bool requireSpecialRatio = false)
    {
        error = null;
        if (requireSpecialRatio && (specialPercent < 0 || specialPercent > 100))
        { error = "특별맵 비율은 0~100%여야 합니다."; return false; }
        if (!enabled) return true;
        if (themePercent < 0 || specialPercent < 0 || commonPercent < 0 ||
            themePercent > 100 || specialPercent > 100 || commonPercent > 100 ||
            themePercent + specialPercent + commonPercent != 100)
        { error = "맵 구성 비율의 합은 100이어야 합니다."; return false; }
        return true;
    }

    public static bool IsRatioCandidate(StageMapLayout layout, StageRoomNode room) =>
        room != null && room.coord != layout.startSectorCoord && room.roomType == StageRoomType.Normal &&
        (room.raidRole is RaidRoomRole.Normal or RaidRoomRole.None) && !layout.extractionCandidates.Contains(room.coord);

    public static int RatioCandidateCount(StageMapLayout layout) => layout.rooms.FindAll(room => IsRatioCandidate(layout, room)).Count;

    public int AdditionalSpecialBudget(StageMapLayout layout, bool independentSpecials = false)
    {
        if (independentSpecials) return Mathf.RoundToInt(RatioCandidateCount(layout) * specialPercent / 100f);
        int budgetRooms = 0, reservedSpecial = 0;
        foreach (var room in layout.rooms)
        {
            if (room.coord == layout.startSectorCoord || room.roomType != StageRoomType.Normal) continue;
            if (room.raidRole is RaidRoomRole.Normal or RaidRoomRole.None or RaidRoomRole.Generator or RaidRoomRole.Vault)
                budgetRooms++;
            if (room.raidRole is RaidRoomRole.Generator or RaidRoomRole.Vault) reservedSpecial++;
        }
        return Mathf.Max(0, Mathf.RoundToInt(budgetRooms * specialPercent / 100f) - reservedSpecial);
    }

    public bool TryAssign(StageDefinitionSO stage, StageMapLayout layout, out string error)
    {
        if (!TryValidate(out error)) return false;
        if (!enabled) return true;
        var ordinary = new List<StageRoomNode>();
        int budgetRooms = 0;
        bool independent = stage.SpecialSectors.useRatio;
        foreach (var room in layout.rooms)
        {
            if (independent)
            {
                if (!IsRatioCandidate(layout, room)) continue;
                budgetRooms++;
                if (string.IsNullOrEmpty(room.specialSectorPoolId)) ordinary.Add(room);
                continue;
            }
            if (room.coord == layout.startSectorCoord || room.roomType != StageRoomType.Normal) continue;
            if (room.raidRole is RaidRoomRole.Normal or RaidRoomRole.None or RaidRoomRole.Generator or RaidRoomRole.Vault)
                budgetRooms++;
            if ((room.raidRole is RaidRoomRole.Normal or RaidRoomRole.None) && string.IsNullOrEmpty(room.specialSectorPoolId))
                ordinary.Add(room);
        }
        int themed = Mathf.Min(ordinary.Count, Mathf.RoundToInt(budgetRooms * themePercent / 100f));
        if (themed > 0 && !HasValid(themePresets))
        { error = "전용맵 비율이 있지만 유효한 Normal 전용 프리팹 풀이 없습니다."; return false; }
        if (ordinary.Count > themed && !HasValid(CommonPool(stage)))
        { error = "공용 전투방에 유효한 Normal 프리팹 풀이 없습니다."; return false; }
        var random = new System.Random(StageDefinitionSO.BuildSectorSeed(layout.stageSeed, Vector2Int.zero, 19249));
        for (int i = ordinary.Count - 1; i > 0; i--)
        { int j = random.Next(i + 1); (ordinary[i], ordinary[j]) = (ordinary[j], ordinary[i]); }
        for (int i = 0; i < ordinary.Count; i++) ordinary[i].combatPoolId = i < themed ? "theme" : "common";
        return true;
    }

    // A saved category is resolved even if its authoring toggle was later disabled.
    public bool TryResolve(StageDefinitionSO stage, StageMapLayout layout, StageRoomNode room, out SectorPresetSO preset)
    {
        preset = null;
        IReadOnlyList<StageDefinitionSO.WeightedSectorPreset> pool = room.combatPoolId == "theme"
            ? themePresets : room.combatPoolId == "common" ? CommonPool(stage) : null;
        if (pool == null) return false;
        double total = 0;
        foreach (var entry in pool) if (Valid(entry)) total += entry.weight;
        if (total <= 0) return false;
        var random = new System.Random(StageDefinitionSO.BuildSectorSeed(layout.stageSeed, room.coord, 19253));
        double roll = random.NextDouble() * total;
        foreach (var entry in pool)
        {
            if (!Valid(entry)) continue;
            roll -= entry.weight;
            if (roll < 0) { preset = entry.preset; return true; }
        }
        return false;
    }

    private IReadOnlyList<StageDefinitionSO.WeightedSectorPreset> CommonPool(StageDefinitionSO stage) =>
        commonPresets != null && commonPresets.Count > 0 ? commonPresets : stage.NormalSectorPresets;
    private static bool HasValid(IReadOnlyList<StageDefinitionSO.WeightedSectorPreset> pool)
    {
        if (pool != null) foreach (var entry in pool) if (Valid(entry)) return true;
        return false;
    }
    private static bool Valid(StageDefinitionSO.WeightedSectorPreset entry) => entry != null && entry.weight > 0 &&
        entry.preset != null && entry.preset.IsValid && entry.preset.RoomType == StageRoomType.Normal;
}
