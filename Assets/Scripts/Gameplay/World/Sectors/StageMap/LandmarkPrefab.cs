using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored compound prefab. Interior geometry/portals must be baked in the editor, never generated in play.</summary>
public sealed class LandmarkPrefab : MonoBehaviour
{
    [Serializable]
    public sealed class Cell
    {
        public Vector2Int offset;
        public SectorRuntime sector;
        public SectorPresetSO preset;
    }
    public string variantId = "layout_1";
    public List<Cell> cells = new();
    private readonly HashSet<SectorRuntime> _ready = new();
    private bool _completing;
    public string RuntimeLandmarkId {get;set;}
    public static bool SameGroup(SectorRuntime a, SectorRuntime b)
    {
        if(a==b)return true;
        if(a==null||b==null)return false;
        var group=a.GetComponentInParent<LandmarkPrefab>();
        return group!=null&&group==b.GetComponentInParent<LandmarkPrefab>();
    }
    public bool TryComplete(SectorRuntime source, SectorStateManager manager)
    {
        if(_completing)return false;
        _ready.Add(source);
        foreach(var cell in cells)
        {
            if(cell.sector==null)return false;
            var spawner=cell.sector.GetComponentInChildren<SectorEnemySpawner>(true);
            if(!_ready.Contains(cell.sector)&&!cell.sector.IsCleared&&spawner!=null&&spawner.HasConfiguredEnemyEncounter&&
                !spawner.HasCompletedEncounter)return false;
        }
        _completing=true;
        try
        {
            bool changed=false;
            foreach(var cell in cells)changed|=manager.CompleteSector(cell.sector);
            if(changed)
            {
                QuestRuntime.ReportAction("",cells.Count,QuestObjectiveType.SectorClear);
                QuestRuntime.ReportAction(RuntimeLandmarkId,1,QuestObjectiveType.LandmarkClear);
            }
            return changed;
        }
        finally {_completing=false;}
    }
    public bool Completing => _completing;
    public bool TryValidate(out string error)
    {
        error=null;
        if(string.IsNullOrWhiteSpace(variantId)||cells==null||cells.Count<1||cells.Count>4)
        {error="랜드마크는 고정 variant ID와 1~4칸이 필요합니다.";return false;}
        var coords=new HashSet<Vector2Int>();var sectors=new HashSet<SectorRuntime>();
        foreach(var cell in cells)
            if(cell==null||cell.sector==null||cell.preset==null||!cell.sector.transform.IsChildOf(transform)||
                !coords.Add(cell.offset)||!sectors.Add(cell.sector))
            {error="랜드마크 칸의 좌표/자식 섹터/프리셋이 누락되거나 중복됩니다.";return false;}
        if(!coords.Contains(Vector2Int.zero)){error="기준 칸 (0,0)이 필요합니다.";return false;}
        var reached=new HashSet<Vector2Int>{Vector2Int.zero};var queue=new Queue<Vector2Int>();queue.Enqueue(Vector2Int.zero);
        while(queue.Count>0)
        {
            var c=queue.Dequeue();
            foreach(var delta in LandmarkMapGenerator.Steps)
                if(coords.Contains(c+delta)&&reached.Add(c+delta))queue.Enqueue(c+delta);
        }
        if(reached.Count!=coords.Count){error="랜드마크 칸이 상하좌우로 연결되어야 합니다.";return false;}
        return true;
    }
}
