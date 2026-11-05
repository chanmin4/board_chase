param([string]$ProjectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference='Stop'
function Read-Source([string]$path){Get-Content -LiteralPath (Join-Path $ProjectRoot $path) -Raw}
function Member([string]$source,[string]$signature){
 $start=$source.IndexOf($signature,[StringComparison]::Ordinal);if($start -lt 0){throw "Missing $signature"}
 $brace=$source.IndexOf('{',$start);$depth=0
 for($i=$brace;$i -lt $source.Length;$i++){if($source[$i] -eq '{'){$depth++};if($source[$i] -eq '}'){$depth--;if(!$depth){return $source.Substring($start,$i-$start+1)}}}
 throw "Unclosed $signature"
}
$shim=@'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;
namespace UnityEngine {
 public class Object {public string name;public HideFlags hideFlags;public static T Instantiate<T>(T source)where T:class=>(T)typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(source,null);}
 public enum HideFlags {DontSave}
 public class ScriptableObject:Object {}
 public class Component:Object {readonly Dictionary<Type,object> parts=new();public Transform transform=new();public T GetComponent<T>()where T:class=>parts.TryGetValue(typeof(T),out var value)?value as T:null;public void Attach<T>(T item)where T:class=>parts[typeof(T)]=item;}
 public class MonoBehaviour:Component {}
 public class Transform {public Vector3 position;}
 public struct Vector3 {public float x,y,z;}
 public struct Quaternion {public static Quaternion identity=>new();}
 public class CreateAssetMenuAttribute:Attribute {public string menuName,fileName;}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string s){}}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}
 public class MinAttribute:Attribute {public MinAttribute(float n){}}
 public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
 public class SerializeField:Attribute {}public class HideInInspector:Attribute {}public class DisallowMultipleComponent:Attribute {}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type){}}
 public static class Application {public static bool isPlaying=true;}
 public static class Time {public static float time,unscaledTime;}
 public static class Debug {public static void LogError(object s,object c=null){}public static void LogWarning(object s,object c=null){}}
 public static class Resources {public static PassiveModuleRulesSO Rules;public static T Load<T>(string name)where T:class=>Rules as T;}
 public static class JsonUtility {public static string ToJson(object value)=>System.Text.Json.JsonSerializer.Serialize(value,new System.Text.Json.JsonSerializerOptions{IncludeFields=true});}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b);public static float Clamp01(float v)=>Math.Clamp(v,0,1);public static int CeilToInt(float n)=>(int)Math.Ceiling(n);public static float Lerp(float a,float b,float t)=>a+(b-a)*t;}
}
namespace UnityEngine.Serialization {public class FormerlySerializedAsAttribute:Attribute {public FormerlySerializedAsAttribute(string s){}}}
public class ReadOnlyAttribute:Attribute {}
public enum PlayerShopItemRarity {Normal=0,Rare=1,Unique=3,Legendary=2,Mythic=4}
public enum ItemKind {Misc,Passive}
public class ItemSO:ScriptableObject {public string Guid;public PlayerShopItemRarity rarity;public virtual PlayerShopItemRarity Rarity=>rarity;public virtual int MaxStack=>1;public virtual ItemKind Kind=>ItemKind.Misc;protected void SetRuntimeGuid(string id)=>Guid=id;}
public class ValuableItemSO:ItemSO {public bool WasPassive;}
public class RecyclingRecipe {public ItemKind kind;public PlayerShopItemRarity rarity;public ItemSO output;public int outputAmount;public float seconds;}
public class Damageable {public bool IsDead;}
public class PlayerRunStateSaveData {public bool hasSnapshot=true;public int unlockedPassiveSlotCount=2,passiveSlotSchemaVersion;public List<string> equippedPassiveGuids=new();}
public class Save {public List<PassiveInstanceSaveData> passiveInstances=new();}
public class RunSaveData {public List<PassiveInstanceSaveData> passiveInstances=new();public StartingPassivePreparationSaveData startingPassives=new();}
public class SaveSystem {public static SaveSystem Active=new();public Save Profile=new();public RunSaveData RunData=new();public int Writes;public void SaveDataToDisk()=>Writes++;}
public static partial class GameProgressionRuntime {public static int Level=1;public static bool PassiveEffectsActive;public static bool IsHomePreparation=>!PassiveEffectsActive;public static bool CanUse(ItemSO item,out string message){message="";return true;}}
public static class InventoryItemDisplayUtility {public static string ResolveItemName(ItemSO item)=>item?.name??"";}
public class RuntimeContentCatalogSO {public Dictionary<string,ItemSO> Items=new();public bool TryResolveItem(string id,out ItemSO item){item=null;if(string.IsNullOrEmpty(id))return false;return PassiveInstanceRuntime.IsInstanceId(id)?PassiveInstanceRuntime.TryResolve(id,this,out item):Items.TryGetValue(id,out item);}}
public class LootRollResult {public LootRollResult(ItemSO item,int amount,PlayerShopItemRarity rarity){}}
public class WorldItemPickup {}
public static class WorldItemPickupSpawner {
 public static bool Fail;public static List<string> Dropped=new();
 public static bool TrySpawn(ItemSO item,int amount,int bullets,float price,float armor,float weapon,float weaponMax,float gas,float armorMax,float gasMax,Vector3 position,Quaternion rotation,out WorldItemPickup pickup){pickup=Fail?null:new();if(!Fail)Dropped.Add(item.name);return !Fail;}
}
public class PlayerInventoryItemStack {
 public ItemSO Item;public int Amount;public bool IsEmpty=>Item==null||Amount<=0;
 public float ArmorDurability=-1,ArmorMaxDurability=-1,WeaponCurrentDurability=-1,WeaponMaxDurability=-1,GasMaskFilterDurability=-1,GasMaskMaxFilterDurability=-1;
 public void Set(ItemSO item,int amount){Item=item;Amount=amount;}public void Clear(){Item=null;Amount=0;}public bool Remove(int n){Amount-=n;return Amount<=0;}
}
public partial class PlayerInventoryRuntime:MonoBehaviour {
 public List<PlayerInventoryItemStack> _items=new();public IReadOnlyList<PlayerInventoryItemStack> Items=>_items;public PlayerPassiveInventoryRuntime Owner;public event Action OnChanged;public int Notifications;
 public int Capacity {get{double extra=0;if(Owner!=null)foreach(var p in Owner.Items)foreach(var m in p.StatModifiers)if(m.stat==PlayerStatId.InventorySlots)extra+=Resources.Rules?.ApplyStrength(p,m).value??m.value;return 6+(int)Math.Floor(extra);}}
 private bool CanAcquireOwnedAmount(ItemSO item,int amount,out string message){message="";return true;}
 private void NormalizeStorage(){while(_items.Count<Capacity)_items.Add(new());}
 private void NotifyChanged(){Notifications++;OnChanged?.Invoke();}private static bool Fail(string why,out string message){message=why;return false;}
 public int GetInventoryItemAmount(ItemSO item)=>item==null?0:_items.Where(s=>!s.IsEmpty&&PassiveInstanceRuntime.MatchesDefinition(s.Item,item)).Sum(s=>s.Amount);
 public bool TryPickupPassiveItem(PassiveItemSO source,out string message){NormalizeStorage();int index=_items.FindIndex(s=>s.IsEmpty);message="";if(index<0)return false;_items[index].Set(PassiveInstanceRuntime.Create(source),1);NotifyChanged();return true;}
 private static bool IsSameItemIdentity(ItemSO a,ItemSO b)=>PassiveInstanceRuntime.MatchesDefinition(a,b);
 public void Pad()=>NormalizeStorage();public void Signal()=>NotifyChanged();
 public void ClearPassiveItems(){foreach(var stack in _items)if(stack.Item is PassiveItemSO)stack.Clear();NotifyChanged();}
}
'@
$paths=@('Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/StatModifierTypes.cs',
 'Assets/Scripts/Gameplay/Items/Definitions/PassiveItemSO.cs','Assets/Scripts/Gameplay/Items/Definitions/PassiveModuleRulesSO.cs',
 'Assets/Scripts/Gameplay/Items/Definitions/PassiveInstanceRuntime.cs',
 'Assets/Scripts/Gameplay/Entities/Shooter/Player/Passive/PlayerPassiveInventoryRuntime.cs',
 'Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.StartingPassives.cs',
 'Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.PassiveRaid.cs')
$parts=$paths|ForEach-Object {(Read-Source $_)-replace '(?m)^using [^\r\n]+;\r?\n',''}
$parts+='public partial class PlayerInventoryRuntime {'+(Read-Source 'Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.PassiveModules.cs' | ForEach-Object { (Member $_ 'private bool IsRaidLockedEquippedModule(')+(Member $_ 'public bool TryBindOwnedPassive(') })+'}'
$parts+='public static partial class GameProgressionRuntime {'+(Member (Read-Source 'Assets/Scripts/Gameplay/Progression/GameProgressionRuntime.cs') 'public static bool CanChangePassives(')+'}'
$runner=@'
public static class RaidPassiveProbe {
 static int count;static RuntimeContentCatalogSO catalog=new();
 static void Check(bool ok,string reason){count++;if(!ok)throw new Exception(reason);}
 static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
 static PassiveItemSO Source(string name,PassiveCategory category,PlayerStatId stat=PlayerStatId.AttackDamage,float value=1){
  var p=new PassiveItemSO{name=name,Guid=name};Set(p,"_category",category);Set(p,"_effectGroup",name);
  Set(p,"_statModifiers",new[]{new PlayerStatModifier{stat=stat,value=value,type=StatModifierType.FlatAdd}});
  catalog.Items[name]=p;return p;
 }
 static (PlayerInventoryRuntime bag,PlayerPassiveInventoryRuntime slots) Player(){
  var bag=new PlayerInventoryRuntime();var slots=new PlayerPassiveInventoryRuntime();bag.Owner=slots;bag.Attach(slots);slots.Attach(bag);
  Set(slots,"_inventoryRuntime",bag);Set(slots,"_changeCooldownSeconds",0f);bag.Pad();
  typeof(PlayerPassiveInventoryRuntime).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(slots,null);return(bag,slots);
 }
 static PassiveItemSO Add(PlayerInventoryRuntime bag,PassiveItemSO source,int index){bag.Pad();while(bag._items.Count<=index)bag._items.Add(new());var instance=(PassiveItemSO)PassiveInstanceRuntime.Create(source);bag._items[index].Set(instance,1);bag.Signal();return instance;}
 public static string Run(){
  Resources.Rules=new PassiveModuleRulesSO{changeCooldownSeconds=0};GameProgressionRuntime.Level=1;GameProgressionRuntime.PassiveEffectsActive=false;
  var combat=Source("combat",PassiveCategory.Combat);var survival=Source("survival",PassiveCategory.Survival,PlayerStatId.MaxHealth,10);
  var other=Source("other",PassiveCategory.Combat,PlayerStatId.PenetrationClass,.3f);
  var (bag,slots)=Player();Check(slots.MaxSlotCount==8&&slots.UnlockedSlotCount==2&&slots.IsSlotUnlocked(0)&&slots.IsSlotUnlocked(1)&&!slots.IsSlotUnlocked(2),"Initial category unlocks");
  var first=Add(bag,combat,0);var second=Add(bag,other,1);var safe=Add(bag,survival,2);
  Check(!slots.TryEquip(first,1,out _),"Cross-category equipment accepted");
  Check(slots.TryEquip(combat,0,out _)&&first.IsBound&&first.EverEquipped&&slots.Items.Count==0,"HOME equip did not bind its owned rolled copy without active effects");
  GameProgressionRuntime.Level=5;var duplicate=Add(bag,combat,4);Check(!slots.TryEquip(duplicate,2,out _),"Same-effect duplicate equipped");GameProgressionRuntime.Level=1;
  Check(slots.TryEquip(second,0,out _)&&slots.EquippedSlots[0]==second,"HOME replacement failed");
  Check(slots.TryEquip(safe,1,out _)&&slots.TryUnequip(1,out _),"HOME survival equip/unequip failed");
  GameProgressionRuntime.PassiveEffectsActive=true;slots.RefreshModuleEffects();
  Check(!slots.TryEquip(first,0,out var locked)&&locked.Contains("HOME"),"Raid replacement accepted");
  Check(!slots.TryEquipFromInventory(2,1,out _)&&!slots.TryEquipFirstAvailable(safe,out _),"Raid empty slot equipped");
  Check(!slots.TryUnequip(0,out _)&&slots.Items.Count==1&&slots.Items.Contains(second),"Raid unequip changed effects");
  GameProgressionRuntime.PassiveEffectsActive=false;slots.RefreshModuleEffects();
  var fresh=Add(bag,combat,3);Check(!fresh.DisabledForRaid&&!fresh.IsBound,"Template mutated by another instance binding");
  // Save reconstruction uses actual instance registry; state survives a new live instance.
  string id=first.Guid;((Dictionary<string,PassiveItemSO>)typeof(PassiveInstanceRuntime).GetField("Live",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Remove(id);
  Check(catalog.TryResolveItem(id,out var resolved)&&((PassiveItemSO)resolved).EverEquipped,"Bound state lost during registry restore");
  for(int level=1;level<=30;level++){
   GameProgressionRuntime.Level=level;int expected=0;
   for(int i=0;i<8;i++){bool unlocked=level>=Resources.Rules.SlotLevel((PassiveCategory)(i%2),i/2);Check(slots.IsSlotUnlocked(i)==unlocked,"Slot-level policy differs");if(unlocked)expected++;}
   Check(slots.UnlockedSlotCount==expected,"Non-contiguous unlock count is wrong");
  }
  GameProgressionRuntime.Level=18;var snapshot=new PlayerRunStateSaveData();slots.CaptureRunState(snapshot);Check(snapshot.equippedPassiveGuids.Count==8&&snapshot.passiveSlotSchemaVersion==2,"Eight-slot save schema missing");
  var (restoreBag,restoreSlots)=Player();for(int i=0;i<bag._items.Count;i++){while(restoreBag._items.Count<=i)restoreBag._items.Add(new());if(!bag._items[i].IsEmpty)restoreBag._items[i].Set(bag._items[i].Item,bag._items[i].Amount);}
  Check(restoreSlots.TryRestoreRunState(snapshot,catalog,out _)&&restoreSlots.EquippedSlots[0]==second,"Eight-slot assignments not restored");
  var (legacyBag,legacySlots)=Player();var raw=Source("legacy",PassiveCategory.Survival,PlayerStatId.MaximumStamina,5);legacyBag._items[0].Set(raw,1);
  var legacy=new PlayerRunStateSaveData{equippedPassiveGuids=new(){raw.Guid,"","","",""},passiveSlotSchemaVersion=0};
  Check(legacySlots.TryRestoreRunState(legacy,catalog,out _)&&legacySlots.EquippedSlots[1]?.IsRolledInstance==true&&legacyBag._items[0].Item==legacySlots.EquippedSlots[1],"Legacy source binding/category migration failed or rolled twice");
  var multiplier=Resources.Rules.ApplyStrength(combat,new PlayerStatModifier{stat=PlayerStatId.AttackDamage,type=StatModifierType.PercentMultiply,value=2});Check(multiplier.value==2.5f,"Strength multiplier incorrectly clamps at one");
  // Successful extraction preserves the equipped loadout; preparation counters reset here.
  SaveSystem.Active.RunData.startingPassives.drawsUsed=2;bag.EndPassiveRaid();
  Check(bag.GetInventoryItemAmount(second)==1&&slots.EquippedSlots[0]==second,"Extraction did not preserve equipped module");
  Check(bag.GetInventoryItemAmount(fresh)==1&&slots.Items.Count==0&&SaveSystem.Active.RunData.startingPassives.drawsUsed==0,"Unused loot or preparation reset lost");
  // Effect activity is run-only even when HOME has prepared slots.
  GameProgressionRuntime.PassiveEffectsActive=false;Check(slots.TryEquip(fresh,0,out _)&&slots.Items.Count==0,"HOME prepared passives apply combat effects");
  GameProgressionRuntime.PassiveEffectsActive=true;slots.RefreshModuleEffects();Check(slots.Items.Count==1,"Prepared passive not active on raid start");
  // Capacity shrinks without sorting. Full stacks leave from the last absolute slot down.
  var storage=Source("storage",PassiveCategory.Survival,PlayerStatId.InventorySlots,4);Resources.Rules.survivalEffectMultiplier=1;
  var (overflow,packSlots)=Player();var pack=Add(overflow,storage,0);
  GameProgressionRuntime.PassiveEffectsActive=false;Check(packSlots.TryEquip(pack,1,out _),"Storage equip failed");
  GameProgressionRuntime.PassiveEffectsActive=true;packSlots.RefreshModuleEffects();overflow.Pad();
  for(int i=1;i<10;i++)overflow._items[i].Set(new ItemSO{name="slot-"+i,Guid="item"+i},1);
  WorldItemPickupSpawner.Dropped.Clear();WorldItemPickupSpawner.Fail=true;
  Check(!packSlots.TryUnequip(1,out _)&&overflow._items[9].Amount==1&&WorldItemPickupSpawner.Dropped.Count==0,"Raid storage module changed capacity");
  GameProgressionRuntime.PassiveEffectsActive=false;packSlots.RefreshModuleEffects();
  Check(overflow._items[9].Amount==1&&WorldItemPickupSpawner.Dropped.Count==0,"Failed spawn deleted/reordered overflow");
  WorldItemPickupSpawner.Fail=false;overflow.DropPassiveCapacityOverflow();
  Check(WorldItemPickupSpawner.Dropped.SequenceEqual(new[]{"slot-9","slot-8","slot-7","slot-6"})&&overflow._items[5].Amount==1,"Overflow did not drop tail-first without compaction");
  // Starting draws: preflight, one deterministic pool, per-preparation counter and negative resale loop.
  GameProgressionRuntime.PassiveEffectsActive=false;GameProgressionRuntime.Level=1;
  SaveSystem.Active.RunData.startingPassives=new();var (drawBag,drawSlots)=Player();
  var module=new ItemSO{Guid="module",name="module"};Resources.Rules.unifiedModule=module;Resources.Rules.startingPool=new[]{combat};
  drawBag._items[0].Set(module,20);for(int i=1;i<6;i++)drawBag._items[i].Set(module,1);
  Check(!drawBag.TryDrawStartingPassive(0,out _)&&drawBag.GetInventoryItemAmount(module)==25&&SaveSystem.Active.RunData.startingPassives.drawsUsed==0,"Full inventory consumed draw/currency");
  drawBag._items[1].Clear();Resources.Rules.startingPool=Array.Empty<PassiveItemSO>();
  Check(!drawBag.TryDrawStartingPassive(0,out _)&&drawBag.GetInventoryItemAmount(module)==24,"Empty pool consumed modules");
  Resources.Rules.startingPool=new[]{combat};Check(drawBag.TryDrawStartingPassive(0,out _)&&drawBag.GetInventoryItemAmount(module)==14&&SaveSystem.Active.RunData.startingPassives.drawsUsed==1,"Starting transaction wrong");
  var rolled=(PassiveItemSO)drawBag._items[1].Item;Check(rolled.MaximumDismantleYield==9&&!rolled.EverEquipped,"Starting draw bound itself or lost refund ceiling");
  Resources.Rules.tiers=new[]{new PassivePartsTier{rarity=rolled.Rarity,moduleYield=100}};
  Check(Resources.Rules.RecyclingRecipeFor(rolled).outputAmount<10,"Draw -> dismantle loop can return its full cost");
  Check(!drawBag.TryDrawStartingPassive(0,out _)&&!drawBag.TryDrawStartingPassive(2,out _),"Draw count/deprecated high tier bypass");
  SaveSystem.Active.RunData.startingPassives.committed=true;Check(drawBag.StartingPassiveDrawsRemaining==0&&!drawBag.TryDrawStartingPassive(0,out _),"Committed preparation can roll again");
  GameProgressionRuntime.PassiveEffectsActive=true;Check(!drawBag.TryDrawStartingPassive(0,out _),"Starting roll works inside raid");
  return $"PASS {count} actual passive/instance/slot/start-draw/overflow checks with managed Unity/storage shims. Not Unity rendering, physics, ES3 or Play Mode.";
 }
}
'@
$code=($shim+"`n"+($parts -join "`n")+"`n"+$runner)-replace '\[Range\(','[UnityEngine.RangeAttribute('
Add-Type -TypeDefinition $code -Language CSharp -IgnoreWarnings -WarningAction SilentlyContinue
[RaidPassiveProbe]::Run()
