#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VSplatter.StateMachine.Debugging
{
    /// <summary>Editor-only observation of executed predicates, never an extra AI evaluation.</summary>
    public static class HfsmExecutionTrace
    {
        public sealed class Check
        {
            public string Name, Join;
            public bool Expected;
            public bool? Actual;
        }
        public sealed class Entry
        {
            public StateMachine Owner;
            public int Frame, Level, TargetLevel, Row, Priority;
            public float Time;
            public string From, To, Table, Rule;
            public bool Passed, Always;
            public StateExitReason ExitReason;
            public Check[] Checks;
        }
        public static bool Enabled { get; set; }
        private static readonly List<Entry> History = new();
        private static readonly Dictionary<int, List<Entry>> Latest = new();
        public static IReadOnlyList<Entry> Transitions => History;
        public static IReadOnlyList<Entry> LatestChecks(StateMachine owner) => owner != null &&
            Latest.TryGetValue(owner.GetInstanceID(), out var entries) ? entries : Array.Empty<Entry>();
        public static void Clear() { History.Clear(); Latest.Clear(); }
        internal static void Commit(Entry entry)
        {
            if (!Enabled || entry == null || entry.Owner == null) return;
            int key = entry.Owner.GetInstanceID();
            if (!Latest.TryGetValue(key, out var entries))
            {
                if (Latest.Count >= 64) Latest.Clear();
                Latest[key] = entries = new List<Entry>();
            }
            if (entries.Count > 0 && entries[0].Frame != entry.Frame) entries.Clear();
            if (entries.Count < 128) entries.Add(entry);
            if (!entry.Passed) return;
            History.Add(entry);
            if (History.Count > 512) History.RemoveAt(0);
        }
    }
}
#endif
