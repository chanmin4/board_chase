param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference = 'Stop'
# Executes real module sources against managed storage/Unity shims. NOT a Unity Play Mode test.
function Read-Source([string]$path) { Get-Content -LiteralPath (Join-Path $ProjectRoot $path) -Raw }
function Member([string]$source, [string]$signature) {
    $start = $source.IndexOf($signature, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing $signature" }
    $brace = $source.IndexOf('{', $start); $depth = 0
    for ($i=$brace; $i -lt $source.Length; $i++) {
        if ($source[$i] -eq '{') {$depth++}
        if ($source[$i] -eq '}') {$depth--; if ($depth -eq 0) {return $source.Substring($start,$i-$start+1)}}
    }
    throw 'Unclosed member'
}
$shim = @'
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
namespace UnityEngine {
 public class Object { public string name; public HideFlags hideFlags; public static T Instantiate<T>(T value) where T:Object => (T)value.MemberwiseClone(); }
 public class ScriptableObject:Object {}
 public enum HideFlags { DontSave }
 public class HeaderAttribute:Attribute { public HeaderAttribute(string value){} }
 public class TooltipAttribute:Attribute { public TooltipAttribute(string value){} }
 public class MinAttribute:Attribute { public MinAttribute(float value){} }
 public class RangeAttribute:Attribute { public RangeAttribute(float a,float b){} }
 public class SerializeField:Attribute {}
 public class HideInInspector:Attribute {}
 public class CreateAssetMenuAttribute:Attribute { public string fileName,menuName; }
 public enum RuntimeInitializeLoadType { SubsystemRegistration }
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
 public static class Application { public static bool isPlaying=true; }
 public static class Resources { public static object Value; public static T Load<T>(string key) where T:class => Value as T; }
 public static class Debug { public static void LogError(object message, object context=null){} }
 public static class JsonUtility { public static string ToJson(object value)=>""; }
 public static class Mathf {
  public static bool Approximately(float a,float b)=>Math.Abs(a-b)<.000001f;
  public static float Clamp01(float v)=>Math.Clamp(v,0f,1f);
  public static float Lerp(float a,float b,float v)=>a+(b-a)*Clamp01(v);
  public static float Min(float a,float b)=>Math.Min(a,b); public static int Min(int a,int b)=>Math.Min(a,b);
  public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
  public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b);
  public static int CeilToInt(float v)=>(int)Math.Ceiling(v);
 }
}
public enum ItemKind { Misc,Passive }
public class RecyclingRecipe {public ItemKind kind;public PlayerShopItemRarity rarity;public ItemSO output;public int outputAmount;public float seconds;}
public class ItemSO:UnityEngine.ScriptableObject {
 public string Guid; public int Stack=1;
 protected void SetRuntimeGuid(string id)=>Guid=id;
 public virtual int MaxStack=>Stack;
 public virtual PlayerShopItemRarity Rarity=>PlayerShopItemRarity.Normal;
 public virtual ItemKind Kind=>ItemKind.Misc;
}
public class Save { public List<PassiveInstanceSaveData> passiveInstances=new(); }
public class RunSaveData { public List<PassiveInstanceSaveData> passiveInstances=new(); }
public class SaveSystem { public static SaveSystem Active=new(); public Save Profile=new(); public RunSaveData RunData=new(); }
public class RuntimeContentCatalogSO { public Dictionary<string,ItemSO> Items=new(); public bool TryResolveItem(string id,out ItemSO item)=>Items.TryGetValue(id,out item); }
public struct LootRollResult { public LootRollResult(ItemSO item,int amount,PlayerShopItemRarity rarity){} }
public class PlayerInventoryItemStack {
 public ItemSO Item; public int Amount;
 public bool IsEmpty=>Item==null||Amount==0;
 public void Set(ItemSO item,int amount){Item=item;Amount=amount;}
 public bool Remove(int amount){Amount-=amount;return Amount<=0;}
 public void Clear(){Item=null;Amount=0;}
}
public class PlayerPassiveInventoryRuntime {public int Destroyed,Refreshed; public List<PassiveItemSO> EquippedSlots=new(); public void ForgetDestroyedPassive(PassiveItemSO item)=>Destroyed++; public void RefreshModuleEffects()=>Refreshed++;}
public static class GameProgressionRuntime { public static bool AllowChanges=true; public static bool CanChangePassives(out string message){message="HOME only";return AllowChanges;} }
public partial class PlayerInventoryRuntime {
 public readonly List<PlayerInventoryItemStack> _items=new();
 public PlayerPassiveInventoryRuntime Owner=new();
 public int Notifications;
 public T GetComponent<T>() where T:class => Owner as T;
 private static bool Fail(string text,out string message){message=text;return false;}
 private void NormalizeStorage(){}
 private bool IsValidIndex(int i)=>i>=0&&i<_items.Count&&!_items[i].IsEmpty;
 private bool CanAcquireOwnedAmount(ItemSO i,int n,out string m){m="";return true;}
 private int CalculateStoreCapacity(ItemSO item)=>_items.Sum(s=>s.IsEmpty?item.MaxStack:s.Item==item?item.MaxStack-s.Amount:0);
 private int GetInventoryItemAmount(ItemSO item)=>_items.Where(s=>s.Item==item).Sum(s=>s.Amount);
 private static bool IsSameItemIdentity(ItemSO a,ItemSO b)=>a==b||(a!=null&&b!=null&&a.Guid==b.Guid);
 private bool TryStoreItem(ItemSO item,int amount,out string message){
  message="";if(CalculateStoreCapacity(item)<amount)return false;
  foreach(var s in _items){ if(s.IsEmpty){s.Set(item,Math.Min(amount,item.MaxStack));amount-=s.Amount;}else if(s.Item==item){int n=Math.Min(amount,item.MaxStack-s.Amount);s.Amount+=n;amount-=n;}if(amount==0)return true; }return false;
 }
 private void NotifyChanged()=>Notifications++;
 public bool CanLoseInventorySlots(int value,out string message){message="";return true;}
 public void Add(ItemSO item,int amount=1){var s=new PlayerInventoryItemStack();if(item!=null)s.Set(item,amount);_items.Add(s);}
}
namespace CombatModulesProbe {
'@
$parts = @(
 'Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopItemRarity.cs',
 'Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/StatModifierTypes.cs',
 'Assets/Scripts/Gameplay/Items/Definitions/PassiveItemSO.cs',
 'Assets/Scripts/Gameplay/Items/Definitions/PassiveInstanceRuntime.cs',
 'Assets/Scripts/Gameplay/Items/Definitions/PassiveModuleRulesSO.cs',
 'Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.PassiveModules.cs'
) | ForEach-Object { (Read-Source $_) -replace '(?m)^using [^\r\n]+;\r?\n','' }
# The actual partial storage owner belongs beside the shim, not inside a nested namespace.
$shim = $shim.Replace("namespace CombatModulesProbe {",'')
$weapon = Member (Read-Source 'Assets/Scripts/Gameplay/Items/Weapons/Definitions/RangedWeaponSO.cs') 'public float MalfunctionChance'
$wear = Member (Read-Source 'Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs') 'public bool ApplyCurrentWeaponFireWear'
$pocket = Member (Read-Source 'Assets/Scripts/Core/Persistence/Save.cs') 'public int PocketSlotBonus'
$quest = Member (Read-Source 'Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs') 'public sealed class QuestRuntimeState'
$support = @'
public class ProbeWeapon {float _malfunctionThreshold=.90f,_malfunctionReferenceCondition=.30f,_malfunctionReferenceCumulativeChance=.25f;int _malfunctionReferenceShots=30;
'@ + $weapon + "`n}`n"
$support += @'
public class RangedWeaponSO:ItemSO { public float DurabilityLossPerFire=.05f; }
public class ProbeEquipment {
 public bool IsEnemyWeaponOwner;
 public ItemSO[] _weaponSlots={new RangedWeaponSO()};
 public float[] _weaponCurrentDurabilities={100f};
 public int ActiveWeaponSlotIndex=>0;
 public int Changes;
 public void EnsureWeaponSlots(){}
 public void ClearWeapon(int index){_weaponSlots[index]=null;Changes++;}
 public void PublishWeaponSlotChanged(int index)=>Changes++;
'@ + $wear + "`n}`n"
$support += 'public class QuestPassiveSlotRewardSaveData { public string questId; public int rewardIndex,slotAmount; } public class ProbeProfile { public List<QuestPassiveSlotRewardSaveData> questPocketSlotRewards=new();' + $pocket + "}`n"
$support += @'
public class QuestDefinitionSO {public string QuestId="q"; public QuestObjectiveDefinition[] Objectives={new(),new()};public object[] Rewards=Array.Empty<object>();}
public class QuestObjectiveDefinition {public int RequiredCount=2;}
public class QuestProgressSaveData {public string questId;public bool accepted,completed;public int[] objectiveProgress;public bool[] rewardGranted;}
'@ + $quest
$runner = @'
public static class CombatModulesProbe {
 static int assertions;
 static void Check(bool condition,string message){assertions++;if(!condition)throw new Exception(message);}
 static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
 static PassiveItemSO Definition(string id,PlayerStatId stat=PlayerStatId.PenetrationClass){
  var p=new PassiveItemSO {Guid=id,name=id};Set(p,"_randomStatsEnabled",true);
  Set(p,"_rarityRolls",new[]{new PassiveRarityRoll{rarity=PlayerShopItemRarity.Normal,stats=new(){new(){stat=stat,minimum=.1f,maximum=.5f}}}});return p;
 }
 public static string Run(){
  var p=Definition("penetration");var catalog=new RuntimeContentCatalogSO();catalog.Items[p.Guid]=p;
  var random=new Random(7811);
  var item=(PassiveItemSO)PassiveInstanceRuntime.Create(p,random);
  string id=item.Guid;
  Check(item.IsRolledInstance&&item!=p,"No unique instance");
  Check(PassiveInstanceRuntime.Bind(item)&&item.IsBound,"Binding failed");
  for(int n=0;n<1000;n++){
   Check(PassiveInstanceRuntime.TryReroll(item,random),"Reroll failed");
   var value=item.StatModifiers.Single();
   Check(value.stat==PlayerStatId.PenetrationClass&&value.value>=.1f&&value.value<=.5f,"Effect changed/range invalid");
   Check(item.Guid==id&&item.IsBound&&item.Rarity==PlayerShopItemRarity.Normal,"Identity/binding/rarity changed");
  }
  var max=(PassiveItemSO)PassiveInstanceRuntime.Create(p,random,maximum:true);
  Check(max.StatModifiers.Single().value==.5f,"Boss did not use maximum");
  Check(item.SharesEffectWith(max),"Same effect allowed");
  var other=(PassiveItemSO)PassiveInstanceRuntime.Create(Definition("armor",PlayerStatId.ArmorClass),random);
  Check(!item.SharesEffectWith(other),"Different effect rejected");
  var fixedSource=Definition("fixed");Set(fixedSource,"_allowNumericReroll",false);
  var fixedModule=(PassiveItemSO)PassiveInstanceRuntime.Create(fixedSource,random);
  Check(PassiveInstanceRuntime.Bind(fixedModule)&&fixedModule.IsBound,"Fixed module binding blocked by reroll policy");
  Check(!PassiveInstanceRuntime.TryReroll(fixedModule,random),"Fixed effect was numerically rerolled");
  Set(p,"_effectGroup","penetration");Set(other.SourceDefinition,"_effectGroup","armor");
  Check(!item.SharesEffectWith(other),"Penalty stat overlap blocked a different effect group");
  typeof(PassiveInstanceRuntime).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
  Check(PassiveInstanceRuntime.TryResolve(id,catalog,out var restored)&&((PassiveItemSO)restored).IsBound,"Save restore lost binding");
  Check(((PassiveItemSO)restored).StatModifiers.Single().value==item.StatModifiers.Single().value,"Save restore rerolled value");
  item=(PassiveItemSO)restored;
  SaveSystem.Active.RunData.passiveInstances[SaveSystem.Active.RunData.passiveInstances.FindIndex(x=>x.instanceId==id)]=
   new PassiveInstanceSaveData {instanceId=id,definitionGuid=p.Guid,rarity=item.Rarity,modifiers=(PlayerStatModifier[])item.StatModifiers.Clone()};
  Check(PassiveInstanceRuntime.TryReroll(item,random),"Restored reroll failed");
  Check(ReferenceEquals(SaveSystem.Active.Profile.passiveInstances.Find(x=>x.instanceId==id),
   SaveSystem.Active.RunData.passiveInstances.Find(x=>x.instanceId==id)),"Run partition still contains stale roll");
  var parts=new ItemSO {Guid="parts",Stack=999};
  var rules=new PassiveModuleRulesSO {tiers=new[]{new PassivePartsTier {rarity=PlayerShopItemRarity.Normal,parts=parts}}};Resources.Value=rules;
  var inventory=new PlayerInventoryRuntime();inventory.Add(item);
  Check(!inventory.TryDismantlePassive(0,out _)&&inventory._items[0].Item==item,"Full bag consumed module");
  inventory.Add(parts,4);
  Check(!inventory.TryDismantlePassive(0,out _)&&inventory._items[0].Item==item&&inventory._items[1].Amount==4,"Immediate dismantle must be retired");
  Check(inventory.Owner.Destroyed==0,"Retired dismantle removed effects");
  var inv2=new PlayerInventoryRuntime();inv2.Add(item);inv2.Add(parts,999);
  Check(!inv2.TryDismantlePassive(0,out _)&&inv2._items[1].Amount==999,"Full output stack accepted");
  Check(inv2.TryRerollPassive(0,out _)&&inv2._items[1].Amount==998&&item.Guid==id&&item.IsBound,"Reroll payment/identity failed");
  GameProgressionRuntime.AllowChanges=false;
  Check(!inv2.TryRerollPassive(0,out _)&&inv2._items[1].Amount==998,"Raid reroll consumed parts");
  GameProgressionRuntime.AllowChanges=true;
  var stepped=new PassiveStatRollRange{minimum=.05f,maximum=.25f,step=.05f};
  for(int i=0;i<1000;i++) { float v=stepped.Roll(random);Check(v>=.04999f&&v<=.25001f&&Math.Abs(v/.05f-Math.Round(v/.05f))<.00001,"Non-stepped passive roll"); }
  Check(stepped.Roll(random,true)==.25f,"Boss stepped maximum not preserved");
  var inv3=new PlayerInventoryRuntime();inv3.Add(item);inv3.Add(null);
  Check(!inv3.TryRerollPassive(0,out _)&&inv3._items[0].Item==item,"Missing parts mutated inventory");
  var profile=new ProbeProfile();profile.questPocketSlotRewards.Add(new(){questId="q",rewardIndex=0,slotAmount=2});
  profile.questPocketSlotRewards.Add(new(){questId="q",rewardIndex=0,slotAmount=2});
  Check(profile.PocketSlotBonus==2,"Duplicate pocket grant");
  profile.questPocketSlotRewards.Add(new(){questId="q2",rewardIndex=0,slotAmount=8});
  Check(profile.PocketSlotBonus==4,"Pocket limit exceeded");
  var quest=new QuestRuntimeState();quest.Accept(new());
  for(int i=0;i<2;i++){quest.TryAddProgress(0);quest.TryAddProgress(1);}
  Check(quest.IsReadyToTurnIn&&!quest.IsCompleted,"Objectives auto-completed quest");
  Check(quest.TryComplete()&&quest.IsCompleted&&!quest.TryComplete(),"Explicit completion not once-only");
  var weapon=new ProbeWeapon();
  Check(weapon.MalfunctionChance(1f)==0f&&weapon.MalfunctionChance(.90f)==0f,"Safe threshold malfunction");
  float firstBucket=weapon.MalfunctionChance(.899999f);
  Check(firstBucket>0f&&firstBucket<.0002f,"First sub-90% bucket missing or too large");
  Check(Math.Abs(weapon.MalfunctionChance(.89f)-firstBucket)<.00000001f,"Wrong 89% boundary");
  Check(Math.Abs(weapon.MalfunctionChance(.88999f)-2f*firstBucket)<.00000001f,"Not a 1% step");
  Check(Math.Abs(1d-Math.Pow(1d-weapon.MalfunctionChance(.3f),30)-.25d)<.000001d,"30% / 30 shots must give 25% cumulative chance");
  for(int percent=89;percent>=0;percent--)
   Check(Math.Abs(weapon.MalfunctionChance(percent/100f)-(90-percent)*firstBucket)<.0000001f,"Whole-percent float boundary error: "+percent);
  Check(weapon.MalfunctionChance(.5f)>0f&&weapon.MalfunctionChance(.1f)>weapon.MalfunctionChance(.5f),"Wear does not increase chance");
  Check(Math.Abs(weapon.MalfunctionChance(0f)-weapon.MalfunctionChance(.3f)*1.5f)<.00000001f,"Wrong last bucket");
  var playerWear=new ProbeEquipment();
  Check(playerWear.ApplyCurrentWeaponFireWear(),"Player shot did not wear weapon");
  Check(Math.Abs(playerWear._weaponCurrentDurabilities[0]-99.95f)<.00001f,"Player shot did not consume .05 durability");
  for(int shot=1;shot<30;shot++) Check(playerWear.ApplyCurrentWeaponFireWear(),"Player wear failed");
  Check(Math.Abs(playerWear._weaponCurrentDurabilities[0]-98.5f)<.0002f,"30 shots must consume 1.5 durability");
  var enemyWear=new ProbeEquipment{IsEnemyWeaponOwner=true};enemyWear._weaponCurrentDurabilities[0]=37.5f;
  for(int shot=0;shot<100;shot++) Check(!enemyWear.ApplyCurrentWeaponFireWear(),"Enemy weapon wore down");
  Check(enemyWear._weaponCurrentDurabilities[0]==37.5f&&enemyWear.Changes==0,"Enemy condition changed");
  Check((int)PlayerShopItemRarity.Unique==3&&(int)PlayerShopItemRarity.Legendary==2,"Legacy rarity enum changed");
  var legacy=Definition("legacy");Set(legacy,"_randomStatsEnabled",false);
  Set(legacy,"_statModifiers",new[]{new PlayerStatModifier {stat=PlayerStatId.MaxHealth,type=StatModifierType.PercentAdd,value=12.5f}});
  var legacyInstance=(PassiveItemSO)PassiveInstanceRuntime.Create(legacy);catalog.Items[legacy.Guid]=legacy;
  typeof(PassiveInstanceRuntime).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
  Check(PassiveInstanceRuntime.TryResolve(legacyInstance.Guid,catalog,out var fixedRestored),"Legacy fixed modifiers could not restore");
  Check(((PassiveItemSO)fixedRestored).StatModifiers[0].type==StatModifierType.PercentAdd,"Legacy modifier semantics changed");
  return $"PASS {assertions} assertions. Real source: instance/binding/save restore, fixed binding and reroll policy, 1000 numeric rerolls, boss max, effect groups, retired immediate dismantle, part cost, duplicate/capped pocket grants, manual quest completion, malfunction curve. Managed shims only; Unity import/Play Mode NOT tested.";
 }
}
'@
$code=$shim+"`n"+($parts -join "`n")+"`n"+$support+"`n"+$runner
Add-Type -TypeDefinition $code -Language CSharp -IgnoreWarnings
[CombatModulesProbe]::Run()
