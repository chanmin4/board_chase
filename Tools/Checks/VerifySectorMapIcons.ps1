param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference = 'Stop'
# Actual icon policy/settings source, compiled with minimal engine-type shims. Not a Unity/UI test.
function Read-MapSource([string]$relative) { Get-Content -LiteralPath (Join-Path $ProjectRoot $relative) -Raw }
function Read-MapMember([string]$source, [string]$signature) {
    $start = $source.IndexOf($signature, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing source member: $signature" }
    $brace = $source.IndexOf('{', $start); $depth = 0
    for ($i = $brace; $i -lt $source.Length; $i++) {
        if ($source[$i] -eq '{') { $depth++ }
        if ($source[$i] -eq '}') { $depth--; if ($depth -eq 0) { return $source.Substring($start, $i - $start + 1) } }
    }
    throw "Unclosed member: $signature"
}
$taskShim = @'
using System;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public class ScriptableObject {}
 public class Sprite { public string name; public Sprite(string value) {name=value;} }
 public struct Vector2Int { public int x,y; }
 public struct Color { public Color(float r,float g,float b,float a=1) {} }
 public sealed class SerializeField : Attribute {}
 public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string text) {} }
 public sealed class RangeAttribute : Attribute { public RangeAttribute(float a,float b) {} }
 public sealed class CreateAssetMenuAttribute : Attribute { public string fileName,menuName; }
}
namespace MapIconProbe {
'@
$taskMapSettings = (Read-MapSource 'Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/ScriptableObject/MiniMapUISettingsSO.cs') -replace '(?m)^using [^\r\n]+;\r?\n','' -replace '\[SerializeField, Range\(', '[SerializeField, UnityEngine.RangeAttribute('
$taskPolicy = (Read-MapSource 'Assets/Scripts/Presentation/UI/Gameplay/Map/SectorMapIconPolicy.cs') -replace '(?m)^using [^\r\n]+;\r?\n',''
$taskShared = (Read-MapSource 'Assets/Scripts/Presentation/UI/Gameplay/Map/SectorMapIconSetSO.cs') -replace '(?m)^using [^\r\n]+;\r?\n',''
$taskNode = (Read-MapSource 'Assets/Scripts/Gameplay/World/Sectors/StageMap/StageRoomNode.cs') -replace '(?m)^using [^\r\n]+;\r?\n',''
$taskFlags = Read-MapMember (Read-MapSource 'Assets/Scripts/Gameplay/World/Sectors/StageMap/StageRoomType.cs') 'public enum StageRoomConnectionFlags'
$taskKey = Read-MapMember (Read-MapSource 'Assets/Scripts/Gameplay/Items/Definitions/KeyItemSO.cs') 'public enum LootKeyTier'
$taskTrait = Read-MapMember (Read-MapSource 'Assets/Scripts/Gameplay/World/Sectors/Traits/SectorRoomTraitTableSO.cs') 'public enum SectorRoomTraitType'
$taskRoles = Read-MapMember (Read-MapSource 'Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapSettings.cs') 'public enum RaidRoomRole'
$taskRoomTypes = Read-MapMember (Read-MapSource 'Assets/Scripts/Gameplay/World/Sectors/StageMap/StageRoomType.cs') 'public enum StageRoomType'
$taskOccupancy = Read-MapSource 'Assets/Scripts/Gameplay/World/Sectors/SectorOccupancy.cs'
$taskEnums = @('public enum SectorOwner','public enum SectorContestState','public enum SectorSpecialState') | ForEach-Object {Read-MapMember $taskOccupancy $_}
$taskSnapshot = Read-MapMember (Read-MapSource 'Assets/Scripts/Core/Events/ScriptableObject/SectorMapSnapshotEventChannelSO.cs') 'public struct SectorMapCellSnapshot'
$taskRunner = @'
public static class Probe {
 static int checks;
 static void Set(object owner,string field,object value) => owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);
 static void Equal(Sprite actual,Sprite expected,string reason) {checks++; if(!ReferenceEquals(actual,expected)) throw new Exception(reason);}
 static void Kind(SectorMapIconKind actual,SectorMapIconKind expected,string reason) {checks++; if(actual!=expected) throw new Exception(reason);}
 public static string Run() {
  var settings=new MiniMapUISettingsSO(); var art=new Sprite("retired-preset-image"); var fallback=new Sprite("fallback");
  foreach(var field in new[]{"_lockedIcon","_namedIcon","_bossIcon","_hatchIcon","_generatorIcon","_extractionIcon","_merchantIcon","_armoryIcon","_medicalIcon","_vaultIcon"}) Set(settings,field,new Sprite(field));
  var icons=new SectorMapIconSetSO(); Set(settings,"_sectorIcons",icons);
  foreach(var field in typeof(SectorMapIconSetSO).GetFields(BindingFlags.Instance|BindingFlags.NonPublic)) if(field.FieldType==typeof(Sprite)) field.SetValue(icons,new Sprite(field.Name));
  foreach(SectorMapIconKind kind in Enum.GetValues(typeof(SectorMapIconKind))) foreach(bool known in new[]{false,true}) foreach(bool locked in new[]{false,true}) {
   var snapshot=new SectorMapCellSnapshot {roomType=StageRoomType.Normal,raidRole=RaidRoomRole.Normal,mapIconKind=kind,isRevealed=known,isLocked=locked,mapIcon=art};
   var expectedKind=!known||locked?SectorMapIconKind.Unknown:kind==SectorMapIconKind.None?SectorMapIconKind.NormalCombat:kind;
   Equal(SectorMapIconPolicy.MiniMapIcon(snapshot,settings),icons.GetIcon(expectedKind),"Shared mini-map kind mismatch");
   Equal(SectorMapIconPolicy.SharedIcon(snapshot,icons,fallback),icons.GetIcon(expectedKind),"Shared full-map kind mismatch");
  }
  var roleKinds=new System.Collections.Generic.Dictionary<RaidRoomRole,SectorMapIconKind> {
   [RaidRoomRole.Hatch]=SectorMapIconKind.Hatch,[RaidRoomRole.Generator]=SectorMapIconKind.Generator,
   [RaidRoomRole.Extraction]=SectorMapIconKind.Extraction,[RaidRoomRole.Merchant]=SectorMapIconKind.Merchant,
   [RaidRoomRole.Boss]=SectorMapIconKind.Boss,[RaidRoomRole.Armory]=SectorMapIconKind.Armory,
   [RaidRoomRole.Medical]=SectorMapIconKind.Medical,[RaidRoomRole.Vault]=SectorMapIconKind.Vault,
   [RaidRoomRole.UnknownCandidate]=SectorMapIconKind.Unknown };
  foreach(RaidRoomRole role in Enum.GetValues(typeof(RaidRoomRole))) foreach(bool known in new[]{false,true}) foreach(bool locked in new[]{false,true}) foreach(bool extraction in new[]{false,true}) foreach(bool publicMarker in new[]{false,true}) {
   var snapshot=new SectorMapCellSnapshot {raidRole=role,roomType=StageRoomType.Normal,isRevealed=known,isLocked=locked,isExtraction=extraction,hasPublicRaidMarker=publicMarker,mapIconKind=SectorMapIconKind.Office,mapIcon=art};
   var rk=roleKinds.TryGetValue(role,out var mapped)?mapped:SectorMapIconKind.None;
   var expected=extraction?SectorMapIconKind.Extraction:rk;
   if(!known||locked) expected=publicMarker&&expected!=SectorMapIconKind.None?expected:SectorMapIconKind.Unknown;
   else if(expected==SectorMapIconKind.None) expected=SectorMapIconKind.Office;
   Equal(SectorMapIconPolicy.MiniMapIcon(snapshot,settings),icons.GetIcon(expected),"Discovery/public landmark mismatch");
   Equal(SectorMapIconPolicy.SharedIcon(snapshot,icons,fallback),icons.GetIcon(expected),"Full-map landmark mismatch");
  }
  foreach(var pair in new[]{("office",SectorMapIconKind.Office),("storage",SectorMapIconKind.Warehouse),("warehouse",SectorMapIconKind.Warehouse),("security",SectorMapIconKind.Armory),("armory",SectorMapIconKind.Armory),("medical",SectorMapIconKind.Medical),("vault",SectorMapIconKind.Vault),("server",SectorMapIconKind.Server)}) {
   var room=new StageRoomNode(default,StageRoomType.Normal,StageRoomConnectionFlags.None) {raidRole=RaidRoomRole.Normal,specialSectorPoolId=pair.Item1};
   Kind(SectorMapIconPolicy.KindForRoom(room),pair.Item2,"Special pool kind mismatch");
  }
  var candidate=new StageRoomNode(default,StageRoomType.Normal,StageRoomConnectionFlags.None) {raidRole=RaidRoomRole.Boss};
  Kind(SectorMapIconPolicy.KindForRoom(candidate),SectorMapIconKind.NormalCombat,"Absent boss shows boss image");
  candidate.hasMerchant=true; Kind(SectorMapIconPolicy.KindForRoom(candidate),SectorMapIconKind.Merchant,"Merchant overlay kind missing");
  candidate.hasMerchant=false; candidate.combatPoolId="theme";
  Kind(SectorMapIconPolicy.KindForRoom(candidate),SectorMapIconKind.ThemedCombat,"Themed kind missing");
  var plain=new SectorMapCellSnapshot {roomType=StageRoomType.Normal,isRevealed=true,mapIcon=art,mapIconKind=SectorMapIconKind.NormalCombat};
  foreach(var replacement in new[]{new Sprite("normal-one"),new Sprite("normal-two")}) {
   Set(icons,"_normalCombat",replacement);
   Equal(SectorMapIconPolicy.MiniMapIcon(plain,settings),replacement,"Shared edit did not update mini-map");
   Equal(SectorMapIconPolicy.SharedIcon(plain,icons,fallback),replacement,"Shared edit did not update full-map");
  }
  Equal(SectorMapIconPolicy.SharedIcon(plain,null,fallback),fallback,"Retired preset image still active");
  plain.isRevealed=false;
  Equal(SectorMapIconPolicy.MiniMapIcon(plain,null,fallback),fallback,"Hidden custom art leaked without settings");
  var start=new SectorMapCellSnapshot {isStartSector=true,isRevealed=true};
  Equal(SectorMapIconPolicy.MiniMapIcon(start,settings),settings.GetLandmarkIcon(RaidRoomRole.Hatch),"Start fallback differs from runtime mini-map");
  Set(icons,"_vault",null); var vault=new SectorMapCellSnapshot {isRevealed=true,raidRole=RaidRoomRole.Vault};
  Equal(SectorMapIconPolicy.SharedIcon(vault,icons,fallback),null,"Cleared shared slot still displays a legacy sprite");
  foreach(var kind in new[]{SectorMapIconKind.NormalCombat,SectorMapIconKind.ThemedCombat,SectorMapIconKind.Office})
   foreach(bool known in new[]{false,true}) foreach(bool locked in new[]{false,true}) foreach(bool publicExit in new[]{false,true}) {
    var exit=new SectorMapCellSnapshot {isExtraction=true,isRevealed=known,isLocked=locked,hasPublicRaidMarker=publicExit,
     raidRole=RaidRoomRole.Extraction,roomType=StageRoomType.Normal,mapIconKind=kind};
    var underlying=SectorMapIconPolicy.RoomIconSnapshot(exit);
    Kind(SectorMapIconPolicy.VisibleKind(underlying),known&&!locked?kind:SectorMapIconKind.Unknown,"Exit disclosed or replaced underlying room");
    Equal(SectorMapIconPolicy.MiniMapIcon(underlying,settings),icons.GetIcon(known&&!locked?kind:SectorMapIconKind.Unknown),"Wrong left sprite");
    Equal(SectorMapIconPolicy.ExtractionOverlay(exit,icons,fallback),known||publicExit?icons.GetIcon(SectorMapIconKind.Extraction):null,"Wrong right sprite/discovery");
    if(!exit.isExtraction || exit.raidRole!=RaidRoomRole.Extraction) throw new Exception("Room normalization mutated snapshot");
   }
  foreach(var kind in new[]{SectorMapIconKind.Hatch,SectorMapIconKind.NormalCombat}) {
   var hatch=new SectorMapCellSnapshot {isStartSector=kind==SectorMapIconKind.NormalCombat,isExtraction=true,isRevealed=true,
    raidRole=RaidRoomRole.Hatch,mapIconKind=kind};
   Equal(SectorMapIconPolicy.ExtractionOverlay(hatch,icons,fallback),null,"Hatch got exterior overlay");
  }
  var terminal=new StageRoomNode(default,StageRoomType.Normal,StageRoomConnectionFlags.None) {raidRole=RaidRoomRole.Extraction};
  Kind(SectorMapIconPolicy.KindForRoom(terminal),SectorMapIconKind.NormalCombat,"Exit lost common combat type");
  terminal.combatPoolId="theme";
  Kind(SectorMapIconPolicy.KindForRoom(terminal),SectorMapIconKind.ThemedCombat,"Exit lost themed combat type");
  return $"Shared map sprite policy: {checks} actual-source assertions passed. Unity import, serialization, inspector and rendered UI NOT tested.";
 }
}
}
'@
$taskCode = $taskShim + "`n" + $taskRoles + $taskRoomTypes + $taskFlags + $taskKey + $taskTrait + ($taskEnums -join "`n") + $taskShared + $taskNode + $taskSnapshot + $taskMapSettings + $taskPolicy + $taskRunner
Add-Type -TypeDefinition $taskCode -Language CSharp -IgnoreWarnings -WarningAction SilentlyContinue
[MapIconProbe.Probe]::Run()
$taskSharedGuid = [regex]::Match((Read-MapSource 'Assets/ScriptableObject/GamePlay/UI/Map/SectorMapIcons.asset.meta'), '(?m)^guid: ([0-9a-f]{32})').Groups[1].Value
if (-not $taskSharedGuid) { throw 'Shared sprite SO GUID missing' }
foreach ($profile in @('MiniMapUISettings.asset','FullMapUISettings.asset')) {
    $source = Read-MapSource "Assets/ScriptableObject/GamePlay/UI/Map/$profile"
    if ($source -notmatch ('_sectorIcons: \{fileID: 11400000, guid: ' + $taskSharedGuid)) { throw "Shared sprite SO not connected: $profile" }
}
$taskRows = [regex]::Matches((Read-MapSource 'Assets/Scripts/Authoring/Editor/StageMakerWindow.MapImages.cs'), '\(SectorMapIconKind\.(\w+), "[^"]+", "(_\w+)"\)')
if ($taskRows.Count -ne 15) { throw "Expected 15 global type rows, found $($taskRows.Count)" }
foreach ($row in $taskRows) {
    if (-not [MapIconProbe.SectorMapIconSetSO].GetField($row.Groups[2].Value, [Reflection.BindingFlags]'Instance,NonPublic')) { throw "Invalid global sprite field: $($row.Value)" }
}
'Shared SO links + all 15 editor category fields verified statically.'
$taskCellPrefab = Read-MapSource 'Assets/Prefabs/UI/GamePlay/MAP/MiniMapCell.prefab'
$taskCellIds = [regex]::Matches($taskCellPrefab, '(?m)^--- !u!\d+ &(-?\d+)') | ForEach-Object { $_.Groups[1].Value }
if (@($taskCellIds | Group-Object | Where-Object Count -gt 1).Count) { throw 'Duplicate mini-map prefab file IDs' }
foreach ($field in @('_pairedRoomIcon','_extractionIcon')) {
    $taskCellId = [regex]::Match($taskCellPrefab, ($field + ': \{fileID: (\d+)\}')).Groups[1].Value
    if (-not $taskCellId -or $taskCellId -notin $taskCellIds) { throw "Missing authored mini-map Image reference: $field" }
}
'Authored mini-map pair Image references + unique prefab file IDs verified statically.'
