using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class QuestMakerTests
{
    private readonly List<Object> _created = new();
    private T Asset<T>() where T : ScriptableObject { var value=ScriptableObject.CreateInstance<T>(); _created.Add(value); return value; }
    private QuestDefinitionSO Quest(string name,QuestBranch branch=QuestBranch.Main)
    {
        var quest=Asset<QuestDefinitionSO>(); quest.name=name;
        using var serialized=new SerializedObject(quest);
        serialized.FindProperty("_questId").stringValue=name;
        serialized.FindProperty("_displayName").stringValue=name;
        serialized.FindProperty("_branch").intValue=(int)branch;
        serialized.ApplyModifiedPropertiesWithoutUndo(); return quest;
    }
    private static void RawParents(QuestDefinitionSO target,params QuestDefinitionSO[] parents)
    {
        using var serialized=new SerializedObject(target); var property=serialized.FindProperty("_prerequisites");
        property.arraySize=parents.Length;
        for(int i=0;i<parents.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue=parents[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    [TearDown] public void Cleanup()
    {
        for(int i=_created.Count-1;i>=0;i--) if(_created[i]!=null) { Undo.ClearUndo(_created[i]); Object.DestroyImmediate(_created[i]); }
        _created.Clear();
    }

    [Test] public void BranchLabelsPreserveExistingRuntimeValues()
    {
        Assert.That((int)QuestBranch.Main,Is.EqualTo(0)); Assert.That((int)QuestBranch.Item,Is.EqualTo(1)); Assert.That((int)QuestBranch.Stats,Is.EqualTo(2));
        Assert.That(QuestMakerGraph.BranchLabel(QuestBranch.Main),Is.EqualTo("MainQuest"));
        Assert.That(QuestMakerGraph.BranchLabel(QuestBranch.Stats),Is.EqualTo("StatUnlock"));
        Assert.That(QuestMakerGraph.BranchLabel(QuestBranch.Item),Is.EqualTo("GearUnlock"));
    }

    [Test] public void FilterShowsBranchWithExternalAncestorContextWithoutMutatingData()
    {
        var main=Quest("main"); var stat=Quest("stat",QuestBranch.Stats); var gear=Quest("gear",QuestBranch.Item);
        RawParents(stat,main); RawParents(gear,stat);
        string before=EditorJsonUtility.ToJson(gear);
        var layout=QuestMakerGraph.Build(new[]{gear},QuestBranch.Item,null);
        Assert.That(layout.Focus.SetEquals(new[]{gear}),Is.True);
        Assert.That(layout.Nodes,Is.EquivalentTo(new[]{main,stat,gear}));
        Assert.That(layout.Rects[main].x,Is.LessThan(layout.Rects[stat].x));
        Assert.That(layout.Rects[stat].x,Is.LessThan(layout.Rects[gear].x));
        Assert.That(EditorJsonUtility.ToJson(gear),Is.EqualTo(before));
    }

    [Test] public void SearchRetainsAncestorsAndNeverPullsUnrelatedQuests()
    {
        var a=Quest("A"); var b=Quest("Long rifle",QuestBranch.Item); var unrelated=Quest("Unrelated"); RawParents(b,a);
        var layout=QuestMakerGraph.Build(new[]{a,b,unrelated},null,"RIFLE");
        Assert.That(layout.Focus.SetEquals(new[]{b}),Is.True); CollectionAssert.DoesNotContain(layout.Nodes,unrelated);
        Assert.That(QuestMakerGraph.Build(null,null,null).Nodes,Is.Empty);
    }

    [Test] public void LayoutIsDeterministicAndBranchesDoNotOverlap()
    {
        var quests=new List<QuestDefinitionSO>();
        foreach(var branch in QuestMakerGraph.Branches) for(int i=0;i<7;i++) quests.Add(Quest(branch+"_"+i,branch));
        var layout=QuestMakerGraph.Build(quests,null,null); var repeat=QuestMakerGraph.Build(quests,null,null);
        Assert.That(layout.Lanes.Count,Is.EqualTo(3));
        for(int i=0;i<quests.Count;i++)
        {
            Assert.That(layout.Rects[quests[i]],Is.EqualTo(repeat.Rects[quests[i]]));
            for(int j=i+1;j<quests.Count;j++) Assert.That(layout.Rects[quests[i]].Overlaps(layout.Rects[quests[j]]),Is.False);
        }
    }

    [Test] public void LinkAddsOnlyForwardPrerequisiteAndKeepsOtherInputs()
    {
        var a=Quest("A"); var b=Quest("B"); var c=Quest("C"); RawParents(c,b);
        Assert.That(QuestMakerGraph.TryLink(a,c,out _),Is.True);
        Assert.That(a.Prerequisites,Is.Empty); Assert.That(c.Prerequisites,Is.EquivalentTo(new[]{a,b}));
        Assert.That(QuestMakerGraph.TryLink(a,c,out _),Is.False);
    }

    [Test] public void SelfAndIndirectCyclesAreBlockedIncludingOutsideSet()
    {
        var a=Quest("A"); var b=Quest("B"); var c=Quest("C"); RawParents(b,a); RawParents(c,b);
        Assert.That(QuestMakerGraph.TryLink(c,a,out _),Is.False);
        Assert.That(QuestMakerGraph.TryLink(a,a,out _),Is.False);
        Assert.That(QuestMakerGraph.TryLink(null,a,out _),Is.False);
        Assert.That(a.Prerequisites,Is.Empty);
    }

    [Test] public void ExistingCyclesAndNullEntriesCanBeDisplayedAndRepaired()
    {
        var a=Quest("A"); var b=Quest("B"); RawParents(a,b,null); RawParents(b,a);
        var layout=QuestMakerGraph.Build(new[]{a,a,null,b},null,null);
        Assert.That(layout.Nodes.Count,Is.EqualTo(2));
        Assert.That(QuestMakerGraph.Unlink(b,a),Is.True);
        Assert.That(QuestMakerGraph.Unlink(null,a),Is.True);
        Assert.That(a.Prerequisites,Is.Empty);
    }

    [Test] public void RemoveFromSetKeepsAssetAndLinksButLeavesNoNullSlots()
    {
        var line=Asset<QuestLineSO>(); var a=Quest("A"); var b=Quest("B"); RawParents(b,a);
        QuestMakerGraph.AddToLine(line,a); QuestMakerGraph.AddToLine(line,b);
        Assert.That(QuestMakerGraph.AddToLine(line,a),Is.False);
        QuestMakerGraph.RemoveFromLine(line,a);
        Assert.That(line.Quests,Is.EqualTo(new[]{b})); Assert.That(b.Prerequisites,Is.EqualTo(new[]{a})); Assert.That(a,Is.Not.Null);
        Assert.That(QuestMakerGraph.Build(line.Quests,null,null).Nodes,Does.Contain(a));
    }

    [Test] public void BranchEditPreservesQuestIdObjectivesRewardsAndLinks()
    {
        var a=Quest("A"); var b=Quest("B"); RawParents(b,a);
        using(var serialized=new SerializedObject(b))
        {
            serialized.FindProperty("_objectives").arraySize=1;
            serialized.FindProperty("_objectives").GetArrayElementAtIndex(0).FindPropertyRelative("_requiredCount").intValue=42;
            serialized.FindProperty("_rewards").arraySize=1;
            serialized.FindProperty("_rewards").GetArrayElementAtIndex(0).FindPropertyRelative("_statValue").floatValue=13;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        var before=EditorJsonUtility.ToJson(b);
        QuestMakerGraph.SetBranch(b,QuestBranch.Stats);
        Assert.That(b.Branch,Is.EqualTo(QuestBranch.Stats));
        Assert.That(b.Prerequisites,Is.EqualTo(new[]{a})); Assert.That(b.Objectives[0].RequiredCount,Is.EqualTo(42)); Assert.That(b.Rewards[0].StatValue,Is.EqualTo(13));
        QuestMakerGraph.SetBranch(b,QuestBranch.Main);
        Assert.That(EditorJsonUtility.ToJson(b),Is.EqualTo(before));
    }

    [Test] public void LinkAndUnlinkSupportUndoRedo()
    {
        var a=Quest("A"); var b=Quest("B");
        QuestMakerGraph.TryLink(a,b,out _); Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(b.Prerequisites,Is.Empty);
        Undo.PerformRedo(); Assert.That(b.Prerequisites,Is.EqualTo(new[]{a}));
        QuestMakerGraph.Unlink(a,b); Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(b.Prerequisites,Is.EqualTo(new[]{a}));
    }

    [Test] public void WindowOpensWithNewTitleAndStableClass()
    {
        var window=Asset<QuestDesignTreeWindow>();
        Assert.That(window.titleContent.text,Is.EqualTo("Quest Maker"));
        Assert.That(window.GetType().Name,Is.EqualTo("QuestDesignTreeWindow"));
    }

    [Test] public void MoveStartPreservesOtherPrerequisitesAcrossBranches()
    {
        var a=Quest("A"); var b=Quest("B",QuestBranch.Item); var c=Quest("C",QuestBranch.Stats); var other=Quest("Other");
        RawParents(b,a,other);
        Assert.That(QuestMakerGraph.TryMoveLink(a,b,c,b,out _),Is.True);
        Assert.That(b.Prerequisites,Is.EquivalentTo(new[]{c,other}));
        Assert.That(b.Branch,Is.EqualTo(QuestBranch.Item)); Assert.That(a.Prerequisites,Is.Empty);
    }

    [Test] public void MoveEndIsOneUndoAndRedoAcrossBothTargets()
    {
        var a=Quest("A"); var b=Quest("B"); var c=Quest("C"); var other=Quest("Other");
        RawParents(b,a,other); RawParents(c,other);
        Assert.That(QuestMakerGraph.TryMoveLink(a,b,a,c,out _),Is.True);
        Assert.That(b.Prerequisites,Is.EqualTo(new[]{other})); Assert.That(c.Prerequisites,Is.EquivalentTo(new[]{other,a}));
        Undo.PerformUndo();
        Assert.That(b.Prerequisites,Is.EqualTo(new[]{a,other})); Assert.That(c.Prerequisites,Is.EqualTo(new[]{other}));
        Undo.PerformRedo();
        Assert.That(b.Prerequisites,Is.EqualTo(new[]{other})); Assert.That(c.Prerequisites,Is.EquivalentTo(new[]{other,a}));
    }

    [Test] public void RejectedMovesKeepOriginalAndOtherLinksUnchanged()
    {
        var a=Quest("A"); var b=Quest("B"); var c=Quest("C"); RawParents(b,a); RawParents(c,b,a);
        string before=EditorJsonUtility.ToJson(a)+EditorJsonUtility.ToJson(b)+EditorJsonUtility.ToJson(c);
        Assert.That(QuestMakerGraph.TryMoveLink(a,b,c,b,out _),Is.False); // B -> C -> B
        Assert.That(QuestMakerGraph.TryMoveLink(a,b,a,c,out _),Is.False); // already exists
        Assert.That(QuestMakerGraph.TryMoveLink(a,b,b,b,out _),Is.False); // self
        Assert.That(QuestMakerGraph.TryMoveLink(a,b,a,b,out _),Is.False); // unchanged
        Assert.That(QuestMakerGraph.TryMoveLink(a,b,null,b,out _),Is.False);
        Assert.That(QuestMakerGraph.TryMoveLink(c,a,a,b,out _),Is.False); // stale original
        Assert.That(EditorJsonUtility.ToJson(a)+EditorJsonUtility.ToJson(b)+EditorJsonUtility.ToJson(c),Is.EqualTo(before));
    }

    [Test] public void MoveCanRepairAnExistingCycle()
    {
        var a=Quest("A"); var b=Quest("B"); var c=Quest("C"); RawParents(b,a); RawParents(a,b);
        Assert.That(QuestMakerGraph.TryMoveLink(a,b,c,b,out _),Is.True);
        Assert.That(b.Prerequisites,Is.EqualTo(new[]{c}));
        Assert.That(a.Prerequisites,Is.EqualTo(new[]{b}));
    }

    [Test] public void PendingMoveAndEscapeDoNotEditTheAsset()
    {
        var a=Quest("A"); var b=Quest("B"); RawParents(b,a);
        var window=Asset<QuestDesignTreeWindow>(); var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        typeof(QuestDesignTreeWindow).GetMethod("BeginMoveConnection",flags).Invoke(window,new object[]{a,b,true});
        Assert.That(b.Prerequisites,Is.EqualTo(new[]{a}));
        typeof(QuestDesignTreeWindow).GetMethod("CancelConnection",flags).Invoke(window,null);
        Assert.That(b.Prerequisites,Is.EqualTo(new[]{a}));
        Assert.That(typeof(QuestDesignTreeWindow).GetProperty("HasPendingConnection",flags).GetValue(window),Is.False);
    }

    [TestCase(1080f,500f,800f)]
    [TestCase(1600f,450f,500f)]
    [TestCase(1080f,0f,0f)]
    public void PanelConstraintsReserveBothSidebarsAndUsableGraph(float width,float left,float right)
    {
        QuestDesignTreeWindow.ConstrainPanelWidths(width,ref left,ref right);
        Assert.That(left,Is.GreaterThanOrEqualTo(196)); Assert.That(right,Is.GreaterThanOrEqualTo(300));
        Assert.That(width-left-right-12,Is.GreaterThanOrEqualTo(300));
    }

    [Test] public void CurvedConnectionPickingUsesActualScreenGeometry()
    {
        var from=new Rect(10,20,200,100); var to=new Rect(510,320,200,100);
        var point=QuestMakerGraph.ConnectionPoint(from,to,.5f);
        Assert.That(QuestMakerGraph.DistanceToConnection(point,from,to),Is.LessThan(.1f));
        Assert.That(QuestMakerGraph.DistanceToConnection(point+Vector2.up*4,from,to),Is.LessThan(9));
        Assert.That(QuestMakerGraph.DistanceToConnection(new Vector2(350,600),from,to),Is.GreaterThan(9));
    }

    [Test] public void ValidationReportsBrokenEntriesAndExternalPrerequisitesWithoutChangingData()
    {
        var line=Asset<QuestLineSO>(); var a=Quest("A"); var b=Quest("B"); RawParents(b,a,null);
        using(var serialized=new SerializedObject(line))
        {
            var entries=serialized.FindProperty("_quests"); entries.arraySize=3;
            entries.GetArrayElementAtIndex(0).objectReferenceValue=b;
            entries.GetArrayElementAtIndex(1).objectReferenceValue=null;
            entries.GetArrayElementAtIndex(2).objectReferenceValue=b;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        var window=Asset<QuestDesignTreeWindow>();
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        typeof(QuestDesignTreeWindow).GetField("_questLine",flags).SetValue(window,line);
        string before=EditorJsonUtility.ToJson(line)+EditorJsonUtility.ToJson(b);
        typeof(QuestDesignTreeWindow).GetMethod("ValidateQuestLine",flags).Invoke(window,null);
        var issues=(List<string>)typeof(QuestDesignTreeWindow).GetField("_lineValidationIssues",flags).GetValue(window);
        Assert.That(issues.Any(i=>i.Contains("Quest entry 2 is missing")),Is.True);
        Assert.That(issues.Any(i=>i.Contains("duplicated set entry")),Is.True);
        Assert.That(issues.Any(i=>i.Contains("missing prerequisite")),Is.True);
        Assert.That(issues.Any(i=>i.StartsWith("A: no objective")),Is.True);
        Assert.That(EditorJsonUtility.ToJson(line)+EditorJsonUtility.ToJson(b),Is.EqualTo(before));
    }
}
