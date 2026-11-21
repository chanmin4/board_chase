using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class PlayerRecyclingPanelUI
{
    private GameObject _craftPanel;
    private TMP_Text _craftFeedback;
    private readonly List<(CraftingRecipe recipe, TMP_Text label, Button button)> _craftRows = new();
    private float _nextCraftRefresh;

    private void InitializeCrafting()
    {
        var open=MakeButton("제작",transform,new Vector2(-245,-28),new Vector2(150,34));
        _craftPanel=new GameObject("HOME 제작",typeof(RectTransform),typeof(Image));
        _craftPanel.transform.SetParent(transform,false);
        var rect=(RectTransform)_craftPanel.transform;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);
        rect.anchoredPosition=new Vector2(0,-70);rect.sizeDelta=new Vector2(620,500);
        _craftPanel.GetComponent<Image>().color=new Color(.025f,.07f,.09f,.99f);
        _craftFeedback=MakeText("제작 · HOME 전용 / 현재 레시피는 제작 완료 시에만 변경",_craftPanel.transform,
            new Vector2(0,-25),new Vector2(590,40));
        var close=MakeButton("닫기",_craftPanel.transform,new Vector2(260,-470),new Vector2(80,30));
        close.onClick.AddListener(()=>_craftPanel.SetActive(false));
        var scrollGo=new GameObject("제작 스크롤",typeof(RectTransform),typeof(ScrollRect));
        scrollGo.transform.SetParent(_craftPanel.transform,false);
        var sr=(RectTransform)scrollGo.transform; sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;
        sr.offsetMin=new Vector2(12,50);sr.offsetMax=new Vector2(-12,-52);
        var view=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D));
        view.transform.SetParent(sr,false);
        var vr=(RectTransform)view.transform;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=vr.offsetMax=Vector2.zero;
        view.GetComponent<Image>().color=new Color(0,0,0,.05f);
        var content=new GameObject("레시피",typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(vr,false);content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,1);
        var catalog=CraftingCatalogSO.Active;
        int row=0;
        foreach(var recipe in catalog?.recipes ?? new())
        {
            if(recipe?.output==null) continue;
            var captured=recipe;
            var button=MakeButton("",content,new Vector2(0,-row*160-76),new Vector2(570,150));
            var label=button.GetComponentInChildren<TMP_Text>();label.fontSize=14;
            button.onClick.AddListener(()=>{
                _owner.InventoryRuntime.TryCraft(captured.id,out string message);
                _craftFeedback.text=message;RefreshCraftingRows();
            });
            _craftRows.Add((recipe,label,button));row++;
        }
        content.sizeDelta=new Vector2(580,row*160);
        var scroll=scrollGo.GetComponent<ScrollRect>();scroll.viewport=vr;scroll.content=content;
        scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=30;
        _craftPanel.SetActive(false);
        open.onClick.AddListener(()=>{
            _panel.SetActive(false);_craftPanel.SetActive(!_craftPanel.activeSelf);
            if(_craftPanel.activeSelf)
            {
                if(!_owner.InventoryRuntime.PrepareCraftRecipes(out string error))_craftFeedback.text=error;
                RefreshCraftingRows();
            }
        });
    }
    private void RefreshCraftingRows()
    {
        foreach(var row in _craftRows)
        {
            bool unlocked=row.recipe.unlockedByDefault ||
                SaveSystem.Active?.Profile.unlockedCraftRecipeIds?.Contains(row.recipe.id)==true;
            row.button.interactable=unlocked&&GameProgressionRuntime.IsHomePreparation;
            string text=row.recipe.output.name+" ×"+row.recipe.amount+(row.recipe.randomModuleMaterials?" · "+row.recipe.moduleRarity:"")+(unlocked?"":" · 제작법 미해금");
            if(_owner.InventoryRuntime.TryGetCraftRecipe(row.recipe.id,out var state,out string error))
            {
                foreach(var material in state.materials)
                {
                    var source=CraftingCatalogSO.Active.materials.Find(x=>x!=null&&x.Guid==material.itemGuid);
                    ItemSO item=source;
                    if(row.recipe.currency?.Guid==material.itemGuid)item=row.recipe.currency;
                    foreach(var fixedRow in row.recipe.fixedMaterials ?? System.Array.Empty<ShopTradeRequirement>())
                        if(fixedRow?.Item?.Guid==material.itemGuid)item=fixedRow.Item;
                    text+="\n"+(item!=null?item.name:material.itemGuid)+" "+_owner.InventoryRuntime.GetInventoryItemAmount(item)+"/"+material.amount;
                }
            }
            else {text+="\n"+error;row.button.interactable=false;}
            row.label.text=text;
        }
    }
    private void TickCrafting()
    {
        if(_craftPanel?.activeSelf!=true||Time.unscaledTime<_nextCraftRefresh)return;
        _nextCraftRefresh=Time.unscaledTime+1f;RefreshCraftingRows();
    }
}
