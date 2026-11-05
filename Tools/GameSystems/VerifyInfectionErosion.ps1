param([string]$ProjectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference='Stop'
# Executes actual health setters and infection resource/death methods with managed lifecycle shims.
# Does not simulate Unity callbacks, physics, authored UI or Play Mode.
function Member([string]$source,[string]$signature){
 $start=$source.IndexOf($signature,[StringComparison]::Ordinal)
 if($start -lt 0){throw "Missing $signature"};$brace=$source.IndexOf('{',$start);$depth=0
 for($i=$brace;$i -lt $source.Length;$i++){
  if($source[$i] -eq '{'){$depth++}
  if($source[$i] -eq '}'){$depth--;if($depth -eq 0){return $source.Substring($start,$i-$start+1)}}
 };throw "Unclosed $signature"
}
$health=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs') -Raw
$infection=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterInfection.cs') -Raw
$player=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs') -Raw
$shim=@'
using System;
using UnityEngine;
namespace UnityEngine {
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);public static float Clamp01(float v)=>Math.Clamp(v,0,1);}
 public static class Debug {public static void LogError(object message,object context){}}
}
public enum EntityDeathCause {None,HealthDepleted,InfectionOvertake}
public enum DamageNumberKind {Heal}
public partial class Damageable {
 float _maxHealth=100,_uninfectedMaxHealth=100,_infectionMaxHealthLoss,_currentHealth=100;
 public bool IsDead;public EntityDeathCause LastDeathCause;public Action Changed;public object _lastDamageAttacker;
 public float MaxHealth=>_maxHealth;public float UninfectedMaxHealth=>_uninfectedMaxHealth;public float CurrentHealth=>_currentHealth;
 void SyncRuntimeHealthDebug(){}void NotifyHealthChanged()=>Changed?.Invoke();void ReportDamageNumber(float n,DamageNumberKind kind){}
 void Die(EntityDeathCause cause,string detail){_currentHealth=0;IsDead=true;LastDeathCause=cause;Changed?.Invoke();}
 public void Kill(EntityDeathCause cause)=>Die(cause,"");
}
public partial class ShooterInfection {
 public Damageable Health=new();Damageable _damageable;bool _isDead;float _currentInfection;
 public float CurrentHealth=>Health.CurrentHealth;public float InfectionLimit=>Health.UninfectedMaxHealth;public float InfectionGainMultiplier=1;
 public bool IsDead=>_isDead;public float Loss=>_currentInfection;public float CurrentInfection=>_currentInfection;
 public ShooterInfection(){_damageable=Health;Health.Changed=CheckDeathState;}
 void StartRecoveryWait(){}void LogInfectionLifecycle(string a,string b){}void PublishCurrentSnapshot(){}
 void MarkDead(bool infection){_isDead=true;}
}
public class RaidRunSaveData {public float ampouleRemainingSeconds,ampouleRecoveryPerSecond,ampouleExposureMultiplier=1;}
public class FakeStatusRuntime {public bool Removed;public float RemainingLoss;public void Remove(object definition)=>Removed=true;public void RefreshInfectionErosionStacks(float loss)=>RemainingLoss=loss;}
public class InfectionAggravationStatusSO {public static object Active=new();}
public static class GameProgressionRuntime {public static bool InRaid=true;}
public enum PlayerStatId {SuppressionDurationBonus}
public class PlayerStatsRuntime {public float AdditionalStat(PlayerStatId stat)=>0;}
public static class EntityComponentResolver {public static T Resolve<T>(object owner)where T:class=>null;}
public partial class PlayerShooterInfection:ShooterInfection {
 public RaidRunSaveData MedicalState=new();public FakeStatusRuntime StatusEffects=new();
 float _homeAmpouleSeconds,_homeAmpouleRate,_homeAmpouleMultiplier=.5f;
 public bool IsAmpouleActive=>(MedicalState!=null?MedicalState.ampouleRemainingSeconds:_homeAmpouleSeconds)>0;
}
'@
$parts=@('public partial class Damageable {')
foreach($signature in @('public void SetMaxHealth(','public void SetInfectionMaxHealthLoss(','public void SetCurrentHealth(','public void Heal(','public void Revive(')){
 $parts+=Member $health $signature
}
$parts+='}';$parts+='public partial class ShooterInfection {'
foreach($signature in @('public void ReceiveStatusInfectionDamage(','public void ReduceInfection(','public void ClearInfection(','public void RestoreCurrentInfection(','private void ClampInfectionToCurrentHealth(','private void CheckDeathState(')){
 $parts+=Member $infection $signature
}
$parts+='}'
$parts+=((Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionAggravationState.cs') -Raw)-replace '(?m)^using [^\r\n]+;\r?\n','')
$parts+='public partial class PlayerShooterInfection {'
foreach($signature in @('public bool TryStartAmpoule(','public bool TryUseInfectionSyringe(','public bool TryPurifyAtHome(')){$parts+=Member $player $signature}
$parts+='}'
$runner=@'
public static class ErosionProbe {
 static int count;static void Check(bool ok,string why){count++;if(!ok)throw new Exception(why);}
 public static string Run(){
  var player=new ShooterInfection();player.ReceiveStatusInfectionDamage(20);
  Check(player.Health.MaxHealth==80&&player.CurrentHealth==80,"20 erosion must produce 80/80");
  player.Health.SetCurrentHealth(30);player.ReceiveStatusInfectionDamage(35);
  Check(player.Loss==55&&player.Health.MaxHealth==45&&player.CurrentHealth==30&&!player.IsDead,"Old infection >= current HP death rule remains");
  player.ReduceInfection(20);Check(player.Health.MaxHealth==65&&player.CurrentHealth==30,"Syringe healed current HP or failed to restore ceiling");
  player.Health.Heal(500);Check(player.CurrentHealth==65,"Heal bypassed eroded ceiling");
  player.Health.SetMaxHealth(150,false);Check(player.Health.MaxHealth==115&&player.CurrentHealth==65&&player.Loss==35,"Stat rebuild lost/doubled erosion");
  player.ClearInfection();Check(player.Health.MaxHealth==150&&player.CurrentHealth==65,"HOME purification healed ordinary wounds");
  player.ReceiveStatusInfectionDamage(100);Check(player.Health.MaxHealth==50&&!player.IsDead,"Remaining HP threshold revived");
  player.ReceiveStatusInfectionDamage(50);Check(player.Health.MaxHealth==0&&player.CurrentHealth==0&&player.IsDead&&player.Health.LastDeathCause==EntityDeathCause.InfectionOvertake,"Zero ceiling must kill");
  player.Health.Revive();Check(player.Health.IsDead&&player.CurrentHealth==0,"Revive bypassed exhausted maximum HP");
  var restored=new ShooterInfection();restored.RestoreCurrentInfection(80);restored.Health.SetCurrentHealth(12);
  Check(restored.Health.MaxHealth==20&&restored.CurrentHealth==12&&!restored.IsDead,"Save restoration revived old lethal threshold");
  foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f,0f}){
   restored.ReceiveStatusInfectionDamage(invalid);restored.ReduceInfection(invalid);
   Check(restored.Loss==80&&restored.Health.MaxHealth==20,"Invalid treatment/erosion accepted");
  }
  var medic=new PlayerShooterInfection();
  Check(medic.TryStartAmpoule(3,15,.5f),"Preventive ampoule requires existing infection/suppression");
  Check(medic.MedicalState.ampouleRemainingSeconds==15&&medic.MedicalState.ampouleExposureMultiplier==.5f,"Ampoule prevention not stored");
  Check(!medic.TryStartAmpoule(100,90,.1f)&&medic.MedicalState.ampouleRemainingSeconds==15,"Ampoule stacks/refreshes");
  medic.ReceiveStatusInfectionDamage(20);Check(medic.TryUseInfectionSyringe(10)&&medic.Health.MaxHealth==90&&medic.CurrentHealth==80&&medic.StatusEffects.RemainingLoss==10,"Syringe ceiling-only recovery");
  Check(!medic.TryUseInfectionSyringe(float.NaN),"Invalid syringe accepted");
  Check(!medic.TryPurifyAtHome(),"Purifier works inside raid");
  GameProgressionRuntime.InRaid=false;medic.MedicalState=null;
  Check(medic.TryPurifyAtHome()&&medic.Loss==0&&medic.CurrentHealth==80&&medic.StatusEffects.Removed,"HOME purification did not clear erosion/status");
  Check(!medic.TryStartAmpoule(3,15,.5f),"Preventive raid medicine works at HOME");
  for(int hp=1;hp<=100;hp++)for(int loss=0;loss<100;loss++){
   var sample=new ShooterInfection();sample.Health.SetCurrentHealth(hp);sample.ReceiveStatusInfectionDamage(loss);
   Check(!sample.IsDead&&sample.Health.MaxHealth==100-loss&&sample.CurrentHealth==Math.Min(hp,100-loss),"Erosion matrix includes spurious death/double HP damage");
  }
  return $"PASS {count} actual health-setter/infection-resource checks (managed lifecycle shims, not Unity Play Mode).";
 }
}
'@
Add-Type -TypeDefinition ($shim+"`n"+($parts -join "`n")+"`n"+$runner) -Language CSharp
[ErosionProbe]::Run()
$syringeGuid='4fb761ecfe524c98ae16f39f6cad8ad7'
$syringe=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/InfectionSyringe.asset') -Raw
if($syringe -notmatch '_medicineKind: 1' -or $syringe -notmatch '_healAmount: 0' -or $syringe -notmatch '_infectionRecoveryAmount: 20'){throw 'Syringe authoring contract changed'}
foreach($path in @('Assets/Resources/RuntimeContentCatalog.asset','Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidLoot_Medical.asset','Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_MedicalBox_Small.asset')){
 if(-not (Get-Content -LiteralPath (Join-Path $ProjectRoot $path) -Raw).Contains($syringeGuid)){throw "Missing syringe registration: $path"}
}
if((Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets/ScriptableObject/GameScene/Shop') -Recurse -Filter '*.asset' | Select-String -SimpleMatch $syringeGuid)){throw 'Raid-only syringe registered in a shop'}
$restore=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs') -Raw
if($restore.IndexOf('infection.RestoreCurrentInfection(source.currentInfection)') -gt $restore.IndexOf('damageable.SetCurrentHealth(source.currentHealth)')){throw 'Restoring HP before erosion loses wounded health'}
Write-Output 'PASS syringe catalog/medical loot/shop exclusion and cap-before-HP save restoration source contracts.'
