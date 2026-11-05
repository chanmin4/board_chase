using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class InfectionSpreadSaveData
{
    public bool initialized;
    public float remainingSeconds;
    public int step;
}

/// <summary>One stage timer and a simultaneous graph frontier. Newly infected rooms wait until the next pulse.</summary>
public static class StageGasSpread
{
    public static void Initialize(RaidRunSaveData state, StageEncounterRules rules)
    {
        if (state == null || rules == null) return;
        var clock = state.infectionSpread ??= new();
        if (clock.initialized) return;
        clock.initialized = true;
        clock.remainingSeconds = Mathf.Max(1, rules.infectionSpreadIntervalSeconds);
    }

    public static int Tick(RaidRunSaveData state, StageEncounterRules rules, float delta)
    {
        if (state == null || !state.active || state.extractionCommitted || state.layout == null ||
            rules == null || !rules.infectionSpreadEnabled) return 0;
        Initialize(state, rules);
        var clock = state.infectionSpread;
        if (!RuntimeCountdown.Tick(ref clock.remainingSeconds, delta)) return 0;
        // As in the old countdown, finish one pulse then reset together. No offline/long-frame avalanche.
        clock.remainingSeconds = Mathf.Max(1, rules.infectionSpreadIntervalSeconds);
        int step = clock.step++;
        return Spread(state.layout, step, rules.maximumNewInfectedRoomsPerTick);
    }

    public static int Spread(StageMapLayout layout, int step, int maximumNewRooms = 0)
    {
        if (layout?.rooms == null) return 0;
        var sources = new List<StageRoomNode>();
        foreach (var room in layout.rooms) if (room != null && room.virusRoom) sources.Add(room);
        sources.Sort((a,b) => a.coord.y != b.coord.y ? a.coord.y.CompareTo(b.coord.y) : a.coord.x.CompareTo(b.coord.x));
        var random = new System.Random(StageDefinitionSO.BuildSectorSeed(layout.stageSeed,new Vector2Int(step,0),281028));
        var chosen = new List<StageRoomNode>();
        var claimed = new HashSet<Vector2Int>();
        var directions = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        var flags = new[] { StageRoomConnectionFlags.Left, StageRoomConnectionFlags.Right, StageRoomConnectionFlags.Up, StageRoomConnectionFlags.Down };
        var opposite = new[] { StageRoomConnectionFlags.Right, StageRoomConnectionFlags.Left, StageRoomConnectionFlags.Down, StageRoomConnectionFlags.Up };
        foreach (var source in sources)
        {
            var candidates = new List<StageRoomNode>();
            for (int i=0;i<directions.Length;i++)
                if (source.HasConnection(flags[i]) && layout.TryGetRoom(source.coord+directions[i],out var next) &&
                    !next.virusRoom && next.HasConnection(opposite[i])) candidates.Add(next);
            if (candidates.Count == 0) continue;
            var target = candidates[random.Next(candidates.Count)];
            if (claimed.Add(target.coord)) chosen.Add(target);
        }
        for (int i=chosen.Count-1;i>0;i--) { int j=random.Next(i+1); (chosen[i],chosen[j])=(chosen[j],chosen[i]); }
        int count = maximumNewRooms > 0 ? Mathf.Min(maximumNewRooms,chosen.Count) : chosen.Count;
        for (int i=0;i<count;i++) chosen[i].virusRoom = true;
        return count;
    }
}
