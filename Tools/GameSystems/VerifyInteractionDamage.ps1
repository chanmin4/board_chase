param([string]$ProjectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference='Stop'
# Runs real notification classification, DOT entry points and interruption filters using managed Unity shims.
# UI/coroutine scheduling, native Unity input and Play Mode are not exercised.
function Read-Source([string]$path){Get-Content -LiteralPath (Join-Path $ProjectRoot $path) -Raw}
function Member([string]$source,[string]$signature){
 $start=$source.IndexOf($signature,[StringComparison]::Ordinal)
 if($start -lt 0){throw "Missing $signature"};$brace=$source.IndexOf('{',$start);$depth=0
 for($i=$brace;$i -lt $source.Length;$i++){
  if($source[$i] -eq '{'){$depth++}
  if($source[$i] -eq '}'){$depth--;if($depth -eq 0){return $source.Substring($start,$i-$start+1)}}
 };throw "Unclosed $signature"
}
$damage=Read-Source 'Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs'
$world=Read-Source 'Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs'
$medical=Read-Source 'Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs'
$shim=@'
using System;
using UnityEngine;
namespace UnityEngine {
 public class GameObject {}
 public class Component {public GameObject gameObject=new();public T GetComponent<T>()where T:class=>null;public T GetComponentInParent<T>()where T:class=>null;}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);}
}
public class Enemy {public void NotifyDamagedBy(GameObject attacker){}}
public class HitEvent {public void RaiseEvent(GameObject go){}}
public enum EntityDeathCause {HealthDepleted}
public enum DamageNumberKind {Infection}
public class PlayerShooterInfection {public float Exposure,LastHealthLoss;public void AddExposure(float value)=>Exposure+=value;public void AddHitExposure(float amount)=>LastHealthLoss=amount;}
public static class EntityComponentResolver {public static T Resolve<T>(Damageable target)where T:class=>target.PlayerInfection as T;}
public class Damageable:Component {
 public PlayerShooterInfection PlayerInfection;
 public bool IsDead,GetHit;public float _currentHealth=100,_maxHealth=100;private GameObject _lastDamageAttacker;private HitEvent _hitReceivedEvent;
 public event Action<Damageable,float> OnDamaged;
 public bool IsPeriodicStatusDamage {get;private set;}
 private void SyncRuntimeHealthDebug(){}private void NotifyHealthChanged(){}private void ReportDamageNumber(float d,DamageNumberKind k){}
 private float ResolveInfectionDamageOverTimeMultiplier()=>1;
 private void Die(EntityDeathCause cause,string reason)=>IsDead=true;
 public void DirectHit(float damage)=>PublishDamageReceived(damage);
'@
$code=$shim+(Member $damage 'private void PublishDamageReceived')+(Member $damage 'public float ReceiveStatusDamageOverTime')+(Member $damage 'public float ReceiveInfectionDamageOverTime')+"`n}`n"
$code+='public class WorldCast {public int Cancellations;private void CancelForAction()=>Cancellations++;public void Receive(Damageable target,float damage)=>CancelForDamage(target,damage);'+(Member $world 'private void CancelForDamage')+"}`n"
$code+='public class MedicalCast {public int Cancellations;private void CancelActiveUse(string message,bool notify)=>Cancellations++;public void Receive(Damageable target,float damage)=>HandleDamaged(target,damage);'+(Member $medical 'private void HandleDamaged')+"}`n"
$runner=@'
public static class InteractionDamageProbe {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 public static string Run(){
  var target=new Damageable();var world=new WorldCast();var medicine=new MedicalCast();
  target.OnDamaged+=world.Receive;target.OnDamaged+=medicine.Receive;
  target.ReceiveStatusDamageOverTime(5);Check(target._currentHealth==95,"Bleeding damage disabled");
  Check(world.Cancellations==0&&medicine.Cancellations==0,"Bleeding interrupted interaction or medicine");
  Check(!target.IsPeriodicStatusDamage,"Bleeding flag leaked");
  target.ReceiveInfectionDamageOverTime(3,new GameObject());Check(target._currentHealth==92,"Infection damage disabled");
  Check(world.Cancellations==0&&medicine.Cancellations==0,"Infection DOT interrupted interaction or medicine");
  Check(!target.IsPeriodicStatusDamage,"Infection flag leaked");
  target.DirectHit(1);Check(world.Cancellations==1&&medicine.Cancellations==1,"Direct weapon damage no longer interrupts");
  target.ReceiveStatusDamageOverTime(1);target.ReceiveInfectionDamageOverTime(1);
  Check(world.Cancellations==1&&medicine.Cancellations==1,"DOT after direct hit inherited the wrong classification");
  target.DirectHit(1);Check(world.Cancellations==2&&medicine.Cancellations==2,"Direct damage after DOT did not interrupt");
  var nested=new Damageable();var nestedWorld=new WorldCast();nested.OnDamaged+=nestedWorld.Receive;
  bool inner=false;nested.OnDamaged+=(victim,amount)=>{
   if(inner){Check(!victim.IsPeriodicStatusDamage,"Nested direct hit inherited DOT exemption");return;}
   Check(victim.IsPeriodicStatusDamage,"Outer DOT lost classification");inner=true;victim.DirectHit(1);
   Check(victim.IsPeriodicStatusDamage,"Nested hit did not restore outer classification");
  };
  nested.ReceiveInfectionDamageOverTime(1);
  Check(nestedWorld.Cancellations==1&&!nested.IsPeriodicStatusDamage,"Nested hit cancellation or final flag restoration failed");
  foreach(bool infection in new[]{false,true}){
   var broken=new Damageable();broken.OnDamaged+=(v,d)=>throw new InvalidOperationException("listener");
   try {if(infection)broken.ReceiveInfectionDamageOverTime(1);else broken.ReceiveStatusDamageOverTime(1);throw new Exception("Listener exception swallowed");}
   catch(InvalidOperationException){}
   Check(!broken.IsPeriodicStatusDamage,"Listener exception leaked periodic classification");
  }
  var lethal=new Damageable();lethal.ReceiveStatusDamageOverTime(100);Check(lethal.IsDead,"Status death disabled");
  var lethalInfection=new Damageable();lethalInfection.ReceiveInfectionDamageOverTime(100);Check(lethalInfection.IsDead,"Infection DOT death disabled");
  var player=new Damageable{PlayerInfection=new()};var playerWorld=new WorldCast();player.OnDamaged+=playerWorld.Receive;
  player.ReceiveInfectionDamageOverTime(5);
  Check(player._currentHealth==100&&player.PlayerInfection.Exposure==5,"Player infection injection still applies HP DOT");
  Check(playerWorld.Cancellations==0,"Player infection exposure cancels interaction");
  player._currentHealth=3;
  player.ReceiveStatusDamageOverTime(10);
  Check(player.PlayerInfection.LastHealthLoss==3,"Overkill, not actual HP loss, fed the infection gauge");
  Check(playerWorld.Cancellations==0,"Bleeding exposure interrupted interaction");
  return $"PASS {checks} interaction damage checks: actual bleeding/infection damage retained, world/medical casts exempt from status ticks only, direct weapon/nested hits still interrupt, exception-safe flags and lethal status damage. Managed shims; not Unity Play Mode.";
 }
}
'@
Add-Type -TypeDefinition ($code+$runner) -Language CSharp -IgnoreWarnings
[InteractionDamageProbe]::Run()
$direct=Member $damage 'private void ApplyHealthHit'
if(-not $direct.Contains('PublishDamageReceived(finalDamage);')){throw 'Direct attacks must publish non-status damage.'}
