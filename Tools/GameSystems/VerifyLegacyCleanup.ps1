param([string]$ProjectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference='Stop'
function Read-Source([string]$path){Get-Content -LiteralPath (Join-Path $ProjectRoot $path) -Raw}
function Member([string]$source,[string]$signature){
 $start=$source.IndexOf($signature,[StringComparison]::Ordinal)
 if($start -lt 0){throw "Missing $signature"};$brace=$source.IndexOf('{',$start);$depth=0
 for($i=$brace;$i -lt $source.Length;$i++){
  if($source[$i] -eq '{'){$depth++}
  if($source[$i] -eq '}'){$depth--;if($depth -eq 0){return $source.Substring($start,$i-$start+1)}}
 };throw "Unclosed $signature"
}
$wip='Assets/Scripts/WIP/Legacy/MetaProgression/'
$types=Read-Source ($wip+'Scripts/MetaUpgradeTypes.cs')
$catalog=Read-Source ($wip+'Scripts/MetaUpgradeCatalogSO.cs')
$controller=Read-Source ($wip+'Scripts/MetaProgressController.cs')
$save=Read-Source 'Assets/Scripts/Core/Persistence/Save.cs'
$shim=@'
using System;
using UnityEngine;
namespace UnityEngine { public class Sprite {} public class MinAttribute:Attribute {public MinAttribute(float n){}} public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);}}
public struct PlayerStatModifier {}
'@
$code=$shim+(Member $types 'public enum MetaUpgradeId')+(Member $types 'public struct MetaUpgradeSnapshot')+(Member $types 'public struct MetaProgressSnapshot')+(Member $save 'public class MetaUpgradeSaveData')
$code+='public class CatalogProbe {'+(Member $catalog 'public MetaProgressSnapshot BuildSnapshot')+(Member $catalog 'public void ApplyMetaModifiers')+"}`n"
$code+='public class ControllerProbe {'+(Member $controller 'public int GetLevel')+(Member $controller 'public bool TryPurchase')+"}`n"
$code+=@'
public static class LegacyCleanupProbe {
 public static int Run(){
  int checks=0;var saved=new MetaUpgradeSaveData();var catalog=new CatalogProbe();var controller=new ControllerProbe();
  foreach(MetaUpgradeId id in Enum.GetValues(typeof(MetaUpgradeId))){
   saved.SetLevel(id,50);if(saved.GetLevel(id)!=50)throw new Exception("Legacy save payload discarded");checks++;
   if(controller.GetLevel(id)!=0||controller.TryPurchase(id))throw new Exception("Retired purchase/level path active");checks++;
  }
  int bonuses=0;catalog.ApplyMetaModifiers(saved,_=>bonuses++);
  if(bonuses!=0)throw new Exception("Old meta save levels still grant stats");checks++;
  var preview=catalog.BuildSnapshot(saved,int.MaxValue);
  if(preview.currency!=0||preview.upgrades.Length!=0)throw new Exception("Retired catalog still offers purchases");checks++;
  return checks;
 }
}
'@
Add-Type -TypeDefinition $code -Language CSharp -IgnoreWarnings
$checks=[LegacyCleanupProbe]::Run()
$guids=@{
 'Scripts/MetaProgressController.cs'='ac90ba8c90ed9424ebdfeddf71684809';
 'Scripts/MetaUpgradeTypes.cs'='c155bd541bbedc846853fdafe9076560';
 'Scripts/MetaUpgradeCatalogSO.cs'='f540ffd575d06954a92e60c3db9b8a31';
 'Scripts/MetaProgressChangedEventChannelSO.cs'='0b2d89f07e6bfe64f8eef4ec8c0ae127';
 'Scripts/MetaStatsPanelUI.cs'='c00832e1a07efb344950a784c4112421';
 'Scripts/MetaSystemPanelUI.cs'='be3e626f0d41ace479b471e13740c895';
 'Scripts/MetaUpgradePanelUI.cs'='48ba0968b9b2f9e44815222ea0a06f63';
 'Scripts/MetaUpgradeRowUI.cs'='de3147d98fca65847ab3c0a6078fa6be';
 'Assets/METAUpgradeCatalog.asset'='bf6a677aafa387f459b521e906869148';
 'Assets/METAProgressRefreshRequestedEventChannel.asset'='4c8b92db5e7f38143adf84739b9ad2c6';
 'Assets/MetaProgressChangedEventChannel.asset'='cb2f870bda4dd4e4f972e928de8bf814';
 'Prefabs/METAStatsPanel.prefab'='450e3b09091a33a4e9001d9ef08b05ce'
}
foreach($entry in $guids.GetEnumerator()){
 $text=Read-Source ($wip+$entry.Key+'.meta')
 if($text -notmatch ('(?m)^guid: '+$entry.Value+'\s*$')){throw "Moved GUID changed: $($entry.Key)"};$checks++
}
$maker=Read-Source 'Assets/Scripts/Authoring/Editor/CharacterStatusMakerWindow.cs'
if($maker.Contains('"MetaUpgradeCatalogSO"')){throw 'Legacy catalog still listed by Game System Tool'};$checks++
$main=Read-Source 'Assets/Scripts/Presentation/UI/Common/UIMainMenu.cs'
if((Member $main 'private void HandlePlayerUpgradeClicked').Contains('.Invoke(')){throw 'Legacy main-menu entry still invokes progression'};$checks++
if(-not $save.Contains('public MetaUpgradeSaveData _metaUpgrades') -or -not $save.Contains('public List<QuestStatRewardSaveData> _questStatRewards')){throw 'Save compatibility/current quest data removed'};$checks++
$passive=Read-Source 'Assets/Resources/PassiveModuleRules.asset'
if($passive.Contains('dismantleAmount:')){throw 'Retired immediate-dismantle quantity still authored'};$checks++
$rules=Read-Source 'Assets/Resources/RecyclingRules.asset'
if($rules -match '(?m)^    - kind: 1\s*$'){throw 'Duplicated passive recipe still authored'};$checks++
if([regex]::Matches($rules,'(?m)^    - kind: [23]\s*$').Count -ne 10){throw 'Weapon/armor recipes lost'};$checks++
$live=Read-Source 'Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs'
if($live.Contains('ApplyMetaModifiers')){throw 'Active player stats still consume old meta levels'};$checks++
Write-Output "PASS $checks legacy cleanup checks: inert old progression, intact legacy payload API, 12 preserved moved GUIDs, hidden authoring/menu entry, and single-source passive recipes. Managed/source checks, not Unity import or Play Mode."
