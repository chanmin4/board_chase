#Requires -Version 7
param([string]$ProjectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference='Stop'
# Executes the production partial's gating/group ownership/action dispatch with managed data shims.
# Not the complete QuestRuntime, actual NPC turn-in UI, ES3, or Play Mode.
$shim=@'
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {public class Placeholder {}}
public class ItemSO {}
public enum QuestObjectiveType {Action=6,Craft=7,SectorClear=8,LandmarkClear=10}
public class QuestObjectiveDefinition {public QuestObjectiveType ObjectiveType;public string ActionId;public ItemSO TargetItem;public int RequiredCount=1;}
public class QuestRewardDefinition {}
public class QuestDefinitionSO {public string QuestId,SharedGrowthRewardGroup;public int[] RequiredVisitedStages=Array.Empty<int>();public QuestObjectiveDefinition[] Objectives=Array.Empty<QuestObjectiveDefinition>();}
public class Save {public List<int> visitedStageIndices=new();public List<string> unlockedCraftRecipeIds=new(),sharedQuestRewardGroups=new();}
public class SaveSystem {public static SaveSystem Active=new();public Save Profile=new();}
public class QuestState {
 public bool IsAccepted=true,IsCompleted;public QuestDefinitionSO Definition;public int[] ObjectiveProgress;
 public bool TrySetProgress(int i,int amount){if(ObjectiveProgress[i]==amount)return false;ObjectiveProgress[i]=amount;return true;}
}
public sealed partial class QuestRuntime {
 static QuestRuntime _instance;
 readonly List<QuestState> _states=new();
 readonly HashSet<string> granted=new();public int Grants,Notifications;public bool FailGrant;public Action DuringGrant;
 public QuestRuntime(){_instance=this;}
 bool TryGrantPersistentStatReward(QuestDefinitionSO q,int i,QuestRewardDefinition r){
  if(FailGrant)return false;DuringGrant?.Invoke();if(granted.Add(q.QuestId+":"+i))Grants++;return true;
 }
 void NotifyStateChanged()=>Notifications++;
 public bool Grant(QuestDefinitionSO q,int i)=>GrantSharedStat(q,i,new());
 public QuestState Add(QuestDefinitionSO q,bool completed=false){var s=new QuestState{Definition=q,IsCompleted=completed,ObjectiveProgress=new int[q.Objectives.Length]};_states.Add(s);return s;}
}
public static class QuestSystemsChecks {
 static int count;static void Check(bool yes,string name){if(!yes)throw new Exception(name);count++;}
 public static int Run(){
  var profile=SaveSystem.Active.Profile=new();
  var gate=new QuestDefinitionSO{RequiredVisitedStages=new[]{2,3}};
  Check(!QuestRuntime.HasVisitedStages(gate),"all-of gate empty");profile.visitedStageIndices.Add(2);
  Check(!QuestRuntime.HasVisitedStages(gate),"one branch is not two visited branches");profile.visitedStageIndices.Add(3);
  Check(QuestRuntime.HasVisitedStages(gate),"visited stages allow acceptance");
  Check(QuestRuntime.HasVisitedStages(new()),"empty gate retains legacy behavior");
  Check(!QuestRuntime.GrantCraftUnlock(" "),"blank craft unlock rejected");
  Check(QuestRuntime.GrantCraftUnlock("recipe")&&QuestRuntime.GrantCraftUnlock("recipe")&&profile.unlockedCraftRecipeIds.Count==1,"unlock idempotence");
  var runtime=new QuestRuntime();
  var a=new QuestDefinitionSO{QuestId="branch-a",SharedGrowthRewardGroup="growth-2-3"};
  var b=new QuestDefinitionSO{QuestId="branch-b",SharedGrowthRewardGroup="growth-2-3"};
  runtime.DuringGrant=()=>Check(profile.sharedQuestRewardGroups.Contains("growth-2-3|branch-a"),"owner visible before callbacks can save");
  Check(runtime.Grant(a,0)&&runtime.Grants==1,"first branch growth");
  Check(runtime.Grant(b,0)&&runtime.Grants==1,"other branch skipped");
  Check(runtime.Grant(a,0)&&runtime.Grants==1,"retry same reward idempotent");
  Check(runtime.Grant(a,1)&&runtime.Grants==2,"same quest remaining stat reward");
  runtime.DuringGrant=null;runtime.FailGrant=true;
  Check(!runtime.Grant(new(){QuestId="fail",SharedGrowthRewardGroup="unclaimed"},0)&&!profile.sharedQuestRewardGroups.Contains("unclaimed|fail"),"failed reward releases group");
  runtime.FailGrant=false;
  var action=new QuestDefinitionSO{QuestId="action",Objectives=new[]{new QuestObjectiveDefinition{ObjectiveType=QuestObjectiveType.Action,ActionId="download",RequiredCount=3}}};
  var state=runtime.Add(action);
  QuestRuntime.ReportAction("wrong");Check(state.ObjectiveProgress[0]==0,"wrong action ignored");
  QuestRuntime.ReportAction("download",2);Check(state.ObjectiveProgress[0]==2&&!state.IsCompleted,"action progress never auto turns in");
  QuestRuntime.ReportAction("download",int.MaxValue);Check(state.ObjectiveProgress[0]==3,"progress overflow clamps");
  var before=runtime.Notifications;QuestRuntime.ReportAction("download");Check(runtime.Notifications==before,"completed goal not repeatedly published");
  var completed=runtime.Add(action,true);QuestRuntime.ReportAction("download");Check(completed.ObjectiveProgress[0]==0,"turned-in quest not progressed");
  var item=new ItemSO();var craft=runtime.Add(new(){Objectives=new[]{new QuestObjectiveDefinition{ObjectiveType=QuestObjectiveType.Craft,TargetItem=item,RequiredCount=2}}});
  QuestRuntime.ReportAction("any",1,QuestObjectiveType.Action,item);Check(craft.ObjectiveProgress[0]==0,"event type isolated");
  QuestRuntime.ReportAction("any",1,QuestObjectiveType.Craft,new());Check(craft.ObjectiveProgress[0]==0,"wrong crafting item ignored");
  QuestRuntime.ReportAction("any",1,QuestObjectiveType.Craft,item);Check(craft.ObjectiveProgress[0]==1,"wildcard recipe plus exact item");
  Console.WriteLine("Quest systems managed checks passed: "+count);return count;
 }
}
'@
$source=(Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.Systems.cs') -Raw)-replace '(?m)^using [^\r\n]+;\r?\n',''
Add-Type -TypeDefinition ($shim+[Environment]::NewLine+$source)
[QuestSystemsChecks]::Run()
