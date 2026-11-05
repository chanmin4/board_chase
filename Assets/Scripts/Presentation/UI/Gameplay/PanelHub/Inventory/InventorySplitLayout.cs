using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared A-layout geometry only. Never creates slots, edits inventories, or changes item indices.</summary>
public static class InventorySplitLayout
{
    public static readonly Vector2 PanelSize = new(900f, 760f);
    public const int Columns = 6;
    public const int VisibleRows = 5;
    public const float CellSize = 88f;
    public const float CellGap = 6f;
    public const float Outer = 24f;
    public const float ContentTop = 84f;
    public const float GearWidth = 260f;
    public const float PassiveHeight = 272f;
    public const float EquipmentTop = ContentTop + PassiveHeight + 12f;
    public const float EquipmentHeight = 260f;
    public const float ColumnGap = 16f;
    public const float StorageLeft = Outer + GearWidth + ColumnGap;
    public const float HeaderHeight = 28f;
    public const float PocketHeight = HeaderHeight + CellSize + 16f;
    public const float InventoryTop = ContentTop + PocketHeight + 12f;
    public const float InventoryHeight = HeaderHeight + CellSize * VisibleRows + CellGap * (VisibleRows - 1) + 16f;

    public static void Box(RectTransform rect,float x,float y,float width,float height)
    {
        if(rect==null)return;
        rect.anchorMin=rect.anchorMax=new Vector2(0f,1f);rect.pivot=new Vector2(0f,1f);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;
    }
    public static void Fill(RectTransform rect,Vector2 min,Vector2 max)
    {
        if(rect==null)return;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);
        rect.offsetMin=min;rect.offsetMax=max;rect.localScale=Vector3.one;
    }
    public static void Grid(RectTransform rect)
    {
        if(rect==null||!rect.TryGetComponent(out GridLayoutGroup grid))return;
        grid.cellSize=new Vector2(CellSize,CellSize);grid.spacing=new Vector2(CellGap,CellGap);
        grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=Columns;
        grid.childAlignment=TextAnchor.UpperLeft;grid.enabled=true;
    }
    public static void EquipmentBody(RectTransform body)
    {
        if(body!=null&&body.TryGetComponent(out GridLayoutGroup grid))grid.enabled=false;
    }
    public static void EquipmentSlot(Component slot,int index)
    {
        if(slot==null)return;
        var rect=slot.transform as RectTransform;
        float scale=index<4?.56f:.68f;
        float x=index<4?9f+index*62f:18f+(index-4)*80f;
        float y=index<4?8f:108f;
        Box(rect,x,y,100f,150f);if(rect!=null)rect.localScale=Vector3.one*scale;
    }
    public static void PassiveSlot(Component slot,int index)
    {
        if(slot==null)return;
        float x=4f+(index/2)*63f;
        float y=8f+(index%2)*112f;
        var rect=slot.transform as RectTransform;Box(rect,x,y,100f,150f);
        if(rect!=null)rect.localScale=Vector3.one*.58f;
    }
    public static void Scroll(RectTransform scroll,RectTransform content)
    {
        Fill(scroll,new Vector2(8f,8f),new Vector2(-8f,-(HeaderHeight+8f)));
        if(content==null)return;
        Grid(content);content.anchorMin=new Vector2(0f,1f);content.anchorMax=Vector2.one;
        content.pivot=new Vector2(.5f,1f);content.sizeDelta=new Vector2(0f,content.sizeDelta.y);
        content.anchoredPosition=new Vector2(0f,content.anchoredPosition.y);
        if(content.TryGetComponent(out ContentSizeFitter fit)){fit.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;fit.enabled=true;}
        if(scroll!=null&&scroll.TryGetComponent(out ScrollRect scroller)){scroller.horizontal=false;scroller.vertical=true;scroller.movementType=ScrollRect.MovementType.Clamped;scroller.content=content;}
    }
}
