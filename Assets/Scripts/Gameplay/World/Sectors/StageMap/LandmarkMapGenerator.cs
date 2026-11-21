using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One saved cell per grid coordinate; cells in a landmark share authored physical space and encounter clear.</summary>
public static class LandmarkMapGenerator
{
    public static readonly Vector2Int[] Steps={Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
    public static readonly StageRoomConnectionFlags[] Flags={StageRoomConnectionFlags.Left,StageRoomConnectionFlags.Right,StageRoomConnectionFlags.Up,StageRoomConnectionFlags.Down};
    public static bool SameLandmark(StageRoomNode a, StageRoomNode b) =>
        a!=null&&b!=null&&!string.IsNullOrEmpty(a.landmarkId)&&a.landmarkId==b.landmarkId&&a.landmarkAnchor==b.landmarkAnchor;
    public static bool TryGenerate(StageDefinitionSO stage,int seed,out StageMapLayout layout,out string error)
    {
        layout=null;error=null;
        if(stage==null||stage.Landmarks?.guaranteed==null||stage.RaidMap==null)
        {error="스테이지/랜드마크/탈출구 설정이 없습니다.";return false;}
        int size=stage.RoomGridSize;
        if(size<1||size>64){error="랜드마크 맵 크기는 1~64입니다.";return false;}
        int stageSeed=StageMapGenerator.BuildStageSeed(seed,stage.StageIndex);
        var random=new System.Random(stageSeed);
        var start=new Vector2Int(size/2,size/2);
        var map=new StageMapLayout{runSeed=seed,stageSeed=stageSeed,stageIndex=stage.StageIndex,gridSize=size,
            usesRaidLayout=true,generationVersion=3,landmarkSchemaVersion=1,
            bossAvoidsExtractionRoutes=stage.RaidMap.preventBossBlockingExtraction,
            startSectorCoord=start,firstRoomCoord=start,hatchRoomCoord=start,hasExtraction=true};
        var nodes=new Dictionary<Vector2Int,StageRoomNode>();
        StageRoomNode Add(Vector2Int c)
        {
            if(!nodes.TryGetValue(c,out var node))
            {
                node=new StageRoomNode(c,StageRoomType.Normal,0){raidRole=RaidRoomRole.Normal};
                nodes.Add(c,node);map.rooms.Add(node);
            }
            return node;
        }
        bool Inside(Vector2Int c)=>c.x>=0&&c.y>=0&&c.x<size&&c.y<size;
        Add(start).raidRole=RaidRoomRole.Hatch;
        Add(start).isCleared=Add(start).isOpened=Add(start).isRevealed=true;
        var ids=new HashSet<string>();var anchors=new List<StageRoomNode>();int powerCount=0;
        foreach(var definition in stage.Landmarks.guaranteed)
        {
            if(definition==null||string.IsNullOrWhiteSpace(definition.id)||!ids.Add(definition.id)||
                definition.variants==null||definition.variants.Count==0)
            {error="랜드마크 정의/고유 ID/프리셋 누락 또는 중복";return false;}
            var variantIds=new HashSet<string>();
            foreach(var candidate in definition.variants)
                if(candidate==null||!candidate.TryValidate(out error)||!variantIds.Add(candidate.variantId))
                {error=error??"랜드마크 프리셋 variant ID가 중복됩니다.";return false;}
            var variant=definition.variants[random.Next(definition.variants.Count)];
            if(variant==null||!variant.TryValidate(out error))return false;
            var positions=new List<Vector2Int>();
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var pos=new Vector2Int(x,y);bool fits=true;
                foreach(var cell in variant.cells)if(!Inside(pos+cell.offset)||nodes.ContainsKey(pos+cell.offset)){fits=false;break;}
                if(fits)positions.Add(pos);
            }
            if(positions.Count==0){error=$"{definition.displayName}: {size}×{size} 안에 {variant.cells.Count}칸을 보장 배치할 공간이 없습니다.";return false;}
            var anchor=positions[random.Next(positions.Count)];
            foreach(var cell in variant.cells)
            {
                var node=Add(anchor+cell.offset);
                node.landmarkId=definition.id;node.landmarkAnchor=anchor;node.landmarkVariantId=variant.variantId;
                node.landmarkOffset=cell.offset;node.isRevealed=true;node.landmarkIcon=definition.mapIconKind;
            }
            anchors.Add(Add(anchor));
            if(definition.powerStation){powerCount++;map.generatorRoomCoord=anchor;Add(anchor).raidRole=RaidRoomRole.Generator;}
        }
        if(powerCount!=1){error="발전소 랜드마크를 정확히 1개 지정하세요.";return false;}
        // Connect landmark anchors to the start with seeded orthogonal paths.
        foreach(var anchor in anchors)
        {
            var c=start;bool xFirst=random.Next(2)==0;
            while(c!=anchor.coord)
            {
                var target=anchor.coord;
                if((xFirst&&c.x!=target.x)||c.y==target.y)c+=new Vector2Int(Math.Sign(target.x-c.x),0);
                else c+=new Vector2Int(0,Math.Sign(target.y-c.y));
                Add(c);
            }
        }
        int targetCount=Mathf.Min(size*size,Mathf.Max(nodes.Count+stage.RaidMap.extractionCount,
            Mathf.RoundToInt(size*size*.32f)));
        while(nodes.Count<targetCount)
        {
            var candidates=new List<Vector2Int>();
            foreach(var node in map.rooms)foreach(var d in Steps)
                if(Inside(node.coord+d)&&!nodes.ContainsKey(node.coord+d)&&!candidates.Contains(node.coord+d))candidates.Add(node.coord+d);
            if(candidates.Count==0)break;
            Add(candidates[random.Next(candidates.Count)]);
        }
        foreach(var node in map.rooms)
            for(int i=0;i<4;i++)if(nodes.ContainsKey(node.coord+Steps[i]))node.connections|=Flags[i];
        var bosses=anchors.FindAll(x=>stage.Landmarks.Find(x.landmarkId).bossEligible);
        if(bosses.Count==0){error="보스를 배정할 랜드마크가 없습니다.";return false;}
        map.bossSpawned=random.Next(100)<stage.RaidMap.bossSpawnPercent;
        var boss=bosses[random.Next(bosses.Count)];
        map.bossRoomCoord=map.goalRoomCoord=boss.coord;
        if(map.bossSpawned)boss.roomType=StageRoomType.Boss;
        var safeRooms=map.bossSpawned&&map.bossAvoidsExtractionRoutes?ReachableWithoutLandmark(map,boss):null;
        var exits=new List<StageRoomNode>();
        foreach(var node in map.rooms)
            if(node.coord!=start&&string.IsNullOrEmpty(node.landmarkId)&&
                (safeRooms==null||safeRooms.Contains(node.coord))&&
                (node.connections!= (StageRoomConnectionFlags)15))exits.Add(node);
        exits.Sort((a,b)=>(Mathf.Abs(b.coord.x-start.x)+Mathf.Abs(b.coord.y-start.y))
            .CompareTo(Mathf.Abs(a.coord.x-start.x)+Mathf.Abs(a.coord.y-start.y)));
        int count=Mathf.Max(1,stage.RaidMap.extractionCount);
        foreach(var exit in exits)
        {
            var original=exit.connections;
            for(int keep=0;keep<4;keep++)
            {
                if(!exit.HasConnection(Flags[keep]))continue;
                for(int d=0;d<4;d++)if(d!=keep&&(original&Flags[d])!=0)
                {exit.connections&=~Flags[d];nodes[exit.coord+Steps[d]].connections&=~Flags[d^1];}
                bool safe=true;
                if(safeRooms!=null)
                {
                    var reached=ReachableWithoutLandmark(map,boss);
                    safe=reached.Contains(exit.coord);
                    foreach(var savedExit in map.extractionRooms)safe&=reached.Contains(savedExit);
                }
                if(safe&&RaidMapGenerator.GetDistances(map,start).Count==nodes.Count)break;
                exit.connections=original;
                for(int d=0;d<4;d++)if((original&Flags[d])!=0)nodes[exit.coord+Steps[d]].connections|=Flags[d^1];
            }
            int connections=0;foreach(var flag in Flags)if(exit.HasConnection(flag))connections++;
            if(connections!=1)continue;
            map.extractionRooms.Add(exit.coord);map.extractionCandidates.Add(exit.coord);
            if(map.extractionRooms.Count==count)break;
        }
        if(safeRooms!=null)safeRooms=ReachableWithoutLandmark(map,boss);
        // Dense small graphs may not have enough prunable leaves. Add explicit short
        // ordinary branches instead of failing an otherwise feasible guaranteed layout.
        foreach(var parent in new List<StageRoomNode>(map.rooms))
        {
            if(map.extractionRooms.Count>=count)break;
            if(map.extractionRooms.Contains(parent.coord)||safeRooms!=null&&!safeRooms.Contains(parent.coord))continue;
            for(int i=0;i<4&&map.extractionRooms.Count<count;i++)
            {
                var coord=parent.coord+Steps[i];
                if(!Inside(coord)||nodes.ContainsKey(coord))continue;
                var leaf=Add(coord);leaf.connections=Flags[i^1];parent.connections|=Flags[i];
                map.extractionRooms.Add(coord);map.extractionCandidates.Add(coord);
            }
        }
        if(map.extractionRooms.Count<count){error="보장 랜드마크·보스 우회 경로를 유지하면서 필요한 탈출구 가지 끝을 만들 공간이 없습니다.";return false;}
        map.extractionCandidates.Add(map.generatorRoomCoord);map.extractionSectorCoord=map.extractionRooms[0];
        StageSystemPolicies.Apply(stage,map);
        layout=map;
        return RaidExtractionPortalPolicy.TryPrepare(layout,out error)&&TryValidate(layout,out error);
    }

    public static bool TryValidate(StageMapLayout map,out string error)
    {
        error="랜드마크 저장 맵의 연결/점유 칸이 잘못되었습니다.";
        if(map==null||map.landmarkSchemaVersion!=1||map.rooms==null||map.gridSize<1)return false;
        var nodes=new Dictionary<Vector2Int,StageRoomNode>();
        foreach(var room in map.rooms)
            if(room==null||room.coord.x<0||room.coord.y<0||room.coord.x>=map.gridSize||room.coord.y>=map.gridSize||
                !nodes.TryAdd(room.coord,room))return false;
        if(!nodes.ContainsKey(map.startSectorCoord))return false;
        foreach(var room in map.rooms)
        {
            if(!string.IsNullOrEmpty(room.landmarkId)&&
                (!nodes.TryGetValue(room.landmarkAnchor,out var anchor)||anchor.landmarkId!=room.landmarkId||
                 room.coord!=room.landmarkAnchor+room.landmarkOffset||anchor.landmarkVariantId!=room.landmarkVariantId))return false;
            for(int i=0;i<4;i++)if(room.HasConnection(Flags[i])&&
                (!nodes.TryGetValue(room.coord+Steps[i],out var adjacent)||!adjacent.HasConnection(Flags[i^1])))return false;
        }
        if(RaidMapGenerator.GetDistances(map,map.startSectorCoord).Count!=nodes.Count)return false;
        if(!nodes.ContainsKey(map.generatorRoomCoord)||!nodes.TryGetValue(map.bossRoomCoord,out var boss)||
            string.IsNullOrEmpty(boss.landmarkId))return false;
        if(map.bossSpawned&&map.bossAvoidsExtractionRoutes)
        {
            var safe=ReachableWithoutLandmark(map,boss);
            foreach(var exit in map.extractionRooms)if(!safe.Contains(exit))
            {error="보스 랜드마크를 통과하지 않으면 외곽 탈출구에 도달할 수 없습니다.";return false;}
        }
        error=null;return RaidExtractionPortalPolicy.TryValidate(map,out error);
    }
    public static HashSet<Vector2Int> ReachableWithoutLandmark(StageMapLayout map,StageRoomNode blocked)
    {
        var reached=new HashSet<Vector2Int>{map.startSectorCoord};
        var queue=new Queue<Vector2Int>();queue.Enqueue(map.startSectorCoord);
        while(queue.Count>0)
        {
            var coord=queue.Dequeue();if(!map.TryGetRoom(coord,out var room))continue;
            for(int i=0;i<4;i++)
            {
                var next=coord+Steps[i];
                if(!room.HasConnection(Flags[i])||!map.TryGetRoom(next,out var adjacent)||
                    !adjacent.HasConnection(Flags[i^1])||SameLandmark(adjacent,blocked)||!reached.Add(next))continue;
                queue.Enqueue(next);
            }
        }
        return reached;
    }
    public static bool TryResolve(StageDefinitionSO stage,StageRoomNode room,out LandmarkPrefab variant,out LandmarkPrefab.Cell cell)
    {
        variant=null;cell=null;
        var definition=stage.Landmarks.Find(room.landmarkId);
        variant=definition?.variants.Find(x=>x!=null&&x.variantId==room.landmarkVariantId);
        if(variant==null)return false;
        cell=variant.cells.Find(x=>x.offset==room.landmarkOffset);
        return cell?.preset!=null&&cell.sector!=null;
    }
}
