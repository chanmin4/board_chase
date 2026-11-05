using System.Collections.Generic;
using UnityEngine;

/// <summary>New layouts only. These choices are snapshots, never rerolled on resume or room re-entry.</summary>
public static class StageSystemPolicies
{
    public static void Apply(StageDefinitionSO stage,StageMapLayout layout)
    {
        var rules=stage?.EncounterRules;
        if(stage==null||layout==null||rules==null||layout.gameSystemPolicyVersion>0)return;
        layout.gameSystemPolicyVersion=1;
        var random=new System.Random(StageDefinitionSO.BuildSectorSeed(layout.stageSeed,Vector2Int.zero,26026));
        var candidates=new List<StageRoomNode>();
        foreach(var room in layout.rooms)
            if(room.coord!=layout.startSectorCoord&&room.raidRole!=RaidRoomRole.Hatch&&room.raidRole!=RaidRoomRole.Generator)
                candidates.Add(room);
        for(int i=candidates.Count-1;i>0;i--){int j=random.Next(i+1);(candidates[i],candidates[j])=(candidates[j],candidates[i]);}
        int count=Mathf.Max(0,rules.infectedRoomCount);
        for(int i=0;i<candidates.Count;i++)
        {
            candidates[i].virusRoom=i<count;
            if(candidates[i].roomType==StageRoomType.Normal&&!candidates[i].hasInternalCombatLock)
                candidates[i].extraEnemyCount=random.NextDouble()<rules.extraEnemyChance?1:0;
        }
        if(stage.StageIndex!=1||!layout.bossSpawned||!layout.TryGetRoom(layout.bossRoomCoord,out var previous))return;
        var bossCandidates=new List<StageRoomNode>();
        foreach(var room in candidates)
        {
            bool allowed=room==previous||room.combatPoolId=="theme"||room.specialSectorPoolId is "office" or "storage" or "warehouse";
            if(!allowed||room.hasInternalCombatLock||layout.IsExtractionCandidate(room.coord)||room.hasMerchant||
                (room!=previous&&room.raidRole!=RaidRoomRole.Normal))continue;
            if(layout.bossAvoidsExtractionRoutes&&!ExitsReachableWithout(layout,room.coord))continue;
            bossCandidates.Add(room);
        }
        if(bossCandidates.Count==0)return;
        var chosen=bossCandidates[random.Next(bossCandidates.Count)];
        previous.roomType=StageRoomType.Normal;previous.raidRole=RaidRoomRole.Normal;
        chosen.roomType=StageRoomType.Boss;chosen.raidRole=RaidRoomRole.Boss;chosen.extraEnemyCount=0;
        layout.bossRoomCoord=layout.goalRoomCoord=chosen.coord;
        layout.bossMayUseSpecialRoom=!string.IsNullOrEmpty(chosen.specialSectorPoolId);
    }
    public static bool ExitsReachableWithout(StageMapLayout layout,Vector2Int blocked)
    {
        if(layout==null||blocked==layout.startSectorCoord)return false;
        var reached=new HashSet<Vector2Int>{layout.startSectorCoord};var queue=new Queue<Vector2Int>();queue.Enqueue(layout.startSectorCoord);
        var steps=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
        while(queue.Count>0)
        {
            var coord=queue.Dequeue();if(!layout.TryGetRoom(coord,out var room))continue;
            foreach(var delta in steps)
            {
                var next=coord+delta;
                var flag=delta==Vector2Int.left?StageRoomConnectionFlags.Left:delta==Vector2Int.right?StageRoomConnectionFlags.Right:
                    delta==Vector2Int.up?StageRoomConnectionFlags.Up:StageRoomConnectionFlags.Down;
                var opposite=delta==Vector2Int.left?StageRoomConnectionFlags.Right:delta==Vector2Int.right?StageRoomConnectionFlags.Left:
                    delta==Vector2Int.up?StageRoomConnectionFlags.Down:StageRoomConnectionFlags.Up;
                if(next==blocked||!layout.TryGetRoom(next,out var other)||!room.HasConnection(flag)||!other.HasConnection(opposite)||!reached.Add(next))continue;
                queue.Enqueue(other.coord);
            }
        }
        foreach(var exit in layout.extractionRooms)if(!reached.Contains(exit))return false;
        return true;
    }
}
