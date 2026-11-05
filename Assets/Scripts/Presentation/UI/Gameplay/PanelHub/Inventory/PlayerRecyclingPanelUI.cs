using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>Inventory-owned processor. Drag inventory items in; click to cancel or claim.</summary>
public sealed class PlayerRecyclingPanelUI:MonoBehaviour
{
    private PlayerInventoryUI _owner;private GameObject _panel;private RectTransform _rows;private TMP_Text _summary;private TMP_Text _feedback;private int _count=-1;
    private RuntimeContentCatalogSO _catalog;
    private GameObject _draftPanel;
    private Button _draftOpen;
    private TMP_Text _draftSummary, _draftFeedback;
    private readonly System.Collections.Generic.List<Button> _draftButtons = new();
    public void Initialize(PlayerInventoryUI owner)
    {
        _owner=owner;
        var open=MakeButton("분해기",transform,new Vector2(-80,-28),new Vector2(150,34));
        open.onClick.AddListener(()=>{if(_draftPanel!=null)_draftPanel.SetActive(false);_panel.SetActive(!_panel.activeSelf);});
        _panel=new GameObject("시간제 분해기",typeof(RectTransform),typeof(Image));_panel.transform.SetParent(transform,false);
        var rect=(RectTransform)_panel.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);
        rect.anchoredPosition=new Vector2(0,-70);rect.sizeDelta=new Vector2(470,260);
        _panel.GetComponent<Image>().color=new Color(.025f,.07f,.09f,.99f);
        _summary=MakeText("경험치 · 분해기",_panel.transform,new Vector2(0,-20),new Vector2(440,40));
        _feedback=MakeText("등록: 드래그 / 취소·완료 수령: 슬롯 클릭",_panel.transform,Vector2.zero,new Vector2(350,36));
        _feedback.rectTransform.anchorMin=_feedback.rectTransform.anchorMax=new Vector2(0,0);
        _feedback.rectTransform.pivot=new Vector2(0,.5f);_feedback.rectTransform.anchoredPosition=new Vector2(10,20);_feedback.fontSize=12;
        var close=MakeButton("닫기",_panel.transform,new Vector2(-45,-235),new Vector2(80,30));close.onClick.AddListener(()=>_panel.SetActive(false));
        var closeRect=(RectTransform)close.transform;closeRect.anchorMin=closeRect.anchorMax=new Vector2(1,0);closeRect.anchoredPosition=new Vector2(-45,20);
        var scrollObject=new GameObject("분해기 스크롤",typeof(RectTransform),typeof(ScrollRect));scrollObject.transform.SetParent(_panel.transform,false);
        var scrollRect=(RectTransform)scrollObject.transform;scrollRect.anchorMin=Vector2.zero;scrollRect.anchorMax=Vector2.one;
        scrollRect.offsetMin=new Vector2(15,45);scrollRect.offsetMax=new Vector2(-15,-50);
        var viewportObject=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D));viewportObject.transform.SetParent(scrollObject.transform,false);
        var viewport=(RectTransform)viewportObject.transform;viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=viewport.offsetMax=Vector2.zero;
        viewportObject.GetComponent<Image>().color=new Color(0,0,0,.05f);
        _rows=new GameObject("독립 타이머 슬롯",typeof(RectTransform)).GetComponent<RectTransform>();_rows.SetParent(viewport,false);
        _rows.anchorMin=_rows.anchorMax=new Vector2(.5f,1);_rows.pivot=new Vector2(.5f,1);_rows.anchoredPosition=new Vector2(0,-50);_rows.sizeDelta=new Vector2(440,160);
        _rows.anchoredPosition=Vector2.zero;
        var scroll=scrollObject.GetComponent<ScrollRect>();scroll.content=_rows;scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=25;scroll.movementType=ScrollRect.MovementType.Clamped;
        _panel.SetActive(false);
        InitializeStartingDraw();
    }
    private void InitializeStartingDraw()
    {
        _draftOpen=MakeButton("시작 패시브 추첨",transform,new Vector2(-245,-28),new Vector2(172,34));
        _draftPanel=new GameObject("HOME 시작 패시브 추첨",typeof(RectTransform),typeof(Image));
        _draftPanel.transform.SetParent(transform,false);
        var rect=(RectTransform)_draftPanel.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);
        rect.anchoredPosition=new Vector2(0,-70);rect.sizeDelta=new Vector2(520,310);
        _draftPanel.GetComponent<Image>().color=new Color(.025f,.07f,.09f,.99f);
        _draftSummary=MakeText("",_draftPanel.transform,new Vector2(0,-26),new Vector2(490,45));
        var rules=PassiveModuleRulesSO.Active;
        for(int i=0;i<(rules?.startingDraws?.Length??0);i++)
        {
            int drawIndex=i;
            var button=MakeButton("",_draftPanel.transform,new Vector2(0,-83-i*48),new Vector2(480,40));
            var buttonRect=(RectTransform)button.transform;buttonRect.anchorMin=buttonRect.anchorMax=new Vector2(.5f,1);
            button.onClick.AddListener(()=>{
                if(_owner?.InventoryRuntime==null)return;
                bool success=_owner.InventoryRuntime.TryDrawStartingPassive(drawIndex,out string message);
                _draftFeedback.text=message;UIAudio.ReportResult(success,UIAudioCueType.InventoryMove);
            });
            _draftButtons.Add(button);
        }
        _draftFeedback=MakeText("결과는 인벤토리에 지급됩니다. 전투/생존 칸에 드래그해 준비하세요.",_draftPanel.transform,new Vector2(0,-247),new Vector2(490,42));
        _draftFeedback.fontSize=13;
        var close=MakeButton("닫기",_draftPanel.transform,new Vector2(0,-288),new Vector2(90,28));
        ((RectTransform)close.transform).anchorMin=((RectTransform)close.transform).anchorMax=new Vector2(.5f,1);
        close.onClick.AddListener(()=>_draftPanel.SetActive(false));
        _draftOpen.onClick.AddListener(()=>{_panel.SetActive(false);_draftPanel.SetActive(!_draftPanel.activeSelf);});
        _draftPanel.SetActive(false);
    }
    private void RefreshStartingDraw()
    {
        if(_draftOpen==null)return;
        var definitions = PassiveModuleRulesSO.Active?.startingDraws;
        bool enabled = definitions != null && System.Array.Exists(definitions, draw => draw != null && draw.enabled);
        _draftOpen.gameObject.SetActive(enabled);
        if (!enabled) { _draftPanel.SetActive(false); return; }
        bool home=GameProgressionRuntime.IsHomePreparation;
        _draftOpen.interactable=home;
        if(!home){_draftPanel.SetActive(false);return;}
        if(!_draftPanel.activeSelf)return;
        var rules=PassiveModuleRulesSO.Active;
        _draftSummary.text=$"HOME 출발 준비 · Lv.{GameProgressionRuntime.Level}\n통합 모듈 {_owner.InventoryRuntime.GetInventoryItemAmount(rules?.unifiedModule)}개 · 남은 추첨 {_owner.InventoryRuntime.StartingPassiveDrawsRemaining}회";
        for(int i=0;i<_draftButtons.Count;i++)
        {
            var draw=rules?.startingDraws!=null&&i<rules.startingDraws.Length?rules.startingDraws[i]:null;
            _draftButtons[i].GetComponentInChildren<TMP_Text>().text=draw==null?"설정 없음":$"{draw.label} · 모듈 {Mathf.Max(2,draw.moduleCost)} · Lv.{draw.requiredLevel}";
            _draftButtons[i].interactable=draw!=null&&draw.enabled&&GameProgressionRuntime.Level>=draw.requiredLevel&&_owner.InventoryRuntime.StartingPassiveDrawsRemaining>0;
        }
    }
    private void Update()
    {
        if(_owner?.InventoryRuntime==null)return;var runtime=_owner.InventoryRuntime;
        RefreshStartingDraw();
        var profile=SaveSystem.Active?.Profile.experience;var pending=SaveSystem.Active?.RunData.experience;
        if(_summary!=null)_summary.text=$"Lv.{GameProgressionRuntime.Level} · 총 XP {profile?.total??0} · 원정 대기 XP {pending?.pending??0}";
        if(!_panel.activeSelf)return;
        if(_count!=runtime.RecyclingCapacity)
        {
            foreach(Transform child in _rows)Destroy(child.gameObject);_count=runtime.RecyclingCapacity;
            ((RectTransform)_panel.transform).sizeDelta=new Vector2(470,Mathf.Clamp(90+_count*60,260,600));
            _rows.sizeDelta=new Vector2(440,_count*60);
            for(int i=0;i<_count;i++)
            {
                int slot=i;var button=MakeButton("",_rows,new Vector2(-220,-i*60),new Vector2(440,54));
                var r=(RectTransform)button.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(0,1);
                var target=button.gameObject.AddComponent<RecyclingDropTarget>();target.owner=_owner;target.index=slot;target.panel=this;
                button.onClick.AddListener(()=>{runtime.TryCollectOrCancelRecycling(slot,out string message);ShowMessage(message);});
            }
        }
        var jobs=runtime.RecyclingSlots;
        for(int i=0;i<_rows.childCount;i++)
        {
            var text=_rows.GetChild(i).GetComponentInChildren<TMP_Text>();var job=jobs!=null&&i<jobs.Count?jobs[i]:null;
            if(job==null||job.IsEmpty){text.text=$"{i+1} · 아이템 드래그";continue;}
            _catalog??=Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
            string label=job.input.itemGuid;
            if(_catalog!=null&&_catalog.TryResolveItem(job.input.itemGuid,out var input))label=InventoryItemDisplayUtility.ResolveItemName(input);
            text.text=job.remaining<=0?$"{i+1} · {label}\n완료 — 클릭해서 받기":$"{i+1} · {label}\n{job.remaining:0.0}초 — 클릭하면 취소·회수";
        }
    }
    public void ShowMessage(string message){if(_feedback!=null)_feedback.text=message;Debug.Log("[Recycling] "+message);}
    private static Button MakeButton(string text,Transform parent,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
        var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(1,1);r.anchoredPosition=pos;r.sizeDelta=size;
        go.GetComponent<Image>().color=new Color(.04f,.28f,.34f,.98f);
        var label=MakeText(text,go.transform,Vector2.zero,size);label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,.5f);
        return go.GetComponent<Button>();
    }
    private static TMP_Text MakeText(string text,Transform parent,Vector2 pos,Vector2 size)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var t=go.GetComponent<TextMeshProUGUI>();t.text=text;t.fontSize=15;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
        t.rectTransform.anchorMin=t.rectTransform.anchorMax=new Vector2(.5f,1);t.rectTransform.anchoredPosition=pos;t.rectTransform.sizeDelta=size;return t;
    }
}
public sealed class RecyclingDropTarget:MonoBehaviour,IDropHandler
{
    public PlayerInventoryUI owner;public int index;public PlayerRecyclingPanelUI panel;
    public void OnDrop(PointerEventData data)
    {
        if(InventoryDragContext.SourceKind!=InventoryDragSourceKind.Inventory||owner?.InventoryRuntime==null)return;
        owner.InventoryRuntime.TryStartRecycling(InventoryDragContext.InventoryIndex,index,out string message);panel?.ShowMessage(message);
    }
}
