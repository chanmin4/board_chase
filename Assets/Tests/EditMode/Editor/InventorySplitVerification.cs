using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Explicit batch-only, transient prefab fixture. Does not save scenes, assets, inventories or profiles.</summary>
public static class InventorySplitVerification
{
    private const string Output="Library/InventorySplitVerification";
    private static int checks;
    private static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use the explicit batch verification command.");
        Directory.CreateDirectory(Output);
        try { VerifyAndRender();File.WriteAllText(Output+"/result.txt","PASS "+checks+" native prefab/layout/scroll checks. PNG uses transient test items; no gameplay inventories were changed.");EditorApplication.Exit(0); }
        catch(Exception ex){File.WriteAllText(Output+"/result.txt","FAIL "+ex);Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    private static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(target);
    private static void VerifyAndRender()
    {
        var hold=new GameObject("Inventory split verification fixture");hold.SetActive(false);
        var camera=new GameObject("UI verification camera",typeof(Camera)).GetComponent<Camera>();
        camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.14f,.2f);
        camera.cullingMask=1<<5;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var canvasGO=new GameObject("Verification canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvasGO.transform.SetParent(hold.transform,false);
        var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var scaler=canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        var prefabs=new[]{"PlayerInventoryPanel","EnemyInventoryPanel"};
        var panels=new GameObject[2];
        for(int side=0;side<2;side++)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/"+prefabs[side]+".prefab");Check(prefab!=null,"Prefab missing "+prefabs[side]);
            var panel=Object.Instantiate(prefab,canvasGO.transform);panels[side]=panel;
            foreach(var behaviour in panel.GetComponentsInChildren<MonoBehaviour>(true))
                if(behaviour is not Graphic && behaviour is not LayoutGroup && behaviour is not ContentSizeFitter && behaviour is not ScrollRect && behaviour is not AspectRatioFitter)
                    behaviour.enabled=false;
            foreach(var t in panel.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
            var root=(RectTransform)panel.transform;root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=new Vector2(side==0?-470:470,0);root.sizeDelta=InventorySplitLayout.PanelSize;root.localScale=Vector3.one;
            if(side==0)
            {
                var ui=panel.GetComponent<PlayerInventoryUI>();
                typeof(PlayerInventoryUI).GetMethod("EnsureSeparatedInventorySectionLayout",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,null);
                ui.ApplyCompactPanelLayout();
            }
            else panel.GetComponent<EnemyInventoryUI>().ApplyContextPresentation();
            var area=side==0?panel.transform.Find("PlayerInventorySection/InventoryArea"):panel.transform.Find("InventoryArea");
            var inventory=area.Find("InventorySection");var pocket=area.Find("PocketSection");
            Check(inventory!=null&&pocket!=null,"Sections missing");
            var scroll=inventory.GetComponentInChildren<ScrollRect>(true);var pocketScroll=pocket.GetComponentInChildren<ScrollRect>(true);
            Check(scroll!=null&&pocketScroll!=null,"Authored scroll views missing");
            Check(scroll.content!=pocketScroll.content,"Pocket and inventory share content");
            var equipment=area.Find("EquipmentSection");
            Check(((RectTransform)equipment).anchoredPosition.x==24,"Gear is not left column");
            Check(((RectTransform)pocket).anchoredPosition.x==300,"Pocket is not right column");
            Check(((RectTransform)inventory).sizeDelta.y==508,"Inventory viewport section not five rows");
            var template=side==0?(InventoryItemSlotUI)Field(panel.GetComponent<PlayerInventoryUI>(),"_inventorySlotPrefab"):
                (InventoryItemSlotUI)Field(panel.GetComponent<EnemyInventoryUI>(),"_inventorySlotPrefab");
            var items=AssetDatabase.FindAssets("t:ItemSO",new[]{"Assets/ScriptableObject/GameScene/GameItem"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItemSO>).Where(x=>x!=null).Take(12).ToArray();
            Check(items.Length>0,"No test item definitions");
            AddCells(template,scroll.content,36,items);AddCells(template,pocketScroll.content,6,items);
            var passives=panel.GetComponentsInChildren<PassiveInventorySlotUI>(true);
            var passiveItems=Resources.LoadAll<PassiveItemSO>("PassiveModules");
            for(int i=0;i<passives.Length;i++)passives[i].BindPassive(i<2&&passiveItems.Length>0?passiveItems[i%passiveItems.Length]:null,i<2);
            foreach(var cg in panel.GetComponentsInChildren<CanvasGroup>(true))if(cg.GetComponent<PassiveInventorySlotUI>()==null)cg.alpha=1;
            foreach(var title in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if(title.name=="PlayerText"||title.name=="EnemyNameText")title.text=side==0?"플레이어":"적 소지품";
                if(title.name=="PocketTitle")title.text="포켓";
                if(title.name=="InventoryTitle"||title.name=="EquipText"&&title.transform.IsChildOf(inventory))title.text="인벤토리";
            }
        }
        var rt=new RenderTexture(1920,1080,24);rt.Create();camera.targetTexture=rt;hold.SetActive(true);
        foreach(var panel in panels)foreach(var cg in panel.GetComponentsInChildren<CanvasGroup>(true))if(cg.GetComponent<PassiveInventorySlotUI>()==null)cg.alpha=1;
        Canvas.ForceUpdateCanvases();
        foreach(var panel in panels)
        {
            foreach(var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);Canvas.ForceUpdateCanvases();
                if(scroll.name=="InventoryScrollView")
                {
                    Check(Mathf.Abs(scroll.viewport.rect.height-464)<.5f,"Viewport does not expose exactly five rows: "+scroll.viewport.rect.height);
                    Check(scroll.content.rect.height>scroll.viewport.rect.height,"Overflow does not scroll");
                    scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();Check(scroll.content.anchoredPosition.y>80,"Scroll did not move content to sixth row");
                    scroll.verticalNormalizedPosition=1;
                }
            }
        }
        foreach(var text in canvasGO.GetComponentsInChildren<TMP_Text>(true))text.ForceMeshUpdate(true);
        Canvas.ForceUpdateCanvases();
        if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});else camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;
        var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
        File.WriteAllBytes(Output+"/inventory-split-native.png",texture.EncodeToPNG());RenderTexture.active=previous;
        Object.DestroyImmediate(texture);camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(hold);Object.DestroyImmediate(camera.gameObject);
    }
    private static void AddCells(InventoryItemSlotUI template,RectTransform root,int count,ItemSO[] items)
    {
        foreach(Transform child in root)child.gameObject.SetActive(false);
        for(int i=0;i<count;i++)
        {
            var slot=Object.Instantiate(template,root);slot.gameObject.SetActive(true);slot.enabled=false;
            if(i<9)slot.BindItem(items[i%items.Length],i==0?99:1);
        }
    }
}
