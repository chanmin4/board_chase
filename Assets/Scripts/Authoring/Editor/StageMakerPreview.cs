using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageMakerPreviewCell
{
    public StageRoomNode room;
    public SectorPresetSO preset;
    public StageBattleEncounterPresetSO encounter;
    public string category, error;
    public int distance;
    public bool locked;
}

public sealed class StageMakerPreview
{
    public StageMapLayout layout;
    public readonly List<StageMakerPreviewCell> cells = new();
    public readonly Dictionary<Vector2Int, StageMakerPreviewCell> byCoord = new();
    public readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);
    public readonly List<string> errors = new();
    public int edges;
    public bool usesSpecialRatio;
    public int ratioBudgetRooms, requestedSpecialRooms, actualSpecialRooms;

    // Read-only SO traversal. Never clone/instantiate a sector, Canvas, scene, or temporary SO.
    public static bool TryBuild(StageDefinitionSO source, int seed, Vector2Int legacyStart,
        IReadOnlyCollection<string> requiredPools, out StageMakerPreview preview, out string error)
    {
        preview = null; error = null;
        if (source == null) { error = "스테이지를 선택하세요."; return false; }
        StageDefinitionSO snapshot = source;
        try
        {
            if (!StageMapBuildPipeline.TryGenerate(snapshot, seed, legacyStart, out var map, out error, requiredPools, false)) return false;
            preview = new StageMakerPreview { layout = map };
            preview.usesSpecialRatio = source.SpecialSectors.useRatio;
            preview.ratioBudgetRooms = StageRoomCompositionSettings.RatioCandidateCount(map);
            preview.requestedSpecialRooms = source.SpecialSectors.enabled && preview.usesSpecialRatio
                ? source.RoomComposition.AdditionalSpecialBudget(map, true) : 0;
            var distances = RaidMapGenerator.GetDistances(map, map.startSectorCoord);
            foreach (var room in map.rooms)
            {
                if (room == null || room.roomType == StageRoomType.Empty) continue;
                var cell = new StageMakerPreviewCell { room = room, category = Category(room),
                    distance = distances.TryGetValue(room.coord, out int d) ? d : -1 };
                if (!StageMapBuildPipeline.TryResolveRoomPreset(snapshot, map, room, out cell.preset,
                    out cell.encounter, out cell.error)) preview.errors.Add(cell.error ?? $"{room.coord}: 프리팹 누락");
                if (room.hasMerchant && snapshot.RaidRuntime.merchantConfig == null)
                {
                    string merchantError = $"{room.coord}: 상인 상점 설정(ShopConfig)이 없습니다. 등장할 맵 > 상인에 등록하세요.";
                    if (string.IsNullOrEmpty(cell.error)) cell.error = merchantError;
                    preview.errors.Add(merchantError);
                }
                if (cell.preset != null)
                {
                    bool authoredLock = map.usesRaidLayout && room.roomType == StageRoomType.Normal &&
                        room.coord != map.startSectorCoord && SectorLockedCombatArea.Find(cell.preset.SectorPrefab) != null;
                    cell.locked = room.hasInternalCombatLock || authoredLock;
                    bool combat = !room.isCleared && (room.roomType == StageRoomType.Boss ||
                        (room.roomType == StageRoomType.Normal && (cell.locked ||
                         NormalSectorPolicySO.ResolveUsesEnemyEncounter(cell.preset.NormalSectorPolicy))));
                    if (combat && room.roomType == StageRoomType.Normal)
                    {
                        cell.encounter = map.usesRaidLayout ? snapshot.RaidRuntime.FindRoom(room.raidRole)?.encounter : null;
                        if (cell.encounter == null && !snapshot.TryPickNormalSectorEncounter(map.stageSeed, room.coord, out cell.encounter))
                        { cell.error = $"{room.coord}: 일반 전투 Encounter 풀이 없습니다."; preview.errors.Add(cell.error); }
                    }
                }
                if (cell.distance < 0) preview.errors.Add($"{room.coord}: 시작점에서 도달할 수 없습니다.");
                preview.cells.Add(cell);
                if (!string.IsNullOrEmpty(room.specialSectorPoolId)) preview.actualSpecialRooms++;
                preview.byCoord[room.coord] = cell;
                preview.counts.TryGetValue(cell.category, out int count);
                preview.counts[cell.category] = count + 1;
                foreach (var flag in new[] { StageRoomConnectionFlags.Right, StageRoomConnectionFlags.Up })
                    if (room.HasConnection(flag)) preview.edges++;
            }
            return true;
        }
        catch (Exception ex)
        { error = "미리보기 실패: " + ex.Message; preview = null; return false; }
    }

    // Extraction is portal metadata on an ordinary room, not its content category.
    public static string ExtractionLabel(StageMapLayout layout, Vector2Int coord, bool compact = false)
    {
        if (layout == null || !layout.IsExtractionSector(coord)) return string.Empty;
        var route = layout.extractionPortals?.Find(candidate => candidate != null && candidate.coord == coord);
        if (route == null && layout.extractionPortalVersion == 1) return "외곽 탈출 · 방향 누락";
        if (route == null && layout.generationVersion == 3) return "외곽 탈출";
        var direction = route != null ? route.direction : layout.extractionDirection;
        string arrow = direction switch
        {
            SectorPortalDirection.XMin => "← 서쪽", SectorPortalDirection.XMax => "→ 동쪽",
            SectorPortalDirection.ZMin => "↓ 남쪽", SectorPortalDirection.ZMax => "↑ 북쪽", _ => "? 방향 미확인"
        };
        return "외곽 탈출 " + (compact ? arrow[0].ToString() : arrow);
    }

    public static string Category(StageRoomNode room)
    {
        if (room.hasMerchant) return "공용 전투 · 상인";
        if (!string.IsNullOrEmpty(room.specialSectorPoolId)) return "특별 · " + room.specialSectorPoolId;
        if (room.roomType == StageRoomType.Boss) return room.combatPoolId == "theme" ? "보스 · 스테이지 전용" : "보스";
        if (room.combatPoolId == "theme") return "스테이지 전용";
        if (room.combatPoolId == "common") return "공용 전투";
        if (room.roomType == StageRoomType.Start) return "시작";
        return room.raidRole switch
        {
            RaidRoomRole.Generator => "발전실", RaidRoomRole.Hatch => "해치",
            RaidRoomRole.Merchant => "상인", RaidRoomRole.Vault => "금고",
            RaidRoomRole.Boss => "일반 전투 · 보스 미출현", RaidRoomRole.Armory => "무기고",
            RaidRoomRole.Medical => "의료실", RaidRoomRole.Extraction => "탈출",
            _ => "일반 전투"
        };
    }
}
