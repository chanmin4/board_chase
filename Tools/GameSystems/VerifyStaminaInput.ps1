param([string]$ProjectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference='Stop'
# Executes the complete production stamina class with Unity clock/component shims; not a Play Mode test.
$shim=@'
using System;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public class Component {public GameObject gameObject=new();}
 public class MonoBehaviour:Component {}
 public class GameObject {public T GetComponent<T>()where T:class=>null;public T AddComponent<T>()where T:new()=>new();}
 public class HideInInspector:Attribute {} public class MinAttribute:Attribute {public MinAttribute(float v){}}
 public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}} public class TooltipAttribute:Attribute {public TooltipAttribute(string t){}}
 public class DisallowMultipleComponent:Attribute {}public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int n){}}
 public static class Time {public static float time,deltaTime=.1f,timeScale=1;}
 public static class Debug {public static void LogWarning(string s,object context){}}
 public static class JsonUtility {public static T FromJson<T>(string s)=>default;public static string ToJson(object o)=>"";}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);public static float Clamp01(float v)=>Clamp(v,0,1);}
}
public enum PlayerStatId {MaximumStamina}
public class StatusEffects {public bool BlocksActions;}
public class ShooterStatsRuntime:Component {public object StatConfig;public StatusEffects StatusEffects;public float StaminaRecoveryMultiplier=1;public float AdditionalStat(PlayerStatId stat,float baseline)=>baseline;}
public class EnemyShooterStatsRuntime:ShooterStatsRuntime {}
public class ShooterStatConfigSO {public ShooterLocomotionSettings Locomotion=new();public ShooterAimFeedbackSettings AimFeedback=new();public float MaximumStamina=200,EnemySprintStartDistance=12,EnemySprintStopDistance=8;public bool EnemyCanSprint=true;}
public class Damageable {public bool IsDead,IsPeriodicStatusDamage;public event Action<Damageable,float> OnDamaged;}
public class ShooterDashController {public bool IsDashing;}
public class PlayerAimController {public void ApplyHitKick(){}}
public static class EntityComponentResolver {public static T Resolve<T>(object source)where T:class=>null;}
public static class GamePause {public static bool IsPaused;}
'@
$source=(Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStaminaRuntime.cs') -Raw)-replace '(?m)^using [^\r\n]+;\r?\n',''
$carry=((Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.CarryWeight.cs') -Raw) -split 'public partial class PlayerInventoryRuntime',2)[0] -replace '(?m)^using [^\r\n]+;\r?\n',''
$runner=@'
public static class StaminaInputProbe {
 static int checks;static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
 static ShooterStaminaRuntime Make(float current,float maximum=200){
  var s=new ShooterStaminaRuntime();Set(s,"_temporarySettings",new ShooterLocomotionSettings{maximumStamina=maximum});Set(s,"_initialized",true);Set(s,"_current",current);return s;
 }
 static void Tick(ShooterStaminaRuntime s,float dt){Time.deltaTime=dt;Time.time+=dt;typeof(ShooterStaminaRuntime).GetMethod("Update",Private).Invoke(s,null);}
 public static string Run(){
  foreach(float current in new[]{0f,18f,19.999f,20f,24f,39f,40f,80f}){
   var s=Make(current);bool expected=current>=20;
   Check(s.TrySpendDash()==expected,"Wrong dash start boundary "+current);
   Check(Math.Abs(s.Current-(expected?Math.Max(0,current-50):current))<.00001f,"Dash cost/zero clamp "+current);
  }
  foreach(float maximum in new[]{200f,400f}){
   var s=Make(maximum*.12f,maximum);s.SetSprintRequested(true);
   Tick(s,.1f);Check(s.IsSprinting,"12% sprint did not start");
   bool crossed=false;
   for(int i=0;i<100&&s.Current>0;i++){
    s.SetSprintRequested(true);Tick(s,.1f);
    if(s.Current>0&&s.Normalized<.1f){crossed=true;Check(s.IsSprinting,"Held sprint stopped below 10%");}
   }
   Check(crossed&&s.Current==0&&!s.IsSprinting,"Held sprint did not reach zero");
   for(int i=0;i<100;i++){s.SetSprintRequested(true);Tick(s,.1f);}
   Check(s.Current>=maximum*.1f&&!s.IsSprinting,"Empty-pool held input restarted after recovery");
   s.SetSprintRequested(false);s.SetSprintRequested(true);Tick(s,.1f);Check(s.IsSprinting,"Release/repress after recovery rejected");
  }
  var denied=Make(18);denied.SetSprintRequested(true);Tick(denied,0);Check(!denied.IsSprinting,"Sub-10% sprint accepted");
  Set(denied,"_current",50f);denied.SetSprintRequested(true);Tick(denied,0);Check(!denied.IsSprinting,"Denied held key started on recovery");
  denied.SetSprintRequested(false);denied.SetSprintRequested(true);Tick(denied,0);Check(denied.IsSprinting,"New eligible press rejected");
  var stationary=Make(18);stationary.SetSprintRequested(true,false);Set(stationary,"_current",50f);stationary.SetSprintRequested(true,true);Tick(stationary,0);
  Check(!stationary.IsSprinting,"Movement start reinterpreted an ineligible held key as a new press");
  var moving=Make(24);moving.SetSprintRequested(true,false);Set(moving,"_current",18f);moving.SetSprintRequested(true,true);Tick(moving,0);
  Check(moving.IsSprinting,"Movement start discarded the original eligible press");
  moving.SetSprintRequested(false);moving.SetSprintRequested(true);Tick(moving,0);Check(!moving.IsSprinting,"Release/repress below 10% allowed");
  var exact=Make(20);exact.SetSprintRequested(true);Tick(exact,0);Check(exact.IsSprinting,"Exactly 10% must be accepted");
  var combo=Make(58,400);combo.SetSprintRequested(true);Check(combo.TrySpendDash()&&combo.Current==8,"Enemy dash remaining-pool spend failed");combo.SetSprintRequested(true);Tick(combo,0);
  Check(combo.IsSprinting,"Dash invented a new sprint key press instead of preserving the held input");
  var noResource=Make(0);noResource.Settings.enabled=false;Check(noResource.TrySpendDash(),"Disabled stamina blocks dash");
  foreach(var row in new(float kg,float speed,float standing,float moving)[]{
   (0,1,1,1),(48.999f,1,1,1),(49,.8f,1,1),(52.499f,.8f,1,1),(52.5f,.75f,1,1),
   (55.999f,.75f,1,1),(56,.7f,.5f,.5f),(59.499f,.7f,.5f,.5f),(59.5f,.6f,.5f,.5f),
   (62.999f,.6f,.5f,.5f),(63,.5f,.5f,0),(69.999f,.5f,.5f,0),(70,.2f,0,0),(105,.2f,0,0)}){
   var load=new PlayerCarryLoad(row.kg,70);
   Check(load.WeightKg==row.kg&&load.MoveSpeedMultiplier==row.speed,"Wrong carry load/speed at "+row.kg+"kg");
   Check(load.RequiresStandingForRecovery==(row.kg>=63&&row.kg<70),"Wrong standing requirement at "+row.kg+"kg");
   foreach(bool movingFlag in new[]{false,true}){
    float expected=movingFlag?row.moving:row.standing;
    Check(load.RecoveryMultiplier(movingFlag)==expected,"Wrong carry recovery at "+row.kg+"kg");
    var subject=Make(50);
    Set(subject,"_stats",new ShooterStatsRuntime{StaminaRecoveryMultiplier=load.RecoveryMultiplier(movingFlag)});
    Tick(subject,1);Check(subject.Current==50+subject.Settings.recoveryPerSecond*expected,"Recovery tick ignores carry multiplier");
   }
  }
  Check(new PlayerCarryLoad(105,70).Ratio==1.5f,"Overcapacity ratio must not be clamped");
  foreach(float maximum in new[]{200f,400f}){
   Time.time=0;var run=Make(maximum,maximum);run.SetSprintRequested(true);Tick(run,.1f);
   Check(Math.Abs(run.Current-(maximum-2))<.001f,"Sprint must cost 2 per 0.1 seconds");
   for(int i=0;i<9;i++)Tick(run,.1f);
   Check(Math.Abs(run.Current-(maximum-20))<.001f,"Sprint must cost 20 per second");
   run.SetSprintRequested(false);float remaining=run.Current;
   Tick(run,1.5f);Check(run.Current==remaining,"Recovery before 1.5s after last sprint spend");
   Tick(run,.49f);Check(run.Current==remaining,"Recovery before 2s after last sprint spend");
   Tick(run,.02f);Check(run.Current>remaining,"Recovery not started after 2s following sprint");
   Time.time=100;var dash=Make(maximum,maximum);Check(dash.TrySpendDash(),"Initial dash failed");
   Check(dash.Current==maximum-50,"Dash must spend 50");Tick(dash,1.5f);
   Check(dash.Current==maximum-50,"Dash recovery started before 2s");
   Check(dash.TrySpendDash(),"Second dash failed");remaining=dash.Current;
   Tick(dash,1.5f);Check(dash.Current==remaining,"Second spend did not reset recovery delay");
   Tick(dash,.49f);Check(dash.Current==remaining,"Recovery before refreshed deadline");
   Tick(dash,.02f);Check(dash.Current>remaining,"Recovery missing after refreshed deadline");
   var low=Make(maximum*.12f,maximum);Check(low.TrySpendDash()&&low.Current==0,"12% dash must still clamp to zero");
  }
  return $"PASS {checks} stamina and carry checks: sprint/dash thresholds, costs and recovery delay; carry boundary speed and moving/stationary recovery at 0-150%. Actual source with managed shims; Unity input/Play Mode not run.";
 }
}
'@
$code=($shim+"`n"+$source+"`n"+$carry+"`n"+$runner)-replace '\[Range\(','[UnityEngine.RangeAttribute('
Add-Type -TypeDefinition $code -Language CSharp -IgnoreWarnings
[StaminaInputProbe]::Run()
$common=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/Resources/ShooterCommonRules.asset') -Raw
foreach($field in @('sprintCostPerSecond: 20','dashCost: 50','recoveryDelay: 2')){
 if($common -notmatch ('(?m)^    '+[regex]::Escape($field)+'\r?$')){throw "Shared stamina asset mismatch: $field"}
}
$configs=Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets/ScriptableObject/GameScene/Character/Shooter') -Recurse -Filter '*.asset' | Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match '_overrideCommonLocomotion:' }
foreach($config in $configs){
 $data=Get-Content -LiteralPath $config.FullName -Raw
 if($data -notmatch '(?m)^  _overrideCommonLocomotion: 0\r?$'){throw "Character no longer uses shared stamina; inspect explicit override: $($config.FullName)"}
}
"PASS shared stamina asset 20/50/2 and $($configs.Count) character configs using common locomotion."
