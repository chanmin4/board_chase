$ErrorActionPreference = 'Stop'
# Executes production runtime category, weighted-selection and inventory-sort code
# with small managed Unity/inventory shims. No Unity or save files are mutated.
$taskSources = @(
  'Assets/Scripts/Gameplay/Items/Definitions/ValuableCategoryCatalogSO.cs',
  'Assets/Scripts/Gameplay/Items/Definitions/ValuableItemSO.cs',
  'Assets/Scripts/Gameplay/Items/Definitions/CurrencyItemSO.cs',
  'Assets/Scripts/Gameplay/Items/Loot/LootItemCategories.cs',
  'Assets/Scripts/Gameplay/Items/Loot/LootCategorySelection.cs',
  'Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Sorting.cs'
)
$taskShim = @'
using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine {
 public class Object { public string name; }
 public class ScriptableObject : Object { }
 public static class Resources { public static object Value; public static T Load<T>(string s) where T:class => Value as T; }
 public class CreateAssetMenuAttribute:Attribute { public string menuName,fileName; }
 public class SerializeField:Attribute { }
 public class HideInInspector:Attribute { }
 public class TooltipAttribute:Attribute { public TooltipAttribute(string s){} }
 public class MinAttribute:Attribute { public MinAttribute(float f){} }
}
public enum ItemKind { Misc, Passive, Weapon, Armor, Bullet, Currency, Usable, Backpack, GasMask, Key, KeyTool, Valuable }
public enum PlayerShopItemRarity { Normal, Rare, Legendary, Unique, Mythic }
namespace UnityEditor { public static class Undo { public static void RecordObject(object o,string n){} } public static class EditorUtility { public static void SetDirty(object o){} } }
public class ItemSO:UnityEngine.ScriptableObject { public virtual ItemKind Kind=>ItemKind.Misc; public virtual int MaxStack=>1; public PlayerShopItemRarity Rarity; }
public class WeaponSO:ItemSO {}
public class MeleeWeaponSO:WeaponSO {}
public enum RangedWeaponType { SubmachineGun, AssaultRifle, LightMachineGun, Pistol, Revolver, Shotgun, SniperRifle }
public class RangedWeaponSO:WeaponSO { public RangedWeaponType WeaponType; }
public enum BulletFamily { Light, Medium, Heavy, ShotgunShell, Sniper }
public class BulletSO:ItemSO { public BulletFamily Family; }
public class ConsumableItemSO:ItemSO {}
public class ThrowableUsableItemSO:ConsumableItemSO {}
public class MedicalItemSO:ConsumableItemSO {}
public class ArmorItemSO:ItemSO {}
public class GasMaskItemSO:ItemSO {}
public class BackpackItemSO:ItemSO {}
public class KeyItemSO:ItemSO {}
public class KeyToolItemSO:ItemSO {}
public class PassiveItemSO:ItemSO {}
public class LootItemEntry { public ItemSO item; public bool useManualShare; public float manualSharePercent; }
public class LootRarityTier { public PlayerShopItemRarity rarity; public List<LootItemEntry> items=new(); }
public class LootStagePool {
 public List<LootRarityTier> rarityTiers=new(); public LootRarityCategorySettings Settings;
 public LootRarityCategorySettings EnsureCategorySettings(PlayerShopItemRarity rarity)=>Settings??=new LootRarityCategorySettings{rarity=rarity};
}
public class LootTableSO { public LootStagePool Pool=new(); public LootCategorySelection GetCategorySelection(LootRarityTier tier)=>new(tier,Pool.Settings); }
public class PlayerInventoryItemStack { public ItemSO Item; public int Amount=1; public int Condition; public bool IsEmpty=>Item==null||Amount<=0; }
public class PlayerConsumableUseRuntime { public bool IsUsing; }
public partial class PlayerInventoryRuntime {
 public List<PlayerInventoryItemStack> _items=new(); public int PocketCapacity=2; public bool Notified;
 public T GetComponent<T>() where T:class => null; public void NotifyChanged(){Notified=true;}
}
public static class ValuableCategoryProbe {
 static int checks;
 static void Check(bool b,string label) {checks++; if(!b)throw new Exception(label);}
 static void Near(float a,float b,string label){Check(Math.Abs(a-b)<0.001f,label+": "+a+" != "+b);}
 static ValuableItemSO Item(string id,string name){var item=new ValuableItemSO{name=name};typeof(ValuableItemSO).GetField("_valuableCategoryId",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(item,id);return item;}
 static LootItemEntry Entry(ItemSO i,float? manual=null)=>new LootItemEntry{item=i,useManualShare=manual.HasValue,manualSharePercent=manual??0};
 public static string Run(){
  var catalog=new ValuableCategoryCatalogSO(); UnityEngine.Resources.Value=catalog;
  catalog.categories.Add(new ValuableCategoryCatalogSO.Category{id="tools",label="도구"});
  catalog.categories.Add(new ValuableCategoryCatalogSO.Category{id="construction",label="건설자재"});
  catalog.categories.Add(new ValuableCategoryCatalogSO.Category{id="currency",label="화폐"});
  var money = new CurrencyItemSO();
  Check(money.Kind == ItemKind.Valuable && money.ValuableCategoryLabel == "화폐" && money.MaxStack == 500000,"currency classification/stack");
  Check(LootItemCategories.GetKey(money) == "Valuable:currency","currency loot key");
  var currencyTier=new LootRarityTier();
  currencyTier.items.AddRange(new[]{Entry(money),Entry(Item("tools","tool")),Entry(new ArmorItemSO())});
  var currencyRules=new LootRarityCategorySettings{useCustomCategoryWeights=true};
  currencyRules.SetWeight("Currency",10);currencyRules.SetWeight("Valuable",30);currencyRules.SetWeight("Armor",60);
  Near(new LootCategorySelection(currencyTier,currencyRules).CategoryPercent(LootItemCategories.CurrencyKey),10,"legacy currency weight preserved");
  currencyRules.SetWeight(LootItemCategories.CurrencyKey,0);
  Near(new LootCategorySelection(currencyTier,currencyRules).CategoryPercent(LootItemCategories.CurrencyKey),0,"explicit currency weight overrides legacy");
  var a=Entry(Item("tools","A"),20);var b=Entry(Item("construction","B"));var c=Entry(Item("","C"));var d=Entry(new ArmorItemSO());
  Check(LootItemCategories.GetKey(a.item)=="Valuable:tools","stable key");
  Check(((ValuableItemSO)a.item).ValuableCategoryLabel=="도구","label");
  var tier=new LootRarityTier();tier.items.AddRange(new[]{a,b,c,d});
  var auto=new LootCategorySelection(tier,null);
  Near(auto.ItemPercentInTier(a),20,"auto manual remains20");Near(auto.ItemPercentInTier(b),80f/3,"auto automatic preserved");
  var settings=new LootRarityCategorySettings{useCustomCategoryWeights=true};settings.SetWeight("Valuable",40);settings.SetWeight("Armor",60);
  var legacy=new LootCategorySelection(tier,settings);
  Near(legacy.ItemPercentInTier(a),8,"old budget manual20% of40");Near(legacy.ItemPercentInTier(b),16,"old budget automatic40% of40");Near(legacy.ItemPercentInTier(c),16,"old unclassified40% of40");Near(legacy.ItemPercentInTier(d),60,"other weight preserved");
  catalog.categories[0].label="作業道具";
  Check(LootItemCategories.GetKey(a.item)=="Valuable:tools","rename stable key");Near(new LootCategorySelection(tier,settings).ItemPercentInTier(a),8,"rename probability stable");
  var table=new LootTableSO();table.Pool.rarityTiers.Add(tier);table.Pool.Settings=settings;
  ActualLootAuthoring.SetCategoryWeight(table,tier,"Valuable:construction",0);
  var custom=new LootCategorySelection(tier,settings);
  Near(custom.ItemPercentInTier(b),0,"child zero excludes");Near(custom.ItemPercentInTier(a),8f/84*100,"other effective weights retained");
  settings.SetWeight("Valuable:construction",float.NaN);Check(settings.GetWeight("Valuable:construction")==0,"nan sanitized");
  var rng=new Random(291026);for(int i=0;i<2000;i++)Check(custom.Pick(rng)!=b,"disabled group never picked");
  var mixedTier=new LootRarityTier();var ma=Entry(Item("tools","M1"),20);var mb=Entry(Item("tools","M2"));var mc=Entry(Item("construction","M3"));mixedTier.items.AddRange(new[]{ma,mb,mc});
  var mixed=new LootTableSO();mixed.Pool.rarityTiers.Add(mixedTier);var ms=mixed.Pool.EnsureCategorySettings(0);ms.useCustomCategoryWeights=true;ms.SetWeight("Valuable",40);
  var pre=mixed.GetCategorySelection(mixedTier);ActualLootAuthoring.SetCategoryWeight(mixed,mixedTier,"Valuable:construction",16);var post=mixed.GetCategorySelection(mixedTier);
  Near(post.ItemPercentInTier(ma),pre.ItemPercentInTier(ma),"mixed manual sibling preserved");Near(post.ItemPercentInTier(mb),pre.ItemPercentInTier(mb),"mixed automatic sibling preserved");Near(post.ItemPercentInTier(mc),pre.ItemPercentInTier(mc),"edited same weight preserves probability");
  catalog.categories.RemoveAt(0);Check(LootItemCategories.GetKey(a.item)=="Valuable","missing category fallback");Check(((ValuableItemSO)a.item).ValuableCategoryLabel=="미분류","missing label safe");
  catalog.categories.Insert(0,new ValuableCategoryCatalogSO.Category{id="tools",label="도구"});
  var first=new PlayerInventoryItemStack{Item=b.item,Condition=12};var second=new PlayerInventoryItemStack{Item=a.item,Condition=77};var empty=new PlayerInventoryItemStack();
  var stacks=new List<PlayerInventoryItemStack>{empty,first,second};
  Check(PlayerInventoryRuntime.SortStackRange(stacks,0,3,InventorySortMode.Category),"sort changes");
  Check(ReferenceEquals(stacks[0],second)&&ReferenceEquals(stacks[1],first)&&ReferenceEquals(stacks[2],empty),"category order and empty last");Check(stacks[0].Condition==77&&stacks[1].Condition==12,"condition preserved");
  Check(!PlayerInventoryRuntime.SortStackRange(stacks,0,3,InventorySortMode.Category),"stable repeated sort");
  catalog.categories.Reverse();PlayerInventoryRuntime.SortStackRange(stacks,0,3,InventorySortMode.Category);Check(ReferenceEquals(stacks[0],first),"catalog order respected");
  var player=new PlayerInventoryRuntime();player._items.AddRange(new[]{empty,first,second});player.SortInventory(InventorySortMode.Category);Check(ReferenceEquals(player._items[2],second),"pocket and bag never mixed");
  first.Item.Rarity=PlayerShopItemRarity.Unique;second.Item.Rarity=PlayerShopItemRarity.Legendary;PlayerInventoryRuntime.SortStackRange(stacks,0,3,true);Check(ReferenceEquals(stacks[0],second),"legacy rarity rank kept");
  return "PASS "+checks+" production runtime Valuable classification/loot/sort checks. Managed shims, not Unity Play Mode.";
 }
}
'@
$taskUnits = @($taskShim) + @($taskSources | ForEach-Object { [IO.File]::ReadAllText((Join-Path (Get-Location) $_)) })
$taskAuthoring = [IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Editor/LootMakerAuthoring.cs'))
$taskMethodStart = $taskAuthoring.IndexOf('    public static void SetCategoryWeight(')
$taskMethodEnd = $taskAuthoring.IndexOf('    /// <summary>Classify only this box.', $taskMethodStart)
if ($taskMethodStart -lt 0 -or $taskMethodEnd -le $taskMethodStart) { throw 'Cannot locate production authoring method.' }
$taskUnits += 'public static class ActualLootAuthoring {' + $taskAuthoring.Substring($taskMethodStart, $taskMethodEnd - $taskMethodStart) + '}'
# Separate syntax trees preserve production using directives.
$taskCombined = ($taskUnits | ForEach-Object { $_ -replace '(?m)^using [^;\r\n]+;\r?\n','' }) -join "`n"
Add-Type -TypeDefinition ("#pragma warning disable 0649`nusing System;`nusing System.Collections.Generic;`nusing System.Reflection;`nusing UnityEngine;`nusing UnityEditor;`n" + $taskCombined) -WarningAction SilentlyContinue
[ValuableCategoryProbe]::Run()
