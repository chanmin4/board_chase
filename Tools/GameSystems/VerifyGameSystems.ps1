param([string]$ProjectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference='Stop'
# Executes the actual XP, recycling and layout-policy source with small managed Unity/storage shims.
# This does not run Unity physics, serialization callbacks, UI or HFSM.
function Read-Source([string]$path){Get-Content -LiteralPath (Join-Path $ProjectRoot $path) -Raw}
function Member([string]$source,[string]$signature){
 $start=$source.IndexOf($signature,[StringComparison]::Ordinal)
 if($start -lt 0){throw "Missing $signature"};$brace=$source.IndexOf('{',$start);$depth=0
 for($i=$brace;$i -lt $source.Length;$i++){
  if($source[$i] -eq '{'){$depth++}
  if($source[$i] -eq '}'){$depth--;if($depth -eq 0){return $source.Substring($start,$i-$start+1)}}
 };throw "Unclosed $signature"
}
$shim=@'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public class Object {public static T FindFirstObjectByType<T>() where T:class=>PlayerStatsRuntime.Current as T;}
 public class ScriptableObject:Object {}
 public class CreateAssetMenuAttribute:Attribute {public string menuName,fileName;}
 public class Component:Object {public Transform transform;public T GetComponent<T>()where T:class=>null;public T GetComponentInParent<T>()where T:class=>null;}
 public class Transform:Component {public string name="actor";public Transform parent;public GameObject gameObject;public int GetSiblingIndex()=>0;}
 public class GameObject:Object {public Transform transform;}
 public class MinAttribute:Attribute {public MinAttribute(float n){}}
 public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string s){}}
 public class SerializeField:Attribute {}
 public class HideInInspector:Attribute {}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
 public static class Debug {public static void Log(object s){}}
 public static class Time {public static float deltaTime,unscaledTime;}
 public static class Resources {public static object Catalog,PassiveRules,AggravationRules;public static T Load<T>(string s)where T:class=>(s=="PassiveModuleRules"?PassiveRules:s=="InfectionAggravation"?AggravationRules:Catalog) as T;}
 public static class Mathf {
  public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);
  public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)=>Math.Min(a,b);
  public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);
  public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);public static float Clamp01(float v)=>Math.Clamp(v,0,1);
  public static int FloorToInt(float v)=>(int)Math.Floor(v);
  public static float Lerp(float a,float b,float t)=>a+(b-a)*t;
 }
 public struct Vector2Int:IEquatable<Vector2Int> {
  public int x,y;public Vector2Int(int x,int y){this.x=x;this.y=y;}
  public static Vector2Int zero=>new(0,0);public static Vector2Int left=>new(-1,0);public static Vector2Int right=>new(1,0);public static Vector2Int up=>new(0,1);public static Vector2Int down=>new(0,-1);
  public static Vector2Int operator +(Vector2Int a,Vector2Int b)=>new(a.x+b.x,a.y+b.y);
  public static bool operator ==(Vector2Int a,Vector2Int b)=>a.Equals(b);public static bool operator !=(Vector2Int a,Vector2Int b)=>!a.Equals(b);
  public bool Equals(Vector2Int b)=>x==b.x&&y==b.y;public override bool Equals(object o)=>o is Vector2Int v&&Equals(v);public override int GetHashCode()=>HashCode.Combine(x,y);
 }
}
public enum PlayerStatId {MaxHealth,InventorySlots,ExperienceGainBonus}
public enum StatModifierType {FlatAdd,PercentMultiply,Override}
public struct PlayerStatModifier {public PlayerStatId stat;public StatModifierType type;public float value;}
public enum PlayerShopItemRarity {Normal=0,Rare=1,Unique=3,Legendary=2,Mythic=4}
public enum ItemKind {Misc=0,Passive=1,Weapon=2,Armor=3}
public class ItemSO {public string Guid;public string name;public int UsageLevelOverride,MaxStack=1,MaxOwnedAmount;public PlayerShopItemRarity Rarity;public virtual ItemKind Kind=>ItemKind.Misc;}
public class ArmorItemSO:ItemSO {public int ArmorClass;}
public class BulletSO:ItemSO {public int Tier;}
public class RangedWeaponSO:ItemSO {public int WeaponType;public override ItemKind Kind=>ItemKind.Weapon;}
public class PassiveItemSO:ItemSO {public int DismantlePartAmount=1,MaximumDismantleYield;public bool EverEquipped,DisabledForRaid;public PassiveCategory Category;public PlayerStatModifier[] StatModifiers=Array.Empty<PlayerStatModifier>();public override ItemKind Kind=>ItemKind.Passive;}
public static class PassiveInstanceRuntime {public static bool MatchesDefinition(ItemSO a,ItemSO b)=>a==b;public static bool NeedsRoll(ItemSO item)=>false;public static ItemSO Create(ItemSO item)=>item;public static bool CanCreate(ItemSO item,out string m){m="";return true;}}
public class ShopTradeRequirement {public ItemSO Item;public int Amount;}
public class PlayerStatsRuntime:Component {public static PlayerStatsRuntime Current;public float InfectionDefense,ExperienceBonus;public float AdditionalStat(PlayerStatId stat)=>stat==PlayerStatId.ExperienceGainBonus?ExperienceBonus:0;public void RebuildAndPublish(){}}
public class GameProgressionIdentity {public string Key;}
public class ShooterInfection:Component {public float Infection;public virtual void AddInfection(float amount)=>ReceiveStatusInfectionDamage(amount);public void ReceiveStatusInfectionDamage(float amount)=>Infection+=amount;}
public class StatusEffectDefinitionSO:ScriptableObject {public int MaximumStacks=20;public float DamageTickInterval=5,DamagePerStackPerSecond=.2f;public virtual bool PersistentStacks=>false;public virtual bool DealsInfectionDamage=>false;}
public sealed partial class EntityStatusEffectRuntime:Component {
 public PlayerShooterInfection Owner;public int BleedingStacks;public new T GetComponent<T>()where T:class=>Owner as T;
 public List<ActiveStatusEffect> _activeEffects=new();public event Action<EntityStatusEffectRuntime> OnChanged;
 public class ActiveStatusEffect {public StatusEffectDefinitionSO Definition;private InfectionExposureSaveData state;
 public ActiveStatusEffect(StatusEffectDefinitionSO d,float time,InfectionExposureSaveData s){Definition=d;state=s;}public bool UsesAccumulationState(InfectionExposureSaveData s)=>ReferenceEquals(state,s);}
 public void Step(float dt)=>TickInfectionAggravation(dt);
}
public partial class PlayerShooterInfection:ShooterInfection {public bool IsDead,IsDeployed;public float AmpouleExposureMultiplier=1,AmpouleGaugeRecoveryPerSecond;public EntityStatusEffectRuntime StatusEffects;
 public PlayerShooterInfection(){StatusEffects=new(){Owner=this};}public void Step(float dt)=>StatusEffects.Step(dt);}
public class Damageable:Component {public bool IsDead;}
public static class SectorRoomTraitRuntime {public static float CurrentPlayerBiochemicalGasInfectionPerSecond;}
public enum EnemyShooterRole {StandardEnemy,Boss}
public class EnemyShooterConfigSO {public EnemyShooterRole EnemyRole;}
public class EnemyShooterStatsRuntime {public EnemyShooterConfigSO Config;}
public class SectorRuntime:Component {public Vector2Int Coord;}
public static class EntityComponentResolver {public static T Resolve<T>(object source)where T:class=>source is EntityStatusEffectRuntime status?status.Owner as T:null;}
public enum RunLifecyclePhase {Home,RunActive}
public class RunLifecycleCoordinator {public static RunLifecycleCoordinator Instance=new();public RunLifecyclePhase Phase;}
public class RaidRunSaveData {public bool active,extractionCommitted;public StageMapLayout layout;public InfectionSpreadSaveData infectionSpread=new();}
public class RaidState {public bool active;public StageMapLayout layout=new();}
public class RaidRuntime {public static RaidRuntime Active=new();public RaidState State=new();public bool IsPlayerInRaid;public StageDefinitionSO Definition=new();}
public class Save {public PlayerExperienceSaveData experience=new();public int recyclingSlotBonus;}
public class StartingPassivePreparationSaveData {public bool committed;}
public class RunSaveData {public StartingPassivePreparationSaveData startingPassives=new();public RunExperienceSaveData experience=new();public List<RecyclingSlotSaveData> recyclingSlots=new();public InfectionExposureSaveData infectionExposure=new();}
public class SaveSystem {public static SaveSystem Active=new();public Save Profile=new();public RunSaveData RunData=new();public int Writes;public void SaveDataToDisk()=>Writes++;}
public class ExperienceRulesSO {public static ExperienceRulesSO Active=new();public ExperienceRules experience=new();}
public class ItemUsageRulesSO {public static ItemUsageRulesSO Active=new();public UsageLevelRules usage=new();}
public class RecyclingRulesSO {public static RecyclingRulesSO Active=new();public RecyclingRules recycling=new();}
public class PlayerRunInventorySlotSaveData {public string itemGuid;public int amount;public float armorDurability,armorMaxDurability,weaponCurrentDurability,weaponMaxDurability,gasMaskFilterDurability,gasMaskMaxFilterDurability;}
public class PlayerInventoryItemStack {
 public ItemSO Item;public int Amount;public float ArmorDurability,ArmorMaxDurability,WeaponCurrentDurability,WeaponMaxDurability,GasMaskFilterDurability,GasMaskMaxFilterDurability;public bool IsEmpty=>Item==null||Amount<=0;
 public void Set(ItemSO item,int amount,float armor=-1,float weapon=-1,float weaponMax=-1,float gas=-1,float armorMax=-1,float gasMax=-1){Item=item;Amount=amount;ArmorDurability=armor;WeaponCurrentDurability=weapon;WeaponMaxDurability=weaponMax;GasMaskFilterDurability=gas;ArmorMaxDurability=armorMax;GasMaskMaxFilterDurability=gasMax;}
 public bool Remove(int n){Amount-=n;return Amount<=0;}public void Clear(){Item=null;Amount=0;}
}
public class PlayerPassiveInventoryRuntime {public List<PassiveItemSO> EquippedSlots=new();public void ForgetDestroyedPassive(PassiveItemSO p)=>EquippedSlots.Remove(p);}
public class RuntimeContentCatalogSO {public Dictionary<string,ItemSO> Items=new();public bool TryResolveItem(string id,out ItemSO item)=>Items.TryGetValue(id,out item);}
public partial class PlayerInventoryRuntime {
 public bool _passiveOverflowPending;public float _nextPassiveOverflowRetry;public void DropPassiveCapacityOverflow(){}
 public List<PlayerInventoryItemStack> _items=new();public int Capacity=>_items.Count;public PlayerPassiveInventoryRuntime Owner=new();public int Notifications;
 public T GetComponent<T>()where T:class=>Owner as T;private void NormalizeStorage(){}private bool IsValidIndex(int i)=>i>=0&&i<_items.Count;
 private static bool Fail(string s,out string m){m=s;return false;}private void NotifyChanged()=>Notifications++;
 private bool CanAcquireOwnedAmount(ItemSO item,int amount,out string message){message="limit";return item.MaxOwnedAmount<=0||_items.Where(s=>s.Item==item).Sum(s=>s.Amount)+amount<=item.MaxOwnedAmount;}
 private bool TryStoreItem(ItemSO item,int amount,out string message)=>TryStoreItem(item,amount,-1,-1,-1,-1,-1,-1,out message);
 private bool TryStoreItem(ItemSO item,int amount,float a,float w,float wm,float g,float am,float gm,out string message){
  message="space";if(_items.Sum(s=>s.IsEmpty?item.MaxStack:s.Item==item?item.MaxStack-s.Amount:0)<amount)return false;
  foreach(var slot in _items){if(!slot.IsEmpty&&slot.Item!=item)continue;int n=Math.Min(amount,slot.IsEmpty?item.MaxStack:item.MaxStack-slot.Amount);slot.Set(item,(slot.IsEmpty?0:slot.Amount)+n,a,w,wm,g,am,gm);amount-=n;if(amount==0)return true;}return false;
 }
 public void Add(ItemSO item,int count=1,float durability=-1){var slot=new PlayerInventoryItemStack();slot.Set(item,count,-1,durability);_items.Add(slot);}
 public void Tick(float dt){Time.deltaTime=dt;Update();}
}
public class StageDefinitionSO {public int StageIndex=1;public StageEncounterRules EncounterRules=new();
'@
$sectorSeed=Member (Read-Source 'Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs') 'public static int BuildSectorSeed'
$shim+=$sectorSeed+"`n}`n"
$shim+='public enum SectorPortalDirection {Left,Right,Up,Down} public enum LootKeyTier {Low,Mid,High} public enum SectorRoomTraitType {None}'
$domains=@{
 ExperienceRules='ExperienceRulesSO';LevelStatReward='ExperienceRulesSO';UsageLevelRules='ItemUsageRulesSO';
 RecyclingRules='RecyclingRulesSO';RecyclingRecipe='RecyclingRulesSO'
}
$parts=@($domains.GetEnumerator()|ForEach-Object {
 $text=Read-Source ("Assets/Scripts/Gameplay/Progression/"+$_.Value+".cs")
 $signature=[regex]::Match($text,('public sealed class '+$_.Key+'\s*\{')).Value
 Member $text $signature
})
$parts+=Member (Read-Source 'Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageEncounterRules.cs') 'public sealed class StageEncounterRules'
$parts+=Member (Read-Source 'Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapSettings.cs') 'public enum RaidRoomRole'
$paths=@('Assets/Scripts/Gameplay/Progression/GameProgressionRuntime.cs',
 'Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionAggravationStatusSO.cs',
 'Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionAggravationState.cs',
 'Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.InfectionAggravation.cs',
 'Assets/Scripts/Gameplay/World/Sectors/Runtime/RuntimeCountdown.cs',
 'Assets/Scripts/Gameplay/World/Sectors/StageMap/StageGasSpread.cs',
 'Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Recycling.cs',
 'Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.Exposure.cs',
 'Assets/Scripts/Gameplay/Items/Definitions/PassiveModuleRulesSO.cs',
 'Assets/Scripts/Gameplay/World/Sectors/StageMap/StageMapLayout.cs',
 'Assets/Scripts/Gameplay/World/Sectors/StageMap/StageRoomNode.cs',
 'Assets/Scripts/Gameplay/World/Sectors/StageMap/StageRoomType.cs',
 'Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSystemPolicies.cs')
$parts+= $paths|ForEach-Object {(Read-Source $_)-replace '(?m)^using [^\r\n]+;\r?\n',''}
$runner=@'
public static class GameSystemsProbe {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static StageMapLayout Map()=>new(){generationVersion=3,bossSpawned=true,bossAvoidsExtractionRoutes=true,startSectorCoord=new(0,0),bossRoomCoord=new(1,1),goalRoomCoord=new(1,1),stageSeed=51,rooms=new(){
  new(new(0,0),StageRoomType.Normal,StageRoomConnectionFlags.Right){raidRole=RaidRoomRole.Hatch},
  new(new(1,0),StageRoomType.Normal,StageRoomConnectionFlags.Left|StageRoomConnectionFlags.Right|StageRoomConnectionFlags.Up){raidRole=RaidRoomRole.Normal,combatPoolId="theme"},
  new(new(2,0),StageRoomType.Normal,StageRoomConnectionFlags.Left){raidRole=RaidRoomRole.Normal},
  new(new(1,1),StageRoomType.Boss,StageRoomConnectionFlags.Down){raidRole=RaidRoomRole.Boss,combatPoolId="theme"}},extractionRooms=new(){new(2,0)},extractionCandidates=new(){new(2,0)}};
 public static string Run(){
  var rules=ExperienceRulesSO.Active;rules.experience.experiencePerLevel=new[]{10,20,30};rules.experience.maximumLevel=4;
  Resources.PassiveRules=new PassiveModuleRulesSO();Resources.AggravationRules=new InfectionAggravationStatusSO();
  Check(rules.experience.Level(-1)==1&&rules.experience.Level(9)==1&&rules.experience.Level(10)==2,"Level boundary");
  Check(rules.experience.Level(60)==4&&rules.experience.Level(long.MaxValue)==4,"Multiple levels/max boundary");
  Check(rules.experience.IntoLevel(31)==1,"Within-level XP");
  for(int n=0;n<1000;n++){Check(GameProgressionRuntime.SettlementAmount(n,1)==n,"Extraction 100%");Check(GameProgressionRuntime.SettlementAmount(n,.5f)==n/2,"Death floor 50%");}
  Check(GameProgressionRuntime.SettlementAmount(int.MaxValue,2)==int.MaxValue,"Settlement overflow");
  Check(GameProgressionRuntime.SettlementAmount(1,float.NaN)==0,"NaN multiplier");
  Check(GameProgressionRuntime.CanChangePassives(out _),"HOME change denied");
  RunLifecycleCoordinator.Instance.Phase=RunLifecyclePhase.RunActive;Check(!GameProgressionRuntime.CanChangePassives(out _),"Raid passive switching allowed");
  RaidRuntime.Active.State.active=true;RaidRuntime.Active.IsPlayerInRaid=true;GameProgressionRuntime.BeginRun();string id=SaveSystem.Active.RunData.experience.id;
  GameProgressionRuntime.Discover("container",101);GameProgressionRuntime.Discover("container",101);GameProgressionRuntime.BeginRun();
  Check(SaveSystem.Active.RunData.experience.id==id&&SaveSystem.Active.RunData.experience.pending==101,"Hatch resets XP or repeated discovery awards XP");
  Check(GameProgressionRuntime.MarkDiscovery("drop")&&!GameProgressionRuntime.MarkDiscovery("drop"),"Zero-XP source repeat");
  var infection=new PlayerShooterInfection();infection.AddExposure(99);Check(infection.ExposureStacks==0&&infection.ExposureGauge==99,"Premature infection stack");
  infection.AddExposure(1);Check(infection.ExposureStacks==1&&infection.ExposureGauge==0,"Full gauge did not add a stack");
  Check(infection.Infection==5,"Full circle did not erode maximum HP immediately");
  infection.Step(4.9f);infection.Step(.1f);Check(infection.Infection==5,"Retired five-second damage tick remains");
  infection.AddExposure(250);Check(infection.ExposureStacks==3&&infection.ExposureGauge==50,"Multiple gauge wrap");
  infection.TreatExposure(25);Check(infection.ExposureStacks==3&&infection.ExposureGauge==25,"Gauge treatment changed erosion stacks");
  infection.StatusEffects.BleedingStacks=2;infection.Step(1);Check(infection.ExposureGauge==27,"Bleeding exposure missing");
  infection.StatusEffects.BleedingStacks=0;infection.Step(1);Check(infection.ExposureGauge==27,"Recovery grace missing");infection.Step(4);Check(infection.ExposureGauge==26.75f,"Slow gauge decay missing");
  float lethal=infection.Infection;infection.Step(0);Check(infection.Infection==lethal,"Lethal infection naturally recovered");
  float beforeAmpoule=infection.ExposureGauge;infection.AmpouleExposureMultiplier=.5f;
  infection.AddExposure(20);Check(infection.ExposureGauge==beforeAmpoule+10,"Ampoule did not halve incoming gauge exposure");
  infection.AmpouleGaugeRecoveryPerSecond=3;infection.Step(5);float gaugeAfterWait=infection.ExposureGauge;
  infection.Step(1);Check(infection.ExposureGauge==gaugeAfterWait-3.25f&&infection.Infection==lethal,"Ampoule changed eroded health or failed to accelerate gauge-only recovery");
  infection.AmpouleExposureMultiplier=1;infection.AmpouleGaugeRecoveryPerSecond=0;
  infection.AddExposure(float.MaxValue);Check(infection.ExposureStacks==20&&infection.ExposureGauge<100,"Large exposure overflow");
  float savedGauge=infection.ExposureGauge;InfectionAggravationStatusSO.Active.Settings.enabled=false;infection.AddExposure(10);Check(infection.ExposureGauge==savedGauge,"Disabled exposure mutated state");InfectionAggravationStatusSO.Active.Settings.enabled=true;
  GameProgressionRuntime.Settle(false);Check(SaveSystem.Active.Profile.experience.total==50,"Death settlement");
  GameProgressionRuntime.Settle(true);Check(SaveSystem.Active.Profile.experience.total==50,"Settled twice");
  SaveSystem.Active.RunData.experience=new(){id=id,pending=999};GameProgressionRuntime.Settle(true);Check(SaveSystem.Active.Profile.experience.total==50,"Run-file replay paid twice");
  GameProgressionRuntime.GrantQuest("q",9);GameProgressionRuntime.GrantQuest("q",9);Check(SaveSystem.Active.Profile.experience.total==59,"Quest XP duplicate");
  GameProgressionRuntime.BeginRun();GameProgressionRuntime.Discover("newrun",11);GameProgressionRuntime.Settle(true);Check(SaveSystem.Active.Profile.experience.total==70,"New-run settlement");
  SaveSystem.Active.RunData.experience=new(){id=id,pending=999};GameProgressionRuntime.Settle(true);Check(SaveSystem.Active.Profile.experience.total==70,"Older-than-last run replay paid twice");
  SaveSystem.Active.Profile.experience.total=long.MaxValue-1;GameProgressionRuntime.GrantQuest("overflow",9);Check(SaveSystem.Active.Profile.experience.total==long.MaxValue,"Total overflow");
  var restricted=new ItemSO{UsageLevelOverride=5};Check(!GameProgressionRuntime.CanUse(restricted,out _),"Item override ignored");
  RaidRuntime.Active.State.active=false;RaidRuntime.Active.IsPlayerInRaid=false;RunLifecycleCoordinator.Instance.Phase=RunLifecyclePhase.Home;
  Check(GameProgressionRuntime.CanChangePassives(out _),"HOME restrictions persisted");
  SaveSystem.Active=new();var item=new RangedWeaponSO{Guid="weapon"};var output=new ItemSO{Guid="parts",MaxStack=10};
  var catalog=new RuntimeContentCatalogSO();catalog.Items[item.Guid]=item;catalog.Items[output.Guid]=output;Resources.Catalog=catalog;
  RecyclingRulesSO.Active.recycling.recipes=new[]{new RecyclingRecipe{exactInput=item,output=output,seconds=120,outputAmount=1}};
  var inv=new PlayerInventoryRuntime();inv.Add(item,1,37.5f);inv.Add(item,1,15);
  var passive=new PassiveItemSO{Guid="passive",DismantlePartAmount=2};var parts=new ItemSO{Guid="module-parts",MaxStack=999};
  var passiveRules=new PassiveModuleRulesSO{tiers=new[]{new PassivePartsTier{rarity=PlayerShopItemRarity.Normal,parts=parts,rerollCost=3,moduleYield=2}}};Resources.PassiveRules=passiveRules;
  var derived=RecyclingRulesSO.Active.recycling.Find(passive);Check(derived.output==parts&&derived.outputAmount==2&&derived.seconds==30,"Passive recipe does not derive from shared parts");
  var replacement=new ItemSO{Guid="replacement"};passiveRules.tiers[0].parts=replacement;passiveRules.recyclingSeconds=45;
  derived=RecyclingRulesSO.Active.recycling.Find(passive);Check(derived.output==replacement&&derived.seconds==45,"Shared passive edit did not update new recycling jobs");
  Check(passiveRules.Find(passive.Rarity).parts==derived.output,"Reroll and recycle material sources diverged");
  Check(typeof(PassivePartsTier).GetField("dismantleAmount")==null,"Retired immediate-dismantle authoring remains");
  Check(inv.TryStartRecycling(0,0,out _)&&inv.TryStartRecycling(1,1,out _),"Independent queue registration");
  inv.Tick(10);Check(inv.RecyclingSlots.All(j=>j.remaining==110),"Independent timers");
  Check(inv.TryCollectOrCancelRecycling(0,out _)&&inv._items[0].WeaponCurrentDurability==37.5f,"Cancel changed condition");
  Check(inv.TryStartRecycling(0,0,out _)&&inv.RecyclingSlots[0].remaining==120,"Cancel/reinsert retained progress");
  inv.Tick(110);Check(inv.RecyclingSlots[0].remaining==10&&inv.RecyclingSlots[1].remaining==0,"Timer isolation");
  inv._items[0].Set(item,1);inv._items[1].Set(item,1);
  Check(!inv.TryCollectOrCancelRecycling(1,out _)&&!inv.RecyclingSlots[1].IsEmpty,"Full bag lost completed output");
  inv._items[0].Clear();Check(inv.TryCollectOrCancelRecycling(1,out _)&&inv._items[0].Item==output,"Completed result missing");
  SaveSystem.Active.Profile.recyclingSlotBonus=2;Check(inv.RecyclingCapacity==4,"Slot bonus missing");
  inv._items[1].Clear();inv.Add(item);Check(inv.TryStartRecycling(2,3,out _),"Expanded slot registration");
  RecyclingRulesSO.Active.recycling.maximumSlots=1;Check(inv.RecyclingCapacity==4,"Reduced limit hid queued item");
  Check(inv.TryCollectOrCancelRecycling(3,out _),"Hidden slot cannot cancel");
  var limited=new ItemSO{Guid="limited",MaxStack=10,MaxOwnedAmount=2};var grants=new[]{new ShopTradeRequirement{Item=limited,Amount=2},new ShopTradeRequirement{Item=limited,Amount=1}};
  inv._items[0].Clear();inv._items[1].Clear();Check(!inv.CanReceiveQuestAcceptance(grants,out _),"Acceptance duplicate bypassed owned cap");
  // Status-owned accumulation: cap, invalid inputs, recovery through stacks, no HP damage path.
  var exposure=new InfectionExposureSaveData();InfectionAggravationMath.Add(exposure,250,100,20,5);
  Check(exposure.stacks==2&&exposure.gauge==50,"Status total accumulation");
  InfectionAggravationMath.Reduce(exposure,151,100);Check(exposure.stacks==0&&exposure.gauge==99,"Natural/treatment reduction through stacks");
  Check(InfectionAggravationMath.Tick(exposure,5,100,5,1,0)==0,"Partial gauge caused Infection damage");
  foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f,0f}){
   float beforeGauge=exposure.gauge;InfectionAggravationMath.Add(exposure,invalid,100,20,5);
   Check(exposure.stacks==0&&exposure.gauge==beforeGauge,"Invalid exposure accepted");
  }
  InfectionAggravationMath.Add(exposure,float.MaxValue,100,20,5);Check(exposure.stacks==20&&exposure.gauge>=0&&exposure.gauge<100,"Maximum stack overflow");
  exposure.gauge=0;InfectionAggravationMath.Reduce(exposure,.25f,100);Check(exposure.stacks==19&&exposure.gauge==99.75f,"Explicit legacy debug reduction");
  InfectionAggravationMath.Reduce(exposure,float.MaxValue,100);Check(exposure.stacks==0&&exposure.gauge==0,"Treatment did not fully clear");
  for(int count=1;count<=20;count++){
   var tick=new InfectionExposureSaveData{stacks=count,nextTick=5,recoveryDelayRemaining=5};
   Check(InfectionAggravationMath.Tick(tick,4,100,5,1,0)==0,"Tick before five seconds");
   Check(InfectionAggravationMath.Tick(tick,1,100,5,1,0)==0,"Retired stack-scaled Infection tick remains");
  }
  StageMapLayout Line()=>new(){stageSeed=15,rooms=new(){
   new(new(0,0),StageRoomType.Normal,StageRoomConnectionFlags.Right){virusRoom=true},
   new(new(1,0),StageRoomType.Normal,StageRoomConnectionFlags.Left|StageRoomConnectionFlags.Right),
   new(new(2,0),StageRoomType.Normal,StageRoomConnectionFlags.Left)}};
  var spreadRules=new StageEncounterRules{infectionSpreadIntervalSeconds=10};
  var raidSpread=new RaidRunSaveData{active=true,layout=Line()};
  Check(StageGasSpread.Tick(raidSpread,spreadRules,9)==0&&!raidSpread.layout.rooms[1].virusRoom,"Early spread");
  Check(StageGasSpread.Tick(raidSpread,spreadRules,1)==1&&raidSpread.layout.rooms[1].virusRoom&&!raidSpread.layout.rooms[2].virusRoom,"Same-pulse chain reaction");
  Check(raidSpread.infectionSpread.remainingSeconds==10,"Global countdown reset");
  Check(StageGasSpread.Tick(raidSpread,spreadRules,10)==1&&raidSpread.layout.rooms[2].virusRoom,"Next-pulse frontier");
  raidSpread.infectionSpread.remainingSeconds=3.25f;int savedStep=raidSpread.infectionSpread.step;
  StageGasSpread.Initialize(raidSpread,spreadRules);Check(raidSpread.infectionSpread.remainingSeconds==3.25f&&raidSpread.infectionSpread.step==savedStep,"Resume reset countdown");
  spreadRules.infectionSpreadEnabled=false;Check(StageGasSpread.Tick(raidSpread,spreadRules,100)==0&&raidSpread.infectionSpread.remainingSeconds==3.25f,"Disabled countdown advanced");
  spreadRules.infectionSpreadEnabled=true;raidSpread.extractionCommitted=true;Check(StageGasSpread.Tick(raidSpread,spreadRules,100)==0,"Spread during extraction");
  for(int seed=0;seed<200;seed++){
   var first=Line();first.stageSeed=seed;var second=Line();second.stageSeed=seed;second.rooms.Reverse();
   Check(StageGasSpread.Spread(first,0)==1&&StageGasSpread.Spread(second,0)==1,"Frontier count");
   foreach(var room in first.rooms){second.TryGetRoom(room.coord,out var other);Check(room.virusRoom==other.virusRoom,"Saved seed/order changed spread");}
   Check(!first.rooms[2].virusRoom,"New source spread in same pulse");
   Check(StageGasSpread.Spread(first,1)==1&&StageGasSpread.Spread(first,2)==0,"Saturated map keeps spreading");
  }
  var disconnected=Line();disconnected.rooms[1].connections=StageRoomConnectionFlags.Right;
  Check(StageGasSpread.Spread(disconnected,0)==0,"Spread crossed one-way/missing portal");
  var map=Map();Check(StageSystemPolicies.ExitsReachableWithout(map,new(1,1)),"Safe boss rejected");Check(!StageSystemPolicies.ExitsReachableWithout(map,new(1,0)),"Route-blocking boss allowed");
  for(int seed=0;seed<200;seed++){
   map=Map();map.stageSeed=seed;var before=map.rooms.Select(r=>r.connections).ToArray();StageSystemPolicies.Apply(new StageDefinitionSO(),map);
   Check(StageSystemPolicies.ExitsReachableWithout(map,map.bossRoomCoord),"Boss blocks extraction");
   Check(map.rooms.Select(r=>r.connections).SequenceEqual(before),"Policy rewrote graph/HFSM topology");
   var virus=map.rooms.Select(r=>r.virusRoom).ToArray();var boss=map.bossRoomCoord;StageSystemPolicies.Apply(new StageDefinitionSO(),map);
   Check(map.bossRoomCoord==boss&&map.rooms.Select(r=>r.virusRoom).SequenceEqual(virus),"Reentry rerolled policy");
  }
  var usedBag=new PlayerInventoryRuntime();var used=new PassiveItemSO{Guid="used",EverEquipped=true};usedBag.Add(used);
  Check(!usedBag.TryStartRecycling(0,0,out _)&&usedBag._items[0].Item==used,"Worn passive dismantled or lost");
  used.EverEquipped=false;used.DisabledForRaid=true;Check(!usedBag.TryStartRecycling(0,0,out _),"Disabled passive dismantled");
  used.DisabledForRaid=false;usedBag.Owner.EquippedSlots.Add(used);Check(!usedBag.TryStartRecycling(0,0,out _),"Equipped passive dismantled");
  PlayerStatsRuntime.Current=new(){ExperienceBonus=.5f};RaidRuntime.Active.State.active=true;RaidRuntime.Active.IsPlayerInRaid=true;RunLifecycleCoordinator.Instance.Phase=RunLifecyclePhase.RunActive;
  SaveSystem.Active.RunData.experience=new RunExperienceSaveData{id="xp-time"};long totalBefore=SaveSystem.Active.Profile.experience.total;
  GameProgressionRuntime.Discover("bonus-time",10);Check(SaveSystem.Active.RunData.experience.pending==15,"XP bonus not applied at acquisition");
  PlayerStatsRuntime.Current.ExperienceBonus=0;GameProgressionRuntime.Discover("plain-time",10);Check(SaveSystem.Active.RunData.experience.pending==25,"Removed passive still grants XP bonus");
  GameProgressionRuntime.Discover("bonus-time",10);Check(SaveSystem.Active.RunData.experience.pending==25,"Bonus bypasses discovery dedup");
  PlayerStatsRuntime.Current.ExperienceBonus=10;GameProgressionRuntime.Settle(true);Check(SaveSystem.Active.Profile.experience.total-totalBefore==25,"Settlement reapplied current passive XP bonus");
  return $"PASS {checks} checks: actual XP/acquisition bonus/settlement/dedup/level gates, worn/disabled recycler guard, independent recycler/cancel/condition/space/slot limits, infection stacks/ticks/treatment, boss-route and snapshot policies. Managed shims; not Unity import, UI, physics or Play Mode.";
 }
}
'@
$code=($shim+"`n"+($parts -join "`n")+"`n"+$runner)-replace '\[Range\(','[UnityEngine.RangeAttribute('
Add-Type -TypeDefinition $code -Language CSharp -IgnoreWarnings
[GameSystemsProbe]::Run()
