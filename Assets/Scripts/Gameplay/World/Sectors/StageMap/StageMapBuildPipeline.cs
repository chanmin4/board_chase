using System.Collections.Generic;
using UnityEngine;

/// <summary>Pure graph/preset generation shared by gameplay and Stage Maker. No scene objects, saves or Unity Random.</summary>
public static class StageMapBuildPipeline
{
    public static bool TryGenerate(StageDefinitionSO stage, int runSeed, Vector2Int legacyStart,
        out StageMapLayout layout, out string error, IReadOnlyCollection<string> requiredPools = null,
        bool readRuntimeQuests = true)
    {
        layout = null; error = null;
        if (stage == null) { error = "Stage SO가 없습니다."; return false; }
        if (!System.Enum.IsDefined(typeof(StageMapPolicy), stage.MapPolicy) || stage.RaidMap == null || stage.RaidRuntime == null)
        { error = "맵 정책 또는 Raid Map/Runtime 설정이 없습니다."; return false; }
        bool specialRatio = stage.SpecialSectors.enabled && stage.SpecialSectors.useRatio;
        if (!stage.RoomComposition.TryValidate(out error, specialRatio)) return false;
        bool startOnly = stage.UseStartSectorOnly || stage.RoomGridSize <= 0;
        int grid = startOnly ? 0 : stage.RoomGridSize;
        if (!startOnly && stage.MapPolicy != StageMapPolicy.Legacy)
        {
            bool generated = stage.MapPolicy == StageMapPolicy.RadialBraidedRaid
                ? RadialBraidedRaidGenerator.TryGenerate(runSeed, stage.StageIndex, grid, stage.RaidMap,
                    out layout, out error, stage.RaidRuntime.vaultUsesInternalCombatLock)
                : RaidMapGenerator.TryGenerate(runSeed, stage.StageIndex, grid, legacyStart, stage.RaidMap, out layout, out error);
            if (!generated) return false;
            int? count = specialRatio ? stage.RoomComposition.AdditionalSpecialBudget(layout, true)
                : stage.RoomComposition.enabled ? stage.RoomComposition.AdditionalSpecialBudget(layout) : null;
            if (!stage.SpecialSectors.TryAssign(layout, out error, requiredPools, readRuntimeQuests, count))
            { layout = null; return false; }
        }
        else if (!startOnly && stage.HasEnabledBranchTopology)
        {
            if (!stage.TryPickBranchTopology(StageMapGenerator.BuildStageSeed(runSeed, stage.StageIndex), out var topology))
            { error = "유효한 Branch Topology가 없습니다."; return false; }
            if (!StageMapGenerator.TryGenerateBranchTopology(runSeed, stage.StageIndex, legacyStart,
                stage.GoalRoomType, topology, out layout, out error)) return false;
        }
        else layout = StageMapGenerator.GenerateFullGrid(runSeed, stage.StageIndex, grid, legacyStart, stage.GoalRoomType,
            StageTreasureRoomGenerationSettings.Disabled, StageShopRoomGenerationSettings.Disabled);
        if (!stage.RoomComposition.TryAssign(stage, layout, out error)) { layout = null; return false; }
        StageSystemPolicies.Apply(stage,layout);
        if (!RaidMerchantPlacement.TryAssign(layout, stage.RaidMap, out error)) { layout = null; return false; }
        return true;
    }

    public static bool TryResolveRoomPreset(StageDefinitionSO stage, StageMapLayout layout, StageRoomNode room,
        out SectorPresetSO preset, out StageBattleEncounterPresetSO bossEncounter, out string error)
    {
        preset = null; bossEncounter = null; error = null;
        if (stage == null || layout == null || room == null) { error = "맵/방/SO가 없습니다."; return false; }
        if (room.roomType == StageRoomType.Boss)
        {
            if (layout.landmarkPlacementVersion == 1)
            {
                bool resolved=layout.bossMayUseSpecialRoom&&!string.IsNullOrEmpty(room.specialSectorPoolId)
                    ? stage.SpecialSectors.TryResolve(room,out preset)
                    : room.combatPoolId=="theme"&&stage.RoomComposition.TryResolve(stage,layout,room,out preset);
                if (!resolved)
                { error = $"{room.coord}: {stage.StageIndex}스테이지 보스용 전용맵 풀이 비어 있거나 유효하지 않습니다. 등장할 맵 > 스테이지 전용맵 풀에 Normal 프리셋을 등록하세요."; return false; }
                if (!stage.TryPickBossEncounter(layout.stageSeed, room.coord, out bossEncounter))
                { error = $"{room.coord}: 유효한 보스 Encounter가 없습니다. 보스 전투 목록을 확인하세요."; return false; }
            }
            else
            {
                if (!stage.TryPickBossBattle(layout.stageSeed, room.coord, out bossEncounter, out var boss))
                { error = $"{room.coord}: 유효한 보스 전투/섹터 쌍이 없습니다."; return false; }
                preset = boss;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(room.specialSectorPoolId))
            {
                if (!stage.SpecialSectors.TryResolve(room, out preset))
                { error = $"{room.coord}: 특별맵 {room.specialSectorPoolId}/{room.specialSectorVariantId}을 찾을 수 없습니다. 재추첨하지 않습니다."; return false; }
                return true;
            }
            if (!string.IsNullOrEmpty(room.combatPoolId))
            {
                if (!stage.RoomComposition.TryResolve(stage, layout, room, out preset))
                { error = $"{room.coord}: 저장된 {room.combatPoolId} 풀이 비어 있습니다. 재추첨하지 않습니다."; return false; }
            }
            else if (!stage.TryPickSectorPreset(layout.stageSeed, room.coord, room.roomType, out preset))
            { error = $"{room.coord}: {room.roomType} 섹터 프리팹 풀이 없습니다."; return false; }
        }
        var binding = layout.usesRaidLayout ? stage.RaidRuntime.FindRoom(room.raidRole) : null;
        if (binding != null && room.roomType == StageRoomType.Normal && string.IsNullOrEmpty(room.combatPoolId) &&
            binding.TryPickSectorPreset(layout.stageSeed, room.coord, out var rolePreset)) preset = rolePreset;
        return preset != null;
    }
}
