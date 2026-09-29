using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Shared authoring operations. QuestBranch values and runtime acceptance rules stay unchanged.
public static class QuestMakerGraph
{
    public static readonly QuestBranch[] Branches = { QuestBranch.Main, QuestBranch.Stats, QuestBranch.Item };
    public static string BranchLabel(QuestBranch branch) => branch switch
    { QuestBranch.Main => "MainQuest", QuestBranch.Stats => "StatUnlock", QuestBranch.Item => "GearUnlock", _ => branch.ToString() };

    public static List<QuestDefinitionSO> Unique(IEnumerable<QuestDefinitionSO> quests)
    {
        var result = new List<QuestDefinitionSO>(); var seen = new HashSet<QuestDefinitionSO>();
        if (quests != null) foreach (var quest in quests) if (quest != null && seen.Add(quest)) result.Add(quest);
        return result;
    }

    public sealed class Layout
    {
        public readonly List<QuestDefinitionSO> Nodes = new();
        public readonly HashSet<QuestDefinitionSO> Focus = new();
        public readonly Dictionary<QuestDefinitionSO, Rect> Rects = new();
        public readonly Dictionary<QuestBranch, Rect> Lanes = new();
        public Vector2 Size = new(300, 200);
    }

    public static Layout Build(IEnumerable<QuestDefinitionSO> line, QuestBranch? filter, string search)
    {
        var result = new Layout();
        search = search?.Trim();
        foreach (var quest in Unique(line))
            if ((!filter.HasValue || quest.Branch == filter.Value) && (string.IsNullOrEmpty(search) ||
                quest.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                quest.QuestId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                quest.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
            { result.Focus.Add(quest); result.Nodes.Add(quest); }
        // Retain every ancestor as context. Filtering must not hide cross-branch/line requirements.
        var seen = new HashSet<QuestDefinitionSO>(result.Nodes);
        for (int i = 0; i < result.Nodes.Count; i++)
            foreach (var parent in result.Nodes[i].Prerequisites)
                if (parent != null && seen.Add(parent)) result.Nodes.Add(parent);
        var depths = new Dictionary<QuestDefinitionSO, int>();
        foreach (var quest in result.Nodes) Depth(quest, seen, depths, new HashSet<QuestDefinitionSO>());
        float y = 24;
        var branches = new List<QuestBranch>(Branches);
        foreach (var quest in result.Nodes) if (!branches.Contains(quest.Branch)) branches.Add(quest.Branch);
        foreach (var branch in branches)
        {
            var rows = new Dictionary<int, int>(); int maxRows = 0;
            foreach (var quest in result.Nodes)
            {
                if (quest.Branch != branch) continue;
                int depth = depths[quest]; rows.TryGetValue(depth, out int row);
                result.Rects[quest] = new Rect(24 + depth * 292, y + 38 + row * 160, 236, 132);
                rows[depth] = row + 1; maxRows = Mathf.Max(maxRows, row + 1);
                result.Size.x = Mathf.Max(result.Size.x, 48 + depth * 292 + 236);
            }
            if (maxRows == 0) continue;
            float height = 52 + maxRows * 160;
            result.Lanes[branch] = new Rect(8, y, 1, height);
            y += height + 24;
        }
        result.Size.y = Mathf.Max(200, y);
        return result;
    }

    private static int Depth(QuestDefinitionSO quest, HashSet<QuestDefinitionSO> nodes,
        Dictionary<QuestDefinitionSO, int> depths, HashSet<QuestDefinitionSO> path)
    {
        if (depths.TryGetValue(quest, out int depth)) return depth;
        if (!path.Add(quest)) return 0; // Existing corrupt cycles remain drawable and can be unlinked.
        foreach (var parent in quest.Prerequisites)
            if (parent != null && nodes.Contains(parent)) depth = Mathf.Max(depth, Depth(parent, nodes, depths, path) + 1);
        path.Remove(quest); depths[quest] = depth; return depth;
    }

    public static bool WouldCreateCycle(QuestDefinitionSO source, QuestDefinitionSO target,
        QuestDefinitionSO ignoredSource = null, QuestDefinitionSO ignoredTarget = null)
    {
        if (source == null || target == null || source == target) return true;
        var stack = new Stack<QuestDefinitionSO>(); var seen = new HashSet<QuestDefinitionSO>(); stack.Push(source);
        while (stack.Count > 0)
        {
            var current = stack.Pop(); if (!seen.Add(current)) continue;
            foreach (var parent in current.Prerequisites)
            {
                if (current == ignoredTarget && parent == ignoredSource) continue;
                if (parent == target) return true;
                if (parent != null) stack.Push(parent);
            }
        }
        return false;
    }

    public static bool TryLink(QuestDefinitionSO source, QuestDefinitionSO target, out string message)
    {
        if (WouldCreateCycle(source, target)) { message = "Cannot link: missing quest, self-link or circular dependency."; return false; }
        if (Array.IndexOf(target.Prerequisites, source) >= 0) { message = "This link already exists."; return false; }
        using var serialized = new SerializedObject(target);
        var links = serialized.FindProperty("_prerequisites");
        Undo.RecordObject(target, "Link Quest Forward");
        links.arraySize++; links.GetArrayElementAtIndex(links.arraySize - 1).objectReferenceValue = source;
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(target);
        message = source.DisplayName + " -> " + target.DisplayName;
        return true;
    }

    public static void RemoveReferenceAt(SerializedProperty array, int index)
    {
        // Unity's first deletion of an object reference can leave a null entry instead of removing it.
        array.GetArrayElementAtIndex(index).objectReferenceValue = null;
        array.DeleteArrayElementAtIndex(index);
    }

    // Validate before touching either endpoint. Cancellation/rejection never loses the original edge.
    public static bool TryMoveLink(QuestDefinitionSO oldSource, QuestDefinitionSO oldTarget,
        QuestDefinitionSO newSource, QuestDefinitionSO newTarget, out string message)
    {
        if (oldSource == null || oldTarget == null || Array.IndexOf(oldTarget.Prerequisites, oldSource) < 0)
        { message = "The original connection no longer exists."; return false; }
        if (oldSource == newSource && oldTarget == newTarget)
        { message = "Choose a different quest, or press Escape to cancel."; return false; }
        if (WouldCreateCycle(newSource, newTarget, oldSource, oldTarget))
        { message = "Cannot move: missing quest, self-link or circular dependency. Original link kept."; return false; }
        if (Array.IndexOf(newTarget.Prerequisites, newSource) >= 0)
        { message = "That connection already exists. Original link kept."; return false; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Move Quest Connection");
        try
        {
            Unlink(oldSource, oldTarget);
            using var serialized = new SerializedObject(newTarget);
            var links = serialized.FindProperty("_prerequisites");
            Undo.RecordObject(newTarget, "Move Quest Connection");
            links.arraySize++;
            links.GetArrayElementAtIndex(links.arraySize - 1).objectReferenceValue = newSource;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(newTarget);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
        finally { Undo.IncrementCurrentGroup(); }
        message = newSource.DisplayName + " -> " + newTarget.DisplayName;
        return true;
    }

    public static Vector2 ConnectionPoint(Rect source, Rect target, float t)
    {
        Vector2 a = new(source.xMax, source.center.y), d = new(target.xMin, target.center.y);
        Vector2 b = a + Vector2.right * 30, c = d - Vector2.right * 30;
        float u = 1 - t;
        return u*u*u*a + 3*u*u*t*b + 3*u*t*t*c + t*t*t*d;
    }

    // Screen-space tolerance stays usable at every zoom level; matches the drawn cubic curve.
    public static float DistanceToConnection(Vector2 point, Rect source, Rect target)
    {
        Vector2 previous = ConnectionPoint(source, target, 0);
        float distance = float.MaxValue;
        for (int i = 1; i <= 48; i++)
        {
            Vector2 next = ConnectionPoint(source, target, i / 48f);
            Vector2 segment = next - previous;
            float t = segment.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector2.Dot(point - previous, segment) / segment.sqrMagnitude) : 0;
            distance = Mathf.Min(distance, Vector2.Distance(point, previous + segment * t));
            previous = next;
        }
        return distance;
    }

    public static bool Unlink(QuestDefinitionSO source, QuestDefinitionSO target)
    {
        if (target == null) return false;
        using var serialized = new SerializedObject(target);
        var links = serialized.FindProperty("_prerequisites"); bool changed = false;
        Undo.RecordObject(target, "Unlink Quest");
        for (int i = links.arraySize - 1; i >= 0; i--)
            if (links.GetArrayElementAtIndex(i).objectReferenceValue == source)
            { RemoveReferenceAt(links, i); changed = true; }
        if (changed) { serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(target); }
        return changed;
    }

    public static bool AddToLine(QuestLineSO line, QuestDefinitionSO quest)
    {
        if (line == null || quest == null || Array.IndexOf(line.Quests, quest) >= 0) return false;
        using var serialized = new SerializedObject(line); var quests = serialized.FindProperty("_quests");
        Undo.RecordObject(line, "Add Quest To Line");
        quests.arraySize++; quests.GetArrayElementAtIndex(quests.arraySize - 1).objectReferenceValue = quest;
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(line); return true;
    }

    public static void RemoveFromLine(QuestLineSO line, QuestDefinitionSO quest)
    {
        if (line == null || quest == null) return;
        using var serialized = new SerializedObject(line); var quests = serialized.FindProperty("_quests");
        Undo.RecordObject(line, "Remove Quest From Line");
        for (int i = quests.arraySize - 1; i >= 0; i--)
            if (quests.GetArrayElementAtIndex(i).objectReferenceValue == quest) RemoveReferenceAt(quests, i);
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(line);
        // Deliberately keep the quest asset and all incoming/outgoing prerequisite links.
    }

    public static void SetBranch(QuestDefinitionSO quest, QuestBranch branch)
    {
        if (quest == null || quest.Branch == branch) return;
        using var serialized = new SerializedObject(quest);
        Undo.RecordObject(quest, "Change Quest Branch");
        serialized.FindProperty("_branch").intValue = (int)branch;
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(quest);
    }
}
