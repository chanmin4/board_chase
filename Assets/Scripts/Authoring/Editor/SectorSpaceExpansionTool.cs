using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit author-time migration. Never scales the player, equipment, props or runtime sector root.</summary>
public static class SectorSpaceExpansionTool
{
    [MenuItem("Tools/Game Tool/Sector Maker/선택 섹터 공간 1.2배 저장")]
    public static void ExpandSelected()
    {
        string path=AssetDatabase.GetAssetPath(Selection.activeObject);
        if(string.IsNullOrEmpty(path)||!path.StartsWith("Assets/")||!path.EndsWith(".prefab"))
        {EditorUtility.DisplayDialog("섹터 공간","프로젝트 창에서 수정할 섹터 프리팹을 선택하세요. 프리팹 전체 스케일은 변경하지 않습니다.","확인");return;}
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var sector in root.GetComponentsInChildren<SectorRuntime>(true))Expand(sector);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        Debug.Log("선택 섹터의 바닥·외곽 경계·포탈만 1.2배 공간으로 저장했습니다. 내부 소품/스폰·NavMesh와 StageSectorInstantiator의 Room Spacing XZ(예: 40→48)를 함께 확인하세요.");
    }
    private static void Expand(SectorRuntime sector)
    {
        float factor=1.2f/Mathf.Max(.01f,sector.AuthoredSpaceMultiplier);
        if(Mathf.Approximately(factor,1f))return;
        var pivot=sector.transform.position;
        var positions=new Dictionary<Transform,Vector3>();
        var floors=new HashSet<Transform>();
        void Move(Transform t){if(t!=null&&!positions.ContainsKey(t))positions.Add(t,t.position);}
        foreach(var collider in sector.GetComponentsInChildren<Collider>(true))
        {
            if(collider.gameObject.layer!=LayerMask.NameToLayer("SectorFloor"))continue;
            var t=collider.transform;bool nested=false;
            for(var parent=t.parent;parent!=null&&parent!=sector.transform;parent=parent.parent)
                if(parent.gameObject.layer==LayerMask.NameToLayer("SectorFloor")){nested=true;break;}
            if(!nested){floors.Add(t);Move(t);}
        }
        foreach(var edge in new[]{sector.XMin,sector.XMax,sector.ZMin,sector.ZMax})
        {
            Move(edge?.bound?.transform);Move(edge?.portal?.transform);
        }
        foreach(var floor in floors)
        {
            var scale=floor.localScale;scale.x*=factor;scale.z*=factor;floor.localScale=scale;
        }
        var ordered=new List<Transform>(positions.Keys);
        int Depth(Transform t){int count=0;for(;t!=null;t=t.parent)count++;return count;}
        ordered.Sort((a,b)=>Depth(a).CompareTo(Depth(b)));
        foreach(var target in ordered)
        {
            var delta=positions[target]-pivot;delta.x*=factor;delta.z*=factor;
            target.position=pivot+delta;
        }
        foreach(var edge in new[]{sector.XMin,sector.XMax})
            if(edge?.bound!=null){var scale=edge.bound.transform.localScale;scale.z*=factor;edge.bound.transform.localScale=scale;}
        foreach(var edge in new[]{sector.ZMin,sector.ZMax})
            if(edge?.bound!=null){var scale=edge.bound.transform.localScale;scale.x*=factor;edge.bound.transform.localScale=scale;}
        sector.AuthoredSpaceMultiplier=1.2f;
        EditorUtility.SetDirty(sector);
    }
}
