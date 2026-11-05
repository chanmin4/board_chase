using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>User-triggered authoring only. Never converts models automatically at runtime.</summary>
internal static class LootObjectConversionUtility
{
    internal static LootObjectSO Source;
    internal static EnemyLootOpenRequestEventChannelSO OpenChannel;
    internal static UIOverlayRequestEventChannelSO OverlayChannel;
    public static void Draw()
    {
        EditorGUILayout.Space();EditorGUILayout.LabelField("선택한 모델 → 루팅 오브젝트",EditorStyles.boldLabel);
        Source=(LootObjectSO)EditorGUILayout.ObjectField("루팅 원본",Source,typeof(LootObjectSO),false);
        OpenChannel=(EnemyLootOpenRequestEventChannelSO)EditorGUILayout.ObjectField("루팅 열기 채널",OpenChannel,typeof(EnemyLootOpenRequestEventChannelSO),false);
        OverlayChannel=(UIOverlayRequestEventChannelSO)EditorGUILayout.ObjectField("공용 UI 채널",OverlayChannel,typeof(UIOverlayRequestEventChannelSO),false);
        using(new EditorGUI.DisabledScope(Source==null||OpenChannel==null||OverlayChannel==null||Selection.activeGameObject==null||EditorApplication.isPlayingOrWillChangePlaymode))
            if(GUILayout.Button("선택 오브젝트 루팅화 · 중앙 앵커 추가"))ConvertSelected();
        EditorGUILayout.HelpBox("씬 또는 Prefab 모드에서 모델을 선택하세요. 중앙 상호작용 앵커·트리거·기존 루팅 컴포넌트를 미리 배치합니다. Ctrl+Z 가능. 기존 물리 충돌·메시·레이어는 유지합니다.",MessageType.None);
    }
    private static void ConvertSelected()
    {
        var obj=Selection.activeGameObject;
        if(obj==null||!obj.scene.IsValid()||Source?.LootTable==null)return;
        int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("루팅 오브젝트화");
        var loot=obj.GetComponent<LootableObject>()??Undo.AddComponent<LootableObject>(obj);
        if(obj.GetComponent<StructureLootInventoryRuntime>()==null)Undo.AddComponent<StructureLootInventoryRuntime>(obj);
        var data=new SerializedObject(loot);data.Update();
        data.FindProperty("_lootObject").objectReferenceValue=Source;
        data.FindProperty("_openRequestChannel").objectReferenceValue=OpenChannel;
        data.FindProperty("_overlayRequestChannel").objectReferenceValue=OverlayChannel;data.ApplyModifiedProperties();
        var center=new Bounds(obj.transform.position,Vector3.zero);bool first=true;
        foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))
            if(first){center=renderer.bounds;first=false;}else center.Encapsulate(renderer.bounds);
        var existing=obj.transform.Find("LootInteractionCenter");
        var anchorObject=existing!=null?existing.gameObject:new GameObject("LootInteractionCenter");
        if(existing==null)Undo.RegisterCreatedObjectUndo(anchorObject,"중앙 앵커");
        else Undo.RecordObject(anchorObject.transform,"중앙 앵커 이동");
        anchorObject.layer=obj.layer;anchorObject.transform.SetParent(obj.transform,false);anchorObject.transform.position=center.center;
        var anchor=obj.GetComponent<InteractionPromptAnchor>()??Undo.AddComponent<InteractionPromptAnchor>(obj);
        var anchorData=new SerializedObject(anchor);anchorData.FindProperty("_anchor").objectReferenceValue=anchorObject.transform;anchorData.ApplyModifiedProperties();
        var trigger=anchorObject.GetComponent<SphereCollider>()??Undo.AddComponent<SphereCollider>(anchorObject);Undo.RecordObject(trigger,"루팅 트리거");trigger.isTrigger=true;trigger.radius=.8f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(loot);PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
        EditorSceneManager.MarkSceneDirty(obj.scene);Undo.CollapseUndoOperations(group);
    }
}
