using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>A compact ring in the existing player-health status strip, not a second HUD canvas.</summary>
public sealed class InfectionAggravationGaugeUI:MaskableGraphic
{
    private EntityStatusEffectRuntime _runtime;
    private TMP_Text _text;
    private float _value;
    public static GameObject Create(RectTransform parent, EntityStatusEffectRuntime runtime, float size)
    {
        var go=new GameObject("감염악화 원형",typeof(RectTransform),typeof(LayoutElement),typeof(InfectionAggravationGaugeUI));
        go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.sizeDelta=new Vector2(size,size);
        var le=go.GetComponent<LayoutElement>();le.preferredWidth=le.preferredHeight=size;
        go.GetComponent<InfectionAggravationGaugeUI>()._runtime=runtime;
        return go;
    }
    protected override void Awake()
    {
        base.Awake();color=InfectionAggravationStatusSO.Active!=null?InfectionAggravationStatusSO.Active.IconColor:new Color(.95f,.65f,.15f);raycastTarget=false;
        var child=new GameObject("감염 스택",typeof(RectTransform),typeof(TextMeshProUGUI));child.transform.SetParent(transform,false);
        _text=child.GetComponent<TextMeshProUGUI>();_text.fontSize=12;_text.color=Color.white;_text.alignment=TextAlignmentOptions.Center;_text.raycastTarget=false;
        _text.rectTransform.anchorMin=Vector2.zero;_text.rectTransform.anchorMax=Vector2.one;_text.rectTransform.offsetMin=_text.rectTransform.offsetMax=Vector2.zero;
    }
    private void Update()
    {
        if(_runtime==null)return;
        int stacks=_runtime.InfectionAggravationStacks;
        float value=Mathf.Clamp01(_runtime.InfectionAggravationGauge/_runtime.InfectionAggravationCapacity);
        if(!Mathf.Approximately(_value,value)){_value=value;SetVerticesDirty();}
        _text.text=stacks.ToString();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var rect=rectTransform.rect;Vector2 center=rect.center;
        float radius=Mathf.Min(rect.width,rect.height)*.48f;const int segments=48;
        for(int i=0;i<segments;i++)
        {
            float a=(90-i*360f/segments)*Mathf.Deg2Rad,b=(90-(i+1)*360f/segments)*Mathf.Deg2Rad;
            Color c=i<segments*_value?color:new Color(.2f,.25f,.25f,.65f);int start=mesh.currentVertCount;
            mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);
            mesh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,c,Vector2.zero);
            mesh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(radius-3),c,Vector2.zero);
            mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius-3),c,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
