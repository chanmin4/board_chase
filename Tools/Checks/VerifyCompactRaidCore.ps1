param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
$ErrorActionPreference = 'Stop'
# Standalone managed probe, not a Unity compile/launch/test. Exercises the actual generator sources
# with minimal Vector2Int/Mathf/attribute shims; Unity import, serialization and Play Mode remain separate.
function Read-Source([string]$relative) { Get-Content -LiteralPath (Join-Path $ProjectRoot $relative) -Raw }
function Get-MemberText([string]$source, [string]$signature) {
    $start = $source.IndexOf($signature, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing source member: $signature" }
    $brace = $source.IndexOf('{', $start); $depth = 0
    for ($i = $brace; $i -lt $source.Length; $i++) {
        if ($source[$i] -eq '{') { $depth++ }
        if ($source[$i] -eq '}') { $depth--; if ($depth -eq 0) { return $source.Substring($start, $i - $start + 1) } }
    }
    throw "Unclosed source member: $signature"
}
$taskShim = @'
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {
 public sealed class HideInInspector : Attribute {}
 public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string text) {} }
 public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string text) {} }
 public sealed class RangeAttribute : Attribute { public RangeAttribute(float a, float b) {} }
 public sealed class MinAttribute : Attribute { public MinAttribute(float value) {} }
 public struct Vector2Int : IEquatable<Vector2Int> {
  public int x, y;
  public Vector2Int(int x, int y) { this.x=x; this.y=y; }
  public static Vector2Int zero => new(0,0);
  public static Vector2Int left => new(-1,0); public static Vector2Int right => new(1,0);
  public static Vector2Int up => new(0,1); public static Vector2Int down => new(0,-1);
  public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new(a.x+b.x,a.y+b.y);
  public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new(a.x-b.x,a.y-b.y);
  public static bool operator ==(Vector2Int a, Vector2Int b) => a.Equals(b);
  public static bool operator !=(Vector2Int a, Vector2Int b) => !a.Equals(b);
  public bool Equals(Vector2Int other) => x==other.x && y==other.y;
  public override bool Equals(object other) => other is Vector2Int v && Equals(v);
  public override int GetHashCode() => HashCode.Combine(x,y);
 }
 public static class Mathf {
  public static int Abs(int v)=>Math.Abs(v); public static int Min(int a,int b)=>Math.Min(a,b);
  public static int Max(int a,int b)=>Math.Max(a,b); public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));
  public static int RoundToInt(float v)=>(int)Math.Round(v,MidpointRounding.ToEven);
  public static int FloorToInt(float v)=>(int)Math.Floor(v);
 }
}
namespace BoardChaseCompactProbe {
'@
$taskParts = @('RaidMapSettings.cs','StageMapLayout.cs','StageRoomNode.cs','StageRoomType.cs',
    'RaidExtractionPortalPolicy.cs','RadialBraidedRaidGenerator.cs','RadialBraidedRaidGenerator.Compact.cs',
    'RadialBraidedRaidGenerator.Landmarks.cs','RaidMerchantPlacement.cs') |
    ForEach-Object { ((Read-Source "Assets/Scripts/Gameplay/World/Sectors/StageMap/$_") -replace '(?m)^using [^\r\n]+;\r?\n','') -replace '\[Range\(', '[UnityEngine.RangeAttribute(' }
$taskPortal = Get-MemberText (Read-Source 'Assets/Scripts/Gameplay/World/Sectors/Portal/PortalDirectionUtility.cs') 'public enum SectorPortalDirection'
$taskKey = Get-MemberText (Read-Source 'Assets/Scripts/Gameplay/Items/Definitions/KeyItemSO.cs') 'public enum LootKeyTier'
$taskTrait = Get-MemberText (Read-Source 'Assets/Scripts/Gameplay/World/Sectors/Traits/SectorRoomTraitTableSO.cs') 'public enum SectorRoomTraitType'
$taskStageSeed = Get-MemberText (Read-Source 'Assets/Scripts/Gameplay/World/Sectors/StageMap/StageMapGenerator.cs') 'public static int BuildStageSeed'
$taskSectorSeed = Get-MemberText (Read-Source 'Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs') 'public static int BuildSectorSeed'
$taskDistances = Get-MemberText (Read-Source 'Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapGenerator.cs') 'public static Dictionary<Vector2Int, int> GetDistances'
$taskSupport = "public static class StageMapGenerator { $taskStageSeed } public static class StageDefinitionSO { $taskSectorSeed }"
$taskSupport += "public static class RaidMapGenerator { private static readonly Vector2Int[] Offsets={Vector2Int.left,Vector2Int.right,Vector2Int.down,Vector2Int.up}; private static readonly StageRoomConnectionFlags[] Flags={StageRoomConnectionFlags.Left,StageRoomConnectionFlags.Right,StageRoomConnectionFlags.Down,StageRoomConnectionFlags.Up}; $taskDistances }"
$taskRunner = @'
public static class Probe {
 static StageMapLayout MerchantMap(int seed,int commonCount=2) {
  var map=new StageMapLayout {generationVersion=3,landmarkPlacementVersion=1,usesRaidLayout=true,stageSeed=StageMapGenerator.BuildStageSeed(seed,1),startSectorCoord=Vector2Int.zero};
  for(int i=0;i<=commonCount+6;i++) map.rooms.Add(new StageRoomNode(new Vector2Int(i,0),StageRoomType.Normal,StageRoomConnectionFlags.None) {raidRole=RaidRoomRole.Normal,combatPoolId="common"});
  map.rooms[commonCount+1].combatPoolId="theme";
  map.rooms[commonCount+2].specialSectorPoolId="office";
  map.rooms[commonCount+3].roomType=StageRoomType.Boss;
  map.rooms[commonCount+4].raidRole=RaidRoomRole.Generator;
  map.extractionRooms.Add(map.rooms[commonCount+5].coord); map.extractionCandidates.Add(map.rooms[commonCount+5].coord);
  map.rooms[commonCount+6].hasInternalCombatLock=true;
  return map;
 }
 static void CheckLandmarks(StageMapLayout map) {
  if(map.landmarkPlacementVersion!=1) throw new Exception("Missing placement version");
  if(map.gridSize==1) return;
  if(map.extractionCandidates.Contains(map.bossRoomCoord) || map.extractionCandidates.Count!=map.extractionRooms.Count+1) throw new Exception("Boss reserved as exit candidate");
  foreach(var coord in map.extractionCandidates) {
   map.TryGetRoom(coord,out var room); int degree=0;
   foreach(var flag in new[]{StageRoomConnectionFlags.Left,StageRoomConnectionFlags.Right,StageRoomConnectionFlags.Up,StageRoomConnectionFlags.Down}) if(room.HasConnection(flag)) degree++;
   if(degree!=1) throw new Exception("Power/exit is not a leaf");
  }
  map.TryGetRoom(map.bossRoomCoord,out var boss);
  if(map.bossSpawned && boss.combatPoolId!="theme") throw new Exception("Boss not in theme pool");
  if(map.bossSpawned && map.bossAvoidsExtractionRoutes && !CheckExitBypass(map)) throw new Exception("Boss blocks an exit");
 }
 // Independent check: disconnect boss edges, then use the project's ordinary BFS.
 static bool CheckExitBypass(StageMapLayout map) {
  map.TryGetRoom(map.bossRoomCoord,out var boss); var connections=boss.connections;
  try {
   boss.connections=StageRoomConnectionFlags.None;
   var reached=RaidMapGenerator.GetDistances(map,map.startSectorCoord);
   foreach(var exit in map.extractionRooms) if(!reached.ContainsKey(exit)) return false;
   return true;
  } finally {boss.connections=connections;}
 }
 public static string Run() {
  int small=0, normal=0, extended=0;
  for(int grid=1;grid<=7;grid++) foreach(float density in new[]{.15f,.32f,.65f}) for(int seed=0;seed<64;seed++) {
   var settings=new RaidMapSettings { roomDensity=density,candidateCount=8,extractionCount=6,minimumExtractionPathDistance=100,preventBossBlockingExtraction=false };
   if(!RadialBraidedRaidGenerator.TryGenerate(seed,1,grid,settings,out var map,out string error,true)) throw new Exception($"grid={grid},seed={seed}: {error}");
   if(!map.compactTestLayout || !RadialBraidedRaidGenerator.TryValidate(map,out error)) throw new Exception("Bad compact graph: "+error);
   CheckLandmarks(map);
   if(RaidMapGenerator.GetDistances(map,map.startSectorCoord).Count!=map.rooms.Count) throw new Exception("Disconnected graph");
   if(settings.candidateCount!=8 || settings.extractionCount!=6 || settings.minimumExtractionPathDistance!=100 || settings.roomDensity!=density) throw new Exception("Mutated settings");
   foreach(var room in map.rooms) if(room.coord.x<0 || room.coord.y<0 || room.coord.x>=grid || room.coord.y>=grid) throw new Exception("Out of bounds");
   if(grid==1 && (map.rooms.Count!=1 || map.rooms[0].raidRole!=RaidRoomRole.Normal || map.extractionPortals.Count!=1 || !map.rooms[0].isCleared)) throw new Exception("Invalid singleton HOME exit");
   small++;
  }
  foreach(int grid in new[]{8,10,12,14,16}) for(int seed=0;seed<32;seed++) {
   var settings=new RaidMapSettings {vaultNearCandidateChance=0};
   if(!RadialBraidedRaidGenerator.TryGenerate(seed,1,grid,settings,out var map,out string error,true) || map.compactTestLayout || !RadialBraidedRaidGenerator.TryValidate(map,out error)) throw new Exception($"Normal regression grid={grid},seed={seed}: {error}");
   CheckLandmarks(map);
   map.compactTestLayout=true;
   if(RadialBraidedRaidGenerator.TryValidate(map,out _)) throw new Exception("Compact flag bypassed normal validation");
   normal++;
  }
  foreach(int grid in new[]{8,10,16,32}) foreach(float density in new[]{.15f,.65f}) foreach(int braid in new[]{0,25,100}) foreach(int exits in new[]{1,2,6}) for(int seed=0;seed<(grid>=32?1:8);seed++) {
   var settings=new RaidMapSettings {roomDensity=density,braidPercent=braid,extractionCount=exits,bossSpawnPercent=100,vaultNearCandidateChance=1};
   if(!RadialBraidedRaidGenerator.TryGenerate(seed,3,grid,settings,out var map,out string error,true)) throw new Exception($"Extended grid={grid},density={density},braid={braid},exits={exits},seed={seed}: {error}");
   CheckLandmarks(map);
   if(map.extractionRooms.Count!=exits) throw new Exception("Normal exit count silently reduced");
   settings.candidateCount=-999; settings.minimumExtractionPathDistance=int.MaxValue;
   settings.customizeTopology=true; settings.topology.candidateSeparationWeight=settings.topology.candidateDistanceWeight=-1;
   settings.topology.vaultCandidateDistance=settings.topology.merchantMinimumDistance=int.MaxValue;
   if(!RadialBraidedRaidGenerator.TryGenerate(seed,3,grid,settings,out var other,out error,true)) throw new Exception("Retired settings blocked generation: "+error);
   if(map.rooms.Count!=other.rooms.Count || map.generatorRoomCoord!=other.generatorRoomCoord || map.bossRoomCoord!=other.bossRoomCoord) throw new Exception("Retired settings changed layout");
   for(int i=0;i<map.rooms.Count;i++) if(map.rooms[i].coord!=other.rooms[i].coord || map.rooms[i].connections!=other.rooms[i].connections || map.rooms[i].raidRole!=other.rooms[i].raidRole) throw new Exception("Retired settings changed graph");
   extended++;
  }
  foreach(float density in new[]{.15f,.65f}) foreach(int braid in new[]{0,100}) {
   var settings=new RaidMapSettings {roomDensity=density,braidPercent=braid,extractionCount=6,bossSpawnPercent=100,vaultNearCandidateChance=0};
   if(!RadialBraidedRaidGenerator.TryGenerate(0,6,64,settings,out var map,out string error,true)) throw new Exception("64-grid boundary: "+error);
   CheckLandmarks(map); extended++;
  }
  int protectedMaps=0, explicitFailures=0, unsafeOff=0;
  foreach(int grid in new[]{2,3,4,7,8,10,16}) foreach(int exits in new[]{1,2,6}) for(int seed=0;seed<32;seed++) {
   var settings=new RaidMapSettings {bossSpawnPercent=100,extractionCount=exits,vaultNearCandidateChance=0,preventBossBlockingExtraction=false};
   if(!RadialBraidedRaidGenerator.TryGenerate(seed,1,grid,settings,out var off,out string error,true)) throw new Exception("OFF unexpectedly failed: "+error);
   if(off.bossAvoidsExtractionRoutes) throw new Exception("OFF snapshot not saved");
   if(!CheckExitBypass(off)) {
    unsafeOff++; off.bossAvoidsExtractionRoutes=true;
    if(RadialBraidedRaidGenerator.TryValidate(off,out _)) throw new Exception("Validation accepted blocked exit");
    off.bossAvoidsExtractionRoutes=false;
    if(!RadialBraidedRaidGenerator.TryValidate(off,out error)) throw new Exception("Old/unprotected layout rejected: "+error);
   }
   settings.preventBossBlockingExtraction=true;
   if(!RadialBraidedRaidGenerator.TryGenerate(seed,1,grid,settings,out var map,out error,true)) {
    if(grid>=8 || error==null || !error.Contains("보스가 탈출 경로")) throw new Exception($"Unexpected protection failure grid={grid},exits={exits},seed={seed}: "+error);
    explicitFailures++; continue;
   }
   if(!map.bossAvoidsExtractionRoutes || !CheckExitBypass(map)) throw new Exception("ON bypass missing");
   CheckLandmarks(map); protectedMaps++;
  }
  if(unsafeOff==0 || protectedMaps==0 || explicitFailures==0) throw new Exception("Protection fixture coverage missing");
  int merchants=0; var merchantSettings=new RaidMapSettings();
  if(merchantSettings.merchantSpawnPercent!=10) throw new Exception("Merchant default is not 10%");
  for(int seed=0;seed<10000;seed++) {
   var map=MerchantMap(seed);
   if(!RaidMerchantPlacement.TryAssign(map,merchantSettings,out string error)) throw new Exception(error);
   var picked=map.rooms.FindAll(room=>room.hasMerchant);
   if(picked.Count>1 || picked.Count!=(map.merchantRollSucceeded?1:0)) throw new Exception("Not one stage-level merchant roll");
   if(picked.Count==1) {
    merchants++;
    if(picked[0].coord.x<1 || picked[0].coord.x>2 || picked[0].isCleared || picked[0].raidRole!=RaidRoomRole.Normal || picked[0].combatPoolId!="common") throw new Exception("Wrong merchant room or cleared combat");
   }
   var many=MerchantMap(seed,24);
   if(!RaidMerchantPlacement.TryAssign(many,merchantSettings,out error) || many.merchantRollSucceeded!=map.merchantRollSucceeded) throw new Exception("Room count changed merchant chance");
   merchantSettings.merchantSpawnPercent=0;
   if(!RaidMerchantPlacement.TryAssign(map,merchantSettings,out error) || map.rooms.FindAll(room=>room.hasMerchant).Count!=picked.Count) throw new Exception("Rerolled saved decision");
   merchantSettings.merchantSpawnPercent=10;
  }
  if(merchants<850 || merchants>1150) throw new Exception("10% distribution outside tolerance: "+merchants);
  foreach(int percent in new[]{0,100}) for(int seed=0;seed<32;seed++) {
   var settings=new RaidMapSettings {merchantSpawnPercent=percent}; var map=MerchantMap(seed);
   if(!RaidMerchantPlacement.TryAssign(map,settings,out string error) || map.rooms.FindAll(room=>room.hasMerchant).Count!=(percent==100?1:0)) throw new Exception("0/100 merchant boundary: "+error);
   var empty=MerchantMap(seed); foreach(var room in empty.rooms) room.combatPoolId="theme";
   if(!RaidMerchantPlacement.TryAssign(empty,settings,out error) || empty.rooms.Exists(room=>room.hasMerchant)) throw new Exception("Merchant placed outside common rooms");
  }
  for(int seed=0;seed<32;seed++) {
   var settings=new RaidMapSettings {merchantSpawnPercent=100,bossSpawnPercent=0,vaultNearCandidateChance=0};
   if(!RadialBraidedRaidGenerator.TryGenerate(seed,1,8,settings,out var map,out string error,true)) throw new Exception(error);
   if(!RaidMerchantPlacement.TryAssign(map,settings,out error) || !RadialBraidedRaidGenerator.TryValidate(map,out error)) throw new Exception("Merchant invalidated graph: "+error);
   if(map.rooms.FindAll(room=>room.hasMerchant).Count!=1) throw new Exception("Missing graph merchant");
  }
  return $"Standalone core: {small} compact + {normal} normal + {extended} boundary cases; protection: {protectedMaps} safe, {explicitFailures} expected small-map errors, {unsafeOff} blocked OFF fixtures. Merchant: {merchants}/10000 stages at 10%, 24-room chance independence, stored-decision and 0/100/eligibility/32 real-graph checks passed. Unity import/serialization/Play Mode NOT tested.";
 }
}
}
'@
$taskCode = $taskShim + "`n" + ($taskParts -join "`n") + "`n" + $taskPortal + $taskKey + $taskTrait + $taskSupport + $taskRunner
Add-Type -TypeDefinition $taskCode -Language CSharp
[BoardChaseCompactProbe.Probe]::Run()
