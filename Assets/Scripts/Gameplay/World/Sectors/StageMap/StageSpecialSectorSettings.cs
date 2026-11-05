using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SpecialSectorVariant
{
    [Tooltip("저장 호환용 고유 ID. 출시/배치 후 변경하지 않습니다.")]
    public string id = "variant_1";
    public SectorPresetSO preset;
    [Min(0)] public int weight = 1;
}

[Serializable]
public sealed class SpecialSectorPool
{
    public bool disabled;
    [Tooltip("office, storage, medical 등 종류별 고유 ID. 종류를 먼저 추첨합니다.")]
    public string id = "office";
    [Min(0)] public int weight = 1;
    [Min(0)] public int minimumCount;
    [Min(0)] public int maximumCount = 1;
    [Min(0)] public int minimumStartDistance;
    [Tooltip("0이면 제한 없음. 실제 포탈 경로 거리입니다.")]
    [Min(0)] public int maximumStartDistance;
    public SpecialSectorDirection direction;
    public List<SpecialSectorVariant> variants = new();
    [Tooltip("잠금 전에는 외곽 통행, 열쇠 확인 후 차단벽 하강과 섹터 전투. 선택 결과는 방에 저장됩니다.")]
    public bool lockedCombatArea;
    public LootKeyTier keyTier = LootKeyTier.Normal;
    [Tooltip("수락했고 아직 완료하지 않은 퀘스트가 있으면 해당 종류를 보장합니다. 미수락/완료 퀘스트는 영향 없습니다.")]
    public List<QuestDefinitionSO> requiredByQuests = new();
    public bool IsRequired
    {
        get
        {
            var runtime = QuestRuntime.Instance;
            if (runtime == null || requiredByQuests == null) return false;
            foreach (var state in runtime.States)
                if (state != null && state.IsAccepted && !state.IsCompleted && requiredByQuests.Contains(state.Definition)) return true;
            return false;
        }
    }
}

public enum SpecialSectorDirection { Any, North, South, East, West }

/// <summary>Opt-in stage authoring. New raids only; selected stable IDs are saved on room nodes.</summary>
[Serializable]
public sealed class StageSpecialSectorSettings
{
    public bool enabled;
    [Tooltip("기본은 비율 목표로 추첨하고 부족분은 일반방으로 채웁니다. OFF는 기존 개수 방식입니다.")]
    public bool useRatio = true;
    [Tooltip("비율 모드에서도 아래 전체/종류별 최소·최대를 강제로 적용할 때만 켭니다.")]
    public bool useAdvancedCountLimits;
    [Min(0)] public int minimumCount = 2;
    [Min(0)] public int maximumCount = 3;
    public bool allowRepeatedTypes;
    [Tooltip("동일 종류의 변형을 우선 중복 없이 사용합니다. 변형을 모두 쓰면 다시 허용합니다.")]
    public bool avoidRepeatedVariants;
    public List<SpecialSectorPool> pools = new();

    public bool TryAssign(StageMapLayout layout, out string error,
        IReadOnlyCollection<string> requiredPoolIds = null, bool readRuntimeQuests = true, int? requestedCount = null)
    {
        error = null;
        if (!enabled)
        {
            if (requiredPoolIds != null && requiredPoolIds.Count > 0)
            { error = "필수 종류를 요청했지만 특별맵 추첨이 OFF입니다."; return false; }
            return true;
        }
        bool countLimits = !useRatio || useAdvancedCountLimits;
        if (layout?.rooms == null || (countLimits && (minimumCount < 0 || maximumCount < minimumCount)))
        { error = "특별 섹터 최소/최대 수량 또는 맵이 잘못되었습니다."; return false; }
        var eligible = new List<SpecialSectorPool>();
        bool Required(SpecialSectorPool pool) =>
            (requiredPoolIds != null && System.Linq.Enumerable.Contains(requiredPoolIds, pool.id)) ||
            (readRuntimeQuests && pool.IsRequired);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pool in pools ?? new List<SpecialSectorPool>())
        {
            if (pool == null) continue;
            if (string.IsNullOrWhiteSpace(pool.id) || !ids.Add(pool.id))
            { error = "특별 섹터 종류 ID가 비어 있거나 중복입니다."; return false; }
            var variantIds = new HashSet<string>(StringComparer.Ordinal);
            bool valid = false;
            foreach (var variant in pool.variants ?? new List<SpecialSectorVariant>())
            {
                if (variant == null) continue;
                if (string.IsNullOrWhiteSpace(variant.id) || !variantIds.Add(variant.id))
                { error = $"특별 섹터 {pool.id} 변형 ID가 비어 있거나 중복입니다."; return false; }
                if (IsValid(variant)) valid = true;
            }
            if ((countLimits && (pool.minimumCount < 0 || pool.maximumCount < pool.minimumCount)) ||
                pool.minimumStartDistance < 0 || pool.maximumStartDistance < 0 ||
                (pool.maximumStartDistance > 0 && pool.minimumStartDistance > pool.maximumStartDistance) ||
                !Enum.IsDefined(typeof(SpecialSectorDirection), pool.direction))
            { error = $"특별 섹터 {pool.id}: 종류별 최소/최대 또는 거리/방향이 잘못되었습니다."; return false; }
            if ((Required(pool) || (countLimits && !pool.disabled && pool.minimumCount > 0)) &&
                (pool.disabled || !valid || pool.weight <= 0 || (countLimits && pool.maximumCount < 1)))
            { error = $"퀘스트 필수 특별 섹터 {pool.id}에 활성 변형/가중치가 없습니다."; return false; }
            if (!pool.disabled && valid && pool.weight > 0 && (!countLimits || pool.maximumCount > 0)) eligible.Add(pool);
        }
        if (requiredPoolIds != null)
            foreach (string id in requiredPoolIds)
                if (!eligible.Exists(p => p.id == id))
                { error = $"요청한 필수 특별 종류 '{id}'가 활성 목록에 없습니다."; return false; }
        var rooms = layout.rooms.FindAll(n => StageRoomCompositionSettings.IsRatioCandidate(layout, n));
        int maximum = countLimits ? Mathf.Max(0, maximumCount) : rooms.Count;
        var selected = new List<SpecialSectorPool>();
        var counts = new Dictionary<SpecialSectorPool, int>();
        int capacity = 0;
        foreach (var pool in eligible)
        {
            int maximumForType = allowRepeatedTypes ? (countLimits ? Mathf.Min(pool.maximumCount, rooms.Count) : rooms.Count) : 1;
            int minimumForType = Mathf.Max(countLimits ? pool.minimumCount : 0, Required(pool) ? 1 : 0);
            if (minimumForType > maximumForType)
            { error = $"특별 섹터 {pool.id}: 종류 중복 설정/최대치가 필수 최소 {minimumForType}개를 허용하지 않습니다."; return false; }
            capacity = Mathf.Min(rooms.Count, capacity + maximumForType);
            counts[pool] = minimumForType;
            for (int i = 0; i < minimumForType; i++) selected.Add(pool);
        }
        int required = selected.Count;
        if (required > maximum || required > rooms.Count)
        { error = $"필수 특별 섹터 {required}개를 상한 {maximum}/배치 가능 방 {rooms.Count}개 안에 배치할 수 없습니다."; return false; }
        int minimum = countLimits ? Mathf.Clamp(minimumCount, 0, maximum) : 0;
        if (minimum > capacity || minimum > rooms.Count)
        { error = "특별 섹터 최소 수량보다 유효 종류 또는 배치 가능 방이 적습니다."; return false; }
        var random = new System.Random(StageDefinitionSO.BuildSectorSeed(layout.stageSeed, Vector2Int.zero, 19237));
        int upper = Mathf.Min(maximum, Mathf.Min(capacity, rooms.Count));
        int count = Mathf.Max(required, requestedCount.HasValue
            ? Mathf.Clamp(requestedCount.Value, minimum, upper) : random.Next(minimum, upper + 1));
        // Mandatory pools first. Random types cannot consume their quota/rooms.
        int TypeLimit(SpecialSectorPool pool) => allowRepeatedTypes ? (countLimits ? pool.maximumCount : rooms.Count) : 1;
        eligible.RemoveAll(pool => counts[pool] >= TypeLimit(pool));
        while (selected.Count < count && eligible.Count > 0)
        {
            var pool = Pick(eligible, p => p.weight, random);
            selected.Add(pool); counts[pool]++;
            if (counts[pool] >= TypeLimit(pool)) eligible.Remove(pool);
        }
        var distances = RaidMapGenerator.GetDistances(layout, layout.startSectorCoord);
        StageRoomNode[] planned = null;
        if (selected.Exists(p => p.minimumStartDistance > 0 || p.maximumStartDistance > 0 || p.direction != SpecialSectorDirection.Any) &&
            !TryPlanConstrainedRooms(selected, rooms, distances, layout.startSectorCoord, random,
                out planned, out error, useRatio ? required : selected.Count, useRatio)) return false;
        var usedPrefabs = new HashSet<SectorRuntime>();
        int assignedCount = 0;
        for (int selectedIndex = 0; selectedIndex < selected.Count; selectedIndex++)
        {
            // Optional ratio picks may have no legal position. Their share stays ordinary.
            if (planned != null && planned[selectedIndex] == null) continue;
            var pool = selected[selectedIndex];
            var validVariants = pool.variants.FindAll(IsValid);
            if (avoidRepeatedVariants && validVariants.Exists(v => !usedPrefabs.Contains(v.preset.SectorPrefab)))
                validVariants.RemoveAll(v => usedPrefabs.Contains(v.preset.SectorPrefab));
            var variant = Pick(validVariants, v => v.weight, random);
            usedPrefabs.Add(variant.preset.SectorPrefab);
            var candidates = rooms.FindAll(candidateRoom => distances.TryGetValue(candidateRoom.coord, out int distance) &&
                distance >= pool.minimumStartDistance && (pool.maximumStartDistance == 0 || distance <= pool.maximumStartDistance) &&
                MatchesDirection(candidateRoom.coord - layout.startSectorCoord, pool.direction));
            if (candidates.Count == 0)
            { error = $"특별 섹터 '{pool.id}'의 거리/방향에 맞는 빈 방이 없습니다. seed={layout.runSeed}, 최소거리={pool.minimumStartDistance}, 최대거리={pool.maximumStartDistance}, 방향={pool.direction}"; return false; }
            var room = planned != null ? planned[selectedIndex] : candidates[random.Next(candidates.Count)]; rooms.Remove(room);
            room.specialSectorPoolId = pool.id;
            room.specialSectorVariantId = variant.id;
            room.hasInternalCombatLock = pool.lockedCombatArea;
            room.internalCombatKeyTier = pool.keyTier;
            assignedCount++;
        }
        if (assignedCount < minimum)
        { error = $"특별맵 강제 최소 {minimum}개 중 {assignedCount}개만 배치할 수 있습니다. 배치 조건 또는 강제 최소를 조정하세요."; return false; }
        return true;
    }

    // Bipartite matching prevents an unrestricted room from stealing the only location
    // available to a directional/nearby mandatory type. No recursion or scene state.
    private static bool TryPlanConstrainedRooms(List<SpecialSectorPool> selected, List<StageRoomNode> rooms,
        Dictionary<Vector2Int, int> distances, Vector2Int start, System.Random random,
        out StageRoomNode[] assigned, out string error, int mandatoryCount, bool allowOptionalShortfall)
    {
        assigned = new StageRoomNode[selected.Count]; error = null;
        var candidates = new Dictionary<SpecialSectorPool, List<StageRoomNode>>();
        foreach (var pool in selected)
        {
            if (candidates.ContainsKey(pool)) continue;
            var choices = rooms.FindAll(node => distances.TryGetValue(node.coord, out int d) &&
                d >= pool.minimumStartDistance && (pool.maximumStartDistance == 0 || d <= pool.maximumStartDistance) &&
                MatchesDirection(node.coord - start, pool.direction));
            for (int i = choices.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); (choices[i], choices[j]) = (choices[j], choices[i]); }
            candidates.Add(pool, choices);
        }
        var order = new List<int>();
        for (int i = 0; i < selected.Count; i++) order.Add(i);
        order.Sort((a, b) => candidates[selected[a]].Count != candidates[selected[b]].Count
            ? candidates[selected[a]].Count.CompareTo(candidates[selected[b]].Count) : a.CompareTo(b));
        if (allowOptionalShortfall)
            order.Sort((a, b) => (a < mandatoryCount) != (b < mandatoryCount)
                ? (a < mandatoryCount ? -1 : 1)
                : candidates[selected[a]].Count != candidates[selected[b]].Count
                    ? candidates[selected[a]].Count.CompareTo(candidates[selected[b]].Count) : a.CompareTo(b));
        var owners = new Dictionary<StageRoomNode, int>();
        foreach (int requested in order)
        {
            var queue = new Queue<int>(); queue.Enqueue(requested);
            var visitedSlots = new HashSet<int> { requested };
            var previousSlot = new Dictionary<StageRoomNode, int>();
            StageRoomNode free = null;
            while (queue.Count > 0 && free == null)
            {
                int slot = queue.Dequeue();
                foreach (var choice in candidates[selected[slot]])
                {
                    if (previousSlot.ContainsKey(choice)) continue;
                    previousSlot[choice] = slot;
                    if (!owners.TryGetValue(choice, out int owner)) { free = choice; break; }
                    if (visitedSlots.Add(owner)) queue.Enqueue(owner);
                }
            }
            if (free == null)
            {
                if (allowOptionalShortfall && requested >= mandatoryCount) continue;
                error = $"특별맵 '{selected[requested].id}': 필수 배치의 방향/거리 조건을 만족할 방이 없습니다. 최소/최대 개수 또는 거리/방향을 조정하세요."; return false;
            }
            while (free != null)
            {
                int slot = previousSlot[free];
                StageRoomNode previous = assigned[slot];
                assigned[slot] = free; owners[free] = slot; free = previous;
            }
        }
        return true;
    }

    private static bool MatchesDirection(Vector2Int delta, SpecialSectorDirection direction) => direction switch
    {
        SpecialSectorDirection.North => delta.y > 0,
        SpecialSectorDirection.South => delta.y < 0,
        SpecialSectorDirection.East => delta.x > 0,
        SpecialSectorDirection.West => delta.x < 0,
        _ => true
    };

    // Resuming ignores current probabilities and enabled flag. Missing IDs fail, never silently reroll.
    public bool TryResolve(StageRoomNode room, out SectorPresetSO preset)
    {
        preset = null;
        foreach (var pool in pools ?? new List<SpecialSectorPool>())
            if (pool != null && pool.id == room.specialSectorPoolId)
                foreach (var variant in pool.variants ?? new List<SpecialSectorVariant>())
                    if (variant != null && variant.id == room.specialSectorVariantId && variant.preset != null &&
                        variant.preset.IsValid && variant.preset.RoomType == StageRoomType.Normal)
                    { preset = variant.preset; return true; }
        return false;
    }
    private static bool IsValid(SpecialSectorVariant variant) => variant != null && variant.weight > 0 &&
        variant.preset != null && variant.preset.IsValid && variant.preset.RoomType == StageRoomType.Normal;
    private static T Pick<T>(List<T> rows, Func<T, int> weight, System.Random random)
    {
        double total = 0;
        foreach (var row in rows) total += Math.Max(0, weight(row));
        double roll = random.NextDouble() * total;
        foreach (var row in rows) { roll -= Math.Max(0, weight(row)); if (roll < 0) return row; }
        return rows[rows.Count - 1];
    }
}
